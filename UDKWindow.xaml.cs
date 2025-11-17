using System;
using System.Data;
using System.Data.OleDb;
using System.Windows;
using System.Windows.Controls;

namespace biblioteka
{
    public partial class UDKWindow : Window
    {
        private OleDbConnection connection;
        private DataTable udkTable;

        public UDKWindow(OleDbConnection conn)
        {
            InitializeComponent();
            connection = conn;
            LoadUDK();
        }

        private void LoadUDK()
        {
            try
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                using (OleDbDataAdapter adapter = new OleDbDataAdapter("SELECT ID, Code, Description FROM UDK ORDER BY ID", connection))
                {
                    udkTable = new DataTable();
                    adapter.Fill(udkTable);
                    UDKDataGrid.ItemsSource = udkTable.DefaultView;
                    StatusText.Text = $"Загружено записей: {udkTable.Rows.Count}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Ошибка загрузки данных";
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        private void AddUDK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addUDK = new AddUDKWindow(connection);
                addUDK.Owner = this;
                if (addUDK.ShowDialog() == true)
                {
                    LoadUDK(); // Перезагружаем данные
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка открытия окна добавления: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditUDK_Click(object sender, RoutedEventArgs e)
        {
            if (UDKDataGrid.SelectedItem is DataRowView row)
            {
                try
                {
                    int id = Convert.ToInt32(row["ID"]);
                    var editUDK = new EditUDKWindow(connection, id);
                    editUDK.Owner = this;
                    if (editUDK.ShowDialog() == true)
                    {
                        LoadUDK(); // Перезагружаем данные
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка открытия окна редактирования: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите УДК для редактирования!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteUDK_Click(object sender, RoutedEventArgs e)
        {
            if (UDKDataGrid.SelectedItem is DataRowView row)
            {
                int id = Convert.ToInt32(row["ID"]);
                string code = row["Code"]?.ToString() ?? "";
                string description = row["Description"]?.ToString() ?? "";

                var confirm = MessageBox.Show(
                    $"Вы уверены, что хотите удалить УДК?\n\nКод: {code}\nОписание: {description}",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Создаем новое подключение для операции удаления
                        string connectionString = connection.ConnectionString;
                        using (var deleteConnection = new OleDbConnection(connectionString))
                        {
                            deleteConnection.Open();

                            // Проверяем, используется ли УДК в книгах
                            using (var checkCmd = new OleDbCommand("SELECT COUNT(*) FROM Books WHERE UDK_ID = ?", deleteConnection))
                            {
                                checkCmd.Parameters.AddWithValue("?", id);
                                int usageCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                                if (usageCount > 0)
                                {
                                    MessageBox.Show(
                                        $"Невозможно удалить УДК!\n\nЭтот УДК используется в {usageCount} книгах.\nСначала удалите или измените связанные книги.",
                                        "Ошибка",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                                    return;
                                }
                            }

                            using (OleDbCommand cmd = new OleDbCommand("DELETE FROM UDK WHERE ID = ?", deleteConnection))
                            {
                                cmd.Parameters.AddWithValue("?", id);
                                int affected = cmd.ExecuteNonQuery();

                                if (affected > 0)
                                {
                                    MessageBox.Show("УДК успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                                    // Обновляем DataTable и DataGrid
                                    if (udkTable != null)
                                    {
                                        // Находим и удаляем строку из DataTable
                                        DataRow[] rowsToDelete = udkTable.Select($"ID = {id}");
                                        foreach (DataRow rowToDelete in rowsToDelete)
                                        {
                                            udkTable.Rows.Remove(rowToDelete);
                                        }

                                        // Обновляем ItemsSource
                                        UDKDataGrid.ItemsSource = udkTable.DefaultView;
                                        StatusText.Text = $"Загружено записей: {udkTable.Rows.Count}";
                                    }
                                    else
                                    {
                                        // Если таблица не загружена, перезагружаем полностью
                                        LoadUDK();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите УДК для удаления!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Закрываем подключение при закрытии окна
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (connection.State == ConnectionState.Open)
                connection.Close();
        }

        // Метод для принудительного обновления данных
        private void RefreshUDK_Click(object sender, RoutedEventArgs e)
        {
            LoadUDK();
        }
    }
}