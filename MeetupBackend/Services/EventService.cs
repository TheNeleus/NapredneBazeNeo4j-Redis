using MeetupBackend.Models;
using Neo4j.Driver;
using StackExchange.Redis;

namespace MeetupBackend.Services
{
    public class EventService
    {
        private readonly IDriver _driver;
        private readonly IDatabase _redisDb;

        public EventService(IDriver driver, IConnectionMultiplexer redis)
        {
            _driver = driver;
            _redisDb = redis.GetDatabase();
        }

        public async Task<Event> CreateEvent(Event evt, string creatorId)
        {
            evt.Id = Guid.NewGuid().ToString();

            await using var session = _driver.AsyncSession();
            
            var query = @"
                MATCH (u:User {id: $creatorId})
                CREATE (e:Event {
                    id: $id,
                    title: $title,
                    description: $description,
                    date: $date,
                    category: $category,
                    latitude: $latitude,
                    longitude: $longitude
                })
                CREATE (u)-[:CREATED]->(e)
                RETURN e.id as id, e.title as title";

            await session.RunAsync(query, new
            {
                creatorId = creatorId,
                
                id = evt.Id,
                title = evt.Title,
                description = evt.Description,
                date = evt.Date.ToString("s"),
                category = evt.Category,
                latitude = evt.Latitude,
                longitude = evt.Longitude
            });

            return evt;
        }

        public async Task AttendEvent(string userId, string eventId)
        {
            await using var session = _driver.AsyncSession();

            var query = @"
                MATCH (u:User {id: $userId})
                MATCH (e:Event {id: $eventId})
                MERGE (u)-[:ATTENDING]->(e)
                RETURN u.name, e.title";

            await session.RunAsync(query, new { userId, eventId });
        }

        public async Task<bool> DeleteEvent(string userId, string eventId)
        {
            await using var session = _driver.AsyncSession();

            var query = @"
                MATCH (u:User {id: $userId})
                MATCH (e:Event {id: $eventId})
                OPTIONAL MATCH (u)-[:HAS_ROLE]->(adminRole:Role {name: 'Admin'})
                OPTIONAL MATCH (u)-[creatorRel:CREATED]->(e)
                WITH e, adminRole, creatorRel
                WHERE adminRole IS NOT NULL OR creatorRel IS NOT NULL
                DETACH DELETE e
                RETURN count(e) as deletedCount";

            var result = await session.RunAsync(query, new { userId, eventId });

            if (await result.FetchAsync())
            {
                return result.Current["deletedCount"].As<int>() > 0;
            }

            return false;
        }

        public async Task<List<Dictionary<string, object>>> GetRecommendedEvents(string userId)
        {
            await using var session = _driver.AsyncSession();

            var query = @"
                MATCH (me:User {id: $userId})-[:FRIEND]->(f:User)-[:ATTENDING]->(e:Event)
                RETURN e.id as id, 
                       e.title as title, 
                       e.description as description, 
                       e.date as date, 
                       count(f) as friendsGoing
                ORDER BY friendsGoing DESC";

            var result = await session.RunAsync(query, new { userId });

            var recommendations = new List<Dictionary<string, object>>();

            await result.ForEachAsync(record =>
            {
                var eventData = new Dictionary<string, object>
                {
                    { "id", record["id"].As<string>() },
                    { "title", record["title"].As<string>() },
                    { "description", record["description"].As<string>() },
                    { "date", record["date"].As<string>() },
                    { "friendsGoing", record["friendsGoing"].As<int>() }
                };
                recommendations.Add(eventData);
            });

            return recommendations;
        }
    }
}