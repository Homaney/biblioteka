using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace biblioteka
{
    public partial class ReadersInfoWindow : Window
    {
        private int currentReaderId;
        private bool isHistoryView = false; // Флаг для отслеживания текущей вкладки

        public ReadersInfoWindow()
        {
            InitializeComponent();
            LoadReaders();
        }

        private void LoadReaders()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT ID, FullName, Phone, Address, RegistrationDate, BirthDate 
                        FROM Readers ORDER BY FullName";

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);
                        ReadersList.ItemsSource = table.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке читателей: " + ex.Message);
            }
        }

        private void ReadersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReadersList.SelectedItem is DataRowView readerRow)
            {
                currentReaderId = Convert.ToInt32(readerRow["ID"]);

                // Основная информация
                FullNameText.Text = readerRow["FullName"].ToString();
                PhoneText.Text = readerRow["Phone"]?.ToString() ?? "не указан";
                AddressText.Text = readerRow["Address"]?.ToString() ?? "не указан";
                BirthDateText.Text = GetDateString(readerRow["BirthDate"]);
                RegistrationDateText.Text = GetDateString(readerRow["RegistrationDate"]);

                // Сбрасываем списки перед загрузкой
                CurrentBooksList.ItemsSource = null;
                HistoryBooksList.ItemsSource = null;

                // Загружаем книги в зависимости от текущей вкладки
                if (isHistoryView)
                {
                    LoadHistoryBooks(currentReaderId);
                }
                else
                {
                    LoadCurrentBooks(currentReaderId);
                }

                // Показываем карточку с анимацией
                ReaderCard.Visibility = Visibility.Visible;
                DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                ReaderCard.BeginAnimation(OpacityProperty, fadeIn);
                SelectHint.Visibility = Visibility.Collapsed;
                PrintButton.Visibility = Visibility.Visible;
            }
            else
            {
                PrintButton.Visibility = Visibility.Collapsed;
            }
        }

        private string GetDateString(object dateValue)
        {
            return dateValue != DBNull.Value ? Convert.ToDateTime(dateValue).ToShortDateString() : "не указана";
        }

        private void LoadCurrentBooks(int readerId)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT ib.ID AS IssuedId, b.Title AS BookTitle, bi.InventoryNumber, 
                               ib.IssueDate, ib.PlannedReturnDate, ib.ActualReturnDate, ib.Status
                        FROM IssuedBooks ib
                        JOIN BookInstances bi ON ib.InstanceID = bi.ID
                        JOIN Books b ON bi.BookID = b.ID
                        WHERE ib.ReaderID = @ReaderID AND ib.Status = N'Выдана'";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@ReaderID", readerId);
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable table = new DataTable();
                            adapter.Fill(table);

                            var books = new List<dynamic>();
                            foreach (DataRow row in table.Rows)
                            {
                                DateTime issue = Convert.ToDateTime(row["IssueDate"]);
                                DateTime planned = Convert.ToDateTime(row["PlannedReturnDate"]);
                                bool overdue = planned < DateTime.Now && row["ActualReturnDate"] == DBNull.Value;
                                bool warning = (planned - DateTime.Now).TotalDays <= 3 &&
                                              (planned - DateTime.Now).TotalDays > 0 &&
                                              row["ActualReturnDate"] == DBNull.Value;

                                string statusText = overdue ? "ПРОСРОЧЕНА" :
                                                    warning ? "СКОРО ВОЗВРАТ" : "В СРОК";
                                SolidColorBrush statusColor = overdue ? new SolidColorBrush(Colors.Red) :
                                                            warning ? new SolidColorBrush(Colors.Yellow) :
                                                            new SolidColorBrush(Colors.Green);

                                books.Add(new
                                {
                                    IssuedId = row["IssuedId"],
                                    BookTitle = row["BookTitle"].ToString(),
                                    InventoryNumber = "Инв. №: " + row["InventoryNumber"].ToString(),
                                    Status = $"Выдано: {issue:dd.MM.yyyy} • Возврат: {planned:dd.MM.yyyy} • {statusText}",
                                    StatusColor = statusColor,
                                    CardColor = overdue ? new SolidColorBrush(Color.FromArgb(30, 255, 118, 117)) :
                                              warning ? new SolidColorBrush(Color.FromArgb(30, 253, 203, 110)) :
                                              new SolidColorBrush(Color.FromArgb(30, 0, 184, 148))
                                });
                            }
                            CurrentBooksList.ItemsSource = books;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке текущих книг: " + ex.Message);
            }
        }

        private void LoadHistoryBooks(int readerId)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT ib.ID AS IssuedId, b.Title AS BookTitle, bi.InventoryNumber, 
                               ib.IssueDate, ib.PlannedReturnDate, ib.ActualReturnDate, ib.Status
                        FROM IssuedBooks ib
                        JOIN BookInstances bi ON ib.InstanceID = bi.ID
                        JOIN Books b ON bi.BookID = b.ID
                        WHERE ib.ReaderID = @ReaderID AND ib.Status = 'Возвращена'
                        ORDER BY ib.ActualReturnDate DESC";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@ReaderID", readerId);
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable table = new DataTable();
                            adapter.Fill(table);

                            var books = new List<dynamic>();
                            foreach (DataRow row in table.Rows)
                            {
                                DateTime issue = Convert.ToDateTime(row["IssueDate"]);
                                DateTime planned = Convert.ToDateTime(row["PlannedReturnDate"]);
                                DateTime actual = Convert.ToDateTime(row["ActualReturnDate"]);

                                TimeSpan difference = actual - planned;
                                int daysDifference = (int)difference.TotalDays;
                                bool returnedOnTime = daysDifference <= 0;

                                string statusText, returnInfo;
                                SolidColorBrush statusColor;

                                if (returnedOnTime)
                                {
                                    statusText = daysDifference < 0 ? "ВОЗВРАЩЕНА ДОСРОЧНО" : "ВОЗВРАЩЕНА ВОВРЕМЯ";
                                    returnInfo = daysDifference < 0 ?
                                        $"Возвращена: {actual:dd.MM.yyyy} (досрочно на {-daysDifference} дн.)" :
                                        $"Возвращена: {actual:dd.MM.yyyy} (в срок)";
                                    statusColor = new SolidColorBrush(Colors.Green);
                                }
                                else
                                {
                                    statusText = "ВОЗВРАЩЕНА С ОПОЗДАНИЕМ";
                                    returnInfo = $"Возвращена: {actual:dd.MM.yyyy} (опоздание: {daysDifference} дн.)";
                                    statusColor = new SolidColorBrush(Colors.Orange);
                                }

                                books.Add(new
                                {
                                    IssuedId = row["IssuedId"],
                                    BookTitle = row["BookTitle"].ToString(),
                                    InventoryNumber = "Инв. №: " + row["InventoryNumber"].ToString(),
                                    Status = $"Выдано: {issue:dd.MM.yyyy} • План возврата: {planned:dd.MM.yyyy} • {statusText}",
                                    StatusColor = statusColor,
                                    ReturnInfo = returnInfo
                                });
                            }
                            HistoryBooksList.ItemsSource = books;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке истории книг: " + ex.Message);
            }
        }

        private void ReturnBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int issuedId)
            {
                var confirm = MessageBox.Show("Вы уверены, что хотите вернуть книгу?", "Подтверждение возврата",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        connection.Open();

                        int instanceId;
                        DateTime plannedReturnDate;
                        using (SqlCommand getInfoCmd = new SqlCommand(
                            "SELECT InstanceID, PlannedReturnDate FROM IssuedBooks WHERE ID = @ID", connection))
                        {
                            getInfoCmd.Parameters.AddWithValue("@ID", issuedId);
                            using (SqlDataReader reader = getInfoCmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    instanceId = Convert.ToInt32(reader["InstanceID"]);
                                    plannedReturnDate = Convert.ToDateTime(reader["PlannedReturnDate"]);
                                }
                                else
                                {
                                    MessageBox.Show("❌ Не удалось найти информацию о выдаче!", "Ошибка",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
                            }
                        }

                        DateTime actualReturnDate = DateTime.Now;
                        TimeSpan difference = actualReturnDate - plannedReturnDate;
                        int daysDifference = (int)difference.TotalDays;
                        bool returnedOnTime = daysDifference <= 0;

                        // Обновляем IssuedBooks
                        using (SqlCommand updateIssued = new SqlCommand(
                            "UPDATE IssuedBooks SET ActualReturnDate = @ActualReturnDate, Status = N'Возвращена', ReturnedOnTime = @ReturnedOnTime WHERE ID = @ID", connection))
                        {
                            updateIssued.Parameters.AddWithValue("@ActualReturnDate", actualReturnDate);
                            updateIssued.Parameters.AddWithValue("@ReturnedOnTime", returnedOnTime);
                            updateIssued.Parameters.AddWithValue("@ID", issuedId);
                            updateIssued.ExecuteNonQuery();
                        }

                        // Обновляем BookInstances
                        using (SqlCommand updateInstance = new SqlCommand(
                            "UPDATE BookInstances SET Status = N'Доступна' WHERE ID = @ID", connection))
                        {
                            updateInstance.Parameters.AddWithValue("@ID", instanceId);
                            updateInstance.ExecuteNonQuery();
                        }

                        string message = returnedOnTime ?
                            (daysDifference < 0 ? $"✅ Книга возвращена досрочно! (на {-daysDifference} дней раньше)" : "✅ Книга возвращена вовремя!") :
                            $"⚠️ Книга возвращена с опозданием на {daysDifference} дней!";

                        MessageBox.Show(message, "Возврат книги", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Перезагружаем списки книг для текущего читателя
                        if (isHistoryView)
                        {
                            LoadHistoryBooks(currentReaderId);
                        }
                        else
                        {
                            LoadCurrentBooks(currentReaderId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ Ошибка при возврате книги: " + ex.Message, "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SwitchToCurrentBooks(object sender, RoutedEventArgs e)
        {
            isHistoryView = false;

            CurrentBooksTab.Background = new SolidColorBrush(Color.FromRgb(108, 92, 231));
            CurrentBooksTab.Foreground = Brushes.White;
            HistoryTab.Background = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            HistoryTab.Foreground = new SolidColorBrush(Color.FromRgb(176, 176, 176));

            // Очищаем историю перед переключением
            HistoryBooksList.ItemsSource = null;
            CurrentBooksList.Visibility = Visibility.Visible;
            HistoryBooksList.Visibility = Visibility.Collapsed;

            // Загружаем текущие книги для текущего читателя
            if (currentReaderId > 0)
            {
                LoadCurrentBooks(currentReaderId);
            }
        }

        private void SwitchToHistory(object sender, RoutedEventArgs e)
        {
            isHistoryView = true;

            HistoryTab.Background = new SolidColorBrush(Color.FromRgb(108, 92, 231));
            HistoryTab.Foreground = Brushes.White;
            CurrentBooksTab.Background = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            CurrentBooksTab.Foreground = new SolidColorBrush(Color.FromRgb(176, 176, 176));

            // Очищаем текущие книги перед переключением
            CurrentBooksList.ItemsSource = null;
            HistoryBooksList.Visibility = Visibility.Visible;
            CurrentBooksList.Visibility = Visibility.Collapsed;

            // Загружаем историю для текущего читателя
            if (currentReaderId > 0)
            {
                LoadHistoryBooks(currentReaderId);
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReadersList.SelectedItem == null)
            {
                MessageBox.Show("Выберите читателя для печати", "Печать",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var printVisual = CreatePrintVisual();
                    printDialog.PrintVisual(printVisual, $"Карточка читателя - {FullNameText.Text}");

                    MessageBox.Show("Карточка отправлена на печать!", "Печать",
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
                Text = "КАРТОЧКА ЧИТАТЕЛЯ БИБЛИОТЕКИ",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = Brushes.Black
            });

            // Основная информация в таблице
            var infoGrid = new Grid();
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            infoGrid.Margin = new Thickness(0, 0, 0, 20);

            AddPrintRow(infoGrid, 0, "ФИО:", FullNameText.Text);
            AddPrintRow(infoGrid, 1, "Телефон:", PhoneText.Text);
            AddPrintRow(infoGrid, 2, "Дата рождения:", BirthDateText.Text);
            AddPrintRow(infoGrid, 3, "Дата регистрации:", RegistrationDateText.Text);
            AddPrintRow(infoGrid, 4, "Адрес:", AddressText.Text);

            printContainer.Children.Add(infoGrid);

            // Текущие книги
            if (CurrentBooksList.Items.Count > 0)
            {
                printContainer.Children.Add(new TextBlock
                {
                    Text = "ТЕКУЩИЕ КНИГИ НА РУКАХ:",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = Brushes.Black
                });

                foreach (dynamic book in CurrentBooksList.Items)
                {
                    var bookPanel = new StackPanel
                    {
                        Margin = new Thickness(0, 0, 0, 8),
                        Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0))
                    };

                    bookPanel.Children.Add(new TextBlock
                    {
                        Text = book.BookTitle,
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 12,
                        Foreground = Brushes.Black,
                        TextWrapping = TextWrapping.Wrap
                    });

                    bookPanel.Children.Add(new TextBlock
                    {
                        Text = book.InventoryNumber + " • " + book.Status,
                        FontSize = 11,
                        Foreground = Brushes.DarkGray,
                        Margin = new Thickness(0, 2, 0, 0)
                    });

                    printContainer.Children.Add(bookPanel);
                }
            }

            // Подпись и дата
            var signaturePanel = new StackPanel { Margin = new Thickness(0, 30, 0, 0) };

            signaturePanel.Children.Add(new TextBlock
            {
                Text = $"Дата печати: {DateTime.Now:dd.MM.yyyy HH:mm}",
                FontSize = 10,
                Foreground = Brushes.Gray,
                FontStyle = FontStyles.Italic
            });

            signaturePanel.Children.Add(new TextBlock
            {
                Text = "_________________________________",
                FontSize = 10,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 20, 0, 0)
            });

            signaturePanel.Children.Add(new TextBlock
            {
                Text = "Подпись библиотекаря",
                FontSize = 10,
                Foreground = Brushes.Black,
                FontStyle = FontStyles.Italic
            });

            printContainer.Children.Add(signaturePanel);

            return new ScrollViewer { Content = printContainer, Padding = new Thickness(10) };
        }

        private void AddPrintRow(Grid grid, int row, string label, string value)
        {
            var labelText = new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 0, 10, 5)
            };

            var valueText = new TextBlock
            {
                Text = value,
                FontSize = 12,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 0, 0, 5),
                TextWrapping = TextWrapping.Wrap
            };

            Grid.SetRow(labelText, row);
            Grid.SetColumn(labelText, 0);
            Grid.SetRow(valueText, row);
            Grid.SetColumn(valueText, 1);

            grid.Children.Add(labelText);
            grid.Children.Add(valueText);
        }

        private void AddReader_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addReaderWindow = new AddReaderWindow();
                addReaderWindow.Owner = this;
                if (addReaderWindow.ShowDialog() == true)
                {
                    LoadReaders();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении читателя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}