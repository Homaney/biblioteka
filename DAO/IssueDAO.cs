using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using biblioteka.Entities;

namespace biblioteka.DAO
{
	public class IssueDAO
	{
		public List<IssuedBookEntity> GetAll()
		{
			var list = new List<IssuedBookEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT ID, InstanceID, ReaderID, IssueDate, PlannedReturnDate, 
                           ActualReturnDate, Status, ReturnedOnTime
                    FROM IssuedBooks
                    ORDER BY IssueDate DESC";
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

		public List<IssuedBookEntity> GetActiveIssues()
		{
			var list = new List<IssuedBookEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT ID, InstanceID, ReaderID, IssueDate, PlannedReturnDate, 
                           ActualReturnDate, Status, ReturnedOnTime
                    FROM IssuedBooks
                    WHERE Status = N'Выдана'
                    ORDER BY PlannedReturnDate";
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

		public List<IssuedBookEntity> GetOverdueIssues()
		{
			var list = new List<IssuedBookEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT ID, InstanceID, ReaderID, IssueDate, PlannedReturnDate, 
                           ActualReturnDate, Status, ReturnedOnTime
                    FROM IssuedBooks
                    WHERE Status = N'Выдана' AND PlannedReturnDate < GETDATE()
                    ORDER BY PlannedReturnDate";
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

		public List<IssuedBookEntity> GetByReaderId(int readerId, bool activeOnly = true)
		{
			var list = new List<IssuedBookEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT ID, InstanceID, ReaderID, IssueDate, PlannedReturnDate, 
                           ActualReturnDate, Status, ReturnedOnTime
                    FROM IssuedBooks
                    WHERE ReaderID = @ReaderID";
				if (activeOnly)
					query += " AND Status = N'Выдана'";
				query += " ORDER BY IssueDate DESC";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ReaderID", readerId);
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

		public List<IssuedBookEntity> GetHistoryByReaderId(int readerId)
		{
			var list = new List<IssuedBookEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT ID, InstanceID, ReaderID, IssueDate, PlannedReturnDate, 
                           ActualReturnDate, Status, ReturnedOnTime
                    FROM IssuedBooks
                    WHERE ReaderID = @ReaderID AND Status = N'Возвращена'
                    ORDER BY ActualReturnDate DESC";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ReaderID", readerId);
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

		public IssuedBookEntity GetById(int id)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT ID, InstanceID, ReaderID, IssueDate, PlannedReturnDate, 
                           ActualReturnDate, Status, ReturnedOnTime
                    FROM IssuedBooks
                    WHERE ID = @ID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ID", id);
					using (var reader = cmd.ExecuteReader())
					{
						if (reader.Read())
							return MapReaderToEntity(reader);
						return null;
					}
				}
			}
		}

		public void Insert(IssuedBookEntity issue)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    INSERT INTO IssuedBooks (InstanceID, ReaderID, IssueDate, PlannedReturnDate, Status)
                    VALUES (@InstanceID, @ReaderID, @IssueDate, @PlannedReturnDate, @Status)";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@InstanceID", issue.InstanceID);
					cmd.Parameters.AddWithValue("@ReaderID", issue.ReaderID);
					cmd.Parameters.AddWithValue("@IssueDate", issue.IssueDate);
					cmd.Parameters.AddWithValue("@PlannedReturnDate", issue.PlannedReturnDate);
					cmd.Parameters.AddWithValue("@Status", issue.Status);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void ReturnBook(int issueId, DateTime actualReturnDate, bool returnedOnTime)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    UPDATE IssuedBooks 
                    SET ActualReturnDate = @ActualReturnDate, 
                        Status = N'Возвращена', 
                        ReturnedOnTime = @ReturnedOnTime 
                    WHERE ID = @ID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ID", issueId);
					cmd.Parameters.AddWithValue("@ActualReturnDate", actualReturnDate);
					cmd.Parameters.AddWithValue("@ReturnedOnTime", returnedOnTime);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public int GetActiveIssuesCount()
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "SELECT COUNT(*) FROM IssuedBooks WHERE Status = N'Выдана'";
				using (var cmd = new SqlCommand(query, connection))
				{
					return (int)cmd.ExecuteScalar();
				}
			}
		}

		public int GetOverdueCount()
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT COUNT(*) FROM IssuedBooks 
                    WHERE Status = N'Выдана' AND PlannedReturnDate < GETDATE()";
				using (var cmd = new SqlCommand(query, connection))
				{
					return (int)cmd.ExecuteScalar();
				}
			}
		}

		private IssuedBookEntity MapReaderToEntity(SqlDataReader reader)
		{
			return new IssuedBookEntity
			{
				ID = reader.GetInt32(0),
				InstanceID = reader.GetInt32(1),
				ReaderID = reader.GetInt32(2),
				IssueDate = reader.GetDateTime(3),
				PlannedReturnDate = reader.GetDateTime(4),
				ActualReturnDate = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
				Status = reader.GetString(6),
				ReturnedOnTime = reader.GetBoolean(7)
			};
		}
	}
}