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
			var list = new List<BookEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT ID, Title, Yearr, UDK_ID, Description, Price, 
                           TotalCopies, AvailableForSale, Authors
                    FROM Books 
                    ORDER BY ID";
				using (var cmd = new SqlCommand(query, connection))
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						list.Add(new BookEntity
						{
							ID = reader.GetInt32(0),
							Title = reader.GetString(1),
							Yearr = reader.GetInt32(2),
							UDK_ID = reader.GetInt32(3),
							Description = reader.IsDBNull(4) ? null : reader.GetString(4),
							Price = reader.GetDecimal(5),
							TotalCopies = reader.GetInt32(6),
							AvailableForSale = reader.GetInt32(7),
							Authors = reader.IsDBNull(8) ? null : reader.GetString(8)
						});
					}
				}
			}
			return list;
		}

		public BookEntity GetById(int id)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT ID, Title, Yearr, UDK_ID, Description, Price, 
                           TotalCopies, AvailableForSale, Authors
                    FROM Books 
                    WHERE ID = @ID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ID", id);
					using (var reader = cmd.ExecuteReader())
					{
						if (reader.Read())
						{
							return new BookEntity
							{
								ID = reader.GetInt32(0),
								Title = reader.GetString(1),
								Yearr = reader.GetInt32(2),
								UDK_ID = reader.GetInt32(3),
								Description = reader.IsDBNull(4) ? null : reader.GetString(4),
								Price = reader.GetDecimal(5),
								TotalCopies = reader.GetInt32(6),
								AvailableForSale = reader.GetInt32(7),
								Authors = reader.IsDBNull(8) ? null : reader.GetString(8)
							};
						}
						return null;
					}
				}
			}
		}

		public int Insert(BookEntity book)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    INSERT INTO Books (Title, Yearr, UDK_ID, Description, Price, Authors)
                    VALUES (@Title, @Yearr, @UDK_ID, @Description, @Price, @Authors);
                    SELECT SCOPE_IDENTITY();";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@Title", book.Title);
					cmd.Parameters.AddWithValue("@Yearr", book.Yearr);
					cmd.Parameters.AddWithValue("@UDK_ID", book.UDK_ID);
					cmd.Parameters.AddWithValue("@Description", (object)book.Description ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Price", book.Price);
					cmd.Parameters.AddWithValue("@Authors", (object)book.Authors ?? DBNull.Value);
					return Convert.ToInt32(cmd.ExecuteScalar());
				}
			}
		}

		public void Update(BookEntity book)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    UPDATE Books 
                    SET Title = @Title, Yearr = @Yearr, UDK_ID = @UDK_ID, 
                        Description = @Description, Price = @Price, Authors = @Authors
                    WHERE ID = @ID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ID", book.ID);
					cmd.Parameters.AddWithValue("@Title", book.Title);
					cmd.Parameters.AddWithValue("@Yearr", book.Yearr);
					cmd.Parameters.AddWithValue("@UDK_ID", book.UDK_ID);
					cmd.Parameters.AddWithValue("@Description", (object)book.Description ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Price", book.Price);
					cmd.Parameters.AddWithValue("@Authors", (object)book.Authors ?? DBNull.Value);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void Delete(int id)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				// Сначала удаляем экземпляры
				string deleteInstances = "DELETE FROM BookInstances WHERE BookID = @BookID";
				using (var cmd = new SqlCommand(deleteInstances, connection))
				{
					cmd.Parameters.AddWithValue("@BookID", id);
					cmd.ExecuteNonQuery();
				}
				// Потом книгу
				string deleteBook = "DELETE FROM Books WHERE ID = @ID";
				using (var cmd = new SqlCommand(deleteBook, connection))
				{
					cmd.Parameters.AddWithValue("@ID", id);
					cmd.ExecuteNonQuery();
				}
			}
		}
	}
}