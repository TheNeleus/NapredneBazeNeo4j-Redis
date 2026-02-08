using Neo4j.Driver;
using StackExchange.Redis;
using MeetupBackend.Services; 
using MeetupBackend.Hubs;    
using MeetupBackend.Workers; 

var builder = WebApplication.CreateBuilder(args);

var redisConnString = builder.Configuration.GetConnectionString("Redis");
var redisConfig = ConfigurationOptions.Parse(redisConnString);
var redisConnection = ConnectionMultiplexer.Connect(redisConfig);

builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

var neo4jUri = builder.Configuration["ConnectionStrings:Neo4jUri"];
var neo4jUser = builder.Configuration["ConnectionStrings:Neo4jUser"];
var neo4jPass = builder.Configuration["ConnectionStrings:Neo4jPass"];

var neo4jDriver = GraphDatabase.Driver(neo4jUri, AuthTokens.Basic(neo4jUser, neo4jPass));
builder.Services.AddSingleton<IDriver>(neo4jDriver);


builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<UserService>();
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