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
        private const int BATCH_SIZE = 50; // Trigger za prebacivanje

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

                // Proveravaj na svaki minut
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessChatData()
        {
            var server = _redis.GetServer("localhost", 6379);
            var db = _redis.GetDatabase();

            foreach (var key in server.Keys(pattern: "chat:event:*:history"))
            {
                long listLength = await db.ListLengthAsync(key);

                if (listLength > BATCH_SIZE)
                {
                    
                    long countToMigrate = listLength - BATCH_SIZE;
                    var oldMessages = await db.ListRangeAsync(key, 0, countToMigrate - 1);
                    
                    var messagesToMigrate = new List<EventChatMessage>();
                    foreach (var item in oldMessages)
                    {
                        var msg = JsonSerializer.Deserialize<EventChatMessage>(item.ToString());
                        if (msg != null) messagesToMigrate.Add(msg);
                    }

                    if (messagesToMigrate.Count != 0)
                    {
                        //Upisi u Neo4j
                        await SaveBatchToNeo4j(messagesToMigrate);

                        //Obrisi iz Redisa (Trimuj listu)
                        //LTRIM key start stop -> Zadrzavamo od BATCH_SIZE pa na dalje
                        await db.ListTrimAsync(key, countToMigrate, -1); 
                        
                        _logger.LogInformation($"Migrated {messagesToMigrate.Count} messages for {key} to Neo4j.");
                    }
                }
            }
        }

        private async Task SaveBatchToNeo4j(List<EventChatMessage> messages)
        {
            await using var session = _neo4jDriver.AsyncSession();

            //UNWIND za listu zbog performansi
            var query = @"
                UNWIND $batch as msg
                MATCH (u:User {id: msg.SenderId})
                MATCH (e:Event {id: msg.EventId})
                CREATE (m:Message {
                    content: msg.Content, 
                    timestamp: msg.Timestamp
                })
                CREATE (u)-[:SENT]->(m)
                CREATE (m)-[:POSTED_IN]->(e)
            ";

            await session.RunAsync(query, new { batch = messages });
        }
    }
}