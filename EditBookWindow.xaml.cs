using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using biblioteka.DTO;
using biblioteka.Services;

namespace biblioteka
{
    public partial class EditBookWindow : Window
    {
        private readonly int _bookId;
        private readonly BookService _bookService;
        private readonly UDKService _udkService;
        private readonly AuthorService _authorService;

        private List<UDKDto> _udkList;
        private List<AuthorDto> _authorsList;
        private List<AuthorDto> _selectedAuthors = new List<AuthorDto>();
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
                _authorsList = _authorService.GetAll();
                _udkList = _udkService.GetAll();

                _currentBookDto = _bookService.GetBookById(_bookId);
                if (_currentBookDto == null)
                {
                    MessageBox.Show("Книга не найдена!");
                    Close();
                    return;
                }

                TitleBox.Text = _currentBookDto.Title;
                YearBox.Text = _currentBookDto.Year.ToString();
                DescriptionBox.Text = _currentBookDto.Description;

                _selectedAuthors.Clear();
                foreach (var author in _currentBookDto.Authors)
                {
                    _selectedAuthors.Add(author);
                }

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
                var menuItem = new MenuItem { Header = author.FullName, Tag = author };
                menuItem.Click += (s, args) =>
                {
                    if (s is MenuItem mi && mi.Tag is AuthorDto selectedAuthor)
                    {
                        AuthorsTextBox.Text = selectedAuthor.FullName;
                    }
                };
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
                    if (s is MenuItem mi)
                    {
                        UDKTextBox.Text = udk.Code;
                        _selectedUdkId = udk.Id;
                    }
                };
                contextMenu.Items.Add(menuItem);
            }
            contextMenu.PlacementTarget = UDKTextBox;
            contextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void AddAuthor_Click(object sender, RoutedEventArgs e)
        {
            string authorName = AuthorsTextBox.Text.Trim();
            if (string.IsNullOrEmpty(authorName) || authorName == "Выберите автора...")
                return;

            var author = _authorsList.FirstOrDefault(a => a.FullName.Equals(authorName, StringComparison.OrdinalIgnoreCase));
            if (author == null)
            {
                author = new AuthorDto { FullName = authorName };
            }

            if (!_selectedAuthors.Any(a => a.FullName.Equals(author.FullName, StringComparison.OrdinalIgnoreCase)))
            {
                _selectedAuthors.Add(author);
                SelectedAuthorsList.Items.Refresh();
            }
            AuthorsTextBox.Text = "Выберите автора...";
        }

        private void RemoveAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AuthorDto author)
            {
                _selectedAuthors.Remove(author);
                SelectedAuthorsList.Items.Refresh();
            }
        }

        private void AddInstance_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddInstanceDialog(_bookId);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                _currentBookDto = _bookService.GetBookById(_bookId);
                _availableInstancesCount = _currentBookDto.AvailableInstances;
                InstancesCountText.Text = $"Доступно экземпляров: {_availableInstancesCount}";
                BookUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        private void RemoveInstance_Click(object sender, RoutedEventArgs e)
        {
            if (_availableInstancesCount == 0)
            {
                MessageBox.Show("Нет доступных экземпляров", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Удалить один доступный экземпляр?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                _bookService.RemoveAvailableInstance(_bookId);
                _currentBookDto = _bookService.GetBookById(_bookId);
                _availableInstancesCount = _currentBookDto.AvailableInstances;
                InstancesCountText.Text = $"Доступно экземпляров: {_availableInstancesCount}";
                BookUpdated?.Invoke(this, EventArgs.Empty);
                MessageBox.Show("Экземпляр удалён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    Authors = _selectedAuthors.ToList()
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
            return true;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !textBox.IsReadOnly)
                textBox.CaretIndex = textBox.Text.Length;
        }
    }
}