using Neo4j.Driver;
using StackExchange.Redis;
using MeetupBackend.Services; 
using MeetupBackend.Hubs;    
using MeetupBackend.Workers; 
var builder = WebApplication.CreateBuilder(args);


var redisConfig = ConfigurationOptions.Parse("localhost:6379");
var redisConnection = ConnectionMultiplexer.Connect(redisConfig);

builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);


var neo4jDriver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "tajnaSifra123"));
builder.Services.AddSingleton<IDriver>(neo4jDriver);


builder.Services.AddScoped<MeetupBackend.Services.EventService>();
builder.Services.AddScoped<MeetupBackend.Services.UserService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddSignalR();


builder.Services.AddHostedService<ChatPersistenceWorker>();
builder.Services.AddHostedService<RedisSubscriberService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(builder => {
        builder.WithOrigins("http://localhost:5173")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials(); 
    });
});

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();

app.MapHub<ChatHub>("/chatHub");

app.Lifetime.ApplicationStopped.Register(() =>
{
    redisConnection.Dispose();
    neo4jDriver.Dispose();
});

app.Run();