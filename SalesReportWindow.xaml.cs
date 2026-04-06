using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace biblioteka
{
    public partial class SalesReportWindow : Window
    {
        private DataTable _reportData;

        public SalesReportWindow()
        {
            InitializeComponent();
            StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);
            EndDatePicker.SelectedDate = DateTime.Today;
            LoadReport();
        }

        private void LoadReport()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    string query = @"
                        SELECT 
                            CAST(s.SaleDate AS DATE) AS SaleDate,
                            b.Title AS BookTitle,
                            r.FullName AS Buyer,
                            s.Quantity,
                            s.UnitPrice,
                            s.TotalAmount,
                            ISNULL(s.Notes, '—') AS Notes
                        FROM Sales s
                        JOIN Books b ON s.BookID = b.ID
                        JOIN Readers r ON s.ReaderID = r.ID
                        WHERE (@StartDate IS NULL OR s.SaleDate >= @StartDate)
                          AND (@EndDate IS NULL OR s.SaleDate <= @EndDate)
                        ORDER BY s.SaleDate DESC";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@StartDate",
                            StartDatePicker.SelectedDate.HasValue ? (object)StartDatePicker.SelectedDate.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@EndDate",
                            EndDatePicker.SelectedDate.HasValue ? (object)EndDatePicker.SelectedDate.Value.AddDays(1) : DBNull.Value);

                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            _reportData = new DataTable();
                            adapter.Fill(_reportData);

                            if (_reportData.Columns.Contains("BookTitle"))
                                _reportData.Columns["BookTitle"].ColumnName = "Title";

                            SalesDataGrid.ItemsSource = _reportData.DefaultView;
                            UpdateTotals();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки отчета: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTotals()
        {
            if (_reportData != null && _reportData.Rows.Count > 0)
            {
                decimal total = 0;
                int totalBooks = 0;
                foreach (DataRow row in _reportData.Rows)
                {
                    total += Convert.ToDecimal(row["TotalAmount"]);
                    totalBooks += Convert.ToInt32(row["Quantity"]);
                }
                TotalText.Text = $"Всего продаж: {_reportData.Rows.Count} | Продано книг: {totalBooks} | Общая сумма: {total:F2} BYN";
            }
            else
            {
                TotalText.Text = "Нет данных за выбранный период";
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            LoadReport();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_reportData == null || _reportData.Rows.Count == 0)
                {
                    MessageBox.Show("Нет данных для печати", "Печать",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var printVisual = CreatePrintVisual();
                    printDialog.PrintVisual(printVisual, $"Отчет по продажам с {StartDatePicker.SelectedDate.Value:dd.MM.yyyy} по {EndDatePicker.SelectedDate.Value:dd.MM.yyyy}");

                    MessageBox.Show("Отчет отправлен на печать!", "Печать",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при печати: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FrameworkElement CreatePrintVisual()
        {
            var printContainer = new StackPanel
            {
                Background = Brushes.White,
                Margin = new Thickness(50)
            };

            // Заголовок
            printContainer.Children.Add(new TextBlock
            {
                Text = "ОТЧЕТ ПО ПРОДАЖАМ",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = Brushes.Black
            });

            // Период
            printContainer.Children.Add(new TextBlock
            {
                Text = $"Период: {StartDatePicker.SelectedDate.Value:dd.MM.yyyy} - {EndDatePicker.SelectedDate.Value:dd.MM.yyyy}",
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = Brushes.Black
            });

            // Таблица
            var grid = new Grid();
            grid.Margin = new Thickness(0, 0, 0, 20);

            // Определяем колонки с фиксированной шириной
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });   // Дата
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });  // Книга
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });  // Покупатель
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });   // Кол-во
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });   // Цена
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });   // Сумма
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });  // Примечание

            // Определяем строки: заголовок + каждая запись
            int totalRows = 1 + _reportData.Rows.Count;
            for (int i = 0; i < totalRows; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // Заголовки колонок (без фона, просто жирный текст)
            string[] headers = { "Дата", "Книга", "Покупатель", "Кол-во", "Цена", "Сумма", "Примечание" };
            TextAlignment[] alignments = { TextAlignment.Center, TextAlignment.Left, TextAlignment.Left,
                                          TextAlignment.Center, TextAlignment.Right, TextAlignment.Right, TextAlignment.Left };

            for (int i = 0; i < headers.Length; i++)
            {
                var border = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0.5),
                    Padding = new Thickness(5, 3, 5, 3),
                    Background = Brushes.White // белый фон
                };

                var header = new TextBlock
                {
                    Text = headers[i],
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black,
                    TextAlignment = alignments[i],
                    VerticalAlignment = VerticalAlignment.Center
                };

                border.Child = header;
                Grid.SetRow(border, 0);
                Grid.SetColumn(border, i);
                grid.Children.Add(border);
            }

            // Заполняем данными с чередованием фона
            int rowIndex = 1;
            foreach (DataRowView item in _reportData.DefaultView)
            {
                // чередование фона: чётные строки светло-серые, нечётные белые
                Color bgColor = (rowIndex % 2 == 0) ? Color.FromRgb(240, 240, 240) : Colors.White;

                AddPrintCell(grid, rowIndex, 0, Convert.ToDateTime(item["SaleDate"]).ToString("dd.MM.yyyy"), TextAlignment.Center, bgColor);
                AddPrintCell(grid, rowIndex, 1, item["Title"]?.ToString() ?? "", TextAlignment.Left, bgColor);
                AddPrintCell(grid, rowIndex, 2, item["Buyer"]?.ToString() ?? "", TextAlignment.Left, bgColor);
                AddPrintCell(grid, rowIndex, 3, item["Quantity"]?.ToString() ?? "", TextAlignment.Center, bgColor);
                AddPrintCell(grid, rowIndex, 4, Convert.ToDecimal(item["UnitPrice"]).ToString("F2") + " BYN", TextAlignment.Right, bgColor);
                AddPrintCell(grid, rowIndex, 5, Convert.ToDecimal(item["TotalAmount"]).ToString("F2") + " BYN", TextAlignment.Right, bgColor);
                AddPrintCell(grid, rowIndex, 6, item["Notes"]?.ToString() ?? "", TextAlignment.Left, bgColor);
                rowIndex++;
            }

            printContainer.Children.Add(grid);

            // Итоги (тоже без фона, просто жирный текст)
            var totalsBorder = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 10, 0, 0),
                Background = Brushes.White
            };

            var totals = new TextBlock
            {
                Text = TotalText.Text.Replace(" | ", "    "),
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                TextAlignment = TextAlignment.Center
            };
            totalsBorder.Child = totals;
            printContainer.Children.Add(totalsBorder);

            // Дата печати
            printContainer.Children.Add(new TextBlock
            {
                Text = $"Дата печати: {DateTime.Now:dd.MM.yyyy HH:mm}",
                FontSize = 10,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 30, 0, 0),
                FontStyle = FontStyles.Italic,
                HorizontalAlignment = HorizontalAlignment.Right
            });

            return new ScrollViewer { Content = printContainer, Padding = new Thickness(10) };
        }

        private void AddPrintCell(Grid grid, int row, int column, string text, TextAlignment alignment, Color bgColor)
        {
            var border = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(5, 3, 5, 3),
                Background = new SolidColorBrush(bgColor)
            };

            var cell = new TextBlock
            {
                Text = text,
                Foreground = Brushes.Black,
                TextAlignment = alignment,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.Child = cell;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, column);
            grid.Children.Add(border);
        }
    }
}