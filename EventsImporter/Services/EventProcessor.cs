using EventImporter.Entities;
using Serilog;
using System.Collections.Concurrent;

namespace EventImporter.Services
{
    public class EventProcessor
    {
        private static readonly ConcurrentDictionary<long, ConcurrentQueue<EventMessage>> eventQueues = new ConcurrentDictionary<long, ConcurrentQueue<EventMessage>>();
        private static readonly ConcurrentDictionary<long, bool> processedEventIDs = new ConcurrentDictionary<long, bool>();
        private static readonly ConcurrentQueue<SportsEvent> eventBatch = new ConcurrentQueue<SportsEvent>();
        private static int currentBatchSize = 0;

        private readonly DatabaseHandler databaseHandler;

        public EventProcessor(DatabaseHandler databaseHandler)
        {
            this.databaseHandler = databaseHandler;
        }

        public async Task ProcessEventsAsync(List<EventMessage> eventMessages)
        {
            foreach (var message in eventMessages)
            {
                var queue = eventQueues.GetOrAdd(message.Event.ProviderEventID, new ConcurrentQueue<EventMessage>());
                queue.Enqueue(message);
            }

            var tasks = eventQueues.Values.Select(queue => ProcessEventMessagesAsync(queue)).ToArray();
            await Task.WhenAll(tasks);

            await SaveRemainingBatch();
        }

        private async Task ProcessEventMessagesAsync(ConcurrentQueue<EventMessage> queue)
        {
            SportsEvent currentEvent = null;

            while (queue.TryDequeue(out var message))
            {
                if (currentEvent == null)
                {
                    currentEvent = message.Event;
                }
                else
                {
                    UpdateEventData(currentEvent, message.Event);
                }

            }
            if (currentEvent != null)
            {
                await SimulateApiCallAsync(currentEvent.EventName);
                await AddEventToBatch(currentEvent);
            }
        }

        private void UpdateEventData(SportsEvent existingEvent, SportsEvent newEvent)
        {
            if (existingEvent.EventDate != newEvent.EventDate)
            {
                existingEvent.EventDate = newEvent.EventDate;
            }

            foreach (var newOdds in newEvent.OddsList)
            {
                var oddsDictionary = existingEvent.OddsList.ToDictionary(o => o.ProviderOddsID);

                if (oddsDictionary.TryGetValue(newOdds.ProviderOddsID, out var existingOdds))
                {
                    existingOdds.OddsRate = newOdds.OddsRate;
                    existingOdds.Status = newOdds.Status;
                }
                else
                {
                    existingEvent.OddsList.Add(newOdds);
                }
            }
        }

        private async Task AddEventToBatch(SportsEvent sportsEvent)
        {
            if (!processedEventIDs.TryAdd(sportsEvent.ProviderEventID, true))
            {
                return;
            }

            eventBatch.Enqueue(sportsEvent);
            Log.Information("Added event {EventName} to batch.", sportsEvent.EventName);

            int batchSizeAfterAdd = Interlocked.Increment(ref currentBatchSize);

            if (batchSizeAfterAdd >= Config.BatchSize)
            {
                List<SportsEvent> batchToSave = new List<SportsEvent>();

                while (batchToSave.Count < Config.BatchSize && eventBatch.TryDequeue(out var dequeuedEvent))
                {
                    batchToSave.Add(dequeuedEvent);
                }

                Interlocked.Add(ref currentBatchSize, -batchToSave.Count);

                if (batchToSave.Count > 0)
                {
                    Log.Information("Saving data from batch of size {BatchSize}.", batchToSave.Count);
                    await databaseHandler.SaveEventsInBatch(batchToSave);
                }
            }
        }

        private async Task SaveRemainingBatch()
        {
            List<SportsEvent> batchToSave = new List<SportsEvent>();

            while (eventBatch.TryDequeue(out var dequeuedEvent))
            {
                batchToSave.Add(dequeuedEvent);
            }

            if (batchToSave.Any())
            {
                Log.Information("Saving data from remaining batch.");
                await databaseHandler.SaveEventsInBatch(batchToSave);
            }
        }

        private async Task SimulateApiCallAsync(string eventName)
        {
            Random rand = new Random();
            int delay = rand.Next(0, Config.MaxApiDelay);
            await Task.Delay(delay);
            Log.Information("Calling API for " + eventName);

        }
    }
}
