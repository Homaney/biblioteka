using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace biblioteka
{
    public partial class AddBookWindow : Window
    {
        private readonly OleDbConnection connection;
        private readonly List<string> authorsList = new List<string>();
        private readonly DataTable udksTable = new DataTable();
        private string selectedUDK = "";
        private int selectedUDKId = -1;
        private ObservableCollection<string> selectedAuthors = new ObservableCollection<string>();

        // Событие для уведомления о добавлении книги
        public event EventHandler BookAdded;

        public AddBookWindow(OleDbConnection conn)
        {
            InitializeComponent();
            connection = conn;
            SelectedAuthorsList.ItemsSource = selectedAuthors;
            QuantityBox.Text = "1";
            LoadAuthors();
            LoadUDKs();
        }

        private void LoadAuthors()
        {
            authorsList.Clear();
            try
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                using (var cmd = new OleDbCommand("SELECT FullName FROM Authors ORDER BY FullName", connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            string name = reader.GetString(0);
                            if (!string.IsNullOrWhiteSpace(name))
                                authorsList.Add(name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки авторов: " + ex.Message);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        private void LoadUDKs()
        {
            try
            {
                udksTable.Clear();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                var adapter = new OleDbDataAdapter("SELECT ID, Code, Description FROM UDK ORDER BY Code", connection);
                adapter.Fill(udksTable);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки УДК: " + ex.Message);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        private void AuthorDropdown_Click(object sender, RoutedEventArgs e)
        {
            var contextMenu = new ContextMenu();

            foreach (var author in authorsList)
            {
                var menuItem = new MenuItem { Header = author, Tag = author };
                menuItem.Click += (s, args) =>
                {
                    AuthorInputBox.Text = author;
                };
                contextMenu.Items.Add(menuItem);
            }

            contextMenu.PlacementTarget = sender as Button;
            contextMenu.IsOpen = true;
        }

        private void UDKDropdown_Click(object sender, RoutedEventArgs e)
        {
            var contextMenu = new ContextMenu();

            foreach (DataRow row in udksTable.Rows)
            {
                string code = row["Code"]?.ToString() ?? "—";
                string desc = row["Description"]?.ToString() ?? "—";
                int udkId = Convert.ToInt32(row["ID"]);

                var menuItem = new MenuItem
                {
                    Header = $"{code} - {desc}",
                    Tag = udkId
                };
                menuItem.Click += (s, args) =>
                {
                    selectedUDKId = udkId;
                    selectedUDK = code;
                    UDKDisplayBox.Text = code;
                };
                contextMenu.Items.Add(menuItem);
            }

            contextMenu.PlacementTarget = sender as Button;
            contextMenu.IsOpen = true;
        }

        private void AddAuthor_Click(object sender, RoutedEventArgs e)
        {
            string authorToAdd = AuthorInputBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(authorToAdd))
            {
                MessageBox.Show("Введите имя автора");
                AuthorInputBox.Focus();
                return;
            }

            // Добавляем автора в базу, если его нет
            if (!authorsList.Contains(authorToAdd))
            {
                if (AddNewAuthorToDatabase(authorToAdd))
                {
                    authorsList.Add(authorToAdd);
                    authorsList.Sort();
                }
                else
                {
                    MessageBox.Show("Не удалось добавить автора в базу данных");
                    return;
                }
            }

            // Добавляем автора в список выбранных
            if (!selectedAuthors.Contains(authorToAdd))
            {
                selectedAuthors.Add(authorToAdd);
            }

            // Очищаем поле ввода
            AuthorInputBox.Text = "";
        }

        private bool AddNewAuthorToDatabase(string authorName)
        {
            try
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                using (var cmd = new OleDbCommand("INSERT INTO Authors (FullName) VALUES (?)", connection))
                {
                    cmd.Parameters.AddWithValue("@FullName", authorName);
                    int result = cmd.ExecuteNonQuery();
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении автора в базу: " + ex.Message);
                return false;
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        private void RemoveAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string author)
            {
                selectedAuthors.Remove(author);
            }
        }

        private void AddUDKButton_Click(object sender, RoutedEventArgs e)
        {
            var addUDKWindow = new AddUDKWindow(connection);
            addUDKWindow.Owner = this;
            if (addUDKWindow.ShowDialog() == true)
            {
                // Обновляем список УДК после успешного добавления
                LoadUDKs();
            }
        }

        private void NumericTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
            }
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(QuantityBox.Text, out int quantity))
            {
                quantity++;
                QuantityBox.Text = quantity.ToString();
            }
            else
            {
                QuantityBox.Text = "1";
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(QuantityBox.Text, out int quantity) && quantity > 1)
            {
                quantity--;
                QuantityBox.Text = quantity.ToString();
            }
        }

        private void AddBook_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string id = IdentifierBox.Text.Trim();
                string title = TitleBox.Text.Trim();
                string year = YearBox.Text.Trim();
                string description = DescriptionBox.Text.Trim();

                // Получаем количество экземпляров
                if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity < 1)
                {
                    MessageBox.Show("Введите корректное количество экземпляров (не менее 1)");
                    QuantityBox.Focus();
                    return;
                }

                // Проверка обязательных полей
                if (string.IsNullOrEmpty(id))
                {
                    MessageBox.Show("Введите ID книги");
                    IdentifierBox.Focus();
                    return;
                }

                if (!int.TryParse(id, out int bookId))
                {
                    MessageBox.Show("ID книги должен быть числом");
                    IdentifierBox.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(title))
                {
                    MessageBox.Show("Введите название книги");
                    TitleBox.Focus();
                    return;
                }

                if (selectedAuthors.Count == 0)
                {
                    MessageBox.Show("Добавьте хотя бы одного автора");
                    return;
                }

                if (selectedUDKId == -1)
                {
                    MessageBox.Show("Выберите УДК из списка");
                    return;
                }

                if (string.IsNullOrEmpty(year))
                {
                    MessageBox.Show("Введите год издания");
                    YearBox.Focus();
                    return;
                }

                // Проверяем что год - число
                if (!int.TryParse(year, out int yearValue))
                {
                    MessageBox.Show("Год должен быть числом");
                    YearBox.Focus();
                    return;
                }

                if (yearValue < 1000 || yearValue > DateTime.Now.Year + 5)
                {
                    MessageBox.Show("Год должен быть числом от 1000 до " + (DateTime.Now.Year + 5));
                    YearBox.Focus();
                    return;
                }

                // Сохраняем книгу в базу данных
                if (SaveBookToDatabase(bookId, title, selectedAuthors.ToList(), selectedUDKId, yearValue, description, quantity))
                {
                    MessageBox.Show($"Книга успешно добавлена в базу данных!\n\nID: {bookId}\nНазвание: {title}\nАвторы: {string.Join(", ", selectedAuthors)}\nУДК: {selectedUDK}\nГод: {year}\nКоличество экземпляров: {quantity}");

                    // Вызываем событие добавления книги
                    BookAdded?.Invoke(this, EventArgs.Empty);

                    ClearForm();
                }
                else
                {
                    MessageBox.Show("Ошибка при сохранении книги в базу данных");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении книги: " + ex.Message);
            }
        }

        private bool SaveBookToDatabase(int id, string title, List<string> authors, int udkId, int year, string description, int quantity)
        {
            try
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Проверяем, существует ли уже книга с таким ID
                        using (var checkCmd = new OleDbCommand("SELECT COUNT(*) FROM Books WHERE Identifier = ?", connection, transaction))
                        {
                            checkCmd.Parameters.AddWithValue("@Identifier", id);
                            int existingCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (existingCount > 0)
                            {
                                MessageBox.Show("Книга с таким ID уже существует в базе данных");
                                transaction.Rollback();
                                return false;
                            }
                        }

                        // Добавляем книгу в таблицу Books
                        using (var cmd = new OleDbCommand(
                            "INSERT INTO Books (Identifier, Title, Yearr, UDK_ID, Description) VALUES (?, ?, ?, ?, ?)",
                            connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Identifier", id);
                            cmd.Parameters.AddWithValue("@Title", title);
                            cmd.Parameters.AddWithValue("@Yearr", year);
                            cmd.Parameters.AddWithValue("@UDK_ID", udkId);
                            cmd.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(description) ? DBNull.Value : (object)description);

                            int booksResult = cmd.ExecuteNonQuery();
                            if (booksResult == 0)
                            {
                                transaction.Rollback();
                                MessageBox.Show("Не удалось добавить книгу в таблицу Books");
                                return false;
                            }
                        }

                        // Создаем экземпляры книги в таблице BookInstances
                        for (int i = 0; i < quantity; i++)
                        {
                            string inventoryNumber = $"{id}-{i + 1:000}";

                            using (var instanceCmd = new OleDbCommand(
                                "INSERT INTO BookInstances (BookID, InventoryNumber, Status, AcquisitionDate) VALUES (?, ?, ?, ?)",
                                connection, transaction))
                            {
                                instanceCmd.Parameters.AddWithValue("@BookID", id);
                                instanceCmd.Parameters.AddWithValue("@InventoryNumber", inventoryNumber);
                                instanceCmd.Parameters.AddWithValue("@Status", "Доступна");
                                instanceCmd.Parameters.AddWithValue("@AcquisitionDate", DateTime.Now.Date);

                                instanceCmd.ExecuteNonQuery();
                            }
                        }

                        // Добавляем связи книга-автор в таблицу BookAuthors
                        foreach (var author in authors)
                        {
                            // Получаем ID автора
                            int authorId = GetAuthorId(author, transaction);
                            if (authorId == -1)
                            {
                                transaction.Rollback();
                                MessageBox.Show($"Ошибка: автор '{author}' не найден в базе данных");
                                return false;
                            }

                            // Проверяем, существует ли уже такая связь
                            using (var checkLinkCmd = new OleDbCommand(
                                "SELECT COUNT(*) FROM BookAuthors WHERE BookID = ? AND AuthorID = ?",
                                connection, transaction))
                            {
                                checkLinkCmd.Parameters.AddWithValue("@BookID", id);
                                checkLinkCmd.Parameters.AddWithValue("@AuthorID", authorId);
                                int linkCount = Convert.ToInt32(checkLinkCmd.ExecuteScalar());

                                if (linkCount == 0)
                                {
                                    // Добавляем связь только если её нет
                                    using (var linkCmd = new OleDbCommand(
                                        "INSERT INTO BookAuthors (BookID, AuthorID) VALUES (?, ?)",
                                        connection, transaction))
                                    {
                                        linkCmd.Parameters.AddWithValue("@BookID", id);
                                        linkCmd.Parameters.AddWithValue("@AuthorID", authorId);
                                        linkCmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Ошибка при сохранении книги: " + ex.Message + "\n\nДетали: " + ex.InnerException?.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка подключения к базе данных: " + ex.Message);
                return false;
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        private int GetAuthorId(string authorName, OleDbTransaction transaction)
        {
            try
            {
                using (var cmd = new OleDbCommand("SELECT ID FROM Authors WHERE FullName = ?", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@FullName", authorName);
                    var result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении ID автора '{authorName}': {ex.Message}");
                return -1;
            }
        }

        private void ClearForm()
        {
            IdentifierBox.Text = "";
            TitleBox.Text = "";
            YearBox.Text = "";
            DescriptionBox.Text = "";
            selectedAuthors.Clear();
            AuthorInputBox.Text = "";
            UDKDisplayBox.Text = "";
            selectedUDK = "";
            selectedUDKId = -1;
            QuantityBox.Text = "1";
        }
    }
}