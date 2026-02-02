using MeetupBackend.Models;
using Neo4j.Driver;
using StackExchange.Redis;
using System.Text.Json;

namespace MeetupBackend.Workers
{
    public class ChatPersistenceWorker : BackgroundService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDriver _neo4jDriver;
        private readonly ILogger<ChatPersistenceWorker> _logger;

        private const int BATCH_SIZE = 50; // Kolicinski limit (Keep Hot Data)
        private readonly TimeSpan MAX_AGE = TimeSpan.FromMinutes(2); // Vremenski limit (za testiranje 2 min, produkcija npr 60 min)

        public ChatPersistenceWorker(IConnectionMultiplexer redis, IDriver driver, ILogger<ChatPersistenceWorker> logger)
        {
            _redis = redis;
            _neo4jDriver = driver;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessChatData();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error migrating chat data to Neo4j");
                }

                // U produkciji vrati na TimeSpan.FromMinutes(1)
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task ProcessChatData()
        {
            var endpoint = _redis.GetEndPoints().FirstOrDefault();
            if (endpoint == null) return;

            var server = _redis.GetServer(endpoint);
            var db = _redis.GetDatabase();

            foreach (var key in server.Keys(pattern: "chat:event:*:history"))
            {
                long listLength = await db.ListLengthAsync(key);
                if (listLength == 0) continue;

                bool shouldMigrate = false;
                long countToKeep = BATCH_SIZE;

                if (listLength > BATCH_SIZE)
                {
                    shouldMigrate = true;
                    _logger.LogInformation($"[Trigger-Count] Key: {key} has {listLength} messages (Limit: {BATCH_SIZE}).");
                }
                else
                {
                    var oldestRedisMsg = await db.ListGetByIndexAsync(key, 0);
                    if (oldestRedisMsg.HasValue)
                    {
                        var msg = JsonSerializer.Deserialize<EventChatMessage>(oldestRedisMsg.ToString());
                        if (msg != null)
                        {
                            var age = DateTime.UtcNow - msg.Timestamp;
                            if (age > MAX_AGE)
                            {
                                shouldMigrate = true;
                                countToKeep = 0; 
                                _logger.LogInformation($"[Trigger-Time] Key: {key} oldest message is {age.TotalMinutes:F1} min old.");
                            }
                        }
                    }
                }

                if (shouldMigrate)
                {
                    long countToMigrate = listLength - countToKeep;

                    if (countToMigrate > 0)
                    {
                        var oldMessages = await db.ListRangeAsync(key, 0, countToMigrate - 1);
                        
                        var messagesToMigrate = new List<EventChatMessage>();
                        foreach (var item in oldMessages)
                        {
                            var msg = JsonSerializer.Deserialize<EventChatMessage>(item.ToString());
                            if (msg != null) messagesToMigrate.Add(msg);
                        }

                        if (messagesToMigrate.Count > 0)
                        {
                            await SaveBatchToNeo4j(messagesToMigrate);

                            await db.ListTrimAsync(key, countToMigrate, -1); 
                            
                            _logger.LogInformation($"SUCCESS: Migrated {messagesToMigrate.Count} messages to Neo4j. Remaining in Redis: {await db.ListLengthAsync(key)}");
                        }
                    }
                }
            }
        }

        private async Task SaveBatchToNeo4j(List<EventChatMessage> messages)
        {
            await using var session = _neo4jDriver.AsyncSession();

            // Koristimo MERGE da izbegnemo duplikate ako worker pukne na pola
            var query = @"
                UNWIND $batch as msg
                MERGE (u:User {id: msg.SenderId})
                MERGE (e:Event {id: msg.EventId})
                MERGE (m:Message {timestamp: msg.Timestamp, content: msg.Content}) // Jedinstvenost poruke
                MERGE (u)-[:SENT]->(m)
                MERGE (m)-[:POSTED_IN]->(e)
            ";

            await session.RunAsync(query, new { batch = messages });
        }
    }
}