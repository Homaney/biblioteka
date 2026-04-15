using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using biblioteka.Entities;

namespace biblioteka.DAO
{
	public class ReaderDAO
	{
		public List<ReaderEntity> GetAll()
		{
			var list = new List<ReaderEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "SELECT ID, FullName, Phone, Address, BirthDate, RegistrationDate FROM Readers ORDER BY FullName";
				using (var cmd = new SqlCommand(query, connection))
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						list.Add(MapReaderToEntity(reader));
					}
				}
			}
			return list;
		}

		public ReaderEntity GetById(int id)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "SELECT ID, FullName, Phone, Address, BirthDate, RegistrationDate FROM Readers WHERE ID = @ID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ID", id);
					using (var reader = cmd.ExecuteReader())
					{
						if (reader.Read())
						{
							return MapReaderToEntity(reader);
						}
						return null;
					}
				}
			}
		}

		public List<ReaderEntity> Search(string searchText)
		{
			var list = new List<ReaderEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT ID, FullName, Phone, Address, BirthDate, RegistrationDate 
                    FROM Readers 
                    WHERE FullName LIKE @SearchText OR Phone LIKE @SearchText
                    ORDER BY FullName";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							list.Add(MapReaderToEntity(reader));
						}
					}
				}
			}
			return list;
		}

		public int Insert(ReaderEntity reader)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    INSERT INTO Readers (FullName, Phone, Address, BirthDate, RegistrationDate)
                    VALUES (@FullName, @Phone, @Address, @BirthDate, @RegistrationDate);
                    SELECT SCOPE_IDENTITY();";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@FullName", reader.FullName);
					cmd.Parameters.AddWithValue("@Phone", (object)reader.Phone ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Address", (object)reader.Address ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@BirthDate", reader.BirthDate);
					cmd.Parameters.AddWithValue("@RegistrationDate", reader.RegistrationDate);
					return Convert.ToInt32(cmd.ExecuteScalar());
				}
			}
		}

		public void Update(ReaderEntity reader)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    UPDATE Readers 
                    SET FullName = @FullName, 
                        Phone = @Phone, 
                        Address = @Address, 
                        BirthDate = @BirthDate, 
                        RegistrationDate = @RegistrationDate
                    WHERE ID = @ID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ID", reader.ID);
					cmd.Parameters.AddWithValue("@FullName", reader.FullName);
					cmd.Parameters.AddWithValue("@Phone", (object)reader.Phone ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Address", (object)reader.Address ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@BirthDate", reader.BirthDate);
					cmd.Parameters.AddWithValue("@RegistrationDate", reader.RegistrationDate);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void Delete(int id)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "DELETE FROM Readers WHERE ID = @ID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ID", id);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public bool HasActiveIssues(int readerId)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "SELECT COUNT(*) FROM IssuedBooks WHERE ReaderID = @ReaderID AND Status = N'Выдана'";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ReaderID", readerId);
					int count = (int)cmd.ExecuteScalar();
					return count > 0;
				}
			}
		}

		public bool HasSales(int readerId)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "SELECT COUNT(*) FROM Sales WHERE ReaderID = @ReaderID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ReaderID", readerId);
					int count = (int)cmd.ExecuteScalar();
					return count > 0;
				}
			}
		}

		public bool IsPhoneExists(string phone, int? excludeId = null)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "SELECT COUNT(*) FROM Readers WHERE Phone = @Phone";
				if (excludeId.HasValue)
					query += " AND ID != @ExcludeId";

				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@Phone", phone);
					if (excludeId.HasValue)
						cmd.Parameters.AddWithValue("@ExcludeId", excludeId.Value);

					int count = (int)cmd.ExecuteScalar();
					return count > 0;
				}
			}
		}

		public int GetTotalCount()
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "SELECT COUNT(*) FROM Readers";
				using (var cmd = new SqlCommand(query, connection))
				{
					return (int)cmd.ExecuteScalar();
				}
			}
		}

		private ReaderEntity MapReaderToEntity(SqlDataReader reader)
		{
			return new ReaderEntity
			{
				ID = reader.GetInt32(0),
				FullName = reader.GetString(1),
				Phone = reader.IsDBNull(2) ? null : reader.GetString(2),
				Address = reader.IsDBNull(3) ? null : reader.GetString(3),
				BirthDate = reader.GetDateTime(4),
				RegistrationDate = reader.GetDateTime(5)
			};
		}
	}
}