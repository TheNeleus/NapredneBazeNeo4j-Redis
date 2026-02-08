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
            var existing = await GetUserByEmail(user.Email);
            if (existing != null)
            {
                throw new InvalidOperationException("Email already in use.");
            }

            user.Id = Guid.NewGuid().ToString();

            user.Role = "User";

            await using var session = _driver.AsyncSession();

            var query = @"
                CREATE (u:User {
                    id: $id, 
                    name: $name, 
                    email: $email, 
                    interests: $interests,
                    bio: $bio
                }) 
                WITH u
                MERGE (r:Role {name: $roleName})
                MERGE (u)-[:HAS_ROLE]->(r)
                RETURN u.id as id, u.name as name, u.email as email, u.interests as interests, u.bio as bio, r.name as role";

            var result = await session.RunAsync(query, new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email,
                interests = user.Interests,
                bio = user.Bio,
                roleName = user.Role
            });

            var record = await result.SingleAsync();

            return new User
            {
                Id = record["id"].As<string>(),
                Name = record["name"].As<string>(),
                Email = record["email"].As<string>(),
                Interests = record["interests"].As<List<string>>(),
                Bio = record["bio"].As<string>(),
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

        public async Task<string> CreateSession(string userId)
        {
            string sessionToken = Guid.NewGuid().ToString();

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
                    u.bio = $bio,  
                    u.interests = $interests
                RETURN u.id as id, u.name as name, u.email as email, u.bio as bio, u.interests as interests";

            var result = await session.RunAsync(query, new
            {
                userId,
                name = userUpdates.Name,
                email = userUpdates.Email,
                bio = userUpdates.Bio, 
                interests = userUpdates.Interests
            });

            if (await result.FetchAsync())
            {
                var record = result.Current;
                return new User
                {
                    Id = record["id"].As<string>(),
                    Name = record["name"].As<string>(),
                    Email = record["email"].As<string>(),
                    Bio = record.ContainsKey("bio") && record["bio"] != null ? record["bio"].As<string>() : "",
                    Interests = record["interests"].As<List<string>>(),
                    Role = userUpdates.Role
                };
            }

            return null;
        }
        public async Task<string> AddFriendByEmail(string currentUserId, string friendEmail)
        {
            await using var session = _driver.AsyncSession();

            var findQuery = @"MATCH (u:User {email: $email}) RETURN u.id as id";
            var findResult = await session.RunAsync(findQuery, new { email = friendEmail });

            if (!await findResult.FetchAsync()) 
                return "User not found"; 

            string friendId = findResult.Current["id"].As<string>();

            if (friendId == currentUserId) 
                return "Cannot add yourself";

            var relateQuery = @"
                MATCH (u1:User {id: $u1Id})
                MATCH (u2:User {id: $u2Id})
                MERGE (u1)-[:FRIEND]->(u2)
                RETURN u2.name";

            await session.RunAsync(relateQuery, new { u1Id = currentUserId, u2Id = friendId });

            return "OK";
        }
    }
}