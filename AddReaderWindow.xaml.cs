using System;
using System.Data;
using System.Data.OleDb;
using System.Windows;

namespace biblioteka
{
    public partial class AddReaderWindow : Window
    {
        private OleDbConnection connection;

        // Конструктор с параметром
        public AddReaderWindow(OleDbConnection conn)
        {
            InitializeComponent();
            connection = conn;
        }

        // Конструктор без параметров (если нужен)
        public AddReaderWindow()
        {
            InitializeComponent();
            // Инициализация соединения если нужно
        }

        private void AddReader_Click(object sender, RoutedEventArgs e)
        {
            string fullName = FullNameBox.Text.Trim();
            string phone = PhoneBox.Text.Trim();

            if (string.IsNullOrEmpty(fullName))
            {
                MessageBox.Show("Введите ФИО!");
                return;
            }

            try
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                using (OleDbCommand cmd = new OleDbCommand("INSERT INTO Readers (FullName, Phone) VALUES (?, ?)", connection))
                {
                    cmd.Parameters.Add("?", OleDbType.VarChar).Value = fullName;
                    cmd.Parameters.Add("?", OleDbType.VarChar).Value = string.IsNullOrEmpty(phone) ? DBNull.Value : (object)phone;
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Читатель добавлен!");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }
    }
}