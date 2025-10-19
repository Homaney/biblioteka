using System;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Windows;

namespace biblioteka
{
    public partial class IssueBookWindow : Window
    {
        private OleDbConnection connection;

        public IssueBookWindow()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            LoadBooks();
            LoadReaders();
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
                OleDbDataAdapter adapter = new OleDbDataAdapter("SELECT Id, Title FROM Books ORDER BY Title", connection);
                DataTable table = new DataTable();
                adapter.Fill(table);
                BookComboBox.ItemsSource = table.DefaultView;
                BookComboBox.DisplayMemberPath = "Title";
                BookComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке книг: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private void LoadReaders()
        {
            try
            {
                connection.Open();
                OleDbDataAdapter adapter = new OleDbDataAdapter("SELECT ID, FullName FROM Readers ORDER BY FullName", connection);
                DataTable table = new DataTable();
                adapter.Fill(table);
                ReaderComboBox.ItemsSource = table.DefaultView;
                ReaderComboBox.DisplayMemberPath = "FullName";
                ReaderComboBox.SelectedValuePath = "FullName";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке читателей: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private void AddReader_Click(object sender, RoutedEventArgs e)
        {
            AddReaderWindow addReaderWindow = new AddReaderWindow();
            addReaderWindow.Owner = this;

            if (addReaderWindow.ShowDialog() == true)
            {
                LoadReaders();
                ReaderComboBox.SelectedIndex = ReaderComboBox.Items.Count - 1;
            }
        }

        private void IssueButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка выбранной книги
            if (BookComboBox.SelectedItem == null ||
                string.IsNullOrWhiteSpace(((DataRowView)BookComboBox.SelectedItem)["Title"].ToString()))
            {
                MessageBox.Show("Выберите книгу для выдачи!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка выбранного читателя
            if (ReaderComboBox.SelectedItem == null ||
                string.IsNullOrWhiteSpace(((DataRowView)ReaderComboBox.SelectedItem)["FullName"].ToString()))
            {
                MessageBox.Show("Выберите читателя для выдачи!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Всё ок, получаем данные
            var bookRow = (DataRowView)BookComboBox.SelectedItem;
            var readerRow = (DataRowView)ReaderComboBox.SelectedItem;

            int bookId = Convert.ToInt32(bookRow["Id"]);
            string readerName = readerRow["FullName"].ToString();
            DateTime issueDate = IssueDatePicker.SelectedDate ?? DateTime.Now;
            DateTime returnDate = ReturnDatePicker.SelectedDate ?? DateTime.Now;

            try
            {
                connection.Open();
                string query = "INSERT INTO IssuedBooks ([BookID], [ReaderName], [IssueDate], [ReturnDate]) VALUES (?, ?, ?, ?)";
                OleDbCommand cmd = new OleDbCommand(query, connection);
                cmd.Parameters.AddWithValue("?", bookId);
                cmd.Parameters.AddWithValue("?", readerName);
                cmd.Parameters.AddWithValue("?", issueDate);
                cmd.Parameters.AddWithValue("?", returnDate);
                cmd.ExecuteNonQuery();

                MessageBox.Show("Книга успешно выдана!");
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выдаче книги: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
