using System;
using System.Data;
using System.Data.OleDb;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Windows.Controls.Primitives;

namespace biblioteka
{
    public partial class AddReaderWindow : Window
    {
        private OleDbConnection connection;

        public AddReaderWindow(OleDbConnection conn)
        {
            InitializeComponent();
            connection = conn;

            // Устанавливаем текущую дату по умолчанию
            BirthDatePicker.SelectedDate = DateTime.Today.AddYears(-18);
            RegistrationDatePicker.SelectedDate = DateTime.Today;

            // Применяем темный стиль к DatePicker после загрузки
            Loaded += (s, e) => ApplyDarkStyleToDatePickers();
        }

        public AddReaderWindow()
        {
            InitializeComponent();
            BirthDatePicker.SelectedDate = DateTime.Today.AddYears(-18);
            RegistrationDatePicker.SelectedDate = DateTime.Today;
            Loaded += (s, e) => ApplyDarkStyleToDatePickers();
        }

        private void ApplyDarkStyleToDatePickers()
        {
            ApplyDarkStyleToDatePicker(BirthDatePicker);
            ApplyDarkStyleToDatePicker(RegistrationDatePicker);
        }

        private void ApplyDarkStyleToDatePicker(DatePicker datePicker)
        {
            datePicker.Loaded += (s, e) =>
            {
                // Простая стилизация - устанавливаем цвет текста
                var textBox = datePicker.Template.FindName("PART_TextBox", datePicker) as DatePickerTextBox;
                if (textBox != null)
                {
                    textBox.Foreground = System.Windows.Media.Brushes.White;
                    textBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 42, 42));
                    textBox.BorderThickness = new Thickness(0);
                }
            };
        }

        // Обработчики для телефона
        private void PhoneBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // Подсказка при фокусе (можно убрать, если не нужно)
            if (string.IsNullOrEmpty(PhoneBox.Text))
            {
                PhoneBox.Text = "+375";
                PhoneBox.SelectionStart = PhoneBox.Text.Length;
            }
        }

        private void PhoneBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры, плюс, скобки, дефисы и пробелы
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

            // Проверка всех обязательных полей
            if (string.IsNullOrEmpty(fullName))
            {
                ShowValidationError("Введите ФИО читателя!", FullNameBox);
                return;
            }

            if (string.IsNullOrEmpty(phone) || phone == "+375")
            {
                ShowValidationError("Введите телефон читателя!", PhoneBox);
                return;
            }

            // Проверка что телефон содержит только разрешенные символы
            if (!IsValidPhoneFormat(phone))
            {
                ShowValidationError("Некорректный формат телефона! Разрешены только цифры, +, (), - и пробелы", PhoneBox);
                return;
            }

            // Проверка минимальной длины телефона
            string digitsOnly = Regex.Replace(phone, @"[^\d]", "");
            if (digitsOnly.Length < 7)
            {
                ShowValidationError("Телефон слишком короткий! Должно быть не менее 7 цифр", PhoneBox);
                return;
            }

            if (digitsOnly.Length > 15)
            {
                ShowValidationError("Телефон слишком длинный! Максимум 15 цифр", PhoneBox);
                return;
            }

            if (string.IsNullOrEmpty(address))
            {
                ShowValidationError("Введите адрес читателя!", AddressBox);
                return;
            }

            if (!BirthDatePicker.SelectedDate.HasValue)
            {
                ShowValidationError("Выберите дату рождения!", BirthDatePicker);
                return;
            }

            if (!RegistrationDatePicker.SelectedDate.HasValue)
            {
                ShowValidationError("Выберите дату регистрации!", RegistrationDatePicker);
                return;
            }

            // Проверка что дата рождения не в будущем
            if (BirthDatePicker.SelectedDate.Value > DateTime.Today)
            {
                ShowValidationError("Дата рождения не может быть в будущем!", BirthDatePicker);
                return;
            }

            // Проверка что дата регистрации не в будущем
            if (RegistrationDatePicker.SelectedDate.Value > DateTime.Today)
            {
                ShowValidationError("Дата регистрации не может быть в будущем!", RegistrationDatePicker);
                return;
            }

            // Проверка что дата рождения раньше даты регистрации
            if (BirthDatePicker.SelectedDate.Value >= RegistrationDatePicker.SelectedDate.Value)
            {
                ShowValidationError("Дата рождения должна быть раньше даты регистрации!", BirthDatePicker);
                return;
            }

            try
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                string query = @"
                    INSERT INTO Readers (FullName, Phone, Address, BirthDate, RegistrationDate) 
                    VALUES (?, ?, ?, ?, ?)";

                using (OleDbCommand cmd = new OleDbCommand(query, connection))
                {
                    cmd.Parameters.Add("?", OleDbType.VarChar).Value = fullName;
                    cmd.Parameters.Add("?", OleDbType.VarChar).Value = phone;
                    cmd.Parameters.Add("?", OleDbType.VarChar).Value = address;
                    cmd.Parameters.Add("?", OleDbType.Date).Value = BirthDatePicker.SelectedDate.Value;
                    cmd.Parameters.Add("?", OleDbType.Date).Value = RegistrationDatePicker.SelectedDate.Value;

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("✅ Читатель успешно добавлен!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка при добавлении читателя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        private bool IsValidPhoneFormat(string phone)
        {
            // Проверяем что телефон содержит только разрешенные символы
            return Regex.IsMatch(phone, @"^[\d\+\-\(\)\s]+$");
        }

        private void ShowValidationError(string message, Control control)
        {
            MessageBox.Show(message, "Ошибка валидации",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            control.Focus();
        }
    }
}