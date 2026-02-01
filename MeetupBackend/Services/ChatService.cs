using MeetupBackend.Models;
using Neo4j.Driver;
using StackExchange.Redis;
using System.Text.Json;

namespace MeetupBackend.Services
{
    public class ChatService
    {
        private readonly IDatabase _redisDb;
        private readonly ISubscriber _redisPubSub; // Za Pub/Sub
        private readonly IDriver _neo4jDriver;
        
        // Kanal na koji saljemo sve poruke. Subscriber cee ih filtrirati.
        private const string PUBSUB_CHANNEL = "chat:events:live"; 

        public ChatService(IConnectionMultiplexer redis, IDriver driver)
        {
            _redisDb = redis.GetDatabase();
            _redisPubSub = redis.GetSubscriber();
            _neo4jDriver = driver;
        }

        private string GetEventKey(string eventId) => $"chat:event:{eventId}:history";

        public async Task SendMessage(EventChatMessage message)
        {
            var serialized = JsonSerializer.Serialize(message);

            //Write-Behind: Sacuvaj u Redis Listu za istoriju
            await _redisDb.ListRightPushAsync(GetEventKey(message.EventId), serialized);

            //Pub/Sub: Objavi poruku svim instancama aplikacije(razlicitim serverima)
            //Subscriber servis ce ovo uhvatiti i proslediti SignalR klijentima
            await _redisPubSub.PublishAsync(PUBSUB_CHANNEL, serialized);
        }

        public async Task<List<EventChatMessage>> GetEventHistory(string eventId)
        {
            var messages = new List<EventChatMessage>();
            var key = GetEventKey(eventId);

            //Hot Data (Redis)
            var redisMessages = await _redisDb.ListRangeAsync(key);
            foreach (var item in redisMessages)
            {
                var msg = JsonSerializer.Deserialize<EventChatMessage>(item.ToString());
                if(msg != null) messages.Add(msg);
            }

            //Cold Data (Neo4j)
            await using var session = _neo4jDriver.AsyncSession();
            var query = @"
                MATCH (e:Event {id: $eventId})<-[:POSTED_IN]-(m:Message)<-[:SENT]-(u:User)
                RETURN m.content as content, 
                       m.timestamp as timestamp, 
                       u.id as senderId, 
                       u.name as senderName
                ORDER BY m.timestamp ASC";

            var result = await session.RunAsync(query, new { eventId });
            
            var neo4jMsgs = new List<EventChatMessage>();
            await result.ForEachAsync(record =>
            {
                neo4jMsgs.Add(new EventChatMessage
                {
                    EventId = eventId,
                    Content = record["content"].As<string>(),
                    SenderId = record["senderId"].As<string>(),
                    SenderName = record["senderName"].As<string>(),
                    Timestamp = DateTime.Parse(record["timestamp"].As<string>())
                });
            });

            neo4jMsgs.AddRange(messages);
            return neo4jMsgs;
        }
    }
}