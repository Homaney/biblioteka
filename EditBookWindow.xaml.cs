using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace biblioteka
{
    public partial class EditBookWindow : Window
    {
        private readonly int _bookId;
        private int _selectedUDKId = -1;
        private int _availableInstancesCount = 0;
        private List<string> _authorsList = new List<string>();
        private DataTable _udksTable = new DataTable();
        private List<string> _selectedAuthors = new List<string>();

        public event EventHandler BookUpdated;

        public EditBookWindow(int id)
        {
            InitializeComponent();
            _bookId = id;
            SelectedAuthorsList.ItemsSource = _selectedAuthors;
            Loaded += (s, e) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // Загружаем всех авторов
                    string authorsQuery = "SELECT FullName FROM Authors ORDER BY FullName";
                    using (SqlCommand cmd = new SqlCommand(authorsQuery, connection))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        _authorsList.Clear();
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                                _authorsList.Add(reader.GetString(0));
                        }
                    }

                    // Загружаем УДК
                    string udkQuery = "SELECT ID, Code FROM UDK ORDER BY Code";
                    using (SqlDataAdapter adapter = new SqlDataAdapter(udkQuery, connection))
                    {
                        adapter.Fill(_udksTable);
                    }

                    // Загружаем данные книги
                    string bookQuery = @"
                        SELECT Title, Yearr, UDK_ID, Description, Price, Authors
                        FROM Books WHERE ID = @ID";

                    using (SqlCommand cmd = new SqlCommand(bookQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@ID", _bookId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                TitleBox.Text = reader["Title"]?.ToString() ?? "";
                                YearBox.Text = reader["Yearr"]?.ToString() ?? "";
                                DescriptionBox.Text = reader["Description"]?.ToString() ?? "";
                                PriceBox.Text = reader["Price"] != DBNull.Value
                                    ? ((decimal)reader["Price"]).ToString("F2") : "0.00";

                                // Загружаем авторов из строки (разделены запятыми)
                                string authors = reader["Authors"]?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(authors))
                                {
                                    _selectedAuthors.Clear();
                                    foreach (string author in authors.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                                    {
                                        _selectedAuthors.Add(author.Trim());
                                    }
                                }

                                if (reader["UDK_ID"] != DBNull.Value)
                                {
                                    _selectedUDKId = Convert.ToInt32(reader["UDK_ID"]);
                                    var udkRow = _udksTable.Select($"ID = {_selectedUDKId}").FirstOrDefault();
                                    if (udkRow != null)
                                        UDKTextBox.Text = udkRow["Code"].ToString();
                                }
                            }
                        }
                    }

                    UpdateInstancesCount(connection);
                    IdentifierLabel.Text = $"ID: {_bookId}";

                    // Обновляем список выбранных авторов в UI
                    SelectedAuthorsList.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void UpdateInstancesCount(SqlConnection connection)
        {
            string query = "SELECT COUNT(*) FROM BookInstances WHERE BookID = @BookID AND Status = N'Доступна'";
            using (SqlCommand cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@BookID", _bookId);
                _availableInstancesCount = (int)cmd.ExecuteScalar();
                InstancesCountText.Text = $"Доступно экземпляров: {_availableInstancesCount}";
            }
        }

        private void AuthorsTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var contextMenu = new ContextMenu();
            foreach (var author in _authorsList)
            {
                var menuItem = new MenuItem { Header = author };
                menuItem.Click += (s, args) => AuthorsTextBox.Text = author;
                contextMenu.Items.Add(menuItem);
            }
            contextMenu.PlacementTarget = AuthorsTextBox;
            contextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void UDKTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var contextMenu = new ContextMenu();
            foreach (DataRowView row in _udksTable.DefaultView)
            {
                string code = row["Code"].ToString();
                int id = Convert.ToInt32(row["ID"]);
                var menuItem = new MenuItem
                {
                    Header = code,
                    Tag = id
                };
                menuItem.Click += (s, args) =>
                {
                    UDKTextBox.Text = code;
                    _selectedUDKId = id;
                };
                contextMenu.Items.Add(menuItem);
            }
            contextMenu.PlacementTarget = UDKTextBox;
            contextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void AddAuthor_Click(object sender, RoutedEventArgs e)
        {
            string author = AuthorsTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(author) && author != "Выберите автора...")
            {
                if (!_selectedAuthors.Contains(author))
                {
                    _selectedAuthors.Add(author);
                    SelectedAuthorsList.Items.Refresh();
                }
                AuthorsTextBox.Text = "Выберите автора...";
            }
        }

        private void RemoveAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string author)
            {
                _selectedAuthors.Remove(author);
                SelectedAuthorsList.Items.Refresh();
            }
        }

        private void AddInstance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string inventoryNumber = $"BK-{_bookId}-{DateTime.Now:yyyyMMdd-HHmmss}";

                    string query = @"
                        INSERT INTO BookInstances (BookID, InventoryNumber, Status, CanBeSold)
                        VALUES (@BookID, @InventoryNumber, N'Доступна', 1)";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@BookID", _bookId);
                        cmd.Parameters.AddWithValue("@InventoryNumber", inventoryNumber);
                        cmd.ExecuteNonQuery();
                    }

                    UpdateInstancesCount(connection);
                    MessageBox.Show("Экземпляр добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveInstance_Click(object sender, RoutedEventArgs e)
        {
            if (_availableInstancesCount == 0)
            {
                MessageBox.Show("Нет доступных экземпляров", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show("Удалить один экземпляр?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    string query = @"
                        DELETE TOP (1) FROM BookInstances 
                        WHERE BookID = @BookID AND Status = N'Доступна'";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@BookID", _bookId);
                        cmd.ExecuteNonQuery();
                    }

                    UpdateInstancesCount(connection);
                    MessageBox.Show("Экземпляр удален!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PriceBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0) && e.Text != ".")
            {
                e.Handled = true;
            }

            if (e.Text == "." && ((TextBox)sender).Text.Contains("."))
            {
                e.Handled = true;
            }
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // Сохраняем авторов как строку через запятую
                    string authorsString = string.Join(", ", _selectedAuthors);

                    string updateQuery = @"
                        UPDATE Books 
                        SET Title = @Title, Yearr = @Yearr, UDK_ID = @UDK_ID, 
                            Description = @Description, Price = @Price,
                            Authors = @Authors
                        WHERE ID = @ID";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Title", TitleBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@Yearr", int.Parse(YearBox.Text));
                        cmd.Parameters.AddWithValue("@UDK_ID", _selectedUDKId);
                        cmd.Parameters.AddWithValue("@Description",
                            string.IsNullOrEmpty(DescriptionBox.Text) ? DBNull.Value : (object)DescriptionBox.Text);
                        cmd.Parameters.AddWithValue("@Price", decimal.Parse(PriceBox.Text));
                        cmd.Parameters.AddWithValue("@Authors", authorsString);
                        cmd.Parameters.AddWithValue("@ID", _bookId);

                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Изменения сохранены!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    BookUpdated?.Invoke(this, EventArgs.Empty);
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(TitleBox.Text))
            {
                MessageBox.Show("Введите название!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleBox.Focus();
                return false;
            }

            if (_selectedAuthors.Count == 0)
            {
                MessageBox.Show("Добавьте автора!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (_selectedUDKId == -1)
            {
                MessageBox.Show("Выберите УДК!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(YearBox.Text) || !int.TryParse(YearBox.Text, out int year) ||
                year < 1000 || year > DateTime.Now.Year + 5)
            {
                MessageBox.Show("Введите корректный год!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                YearBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(PriceBox.Text) || !decimal.TryParse(PriceBox.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("Введите корректную цену!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PriceBox.Focus();
                return false;
            }

            return true;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !textBox.IsReadOnly)
            {
                textBox.CaretIndex = textBox.Text.Length;
            }
        }
    }
}