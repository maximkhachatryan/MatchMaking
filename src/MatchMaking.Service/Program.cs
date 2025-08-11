using Confluent.Kafka;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
// {
//     var configuration = builder.Configuration.GetValue<string>("Redis:ConnectionString")!;
//     return ConnectionMultiplexer.Connect(configuration);
// });

var kafkaConfig = new ProducerConfig
{
    BootstrapServers = builder.Configuration.GetValue<string>("Kafka:BootstrapServers")
};

builder.Services.AddSingleton<IProducer<Null, string>>(_ => new ProducerBuilder<Null, string>(kafkaConfig).Build());
var app = builder.Build();
app.MapControllers();

app.Run();