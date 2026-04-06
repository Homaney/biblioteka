using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace biblioteka
{
    public partial class AddBookWindow : Window
    {
        private List<string> authorsList = new List<string>();
        private List<UDKItem> udkList = new List<UDKItem>();
        private int selectedUDKId = -1;
        private List<string> selectedAuthors = new List<string>();

        public class UDKItem
        {
            public int ID { get; set; }
            public string Code { get; set; }
            public string Description { get; set; }
        }

        public event EventHandler BookAdded;

        public AddBookWindow()
        {
            InitializeComponent();
            QuantityBox.Text = "1";
            LoadAuthors();
            LoadUDKs();
            SelectedAuthorsList.ItemsSource = selectedAuthors;

            // Инициализируем плейсхолдеры для всех текстовых полей
            InitializePlaceholders();

            // Подписываемся на события для поля автора
            AuthorInputBox.KeyDown += AuthorInputBox_KeyDown;
        }

        private void InitializePlaceholders()
        {
            AddPlaceholderBehavior(IdentifierBox);
            AddPlaceholderBehavior(TitleBox);
            AddPlaceholderBehavior(YearBox);
            AddPlaceholderBehavior(DescriptionBox);
            AddPlaceholderBehavior(AuthorInputBox);
            AddPlaceholderBehavior(UDKDisplayBox);
        }

        private void AddPlaceholderBehavior(TextBox textBox)
        {
            if (textBox == null) return;

            textBox.GotFocus += (s, e) => UpdatePlaceholderVisibility(textBox);
            textBox.LostFocus += (s, e) => UpdatePlaceholderVisibility(textBox);
            textBox.TextChanged += (s, e) => UpdatePlaceholderVisibility(textBox);

            UpdatePlaceholderVisibility(textBox);
        }

        private void UpdatePlaceholderVisibility(TextBox textBox)
        {
            if (textBox == null) return;

            var template = textBox.Template;
            if (template == null) return;

            var placeholder = template.FindName("placeholderText", textBox) as TextBlock;
            if (placeholder == null) return;

            if (textBox.IsReadOnly)
            {
                placeholder.Visibility = Visibility.Collapsed;
                return;
            }

            bool shouldShow = string.IsNullOrEmpty(textBox.Text) && !textBox.IsFocused;
            placeholder.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AuthorInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddAuthorFromInput();
                e.Handled = true;
            }
        }

        private void AddAuthorFromInput()
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
                    return;
                }
            }

            // Добавляем автора в список выбранных
            if (!selectedAuthors.Contains(authorToAdd))
            {
                selectedAuthors.Add(authorToAdd);
                SelectedAuthorsList.Items.Refresh();
            }

            AuthorInputBox.Text = "";
            AuthorsPopup.IsOpen = false;
        }

        private void LoadAuthors()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT FullName FROM Authors ORDER BY FullName";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        authorsList.Clear();
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                                authorsList.Add(reader.GetString(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки авторов: " + ex.Message);
            }
        }

        private void LoadUDKs()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT ID, Code, Description FROM UDK ORDER BY Code";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        udkList.Clear();
                        while (reader.Read())
                        {
                            udkList.Add(new UDKItem
                            {
                                ID = reader.GetInt32(0),
                                Code = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? "" : reader.GetString(2)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки УДК: " + ex.Message);
            }
        }

        private void AuthorDropdown_Click(object sender, RoutedEventArgs e)
        {
            if (authorsList.Count == 0)
            {
                MessageBox.Show("Список авторов пуст. Сначала добавьте авторов.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AuthorsListBox.ItemsSource = null;
            AuthorsListBox.ItemsSource = authorsList;
            AuthorsPopup.IsOpen = true;
            AuthorsPopup.StaysOpen = false;
        }

        private void AuthorItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is string selectedAuthor)
            {
                AuthorInputBox.Text = selectedAuthor;
                AuthorsPopup.IsOpen = false;
                e.Handled = true;
            }
        }

        private void UDKDropdown_Click(object sender, RoutedEventArgs e)
        {
            if (udkList.Count == 0)
            {
                MessageBox.Show("Список УДК пуст. Сначала добавьте УДК.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            UDKListBox.ItemsSource = null;
            UDKListBox.ItemsSource = udkList;
            UDKPopup.IsOpen = true;
            UDKPopup.StaysOpen = false;
        }

        private void UDKItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is UDKItem selectedUDK)
            {
                selectedUDKId = selectedUDK.ID;
                UDKDisplayBox.Text = selectedUDK.Code;
                UDKPopup.IsOpen = false;
                e.Handled = true;
            }
        }

        private void AddAuthor_Click(object sender, RoutedEventArgs e)
        {
            AddAuthorFromInput();
        }

        private bool AddNewAuthorToDatabase(string authorName)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = "INSERT INTO Authors (FullName) VALUES (@FullName)";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@FullName", authorName);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении автора: " + ex.Message);
                return false;
            }
        }

        private void RemoveAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string author)
            {
                selectedAuthors.Remove(author);
                SelectedAuthorsList.Items.Refresh();
            }
        }

        private void AddUDKButton_Click(object sender, RoutedEventArgs e)
        {
            var addUDKWindow = new AddUDKWindow();
            addUDKWindow.Owner = this;
            if (addUDKWindow.ShowDialog() == true)
            {
                LoadUDKs();
            }
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(QuantityBox.Text, out int quantity))
            {
                quantity++;
                QuantityBox.Text = quantity.ToString();
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

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
            }
        }

        private void AddBook_Click(object sender, RoutedEventArgs e)
        {
            string title = TitleBox.Text.Trim();
            string year = YearBox.Text.Trim();
            string description = DescriptionBox.Text.Trim();

            if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity < 1)
            {
                MessageBox.Show("Введите корректное количество экземпляров");
                QuantityBox.Focus();
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

            if (string.IsNullOrEmpty(year) || !int.TryParse(year, out int yearValue))
            {
                MessageBox.Show("Введите корректный год издания");
                YearBox.Focus();
                return;
            }

            if (yearValue < 1000 || yearValue > DateTime.Now.Year + 5)
            {
                MessageBox.Show($"Год должен быть от 1000 до {DateTime.Now.Year + 5}");
                YearBox.Focus();
                return;
            }

            if (SaveBookToDatabase(title, selectedAuthors, selectedUDKId, yearValue, description, quantity))
            {
                MessageBox.Show($"Книга успешно добавлена!");
                BookAdded?.Invoke(this, EventArgs.Empty);
                ClearForm();
            }
        }

        private bool SaveBookToDatabase(string title, List<string> authors, int udkId, int year,
            string description, int quantity)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Преобразуем список авторов в строку через запятую
                            string authorsString = string.Join(", ", authors);

                            // Добавляем книгу
                            string bookQuery = @"
                                INSERT INTO Books (Title, Yearr, UDK_ID, Description, Price, Authors)
                                OUTPUT INSERTED.ID
                                VALUES (@Title, @Yearr, @UDK_ID, @Description, 0, @Authors)";

                            int bookId;
                            using (SqlCommand cmd = new SqlCommand(bookQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Title", title);
                                cmd.Parameters.AddWithValue("@Yearr", year);
                                cmd.Parameters.AddWithValue("@UDK_ID", udkId);
                                cmd.Parameters.AddWithValue("@Description",
                                    string.IsNullOrEmpty(description) ? DBNull.Value : (object)description);
                                cmd.Parameters.AddWithValue("@Authors", authorsString);

                                bookId = (int)cmd.ExecuteScalar();
                            }

                            // Добавляем экземпляры
                            for (int i = 0; i < quantity; i++)
                            {
                                string inventoryNumber = $"{bookId}-{i + 1:000}";
                                string instanceQuery = @"
                                    INSERT INTO BookInstances (BookID, InventoryNumber, Status, CanBeSold)
                                    VALUES (@BookID, @InventoryNumber, N'Доступна', 1)";

                                using (SqlCommand cmd = new SqlCommand(instanceQuery, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@BookID", bookId);
                                    cmd.Parameters.AddWithValue("@InventoryNumber", inventoryNumber);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                            MessageBox.Show($"Книга успешно добавлена!\n\nID: {bookId}\nНазвание: {title}\nАвторы: {authorsString}\nУДК: {udkId}\nГод: {year}\nКоличество экземпляров: {quantity}",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка подключения к базе данных: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
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
            selectedUDKId = -1;
            QuantityBox.Text = "1";
            SelectedAuthorsList.Items.Refresh();

            UpdatePlaceholderVisibility(IdentifierBox);
            UpdatePlaceholderVisibility(TitleBox);
            UpdatePlaceholderVisibility(YearBox);
            UpdatePlaceholderVisibility(DescriptionBox);
            UpdatePlaceholderVisibility(AuthorInputBox);
            UpdatePlaceholderVisibility(UDKDisplayBox);
        }
    }
}