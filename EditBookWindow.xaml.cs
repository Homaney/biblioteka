using biblioteka.DAO;
using biblioteka.DTO;
using biblioteka.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace biblioteka
{
    public partial class EditBookWindow : Window
    {
        private readonly int _bookId;
        private readonly BookService _bookService;
        private readonly UDKService _udkService;
        private readonly AuthorService _authorService;

        private List<UDKDto> _udkList;
        private List<string> _authorsList;
        private List<string> _selectedAuthors = new List<string>();
        private int _selectedUdkId = -1;
        private int _availableInstancesCount = 0;
        private BookDto _currentBookDto;

        public event EventHandler BookUpdated;

        public EditBookWindow(int id)
        {
            InitializeComponent();
            _bookId = id;
            _bookService = new BookService();
            _udkService = new UDKService();
            _authorService = new AuthorService();

            SelectedAuthorsList.ItemsSource = _selectedAuthors;
            Loaded += (s, e) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Загружаем списки
                _authorsList = _authorService.GetAll().Select(a => a.FullName).ToList();
                _udkList = _udkService.GetAll();

                // Загружаем книгу
                _currentBookDto = _bookService.GetBookById(_bookId);
                if (_currentBookDto == null)
                {
                    MessageBox.Show("Книга не найдена!");
                    Close();
                    return;
                }

                // Заполняем поля
                TitleBox.Text = _currentBookDto.Title;
                YearBox.Text = _currentBookDto.Year.ToString();
                DescriptionBox.Text = _currentBookDto.Description;
                PriceBox.Text = _currentBookDto.Price.ToString("F2");

                // Авторы
                if (!string.IsNullOrEmpty(_currentBookDto.Authors))
                {
                    _selectedAuthors.Clear();
                    foreach (string author in _currentBookDto.Authors.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        _selectedAuthors.Add(author.Trim());
                    }
                }

                // УДК
                _selectedUdkId = _currentBookDto.UdkId;
                var udk = _udkList.FirstOrDefault(u => u.Id == _selectedUdkId);
                if (udk != null)
                    UDKTextBox.Text = udk.Code;

                _availableInstancesCount = _currentBookDto.AvailableInstances;
                InstancesCountText.Text = $"Доступно экземпляров: {_availableInstancesCount}";
                IdentifierLabel.Text = $"ID: {_bookId}";
                SelectedAuthorsList.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void AuthorsTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var contextMenu = new ContextMenu();
            foreach (var author in _authorsList)
            {
                var menuItem = new MenuItem { Header = author };
                menuItem.Click += (s, args) => AuthorsTextBox.Text = author;
                contextMenu.Items.Add(menuItem);
            }
            contextMenu.PlacementTarget = AuthorsTextBox;
            contextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void UDKTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var contextMenu = new ContextMenu();
            foreach (var udk in _udkList)
            {
                var menuItem = new MenuItem
                {
                    Header = udk.Code,
                    Tag = udk.Id
                };
                menuItem.Click += (s, args) =>
                {
                    UDKTextBox.Text = udk.Code;
                    _selectedUdkId = udk.Id;
                };
                contextMenu.Items.Add(menuItem);
            }
            contextMenu.PlacementTarget = UDKTextBox;
            contextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void AddAuthor_Click(object sender, RoutedEventArgs e)
        {
            string author = AuthorsTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(author) && author != "Выберите автора...")
            {
                if (!_selectedAuthors.Contains(author))
                {
                    _selectedAuthors.Add(author);
                    SelectedAuthorsList.Items.Refresh();
                }
                AuthorsTextBox.Text = "Выберите автора...";
            }
        }

        private void RemoveAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string author)
            {
                _selectedAuthors.Remove(author);
                SelectedAuthorsList.Items.Refresh();
            }
        }

        private void AddInstance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _bookService.AddInstance(_bookId);
                _currentBookDto = _bookService.GetBookById(_bookId);
                _availableInstancesCount = _currentBookDto.AvailableInstances;
                InstancesCountText.Text = $"Доступно экземпляров: {_availableInstancesCount}";
                MessageBox.Show("Экземпляр добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveInstance_Click(object sender, RoutedEventArgs e)
        {
            if (_availableInstancesCount == 0)
            {
                MessageBox.Show("Нет доступных экземпляров", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Удалить один экземпляр?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                _bookService.RemoveAvailableInstance(_bookId);
                _currentBookDto = _bookService.GetBookById(_bookId);
                _availableInstancesCount = _currentBookDto.AvailableInstances;
                InstancesCountText.Text = $"Доступно экземпляров: {_availableInstancesCount}";
                MessageBox.Show("Экземпляр удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PriceBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0) && e.Text != ".")
                e.Handled = true;
            if (e.Text == "." && ((TextBox)sender).Text.Contains("."))
                e.Handled = true;
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                var updateDto = new BookUpdateDto
                {
                    Title = TitleBox.Text.Trim(),
                    Year = int.Parse(YearBox.Text),
                    UdkId = _selectedUdkId,
                    Description = DescriptionBox.Text.Trim(),
                    Price = decimal.Parse(PriceBox.Text),
                    Authors = _selectedAuthors
                };

                _bookService.UpdateBook(_bookId, updateDto);

                MessageBox.Show("Изменения сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                BookUpdated?.Invoke(this, EventArgs.Empty);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(TitleBox.Text))
            {
                MessageBox.Show("Введите название!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleBox.Focus();
                return false;
            }
            if (_selectedAuthors.Count == 0)
            {
                MessageBox.Show("Добавьте автора!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (_selectedUdkId == -1)
            {
                MessageBox.Show("Выберите УДК!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!int.TryParse(YearBox.Text, out int year) || year < 1000 || year > DateTime.Now.Year + 5)
            {
                MessageBox.Show("Введите корректный год!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                YearBox.Focus();
                return false;
            }
            if (!decimal.TryParse(PriceBox.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("Введите корректную цену!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PriceBox.Focus();
                return false;
            }
            return true;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !textBox.IsReadOnly)
                textBox.CaretIndex = textBox.Text.Length;
        }
    }
}