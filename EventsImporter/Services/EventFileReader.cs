using EventImporter.Entities;
using Serilog;
using System.Text.Json;

namespace EventImporter.Services
{
    public class EventFileReader
    {
        private readonly string filePath;

        public EventFileReader(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("FilePath si empty or null", nameof(filePath));
            }

            this.filePath = filePath;
        }

        public async Task<List<EventMessage>> LoadEventMessagesAsync()
        {
            try
            {
                using FileStream openStream = File.OpenRead(filePath);
                return await JsonSerializer.DeserializeAsync<List<EventMessage>>(openStream);
            }
            catch (FileNotFoundException ex)
            {
                Log.Error(ex, $"Source file not found:{filePath}");

                throw;
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "Error in JSON file.");

                throw;
            }
        }
    }
}
