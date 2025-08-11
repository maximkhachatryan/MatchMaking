using Confluent.Kafka;
using MatchMaking.Common.Constants;
using MatchMaking.Common.Messages;
using MatchMaking.Common.Serialization;
using MatchMaking.Worker.Constants;
using StackExchange.Redis;

namespace MatchMaking.Worker.Consumers;

public class MatchMakingRequestConsumer(IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var redisConnString = configuration["Redis:ConnectionString"]!;
        var redis = await ConnectionMultiplexer.ConnectAsync(redisConnString);

        using var matchRequestConsumer = SubscribeConsumer(configuration);
        using var matchCompleteProducer = CreateProducer(configuration);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumerResult = matchRequestConsumer.Consume(stoppingToken);
                    var message = consumerResult.Message.Value;
                    Console.WriteLine($"Received message with userId: {message.UserId}");

                    var db = redis.GetDatabase();
                    await db.ListRightPushAsync("matchmaking.worker.waitingUsers", message.UserId);


                    while (true)
                    {
                        //Used Lua script to check count and pop n items in atomic way
                        var matchUsersCount = configuration.GetValue<int>("MatchUsersCount");
                        var res = await db.ScriptEvaluateAsync(
                            LuaScripts.LPopNItemsIfExist,
                            keys: [Constants.Constants.RedisKeyWaitingUsers],
                            values: [matchUsersCount.ToString()]
                        );
                        if (res.IsNull)
                        {
                            break;
                        }
                        var result = (RedisResult[])res!;

                        var matchId = Guid.NewGuid().ToString();
                        var completeMessage =
                            new MatchMakingCompleteMessage(matchId, result.Select(x=>x.ToString()).ToArray());

                        Console.WriteLine(completeMessage);
                        
                        await matchCompleteProducer.ProduceAsync(
                            KafkaTopics.KafkaCompleteTopic,
                            new Message<Null, MatchMakingCompleteMessage>
                            {
                                Value = completeMessage
                            }, stoppingToken);
                    }
                }
                catch (ConsumeException ex)
                {
                    Console.WriteLine($"Kafka error: {ex.Error.Reason}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            matchRequestConsumer.Close();
        }
    }

    private IConsumer<Ignore, MatchMakingRequestMessage> SubscribeConsumer(IConfiguration c)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = c["Kafka:BootstrapServers"],
            GroupId = "matchmaking-worker-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };
        var consumer = new ConsumerBuilder<Ignore, MatchMakingRequestMessage>(config)
            .SetValueDeserializer(new KafkaJsonDeserializer<MatchMakingRequestMessage>())
            .Build();

        consumer.Subscribe(KafkaTopics.KafkaRequestTopic);
        return consumer;
    }
    
    private IProducer<Null, MatchMakingCompleteMessage> CreateProducer(IConfiguration c)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = c["Kafka:BootstrapServers"]
        };
        return new ProducerBuilder<Null, MatchMakingCompleteMessage>(config)
            .SetValueSerializer(new KafkaJsonSerializer<MatchMakingCompleteMessage>())
            .Build();
    }
}