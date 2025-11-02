using System;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Windows;

namespace biblioteka
{
    public partial class MainWindow : Window
    {
        private OleDbConnection connection;

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

                // 1. Загружаем все книги
                string booksQuery = "SELECT ID, Title, Yearr, Genre, Description FROM Books ORDER BY ID";
                OleDbDataAdapter booksAdapter = new OleDbDataAdapter(booksQuery, connection);
                DataTable booksTable = new DataTable();
                booksAdapter.Fill(booksTable);

                // Добавляем колонку Authors для отображения
                booksTable.Columns.Add("Authors", typeof(string));

                // 2. Загружаем связи книги-авторы
                string authorsQuery = @"
            SELECT BA.BookID, A.FullName
            FROM BookAuthors BA
            INNER JOIN Authors A ON BA.AuthorID = A.ID";
                OleDbDataAdapter authorsAdapter = new OleDbDataAdapter(authorsQuery, connection);
                DataTable authorsTable = new DataTable();
                authorsAdapter.Fill(authorsTable);

                // 3. Формируем строку авторов для каждой книги
                foreach (DataRow bookRow in booksTable.Rows)
                {
                    int bookId = Convert.ToInt32(bookRow["ID"]);
                    var authors = authorsTable.AsEnumerable()
                        .Where(r => Convert.ToInt32(r["BookID"]) == bookId)
                        .Select(r => r["FullName"].ToString())
                        .ToArray();
                    bookRow["Authors"] = string.Join(", ", authors);
                }

                // 4. Привязываем к DataGrid
                BooksDataGrid.ItemsSource = booksTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке базы: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }


        private void AddBook_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddBookWindow addWindow = new AddBookWindow(connection);
                addWindow.ShowDialog();

                LoadBooks();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии окна добавления книги: " + ex.Message);
            }
        }

        private void DeleteBook_Click(object sender, RoutedEventArgs e)
        {
            if (BooksDataGrid.SelectedItem is DataRowView row)
            {
                int id = Convert.ToInt32(row["ID"]);
                try
                {
                    connection.Open();

                    // Удаляем связи с авторами
                    OleDbCommand deleteLinks = new OleDbCommand("DELETE FROM BookAuthors WHERE BookID = ?", connection);
                    deleteLinks.Parameters.AddWithValue("?", id);
                    deleteLinks.ExecuteNonQuery();

                    // Удаляем книгу
                    OleDbCommand deleteBook = new OleDbCommand("DELETE FROM Books WHERE ID = ?", connection);
                    deleteBook.Parameters.AddWithValue("?", id);
                    deleteBook.ExecuteNonQuery();

                    MessageBox.Show("Книга успешно удалена!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении книги: " + ex.Message);
                }
                finally
                {
                    connection.Close();
                    LoadBooks();
                }
            }
            else
            {
                MessageBox.Show("Выберите книгу для удаления.");
            }
        }

        private void IssueBook_Click(object sender, RoutedEventArgs e)
        {
            IssueBookWindow issueWindow = new IssueBookWindow();
            bool? result = issueWindow.ShowDialog();

            if (result == true)
            {
                LoadBooks();
                MessageBox.Show("Операция завершена.");
            }
        }

        private void ViewReaders_Click(object sender, RoutedEventArgs e)
        {
            ReadersInfoWindow readersWindow = new ReadersInfoWindow();
            readersWindow.ShowDialog();
        }
    }
}
