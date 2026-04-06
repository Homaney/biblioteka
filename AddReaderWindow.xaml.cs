using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace biblioteka
{
    public partial class AddReaderWindow : Window
    {
        private DateTime _minBirthDate; // минимальная разрешённая дата рождения (1900-01-01)
        private DateTime _maxBirthDate; // максимальная разрешённая дата рождения (сегодня - 14 лет)

        public AddReaderWindow()
        {
            InitializeComponent();

            // Дата регистрации – сегодня, будущие даты запрещены
            RegistrationDatePicker.SelectedDate = DateTime.Today;
            RegistrationDatePicker.DisplayDateEnd = DateTime.Today;

            // Вычисляем диапазон для даты рождения
            _minBirthDate = new DateTime(1900, 1, 1);
            _maxBirthDate = DateTime.Today.AddYears(-14); // например, 14 лет назад

            // Устанавливаем ограничения в календаре
            BirthDatePicker.DisplayDateStart = _minBirthDate;
            BirthDatePicker.DisplayDateEnd = _maxBirthDate;

            // Устанавливаем начальную дату – максимально допустимую (самую позднюю)
            BirthDatePicker.SelectedDate = _maxBirthDate;

            // Подписываемся на событие изменения даты (после установки начальной, чтобы не вызвать лишних сообщений)
            BirthDatePicker.SelectedDateChanged += BirthDatePicker_SelectedDateChanged;
        }

        private void BirthDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!BirthDatePicker.SelectedDate.HasValue)
                return;

            // Если выбранная дата позже максимальной (моложе 14 лет) – корректируем без сообщения
            if (BirthDatePicker.SelectedDate.Value > _maxBirthDate)
            {
                BirthDatePicker.SelectedDate = _maxBirthDate;
            }
            // Если выбранная дата раньше минимальной – корректируем
            else if (BirthDatePicker.SelectedDate.Value < _minBirthDate)
            {
                BirthDatePicker.SelectedDate = _minBirthDate;
            }
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
            string fullName = FullNameBox.Text.Trim();
            string phone = PhoneBox.Text.Trim();
            string address = AddressBox.Text.Trim();

            if (string.IsNullOrEmpty(fullName))
            {
                ShowError("Введите ФИО!", FullNameBox);
                return;
            }

            if (string.IsNullOrEmpty(phone) || phone == "+375")
            {
                ShowError("Введите телефон!", PhoneBox);
                return;
            }

            if (!Regex.IsMatch(phone, @"^[\d\+\-\(\)\s]+$"))
            {
                ShowError("Некорректный телефон!", PhoneBox);
                return;
            }

            string digitsOnly = Regex.Replace(phone, @"[^\d]", "");
            if (digitsOnly.Length < 7)
            {
                ShowError("Телефон слишком короткий!", PhoneBox);
                return;
            }

            if (digitsOnly.Length > 15)
            {
                ShowError("Телефон слишком длинный!", PhoneBox);
                return;
            }

            if (string.IsNullOrEmpty(address))
            {
                ShowError("Введите адрес!", AddressBox);
                return;
            }

            if (!BirthDatePicker.SelectedDate.HasValue)
            {
                ShowError("Выберите дату рождения!", BirthDatePicker);
                return;
            }

            if (!RegistrationDatePicker.SelectedDate.HasValue)
            {
                ShowError("Выберите дату регистрации!", RegistrationDatePicker);
                return;
            }

            // Финальная проверка (для надёжности)
            if (BirthDatePicker.SelectedDate.Value > _maxBirthDate)
            {
                ShowError($"Возраст читателя должен быть не младше 14 лет!\nМаксимальная дата рождения: {_maxBirthDate:dd.MM.yyyy}", BirthDatePicker);
                return;
            }

            if (BirthDatePicker.SelectedDate.Value < _minBirthDate)
            {
                ShowError($"Дата рождения не может быть раньше {_minBirthDate:dd.MM.yyyy}!", BirthDatePicker);
                return;
            }

            if (BirthDatePicker.SelectedDate.Value >= RegistrationDatePicker.SelectedDate.Value)
            {
                ShowError("Дата рождения должна быть раньше даты регистрации!", BirthDatePicker);
                return;
            }

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO Readers (FullName, Phone, Address, BirthDate, RegistrationDate) 
                        VALUES (@FullName, @Phone, @Address, @BirthDate, @RegistrationDate)";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@FullName", fullName);
                        cmd.Parameters.AddWithValue("@Phone", phone);
                        cmd.Parameters.AddWithValue("@Address", address);
                        cmd.Parameters.AddWithValue("@BirthDate", BirthDatePicker.SelectedDate.Value);
                        cmd.Parameters.AddWithValue("@RegistrationDate", RegistrationDatePicker.SelectedDate.Value);

                        cmd.ExecuteNonQuery();
                    }

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowError(string message, Control control)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            control.Focus();
        }
    }
}