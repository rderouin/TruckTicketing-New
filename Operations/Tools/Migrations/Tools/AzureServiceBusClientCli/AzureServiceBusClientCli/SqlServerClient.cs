using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace AzureServiceBusClientCli
{
    internal class SqlServerClient
    {
        private readonly string _connectionString;
        private readonly string _tableName = "[DataMigrationMessages]";
       // private readonly string _tableName = "[DataMigrationMessageSIT]";
       
        public SqlServerClient(string connectionString)
        {
            _connectionString = connectionString;
        }


        public string GetMasterDataMessagesCount()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);             

                var sql = @$"
                        SELECT count(*) as cnt
                        FROM [TruckTicketDataMigration].[dbo].{_tableName} m 
                        WHERE  m.[Processed] = 0                       
                      

                ";
                using var command = new SqlCommand(sql, connection);
                connection.Open();
                command.CommandTimeout = 0; // wait indefinitely
                var totalMessages = command.ExecuteScalar().ToString();
                return totalMessages;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }               
        }


        public IEnumerable<DataMigrationMessage> GetDataMessages(int BatchSize = 1000)
        {
            List<DataMigrationMessage> messages = null;

            try
            {

                using var connection = new SqlConnection(_connectionString);
                //var sql = "SELECT Message, MessageType, GeneratedDate, EntityId, AxEntityId, Processed FROM [TruckTicketDataMigration].[dbo].[DataMigrationMasterDataMessages] WHERE Processed = 0;";
                //var sql = "SELECT Message, MessageType, GeneratedDate, EntityId, AxEntityId,Processed FROM [TruckTicketDataMigration].[dbo].[DataMigrationMasterDataMessages] WHERE Processed = 0 and MessageType = 'EDIFieldDefinitionEntity';";
                var sql = @"SELECT m.[Message], m.[MessageType], m.[GeneratedDate], m.[EntityId], m.[AxEntityId], m.[Processed], m.[ID], m.TopicName 
                             FROM 
                             ( 
                                SELECT m.*, ROW_NUMBER() OVER(PARTITION BY m.[MessageType] ORDER BY m.[Id] ASC) rn
                                FROM [TruckTicketDataMigration].[dbo].[DataMigrationMessages] m 
                             ) m 
                             WHERE m.rn <= 10 
                             AND m.[Processed] = 0                       
                             ORDER BY m.[Id] ASC ";
             
                sql = @$"
                        SELECT top({BatchSize}) m.[Message], m.[MessageType], m.[GeneratedDate], m.[EntityId], m.[AxEntityId], m.[Processed], m.[ID], m.TopicName 
                        FROM [TruckTicketDataMigration].[dbo].{_tableName} m 
                        WHERE  m.[Processed] = 0 --and messageType = 'ProductEntity' -- not in ('EDIFieldDefinition', 'TradeAgreement')

                        ORDER BY m.[Id] ASC;
                         ";                       

         
                using var command = new SqlCommand(sql, connection);
                connection.Open();
                command.CommandTimeout = 0; // wait indefinitely
                using var reader = command.ExecuteReader();            
                if (reader.HasRows)
                {
                    messages = new List<DataMigrationMessage>();

                    while (reader.Read())
                    {
                        var message = new DataMigrationMessage
                        {
                            Message = reader.GetString(0),
                            MessageType = reader.GetString(1),
                            GeneratedDate = reader.GetDateTimeOffset(2),
                            EntityId = reader[3] != DBNull.Value && reader[3] != null ? reader.GetGuid(3) : Guid.Empty,
                            AxEntityId = reader.GetString(4),
                            Processed = reader.GetBoolean(5),
                            SequenceId   = reader.GetInt32(6),
                            TopicName= reader.GetString(7),
                        };

                        messages.Add(message);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }                        
            return messages;
        }

        public void MarkMessagesProcessed(List<string> entityIds)
        {
            // Console.WriteLine("Marking MasterData messages updated.");
            var sql = new StringBuilder();
            sql.Append(@"DROP TABLE IF EXISTS #temp; ");
            sql.Append(@"CREATE TABLE #temp (ids nvarchar(50)); ");
            foreach (var id in entityIds)
            {
                sql.Append(@$"INSERT INTO #temp values ('{id}'); ");
            }
            sql.Append(@$"UPDATE [TruckTicketDataMigration].[dbo].{_tableName} SET Processed = 1, ProcessedDate = @processedDate WHERE EntityId IN (select ids from #temp); ");

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql.ToString(), connection);
            connection.Open();
            command.CommandTimeout = 0; // wait indefinitely
            command.Parameters.AddWithValue("@processedDate", DateTimeOffset.Now);
            command.ExecuteNonQuery();
        }
    }
}
