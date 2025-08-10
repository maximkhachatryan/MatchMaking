using System.Reflection.Metadata;
using Confluent.Kafka;

namespace MatchMaking.Worker.Consumers;

public class MatchMakingRequestConsumer(IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kafkaConnString = configuration["Kafka:ConnectionString"]!;
        Console.WriteLine($"Using kafka connection string: {kafkaConnString}");
        
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaConnString,
            GroupId = "matchmaking-worker-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(Constants.KafkaRequestTopic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);
                    Console.WriteLine($"Received message: {cr.Message.Value}");
                }
                catch (ConsumeException ex)
                {
                    Console.WriteLine($"Kafka error: {ex.Error.Reason}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            consumer.Close();
        }
    }
}