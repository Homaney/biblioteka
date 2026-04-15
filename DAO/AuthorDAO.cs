using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using biblioteka.Entities;

namespace biblioteka.DAO
{
	public class AuthorDAO
	{
		public List<AuthorEntity> GetAll()
		{
			var list = new List<AuthorEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "SELECT ID, FullName FROM Authors ORDER BY FullName";
				using (var cmd = new SqlCommand(query, connection))
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						list.Add(new AuthorEntity
						{
							ID = reader.GetInt32(0),
							FullName = reader.GetString(1)
						});
					}
				}
			}
			return list;
		}

		public AuthorEntity GetById(int id)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "SELECT ID, FullName FROM Authors WHERE ID = @ID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ID", id);
					using (var reader = cmd.ExecuteReader())
					{
						if (reader.Read())
						{
							return new AuthorEntity
							{
								ID = reader.GetInt32(0),
								FullName = reader.GetString(1)
							};
						}
						return null;
					}
				}
			}
		}

		public void Insert(AuthorEntity author)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "INSERT INTO Authors (FullName) VALUES (@FullName)";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@FullName", author.FullName);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void Delete(int id)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "DELETE FROM Authors WHERE ID = @ID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ID", id);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public bool IsUsedInBooks(int id)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT COUNT(*) FROM Books 
                    WHERE Authors LIKE '%' + (SELECT FullName FROM Authors WHERE ID = @ID) + '%'";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ID", id);
					int count = (int)cmd.ExecuteScalar();
					return count > 0;
				}
			}
		}
	}
}