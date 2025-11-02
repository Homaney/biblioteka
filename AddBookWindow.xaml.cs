using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Windows;

namespace biblioteka
{
    public partial class AddBookWindow : Window
    {
        private OleDbConnection connection;
        private List<string> selectedAuthors = new List<string>();
        private List<string> existingAuthors = new List<string>();

        public AddBookWindow(OleDbConnection conn)
        {
            InitializeComponent();
            connection = conn;
            LoadAuthors();
        }

        private void LoadAuthors()
        {
            try
            {
                connection.Open();
                OleDbCommand cmd = new OleDbCommand("SELECT FullName FROM Authors ORDER BY FullName", connection);
                OleDbDataReader reader = cmd.ExecuteReader();
                existingAuthors.Clear();
                while (reader.Read())
                {
                    existingAuthors.Add(reader.GetString(0));
                }
                AuthorBox.ItemsSource = existingAuthors;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке авторов: " + ex.Message);
            }
            finally
            {
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

        private void AddBook_Click(object sender, RoutedEventArgs e)
        {
            string title = TitleBox.Text.Trim();
            string genre = GenreBox.Text.Trim();
            string year = YearBox.Text.Trim();
            string description = DescriptionBox.Text.Trim();

            if (string.IsNullOrEmpty(title) || selectedAuthors.Count == 0 || string.IsNullOrEmpty(genre) || string.IsNullOrEmpty(year))
            {
                MessageBox.Show("Заполните все поля кроме описания!");
                return;
            }

            try
            {
                connection.Open();

                // 1. Добавляем книгу
                OleDbCommand addBookCmd = new OleDbCommand("INSERT INTO Books (Title, Yearr, Genre, Description) VALUES (?, ?, ?, ?)", connection);
                addBookCmd.Parameters.AddWithValue("?", title);
                addBookCmd.Parameters.AddWithValue("?", year);
                addBookCmd.Parameters.AddWithValue("?", genre);
                addBookCmd.Parameters.AddWithValue("?", description);
                addBookCmd.ExecuteNonQuery();

                OleDbCommand getBookIdCmd = new OleDbCommand("SELECT @@IDENTITY", connection);
                int bookId = Convert.ToInt32(getBookIdCmd.ExecuteScalar());

                // 2. Добавляем авторов и связи
                foreach (var authorName in selectedAuthors)
                {
                    OleDbCommand findAuthorCmd = new OleDbCommand("SELECT ID FROM Authors WHERE FullName = ?", connection);
                    findAuthorCmd.Parameters.AddWithValue("?", authorName);
                    object authorIdObj = findAuthorCmd.ExecuteScalar();

                    int authorId;
                    if (authorIdObj == null)
                    {
                        OleDbCommand addAuthorCmd = new OleDbCommand("INSERT INTO Authors (FullName) VALUES (?)", connection);
                        addAuthorCmd.Parameters.AddWithValue("?", authorName);
                        addAuthorCmd.ExecuteNonQuery();

                        OleDbCommand getAuthorIdCmd = new OleDbCommand("SELECT @@IDENTITY", connection);
                        authorId = Convert.ToInt32(getAuthorIdCmd.ExecuteScalar());
                    }
                    else
                    {
                        authorId = Convert.ToInt32(authorIdObj);
                    }

                    OleDbCommand linkCmd = new OleDbCommand("INSERT INTO BookAuthors (BookID, AuthorID) VALUES (?, ?)", connection);
                    linkCmd.Parameters.AddWithValue("?", bookId);
                    linkCmd.Parameters.AddWithValue("?", authorId);
                    linkCmd.ExecuteNonQuery();
                }

                MessageBox.Show("Книга успешно добавлена!");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении книги: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
