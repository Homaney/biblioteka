using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace biblioteka
{
    public partial class UDKWindow : Window
    {
        private DataTable udkTable;

        public UDKWindow()
        {
            InitializeComponent();
            LoadUDK();
        }

        private void LoadUDK()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT ID, Code, Description FROM UDK ORDER BY Code";
                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                    {
                        udkTable = new DataTable();
                        adapter.Fill(udkTable);
                        UDKDataGrid.ItemsSource = udkTable.DefaultView;
                        StatusText.Text = $"Загружено записей: {udkTable.Rows.Count}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddUDK_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddUDKWindow();
            addWindow.Owner = this;
            if (addWindow.ShowDialog() == true)
            {
                LoadUDK();
            }
        }

        private void EditUDK_Click(object sender, RoutedEventArgs e)
        {
            if (UDKDataGrid.SelectedItem is DataRowView row)
            {
                int id = Convert.ToInt32(row["ID"]);
                var editWindow = new EditUDKWindow(id);
                editWindow.Owner = this;
                if (editWindow.ShowDialog() == true)
                {
                    LoadUDK();
                }
            }
            else
            {
                MessageBox.Show("Выберите УДК для редактирования!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteUDK_Click(object sender, RoutedEventArgs e)
        {
            if (UDKDataGrid.SelectedItem is DataRowView row)
            {
                int id = Convert.ToInt32(row["ID"]);
                string code = row["Code"]?.ToString() ?? "";

                var confirm = MessageBox.Show(
                    $"Удалить УДК?\n\nКод: {code}",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var connection = DatabaseHelper.GetConnection())
                        {
                            connection.Open();

                            // Проверяем использование
                            string checkQuery = "SELECT COUNT(*) FROM Books WHERE UDK_ID = @ID";
                            using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
                            {
                                checkCmd.Parameters.AddWithValue("@ID", id);
                                int usageCount = (int)checkCmd.ExecuteScalar();

                                if (usageCount > 0)
                                {
                                    MessageBox.Show($"УДК используется в {usageCount} книгах!",
                                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
                            }

                            string deleteQuery = "DELETE FROM UDK WHERE ID = @ID";
                            using (SqlCommand cmd = new SqlCommand(deleteQuery, connection))
                            {
                                cmd.Parameters.AddWithValue("@ID", id);
                                cmd.ExecuteNonQuery();
                            }

                            LoadUDK();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}