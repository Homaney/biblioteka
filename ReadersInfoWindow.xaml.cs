using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace biblioteka
{
    public partial class ReadersInfoWindow : Window
    {
        private OleDbConnection connection;
        private int currentReaderId;

        public ReadersInfoWindow(OleDbConnection conn)
        {
            InitializeComponent();
            connection = conn;
            LoadReaders();
        }

        private void LoadReaders()
        {
            try
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                using (OleDbDataAdapter adapter = new OleDbDataAdapter(
                    "SELECT ID, FullName, Phone, Address, RegistrationDate, BirthDate FROM Readers ORDER BY FullName", connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    ReadersList.ItemsSource = table.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке читателей: " + ex.Message);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
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

                // Даты
                BirthDateText.Text = GetDateString(readerRow["BirthDate"]);
                RegistrationDateText.Text = GetDateString(readerRow["RegistrationDate"]);

                // Загружаем текущие книги
                LoadCurrentBooks(currentReaderId);

                // Показываем карточку с анимацией
                ReaderCard.Visibility = Visibility.Visible;
                DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                ReaderCard.BeginAnimation(OpacityProperty, fadeIn);
                SelectHint.Visibility = Visibility.Collapsed;

                // Показываем кнопку печати
                PrintButton.Visibility = Visibility.Visible;

                // Активируем вкладку текущих книг
                SwitchToCurrentBooks(null, null);
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
                string connectionString = connection.ConnectionString;
                using (var booksConnection = new OleDbConnection(connectionString))
                {
                    booksConnection.Open();

                    string query = @"
                        SELECT ib.ID AS IssuedId, b.Title AS BookTitle, bi.InventoryNumber, 
                               ib.IssueDate, ib.PlannedReturnDate, ib.ActualReturnDate, ib.Status
                        FROM ((IssuedBooks ib INNER JOIN BookInstances bi ON ib.InstanceID = bi.ID)
                        INNER JOIN Books b ON bi.BookID = b.Identifier)
                        WHERE ib.ReaderID = ? AND ib.Status = 'Выдана'";

                    using (OleDbCommand cmd = new OleDbCommand(query, booksConnection))
                    {
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = readerId;
                        using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                        {
                            DataTable table = new DataTable();
                            adapter.Fill(table);
                            var books = new List<dynamic>();
                            foreach (DataRow row in table.Rows)
                            {
                                DateTime issue = Convert.ToDateTime(row["IssueDate"]);
                                DateTime planned = Convert.ToDateTime(row["PlannedReturnDate"]);
                                bool overdue = planned < DateTime.Now && row["ActualReturnDate"] == DBNull.Value;
                                bool warning = (planned - DateTime.Now).TotalDays <= 3 && (planned - DateTime.Now).TotalDays > 0 && row["ActualReturnDate"] == DBNull.Value;

                                string statusText = overdue ? "ПРОСРОЧЕНА" : warning ? "СКОРО ВОЗВРАТ" : "В СРОК";
                                SolidColorBrush statusColor = overdue ? new SolidColorBrush(Colors.Red) :
                                                    warning ? new SolidColorBrush(Colors.Yellow) :
                                                    new SolidColorBrush(Colors.Green);

                                SolidColorBrush cardColor = overdue ? new SolidColorBrush(Color.FromArgb(30, 255, 118, 117)) :
                                                         warning ? new SolidColorBrush(Color.FromArgb(30, 253, 203, 110)) :
                                                         new SolidColorBrush(Color.FromArgb(30, 0, 184, 148));

                                books.Add(new
                                {
                                    IssuedId = row["IssuedId"],
                                    BookTitle = row["BookTitle"].ToString(),
                                    InventoryNumber = "Инв. №: " + row["InventoryNumber"].ToString(),
                                    Status = $"Выдано: {issue:dd.MM.yyyy} • Возврат: {planned:dd.MM.yyyy} • {statusText}",
                                    StatusColor = statusColor,
                                    CardColor = cardColor
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
                string connectionString = connection.ConnectionString;
                using (var booksConnection = new OleDbConnection(connectionString))
                {
                    booksConnection.Open();

                    string query = @"
                        SELECT ib.ID AS IssuedId, b.Title AS BookTitle, bi.InventoryNumber, 
                               ib.IssueDate, ib.PlannedReturnDate, ib.ActualReturnDate, ib.Status
                        FROM ((IssuedBooks ib INNER JOIN BookInstances bi ON ib.InstanceID = bi.ID)
                        INNER JOIN Books b ON bi.BookID = b.Identifier)
                        WHERE ib.ReaderID = ? AND ib.Status = 'Возвращена'
                        ORDER BY ib.ActualReturnDate DESC";

                    using (OleDbCommand cmd = new OleDbCommand(query, booksConnection))
                    {
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = readerId;
                        using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
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
                    string connectionString = connection.ConnectionString;
                    using (var returnConnection = new OleDbConnection(connectionString))
                    {
                        returnConnection.Open();

                        int instanceId;
                        DateTime plannedReturnDate;
                        using (OleDbCommand getInfoCmd = new OleDbCommand(
                            "SELECT InstanceID, PlannedReturnDate FROM IssuedBooks WHERE ID = ?", returnConnection))
                        {
                            getInfoCmd.Parameters.Add("?", OleDbType.Integer).Value = issuedId;
                            using (OleDbDataReader reader = getInfoCmd.ExecuteReader())
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
                        using (OleDbCommand updateIssued = new OleDbCommand(
                            "UPDATE IssuedBooks SET ActualReturnDate = ?, Status = 'Возвращена', ReturnedOnTime = ? WHERE ID = ?", returnConnection))
                        {
                            updateIssued.Parameters.Add("?", OleDbType.Date).Value = actualReturnDate;
                            updateIssued.Parameters.Add("?", OleDbType.Boolean).Value = returnedOnTime;
                            updateIssued.Parameters.Add("?", OleDbType.Integer).Value = issuedId;
                            updateIssued.ExecuteNonQuery();
                        }

                        // Обновляем BookInstances
                        using (OleDbCommand updateInstance = new OleDbCommand(
                            "UPDATE BookInstances SET Status = 'На полке' WHERE ID = ?", returnConnection))
                        {
                            updateInstance.Parameters.Add("?", OleDbType.Integer).Value = instanceId;
                            updateInstance.ExecuteNonQuery();
                        }

                        string message = returnedOnTime ?
                            (daysDifference < 0 ? $"✅ Книга возвращена досрочно! (на {-daysDifference} дней раньше)" : "✅ Книга возвращена вовремя!") :
                            $"⚠️ Книга возвращена с опозданием на {daysDifference} дней!";

                        MessageBox.Show(message, "Возврат книги", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Перезагружаем списки книг
                        if (CurrentBooksList.Visibility == Visibility.Visible)
                            LoadCurrentBooks(currentReaderId);
                        else
                            LoadHistoryBooks(currentReaderId);
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
            CurrentBooksTab.Background = new SolidColorBrush(Color.FromRgb(108, 92, 231));
            CurrentBooksTab.Foreground = Brushes.White;
            HistoryTab.Background = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            HistoryTab.Foreground = new SolidColorBrush(Color.FromRgb(176, 176, 176));

            CurrentBooksList.Visibility = Visibility.Visible;
            HistoryBooksList.Visibility = Visibility.Collapsed;
            LoadCurrentBooks(currentReaderId);
        }

        private void SwitchToHistory(object sender, RoutedEventArgs e)
        {
            HistoryTab.Background = new SolidColorBrush(Color.FromRgb(108, 92, 231));
            HistoryTab.Foreground = Brushes.White;
            CurrentBooksTab.Background = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            CurrentBooksTab.Foreground = new SolidColorBrush(Color.FromRgb(176, 176, 176));

            HistoryBooksList.Visibility = Visibility.Visible;
            CurrentBooksList.Visibility = Visibility.Collapsed;
            LoadHistoryBooks(currentReaderId);
        }

        // НОВЫЙ МЕТОД ДЛЯ ПЕЧАТИ
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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (connection.State == ConnectionState.Open)
                connection.Close();
        }
    }
}