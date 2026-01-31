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

                // Proveravaj na svakih 1 minut
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessChatData()
        {
            var server = _redis.GetServer("localhost", 6379);
            var db = _redis.GetDatabase();

            // Skeniraj sve kljuceve koji pocinju sa "chat:history:"
            foreach (var key in server.Keys(pattern: "chat:history:*"))
            {
                long listLength = await db.ListLengthAsync(key);

                if (listLength > BATCH_SIZE)
                {
                    // Uzmi najstarijih 50 poruka (sa pocetka liste, index 0 do 49)
                    var oldMessages = await db.ListRangeAsync(key, 0, BATCH_SIZE - 1);
                    
                    var messagesToMigrate = new List<EventChatMessage>();
                    foreach (var item in oldMessages)
                    {
                        var msg = JsonSerializer.Deserialize<EventChatMessage>(item.ToString());
                        if (msg != null) messagesToMigrate.Add(msg);
                    }

                    if (messagesToMigrate.Any())
                    {
                        //Upisi u Neo4j
                        await SaveBatchToNeo4j(messagesToMigrate);

                        //Obrisi iz Redisa (Trimuj listu)
                        //LTRIM key start stop -> Zadrzavamo od BATCH_SIZE pa na dalje
                        await db.ListTrimAsync(key, BATCH_SIZE, -1);
                        
                        _logger.LogInformation($"Migrated {messagesToMigrate.Count} messages for {key} to Neo4j.");
                    }
                }
            }
        }

        private async Task SaveBatchToNeo4j(List<EventChatMessage> messages)
        {
            await using var session = _neo4jDriver.AsyncSession();

            // Bulk insert query koristeći UNWIND za performanse
            var query = @"
                UNWIND $batch as msg
                MATCH (u1:User {id: msg.SenderId})
                MATCH (u2:User {id: msg.ReceiverId})
                CREATE (m:Message {
                    content: msg.Content, 
                    timestamp: msg.Timestamp
                })
                CREATE (u1)-[:SENT]->(m)
                CREATE (m)-[:RECEIVED_BY]->(u2)
            ";

            await session.RunAsync(query, new { batch = messages });
        }
    }
}