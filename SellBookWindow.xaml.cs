using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace biblioteka
{
    public partial class SellBookWindow : Window
    {
        public class BookItem
        {
            public int ID { get; set; }
            public string Title { get; set; }
            public decimal Price { get; set; }
            public int Available { get; set; }

            // ТОЛЬКО НАЗВАНИЕ КНИГИ, без скобок
            public override string ToString() => Title;
        }

        public class ReaderItem
        {
            public int ID { get; set; }
            public string FullName { get; set; }
            public override string ToString() => FullName;
        }

        public SellBookWindow()
        {
            try
            {
                InitializeComponent();

                // Устанавливаем дату по умолчанию - сегодня
                if (SaleDatePicker != null)
                {
                    SaleDatePicker.SelectedDate = DateTime.Today;

                    // Ограничиваем выбор даты (не раньше 2000 года и не позже сегодня)
                    SaleDatePicker.DisplayDateStart = new DateTime(2000, 1, 1);
                    SaleDatePicker.DisplayDateEnd = DateTime.Today;
                }

                LoadBooks();
                LoadReaders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации окна: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Дополнительная инициализация после загрузки окна
        }

        private void LoadBooks()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT ID, Title, Price, AvailableForSale 
                        FROM Books 
                        WHERE AvailableForSale > 0 
                        ORDER BY Title";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        var books = new List<BookItem>();
                        while (reader.Read())
                        {
                            books.Add(new BookItem
                            {
                                ID = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Price = reader.GetDecimal(2),
                                Available = reader.GetInt32(3)
                            });
                        }

                        if (BookComboBox != null)
                        {
                            BookComboBox.ItemsSource = books;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки книг: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadReaders()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT ID, FullName FROM Readers ORDER BY FullName";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        var readers = new List<ReaderItem>();
                        while (reader.Read())
                        {
                            readers.Add(new ReaderItem
                            {
                                ID = reader.GetInt32(0),
                                FullName = reader.GetString(1)
                            });
                        }

                        if (ReaderComboBox != null)
                        {
                            ReaderComboBox.ItemsSource = readers;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки читателей: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BookComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (BookComboBox.SelectedItem is BookItem book)
                {
                    if (PriceBox != null)
                    {
                        PriceBox.Text = book.Price.ToString("F2");
                    }
                    if (QuantityBox != null)
                    {
                        QuantityBox.Text = "1";
                    }

                    // Показываем подсказку о доступном количестве
                    if (AvailableHint != null)
                    {
                        AvailableHint.Text = $"Доступно экземпляров: {book.Available}";
                        AvailableHint.Visibility = Visibility.Visible;
                    }

                    UpdateTotal();
                }
                else
                {
                    if (AvailableHint != null)
                    {
                        AvailableHint.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выборе книги: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void QuantityBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTotal();
            CheckAvailability(); // Проверяем доступность при изменении количества
        }

        private void PriceBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            try
            {
                if (BookComboBox?.SelectedItem is BookItem book &&
                    QuantityBox != null && PriceBox != null &&
                    int.TryParse(QuantityBox.Text, out int quantity) &&
                    decimal.TryParse(PriceBox.Text, out decimal price))
                {
                    decimal total = quantity * price;
                    if (TotalText != null)
                    {
                        TotalText.Text = $"{total:F2} BYN";
                    }
                }
                else
                {
                    if (TotalText != null)
                    {
                        TotalText.Text = "0 BYN";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка обновления итога: " + ex.Message);
            }
        }

        private void CheckAvailability()
        {
            try
            {
                if (BookComboBox?.SelectedItem is BookItem book &&
                    QuantityBox != null &&
                    int.TryParse(QuantityBox.Text, out int quantity))
                {
                    if (quantity > book.Available)
                    {
                        // Подсвечиваем поле красным, если количество превышает доступное
                        QuantityBox.BorderBrush = System.Windows.Media.Brushes.Red;
                        QuantityBox.ToolTip = $"Доступно только {book.Available} экземпляров";

                        if (AvailableHint != null)
                        {
                            AvailableHint.Text = $"❌ Доступно только {book.Available} экземпляров!";
                            AvailableHint.Foreground = System.Windows.Media.Brushes.Red;
                        }
                    }
                    else
                    {
                        // Возвращаем нормальный цвет
                        QuantityBox.BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("CardBorder");
                        QuantityBox.ToolTip = null;

                        if (AvailableHint != null)
                        {
                            AvailableHint.Text = $"Доступно экземпляров: {book.Available}";
                            AvailableHint.Foreground = (System.Windows.Media.SolidColorBrush)FindResource("TextSecondary");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка проверки доступности: " + ex.Message);
            }
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (QuantityBox != null && int.TryParse(QuantityBox.Text, out int quantity))
                {
                    quantity++;
                    QuantityBox.Text = quantity.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (QuantityBox != null && int.TryParse(QuantityBox.Text, out int quantity) && quantity > 1)
                {
                    quantity--;
                    QuantityBox.Text = quantity.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void QuantityBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void AddReader_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addReaderWindow = new AddReaderWindow();
                addReaderWindow.Owner = this;
                if (addReaderWindow.ShowDialog() == true)
                {
                    LoadReaders();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении читателя: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsValidSaleDate(DateTime? date)
        {
            if (!date.HasValue)
                return false;

            DateTime selectedDate = date.Value.Date;
            DateTime minDate = new DateTime(2000, 1, 1);
            DateTime maxDate = DateTime.Today;

            return selectedDate >= minDate && selectedDate <= maxDate;
        }

        private void SellButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BookComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите книгу!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (ReaderComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите покупателя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!IsValidSaleDate(SaleDatePicker.SelectedDate))
                {
                    MessageBox.Show("Некорректная дата продажи!\n\nДата должна быть:\n• Не раньше 01.01.2000\n• Не позже сегодняшнего дня",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SaleDatePicker.Focus();
                    return;
                }

                var book = (BookItem)BookComboBox.SelectedItem;
                var reader = (ReaderItem)ReaderComboBox.SelectedItem;

                if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity <= 0)
                {
                    MessageBox.Show("Введите корректное количество!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    QuantityBox.Focus();
                    return;
                }

                if (quantity > book.Available)
                {
                    MessageBox.Show($"Доступно только {book.Available} экземпляров!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    QuantityBox.Focus();
                    return;
                }

                if (!decimal.TryParse(PriceBox.Text, out decimal price) || price < 0)
                {
                    MessageBox.Show("Введите корректную цену!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PriceBox.Focus();
                    return;
                }

                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    string query = @"
                        INSERT INTO Sales (BookID, ReaderID, Quantity, UnitPrice, Notes, SaleDate)
                        VALUES (@BookID, @ReaderID, @Quantity, @UnitPrice, @Notes, @SaleDate)";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@BookID", book.ID);
                        cmd.Parameters.AddWithValue("@ReaderID", reader.ID);
                        cmd.Parameters.AddWithValue("@Quantity", quantity);
                        cmd.Parameters.AddWithValue("@UnitPrice", price);
                        cmd.Parameters.AddWithValue("@Notes",
                            string.IsNullOrEmpty(NotesBox.Text) ? DBNull.Value : (object)NotesBox.Text);
                        cmd.Parameters.AddWithValue("@SaleDate", SaleDatePicker.SelectedDate.Value);

                        cmd.ExecuteNonQuery();
                    }

                    decimal total = quantity * price;
                    MessageBox.Show($"Продажа на сумму {total:F2} BYN успешно оформлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при оформлении продажи: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}