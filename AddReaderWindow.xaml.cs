using System;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

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

        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string pattern = @"^\+375\d{9}$";
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

            if (string.IsNullOrWhiteSpace(fullName))
            {
                MessageBox.Show("Введите ФИО читателя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Regex.IsMatch(phone, @"^\+375\d{9}$"))
            {
                MessageBox.Show("Введите номер в формате +375XXXXXXXXX!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                connection.Open();

                using (var checkCmd = new OleDbCommand("SELECT COUNT(*) FROM Readers WHERE FullName = ? AND Phone = ?", connection))
                {
                    checkCmd.Parameters.Add("?", OleDbType.VarChar).Value = fullName;
                    checkCmd.Parameters.Add("?", OleDbType.VarChar).Value = phone;
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        MessageBox.Show("Такой читатель уже существует!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }

                using (var insertCmd = new OleDbCommand("INSERT INTO Readers (FullName, Phone) VALUES (?, ?)", connection))
                {
                    insertCmd.Parameters.Add("?", OleDbType.VarChar).Value = fullName;
                    insertCmd.Parameters.Add("?", OleDbType.VarChar).Value = phone;
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
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }
    }
}