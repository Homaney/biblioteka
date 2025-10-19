using System;
using System.Data;
using System.Data.OleDb;
using System.IO;
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
                string query = "SELECT Id, Title, Author, Yearr, Genre, Description FROM Books ORDER BY Id";
                OleDbDataAdapter adapter = new OleDbDataAdapter(query, connection);
                DataTable table = new DataTable();
                adapter.Fill(table);

                BooksDataGrid.ItemsSource = table.DefaultView;

                if (BooksDataGrid.Columns.Count > 0)
                {
                    BooksDataGrid.Columns[0].Header = "ID";
                    BooksDataGrid.Columns[1].Header = "Название книги";
                    BooksDataGrid.Columns[2].Header = "Автор";
                    BooksDataGrid.Columns[3].Header = "Год издания";
                    BooksDataGrid.Columns[4].Header = "Жанр";
                    BooksDataGrid.Columns[5].Header = "Описание";
                }
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
            AddBookWindow addWindow = new AddBookWindow();
            if (addWindow.ShowDialog() == true)
            {
                try
                {
                    connection.Open();
                    string query = "INSERT INTO Books ([Title], [Author], [Yearr], [Genre], [Description]) VALUES (?, ?, ?, ?, ?)";
                    OleDbCommand cmd = new OleDbCommand(query, connection);
                    cmd.Parameters.AddWithValue("?", addWindow.NewBook.Title);
                    cmd.Parameters.AddWithValue("?", addWindow.NewBook.Author);
                    cmd.Parameters.AddWithValue("?", addWindow.NewBook.Year);
                    cmd.Parameters.AddWithValue("?", addWindow.NewBook.Genre);
                    cmd.Parameters.AddWithValue("?", addWindow.NewBook.Description);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Книга успешно добавлена!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при добавлении книги: " + ex.Message);
                }
                finally
                {
                    connection.Close();
                    LoadBooks();
                }
            }
        }

        private void DeleteBook_Click(object sender, RoutedEventArgs e)
        {
            if (BooksDataGrid.SelectedItem is DataRowView row)
            {
                int id = Convert.ToInt32(row["Id"]);
                try
                {
                    connection.Open();
                    OleDbCommand cmd = new OleDbCommand("DELETE FROM Books WHERE [Id] = ?", connection);
                    cmd.Parameters.AddWithValue("?", id);
                    cmd.ExecuteNonQuery();
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
    }
}
