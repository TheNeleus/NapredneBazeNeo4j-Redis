using Neo4j.Driver;
using StackExchange.Redis;
using MeetupBackend.Services; // Dodaj ovo
using MeetupBackend.Hubs;     // Dodaj ovo
using MeetupBackend.Workers;  // Dodaj ovo
var builder = WebApplication.CreateBuilder(args);

// --- 1. REDIS KONEKCIJA ---
// Povezujemo se na localhost:6379
var redisConfig = ConfigurationOptions.Parse("localhost:6379");
var redisConnection = ConnectionMultiplexer.Connect(redisConfig);
// Registrujemo kao Singleton (jedna konekcija za celu aplikaciju)
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

// --- 2. NEO4J KONEKCIJA ---
// Povezujemo se na bolt://localhost:7687
// PAŽNJA: Promeni "tajnaSifra123" ako si stavio drugu u docker-compose!
var neo4jDriver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "tajnaSifra123"));
builder.Services.AddSingleton<IDriver>(neo4jDriver);

// Registrujemo naš servis da bi ga Kontroler mogao koristiti
builder.Services.AddScoped<MeetupBackend.Services.EventService>();
builder.Services.AddScoped<MeetupBackend.Services.UserService>();

builder.Services.AddScoped<ChatService>();
builder.Services.AddSignalR();

// DVA Background Workera:
// 1. Za prebacivanje iz Redisa u Neo4j (Clean-up)
builder.Services.AddHostedService<ChatPersistenceWorker>();
// 2. Za Pub/Sub slušanje (Real-time distribution)
builder.Services.AddHostedService<RedisSubscriberService>();


// Dodajemo kontrolere i Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(builder => {
        builder.WithOrigins("http://localhost:3000") // URL tvog frontenda
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials(); 
    });
});

var app = builder.Build();

app.UseCors();

// Konfiguracija HTTP pipeline-a
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
// ... mapiranje
app.MapHub<ChatHub>("/chat");

// Obavezno oslobađanje resursa kad se aplikacija ugasi
app.Lifetime.ApplicationStopped.Register(() =>
{
    redisConnection.Dispose();
    neo4jDriver.Dispose();
});

app.Run();