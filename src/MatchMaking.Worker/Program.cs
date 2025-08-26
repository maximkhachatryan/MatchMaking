using Confluent.Kafka;
using MatchMaking.Common.Messages;
using MatchMaking.Common.Serialization;
using MatchMaking.Worker.BL.Abstraction.Services;
using MatchMaking.Worker.BL.Services;
using MatchMaking.Worker.Consumers;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!)
);

// register Kafka producer once for app lifetime
builder.Services.AddSingleton<IProducer<Null, MatchMakingCompleteMessage>>(sp =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
    };
    return new ProducerBuilder<Null, MatchMakingCompleteMessage>(config)
        .SetValueSerializer(new KafkaJsonSerializer<MatchMakingCompleteMessage>())
        .Build();
});

builder.Services.AddSingleton<IMatchMakingService, MatchMakingService>();
builder.Services.AddHostedService<MatchMakingRequestConsumer>();
var app = builder.Build();
app.Run();