using System;
using System.Data.SqlClient;
using System.Windows;

namespace biblioteka
{
    public partial class EditUDKWindow : Window
    {
        private readonly int udkId;

        public EditUDKWindow(int id)
        {
            InitializeComponent();
            udkId = id;
            Loaded += (s, e) => LoadUDKData();
        }

        private void LoadUDKData()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT Code, Description FROM UDK WHERE ID = @ID";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@ID", udkId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                CodeTextBox.Text = reader["Code"].ToString();
                                DescriptionTextBox.Text = reader["Description"].ToString();
                            }
                            else
                            {
                                MessageBox.Show("УДК не найден!", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
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
                    string checkQuery = "SELECT COUNT(*) FROM UDK WHERE Code = @Code AND ID <> @ID";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@Code", CodeTextBox.Text.Trim());
                        checkCmd.Parameters.AddWithValue("@ID", udkId);
                        int exists = (int)checkCmd.ExecuteScalar();
                        if (exists > 0)
                        {
                            MessageBox.Show("Такой код УДК уже существует!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    string query = "UPDATE UDK SET Code = @Code, Description = @Description WHERE ID = @ID";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Code", CodeTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@Description", DescriptionTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@ID", udkId);
                        cmd.ExecuteNonQuery();
                    }

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Ошибка",
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