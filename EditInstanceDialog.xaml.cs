using System;
using System.Windows;
using biblioteka.DTO;
using biblioteka.Services;

namespace biblioteka
{
    public partial class EditInstanceDialog : Window
    {
        private readonly BookInstanceDto _instance;
        private readonly BookInstanceService _service;

        public EditInstanceDialog(BookInstanceDto instance)
        {
            InitializeComponent();
            _instance = instance;
            _service = new BookInstanceService();

            InventoryNumberText.Text = instance.InventoryNumber;
            PriceBox.Text = instance.Price.ToString("F2");
            CanBeSoldCheckBox.IsChecked = instance.CanBeSold;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string priceText = PriceBox.Text.Trim().Replace('.', ',');
            if (!decimal.TryParse(priceText, out decimal price) || price < 0)
            {
                MessageBox.Show("Введите корректную цену", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var updateDto = new BookInstanceUpdateDto
                {
                    Id = _instance.Id,
                    BookId = _instance.BookId,
                    InventoryNumber = _instance.InventoryNumber,
                    Status = _instance.Status,
                    CanBeSold = CanBeSoldCheckBox.IsChecked ?? false,
                    Price = price
                };
                _service.Update(updateDto);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}