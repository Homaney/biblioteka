using System;
using System.Windows;
using biblioteka.DTO;
using biblioteka.Services;

namespace biblioteka
{
    public partial class UDKWindow : Window
    {
        private readonly UDKService _service;

        public UDKWindow()
        {
            InitializeComponent();
            _service = new UDKService();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var list = _service.GetAll();
                UDKDataGrid.ItemsSource = list;
                StatusText.Text = $"Загружено записей: {list.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
        }

        private void AddUDK_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddUDKWindow();
            window.Owner = this;
            if (window.ShowDialog() == true)
                LoadData();
        }

        private void EditUDK_Click(object sender, RoutedEventArgs e)
        {
            if (UDKDataGrid.SelectedItem is UDKDto selected)
            {
                var window = new EditUDKWindow(selected.Id);
                window.Owner = this;
                if (window.ShowDialog() == true)
                    LoadData();
            }
            else
            {
                MessageBox.Show("Выберите УДК для редактирования");
            }
        }

        private void DeleteUDK_Click(object sender, RoutedEventArgs e)
        {
            if (UDKDataGrid.SelectedItem is UDKDto selected)
            {
                var result = MessageBox.Show($"Удалить УДК '{selected.Code}'?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _service.Delete(selected.Id);
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите УДК для удаления");
            }
        }
    }
}