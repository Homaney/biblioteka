using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace biblioteka
{
    public partial class MainWindow : Window
    {
        private DataTable allBooksTable;

        public MainWindow()
        {
            InitializeComponent();
            InitializeSearchPlaceholder();
            LoadBooks();
            LoadStatistics();
            UpdateStats();
        }

        private void LoadBooks()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    string booksQuery = @"
                        SELECT b.ID, b.Title, b.Yearr, b.Description, u.Code AS UDKCode,
                               b.Price, b.AvailableForSale, b.Authors
                        FROM Books b 
                        LEFT JOIN UDK u ON b.UDK_ID = u.ID 
                        ORDER BY b.ID";

                    using (SqlDataAdapter adapter = new SqlDataAdapter(booksQuery, connection))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);

                        // Добавляем недостающие колонки
                        if (!table.Columns.Contains("Author"))
                            table.Columns.Add("Author", typeof(string));
                        if (!table.Columns.Contains("AuthorShort"))
                            table.Columns.Add("AuthorShort", typeof(string));
                        if (!table.Columns.Contains("AvailableInstances"))
                            table.Columns.Add("AvailableInstances", typeof(int));
                        if (!table.Columns.Contains("Identifier"))
                            table.Columns.Add("Identifier", typeof(int));

                        foreach (DataRow row in table.Rows)
                        {
                            int bookId = Convert.ToInt32(row["ID"]);
                            row["Identifier"] = bookId;

                            // Получаем полных авторов
                            string fullAuthors = row["Authors"]?.ToString() ?? "";
                            row["Author"] = fullAuthors;

                            // Сокращаем авторов до фамилии с инициалами
                            row["AuthorShort"] = ShortenAuthors(fullAuthors);

                            row["AvailableInstances"] = GetAvailableInstances(bookId, connection);
                            row["Description"] = row.IsNull("Description") ? "—" : row["Description"];
                            row["UDKCode"] = row.IsNull("UDKCode") ? "—" : row["UDKCode"];
                            row["Yearr"] = row.IsNull("Yearr") ? 0 : row["Yearr"];
                        }

                        allBooksTable = table;
                        BooksDataGrid.ItemsSource = allBooksTable.DefaultView;
                    }

                    StatusTextBlock.Text = $"Загружено книг: {allBooksTable.Rows.Count}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке книг: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Ошибка загрузки данных";
            }
        }

        // Новый метод для сокращения авторов
        private string ShortenAuthors(string authors)
        {
            if (string.IsNullOrEmpty(authors) || authors == "—")
                return "—";

            string[] authorList = authors.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> shortNames = new List<string>();

            foreach (string author in authorList)
            {
                string trimmed = author.Trim();
                string[] parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 2)
                {
                    // Берем фамилию + первую букву имени
                    string lastName = parts[0];
                    string firstNameInitial = parts[1].Length > 0 ? parts[1][0].ToString() : "";
                    shortNames.Add($"{lastName} {firstNameInitial}.");

                    // Если есть отчество - добавляем его первую букву
                    if (parts.Length >= 3)
                    {
                        string middleInitial = parts[2].Length > 0 ? parts[2][0].ToString() : "";
                        shortNames[shortNames.Count - 1] = $"{lastName} {firstNameInitial}.{middleInitial}.";
                    }
                }
                else
                {
                    shortNames.Add(trimmed); // Если не удалось распарсить, оставляем как есть
                }
            }

            return string.Join(", ", shortNames);
        }

        private void LoadStatistics()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // 1. Всего книг
                    string totalBooksQuery = "SELECT COUNT(*) FROM Books";
                    using (SqlCommand cmd = new SqlCommand(totalBooksQuery, connection))
                    {
                        int totalBooks = (int)cmd.ExecuteScalar();
                        TotalBooksStat.Text = totalBooks.ToString();
                    }

                    // 2. Выдано сейчас
                    string issuedNowQuery = "SELECT COUNT(*) FROM IssuedBooks WHERE Status = N'Выдана'";
                    using (SqlCommand cmd = new SqlCommand(issuedNowQuery, connection))
                    {
                        int issuedNow = (int)cmd.ExecuteScalar();
                        IssuedNowStat.Text = issuedNow.ToString();
                    }

                    // 3. Продано за месяц
                    string monthlySalesQuery = @"
                        SELECT ISNULL(SUM(TotalAmount), 0) 
                        FROM Sales 
                        WHERE SaleDate >= DATEADD(month, -1, GETDATE())";
                    using (SqlCommand cmd = new SqlCommand(monthlySalesQuery, connection))
                    {
                        decimal monthlySales = (decimal)cmd.ExecuteScalar();
                        MonthlySalesStat.Text = $"{monthlySales:N0} BYN";
                    }

                    // 4. Всего читателей
                    string readersCountQuery = "SELECT COUNT(*) FROM Readers";
                    using (SqlCommand cmd = new SqlCommand(readersCountQuery, connection))
                    {
                        int readersCount = (int)cmd.ExecuteScalar();
                        ReadersCountStat.Text = readersCount.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка загрузки статистики: " + ex.Message);
            }
        }

        private string GetAuthorsForBook(int bookId, SqlConnection connection)
        {
            try
            {
                string query = "SELECT Authors FROM Books WHERE ID = @BookID";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@BookID", bookId);
                    object result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "—";
                }
            }
            catch
            {
                return "—";
            }
        }

        private int GetAvailableInstances(int bookId, SqlConnection connection)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM BookInstances WHERE BookID = @BookID AND Status = N'Доступна'";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@BookID", bookId);
                    return (int)cmd.ExecuteScalar();
                }
            }
            catch
            {
                return 0;
            }
        }

        private void UpdateStats()
        {
            if (allBooksTable == null) return;

            int totalBooks = allBooksTable.Rows.Count;
            int availableBooks = 0;

            foreach (DataRow row in allBooksTable.Rows)
            {
                availableBooks += Convert.ToInt32(row["AvailableInstances"]);
            }

            StatsTextBlock.Text = $"Всего книг: {totalBooks} • Доступно экземпляров: {availableBooks}";
        }

        private void InitializeSearchPlaceholder()
        {
            SearchBox.Text = "";
            SearchBox.Foreground = new SolidColorBrush(Colors.White);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (allBooksTable == null) return;

            string filterText = SearchBox.Text.Trim();
            UpdatePlaceholderVisibility();

            if (string.IsNullOrWhiteSpace(filterText))
            {
                BooksDataGrid.ItemsSource = allBooksTable.DefaultView;
                UpdateStats();
                StatusTextBlock.Text = $"Загружено книг: {allBooksTable.Rows.Count}";
                return;
            }

            try
            {
                string escapedText = filterText.Replace("'", "''");
                string filter = $@"Title LIKE '%{escapedText}%' OR 
                                 Author LIKE '%{escapedText}%' OR 
                                 AuthorShort LIKE '%{escapedText}%' OR 
                                 UDKCode LIKE '%{escapedText}%' OR 
                                 Description LIKE '%{escapedText}%'";

                DataView view = new DataView(allBooksTable) { RowFilter = filter };
                BooksDataGrid.ItemsSource = view;
                StatusTextBlock.Text = $"Найдено книг: {view.Count}";
            }
            catch
            {
                // Игнорируем ошибки фильтрации
            }
        }

        private void UpdatePlaceholderVisibility()
        {
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        private void BooksDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(BooksDataGrid.SelectedItem is DataRowView row)) return;

            if (!int.TryParse(row["Identifier"]?.ToString(), out int identifier) || identifier <= 0)
            {
                MessageBox.Show("Некорректный идентификатор книги.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var editWindow = new EditBookWindow(identifier);
                editWindow.Owner = this;
                editWindow.BookUpdated += (s, args) =>
                {
                    LoadBooks();
                    UpdateStats();
                    LoadStatistics();
                };
                editWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия редактора: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddBook_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddBookWindow();
                addWindow.Owner = this;
                addWindow.BookAdded += (s, args) =>
                {
                    LoadBooks();
                    UpdateStats();
                    LoadStatistics();
                };
                addWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия окна добавления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteBook_Click(object sender, RoutedEventArgs e)
        {
            if (!(BooksDataGrid.SelectedItem is DataRowView row))
            {
                MessageBox.Show("Выберите книгу для удаления.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(row["Identifier"]?.ToString(), out int identifier) || identifier <= 0)
            {
                MessageBox.Show("Некорректный идентификатор книги.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string bookTitle = row["Title"]?.ToString() ?? "без названия";
            var confirm = MessageBox.Show(
                $"Вы уверены, что хотите удалить книгу?\n\nID: {identifier}\nНазвание: {bookTitle}\n\nВсе экземпляры книги также будут удалены!",
                "⚠️ Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Удаляем экземпляры
                            using (var cmd = new SqlCommand("DELETE FROM BookInstances WHERE BookID = @BookID",
                                connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@BookID", identifier);
                                cmd.ExecuteNonQuery();
                            }

                            // Удаляем книгу
                            using (var cmd = new SqlCommand("DELETE FROM Books WHERE ID = @ID",
                                connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ID", identifier);
                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            MessageBox.Show("Книга успешно удалена.", "Готово",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                            LoadStatistics();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadBooks();
                UpdateStats();
            }
        }

        private void IssueBook_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var issueWindow = new IssueBookWindow();
                issueWindow.Owner = this;
                if (issueWindow.ShowDialog() == true)
                {
                    LoadBooks();
                    UpdateStats();
                    LoadStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия окна выдачи: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SellBook_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sellWindow = new SellBookWindow();
                sellWindow.Owner = this;
                if (sellWindow.ShowDialog() == true)
                {
                    LoadBooks();
                    UpdateStats();
                    LoadStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия окна продажи: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewReaders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new ReadersInfoWindow().ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия списка читателей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewUDK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new UDKWindow().ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия справочника УДК: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OverdueLoans_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var overdueWindow = new OverdueLoansWindow();
                overdueWindow.Owner = this;
                overdueWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия окна должников: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SalesReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var reportWindow = new SalesReportWindow();
                reportWindow.Owner = this;
                reportWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия отчета по продажам: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}