using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace biblioteka
{
    public partial class MainWindow : Window
    {
        private OleDbConnection connection;
        private DataTable allBooksTable;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            InitializeSearchPlaceholder();
            LoadBooks();
            UpdateStats();
        }

        private void InitializeDatabaseConnection()
        {
            try
            {
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "Library.accdb");
                string directory = Path.GetDirectoryName(dbPath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(dbPath))
                {
                    MessageBox.Show($"База данных не найдена по пути: {dbPath}\nПожалуйста, создайте базу данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                connection = new OleDbConnection($@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;");
                StatusTextBlock.Text = "База данных подключена";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Ошибка подключения к БД";
            }
        }

        private void InitializeSearchPlaceholder()
        {
            SearchBox.Text = "";
            SearchBox.Foreground = new SolidColorBrush(Colors.White);
        }

        private void LoadBooks()
        {
            if (connection == null) return;

            try
            {
                connection.Open();
                string booksQuery = @"SELECT b.Identifier, b.Title, b.Yearr, b.Description, u.Code AS UDKCode 
                                    FROM (Books b LEFT JOIN UDK u ON b.UDK_ID = u.ID) 
                                    ORDER BY b.Identifier";

                using (OleDbDataAdapter adapter = new OleDbDataAdapter(booksQuery, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    if (!table.Columns.Contains("Author"))
                        table.Columns.Add("Author", typeof(string));
                    if (!table.Columns.Contains("AvailableInstances"))
                        table.Columns.Add("AvailableInstances", typeof(int));

                    foreach (DataRow row in table.Rows)
                    {
                        int bookId = Convert.ToInt32(row["Identifier"]);
                        string authors = GetAuthorsForBook(bookId, connection);
                        row["Author"] = authors;
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
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке книг: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Ошибка загрузки данных";
            }
            finally
            {
                if (connection?.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        private string GetAuthorsForBook(int bookId, OleDbConnection conn)
        {
            string authors = "";
            try
            {
                string query = @"SELECT a.FullName
                               FROM Authors a
                               INNER JOIN BookAuthors ba ON a.ID = ba.AuthorID
                               WHERE ba.BookID = ?
                               ORDER BY a.FullName";
                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BookID", bookId);
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        List<string> authorList = new List<string>();
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                                authorList.Add(reader.GetString(0));
                        }
                        authors = authorList.Count > 0 ? string.Join(", ", authorList) : "—";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении авторов: {ex.Message}");
                authors = "—";
            }
            return authors;
        }

        private int GetAvailableInstances(int bookId, OleDbConnection conn)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM BookInstances WHERE BookID = ? AND Status = 'На полке'";
                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BookID", bookId);
                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении экземпляров: {ex.Message}");
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

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (allBooksTable == null) return;

            string filterText = SearchBox.Text.Trim();

            // Обновляем видимость плейсхолдера
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
                                 UDKCode LIKE '%{escapedText}%' OR 
                                 Description LIKE '%{escapedText}%'";

                DataView view = new DataView(allBooksTable) { RowFilter = filter };
                BooksDataGrid.ItemsSource = view;

                StatusTextBlock.Text = $"Найдено книг: {view.Count}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка фильтрации: {ex.Message}");
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // Убираем плейсхолдер при фокусе
            UpdatePlaceholderVisibility();
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Показываем плейсхолдер если поле пустое
            UpdatePlaceholderVisibility();
        }

        private void UpdatePlaceholderVisibility()
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
            {
                SearchPlaceholder.Visibility = Visibility.Visible;
            }
            else
            {
                SearchPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private void BooksDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(BooksDataGrid.SelectedItem is DataRowView row)) return;

            if (!int.TryParse(row["Identifier"]?.ToString(), out int identifier) || identifier <= 0)
            {
                MessageBox.Show("Некорректный идентификатор книги.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var editWindow = new EditBookWindow(connection, identifier);
                editWindow.Owner = this;
                editWindow.BookUpdated += (s, args) =>
                {
                    LoadBooks();
                    UpdateStats();
                };
                editWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия редактора: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddBook_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddBookWindow(connection);
                addWindow.Owner = this;
                addWindow.BookAdded += (s, args) =>
                {
                    LoadBooks();
                    UpdateStats();
                };
                addWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия окна добавления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteBook_Click(object sender, RoutedEventArgs e)
        {
            if (!(BooksDataGrid.SelectedItem is DataRowView row))
            {
                MessageBox.Show("Выберите книгу для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(row["Identifier"]?.ToString(), out int identifier) || identifier <= 0)
            {
                MessageBox.Show("Некорректный идентификатор книги.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string bookTitle = row["Title"]?.ToString() ?? "без названия";
            var confirm = MessageBox.Show(
                $"Вы уверены, что хотите удалить книгу?\n\nID: {identifier}\nНазвание: {bookTitle}\n\nВсе экземпляры книги также будут удалены!",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Удаляем экземпляры
                        using (var cmd = new OleDbCommand("DELETE FROM BookInstances WHERE BookID = ?", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@BookID", identifier);
                            cmd.ExecuteNonQuery();
                        }

                        // Удаляем связи авторов
                        using (var cmd = new OleDbCommand("DELETE FROM BookAuthors WHERE BookID = ?", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@BookID", identifier);
                            cmd.ExecuteNonQuery();
                        }

                        // Удаляем книгу
                        using (var cmd = new OleDbCommand("DELETE FROM Books WHERE Identifier = ?", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Identifier", identifier);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        MessageBox.Show("Книга успешно удалена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (connection?.State == ConnectionState.Open)
                    connection.Close();

                LoadBooks();
                UpdateStats();
            }
        }

        private void IssueBook_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var issueWindow = new IssueBookWindow(connection);
                issueWindow.Owner = this;
                if (issueWindow.ShowDialog() == true)
                {
                    LoadBooks();
                    UpdateStats();
                    MessageBox.Show("Книга успешно выдана.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия окна выдачи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewReaders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new ReadersInfoWindow(connection) { Owner = this }.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия списка читателей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewUDK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new UDKWindow(connection) { Owner = this }.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия справочника УДК: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}