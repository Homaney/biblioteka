using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace biblioteka
{
    public partial class IssueBookWindow : Window
    {
        public class BookItem
        {
            public int ID { get; set; }
            public string Title { get; set; }
            public override string ToString() => Title;
        }

        public class ReaderItem
        {
            public int ID { get; set; }
            public string FullName { get; set; }
            public override string ToString() => FullName;
        }

        public class InstanceItem
        {
            public int ID { get; set; }
            public string InventoryNumber { get; set; }
            public override string ToString() => InventoryNumber;
        }

        public IssueBookWindow()
        {
            InitializeComponent();
            LoadBooks();
            LoadReaders();
            IssueDatePicker.SelectedDate = DateTime.Today;
            PlannedReturnDatePicker.SelectedDate = DateTime.Today.AddDays(14);

            // Запрещаем будущие даты для даты выдачи
            IssueDatePicker.DisplayDateEnd = DateTime.Today;
        }

        private void LoadBooks()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT ID, Title FROM Books ORDER BY Title";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        var books = new List<BookItem>();
                        while (reader.Read())
                        {
                            books.Add(new BookItem
                            {
                                ID = reader.GetInt32(0),
                                Title = reader.GetString(1)
                            });
                        }
                        BookComboBox.ItemsSource = books;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки книг: " + ex.Message);
            }
        }

        private void LoadReaders()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT ID, FullName FROM Readers ORDER BY FullName";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        var readers = new List<ReaderItem>();
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
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки читателей: " + ex.Message);
            }
        }

        private void BookComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BookComboBox.SelectedItem is BookItem selectedBook)
            {
                LoadAvailableInstances(selectedBook.ID);
            }
        }

        private void LoadAvailableInstances(int bookId)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT ID, InventoryNumber 
                        FROM BookInstances 
                        WHERE BookID = @BookID AND Status = N'Доступна' 
                        ORDER BY InventoryNumber";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@BookID", bookId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            var instances = new List<InstanceItem>();
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
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки экземпляров: " + ex.Message);
            }
        }

        private void AddReader_Click(object sender, RoutedEventArgs e)
        {
            var addReaderWindow = new AddReaderWindow();
            if (addReaderWindow.ShowDialog() == true)
            {
                LoadReaders();
            }
        }

        // Проверка даты выдачи
        private bool IsValidIssueDate(DateTime? date)
        {
            if (!date.HasValue)
                return false;

            DateTime selectedDate = date.Value.Date;
            return selectedDate <= DateTime.Today; // Не позже сегодня
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
                MessageBox.Show("Выберите экземпляр!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка даты выдачи
            if (!IsValidIssueDate(IssueDatePicker.SelectedDate))
            {
                MessageBox.Show("Дата выдачи не может быть в будущем!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IssueDatePicker.Focus();
                return;
            }

            var instance = (InstanceItem)InstanceListBox.SelectedItem;
            var reader = (ReaderItem)ReaderComboBox.SelectedItem;
            DateTime issueDate = IssueDatePicker.SelectedDate ?? DateTime.Today;
            DateTime plannedReturnDate = PlannedReturnDatePicker.SelectedDate ?? DateTime.Today.AddDays(14);

            if (plannedReturnDate < issueDate)
            {
                MessageBox.Show("Дата возврата должна быть позже даты выдачи!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    string query = @"
                        INSERT INTO IssuedBooks (InstanceID, ReaderID, IssueDate, PlannedReturnDate, Status)
                        VALUES (@InstanceID, @ReaderID, @IssueDate, @PlannedReturnDate, N'Выдана')";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@InstanceID", instance.ID);
                        cmd.Parameters.AddWithValue("@ReaderID", reader.ID);
                        cmd.Parameters.AddWithValue("@IssueDate", issueDate);
                        cmd.Parameters.AddWithValue("@PlannedReturnDate", plannedReturnDate);
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Книга успешно выдана!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выдаче: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}