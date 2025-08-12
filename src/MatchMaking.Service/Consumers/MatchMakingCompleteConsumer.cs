using Confluent.Kafka;
using MatchMaking.Common.Constants;
using MatchMaking.Common.Messages;
using MatchMaking.Common.Serialization;
using StackExchange.Redis;

namespace MatchMaking.Service.Consumers;

public class MatchMakingCompleteConsumer(IConfiguration configuration, IConnectionMultiplexer redis, ILogger<MatchMakingCompleteConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MatchMakingCompleteConsumer started.");
        
        using var matchCompleteConsumer = SubscribeConsumer(configuration);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
                logger.LogInformation("MatchMakingCompleteConsumer running...");
                try
                {
                    var consumerResult = matchCompleteConsumer.Consume(stoppingToken);
                    var message = consumerResult.Message.Value;
                    Console.WriteLine($"Received complete message: {message}");

                    var db = redis.GetDatabase();
                    var hashEntries = message.UserIds.Select(uid => new HashEntry(uid, message.MatchId)).ToArray();
                    await db.HashSetAsync("user_match_map", hashEntries);

                    var matchUsersKey = $"match:{message.MatchId}:users";

                    var redisUserIds = message.UserIds.Select(u => (RedisValue)u).ToArray();
                    await db.ListRightPushAsync(matchUsersKey, redisUserIds);
                    
                    await db.SetRemoveAsync("waiting:users", redisUserIds);
                }
                catch (ConsumeException ex)
                {
                    logger.LogInformation("MatchMakingCompleteConsumer stopping due to cancellation.");
                    Console.WriteLine($"Kafka error: {ex.Error.Reason}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            matchCompleteConsumer.Close();
        }
    }

    private IConsumer<Ignore, MatchMakingCompleteMessage> SubscribeConsumer(IConfiguration c)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = c["Kafka:BootstrapServers"],
            GroupId = "matchmaking-service-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };
        var consumer = new ConsumerBuilder<Ignore, MatchMakingCompleteMessage>(config)
            .SetValueDeserializer(new KafkaJsonDeserializer<MatchMakingCompleteMessage>())
            .Build();

        consumer.Subscribe(KafkaTopics.KafkaCompleteTopic);
        return consumer;
    }
}