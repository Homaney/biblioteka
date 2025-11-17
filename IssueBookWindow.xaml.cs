using System;
using System.Data;
using System.Data.OleDb;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace biblioteka
{
    public partial class IssueBookWindow : Window
    {
        private OleDbConnection connection;

        // Простые классы для данных
        public class BookItem
        {
            public int Identifier { get; set; }
            public string Title { get; set; }

            // Переопределяем ToString для правильного отображения
            public override string ToString()
            {
                return Title;
            }
        }

        public class ReaderItem
        {
            public int ID { get; set; }
            public string FullName { get; set; }

            public override string ToString()
            {
                return FullName;
            }
        }

        public class InstanceItem
        {
            public int ID { get; set; }
            public string InventoryNumber { get; set; }

            public override string ToString()
            {
                return InventoryNumber;
            }
        }

        public IssueBookWindow(OleDbConnection conn)
        {
            InitializeComponent();
            connection = conn;

            // Изначально устанавливаем SelectedItem в null для показа плейсхолдеров
            BookComboBox.SelectedItem = null;
            ReaderComboBox.SelectedItem = null;

            LoadBooks();
            LoadReaders();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            IssueDatePicker.SelectedDate = DateTime.Today;
            PlannedReturnDatePicker.SelectedDate = DateTime.Today.AddDays(14);
        }

        private void LoadBooks()
        {
            try
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                string query = "SELECT Identifier, Title FROM Books ORDER BY Title";
                using (OleDbCommand cmd = new OleDbCommand(query, connection))
                using (OleDbDataReader reader = cmd.ExecuteReader())
                {
                    var books = new ObservableCollection<BookItem>();
                    while (reader.Read())
                    {
                        books.Add(new BookItem
                        {
                            Identifier = reader.GetInt32(0),
                            Title = reader.GetString(1)
                        });
                    }
                    BookComboBox.ItemsSource = books;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке книг: " + ex.Message);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        private void LoadReaders()
        {
            try
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                string query = "SELECT ID, FullName FROM Readers ORDER BY FullName";
                using (OleDbCommand cmd = new OleDbCommand(query, connection))
                using (OleDbDataReader reader = cmd.ExecuteReader())
                {
                    var readers = new ObservableCollection<ReaderItem>();
                    while (reader.Read())
                    {
                        readers.Add(new ReaderItem
                        {
                            ID = reader.GetInt32(0),
                            FullName = reader.GetString(1)
                        });
                    }
                    ReaderComboBox.ItemsSource = readers;
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

        private void BookComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BookComboBox.SelectedItem is BookItem selectedBook)
            {
                LoadAvailableInstances(selectedBook.Identifier);
            }
            else
            {
                InstanceListBox.ItemsSource = null;
            }
        }

        private void LoadAvailableInstances(int bookId)
        {
            try
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                string query = "SELECT ID, InventoryNumber FROM BookInstances WHERE BookID = ? AND Status = 'На полке' ORDER BY InventoryNumber";
                using (OleDbCommand cmd = new OleDbCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("?", bookId);
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        var instances = new ObservableCollection<InstanceItem>();
                        while (reader.Read())
                        {
                            instances.Add(new InstanceItem
                            {
                                ID = reader.GetInt32(0),
                                InventoryNumber = reader.GetString(1)
                            });
                        }
                        InstanceListBox.ItemsSource = instances;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке экземпляров: " + ex.Message);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        private void AddReader_Click(object sender, RoutedEventArgs e)
        {
            string connectionString = connection.ConnectionString;
            using (var newConnection = new OleDbConnection(connectionString))
            {
                var addReaderWindow = new AddReaderWindow(newConnection);
                if (addReaderWindow.ShowDialog() == true)
                {
                    LoadReaders();
                    ReaderComboBox.SelectedItem = null;
                }
            }
        }

        private void IssueButton_Click(object sender, RoutedEventArgs e)
        {
            if (BookComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите книгу!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ReaderComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите читателя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (InstanceListBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите экземпляр книги!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var instanceItem = (InstanceItem)InstanceListBox.SelectedItem;
            var readerItem = (ReaderItem)ReaderComboBox.SelectedItem;

            int instanceId = instanceItem.ID;
            int readerId = readerItem.ID;
            DateTime issueDate = IssueDatePicker.SelectedDate ?? DateTime.Today;
            DateTime plannedReturnDate = PlannedReturnDatePicker.SelectedDate ?? DateTime.Today.AddDays(14);

            if (plannedReturnDate < issueDate)
            {
                MessageBox.Show("Дата возврата должна быть после даты выдачи!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                using (OleDbCommand checkCmd = new OleDbCommand("SELECT Status FROM BookInstances WHERE ID = ?", connection))
                {
                    checkCmd.Parameters.AddWithValue("?", instanceId);
                    string status = checkCmd.ExecuteScalar()?.ToString();
                    if (status != "На полке")
                    {
                        MessageBox.Show("Этот экземпляр недоступен!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                string query = "INSERT INTO IssuedBooks (InstanceID, ReaderID, IssueDate, PlannedReturnDate, Status) VALUES (?, ?, ?, ?, 'Выдана')";
                using (OleDbCommand cmd = new OleDbCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("?", instanceId);
                    cmd.Parameters.AddWithValue("?", readerId);
                    cmd.Parameters.AddWithValue("?", issueDate);
                    cmd.Parameters.AddWithValue("?", plannedReturnDate);
                    cmd.ExecuteNonQuery();
                }

                using (OleDbCommand updateInstance = new OleDbCommand("UPDATE BookInstances SET Status = 'Выдана' WHERE ID = ?", connection))
                {
                    updateInstance.Parameters.AddWithValue("?", instanceId);
                    updateInstance.ExecuteNonQuery();
                }

                MessageBox.Show("Книга успешно выдана!");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выдаче книги: " + ex.Message);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }
    }
}