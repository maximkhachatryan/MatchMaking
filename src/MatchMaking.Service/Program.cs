using Confluent.Kafka;
using MatchMaking.Common.Messages;
using MatchMaking.Common.Serialization;
using MatchMaking.Service.BL.Abstraction.Services;
using MatchMaking.Service.BL.Services;
using MatchMaking.Service.Consumers;
using MatchMaking.Service.DAL.Abstraction.Repositories;
using MatchMaking.Service.DAL.Repositories;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<MatchMakingCompleteConsumer>();
builder.Services.AddControllers();
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var configuration = builder.Configuration.GetValue<string>("Redis:ConnectionString")!;
    return ConnectionMultiplexer.Connect(configuration);
});

// register Kafka producer once for app lifetime
builder.Services.AddSingleton<IProducer<Null, MatchMakingRequestMessage>>(sp =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
    };
    return new ProducerBuilder<Null, MatchMakingRequestMessage>(config)
        .SetValueSerializer(new KafkaJsonSerializer<MatchMakingRequestMessage>())
        .Build();
});

builder.Services.AddSingleton<IServiceRepository, ServiceRepository>();
builder.Services.AddSingleton<IMatchCompleteHandlerService, MatchCompleteHandlerService>();
builder.Services.AddScoped<IMatchMakingService, MatchMakingService>();
var app = builder.Build();
app.MapControllers();

app.Run();