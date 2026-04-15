using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using biblioteka.DTO;
using biblioteka.Services;

namespace biblioteka
{
	public partial class ReadersInfoWindow : Window
	{
		private readonly ReaderService _readerService;
		private readonly IssueService _issueService;
		private int currentReaderId;
		private bool isHistoryView = false;

		public ReadersInfoWindow()
		{
			InitializeComponent();
			_readerService = new ReaderService();
			_issueService = new IssueService();
			LoadReaders();
		}

		private void LoadReaders()
		{
			try
			{
				var readers = _readerService.GetAll();
				ReadersList.ItemsSource = readers;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при загрузке читателей: " + ex.Message);
			}
		}

		private void ReadersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ReadersList.SelectedItem is ReaderDto selected)
			{
				currentReaderId = selected.Id;
				FullNameText.Text = selected.FullName;
				PhoneText.Text = selected.Phone ?? "не указан";
				AddressText.Text = selected.Address ?? "не указан";
				BirthDateText.Text = selected.BirthDate.ToShortDateString();
				RegistrationDateText.Text = selected.RegistrationDate.ToShortDateString();

				if (isHistoryView)
					LoadHistoryBooks(currentReaderId);
				else
					LoadCurrentBooks(currentReaderId);

				ReaderCard.Visibility = Visibility.Visible;
				DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
				ReaderCard.BeginAnimation(OpacityProperty, fadeIn);
				SelectHint.Visibility = Visibility.Collapsed;
				PrintButton.Visibility = Visibility.Visible;
			}
			else
			{
				PrintButton.Visibility = Visibility.Collapsed;
			}
		}

		private void LoadCurrentBooks(int readerId)
		{
			try
			{
				var issues = _issueService.GetReaderActiveIssues(readerId);
				var books = new List<dynamic>();

				foreach (var issue in issues)
				{
					bool overdue = issue.PlannedReturnDate < DateTime.Now;
					bool warning = (issue.PlannedReturnDate - DateTime.Now).TotalDays <= 3 &&
								  (issue.PlannedReturnDate - DateTime.Now).TotalDays > 0;

					string statusText = overdue ? "ПРОСРОЧЕНА" : (warning ? "СКОРО ВОЗВРАТ" : "В СРОК");
					SolidColorBrush statusColor = overdue ? new SolidColorBrush(Colors.Red) :
												  (warning ? new SolidColorBrush(Colors.Yellow) : new SolidColorBrush(Colors.Green));

					books.Add(new
					{
						IssuedId = issue.Id,
						BookTitle = issue.BookTitle,
						InventoryNumber = "Инв. №: " + issue.InventoryNumber,
						Status = $"Выдано: {issue.IssueDate:dd.MM.yyyy} • Возврат: {issue.PlannedReturnDate:dd.MM.yyyy} • {statusText}",
						StatusColor = statusColor,
						CardColor = overdue ? new SolidColorBrush(Color.FromArgb(30, 255, 118, 117)) :
								   (warning ? new SolidColorBrush(Color.FromArgb(30, 253, 203, 110)) :
											  new SolidColorBrush(Color.FromArgb(30, 0, 184, 148)))
					});
				}

				CurrentBooksList.ItemsSource = books;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при загрузке текущих книг: " + ex.Message);
			}
		}

		private void LoadHistoryBooks(int readerId)
		{
			try
			{
				var issues = _issueService.GetReaderHistory(readerId);
				var books = new List<dynamic>();

				foreach (var issue in issues)
				{
					if (!issue.ActualReturnDate.HasValue) continue;

					DateTime actual = issue.ActualReturnDate.Value;
					TimeSpan difference = actual - issue.PlannedReturnDate;
					int daysDifference = (int)difference.TotalDays;
					bool returnedOnTime = daysDifference <= 0;

					string statusText, returnInfo;
					SolidColorBrush statusColor;

					if (returnedOnTime)
					{
						statusText = daysDifference < 0 ? "ВОЗВРАЩЕНА ДОСРОЧНО" : "ВОЗВРАЩЕНА ВОВРЕМЯ";
						returnInfo = daysDifference < 0 ?
							$"Возвращена: {actual:dd.MM.yyyy} (досрочно на {-daysDifference} дн.)" :
							$"Возвращена: {actual:dd.MM.yyyy} (в срок)";
						statusColor = new SolidColorBrush(Colors.Green);
					}
					else
					{
						statusText = "ВОЗВРАЩЕНА С ОПОЗДАНИЕМ";
						returnInfo = $"Возвращена: {actual:dd.MM.yyyy} (опоздание: {daysDifference} дн.)";
						statusColor = new SolidColorBrush(Colors.Orange);
					}

					books.Add(new
					{
						IssuedId = issue.Id,
						BookTitle = issue.BookTitle,
						InventoryNumber = "Инв. №: " + issue.InventoryNumber,
						Status = $"Выдано: {issue.IssueDate:dd.MM.yyyy} • План возврата: {issue.PlannedReturnDate:dd.MM.yyyy} • {statusText}",
						StatusColor = statusColor,
						ReturnInfo = returnInfo
					});
				}

				HistoryBooksList.ItemsSource = books;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при загрузке истории книг: " + ex.Message);
			}
		}

		private void ReturnBook_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button btn && btn.Tag is int issuedId)
			{
				var confirm = MessageBox.Show("Вы уверены, что хотите вернуть книгу?", "Подтверждение возврата",
					MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (confirm != MessageBoxResult.Yes) return;

				try
				{
					_issueService.ReturnBook(issuedId);

					MessageBox.Show("Книга успешно возвращена!", "Возврат книги", MessageBoxButton.OK, MessageBoxImage.Information);

					if (isHistoryView)
						LoadHistoryBooks(currentReaderId);
					else
						LoadCurrentBooks(currentReaderId);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка при возврате книги: " + ex.Message, "Ошибка",
						MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void SwitchToCurrentBooks(object sender, RoutedEventArgs e)
		{
			isHistoryView = false;

			CurrentBooksTab.Background = new SolidColorBrush(Color.FromRgb(108, 92, 231));
			CurrentBooksTab.Foreground = Brushes.White;
			HistoryTab.Background = new SolidColorBrush(Color.FromRgb(51, 51, 51));
			HistoryTab.Foreground = new SolidColorBrush(Color.FromRgb(176, 176, 176));

			HistoryBooksList.ItemsSource = null;
			CurrentBooksList.Visibility = Visibility.Visible;
			HistoryBooksList.Visibility = Visibility.Collapsed;

			if (currentReaderId > 0)
				LoadCurrentBooks(currentReaderId);
		}

		private void SwitchToHistory(object sender, RoutedEventArgs e)
		{
			isHistoryView = true;

			HistoryTab.Background = new SolidColorBrush(Color.FromRgb(108, 92, 231));
			HistoryTab.Foreground = Brushes.White;
			CurrentBooksTab.Background = new SolidColorBrush(Color.FromRgb(51, 51, 51));
			CurrentBooksTab.Foreground = new SolidColorBrush(Color.FromRgb(176, 176, 176));

			CurrentBooksList.ItemsSource = null;
			HistoryBooksList.Visibility = Visibility.Visible;
			CurrentBooksList.Visibility = Visibility.Collapsed;

			if (currentReaderId > 0)
				LoadHistoryBooks(currentReaderId);
		}

		private void AddReader_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var addReaderWindow = new AddReaderWindow();
				addReaderWindow.Owner = this;
				if (addReaderWindow.ShowDialog() == true)
				{
					LoadReaders();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при добавлении читателя: {ex.Message}", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void PrintButton_Click(object sender, RoutedEventArgs e)
		{
			if (ReadersList.SelectedItem == null)
			{
				MessageBox.Show("Выберите читателя для печати", "Печать",
					MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			try
			{
				PrintDialog printDialog = new PrintDialog();
				if (printDialog.ShowDialog() == true)
				{
					var printVisual = CreatePrintVisual();
					printDialog.PrintVisual(printVisual, $"Карточка читателя - {FullNameText.Text}");

					MessageBox.Show("Карточка отправлена на печать!", "Печать",
						MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при печати: {ex.Message}", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private FrameworkElement CreatePrintVisual()
		{
			var printContainer = new StackPanel
			{
				Background = Brushes.White,
				Margin = new Thickness(50)
			};

			printContainer.Children.Add(new TextBlock
			{
				Text = "КАРТОЧКА ЧИТАТЕЛЯ БИБЛИОТЕКИ",
				FontSize = 18,
				FontWeight = FontWeights.Bold,
				TextAlignment = TextAlignment.Center,
				Margin = new Thickness(0, 0, 0, 20),
				Foreground = Brushes.Black
			});

			var infoGrid = new Grid();
			infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
			infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
			infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			infoGrid.Margin = new Thickness(0, 0, 0, 20);

			AddPrintRow(infoGrid, 0, "ФИО:", FullNameText.Text);
			AddPrintRow(infoGrid, 1, "Телефон:", PhoneText.Text);
			AddPrintRow(infoGrid, 2, "Дата рождения:", BirthDateText.Text);
			AddPrintRow(infoGrid, 3, "Дата регистрации:", RegistrationDateText.Text);
			AddPrintRow(infoGrid, 4, "Адрес:", AddressText.Text);

			printContainer.Children.Add(infoGrid);

			if (CurrentBooksList.Items.Count > 0)
			{
				printContainer.Children.Add(new TextBlock
				{
					Text = "ТЕКУЩИЕ КНИГИ НА РУКАХ:",
					FontSize = 14,
					FontWeight = FontWeights.Bold,
					Margin = new Thickness(0, 0, 0, 10),
					Foreground = Brushes.Black
				});

				foreach (dynamic book in CurrentBooksList.Items)
				{
					var bookPanel = new StackPanel
					{
						Margin = new Thickness(0, 0, 0, 8),
						Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0))
					};

					bookPanel.Children.Add(new TextBlock
					{
						Text = book.BookTitle,
						FontWeight = FontWeights.SemiBold,
						FontSize = 12,
						Foreground = Brushes.Black,
						TextWrapping = TextWrapping.Wrap
					});

					bookPanel.Children.Add(new TextBlock
					{
						Text = book.InventoryNumber + " • " + book.Status,
						FontSize = 11,
						Foreground = Brushes.DarkGray,
						Margin = new Thickness(0, 2, 0, 0)
					});

					printContainer.Children.Add(bookPanel);
				}
			}

			var signaturePanel = new StackPanel { Margin = new Thickness(0, 30, 0, 0) };

			signaturePanel.Children.Add(new TextBlock
			{
				Text = $"Дата печати: {DateTime.Now:dd.MM.yyyy HH:mm}",
				FontSize = 10,
				Foreground = Brushes.Gray,
				FontStyle = FontStyles.Italic
			});

			signaturePanel.Children.Add(new TextBlock
			{
				Text = "_________________________________",
				FontSize = 10,
				Foreground = Brushes.Black,
				Margin = new Thickness(0, 20, 0, 0)
			});

			signaturePanel.Children.Add(new TextBlock
			{
				Text = "Подпись библиотекаря",
				FontSize = 10,
				Foreground = Brushes.Black,
				FontStyle = FontStyles.Italic
			});

			printContainer.Children.Add(signaturePanel);

			return new ScrollViewer { Content = printContainer, Padding = new Thickness(10) };
		}

		private void AddPrintRow(Grid grid, int row, string label, string value)
		{
			var labelText = new TextBlock
			{
				Text = label,
				FontWeight = FontWeights.Bold,
				FontSize = 12,
				Foreground = Brushes.Black,
				Margin = new Thickness(0, 0, 10, 5)
			};

			var valueText = new TextBlock
			{
				Text = value,
				FontSize = 12,
				Foreground = Brushes.Black,
				Margin = new Thickness(0, 0, 0, 5),
				TextWrapping = TextWrapping.Wrap
			};

			Grid.SetRow(labelText, row);
			Grid.SetColumn(labelText, 0);
			Grid.SetRow(valueText, row);
			Grid.SetColumn(valueText, 1);

			grid.Children.Add(labelText);
			grid.Children.Add(valueText);
		}
	}
}