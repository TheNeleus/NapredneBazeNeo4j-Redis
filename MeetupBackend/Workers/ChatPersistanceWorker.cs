using MeetupBackend.DTOs;
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

        private const int BATCH_SIZE = 50;

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
                
                if (listLength <= BATCH_SIZE) continue;

                long countToMigrate = listLength - BATCH_SIZE;

                _logger.LogInformation($"[Migration] Key: {key} has {listLength}. Archiving {countToMigrate} old messages.");

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
                }
            }
        }

        private async Task SaveBatchToNeo4j(List<EventChatMessage> messages)
        {
            if (messages == null || messages.Count == 0) return;

            await using var session = _neo4jDriver.AsyncSession();

            var query = @"
                UNWIND $batch as msg
                MERGE (u:User {id: msg.SenderId})
                MERGE (e:Event {id: msg.EventId})
                MERGE (m:Message {id: msg.Id})
                SET m.timestamp = msg.Timestamp,
                    m.content = msg.Content
                MERGE (u)-[:SENT]->(m)
                MERGE (m)-[:POSTED_IN]->(e)
            ";

            await session.RunAsync(query, new { batch = messages });
        }
    }
}