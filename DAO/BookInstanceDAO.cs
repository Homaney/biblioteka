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
                    SELECT ID, BookID, InventoryNumber, Status, AcquisitionDate, InvoiceNumber, CanBeSold, Price, WriteOffActID
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
                    SELECT ID, BookID, InventoryNumber, Status, AcquisitionDate, InvoiceNumber, CanBeSold, Price, WriteOffActID
                    FROM BookInstances 
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

        public List<BookInstanceEntity> GetByBookId(int bookId)
        {
            var list = new List<BookInstanceEntity>();
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT ID, BookID, InventoryNumber, Status, AcquisitionDate, InvoiceNumber, CanBeSold, Price, WriteOffActID
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
                    SELECT ID, BookID, InventoryNumber, Status, AcquisitionDate, InvoiceNumber, CanBeSold, Price, WriteOffActID
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
                    SELECT ID, BookID, InventoryNumber, Status, AcquisitionDate, InvoiceNumber, CanBeSold, Price, WriteOffActID
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

        public int GetNextSequenceNumber(int bookId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM BookInstances WHERE BookID = @BookID";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@BookID", bookId);
                    int count = (int)cmd.ExecuteScalar();
                    return count + 1;
                }
            }
        }

        public string GenerateInventoryNumber(int bookId)
        {
            int sequence = GetNextSequenceNumber(bookId);
            return $"Б-{bookId:0000}-{sequence:0000}";
        }

        public void Insert(BookInstanceEntity instance)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                string query = @"
                    INSERT INTO BookInstances (BookID, InventoryNumber, Status, CanBeSold, Price, InvoiceNumber, AcquisitionDate)
                    VALUES (@BookID, @InventoryNumber, @Status, @CanBeSold, @Price, @InvoiceNumber, @AcquisitionDate)";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@BookID", instance.BookID);
                    cmd.Parameters.AddWithValue("@InventoryNumber", instance.InventoryNumber);
                    cmd.Parameters.AddWithValue("@Status", instance.Status ?? "Доступна");
                    cmd.Parameters.AddWithValue("@CanBeSold", instance.CanBeSold);
                    cmd.Parameters.AddWithValue("@Price", instance.Price);
                    cmd.Parameters.AddWithValue("@InvoiceNumber", (object)instance.InvoiceNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AcquisitionDate", (object)instance.AcquisitionDate ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void InsertMultiple(int bookId, int quantity, decimal price)
        {
            for (int i = 0; i < quantity; i++)
            {
                string inventoryNumber = GenerateInventoryNumber(bookId);
                var instance = new BookInstanceEntity
                {
                    BookID = bookId,
                    InventoryNumber = inventoryNumber,
                    Status = "Доступна",
                    CanBeSold = true,
                    Price = price
                };
                Insert(instance);
            }
        }

        public void InsertInstances(int bookId, int quantity, decimal price, string invoiceNumber, DateTime acquisitionDate)
        {
            for (int i = 0; i < quantity; i++)
            {
                string inventoryNumber = GenerateInventoryNumber(bookId);
                var instance = new BookInstanceEntity
                {
                    BookID = bookId,
                    InventoryNumber = inventoryNumber,
                    Status = "Доступна",
                    CanBeSold = true,
                    Price = price,
                    InvoiceNumber = invoiceNumber,
                    AcquisitionDate = acquisitionDate
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
                    SET BookID = @BookID, InventoryNumber = @InventoryNumber, Status = @Status, 
                        CanBeSold = @CanBeSold, Price = @Price, InvoiceNumber = @InvoiceNumber,
                        AcquisitionDate = @AcquisitionDate, WriteOffActID = @WriteOffActID
                    WHERE ID = @ID";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ID", instance.ID);
                    cmd.Parameters.AddWithValue("@BookID", instance.BookID);
                    cmd.Parameters.AddWithValue("@InventoryNumber", instance.InventoryNumber);
                    cmd.Parameters.AddWithValue("@Status", instance.Status);
                    cmd.Parameters.AddWithValue("@CanBeSold", instance.CanBeSold);
                    cmd.Parameters.AddWithValue("@Price", instance.Price);
                    cmd.Parameters.AddWithValue("@InvoiceNumber", (object)instance.InvoiceNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AcquisitionDate", (object)instance.AcquisitionDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WriteOffActID", (object)instance.WriteOffActID ?? DBNull.Value);
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

        public decimal? GetMinPrice(int bookId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT MIN(Price) 
                    FROM BookInstances 
                    WHERE BookID = @BookID AND Status = N'Доступна'";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@BookID", bookId);
                    object result = cmd.ExecuteScalar();
                    return result != DBNull.Value ? (decimal?)Convert.ToDecimal(result) : null;
                }
            }
        }

        public bool IsInvoiceNumberExists(string invoiceNumber)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM BookInstances WHERE InvoiceNumber = @InvoiceNumber";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber);
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
                CanBeSold = reader.GetBoolean(6),
                Price = reader.GetDecimal(7),
                WriteOffActID = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8)
            };
        }
    }
}