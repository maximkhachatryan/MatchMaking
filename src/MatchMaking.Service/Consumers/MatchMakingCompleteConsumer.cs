using Confluent.Kafka;
using MatchMaking.Common.Constants;
using MatchMaking.Common.Messages;
using MatchMaking.Common.Serialization;
using MatchMaking.Service.Constants;
using StackExchange.Redis;

namespace MatchMaking.Service.Consumers;

public class MatchMakingCompleteConsumer(IConfiguration configuration, IConnectionMultiplexer redis, ILogger<MatchMakingCompleteConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        using var matchCompleteConsumer = SubscribeConsumer(configuration);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumerResult = matchCompleteConsumer.Consume(stoppingToken);
                    var message = consumerResult.Message.Value;
                    Console.WriteLine($"Received complete message: {message}");

                    var db = redis.GetDatabase();
                    var hashEntries = message.UserIds.Select(uid => new HashEntry(uid, message.MatchId)).ToArray();
                    await db.HashSetAsync(MatchMakingServiceRedisKeys.UserMatchHashKey, hashEntries);

                    var matchUsersKey = string.Format(MatchMakingServiceRedisKeys.MatchUsersListKey, message.MatchId);

                    var redisUserIds = message.UserIds.Select(u => (RedisValue)u).ToArray();
                    await db.ListRightPushAsync(matchUsersKey, redisUserIds);
                    
                    await db.SetRemoveAsync(MatchMakingServiceRedisKeys.WaitingUsersSetKey, redisUserIds);
                }
                catch (ConsumeException ex)
                {
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