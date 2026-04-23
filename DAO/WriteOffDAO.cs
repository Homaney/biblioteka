using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using biblioteka.Entities;

namespace biblioteka.DAO
{
    public class WriteOffDAO
    {
        public int Insert(WriteOffActEntity act, List<int> instanceIds)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Вставляем акт
                        string actQuery = @"
                            INSERT INTO WriteOffActs (ActNumber, WriteOffDate, Reason)
                            VALUES (@ActNumber, @WriteOffDate, @Reason);
                            SELECT SCOPE_IDENTITY();";
                        int actId;
                        using (var cmd = new SqlCommand(actQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ActNumber", act.ActNumber);
                            cmd.Parameters.AddWithValue("@WriteOffDate", act.WriteOffDate);
                            cmd.Parameters.AddWithValue("@Reason", act.Reason);
                            actId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // Обновляем экземпляры
                        foreach (var instanceId in instanceIds)
                        {
                            string updateQuery = @"
                                UPDATE BookInstances 
                                SET Status = N'Списана', WriteOffActID = @ActID
                                WHERE ID = @InstanceID AND Status = N'Доступна'";
                            using (var cmd = new SqlCommand(updateQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ActID", actId);
                                cmd.Parameters.AddWithValue("@InstanceID", instanceId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return actId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<WriteOffActWithCount> GetAllWithCount()
        {
            var list = new List<WriteOffActWithCount>();
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                string query = @"
            SELECT wa.ID, wa.ActNumber, wa.WriteOffDate, wa.Reason, wa.CreatedAt,
                   COUNT(bi.ID) AS InstanceCount
            FROM WriteOffActs wa
            LEFT JOIN BookInstances bi ON wa.ID = bi.WriteOffActID
            GROUP BY wa.ID, wa.ActNumber, wa.WriteOffDate, wa.Reason, wa.CreatedAt
            ORDER BY wa.WriteOffDate DESC";
                using (var cmd = new SqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new WriteOffActWithCount
                        {
                            Id = reader.GetInt32(0),
                            ActNumber = reader.GetString(1),
                            WriteOffDate = reader.GetDateTime(2),
                            Reason = reader.GetString(3),
                            CreatedAt = reader.GetDateTime(4),
                            InstanceCount = reader.GetInt32(5)
                        });
                    }
                }
            }
            return list;
        }

        public WriteOffActEntity GetActById(int id)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                string query = "SELECT ID, ActNumber, WriteOffDate, Reason, CreatedAt FROM WriteOffActs WHERE ID = @ID";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new WriteOffActEntity
                            {
                                ID = reader.GetInt32(0),
                                ActNumber = reader.GetString(1),
                                WriteOffDate = reader.GetDateTime(2),
                                Reason = reader.GetString(3),
                                CreatedAt = reader.GetDateTime(4)
                            };
                        }
                        return null;
                    }
                }
            }
        }

        public List<int> GetActInstanceIds(int actId)
        {
            var ids = new List<int>();
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                string query = "SELECT ID FROM BookInstances WHERE WriteOffActID = @ActID";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ActID", actId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ids.Add(reader.GetInt32(0));
                        }
                    }
                }
            }
            return ids;
        }

        public void UpdateAct(WriteOffActEntity act, List<int> newInstanceIds)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Обновляем сам акт
                        string updateActQuery = @"
                    UPDATE WriteOffActs 
                    SET ActNumber = @ActNumber, WriteOffDate = @WriteOffDate, Reason = @Reason
                    WHERE ID = @ID";
                        using (var cmd = new SqlCommand(updateActQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ID", act.ID);
                            cmd.Parameters.AddWithValue("@ActNumber", act.ActNumber);
                            cmd.Parameters.AddWithValue("@WriteOffDate", act.WriteOffDate);
                            cmd.Parameters.AddWithValue("@Reason", act.Reason);
                            cmd.ExecuteNonQuery();
                        }

                        // 2. Убираем старые связи (возвращаем экземпляры в статус "Доступна")
                        var oldInstanceIds = GetActInstanceIds(act.ID);
                        foreach (var oldId in oldInstanceIds)
                        {
                            string revertQuery = @"
                        UPDATE BookInstances 
                        SET Status = N'Доступна', WriteOffActID = NULL
                        WHERE ID = @ID";
                            using (var cmd = new SqlCommand(revertQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ID", oldId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 3. Назначаем новые экземпляры
                        foreach (var newId in newInstanceIds)
                        {
                            string updateInstanceQuery = @"
                        UPDATE BookInstances 
                        SET Status = N'Списана', WriteOffActID = @ActID
                        WHERE ID = @ID AND Status = N'Доступна'";
                            using (var cmd = new SqlCommand(updateInstanceQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ActID", act.ID);
                                cmd.Parameters.AddWithValue("@ID", newId);
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

        public List<WriteOffActEntity> GetAll()
        {
            var list = new List<WriteOffActEntity>();
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                string query = "SELECT ID, ActNumber, WriteOffDate, Reason, CreatedAt FROM WriteOffActs ORDER BY WriteOffDate DESC";
                using (var cmd = new SqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new WriteOffActEntity
                        {
                            ID = reader.GetInt32(0),
                            ActNumber = reader.GetString(1),
                            WriteOffDate = reader.GetDateTime(2),
                            Reason = reader.GetString(3),
                            CreatedAt = reader.GetDateTime(4)
                        });
                    }
                }
            }
            return list;
        }
    }
}