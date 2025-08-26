using Confluent.Kafka;
using MatchMaking.Common.Constants;
using MatchMaking.Common.Messages;
using MatchMaking.Worker.BL.Abstraction.Services;
using MatchMaking.Worker.DAL.Abstraction.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MatchMaking.Worker.BL.Services;

public class MatchMakingProcessorProcessorService(
    ILogger<MatchMakingProcessorProcessorService> logger,
    IWorkerRepository repository,
    IConfiguration configuration,
    IProducer<Null, MatchMakingCompleteMessage> producer)
    : IMatchMakingProcessorService
{
    private readonly int _matchSize = configuration.GetValue<int>("MatchSize");

    public async Task ProcessRequestAsync(MatchMakingRequestMessage message, CancellationToken ct)
    {
        await repository.LineUpUser(message.UserId);
        
        //Making Matches while relevant number of waiting users exist
        while (true)
        {
            //Popping exactly n (n=_matchSize) users from database if exist in atomic way.
            var poppedUserIds = await repository.PopUsers(_matchSize);
            if (poppedUserIds == null)
                return;

            var matchId = Guid.NewGuid().ToString();
            var completeMessage = new MatchMakingCompleteMessage(matchId, poppedUserIds);

            logger.LogInformation("Match completed: {MatchCompleteMessage}", completeMessage);

            await producer.ProduceAsync(
                KafkaTopics.KafkaCompleteTopic,
                new Message<Null, MatchMakingCompleteMessage> { Value = completeMessage },
                ct
            );
        }
    }
}