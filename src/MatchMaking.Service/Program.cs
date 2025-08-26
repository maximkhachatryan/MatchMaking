using Confluent.Kafka;
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

var kafkaConfig = new ProducerConfig
{
    BootstrapServers = builder.Configuration.GetValue<string>("Kafka:BootstrapServers")
};

builder.Services.AddSingleton<IProducer<Null, string>>(_ => new ProducerBuilder<Null, string>(kafkaConfig).Build());

builder.Services.AddSingleton<IServiceRepository, ServiceRepository>();
builder.Services.AddSingleton<IMatchCompleteHandlerService, MatchCompleteHandlerService>();
builder.Services.AddScoped<IMatchMakingService, MatchMakingService>();
var app = builder.Build();
app.MapControllers();

app.Run();