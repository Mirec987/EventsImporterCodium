using EventImporter.Services;
using EventImporter.Utils;
using Serilog;
using System.Diagnostics;

namespace EventImporter
{
    public class Program
    {

        public static async Task Main(string[] args)
        {
            LoggerConfig.ConfigureLogging();
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                Log.Information("The program is running.");
                Console.WriteLine("The program is running.");

                var fileReader = new EventFileReader(Config.FilePath);
                var databaseHandler = new DatabaseHandler(Config.ConnectionString);
                var eventProcessor = new EventProcessor(databaseHandler);

                var eventMessages = await fileReader.LoadEventMessagesAsync();

                if (eventMessages != null)
                {
                    await databaseHandler.CreateTablesAsync();
                    await eventProcessor.ProcessEventsAsync(eventMessages);
                    Log.Information("All messages processed.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in the main program.");
                Console.WriteLine($"Error in the main program, for more info check the log file.");
            }
            finally
            {
                double elapsedSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 3);
                Log.Information($"Total program execution time: {elapsedSeconds} seconds.");
                Console.WriteLine($"Total program execution time: {elapsedSeconds} seconds, for more info check the log file.");

                Log.CloseAndFlush();
            }
        }
    }
}
