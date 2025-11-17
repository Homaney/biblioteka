using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace biblioteka
{
    public partial class EditBookWindow : Window
    {
        private readonly string connectionString;
        private readonly int bookId;
        private int selectedUDKId = -1;
        private int availableInstancesCount = 0;
        private ContextMenu authorsContextMenu;
        private ContextMenu udkContextMenu;

        public event EventHandler BookUpdated;

        public EditBookWindow(OleDbConnection conn, int id)
        {
            InitializeComponent();
            connectionString = conn.ConnectionString; // Сохраняем строку подключения
            bookId = id;
            InitializeContextMenus();
            Loaded += (s, e) => LoadData();
        }

        private void InitializeContextMenus()
        {
            // Создаем ContextMenu для авторов
            authorsContextMenu = new ContextMenu
            {
                Background = (Brush)FindResource("CardBackground"),
                BorderBrush = (Brush)FindResource("CardBorder"),
                BorderThickness = new Thickness(1)
            };

            // Создаем ContextMenu для УДК
            udkContextMenu = new ContextMenu
            {
                Background = (Brush)FindResource("CardBackground"),
                BorderBrush = (Brush)FindResource("CardBorder"),
                BorderThickness = new Thickness(1)
            };
        }

        private void LoadData()
        {
            using (var connection = new OleDbConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Загружаем авторов
                    var authors = new List<string>();
                    using (var cmd = new OleDbCommand("SELECT FullName FROM Authors ORDER BY FullName", connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                                authors.Add(reader.GetString(0));
                        }
                    }

                    // Загружаем УДК
                    var udks = new DataTable();
                    using (var adapter = new OleDbDataAdapter("SELECT ID, Code FROM UDK ORDER BY Code", connection))
                    {
                        adapter.Fill(udks);
                    }

                    // Загружаем данные книги
                    using (var cmd = new OleDbCommand(
                        "SELECT Title, Yearr, UDK_ID, Description FROM Books WHERE Identifier = ?", connection))
                    {
                        cmd.Parameters.AddWithValue("@Identifier", bookId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                TitleBox.Text = SafeGetValue(reader, "Title");
                                YearBox.Text = SafeGetValue(reader, "Yearr");
                                DescriptionBox.Text = SafeGetValue(reader, "Description");

                                var udkIdObj = SafeGetObject(reader, "UDK_ID");
                                if (udkIdObj != null && udkIdObj != DBNull.Value)
                                {
                                    selectedUDKId = Convert.ToInt32(udkIdObj);
                                    // Устанавливаем текст УДК
                                    foreach (DataRowView row in udks.DefaultView)
                                    {
                                        if (Convert.ToInt32(row["ID"]) == selectedUDKId)
                                        {
                                            UDKTextBox.Text = row["Code"].ToString();
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    UDKTextBox.Text = "Выберите УДК...";
                                    UDKTextBox.Foreground = new SolidColorBrush(Colors.Gray);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Книга не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                Close();
                                return;
                            }
                        }
                    }

                    // Загружаем авторов книги
                    using (var cmd = new OleDbCommand(
                        @"SELECT a.FullName FROM Authors a 
                      INNER JOIN BookAuthors ba ON a.ID = ba.AuthorID 
                      WHERE ba.BookID = ?", connection))
                    {
                        cmd.Parameters.AddWithValue("@BookID", bookId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            SelectedAuthorsList.Items.Clear();
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(0))
                                    SelectedAuthorsList.Items.Add(reader.GetString(0));
                            }
                        }
                    }

                    // Инициализируем текстовые поля
                    AuthorsTextBox.Text = "Выберите автора...";
                    AuthorsTextBox.Foreground = new SolidColorBrush(Colors.Gray);

                    if (string.IsNullOrEmpty(UDKTextBox.Text))
                    {
                        UDKTextBox.Text = "Выберите УДК...";
                        UDKTextBox.Foreground = new SolidColorBrush(Colors.Gray);
                    }

                    // Загружаем количество доступных экземпляров
                    UpdateInstancesCount(connection);

                    IdentifierLabel.Text = $"ID: {bookId}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка загрузки данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            }
        }

        private void UpdateInstancesCount(OleDbConnection connection)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM BookInstances WHERE BookID = ? AND Status = 'На полке'";
                using (var cmd = new OleDbCommand(query, connection))
                {
                    cmd.Parameters.Add("@BookID", OleDbType.Integer).Value = bookId;
                    object result = cmd.ExecuteScalar();
                    availableInstancesCount = result != null ? Convert.ToInt32(result) : 0;
                    InstancesCountText.Text = $"Доступно экземпляров: {availableInstancesCount}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке количества экземпляров: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string SafeGetValue(OleDbDataReader reader, string column)
        {
            try
            {
                int colIndex = reader.GetOrdinal(column);
                return reader.IsDBNull(colIndex) ? "" : reader[colIndex].ToString();
            }
            catch
            {
                return "";
            }
        }

        private object SafeGetObject(OleDbDataReader reader, string column)
        {
            try
            {
                int colIndex = reader.GetOrdinal(column);
                return reader.IsDBNull(colIndex) ? null : reader[colIndex];
            }
            catch
            {
                return null;
            }
        }

        // Обработчик для правильного позиционирования курсора
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Для редактируемых полей ставим курсор в конец текста
                if (!textBox.IsReadOnly)
                {
                    textBox.CaretIndex = textBox.Text.Length;
                }
            }
        }

        private void AuthorsTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OpenAuthorsMenu();
            e.Handled = true;
        }

        private void OpenAuthorsMenu()
        {
            using (var connection = new OleDbConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    if (authorsContextMenu.Items.Count == 0)
                    {
                        // Загружаем авторов в ContextMenu
                        var authors = new List<string>();
                        using (var cmd = new OleDbCommand("SELECT FullName FROM Authors ORDER BY FullName", connection))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(0))
                                    authors.Add(reader.GetString(0));
                            }
                        }

                        foreach (string author in authors)
                        {
                            var menuItem = new MenuItem
                            {
                                Header = author,
                                Style = (Style)FindResource("DarkMenuItem")
                            };
                            menuItem.Click += (s, args) =>
                            {
                                AuthorsTextBox.Text = author;
                                AuthorsTextBox.Foreground = new SolidColorBrush(Colors.White);
                            };
                            authorsContextMenu.Items.Add(menuItem);
                        }
                    }

                    authorsContextMenu.PlacementTarget = AuthorsTextBox;
                    authorsContextMenu.IsOpen = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке авторов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UDKTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OpenUDKMenu();
            e.Handled = true;
        }

        private void OpenUDKMenu()
        {
            using (var connection = new OleDbConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    if (udkContextMenu.Items.Count == 0)
                    {
                        // Загружаем УДК в ContextMenu
                        var udks = new DataTable();
                        using (var adapter = new OleDbDataAdapter("SELECT ID, Code FROM UDK ORDER BY Code", connection))
                        {
                            adapter.Fill(udks);
                        }

                        foreach (DataRowView row in udks.DefaultView)
                        {
                            string code = row["Code"].ToString();
                            int id = Convert.ToInt32(row["ID"]);

                            var menuItem = new MenuItem
                            {
                                Header = code,
                                Style = (Style)FindResource("DarkMenuItem"),
                                Tag = id // Сохраняем ID в Tag
                            };
                            menuItem.Click += (s, args) =>
                            {
                                UDKTextBox.Text = code;
                                UDKTextBox.Foreground = new SolidColorBrush(Colors.White);
                                selectedUDKId = id;
                            };
                            udkContextMenu.Items.Add(menuItem);
                        }
                    }

                    udkContextMenu.PlacementTarget = UDKTextBox;
                    udkContextMenu.IsOpen = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке УДК: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddAuthor_Click(object sender, RoutedEventArgs e)
        {
            string author = AuthorsTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(author) && author != "Выберите автора...")
            {
                if (!SelectedAuthorsList.Items.Cast<string>().Contains(author))
                {
                    SelectedAuthorsList.Items.Add(author);
                    AuthorsTextBox.Text = "Выберите автора...";
                    AuthorsTextBox.Foreground = new SolidColorBrush(Colors.Gray);
                }
                else
                {
                    MessageBox.Show("Этот автор уже добавлен", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Выберите автора из списка", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Content is string author)
            {
                SelectedAuthorsList.Items.Remove(author);
            }
        }

        private void AddInstance_Click(object sender, RoutedEventArgs e)
        {
            using (var connection = new OleDbConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Генерируем инвентарный номер
                    string inventoryNumber = $"BK-{bookId}-{DateTime.Now:yyyyMMdd-HHmmss}";

                    using (var cmd = new OleDbCommand(
                        "INSERT INTO BookInstances (BookID, InventoryNumber, Status, AcquisitionDate) VALUES (?, ?, 'На полке', ?)",
                        connection))
                    {
                        // Явно указываем типы данных для параметров
                        cmd.Parameters.Add("@BookID", OleDbType.Integer).Value = bookId;
                        cmd.Parameters.Add("@InventoryNumber", OleDbType.VarWChar).Value = inventoryNumber;
                        cmd.Parameters.Add("@AcquisitionDate", OleDbType.Date).Value = DateTime.Now;

                        cmd.ExecuteNonQuery();
                    }

                    UpdateInstancesCount(connection);
                    MessageBox.Show("Экземпляр успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при добавлении экземпляра: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RemoveInstance_Click(object sender, RoutedEventArgs e)
        {
            if (availableInstancesCount == 0)
            {
                MessageBox.Show("Нет доступных экземпляров для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Удалить один экземпляр книги?\n\nТекущее количество: {availableInstancesCount}\nПосле удаления: {availableInstancesCount - 1}",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            using (var connection = new OleDbConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Находим первый доступный экземпляр для удаления
                    string query = "SELECT TOP 1 ID FROM BookInstances WHERE BookID = ? AND Status = 'На полке' ORDER BY ID";
                    int instanceId = -1;

                    using (var cmd = new OleDbCommand(query, connection))
                    {
                        cmd.Parameters.Add("@BookID", OleDbType.Integer).Value = bookId;
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            instanceId = Convert.ToInt32(result);
                        }
                    }

                    if (instanceId != -1)
                    {
                        // Удаляем экземпляр
                        using (var cmd = new OleDbCommand("DELETE FROM BookInstances WHERE ID = ?", connection))
                        {
                            cmd.Parameters.Add("@ID", OleDbType.Integer).Value = instanceId;
                            cmd.ExecuteNonQuery();
                        }

                        UpdateInstancesCount(connection);
                        MessageBox.Show("Экземпляр успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Не удалось найти экземпляр для удаления", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении экземпляра: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            using (var connection = new OleDbConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Обновляем книгу
                            using (var cmd = new OleDbCommand(
                                "UPDATE Books SET Title = ?, Yearr = ?, UDK_ID = ?, Description = ? WHERE Identifier = ?",
                                connection, transaction))
                            {
                                cmd.Parameters.Add("@Title", OleDbType.VarWChar).Value = TitleBox.Text.Trim();
                                cmd.Parameters.Add("@Yearr", OleDbType.VarWChar).Value = YearBox.Text.Trim();
                                cmd.Parameters.Add("@UDK_ID", OleDbType.Integer).Value = selectedUDKId > 0 ? (object)selectedUDKId : DBNull.Value;
                                cmd.Parameters.Add("@Description", OleDbType.VarWChar).Value = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? DBNull.Value : (object)DescriptionBox.Text.Trim();
                                cmd.Parameters.Add("@Identifier", OleDbType.Integer).Value = bookId;

                                if (cmd.ExecuteNonQuery() == 0)
                                {
                                    throw new Exception("Книга не найдена в базе данных");
                                }
                            }

                            // Обновляем авторов
                            UpdateBookAuthors(connection, transaction);

                            transaction.Commit();

                            MessageBox.Show("Изменения успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            BookUpdated?.Invoke(this, EventArgs.Empty);
                            Close();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception("Ошибка сохранения изменений: " + ex.Message, ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateBookAuthors(OleDbConnection connection, OleDbTransaction transaction)
        {
            // Удаляем старых авторов
            using (var cmd = new OleDbCommand("DELETE FROM BookAuthors WHERE BookID = ?", connection, transaction))
            {
                cmd.Parameters.Add("@BookID", OleDbType.Integer).Value = bookId;
                cmd.ExecuteNonQuery();
            }

            // Добавляем новых авторов
            foreach (string author in SelectedAuthorsList.Items)
            {
                using (var cmd = new OleDbCommand(
                    "INSERT INTO BookAuthors (BookID, AuthorID) SELECT ?, ID FROM Authors WHERE FullName = ?",
                    connection, transaction))
                {
                    cmd.Parameters.Add("@BookID", OleDbType.Integer).Value = bookId;
                    cmd.Parameters.Add("@FullName", OleDbType.VarWChar).Value = author;

                    int affected = cmd.ExecuteNonQuery();
                    if (affected == 0)
                    {
                        throw new Exception($"Автор '{author}' не найден в базе данных");
                    }
                }
            }
        }

        private bool ValidateInput()
        {
            // Проверка названия
            if (string.IsNullOrWhiteSpace(TitleBox.Text))
            {
                MessageBox.Show("Введите название книги", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleBox.Focus();
                return false;
            }

            // Проверка авторов
            if (SelectedAuthorsList.Items.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы одного автора", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                AuthorsTextBox.Focus();
                return false;
            }

            // Проверка УДК
            if (selectedUDKId == -1 || UDKTextBox.Text == "Выберите УДК...")
            {
                MessageBox.Show("Выберите УДК", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                UDKTextBox.Focus();
                return false;
            }

            // Проверка года
            if (string.IsNullOrWhiteSpace(YearBox.Text))
            {
                MessageBox.Show("Введите год издания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                YearBox.Focus();
                return false;
            }

            if (!int.TryParse(YearBox.Text, out int year) || year < 1000 || year > DateTime.Now.Year + 5)
            {
                MessageBox.Show($"Год должен быть числом от 1000 до {DateTime.Now.Year + 5}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                YearBox.Focus();
                YearBox.SelectAll();
                return false;
            }

            return true;
        }
    }
}