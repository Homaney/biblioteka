using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using biblioteka.DTO;
using biblioteka.Services;

namespace biblioteka
{
	public partial class MainWindow : Window
	{
		private readonly BookService _bookService;
		private readonly ReaderService _readerService;
		private readonly IssueService _issueService;
		private readonly SaleService _saleService;
		private List<BookDto> _allBooks;

		public MainWindow()
		{
			InitializeComponent();
			_bookService = new BookService();
			_readerService = new ReaderService();
			_issueService = new IssueService();
			_saleService = new SaleService();

			InitializeSearchPlaceholder();
			LoadBooks();
			LoadStatistics();
		}

		private void LoadBooks()
		{
			try
			{
				_allBooks = _bookService.GetAllBooks();
				BooksDataGrid.ItemsSource = _allBooks;
				UpdateStats();
				StatusTextBlock.Text = $"Загружено книг: {_allBooks.Count}";
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при загрузке книг: " + ex.Message, "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
				StatusTextBlock.Text = "Ошибка загрузки данных";
			}
		}

		private void LoadStatistics()
		{
			try
			{
				int totalBooks = _allBooks?.Count ?? 0;
				TotalBooksStat.Text = totalBooks.ToString();

				int issuedNow = _issueService.GetActiveIssuesCount();
				IssuedNowStat.Text = issuedNow.ToString();

				var report = _saleService.GetReport(DateTime.Today.AddMonths(-1), DateTime.Today);
				MonthlySalesStat.Text = $"{report.TotalAmount:N0} BYN";

				int readersCount = _readerService.GetAll().Count;
				ReadersCountStat.Text = readersCount.ToString();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Ошибка загрузки статистики: " + ex.Message);
			}
		}

		private void UpdateStats()
		{
			if (_allBooks == null) return;

			int totalBooks = _allBooks.Count;
			int availableBooks = _allBooks.Sum(b => b.AvailableInstances);

			StatsTextBlock.Text = $"Всего книг: {totalBooks} • Доступно экземпляров: {availableBooks}";
		}

		private void InitializeSearchPlaceholder()
		{
			SearchBox.Text = "";
			SearchBox.Foreground = new SolidColorBrush(Colors.White);
		}

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (_allBooks == null) return;

			string filterText = SearchBox.Text.Trim();
			UpdatePlaceholderVisibility();

			if (string.IsNullOrWhiteSpace(filterText))
			{
				BooksDataGrid.ItemsSource = _allBooks;
				UpdateStats();
				StatusTextBlock.Text = $"Загружено книг: {_allBooks.Count}";
				return;
			}

			var filteredBooks = _bookService.SearchBooks(filterText);
			BooksDataGrid.ItemsSource = filteredBooks;
			StatusTextBlock.Text = $"Найдено книг: {filteredBooks.Count}";
		}

		private void UpdatePlaceholderVisibility()
		{
			SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text)
				? Visibility.Visible
				: Visibility.Collapsed;
		}

		private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
		{
			UpdatePlaceholderVisibility();
		}

		private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
		{
			UpdatePlaceholderVisibility();
		}

		private void BooksDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (!(BooksDataGrid.SelectedItem is BookDto selectedBook)) return;

			if (selectedBook.Id <= 0)
			{
				MessageBox.Show("Некорректный идентификатор книги.", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			try
			{
				var editWindow = new EditBookWindow(selectedBook.Id);
				editWindow.Owner = this;
				editWindow.BookUpdated += (s, args) =>
				{
					LoadBooks();
					LoadStatistics();
				};
				editWindow.ShowDialog();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка открытия редактора: {ex.Message}", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void AddBook_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var addWindow = new AddBookWindow();
				addWindow.Owner = this;
				addWindow.BookAdded += (s, args) =>
				{
					LoadBooks();
					LoadStatistics();
				};
				addWindow.ShowDialog();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка открытия окна добавления: {ex.Message}", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void DeleteBook_Click(object sender, RoutedEventArgs e)
		{
			if (!(BooksDataGrid.SelectedItem is BookDto selectedBook))
			{
				MessageBox.Show("Выберите книгу для удаления.", "Внимание",
					MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			if (selectedBook.Id <= 0)
			{
				MessageBox.Show("Некорректный идентификатор книги.", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			var confirm = MessageBox.Show(
				$"Вы уверены, что хотите удалить книгу?\n\nID: {selectedBook.Id}\nНазвание: {selectedBook.Title}\n\nВсе экземпляры книги также будут удалены!",
				"⚠️ Подтверждение удаления",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning,
				MessageBoxResult.No);

			if (confirm != MessageBoxResult.Yes) return;

			try
			{
				_bookService.DeleteBook(selectedBook.Id);
				MessageBox.Show("Книга успешно удалена.", "Готово",
					MessageBoxButton.OK, MessageBoxImage.Information);

				LoadBooks();
				LoadStatistics();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void IssueBook_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var issueWindow = new IssueBookWindow();
				issueWindow.Owner = this;
				if (issueWindow.ShowDialog() == true)
				{
					LoadBooks();
					LoadStatistics();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка открытия окна выдачи: {ex.Message}", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void SellBook_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var sellWindow = new SellBookWindow();
				sellWindow.Owner = this;
				if (sellWindow.ShowDialog() == true)
				{
					LoadBooks();
					LoadStatistics();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка открытия окна продажи: {ex.Message}", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ViewReaders_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				new ReadersInfoWindow().ShowDialog();
				LoadStatistics();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка открытия списка читателей: {ex.Message}", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ViewUDK_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				new UDKWindow().ShowDialog();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка открытия справочника УДК: {ex.Message}", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void OverdueLoans_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var overdueWindow = new OverdueLoansWindow();
				overdueWindow.Owner = this;
				overdueWindow.ShowDialog();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка открытия окна должников: {ex.Message}", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void SalesReport_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var reportWindow = new SalesReportWindow();
				reportWindow.Owner = this;
				reportWindow.ShowDialog();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка открытия отчета по продажам: {ex.Message}", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}