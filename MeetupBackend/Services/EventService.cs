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

            await _redisDb.GeoAddAsync("events:geo", new GeoEntry(evt.Longitude, evt.Latitude, evt.Id));

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

            bool deletedFromNeo4j = false;
            if (await result.FetchAsync())
            {
                deletedFromNeo4j = result.Current["deletedCount"].As<int>() > 0;
            }

            if (deletedFromNeo4j)
            {
                await _redisDb.GeoRemoveAsync("events:geo", eventId);
                return true; 
            }

            return false;
        }

        public async Task<List<Dictionary<string, object>>> GetAllEvents()
        {
            await using var session = _driver.AsyncSession();

            var query = @"
                MATCH (e:Event)
                WHERE datetime(e.date) > datetime() 
                OPTIONAL MATCH (u:User)-[:ATTENDING]->(e)
                OPTIONAL MATCH (creator:User)-[:CREATED]->(e) 
                RETURN e.id as id, 
                    e.title as title, 
                    e.description as description, 
                    e.date as date, 
                    e.category as category,
                    e.latitude as latitude,
                    e.longitude as longitude,
                    collect(u.id) as attendees,
                    creator.id as creatorId";

            var result = await session.RunAsync(query);
            var events = new List<Dictionary<string, object>>();

            await result.ForEachAsync(record =>
            {
                var eventData = new Dictionary<string, object>
                {
                    { "id", record["id"].As<string>() },
                    { "title", record["title"].As<string>() },
                    { "description", record["description"].As<string>() },
                    { "date", record["date"].As<string>() },
                    { "category", record["category"].As<string>() },
                    { "latitude", record["latitude"].As<double>() },
                    { "longitude", record["longitude"].As<double>() },
                    { "attendees", record["attendees"].As<List<string>>() },
                    { "creatorId", record["creatorId"] != null ? record["creatorId"].As<string>() : "" }
                };
                events.Add(eventData);
            });

            return events;
        }

        public async Task<List<Dictionary<string, object>>> GetFriendsEvents(string userId)
        {
            await using var session = _driver.AsyncSession();

            var query = @"
                MATCH (me:User {id: $userId})-[:FRIEND]->(f:User)-[:ATTENDING]->(e:Event)
                WHERE datetime(e.date) > datetime()
                WITH e, count(f) as friendsGoing
                
                OPTIONAL MATCH (u:User)-[:ATTENDING]->(e)
                
                RETURN e.id as id, 
                    e.title as title, 
                    e.description as description, 
                    e.date as date, 
                    e.category as category,
                    e.latitude as latitude,
                    e.longitude as longitude,
                    friendsGoing,
                    collect(u.id) as attendees
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
                    { "friendsGoing", record["friendsGoing"].As<int>() },
                    { "latitude", record["latitude"].As<double>() },
                    { "longitude", record["longitude"].As<double>() },
                    { "attendees", record["attendees"].As<List<string>>() }
                };
                recommendations.Add(eventData);
            });

            return recommendations;
        }

        public async Task<List<Dictionary<string, object>>> GetRecommendedEvents(string userId, double lat, double lon, double radiusKm)
        {
            var geoResults = await _redisDb.GeoRadiusAsync("events:geo", lon, lat, radiusKm, GeoUnit.Kilometers);

            if (geoResults.Length == 0) return new List<Dictionary<string, object>>();

            var nearbyEventIds = geoResults.Select(r => r.Member.ToString()).ToList();

            await using var session = _driver.AsyncSession();

            var query = @"
                MATCH (u:User {id: $userId})
                MATCH (e:Event)
                WHERE e.id IN $nearbyEventIds   
                AND e.category IN u.interests           
                AND datetime(e.date) > datetime()
                
                OPTIONAL MATCH (attendant:User)-[:ATTENDING]->(e)

                RETURN e.id as id, 
                    e.title as title, 
                    e.description as description, 
                    e.date as date, 
                    e.category as category,
                    e.latitude as latitude,
                    e.longitude as longitude,
                    collect(attendant.id) as attendees 
                ORDER BY e.date ASC";

            var result = await session.RunAsync(query, new { userId, nearbyEventIds });

            var recommendations = new List<Dictionary<string, object>>();

            await result.ForEachAsync(record =>
            {
                var eventData = new Dictionary<string, object>
                {
                    { "id", record["id"].As<string>() },
                    { "title", record["title"].As<string>() },
                    { "description", record["description"].As<string>() },
                    { "date", record["date"].As<string>() },
                    { "category", record["category"].As<string>() },
                    { "latitude", record["latitude"].As<double>() },
                    { "longitude", record["longitude"].As<double>() },
                    { "attendees", record["attendees"].As<List<string>>() } 
                };
                recommendations.Add(eventData);
            });

            return recommendations;
        }

        public async Task<List<Dictionary<string, object>>> GetNearestEvents(double lat, double lon, double radiusKm)
        {
            var geoResults = await _redisDb.GeoRadiusAsync(
                "events:geo", lon, lat, radiusKm, GeoUnit.Kilometers, -1, Order.Ascending,
                GeoRadiusOptions.WithDistance | GeoRadiusOptions.WithCoordinates
            );

            if (geoResults.Length == 0) return new List<Dictionary<string, object>>();

            var eventDistances = geoResults.ToDictionary(k => k.Member.ToString(), v => v.Distance.Value);
            var eventIds = eventDistances.Keys.ToList();

            await using var session = _driver.AsyncSession();

            var query = @"
            MATCH (e:Event)
            WHERE e.id IN $eventIds
            AND datetime(e.date) > datetime() 
            
            OPTIONAL MATCH (u:User)-[:ATTENDING]->(e)

            RETURN e.id as id, 
                e.title as title, 
                e.description as description, 
                e.date as date, 
                e.category as category,
                e.latitude as latitude,
                e.longitude as longitude,
                collect(u.id) as attendees";

            var result = await session.RunAsync(query, new { eventIds });
            
            var nearestEvents = new List<Dictionary<string, object>>();

            await result.ForEachAsync(record =>
            {
                string id = record["id"].As<string>();
                if (eventDistances.ContainsKey(id))
                {
                    var eventData = new Dictionary<string, object>
                    {
                        { "id", id },
                        { "title", record["title"].As<string>() },
                        { "date", record["date"].As<string>() },
                        { "distanceKm", Math.Round(eventDistances[id], 2) },
                        { "attendees", record["attendees"].As<List<string>>() }
                    };
                    nearestEvents.Add(eventData);
                }
            });
            return nearestEvents.OrderBy(e => e["distanceKm"]).ToList();
        }

        public async Task<Event?> UpdateEvent(string userId, string eventId, Event updatedEvent)
        {
            await using var session = _driver.AsyncSession();

            var query = @"
                MATCH (u:User {id: $userId})
                MATCH (e:Event {id: $eventId})
                OPTIONAL MATCH (u)-[:HAS_ROLE]->(adminRole:Role {name: 'Admin'})
                OPTIONAL MATCH (u)-[creatorRel:CREATED]->(e)
                WITH e, adminRole, creatorRel
                WHERE adminRole IS NOT NULL OR creatorRel IS NOT NULL
                SET e.title = $title,
                    e.description = $description,
                    e.date = $date,
                    e.category = $category,
                    e.latitude = $latitude,
                    e.longitude = $longitude
                RETURN e.id as id, e.title as title, e.latitude as latitude, e.longitude as longitude";

            var result = await session.RunAsync(query, new
            {
                userId,
                eventId,
                title = updatedEvent.Title,
                description = updatedEvent.Description,
                date = updatedEvent.Date.ToString("s"),
                category = updatedEvent.Category,
                latitude = updatedEvent.Latitude,
                longitude = updatedEvent.Longitude
            });

            if (await result.FetchAsync())
            {

                await _redisDb.GeoAddAsync("events:geo",
                    new GeoEntry(updatedEvent.Longitude, updatedEvent.Latitude, eventId));


                updatedEvent.Id = eventId;
                return updatedEvent;
            }

            return null;
        }
        
        public async Task<List<Dictionary<string, object>>> GetEventAttendees(string eventId)
        {
            await using var session = _driver.AsyncSession();

            var query = @"
                MATCH (u:User)-[:ATTENDING]->(e:Event {id: $eventId})
                RETURN u.id as id, u.name as name, u.email as email";

            var result = await session.RunAsync(query, new { eventId });

            var attendees = new List<Dictionary<string, object>>();

            await result.ForEachAsync(record =>
            {
                attendees.Add(new Dictionary<string, object>
                {
                    { "id", record["id"].As<string>() },
                    { "name", record["name"].As<string>() },
                    { "email", record["email"].As<string>() }
                });
            });

            return attendees;
        }
    }
}