using MeetupBackend.DTOs;
using Neo4j.Driver;
using StackExchange.Redis;
using System.Text.Json;

namespace MeetupBackend.Services
{
    public class ChatService
    {
        private readonly IDriver _driver;            
        private readonly IConnectionMultiplexer _redis;
        
        private const int MAX_ACTIVE_CHATS = 5; 
        private const string TRACKING_KEY = "chat:active_list";

        public ChatService(IDriver driver, IConnectionMultiplexer redis)
        {
            _driver = driver;
            _redis = redis;
        }

        public async Task SendMessage(EventChatMessage message)
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(message);

            var key = $"chat:event:{message.EventId}:history";
            
            await db.ListRightPushAsync(key, json);

            // azuriramo listu
            await ManageActiveChatsLimit(message.EventId);

            await db.PublishAsync("chat:events:live", json);
        }

        public async Task<List<EventChatMessage>> GetEventHistory(string eventId, int page = 0, int pageSize = 50)
        {
            var db = _redis.GetDatabase();
            var redisKey = $"chat:event:{eventId}:history";

            // Stranica 0 - uzmi samo iz Redis-a (aktivne poruke)
            if (page == 0)
            {
                var redisRawData = await db.ListRangeAsync(redisKey, 0, -1);
                var redisMessages = new List<EventChatMessage>();
                
                foreach (var item in redisRawData)
                {
                    var msg = JsonSerializer.Deserialize<EventChatMessage>(item.ToString());
                    if (msg != null) redisMessages.Add(msg);
                }
                
                return redisMessages.OrderBy(m => m.Timestamp).ToList();
            }

            // Stranice > 0 - uzmi iz Neo4j-a (arhivirane poruke)
            await using var session = _driver.AsyncSession();
            
            int skip = (page - 1) * pageSize;

            var query = @"
                MATCH (e:Event {id: $eventId})<-[:POSTED_IN]-(m:Message)
                MATCH (u:User)-[:SENT]->(m)
                RETURN m.id as Id, m.content as Content, m.timestamp as Timestamp, u.id as SenderId, u.name as SenderName
                ORDER BY m.timestamp ASC 
                SKIP $skip 
                LIMIT $limit
            ";

            var cursor = await session.RunAsync(query, new { eventId, skip, limit = pageSize });
            
            var messages = await cursor.ToListAsync(record => new EventChatMessage
            {
                Id = record["Id"].As<string>(),
                EventId = eventId,
                SenderId = record["SenderId"].As<string>(),
                SenderName = record["SenderName"].As<string>(),
                Content = record["Content"].As<string>(),
                Timestamp = DateTime.Parse(record["Timestamp"].As<string>(), null, System.Globalization.DateTimeStyles.RoundtripKind)
            });

            return messages.OrderBy(m => m.Timestamp).ToList();
        }

        private async Task ManageActiveChatsLimit(string currentEventId)
        {
            var db = _redis.GetDatabase();

            await db.ListRemoveAsync(TRACKING_KEY, currentEventId);
            await db.ListLeftPushAsync(TRACKING_KEY, currentEventId);

            long count = await db.ListLengthAsync(TRACKING_KEY);

            while (count > MAX_ACTIVE_CHATS)
            {
                // uzimamo najstariji chat iz liste
                var evictedEventId = await db.ListRightPopAsync(TRACKING_KEY);

                if (evictedEventId.HasValue)
                {
                    var redisKey = $"chat:event:{evictedEventId}:history";

                    // uzmi sve sto je u redis-u pre brisanja
                    var pendingMessages = await db.ListRangeAsync(redisKey, 0, -1);
                    
                    if (pendingMessages.Length > 0)
                    {
                        var messagesToSave = new List<EventChatMessage>();
                        foreach (var item in pendingMessages)
                        {
                            var msg = JsonSerializer.Deserialize<EventChatMessage>(item.ToString());
                            if (msg != null) messagesToSave.Add(msg);
                        }

                        // sacuvaj u neo4j pre brisanja iz redisa
                        Console.WriteLine($"[Cache Eviction] Saving {messagesToSave.Count} messages to Neo4j before deleting Redis key for {evictedEventId}.");
                        await SaveBatchToNeo4j(messagesToSave);
                    }

                    // obrisi iz ram-a
                    await db.KeyDeleteAsync(redisKey);
                    Console.WriteLine($"[Cache Eviction] Removed chat {evictedEventId} from Redis.");
                }
                count--;
            }
        }

        private async Task SaveBatchToNeo4j(List<EventChatMessage> messages)
        {
            if (messages == null || messages.Count == 0) return;

            await using var session = _driver.AsyncSession();

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