using System;
using System.Windows;
using biblioteka.DTO;
using biblioteka.Services;

namespace biblioteka
{
    public partial class EditUDKWindow : Window
    {
        private readonly int _id;
        private readonly UDKService _service;

        public EditUDKWindow(int id)
        {
            InitializeComponent();
            _id = id;
            _service = new UDKService();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var dto = _service.GetById(_id);
                if (dto != null)
                {
                    CodeTextBox.Text = dto.Code;
                    DescriptionTextBox.Text = dto.Description;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
                Close();
            }
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

                _service.Update(_id, dto);

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