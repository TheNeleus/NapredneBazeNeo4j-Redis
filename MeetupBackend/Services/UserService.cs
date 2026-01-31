using MeetupBackend.Models;
using Neo4j.Driver;
using StackExchange.Redis;

namespace MeetupBackend.Services
{
    public class UserService
    {
        private readonly IDriver _driver;
        private readonly IDatabase _redisDb;

        public UserService(IDriver driver, IConnectionMultiplexer redis)
        {
            _driver = driver;
            _redisDb = redis.GetDatabase();
        }

        public async Task<User> CreateUser(User user)
        {
            user.Id = Guid.NewGuid().ToString();
            
            //if (string.IsNullOrEmpty(user.Role)) user.Role = "User";
            user.Role = "User";

            await using var session = _driver.AsyncSession();

            var query = @"
                CREATE (u:User {
                    id: $id, 
                    name: $name, 
                    email: $email, 
                    interests: $interests
                }) 
                WITH u
                MERGE (r:Role {name: $roleName})
                MERGE (u)-[:HAS_ROLE]->(r)
                RETURN u.id as id, u.name as name, u.email as email, u.interests as interests, r.name as role";

            var result = await session.RunAsync(query, new 
            { 
                id = user.Id, 
                name = user.Name, 
                email = user.Email, 
                interests = user.Interests,
                roleName = user.Role 
            });

            var record = await result.SingleAsync();
            
            return new User
            {
                Id = record["id"].As<string>(),
                Name = record["name"].As<string>(),
                Email = record["email"].As<string>(),
                Interests = record["interests"].As<List<string>>(),
                Role = record["role"].As<string>()
            };
        }

        public async Task<User?> GetUserById(string userId)
        {
            await using var session = _driver.AsyncSession();

            var query = @"
                MATCH (u:User {id: $userId})
                OPTIONAL MATCH (u)-[:HAS_ROLE]->(r:Role)
                RETURN u.id as id, 
                       u.name as name, 
                       u.email as email, 
                       u.interests as interests,
                       r.name as role";

            var result = await session.RunAsync(query, new { userId });

            if (await result.FetchAsync())
            {
                var role = result.Current["role"] != null ? result.Current["role"].As<string>() : "User";

                return new User
                {
                    Id = result.Current["id"].As<string>(),
                    Name = result.Current["name"].As<string>(),
                    Email = result.Current["email"].As<string>(),
                    Interests = result.Current["interests"].As<List<string>>(),
                    Role = role
                };
            }

            return null;
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            await using var session = _driver.AsyncSession();

            var query = @"
                MATCH (u:User {email: $email})
                OPTIONAL MATCH (u)-[:HAS_ROLE]->(r:Role)
                RETURN u.id as id, 
                       u.name as name, 
                       u.email as email, 
                       u.interests as interests,
                       r.name as role";

            var result = await session.RunAsync(query, new { email });

            if (await result.FetchAsync())
            {
                var role = result.Current["role"] != null ? result.Current["role"].As<string>() : "User";
                
                return new User
                {
                    Id = result.Current["id"].As<string>(),
                    Name = result.Current["name"].As<string>(),
                    Email = result.Current["email"].As<string>(),
                    Interests = result.Current["interests"].As<List<string>>(),
                    Role = role
                };
            }

            return null;
        }

        public async Task AddFriend(string userId, string friendId)
        {
            await using var session = _driver.AsyncSession();

            var query = @"
                MATCH (u1:User {id: $userId})
                MATCH (u2:User {id: $friendId})
                MERGE (u1)-[:FRIEND]->(u2)
                RETURN u1.name, u2.name";

            await session.RunAsync(query, new { userId, friendId });
        }

        public async Task<string> CreateSession(string userId)
        {
            string sessionToken = Guid.NewGuid().ToString();
            
            // kljuc: "session:guid" => vrednost: "userId"
            string key = $"session:{sessionToken}";

            await _redisDb.StringSetAsync(key, userId, TimeSpan.FromMinutes(30));

            return sessionToken;
        }

        public async Task<string?> GetUserIdFromSession(string sessionToken)
        {
            string key = $"session:{sessionToken}";
            
            var userId = await _redisDb.StringGetAsync(key);

            if (userId.HasValue)
            {
                await _redisDb.KeyExpireAsync(key, TimeSpan.FromMinutes(30));
                return userId.ToString();
            }

            return null;
        }

        public async Task Logout(string sessionToken)
        {
            string key = $"session:{sessionToken}";
            await _redisDb.KeyDeleteAsync(key);
        }

        public async Task<User?> UpdateUser(string userId, User userUpdates)
        {
            await using var session = _driver.AsyncSession();

            var query = @"
                MATCH (u:User {id: $userId})
                SET u.name = $name,
                    u.email = $email,
                    u.interests = $interests
                RETURN u.id as id, u.name as name, u.email as email, u.interests as interests";

            var result = await session.RunAsync(query, new
            {
                userId,
                name = userUpdates.Name,
                email = userUpdates.Email,
                interests = userUpdates.Interests
            });

            if (await result.FetchAsync())
            {
                // Vraćamo ažurirano stanje
                // Napomena: Role se ovde ne menja (to bi trebalo biti u zasebnoj admin metodi)
                return new User
                {
                    Id = result.Current["id"].As<string>(),
                    Name = result.Current["name"].As<string>(),
                    Email = result.Current["email"].As<string>(),
                    Interests = result.Current["interests"].As<List<string>>(),
                    Role = userUpdates.Role // Ili fetchuj ponovo ako je bitno
                };
            }

            return null;
        }
    }
}