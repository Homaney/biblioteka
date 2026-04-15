using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using biblioteka.Entities;

namespace biblioteka.DAO
{
	public class SaleDAO
	{
		public List<SaleEntity> GetAll()
		{
			var list = new List<SaleEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT SaleID, BookID, ReaderID, SaleDate, Quantity, UnitPrice, TotalAmount, Notes
                    FROM Sales
                    ORDER BY SaleDate DESC";
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

		public SaleEntity GetById(int id)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT SaleID, BookID, ReaderID, SaleDate, Quantity, UnitPrice, TotalAmount, Notes
                    FROM Sales
                    WHERE SaleID = @ID";
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

		public List<SaleEntity> GetByDateRange(DateTime startDate, DateTime endDate)
		{
			var list = new List<SaleEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT SaleID, BookID, ReaderID, SaleDate, Quantity, UnitPrice, TotalAmount, Notes
                    FROM Sales
                    WHERE SaleDate >= @StartDate AND SaleDate < @EndDate
                    ORDER BY SaleDate DESC";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@StartDate", startDate);
					cmd.Parameters.AddWithValue("@EndDate", endDate.AddDays(1));
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

		public List<SaleEntity> GetByBookId(int bookId)
		{
			var list = new List<SaleEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT SaleID, BookID, ReaderID, SaleDate, Quantity, UnitPrice, TotalAmount, Notes
                    FROM Sales
                    WHERE BookID = @BookID
                    ORDER BY SaleDate DESC";
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

		public List<SaleEntity> GetByReaderId(int readerId)
		{
			var list = new List<SaleEntity>();
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT SaleID, BookID, ReaderID, SaleDate, Quantity, UnitPrice, TotalAmount, Notes
                    FROM Sales
                    WHERE ReaderID = @ReaderID
                    ORDER BY SaleDate DESC";
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

		public void Insert(SaleEntity sale)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    INSERT INTO Sales (BookID, ReaderID, Quantity, UnitPrice, TotalAmount, Notes, SaleDate)
                    VALUES (@BookID, @ReaderID, @Quantity, @UnitPrice, @TotalAmount, @Notes, @SaleDate)";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@BookID", sale.BookID);
					cmd.Parameters.AddWithValue("@ReaderID", sale.ReaderID);
					cmd.Parameters.AddWithValue("@Quantity", sale.Quantity);
					cmd.Parameters.AddWithValue("@UnitPrice", sale.UnitPrice);
					cmd.Parameters.AddWithValue("@TotalAmount", sale.TotalAmount);
					cmd.Parameters.AddWithValue("@Notes", (object)sale.Notes ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@SaleDate", sale.SaleDate);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void Update(SaleEntity sale)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    UPDATE Sales 
                    SET BookID = @BookID, 
                        ReaderID = @ReaderID, 
                        Quantity = @Quantity, 
                        UnitPrice = @UnitPrice, 
                        TotalAmount = @TotalAmount, 
                        Notes = @Notes, 
                        SaleDate = @SaleDate
                    WHERE SaleID = @SaleID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@SaleID", sale.SaleID);
					cmd.Parameters.AddWithValue("@BookID", sale.BookID);
					cmd.Parameters.AddWithValue("@ReaderID", sale.ReaderID);
					cmd.Parameters.AddWithValue("@Quantity", sale.Quantity);
					cmd.Parameters.AddWithValue("@UnitPrice", sale.UnitPrice);
					cmd.Parameters.AddWithValue("@TotalAmount", sale.TotalAmount);
					cmd.Parameters.AddWithValue("@Notes", (object)sale.Notes ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@SaleDate", sale.SaleDate);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void Delete(int id)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "DELETE FROM Sales WHERE SaleID = @ID";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@ID", id);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public decimal GetTotalSales(DateTime startDate, DateTime endDate)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT ISNULL(SUM(TotalAmount), 0) 
                    FROM Sales 
                    WHERE SaleDate >= @StartDate AND SaleDate < @EndDate";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@StartDate", startDate);
					cmd.Parameters.AddWithValue("@EndDate", endDate.AddDays(1));
					return (decimal)cmd.ExecuteScalar();
				}
			}
		}

		public int GetTotalQuantity(DateTime startDate, DateTime endDate)
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = @"
                    SELECT ISNULL(SUM(Quantity), 0) 
                    FROM Sales 
                    WHERE SaleDate >= @StartDate AND SaleDate < @EndDate";
				using (var cmd = new SqlCommand(query, connection))
				{
					cmd.Parameters.AddWithValue("@StartDate", startDate);
					cmd.Parameters.AddWithValue("@EndDate", endDate.AddDays(1));
					return (int)cmd.ExecuteScalar();
				}
			}
		}

		public int GetTotalCount()
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "SELECT COUNT(*) FROM Sales";
				using (var cmd = new SqlCommand(query, connection))
				{
					return (int)cmd.ExecuteScalar();
				}
			}
		}

		public decimal GetTotalRevenue()
		{
			using (var connection = DatabaseHelper.GetConnection())
			{
				connection.Open();
				string query = "SELECT ISNULL(SUM(TotalAmount), 0) FROM Sales";
				using (var cmd = new SqlCommand(query, connection))
				{
					return (decimal)cmd.ExecuteScalar();
				}
			}
		}

		private SaleEntity MapReaderToEntity(SqlDataReader reader)
		{
			return new SaleEntity
			{
				SaleID = reader.GetInt32(0),
				BookID = reader.GetInt32(1),
				ReaderID = reader.GetInt32(2),
				SaleDate = reader.GetDateTime(3),
				Quantity = reader.GetInt32(4),
				UnitPrice = reader.GetDecimal(5),
				TotalAmount = reader.GetDecimal(6),
				Notes = reader.IsDBNull(7) ? null : reader.GetString(7)
			};
		}
	}
}