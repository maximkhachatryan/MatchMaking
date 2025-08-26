using Confluent.Kafka;
using MatchMaking.Common.Constants;
using MatchMaking.Common.Messages;
using MatchMaking.Worker.BL.Abstraction.Services;
using MatchMaking.Worker.BL.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MatchMaking.Worker.BL.Services;

public class MatchMakingProcessorProcessorService : IMatchMakingProcessorService
{
    private readonly ILogger<MatchMakingProcessorProcessorService> _logger;
    private readonly IDatabase _db;
    private readonly int _matchSize;
    private readonly IProducer<Null, MatchMakingCompleteMessage> _producer;

    public MatchMakingProcessorProcessorService(
        ILogger<MatchMakingProcessorProcessorService> logger,
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        IProducer<Null, MatchMakingCompleteMessage> producer)
    {
        _logger = logger;
        _db = redis.GetDatabase();
        _matchSize = configuration.GetValue<int>("MatchSize");
        _producer = producer;
    }

    public async Task ProcessRequestAsync(MatchMakingRequestMessage message, CancellationToken ct)
    {
        await _db.ListRightPushAsync(MatchMakingWorkerRedisKeys.WaitingRequests, message.UserId);

        while (true)
        {
            //Used Lua script to check count and pop n items in atomic way
            var res = await _db.ScriptEvaluateAsync(
                LuaScripts.LPopNItemsIfExist,
                keys: [MatchMakingWorkerRedisKeys.WaitingRequests],
                values: [_matchSize.ToString()]
            );

            if (res.IsNull)
                return;

            var result = (RedisResult[])res!;
            var matchId = Guid.NewGuid().ToString();
            var completeMessage = new MatchMakingCompleteMessage(
                matchId,
                result.Select(x => x.ToString()).ToArray()
            );

            _logger.LogInformation("Match completed: {MatchCompleteMessage}", completeMessage);

            await _producer.ProduceAsync(
                KafkaTopics.KafkaCompleteTopic,
                new Message<Null, MatchMakingCompleteMessage> { Value = completeMessage },
                ct
            );
        }
    }
}