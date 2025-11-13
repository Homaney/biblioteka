using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
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
                MessageBox.Show("Ошибка при загрузке авторов: " + ex.Message);
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

        private void AddBook_Click(object sender, RoutedEventArgs e)
        {
            string title = TitleBox.Text.Trim();
            string razdel = RazdelBox.Text.Trim();
            string yearStr = YearBox.Text.Trim();
            string description = DescriptionBox.Text.Trim();
            string identifierStr = IdentifierBox.Text.Trim(); // ← Твоё новое поле в XAML
            string quantityStr = QuantityBox.Text.Trim(); // ← Твоё новое поле в XAML

            if (string.IsNullOrEmpty(title) || selectedAuthors.Count == 0 || string.IsNullOrEmpty(razdel) || string.IsNullOrEmpty(yearStr) || string.IsNullOrEmpty(identifierStr) || string.IsNullOrEmpty(quantityStr))
            {
                MessageBox.Show("Заполните все поля!");
                return;
            }

            if (!int.TryParse(yearStr, out int year) || !int.TryParse(identifierStr, out int identifier) || !int.TryParse(quantityStr, out int quantity))
            {
                MessageBox.Show("ID, год и количество должны быть числами!");
                return;
            }

            try
            {
                connection.Open();

                using (OleDbCommand checkCmd = new OleDbCommand("SELECT COUNT(*) FROM Books WHERE Identifier = ?", connection))
                {
                    checkCmd.Parameters.Add("?", OleDbType.Integer).Value = identifier;
                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                    {
                        MessageBox.Show("Книга с таким ID уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                using (OleDbCommand addBookCmd = new OleDbCommand("INSERT INTO Books (Identifier, Title, Yearr, Razdel, Description, Quantity) VALUES (?, ?, ?, ?, ?, ?)", connection))
                {
                    addBookCmd.Parameters.Add("?", OleDbType.Integer).Value = identifier;
                    addBookCmd.Parameters.Add("?", OleDbType.VarChar).Value = title;
                    addBookCmd.Parameters.Add("?", OleDbType.Integer).Value = year;
                    addBookCmd.Parameters.Add("?", OleDbType.VarChar).Value = razdel;
                    addBookCmd.Parameters.Add("?", OleDbType.VarChar).Value = description;
                    addBookCmd.Parameters.Add("?", OleDbType.Integer).Value = quantity;
                    addBookCmd.ExecuteNonQuery();
                }

                int bookId = identifier;

                foreach (var authorName in selectedAuthors)
                {
                    int authorId;
                    using (OleDbCommand findAuthorCmd = new OleDbCommand("SELECT ID FROM Authors WHERE FullName = ?", connection))
                    {
                        findAuthorCmd.Parameters.Add("?", OleDbType.VarChar).Value = authorName;
                        object authorIdObj = findAuthorCmd.ExecuteScalar();

                        if (authorIdObj == null)
                        {
                            using (OleDbCommand addAuthorCmd = new OleDbCommand("INSERT INTO Authors (FullName) VALUES (?)", connection))
                            {
                                addAuthorCmd.Parameters.Add("?", OleDbType.VarChar).Value = authorName;
                                addAuthorCmd.ExecuteNonQuery();
                            }
                            using (OleDbCommand getIdCmd = new OleDbCommand("SELECT @@IDENTITY", connection))
                            {
                                authorId = Convert.ToInt32(getIdCmd.ExecuteScalar());
                            }
                        }
                        else
                        {
                            authorId = Convert.ToInt32(authorIdObj);
                        }
                    }

                    using (OleDbCommand linkCmd = new OleDbCommand("INSERT INTO BookAuthors (BookID, AuthorID) VALUES (?, ?)", connection))
                    {
                        linkCmd.Parameters.Add("?", OleDbType.Integer).Value = bookId;
                        linkCmd.Parameters.Add("?", OleDbType.Integer).Value = authorId;
                        linkCmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Книга успешно добавлена!");
                DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении книги: " + ex.Message);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }
    }
}