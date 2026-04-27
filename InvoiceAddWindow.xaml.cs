using System;
using System.Windows;
using biblioteka.DTO;
using biblioteka.Services;

namespace biblioteka
{
    public partial class InvoiceAddWindow : Window
    {
        private readonly BookService _bookService;

        public InvoiceAddWindow()
        {
            InitializeComponent();
            _bookService = new BookService();
            LoadBooks();
        }

        private void LoadBooks()
        {
            try
            {
                var books = _bookService.GetAllBooks();
                BookComboBox.ItemsSource = books;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки книг: " + ex.Message);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InvoiceNumberBox.Text))
            {
                MessageBox.Show("Введите номер накладной", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!(BookComboBox.SelectedItem is BookDto selectedBook))
            {
                MessageBox.Show("Выберите книгу", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string priceText = PriceBox.Text.Trim().Replace('.', ',');
            if (!decimal.TryParse(priceText, out decimal price) || price < 0)
            {
                MessageBox.Show("Введите корректную цену", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _bookService.AddInstances(selectedBook.Id, quantity, price, InvoiceNumberBox.Text.Trim());
                MessageBox.Show($"Добавлено {quantity} экз. книги «{selectedBook.Title}» по накладной №{InvoiceNumberBox.Text}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}