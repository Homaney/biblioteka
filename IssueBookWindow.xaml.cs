using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using biblioteka.DTO;
using biblioteka.Services;

namespace biblioteka
{
    public partial class IssueBookWindow : Window
    {
        private readonly BookService _bookService;
        private readonly ReaderService _readerService;
        private readonly IssueService _issueService;
        private readonly BookInstanceService _instanceService;
        private List<BookDto> _books;

        public IssueBookWindow()
        {
            InitializeComponent();
            _bookService = new BookService();
            _readerService = new ReaderService();
            _issueService = new IssueService();
            _instanceService = new BookInstanceService();

            IssueDatePicker.SelectedDate = DateTime.Today;
            IssueDatePicker.DisplayDateEnd = DateTime.Today;
            PlannedReturnDatePicker.SelectedDate = DateTime.Today.AddDays(14);

            LoadBooks();
            LoadReaders();
        }

        private void LoadBooks()
        {
            try
            {
                _books = _bookService.GetAllBooks().Where(b => b.AvailableInstances > 0).ToList();
                BookComboBox.ItemsSource = _books;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки книг: " + ex.Message);
            }
        }

        private void LoadReaders()
        {
            try
            {
                var readers = _readerService.GetAll();
                ReaderComboBox.ItemsSource = readers;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки читателей: " + ex.Message);
            }
        }

        private void BookComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BookComboBox.SelectedItem is BookDto selectedBook)
            {
                LoadAvailableInstances(selectedBook.Id);
            }
        }

        private void LoadAvailableInstances(int bookId)
        {
            try
            {
                var instances = _instanceService.GetAvailableByBookId(bookId);
                InstanceListBox.ItemsSource = instances;
                InstanceListBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки экземпляров: " + ex.Message);
            }
        }

        private void AddReader_Click(object sender, RoutedEventArgs e)
        {
            var addReaderWindow = new AddReaderWindow();
            if (addReaderWindow.ShowDialog() == true)
            {
                LoadReaders();
            }
        }

        private void IssueButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(BookComboBox.SelectedItem is BookDto selectedBook))
                {
                    MessageBox.Show("Выберите книгу!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!(ReaderComboBox.SelectedItem is ReaderDto selectedReader))
                {
                    MessageBox.Show("Выберите читателя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (InstanceListBox.SelectedValue == null)
                {
                    MessageBox.Show("Выберите экземпляр!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!IssueDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите дату выдачи!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!PlannedReturnDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите плановую дату возврата!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var issueDto = new IssueCreateDto
                {
                    InstanceId = (int)InstanceListBox.SelectedValue,
                    ReaderId = selectedReader.Id,
                    IssueDate = IssueDatePicker.SelectedDate.Value,
                    PlannedReturnDate = PlannedReturnDatePicker.SelectedDate.Value
                };

                _issueService.IssueBook(issueDto);

                MessageBox.Show("Книга успешно выдана!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}