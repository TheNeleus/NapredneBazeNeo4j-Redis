using MeetupBackend.Hubs;
using MeetupBackend.Models;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using System.Text.Json;

namespace MeetupBackend.Services
{
    public class RedisSubscriberService : BackgroundService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IHubContext<ChatHub> _hubContext;
        private const string PUBSUB_CHANNEL = "chat:events:live";

        public RedisSubscriberService(IConnectionMultiplexer redis, IHubContext<ChatHub> hubContext)
        {
            _redis = redis;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var subscriber = _redis.GetSubscriber();

            await subscriber.SubscribeAsync(PUBSUB_CHANNEL, async (channel, value) =>
            {
                var message = JsonSerializer.Deserialize<EventChatMessage>(value.ToString());
                if (message != null)
                {
                    // Kada stigne poruka iz Redisa, posalji je korisnicima u odredjenoj Grupi
                    // "ReceiveMessage" je ime metode koju frontend slusa
                    await _hubContext.Clients.Group(message.EventId)
                        .SendAsync("ReceiveMessage", message);
                }
            });

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}