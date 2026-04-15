using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using biblioteka.Services;

namespace biblioteka
{
    public partial class SalesReportWindow : Window
    {
        private readonly SaleService _saleService;
        private SaleReportDto _report;

        public SalesReportWindow()
        {
            InitializeComponent();
            _saleService = new SaleService();
            StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);
            EndDatePicker.SelectedDate = DateTime.Today;
            LoadReport();
        }

        private void LoadReport()
        {
            try
            {
                var startDate = StartDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-1);
                var endDate = EndDatePicker.SelectedDate ?? DateTime.Today;

                _report = _saleService.GetReport(startDate, endDate);
                SalesDataGrid.ItemsSource = _report.Sales;
                UpdateTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки отчета: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTotals()
        {
            if (_report != null && _report.Sales != null && _report.Sales.Count > 0)
            {
                TotalText.Text = $"Всего продаж: {_report.Sales.Count} | " +
                                $"Продано книг: {_report.TotalQuantity} | " +
                                $"Общая сумма: {_report.TotalAmount:F2} BYN";
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
                if (_report == null || _report.Sales == null || _report.Sales.Count == 0)
                {
                    MessageBox.Show("Нет данных для печати", "Печать",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var printVisual = CreatePrintVisual();
                    printDialog.PrintVisual(printVisual,
                        $"Отчет по продажам с {_report.StartDate:dd.MM.yyyy} по {_report.EndDate:dd.MM.yyyy}");

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
                Text = $"Период: {_report.StartDate:dd.MM.yyyy} - {_report.EndDate:dd.MM.yyyy}",
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = Brushes.Black
            });

            // Таблица
            var grid = new Grid();
            grid.Margin = new Thickness(0, 0, 0, 20);

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

            int totalRows = 1 + _report.Sales.Count;
            for (int i = 0; i < totalRows; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            string[] headers = { "Дата", "Книга", "Покупатель", "Кол-во", "Цена", "Сумма", "Примечание" };
            TextAlignment[] alignments = { TextAlignment.Center, TextAlignment.Left, TextAlignment.Left,
                                          TextAlignment.Center, TextAlignment.Right, TextAlignment.Right, TextAlignment.Left };

            // Заголовки
            for (int i = 0; i < headers.Length; i++)
            {
                var border = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0.5),
                    Padding = new Thickness(5, 3, 5, 3),
                    Background = Brushes.White
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

            // Данные
            int rowIndex = 1;
            foreach (var sale in _report.Sales)
            {
                Color bgColor = (rowIndex % 2 == 0) ? Color.FromRgb(240, 240, 240) : Colors.White;

                AddPrintCell(grid, rowIndex, 0, sale.SaleDate.ToString("dd.MM.yyyy"), TextAlignment.Center, bgColor);
                AddPrintCell(grid, rowIndex, 1, sale.BookTitle ?? "", TextAlignment.Left, bgColor);
                AddPrintCell(grid, rowIndex, 2, sale.Buyer ?? "", TextAlignment.Left, bgColor);
                AddPrintCell(grid, rowIndex, 3, sale.Quantity.ToString(), TextAlignment.Center, bgColor);
                AddPrintCell(grid, rowIndex, 4, sale.UnitPrice.ToString("F2") + " BYN", TextAlignment.Right, bgColor);
                AddPrintCell(grid, rowIndex, 5, sale.TotalAmount.ToString("F2") + " BYN", TextAlignment.Right, bgColor);
                AddPrintCell(grid, rowIndex, 6, sale.Notes ?? "", TextAlignment.Left, bgColor);
                rowIndex++;
            }

            printContainer.Children.Add(grid);

            // Итоги
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