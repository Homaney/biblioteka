using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using biblioteka.Entities;

namespace biblioteka.DAO
{
    public class BookDAO
    {
        public List<BookEntity> GetAll()
        {
            var books = new Dictionary<int, BookEntity>();
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT b.ID, b.Title, b.Yearr, b.UDK_ID, b.Description, 
                           b.TotalCopies, b.AvailableForSale,
                           a.ID AS AuthorID, a.FullName
                    FROM Books b
                    LEFT JOIN BookAuthors ba ON b.ID = ba.BookID
                    LEFT JOIN Authors a ON ba.AuthorID = a.ID
                    ORDER BY b.ID";

                using (var cmd = new SqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int bookId = reader.GetInt32(0);

                        if (!books.ContainsKey(bookId))
                        {
                            books[bookId] = new BookEntity
                            {
                                ID = bookId,
                                Title = reader.GetString(1),
                                Yearr = reader.GetInt32(2),
                                UDK_ID = reader.GetInt32(3),
                                Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                                TotalCopies = reader.GetInt32(5),
                                AvailableForSale = reader.GetInt32(6)
                            };
                        }
                    }
                }
            }
            return new List<BookEntity>(books.Values);
        }

        public BookEntity GetById(int id)
        {
            BookEntity book = null;
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT b.ID, b.Title, b.Yearr, b.UDK_ID, b.Description, 
                           b.TotalCopies, b.AvailableForSale,
                           a.ID AS AuthorID, a.FullName
                    FROM Books b
                    LEFT JOIN BookAuthors ba ON b.ID = ba.BookID
                    LEFT JOIN Authors a ON ba.AuthorID = a.ID
                    WHERE b.ID = @ID";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (book == null)
                            {
                                book = new BookEntity
                                {
                                    ID = reader.GetInt32(0),
                                    Title = reader.GetString(1),
                                    Yearr = reader.GetInt32(2),
                                    UDK_ID = reader.GetInt32(3),
                                    Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    TotalCopies = reader.GetInt32(5),
                                    AvailableForSale = reader.GetInt32(6)
                                };
                            }
                        }
                    }
                }
            }
            return book;
        }

        public List<AuthorEntity> GetAuthorsByBookId(int bookId)
        {
            var authors = new List<AuthorEntity>();
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT a.ID, a.FullName
                    FROM Authors a
                    INNER JOIN BookAuthors ba ON a.ID = ba.AuthorID
                    WHERE ba.BookID = @BookID
                    ORDER BY a.FullName";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@BookID", bookId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            authors.Add(new AuthorEntity
                            {
                                ID = reader.GetInt32(0),
                                FullName = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return authors;
        }

        public int Insert(BookEntity book, List<AuthorEntity> authors)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Вставляем книгу
                        string bookQuery = @"
                            INSERT INTO Books (Title, Yearr, UDK_ID, Description)
                            VALUES (@Title, @Yearr, @UDK_ID, @Description);
                            SELECT SCOPE_IDENTITY();";

                        int bookId;
                        using (var cmd = new SqlCommand(bookQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Title", book.Title);
                            cmd.Parameters.AddWithValue("@Yearr", book.Yearr);
                            cmd.Parameters.AddWithValue("@UDK_ID", book.UDK_ID);
                            cmd.Parameters.AddWithValue("@Description", (object)book.Description ?? DBNull.Value);
                            bookId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2. Обрабатываем авторов
                        foreach (var author in authors)
                        {
                            int authorId = GetOrCreateAuthor(author.FullName, connection, transaction);

                            // Вставляем связь
                            string linkQuery = "INSERT INTO BookAuthors (BookID, AuthorID) VALUES (@BookID, @AuthorID)";
                            using (var cmd = new SqlCommand(linkQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@BookID", bookId);
                                cmd.Parameters.AddWithValue("@AuthorID", authorId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return bookId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void Update(BookEntity book, List<AuthorEntity> authors)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Обновляем книгу
                        string bookQuery = @"
                            UPDATE Books 
                            SET Title = @Title, Yearr = @Yearr, UDK_ID = @UDK_ID, Description = @Description
                            WHERE ID = @ID";

                        using (var cmd = new SqlCommand(bookQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ID", book.ID);
                            cmd.Parameters.AddWithValue("@Title", book.Title);
                            cmd.Parameters.AddWithValue("@Yearr", book.Yearr);
                            cmd.Parameters.AddWithValue("@UDK_ID", book.UDK_ID);
                            cmd.Parameters.AddWithValue("@Description", (object)book.Description ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        // 2. Удаляем старые связи
                        string deleteLinksQuery = "DELETE FROM BookAuthors WHERE BookID = @BookID";
                        using (var cmd = new SqlCommand(deleteLinksQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@BookID", book.ID);
                            cmd.ExecuteNonQuery();
                        }

                        // 3. Добавляем новые связи
                        foreach (var author in authors)
                        {
                            int authorId = GetOrCreateAuthor(author.FullName, connection, transaction);

                            string linkQuery = "INSERT INTO BookAuthors (BookID, AuthorID) VALUES (@BookID, @AuthorID)";
                            using (var cmd = new SqlCommand(linkQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@BookID", book.ID);
                                cmd.Parameters.AddWithValue("@AuthorID", authorId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void Delete(int id)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                // BookInstances и BookAuthors удалятся каскадно
                string query = "DELETE FROM Books WHERE ID = @ID";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private int GetOrCreateAuthor(string fullName, SqlConnection connection, SqlTransaction transaction)
        {
            // Проверяем существование
            string checkQuery = "SELECT ID FROM Authors WHERE FullName = @FullName";
            using (var cmd = new SqlCommand(checkQuery, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@FullName", fullName);
                object result = cmd.ExecuteScalar();
                if (result != null)
                    return Convert.ToInt32(result);
            }

            // Создаём нового
            string insertQuery = "INSERT INTO Authors (FullName) VALUES (@FullName); SELECT SCOPE_IDENTITY();";
            using (var cmd = new SqlCommand(insertQuery, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@FullName", fullName);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}