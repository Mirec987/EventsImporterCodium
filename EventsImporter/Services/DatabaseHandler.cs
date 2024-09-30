using EventImporter.Entities;
using Microsoft.Data.SqlClient;
using System.Data;

namespace EventImporter.Services
{
    public class DatabaseHandler
    {
        private readonly string connectionString;

        public DatabaseHandler(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task SaveEventsInBatch(List<SportsEvent> events)
        {
            if (events == null || events.Count == 0)
                return;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                DataTable eventTable = new DataTable();
                eventTable.Columns.Add("ProviderEventID", typeof(int));
                eventTable.Columns.Add("EventName", typeof(string));
                eventTable.Columns.Add("EventDate", typeof(DateTime));

                DataTable oddsTable = new DataTable();
                oddsTable.Columns.Add("ProviderOddsID", typeof(int));
                oddsTable.Columns.Add("ProviderEventID", typeof(int));
                oddsTable.Columns.Add("OddsName", typeof(string));
                oddsTable.Columns.Add("OddsRate", typeof(decimal));
                oddsTable.Columns.Add("Status", typeof(string));

                foreach (var e in events)
                {
                    eventTable.Rows.Add(e.ProviderEventID, e.EventName, e.EventDate);
                    foreach (var odds in e.OddsList)
                    {
                        oddsTable.Rows.Add(odds.ProviderOddsID, e.ProviderEventID, odds.OddsName, odds.OddsRate, odds.Status);
                    }
                }

                await BulkInsertAsync(conn, "Events", eventTable, new Dictionary<string, string>
                    {
                        {"ProviderEventID", "ProviderEventID"},
                        {"EventName", "EventName"},
                        {"EventDate", "EventDate"}
                    });

                await BulkInsertAsync(conn, "Odds", oddsTable, new Dictionary<string, string>
                    {
                        {"ProviderOddsID", "ProviderOddsID"},
                        {"ProviderEventID", "ProviderEventID"},
                        {"OddsName", "OddsName"},
                        {"OddsRate", "OddsRate"},
                        {"Status", "Status"}
                    });
            }
        }

        private async Task BulkInsertAsync(SqlConnection conn, string tableName, DataTable dataTable, Dictionary<string, string> columnMappings)
        {
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
            {
                bulkCopy.DestinationTableName = tableName;
                foreach (var mapping in columnMappings)
                {
                    bulkCopy.ColumnMappings.Add(mapping.Key, mapping.Value);
                }
                await bulkCopy.WriteToServerAsync(dataTable);
            }
        }
        public async Task CreateTablesAsync()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string createEventsTableQuery = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Events' AND xtype='U')
                    CREATE TABLE Events (
                        ProviderEventID BIGINT PRIMARY KEY,
                        EventName NVARCHAR(255),
                        EventDate DATETIME
                    )";

                string createOddsTableQuery = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Odds' AND xtype='U')
                    CREATE TABLE Odds (
                        ProviderOddsID BIGINT PRIMARY KEY,
                        ProviderEventID BIGINT,
                        OddsName NVARCHAR(255),
                        OddsRate DECIMAL(18,3),
                        Status NVARCHAR(50),
                        FOREIGN KEY (ProviderEventID) REFERENCES Events(ProviderEventID)
                    )";

                using (SqlCommand createEventsTableCmd = new SqlCommand(createEventsTableQuery, connection))
                using (SqlCommand createOddsTableCmd = new SqlCommand(createOddsTableQuery, connection))
                {
                    await createEventsTableCmd.ExecuteNonQueryAsync();
                    await createOddsTableCmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
