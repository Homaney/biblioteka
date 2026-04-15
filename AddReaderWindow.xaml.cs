using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using biblioteka.DTO;
using biblioteka.Services;

namespace biblioteka
{
	public partial class AddReaderWindow : Window
	{
		private readonly ReaderService _service;

		public AddReaderWindow()
		{
			InitializeComponent();
			_service = new ReaderService();

			RegistrationDatePicker.SelectedDate = DateTime.Today;
			BirthDatePicker.SelectedDate = DateTime.Today.AddYears(-14);
			BirthDatePicker.DisplayDateStart = new DateTime(1900, 1, 1);
			BirthDatePicker.DisplayDateEnd = DateTime.Today.AddYears(-14);
		}

		private void PhoneBox_GotFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(PhoneBox.Text))
			{
				PhoneBox.Text = "+375";
				PhoneBox.SelectionStart = PhoneBox.Text.Length;
			}
		}

		private void PhoneBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			if (!char.IsDigit(e.Text, 0) && e.Text != "+" && e.Text != "(" && e.Text != ")" && e.Text != "-" && e.Text != " ")
			{
				e.Handled = true;
			}
		}

		private void AddReader_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var dto = new ReaderCreateDto
				{
					FullName = FullNameBox.Text.Trim(),
					Phone = PhoneBox.Text.Trim(),
					Address = AddressBox.Text.Trim(),
					BirthDate = BirthDatePicker.SelectedDate ?? DateTime.MinValue,
					RegistrationDate = RegistrationDatePicker.SelectedDate ?? DateTime.MinValue
				};

				_service.Add(dto);

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