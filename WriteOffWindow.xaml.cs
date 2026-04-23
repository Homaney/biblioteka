using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using biblioteka.DTO;
using biblioteka.Services;

namespace biblioteka
{
    public partial class WriteOffWindow : Window
    {
        private readonly BookService _bookService;
        private readonly BookInstanceService _instanceService;
        private readonly WriteOffService _writeOffService;
        private List<InstanceSelectionItem> _instanceItems;

        public WriteOffWindow()
        {
            InitializeComponent();
            _bookService = new BookService();
            _instanceService = new BookInstanceService();
            _writeOffService = new WriteOffService();

            // Устанавливаем дату и ограничения
            WriteOffDatePicker.SelectedDate = DateTime.Today;
            WriteOffDatePicker.DisplayDateEnd = DateTime.Today;  // ← нельзя выбрать дату позже сегодня

            LoadAvailableInstances();
        }

        private void LoadAvailableInstances()
        {
            try
            {
                var books = _bookService.GetAllBooks();
                _instanceItems = new List<InstanceSelectionItem>();

                foreach (var book in books)
                {
                    var instances = _instanceService.GetAvailableByBookId(book.Id);
                    foreach (var inst in instances)
                    {
                        _instanceItems.Add(new InstanceSelectionItem
                        {
                            InstanceId = inst.Id,
                            InventoryNumber = inst.InventoryNumber,
                            BookTitle = book.Title,
                            IsSelected = false
                        });
                    }
                }

                InstancesListBox.ItemsSource = _instanceItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки экземпляров: " + ex.Message);
            }
        }

        private void WriteOffButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ActNumberBox.Text))
            {
                MessageBox.Show("Введите номер акта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ReasonBox.Text))
            {
                MessageBox.Show("Укажите причину списания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!WriteOffDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату списания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем выбранные экземпляры
            var selectedInstances = _instanceItems.Where(item => item.IsSelected).ToList();
            if (selectedInstances.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один экземпляр", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dto = new WriteOffActCreateDto
                {
                    ActNumber = ActNumberBox.Text.Trim(),
                    WriteOffDate = WriteOffDatePicker.SelectedDate.Value,
                    Reason = ReasonBox.Text.Trim(),
                    InstanceIds = selectedInstances.Select(i => i.InstanceId).ToList()
                };

                _writeOffService.CreateWriteOffAct(dto);

                MessageBox.Show("Списание успешно оформлено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class InstanceSelectionItem : INotifyPropertyChanged
        {
            private bool _isSelected;

            public int InstanceId { get; set; }
            public string InventoryNumber { get; set; }
            public string BookTitle { get; set; }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                        OnPropertyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}