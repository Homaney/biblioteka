using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows;
using System.Windows.Controls;
namespace biblioteka
{
    public partial class EditBookWindow : Window
    {
        private OleDbConnection connection;
        private int bookId;
        private List<string> selectedAuthors = new List<string>();
        private List<string> existingAuthors = new List<string>();
        public EditBookWindow(OleDbConnection conn, int id)
        {
            InitializeComponent();
            connection = conn;
            bookId = id;
            IdentifierLabel.Text = $"ID: {bookId}";
            LoadAuthors();
            LoadBookData();
        }
        private void LoadAuthors()
        {
            try
            {
                connection.Open();
                using (OleDbCommand cmd = new OleDbCommand("SELECT FullName FROM Authors ORDER BY FullName", connection))
                using (OleDbDataReader reader = cmd.ExecuteReader())
                {
                    existingAuthors.Clear();
                    while (reader.Read())
                    {
                        existingAuthors.Add(reader.GetString(0));
                    }
                }
                AuthorBox.ItemsSource = existingAuthors;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки авторов: " + ex.Message);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }
        private void LoadBookData()
        {
            try
            {
                connection.Open();
                // Загружаем данные книги
                using (OleDbCommand cmd = new OleDbCommand(
                    "SELECT Title, Yearr, Razdel, Description, Quantity FROM Books WHERE Identifier = ?", connection))
                {
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = bookId;
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            TitleBox.Text = reader["Title"].ToString();
                            YearBox.Text = reader["Yearr"].ToString();
                            RazdelBox.Text = reader["Razdel"].ToString();
                            DescriptionBox.Text = reader["Description"].ToString();
                            QuantityBox.Text = reader["Quantity"].ToString();
                        }
                    }
                }
                // Загружаем авторов книги
                selectedAuthors.Clear();
                using (OleDbCommand cmd = new OleDbCommand(
                    @"SELECT Authors.FullName
              FROM Authors INNER JOIN BookAuthors ON Authors.ID = BookAuthors.AuthorID
              WHERE BookAuthors.BookID = ?", connection))
                {
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = bookId;
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            selectedAuthors.Add(reader.GetString(0));
                        }
                    }
                }
                // ОБНОВЛЯЕМ ВСЁ
                SelectedAuthorsList.ItemsSource = null;
                SelectedAuthorsList.ItemsSource = selectedAuthors;
                AuthorBox.ItemsSource = existingAuthors;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }
        private void AddAuthor_Click(object sender, RoutedEventArgs e)
        {
            string author = AuthorBox.Text.Trim();
            if (!string.IsNullOrEmpty(author) && !selectedAuthors.Contains(author))
            {
                selectedAuthors.Add(author);
                SelectedAuthorsList.ItemsSource = null;
                SelectedAuthorsList.ItemsSource = selectedAuthors;
                AuthorBox.Text = "";
            }
        }
        private void RemoveAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string authorName)
            {
                selectedAuthors.Remove(authorName);
                SelectedAuthorsList.ItemsSource = null;
                SelectedAuthorsList.ItemsSource = selectedAuthors;
            }
        }
        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            string title = TitleBox.Text.Trim();
            string razdel = RazdelBox.Text.Trim();
            string yearStr = YearBox.Text.Trim();
            string description = DescriptionBox.Text.Trim();
            string quantityStr = QuantityBox.Text.Trim();
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(razdel) || string.IsNullOrEmpty(yearStr) || string.IsNullOrEmpty(quantityStr))
            {
                MessageBox.Show("Заполните все поля!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(yearStr, out int year) || !int.TryParse(quantityStr, out int quantity))
            {
                MessageBox.Show("Год и количество должны быть числами!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                connection.Open();
                // Обновляем книгу
                using (OleDbCommand updateBook = new OleDbCommand(
                    "UPDATE Books SET Title=?, Yearr=?, Razdel=?, Description=?, Quantity=? WHERE Identifier=?", connection))
                {
                    updateBook.Parameters.Add("?", OleDbType.VarChar).Value = title;
                    updateBook.Parameters.Add("?", OleDbType.Integer).Value = year;
                    updateBook.Parameters.Add("?", OleDbType.VarChar).Value = razdel;
                    updateBook.Parameters.Add("?", OleDbType.VarChar).Value = description;
                    updateBook.Parameters.Add("?", OleDbType.Integer).Value = quantity;
                    updateBook.Parameters.Add("?", OleDbType.Integer).Value = bookId;
                    updateBook.ExecuteNonQuery();
                }
                // Удаляем старые связи
                using (OleDbCommand deleteLinks = new OleDbCommand("DELETE FROM BookAuthors WHERE BookID = ?", connection))
                {
                    deleteLinks.Parameters.Add("?", OleDbType.Integer).Value = bookId;
                    deleteLinks.ExecuteNonQuery();
                }
                // Добавляем новые
                foreach (var authorName in selectedAuthors)
                {
                    int authorId;
                    using (OleDbCommand findAuthor = new OleDbCommand("SELECT ID FROM Authors WHERE FullName = ?", connection))
                    {
                        findAuthor.Parameters.Add("?", OleDbType.VarChar).Value = authorName;
                        object authorIdObj = findAuthor.ExecuteScalar();
                        if (authorIdObj == null)
                        {
                            using (OleDbCommand addAuthor = new OleDbCommand("INSERT INTO Authors (FullName) VALUES (?)", connection))
                            {
                                addAuthor.Parameters.Add("?", OleDbType.VarChar).Value = authorName;
                                addAuthor.ExecuteNonQuery();
                            }
                            using (OleDbCommand getId = new OleDbCommand("SELECT @@IDENTITY", connection))
                            {
                                authorId = Convert.ToInt32(getId.ExecuteScalar());
                            }
                        }
                        else
                        {
                            authorId = Convert.ToInt32(authorIdObj);
                        }
                    }
                    using (OleDbCommand link = new OleDbCommand("INSERT INTO BookAuthors (BookID, AuthorID) VALUES (?, ?)", connection))
                    {
                        link.Parameters.Add("?", OleDbType.Integer).Value = bookId;
                        link.Parameters.Add("?", OleDbType.Integer).Value = authorId;
                        link.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Изменения сохранены!", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }
    }
}