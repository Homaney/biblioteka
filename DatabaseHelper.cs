using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;

namespace biblioteka
{
    public static class DatabaseHelper
    {
        private static string _connectionString;

        public static string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    _connectionString = @"Data Source=DESKTOP-NM03K47;Initial Catalog=biblioteka;Integrated Security=True;TrustServerCertificate=True";

                    if (string.IsNullOrEmpty(_connectionString))
                    {
                        // Пробуем разные варианты подключения
                        string[] possibleConnections = new string[]
                        {
                            @"Data Source=.\SQLEXPRESS;Initial Catalog=biblioteka;Integrated Security=True;TrustServerCertificate=True",
                            @"Data Source=localhost\SQLEXPRESS;Initial Catalog=biblioteka;Integrated Security=True;TrustServerCertificate=True",
                            @"Data Source=(local)\SQLEXPRESS;Initial Catalog=biblioteka;Integrated Security=True;TrustServerCertificate=True",
                            @"Data Source=.;Initial Catalog=biblioteka;Integrated Security=True;TrustServerCertificate=True"
                        };

                        _connectionString = possibleConnections[0];
                    }
                }
                return _connectionString;
            }
        }

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public static bool TestConnection()
        {
            try
            {
                using (var connection = GetConnection())
                {
                    connection.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к БД: {ex.Message}\n\n" +
                               $"Строка подключения: {ConnectionString}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}