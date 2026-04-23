using biblioteka.DAO;
using biblioteka.DTO;
using biblioteka.Entities;
using biblioteka.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace biblioteka
{
    public partial class EditWriteOffWindow : Window
    {
        private readonly int _actId;
        private readonly WriteOffService _writeOffService;
        private readonly BookService _bookService;
        private readonly BookInstanceService _instanceService;
        private List<InstanceSelectionItem> _instanceItems;

        public EditWriteOffWindow(int actId)
        {
            InitializeComponent();
            _actId = actId;
            _writeOffService = new WriteOffService();
            _bookService = new BookService();
            _instanceService = new BookInstanceService();

            LoadActData();
            LoadAllInstances();
        }

        private void LoadActData()
        {
            WriteOffActEntity act = new WriteOffDAO().GetActById(_actId);
            if (act == null)
            {
                MessageBox.Show("Акт не найден");
                Close();
                return;
            }

            ActNumberBox.Text = act.ActNumber;
            WriteOffDatePicker.SelectedDate = act.WriteOffDate;
            WriteOffDatePicker.DisplayDateEnd = DateTime.Today;
            ReasonBox.Text = act.Reason;
        }

        private void LoadAllInstances()
        {
            var allBooks = _bookService.GetAllBooks();
            var actInstanceIds = new WriteOffDAO().GetActInstanceIds(_actId);
            _instanceItems = new List<InstanceSelectionItem>();

            foreach (var book in allBooks)
            {
                // Загружаем все экземпляры (не только доступные), чтобы можно было видеть уже списанные в этом акте
                var instances = _instanceService.GetByBookId(book.Id);
                foreach (var inst in instances)
                {
                    // Если экземпляр списан в другом акте – не показываем, или показываем только если его WriteOffActID == _actId
                    if (inst.Status == "Списана" && inst.WriteOffActID != _actId)
                        continue;

                    _instanceItems.Add(new InstanceSelectionItem
                    {
                        InstanceId = inst.Id,
                        InventoryNumber = inst.InventoryNumber,
                        BookTitle = book.Title,
                        IsSelected = actInstanceIds.Contains(inst.Id)
                    });
                }
            }

            InstancesListBox.ItemsSource = _instanceItems;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ActNumberBox.Text))
            {
                MessageBox.Show("Введите номер акта");
                return;
            }
            if (string.IsNullOrWhiteSpace(ReasonBox.Text))
            {
                MessageBox.Show("Укажите причину списания");
                return;
            }
            if (!WriteOffDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату списания");
                return;
            }

            var selectedInstanceIds = _instanceItems.Where(i => i.IsSelected).Select(i => i.InstanceId).ToList();
            if (selectedInstanceIds.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один экземпляр");
                return;
            }

            try
            {
                var dto = new WriteOffActUpdateDto
                {
                    Id = _actId,
                    ActNumber = ActNumberBox.Text.Trim(),
                    WriteOffDate = WriteOffDatePicker.SelectedDate.Value,
                    Reason = ReasonBox.Text.Trim(),
                    InstanceIds = selectedInstanceIds
                };

                _writeOffService.UpdateAct(dto);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
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