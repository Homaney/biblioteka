using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace biblioteka
{
    public partial class OverdueLoansWindow : Window
    {
        private DataTable _overdueTable;

        public OverdueLoansWindow()
        {
            InitializeComponent();
            LoadOverdueLoans();
        }

        private void LoadOverdueLoans()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            ib.ID AS IssuedId,
                            r.FullName AS ReaderName,
                            b.Title AS BookTitle,
                            bi.InventoryNumber,
                            ib.IssueDate,
                            ib.PlannedReturnDate,
                            DATEDIFF(day, ib.PlannedReturnDate, GETDATE()) AS DaysOverdue
                        FROM IssuedBooks ib
                        JOIN BookInstances bi ON ib.InstanceID = bi.ID
                        JOIN Books b ON bi.BookID = b.ID
                        JOIN Readers r ON ib.ReaderID = r.ID
                        WHERE ib.Status = N'Выдана' 
                          AND ib.PlannedReturnDate < GETDATE()
                        ORDER BY DaysOverdue DESC";

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                    {
                        _overdueTable = new DataTable();
                        adapter.Fill(_overdueTable);
                        OverdueDataGrid.ItemsSource = _overdueTable.DefaultView;
                    }
                }
                UpdateStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки просроченных выдач: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStats()
        {
            if (_overdueTable != null)
            {
                int count = _overdueTable.Rows.Count;
                if (count == 0)
                {
                    StatsText.Text = "✅ Просроченных выдач нет. Отлично!";
                    StatsText.Foreground = (System.Windows.Media.SolidColorBrush)FindResource("Success");
                }
                else
                {
                    StatsText.Text = $"⚠️ Найдено просроченных выдач: {count}";
                    StatsText.Foreground = (System.Windows.Media.SolidColorBrush)FindResource("Danger");
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadOverdueLoans();
        }

        private void ReturnBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int issuedId)
            {
                var confirm = MessageBox.Show("Вернуть эту книгу?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        connection.Open();

                        // Получаем InstanceID и PlannedReturnDate
                        int instanceId;
                        DateTime plannedReturnDate;
                        using (SqlCommand getInfoCmd = new SqlCommand(
                            "SELECT InstanceID, PlannedReturnDate FROM IssuedBooks WHERE ID = @ID", connection))
                        {
                            getInfoCmd.Parameters.AddWithValue("@ID", issuedId);
                            using (SqlDataReader reader = getInfoCmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    instanceId = Convert.ToInt32(reader["InstanceID"]);
                                    plannedReturnDate = Convert.ToDateTime(reader["PlannedReturnDate"]);
                                }
                                else
                                {
                                    MessageBox.Show("❌ Не удалось найти информацию о выдаче!", "Ошибка",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
                            }
                        }

                        DateTime actualReturnDate = DateTime.Now;
                        bool returnedOnTime = actualReturnDate <= plannedReturnDate;

                        // Обновляем IssuedBooks
                        using (SqlCommand updateIssued = new SqlCommand(
                            "UPDATE IssuedBooks SET ActualReturnDate = @ActualReturnDate, Status = N'Возвращена', ReturnedOnTime = @ReturnedOnTime WHERE ID = @ID", connection))
                        {
                            updateIssued.Parameters.AddWithValue("@ActualReturnDate", actualReturnDate);
                            updateIssued.Parameters.AddWithValue("@ReturnedOnTime", returnedOnTime);
                            updateIssued.Parameters.AddWithValue("@ID", issuedId);
                            updateIssued.ExecuteNonQuery();
                        }

                        // Обновляем BookInstances
                        using (SqlCommand updateInstance = new SqlCommand(
                            "UPDATE BookInstances SET Status = N'Доступна' WHERE ID = @ID", connection))
                        {
                            updateInstance.Parameters.AddWithValue("@ID", instanceId);
                            updateInstance.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Книга возвращена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadOverdueLoans(); // Обновляем список
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ Ошибка при возврате книги: " + ex.Message, "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}