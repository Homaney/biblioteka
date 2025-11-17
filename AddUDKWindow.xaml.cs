using System;
using System.Data.OleDb;
using System.Windows;

namespace biblioteka
{
    public partial class AddUDKWindow : Window
    {
        private OleDbConnection connection;

        public AddUDKWindow(OleDbConnection conn)
        {
            InitializeComponent();
            connection = conn;
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
                connection.Open();

                // Проверяем, существует ли уже такой код УДК
                using (var checkCmd = new OleDbCommand("SELECT COUNT(*) FROM UDK WHERE Code = ?", connection))
                {
                    checkCmd.Parameters.AddWithValue("@p1", CodeTextBox.Text.Trim());
                    int existingCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (existingCount > 0)
                    {
                        MessageBox.Show("УДК с таким кодом уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                using (var cmd = new OleDbCommand("INSERT INTO UDK (Code, Description) VALUES (?, ?)", connection))
                {
                    cmd.Parameters.AddWithValue("@p1", CodeTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@p2", DescriptionTextBox.Text.Trim());

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("УДК успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении УДК: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                connection.Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}