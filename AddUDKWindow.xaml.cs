using System;
using System.Windows;
using biblioteka.DTO;
using biblioteka.Services;

namespace biblioteka
{
    public partial class AddUDKWindow : Window
    {
        public AddUDKWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dto = new UDKCreateDto
                {
                    Code = CodeTextBox.Text.Trim(),
                    Description = DescriptionTextBox.Text.Trim()
                };

                var service = new UDKService();
                service.Add(dto);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}