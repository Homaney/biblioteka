using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using biblioteka.DTO;
using biblioteka.Services;

namespace biblioteka
{
    public partial class SellBookWindow : Window
    {
        private BookService _bookService;
        private ReaderService _readerService;
        private SaleService _saleService;
        private List<BookDto> _books;

        public SellBookWindow()
        {
            // 1. Инициализация компонентов XAML
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка в InitializeComponent (XAML): {ex.Message}\n\n{ex.StackTrace}",
                    "Критическая ошибка XAML", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 2. Создание сервисов
            try
            {
                _bookService = new BookService();
                _readerService = new ReaderService();
                _saleService = new SaleService();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания сервисов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 3. Установка значений по умолчанию для DatePicker
            try
            {
                SaleDatePicker.SelectedDate = DateTime.Today;
                SaleDatePicker.DisplayDateStart = new DateTime(2000, 1, 1);
                SaleDatePicker.DisplayDateEnd = DateTime.Today;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка установки даты: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // 4. Загрузка данных (с защитой от null)
            try
            {
                LoadBooks();
                LoadReaders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Дополнительная инициализация после полной загрузки окна (если нужно)
        }

        private void LoadBooks()
        {
            try
            {
                var allBooks = _bookService?.GetAllBooks();
                _books = allBooks?.Where(b => b != null && b.AvailableInstances > 0).ToList() ?? new List<BookDto>();
                BookComboBox.ItemsSource = _books;
                BookComboBox.DisplayMemberPath = "Title";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки книг: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                BookComboBox.ItemsSource = new List<BookDto>();
            }
        }

        private void LoadReaders()
        {
            try
            {
                var readers = _readerService?.GetAll() ?? new List<ReaderDto>();
                ReaderComboBox.ItemsSource = readers;
                ReaderComboBox.DisplayMemberPath = "FullName";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки читателей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                ReaderComboBox.ItemsSource = new List<ReaderDto>();
            }
        }

        private void BookComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (BookComboBox.SelectedItem is BookDto selectedBook)
                {
                    PriceBox.Text = selectedBook.Price.ToString("F2");
                    QuantityBox.Text = "1";
                    AvailableHint.Text = $"Доступно экземпляров: {selectedBook.AvailableInstances}";
                    AvailableHint.Visibility = Visibility.Visible;
                    UpdateTotal();
                }
                else
                {
                    AvailableHint.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка выбора книги: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void QuantityBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTotal();
            CheckAvailability();
        }

        private void PriceBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            try
            {
                if (!IsLoaded || TotalText == null)
                    return;

                if (BookComboBox?.SelectedItem is BookDto selectedBook &&
                    int.TryParse(QuantityBox?.Text, out int quantity) &&
                    decimal.TryParse(PriceBox?.Text, out decimal price))
                {
                    decimal total = quantity * price;
                    TotalText.Text = $"{total:F2} BYN";
                }
                else
                {
                    TotalText.Text = "0 BYN";
                }
            }
            catch
            {
                if (TotalText != null)
                    TotalText.Text = "0 BYN";
            }
        }

        private void CheckAvailability()
        {
            try
            {
                if (BookComboBox.SelectedItem is BookDto selectedBook &&
                    int.TryParse(QuantityBox.Text, out int quantity))
                {
                    if (quantity > selectedBook.AvailableInstances)
                    {
                        QuantityBox.BorderBrush = Brushes.Red;
                        QuantityBox.ToolTip = $"Доступно только {selectedBook.AvailableInstances} экземпляров";
                        AvailableHint.Text = $"❌ Доступно только {selectedBook.AvailableInstances} экземпляров!";
                        AvailableHint.Foreground = Brushes.Red;
                    }
                    else
                    {
                        QuantityBox.BorderBrush = (SolidColorBrush)FindResource("CardBorder");
                        QuantityBox.ToolTip = null;
                        AvailableHint.Text = $"Доступно экземпляров: {selectedBook.AvailableInstances}";
                        AvailableHint.Foreground = (SolidColorBrush)FindResource("TextSecondary");
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки оформления
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

        private void QuantityBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void PriceBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0) && e.Text != ".")
                e.Handled = true;
            else if (e.Text == "." && ((TextBox)sender).Text.Contains("."))
                e.Handled = true;
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
                MessageBox.Show($"Ошибка добавления читателя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SellButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(BookComboBox.SelectedItem is BookDto selectedBook))
                {
                    MessageBox.Show("Выберите книгу!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!(ReaderComboBox.SelectedItem is ReaderDto selectedReader))
                {
                    MessageBox.Show("Выберите покупателя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!SaleDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите дату продажи!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity <= 0)
                {
                    MessageBox.Show("Введите корректное количество!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    QuantityBox.Focus();
                    return;
                }

                if (!decimal.TryParse(PriceBox.Text, out decimal price) || price < 0)
                {
                    MessageBox.Show("Введите корректную цену!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PriceBox.Focus();
                    return;
                }

                var saleDto = new SaleCreateDto
                {
                    BookId = selectedBook.Id,
                    ReaderId = selectedReader.Id,
                    Quantity = quantity,
                    UnitPrice = price,
                    SaleDate = SaleDatePicker.SelectedDate.Value,
                    Notes = NotesBox.Text?.Trim() ?? ""
                };

                _saleService.AddSale(saleDto);

                MessageBox.Show($"Продажа на сумму {quantity * price:F2} BYN успешно оформлена!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка оформления продажи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}