using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using biblioteka.Entities;

namespace biblioteka.DAO
{
	public class BookInstanceDAO
	{
		public List<BookInstanceEntity> GetAll()
		{
			var list = new List<BookInstanceEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT ID, BookID, InventoryNumber, Status, AcquisitionDate, InvoiceNumber, CanBeSold
                    FROM BookInstances 
                    ORDER BY InventoryNumber";
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

		public BookInstanceEntity GetById(int id)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT ID, BookID, InventoryNumber, Status, AcquisitionDate, InvoiceNumber, CanBeSold
                    FROM BookInstances 
                    WHERE ID = @ID";
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

		public List<BookInstanceEntity> GetByBookId(int bookId)
		{
			var list = new List<BookInstanceEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT ID, BookID, InventoryNumber, Status, AcquisitionDate, InvoiceNumber, CanBeSold
                    FROM BookInstances 
                    WHERE BookID = @BookID
                    ORDER BY InventoryNumber";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@BookID", bookId);
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

		public List<BookInstanceEntity> GetAvailableByBookId(int bookId)
		{
			var list = new List<BookInstanceEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT ID, BookID, InventoryNumber, Status, AcquisitionDate, InvoiceNumber, CanBeSold
                    FROM BookInstances 
                    WHERE BookID = @BookID AND Status = N'Доступна'
                    ORDER BY InventoryNumber";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@BookID", bookId);
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

		public List<BookInstanceEntity> GetSellableByBookId(int bookId)
		{
			var list = new List<BookInstanceEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT ID, BookID, InventoryNumber, Status, AcquisitionDate, InvoiceNumber, CanBeSold
                    FROM BookInstances 
                    WHERE BookID = @BookID AND Status = N'Доступна' AND CanBeSold = 1
                    ORDER BY InventoryNumber";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@BookID", bookId);
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

		public void Insert(BookInstanceEntity instance)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    INSERT INTO BookInstances (BookID, InventoryNumber, Status, CanBeSold)
                    VALUES (@BookID, @InventoryNumber, @Status, @CanBeSold)";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@BookID", instance.BookID);
					cmd.Parameters.AddWithValue("@InventoryNumber", instance.InventoryNumber);
					cmd.Parameters.AddWithValue("@Status", instance.Status ?? "Доступна");
					cmd.Parameters.AddWithValue("@CanBeSold", instance.CanBeSold);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void InsertMultiple(int bookId, int quantity)
		{
			for (int i = 0; i < quantity; i++)
			{
				var instance = new BookInstanceEntity
				{
					BookID = bookId,
					InventoryNumber = $"{bookId}-{i + 1:000}",
					Status = "Доступна",
					CanBeSold = true
				};
				Insert(instance);
			}
		}

		public void Update(BookInstanceEntity instance)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    UPDATE BookInstances 
                    SET BookID = @BookID, 
                        InventoryNumber = @InventoryNumber, 
                        Status = @Status, 
                        CanBeSold = @CanBeSold
                    WHERE ID = @ID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ID", instance.ID);
					cmd.Parameters.AddWithValue("@BookID", instance.BookID);
					cmd.Parameters.AddWithValue("@InventoryNumber", instance.InventoryNumber);
					cmd.Parameters.AddWithValue("@Status", instance.Status);
					cmd.Parameters.AddWithValue("@CanBeSold", instance.CanBeSold);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void UpdateStatus(int instanceId, string status)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "UPDATE BookInstances SET Status = @Status WHERE ID = @ID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ID", instanceId);
					cmd.Parameters.AddWithValue("@Status", status);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void Delete(int id)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "DELETE FROM BookInstances WHERE ID = @ID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ID", id);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void DeleteAvailable(int bookId, int count = 1)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    DELETE TOP (@Count) FROM BookInstances 
                    WHERE BookID = @BookID AND Status = N'Доступна'";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@Count", count);
					cmd.Parameters.AddWithValue("@BookID", bookId);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void DeleteByBookId(int bookId)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "DELETE FROM BookInstances WHERE BookID = @BookID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@BookID", bookId);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public int GetAvailableCount(int bookId)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "SELECT COUNT(*) FROM BookInstances WHERE BookID = @BookID AND Status = N'Доступна'";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@BookID", bookId);
					return (int)cmd.ExecuteScalar();
				}
			}
		}

		public int GetTotalCount(int bookId)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "SELECT COUNT(*) FROM BookInstances WHERE BookID = @BookID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@BookID", bookId);
					return (int)cmd.ExecuteScalar();
				}
			}
		}

		public bool IsInventoryNumberExists(string inventoryNumber, int? excludeId = null)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "SELECT COUNT(*) FROM BookInstances WHERE InventoryNumber = @InventoryNumber";
				if (excludeId.HasValue)
					query += " AND ID != @ExcludeId";

				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@InventoryNumber", inventoryNumber);
					if (excludeId.HasValue)
						cmd.Parameters.AddWithValue("@ExcludeId", excludeId.Value);

					int count = (int)cmd.ExecuteScalar();
					return count > 0;
				}
			}
		}

		private BookInstanceEntity MapReaderToEntity(SqlDataReader reader)
		{
			return new BookInstanceEntity
			{
				ID = reader.GetInt32(0),
				BookID = reader.GetInt32(1),
				InventoryNumber = reader.GetString(2),
				Status = reader.GetString(3),
				AcquisitionDate = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
				InvoiceNumber = reader.IsDBNull(5) ? null : reader.GetString(5),
				CanBeSold = reader.GetBoolean(6)
			};
		}
	}
}