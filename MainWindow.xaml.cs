using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            LoadBooks();
        }

        private void InitializeDatabaseConnection()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "Library.accdb");
            connection = new OleDbConnection($@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;");
        }

        private void LoadBooks()
        {
            try
            {
                connection.Open();
                // 1. Загружаем книги
                string booksQuery = "SELECT Identifier, Title, Yearr, Razdel, Description, Quantity FROM Books ORDER BY Identifier";
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(booksQuery, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    // 2. Добавляем колонку Author
                    if (!table.Columns.Contains("Author"))
                        table.Columns.Add("Author", typeof(string));

                    // 3. Для каждой книги — подтягиваем авторов
                    foreach (DataRow row in table.Rows)
                    {
                        int bookId = Convert.ToInt32(row["Identifier"]);
                        string authors = GetAuthorsForBook(bookId, connection);
                        row["Author"] = authors;
                    }

                    // 4. Нормализация
                    foreach (DataRow row in table.Rows)
                    {
                        row["Description"] = row.IsNull("Description") ? "" : row["Description"];
                        row["Razdel"] = row.IsNull("Razdel") ? "" : row["Razdel"];
                        row["Yearr"] = row.IsNull("Yearr") ? 0 : row["Yearr"];
                        row["Quantity"] = row.IsNull("Quantity") ? 0 : row["Quantity"];
                    }

                    allBooksTable = table;
                    BooksDataGrid.ItemsSource = allBooksTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке базы: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        private string GetAuthorsForBook(int bookId, OleDbConnection conn)
        {
            string authors = "";
            try
            {
                string query = @"
                    SELECT a.FullName
                    FROM Authors a
                    INNER JOIN BookAuthors ba ON a.ID = ba.AuthorID
                    WHERE ba.BookID = ?
                    ORDER BY a.FullName";
                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = bookId;
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        List<string> list = new List<string>();
                        while (reader.Read())
                        {
                            list.Add(reader.GetString(0));
                        }
                        authors = string.Join(", ", list);
                    }
                }
            }
            catch
            {
                authors = "";
            }
            return authors;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (allBooksTable == null) return;

            string filterText = SearchBox.Text.Trim().Replace("'", "''");
            if (string.IsNullOrWhiteSpace(filterText))
            {
                BooksDataGrid.ItemsSource = allBooksTable.DefaultView;
                return;
            }

            string filter = $"Title LIKE '%{filterText}%' OR Author LIKE '%{filterText}%' OR Razdel LIKE '%{filterText}%' OR Description LIKE '%{filterText}%'";
            DataView view = new DataView(allBooksTable) { RowFilter = filter };
            BooksDataGrid.ItemsSource = view;
        }

        private void BooksDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(BooksDataGrid.SelectedItem is DataRowView row)) return;

            if (!int.TryParse(row["Identifier"]?.ToString(), out int identifier) || identifier <= 0)
            {
                MessageBox.Show("Некорректный идентификатор книги.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWindow = new EditBookWindow(connection, identifier);
            if (editWindow.ShowDialog() == true)
                LoadBooks();
        }

        private void AddBook_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddBookWindow(connection);
            if (addWindow.ShowDialog() == true)
                LoadBooks();
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

            var confirm = MessageBox.Show($"Удалить книгу с ID = {identifier}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                connection.Open();
                using (var cmd = new OleDbCommand("DELETE FROM Books WHERE Identifier = ?", connection))
                {
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = identifier;
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Книга удалена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
                LoadBooks();
            }
        }

        private void IssueBook_Click(object sender, RoutedEventArgs e)
        {
            var issueWindow = new IssueBookWindow();
            if (issueWindow.ShowDialog() == true)
            {
                LoadBooks();
                MessageBox.Show("Операция завершена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ViewReaders_Click(object sender, RoutedEventArgs e)
        {
            new ReadersInfoWindow().ShowDialog();
        }
    }
}
