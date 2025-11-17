using System;
using System.Data.OleDb;
using System.Windows;

namespace biblioteka
{
    public partial class EditUDKWindow : Window
    {
        private OleDbConnection connection;
        private readonly int udkId;

        public EditUDKWindow(OleDbConnection conn, int id)
        {
            InitializeComponent();
            connection = conn;
            udkId = id;
            Loaded += (s, e) => LoadUDKData();
        }

        private void LoadUDKData()
        {
            try
            {
                connection.Open();
                using (var cmd = new OleDbCommand("SELECT Code, Description FROM UDK WHERE ID = ?", connection))
                {
                    cmd.Parameters.AddWithValue("@p1", udkId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            CodeTextBox.Text = reader["Code"].ToString();
                            DescriptionTextBox.Text = reader["Description"].ToString();
                        }
                        else
                        {
                            MessageBox.Show("УДК не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
            finally
            {
                connection.Close();
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
                connection.Open();

                // Проверяем, существует ли уже такой код УДК (кроме текущего)
                using (var checkCmd = new OleDbCommand("SELECT COUNT(*) FROM UDK WHERE Code = ? AND ID <> ?", connection))
                {
                    checkCmd.Parameters.AddWithValue("@p1", CodeTextBox.Text.Trim());
                    checkCmd.Parameters.AddWithValue("@p2", udkId);
                    int existingCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (existingCount > 0)
                    {
                        MessageBox.Show("УДК с таким кодом уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                using (var cmd = new OleDbCommand("UPDATE UDK SET Code = ?, Description = ? WHERE ID = ?", connection))
                {
                    cmd.Parameters.AddWithValue("@p1", CodeTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@p2", DescriptionTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@p3", udkId);

                    int affected = cmd.ExecuteNonQuery();

                    if (affected > 0)
                    {
                        MessageBox.Show("УДК успешно обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении УДК: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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