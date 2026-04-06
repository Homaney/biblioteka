using System;
using System.Data.SqlClient;
using System.Windows;

namespace biblioteka
{
    public partial class AddUDKWindow : Window
    {
        public AddUDKWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CodeTextBox.Text))
            {
                MessageBox.Show("Введите код УДК!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                CodeTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                MessageBox.Show("Введите описание УДК!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                DescriptionTextBox.Focus();
                return;
            }

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // Проверка уникальности
                    string checkQuery = "SELECT COUNT(*) FROM UDK WHERE Code = @Code";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@Code", CodeTextBox.Text.Trim());
                        int exists = (int)checkCmd.ExecuteScalar();
                        if (exists > 0)
                        {
                            MessageBox.Show("Такой код УДК уже существует!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    string query = "INSERT INTO UDK (Code, Description) VALUES (@Code, @Description)";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Code", CodeTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@Description", DescriptionTextBox.Text.Trim());
                        cmd.ExecuteNonQuery();
                    }

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}