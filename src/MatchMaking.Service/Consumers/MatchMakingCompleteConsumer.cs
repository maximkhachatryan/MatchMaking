using Confluent.Kafka;
using MatchMaking.Common.Constants;
using MatchMaking.Common.Messages;
using MatchMaking.Common.Serialization;
using MatchMaking.Service.BL.Abstraction.Services;

namespace MatchMaking.Service.Consumers;

public class MatchMakingCompleteConsumer(
    IConfiguration configuration,
    IMatchCompleteHandlerService matchCompleteHandlerService,
    ILogger<MatchMakingCompleteConsumer> logger) : BackgroundService
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
                    var consumerResult = matchCompleteConsumer.Consume(TimeSpan.FromMilliseconds(100));
                    if (consumerResult == null)
                    {
                        await Task.Delay(50, stoppingToken);
                        continue;
                    }

                    var message = consumerResult.Message.Value;
                    logger.LogInformation($"Received match: Id = {message.MatchId}, UserIds = {string.Join(",", message.UserIds)}");

                    await matchCompleteHandlerService.HandleMatchCompleteAsync(message, stoppingToken);
                    
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error processing match complete message.");
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