using System;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace biblioteka
{
    public partial class AddReaderWindow : Window
    {
        private OleDbConnection connection;

        public AddReaderWindow()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
        }

        private void InitializeDatabaseConnection()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "Library.accdb");
            connection = new OleDbConnection($@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;");
        }

        private void PhoneTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string pattern = @"^\+375\d{9}$"; // строго +375XXXXXXXXX
            string input = PhoneTextBox.Text.Trim();

            if (string.IsNullOrEmpty(input))
            {
                PhoneWarningTextBlock.Text = "Введите номер в формате +375XXXXXXXXX";
                PhoneWarningTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
            }
            else if (!Regex.IsMatch(input, pattern))
            {
                PhoneWarningTextBlock.Text = "Номер введён неверно!";
                PhoneWarningTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                PhoneWarningTextBlock.Text = "Номер корректен";
                PhoneWarningTextBlock.Foreground = System.Windows.Media.Brushes.Green;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string fullName = (FullNameTextBox.Text ?? "").Trim();
            string phone = (PhoneTextBox.Text ?? "").Trim();

            // Проверка ФИО
            if (string.IsNullOrWhiteSpace(fullName))
            {
                MessageBox.Show("Введите ФИО читателя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка номера телефона в формате +375XXXXXXXXX
            if (!Regex.IsMatch(phone, @"^\+375\d{9}$"))
            {
                MessageBox.Show("Введите номер в формате +375XXXXXXXXX!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                connection.Open();

                // Проверка на дубликат по ФИО и номеру
                using (var checkCmd = new OleDbCommand("SELECT COUNT(*) FROM Readers WHERE FullName = ? AND Phone = ?", connection))
                {
                    checkCmd.Parameters.AddWithValue("?", fullName);
                    checkCmd.Parameters.AddWithValue("?", phone);
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        MessageBox.Show("Такой читатель уже существует!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }

                // Вставка нового читателя
                using (var insertCmd = new OleDbCommand("INSERT INTO Readers (FullName, Phone) VALUES (?, ?)", connection))
                {
                    insertCmd.Parameters.AddWithValue("?", fullName);
                    insertCmd.Parameters.AddWithValue("?", phone);
                    insertCmd.ExecuteNonQuery();
                }

                MessageBox.Show("Читатель успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении читателя: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
