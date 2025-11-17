using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace biblioteka
{
    public partial class ReadersInfoWindow : Window
    {
        private OleDbConnection connection;
        public ReadersInfoWindow(OleDbConnection conn)
        {
            InitializeComponent();
            connection = conn;
            LoadReaders();
        }
        private void LoadReaders()
        {
            try
            {
                connection.Open();
                using (OleDbDataAdapter adapter = new OleDbDataAdapter("SELECT ID, FullName, Phone FROM Readers ORDER BY FullName", connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    ReadersList.ItemsSource = table.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке читателей: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }
        private void ReadersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReadersList.SelectedItem is DataRowView readerRow)
            {
                string readerName = readerRow["FullName"].ToString();
                FullNameText.Text = readerName;
                PhoneText.Text = "Телефон: " + readerRow["Phone"].ToString();
                LoadIssuedBooks(Convert.ToInt32(readerRow["ID"]));
                ReaderCard.Visibility = Visibility.Visible;
                DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                ReaderCard.BeginAnimation(OpacityProperty, fadeIn);
                SelectHint.Visibility = Visibility.Collapsed;
            }
        }
        private void LoadIssuedBooks(int readerId)
        {
            try
            {
                connection.Open();
                string query = @"
                    SELECT ib.ID AS IssuedId, b.Title AS BookTitle, bi.InventoryNumber, ib.IssueDate, ib.PlannedReturnDate, ib.ActualReturnDate, ib.Status
                    FROM ((IssuedBooks ib INNER JOIN BookInstances bi ON ib.InstanceID = bi.ID)
                    INNER JOIN Books b ON bi.BookID = b.Identifier)
                    WHERE ib.ReaderID = ? AND ib.Status = 'Выдана'";
                using (OleDbCommand cmd = new OleDbCommand(query, connection))
                {
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = readerId;
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);
                        var books = new List<dynamic>();
                        foreach (DataRow row in table.Rows)
                        {
                            DateTime issue = Convert.ToDateTime(row["IssueDate"]);
                            DateTime planned = Convert.ToDateTime(row["PlannedReturnDate"]);
                            bool overdue = planned < DateTime.Now && row["ActualReturnDate"] == DBNull.Value;
                            string statusText = overdue ? "Просрочена" : "В срок";
                            books.Add(new
                            {
                                IssuedId = row["IssuedId"],
                                BookTitle = row["BookTitle"].ToString(),
                                InventoryNumber = "Инв. №: " + row["InventoryNumber"].ToString(),
                                Status = $"Выдано: {issue.ToShortDateString()}, Возврат: {planned.ToShortDateString()} ({statusText})",
                                StatusColor = new SolidColorBrush(overdue ? Colors.Red : Colors.Green)
                            });
                        }
                        BooksList.ItemsSource = books;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке выданных книг: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }
        private void ReturnBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int issuedId)
            {
                var confirm = MessageBox.Show("Вернуть книгу?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;
                try
                {
                    connection.Open();
                    // Находим InstanceID
                    int instanceId;
                    using (OleDbCommand getInstanceCmd = new OleDbCommand("SELECT InstanceID FROM IssuedBooks WHERE ID = ?", connection))
                    {
                        getInstanceCmd.Parameters.Add("?", OleDbType.Integer).Value = issuedId;
                        instanceId = Convert.ToInt32(getInstanceCmd.ExecuteScalar());
                    }
                    // Обновляем IssuedBooks
                    using (OleDbCommand updateIssued = new OleDbCommand("UPDATE IssuedBooks SET ActualReturnDate = ?, Status = 'Возвращена' WHERE ID = ?", connection))
                    {
                        updateIssued.Parameters.Add("?", OleDbType.Date).Value = DateTime.Now;
                        updateIssued.Parameters.Add("?", OleDbType.Integer).Value = issuedId;
                        updateIssued.ExecuteNonQuery();
                    }
                    // Обновляем BookInstances
                    using (OleDbCommand updateInstance = new OleDbCommand("UPDATE BookInstances SET Status = 'На полке' WHERE ID = ?", connection))
                    {
                        updateInstance.Parameters.Add("?", OleDbType.Integer).Value = instanceId;
                        updateInstance.ExecuteNonQuery();
                    }
                    MessageBox.Show("Книга возвращена!", "Готово");
                    // Перезагружаем список
                    ReadersList_SelectionChanged(null, null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при возврате: " + ex.Message);
                }
                finally
                {
                    connection.Close();
                }
            }
        }
    }
}