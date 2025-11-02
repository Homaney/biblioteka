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

        public ReadersInfoWindow()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            LoadReaders();
        }

        private void InitializeDatabaseConnection()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "Library.accdb");
            connection = new OleDbConnection($@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;");
        }

        private void LoadReaders()
        {
            try
            {
                connection.Open();
                OleDbDataAdapter adapter = new OleDbDataAdapter("SELECT ID, FullName, Phone FROM Readers ORDER BY FullName", connection);
                DataTable table = new DataTable();
                adapter.Fill(table);
                ReadersList.ItemsSource = table.DefaultView;
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

                LoadIssuedBooks(readerName);

                // Плавное появление карточки
                ReaderCard.Visibility = Visibility.Visible;
                DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                ReaderCard.BeginAnimation(OpacityProperty, fadeIn);

                SelectHint.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadIssuedBooks(string readerName)
        {
            try
            {
                connection.Open();
                OleDbDataAdapter adapter = new OleDbDataAdapter(
                    "SELECT BookID, IssueDate, ReturnDate FROM IssuedBooks WHERE ReaderName = ?",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("?", readerName);

                DataTable table = new DataTable();
                adapter.Fill(table);

                var books = new List<dynamic>();
                foreach (DataRow row in table.Rows)
                {
                    DateTime issue = Convert.ToDateTime(row["IssueDate"]);
                    string returnStr = row["ReturnDate"] != DBNull.Value ? Convert.ToDateTime(row["ReturnDate"]).ToShortDateString() : "ещё не сдано";

                    bool overdue = row["ReturnDate"] != DBNull.Value && Convert.ToDateTime(row["ReturnDate"]) < DateTime.Now;

                    books.Add(new
                    {
                        BookTitle = row["BookID"].ToString(),
                        Status = $"Выдано: {issue.ToShortDateString()}, Возврат: {returnStr}, {(overdue ? "Просрочена" : "В срок")}",
                        StatusColor = new SolidColorBrush(overdue ? Colors.Red : Colors.Green)
                    });
                }

                BooksList.ItemsSource = books;
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
    }
}
