using MatchMaking.Worker.Consumers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<MatchMakingRequestConsumer>();
var app = builder.Build();
app.Run();