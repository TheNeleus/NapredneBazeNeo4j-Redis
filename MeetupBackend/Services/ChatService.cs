using MeetupBackend.Models;
using Neo4j.Driver;
using StackExchange.Redis;
using System.Text.Json;

namespace MeetupBackend.Services
{
    public class ChatService
    {
        private readonly IDriver _driver;            
        private readonly IConnectionMultiplexer _redis;  

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

            await db.PublishAsync("chat:events:live", json);
        }

        public async Task<List<EventChatMessage>> GetEventHistory(string eventId, int page = 0, int pageSize = 50)
        {
            var allMessages = new List<EventChatMessage>();

            await using var session = _driver.AsyncSession();
            
            int skip = page * pageSize;

            var query = @"
                MATCH (e:Event {id: $eventId})<-[:POSTED_IN]-(m:Message)
                MATCH (u:User)-[:SENT]->(m)
                RETURN m.content as Content, m.timestamp as Timestamp, u.id as SenderId, u.name as SenderName
                ORDER BY m.timestamp DESC 
                SKIP $skip 
                LIMIT $limit
            ";

            var cursor = await session.RunAsync(query, new { eventId, skip, limit = pageSize });
            
            var neo4jMessages = await cursor.ToListAsync(record => new EventChatMessage
            {
                EventId = eventId,
                SenderId = record["SenderId"].As<string>(),
                SenderName = record["SenderName"].As<string>(),
                Content = record["Content"].As<string>(),
                Timestamp = DateTime.Parse(record["Timestamp"].As<string>())
            });

            allMessages.AddRange(neo4jMessages);

            if (page == 0)
            {
                var db = _redis.GetDatabase();
                var redisKey = $"chat:event:{eventId}:history";

                var redisRawData = await db.ListRangeAsync(redisKey, 0, -1);
                
                var redisMessages = new List<EventChatMessage>();
                foreach (var item in redisRawData)
                {
                    var msg = JsonSerializer.Deserialize<EventChatMessage>(item.ToString());
                    if (msg != null)
                    {
                        redisMessages.Add(msg);
                    }
                }

                allMessages.AddRange(redisMessages);
            }

            var uniqueMessages = allMessages
                .DistinctBy(m => new { m.SenderId, m.Timestamp, m.Content }) // Jedinstvenost po ovim poljima
                .ToList();

            return uniqueMessages.OrderBy(m => m.Timestamp).ToList();
        }
    }
}