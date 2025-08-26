using Confluent.Kafka;
using MatchMaking.Common.Constants;
using MatchMaking.Common.Messages;
using MatchMaking.Common.Serialization;
using MatchMaking.Worker.BL.Abstraction.Services;
using MatchMaking.Worker.BL.Constants;
using StackExchange.Redis;

namespace MatchMaking.Worker.Consumers;

public class MatchMakingRequestConsumer(
    IConfiguration configuration,
    ILogger<MatchMakingRequestConsumer> logger,
    IMatchMakingService matchMakingService)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var matchRequestConsumer = SubscribeConsumer(configuration);
        
        try
        {
            var redisConnString = configuration["Redis:ConnectionString"]!;
            //var redis = await ConnectionMultiplexer.ConnectAsync(redisConnString);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumerResult = matchRequestConsumer.Consume(TimeSpan.FromMilliseconds(100));
                    if (consumerResult == null)
                    {
                        await Task.Delay(50, stoppingToken);
                        continue;
                    }

                    var message = consumerResult.Message.Value;
                    logger.LogInformation($"Received message with userId: {message.UserId}");

                    
                    await matchMakingService.HandleRequestAsync(message, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, $"Kafka error: {ex.Error}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error processing match complete message.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("MatchMakingRequestConsumer is stopping.");
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
}