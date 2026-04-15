using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using biblioteka.DTO;
using biblioteka.Services;

namespace biblioteka
{
	public partial class AddBookWindow : Window
	{
		private readonly BookService _bookService;
		private readonly UDKService _udkService;
		private readonly AuthorService _authorService;

		private List<UDKDto> _udkList;
		private List<AuthorDto> _authorsList;
		private List<string> _selectedAuthors = new List<string>();
		private int _selectedUdkId = -1;

		public event EventHandler BookAdded;

		public AddBookWindow()
		{
			InitializeComponent();
			_bookService = new BookService();
			_udkService = new UDKService();
			_authorService = new AuthorService();

			QuantityBox.Text = "1";
			LoadAuthors();
			LoadUDKs();
			SelectedAuthorsList.ItemsSource = _selectedAuthors;
			InitializePlaceholders();
			AuthorInputBox.KeyDown += AuthorInputBox_KeyDown;
		}

		private void InitializePlaceholders()
		{
			AddPlaceholderBehavior(IdentifierBox);
			AddPlaceholderBehavior(TitleBox);
			AddPlaceholderBehavior(YearBox);
			AddPlaceholderBehavior(DescriptionBox);
			AddPlaceholderBehavior(AuthorInputBox);
			AddPlaceholderBehavior(UDKDisplayBox);
		}

		private void AddPlaceholderBehavior(TextBox textBox)
		{
			if (textBox == null) return;
			textBox.GotFocus += (s, e) => UpdatePlaceholderVisibility(textBox);
			textBox.LostFocus += (s, e) => UpdatePlaceholderVisibility(textBox);
			textBox.TextChanged += (s, e) => UpdatePlaceholderVisibility(textBox);
			UpdatePlaceholderVisibility(textBox);
		}

		private void UpdatePlaceholderVisibility(TextBox textBox)
		{
			if (textBox == null) return;
			var template = textBox.Template;
			if (template == null) return;
			var placeholder = template.FindName("placeholderText", textBox) as TextBlock;
			if (placeholder == null) return;
			if (textBox.IsReadOnly)
			{
				placeholder.Visibility = Visibility.Collapsed;
				return;
			}
			bool shouldShow = string.IsNullOrEmpty(textBox.Text) && !textBox.IsFocused;
			placeholder.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
		}

		private void AuthorInputBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				AddAuthorFromInput();
				e.Handled = true;
			}
		}

		private void AddAuthorFromInput()
		{
			string authorToAdd = AuthorInputBox.Text.Trim();
			if (string.IsNullOrWhiteSpace(authorToAdd))
			{
				MessageBox.Show("Введите имя автора");
				AuthorInputBox.Focus();
				return;
			}

			if (!_authorsList.Any(a => a.FullName.Equals(authorToAdd, StringComparison.OrdinalIgnoreCase)))
			{
				try
				{
					_authorService.Add(new AuthorCreateDto { FullName = authorToAdd });
					LoadAuthors(); // обновляем список
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка при добавлении автора: " + ex.Message);
					return;
				}
			}

			if (!_selectedAuthors.Contains(authorToAdd))
			{
				_selectedAuthors.Add(authorToAdd);
				SelectedAuthorsList.Items.Refresh();
			}

			AuthorInputBox.Text = "";
			AuthorsPopup.IsOpen = false;
		}

		private void LoadAuthors()
		{
			try
			{
				_authorsList = _authorService.GetAll();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка загрузки авторов: " + ex.Message);
			}
		}

		private void LoadUDKs()
		{
			try
			{
				_udkList = _udkService.GetAll();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка загрузки УДК: " + ex.Message);
			}
		}

		private void AuthorDropdown_Click(object sender, RoutedEventArgs e)
		{
			if (_authorsList.Count == 0)
			{
				MessageBox.Show("Список авторов пуст. Сначала добавьте авторов.", "Информация",
					MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			AuthorsListBox.ItemsSource = null;
			AuthorsListBox.ItemsSource = _authorsList.Select(a => a.FullName).ToList();
			AuthorsPopup.IsOpen = true;
			AuthorsPopup.StaysOpen = false;
		}

		private void AuthorItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (sender is FrameworkElement element && element.DataContext is string selectedAuthor)
			{
				AuthorInputBox.Text = selectedAuthor;
				AuthorsPopup.IsOpen = false;
				e.Handled = true;
			}
		}

		private void UDKDropdown_Click(object sender, RoutedEventArgs e)
		{
			if (_udkList.Count == 0)
			{
				MessageBox.Show("Список УДК пуст. Сначала добавьте УДК.", "Информация",
					MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			UDKListBox.ItemsSource = null;
			UDKListBox.ItemsSource = _udkList;
			UDKPopup.IsOpen = true;
			UDKPopup.StaysOpen = false;
		}

		private void UDKItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (sender is FrameworkElement element && element.DataContext is UDKDto selectedUDK)
			{
				_selectedUdkId = selectedUDK.Id;
				UDKDisplayBox.Text = selectedUDK.Code;
				UDKPopup.IsOpen = false;
				e.Handled = true;
			}
		}

		private void AddAuthor_Click(object sender, RoutedEventArgs e)
		{
			AddAuthorFromInput();
		}

		private void RemoveAuthor_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button btn && btn.Tag is string author)
			{
				_selectedAuthors.Remove(author);
				SelectedAuthorsList.Items.Refresh();
			}
		}

		private void AddUDKButton_Click(object sender, RoutedEventArgs e)
		{
			var addUDKWindow = new AddUDKWindow();
			addUDKWindow.Owner = this;
			if (addUDKWindow.ShowDialog() == true)
			{
				LoadUDKs();
			}
		}

		private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
		{
			if (int.TryParse(QuantityBox.Text, out int quantity))
			{
				quantity++;
				QuantityBox.Text = quantity.ToString();
			}
		}

		private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
		{
			if (int.TryParse(QuantityBox.Text, out int quantity) && quantity > 1)
			{
				quantity--;
				QuantityBox.Text = quantity.ToString();
			}
		}

		private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			if (!char.IsDigit(e.Text, 0))
				e.Handled = true;
		}

		private void AddBook_Click(object sender, RoutedEventArgs e)
		{
			string title = TitleBox.Text.Trim();
			string year = YearBox.Text.Trim();
			string description = DescriptionBox.Text.Trim();

			if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity < 1)
			{
				MessageBox.Show("Введите корректное количество экземпляров");
				QuantityBox.Focus();
				return;
			}

			if (string.IsNullOrEmpty(title))
			{
				MessageBox.Show("Введите название книги");
				TitleBox.Focus();
				return;
			}

			if (_selectedAuthors.Count == 0)
			{
				MessageBox.Show("Добавьте хотя бы одного автора");
				return;
			}

			if (_selectedUdkId == -1)
			{
				MessageBox.Show("Выберите УДК из списка");
				return;
			}

			if (string.IsNullOrEmpty(year) || !int.TryParse(year, out int yearValue))
			{
				MessageBox.Show("Введите корректный год издания");
				YearBox.Focus();
				return;
			}

			if (yearValue < 1000 || yearValue > DateTime.Now.Year + 5)
			{
				MessageBox.Show($"Год должен быть от 1000 до {DateTime.Now.Year + 5}");
				YearBox.Focus();
				return;
			}

			try
			{
				var dto = new BookCreateDto
				{
					Title = title,
					Year = yearValue,
					UdkId = _selectedUdkId,
					Description = description,
					Price = 0,
					Authors = _selectedAuthors,
					Quantity = quantity
				};
				_bookService.AddBook(dto);
				MessageBox.Show("Книга успешно добавлена!");
				BookAdded?.Invoke(this, EventArgs.Empty);
				ClearForm();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ClearForm()
		{
			IdentifierBox.Text = "";
			TitleBox.Text = "";
			YearBox.Text = "";
			DescriptionBox.Text = "";
			_selectedAuthors.Clear();
			AuthorInputBox.Text = "";
			UDKDisplayBox.Text = "";
			_selectedUdkId = -1;
			QuantityBox.Text = "1";
			SelectedAuthorsList.Items.Refresh();

			UpdatePlaceholderVisibility(IdentifierBox);
			UpdatePlaceholderVisibility(TitleBox);
			UpdatePlaceholderVisibility(YearBox);
			UpdatePlaceholderVisibility(DescriptionBox);
			UpdatePlaceholderVisibility(AuthorInputBox);
			UpdatePlaceholderVisibility(UDKDisplayBox);
		}
	}
}