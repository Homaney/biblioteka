using System;
using System.Data;
using System.Data.OleDb;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace biblioteka
{
    public partial class IssueBookWindow : Window
    {
        private OleDbConnection connection;

        public IssueBookWindow(OleDbConnection conn)
        {
            InitializeComponent();
            connection = conn;

            LoadBooks();
            LoadReaders();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            // Устанавливаем даты после полной загрузки окна
            IssueDatePicker.SelectedDate = DateTime.Today;
            PlannedReturnDatePicker.SelectedDate = DateTime.Today.AddDays(14);

            // Принудительно применяем стили к DatePicker
            ApplyDarkStyleToDatePicker(IssueDatePicker);
            ApplyDarkStyleToDatePicker(PlannedReturnDatePicker);
        }

        private void ApplyDarkStyleToDatePicker(DatePicker datePicker)
        {
            // Ждем пока DatePicker полностью загрузится
            datePicker.Loaded += (s, e) =>
            {
                if (datePicker.Template.FindName("PART_TextBox", datePicker) is DatePickerTextBox textBox)
                {
                    textBox.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
                    textBox.Foreground = Brushes.White;
                    textBox.BorderThickness = new Thickness(0);

                    // Также стилизуем внутренний TextBox
                    if (textBox.Template.FindName("PART_ContentHost", textBox) is ScrollViewer scrollViewer)
                    {
                        scrollViewer.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
                    }
                }
            };
        }

        // Остальные методы остаются без изменений...
        private void LoadBooks()
        {
            try
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                using (OleDbDataAdapter adapter = new OleDbDataAdapter("SELECT Identifier, Title FROM Books ORDER BY Title", connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    BookComboBox.DisplayMemberPath = "Title";
                    BookComboBox.SelectedValuePath = "Identifier";
                    BookComboBox.ItemsSource = table.DefaultView;
                    if (BookComboBox.Items.Count > 0)
                        BookComboBox.SelectedIndex = 0;
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

                using (OleDbDataAdapter adapter = new OleDbDataAdapter("SELECT ID, FullName FROM Readers ORDER BY FullName", connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    ReaderComboBox.DisplayMemberPath = "FullName";
                    ReaderComboBox.SelectedValuePath = "ID";
                    ReaderComboBox.ItemsSource = table.DefaultView;
                    if (ReaderComboBox.Items.Count > 0)
                        ReaderComboBox.SelectedIndex = 0;
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
            if (BookComboBox.SelectedValue == null) return;
            int bookId = Convert.ToInt32(BookComboBox.SelectedValue);
            LoadAvailableInstances(bookId);
        }

        private void LoadAvailableInstances(int bookId)
        {
            try
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                using (OleDbDataAdapter adapter = new OleDbDataAdapter("SELECT ID, InventoryNumber FROM BookInstances WHERE BookID = ? AND Status = 'На полке' ORDER BY InventoryNumber", connection))
                {
                    adapter.SelectCommand.Parameters.Add("?", OleDbType.Integer).Value = bookId;
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    InstanceListBox.DisplayMemberPath = "InventoryNumber";
                    InstanceListBox.SelectedValuePath = "ID";
                    InstanceListBox.ItemsSource = table.DefaultView;
                    if (InstanceListBox.Items.Count > 0)
                        InstanceListBox.SelectedIndex = 0;
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
                    if (ReaderComboBox.Items.Count > 0)
                        ReaderComboBox.SelectedIndex = ReaderComboBox.Items.Count - 1;
                }
            }
        }

        private void IssueButton_Click(object sender, RoutedEventArgs e)
        {
            if (BookComboBox.SelectedValue == null || InstanceListBox.SelectedValue == null || ReaderComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите книгу, экземпляр и читателя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int instanceId = Convert.ToInt32(InstanceListBox.SelectedValue);
            int readerId = Convert.ToInt32(ReaderComboBox.SelectedValue);
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
                    checkCmd.Parameters.Add("?", OleDbType.Integer).Value = instanceId;
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
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = instanceId;
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = readerId;
                    cmd.Parameters.Add("?", OleDbType.Date).Value = issueDate;
                    cmd.Parameters.Add("?", OleDbType.Date).Value = plannedReturnDate;
                    cmd.ExecuteNonQuery();
                }

                using (OleDbCommand updateInstance = new OleDbCommand("UPDATE BookInstances SET Status = 'Выдана' WHERE ID = ?", connection))
                {
                    updateInstance.Parameters.Add("?", OleDbType.Integer).Value = instanceId;
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