using System;
using System.Windows;
using biblioteka.Services;

namespace biblioteka
{
    public partial class AddInstanceDialog : Window
    {
        private readonly int _bookId;
        private readonly BookService _bookService;

        public AddInstanceDialog(int bookId)
        {
            InitializeComponent();
            _bookId = bookId;
            _bookService = new BookService();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity < 1)
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
                for (int i = 0; i < quantity; i++)
                {
                    _bookService.AddInstance(_bookId, price);
                }
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении экземпляра: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}