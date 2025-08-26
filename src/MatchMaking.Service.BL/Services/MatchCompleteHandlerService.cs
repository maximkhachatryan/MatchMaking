using MatchMaking.Common.Messages;
using MatchMaking.Service.BL.Abstraction.Services;
using MatchMaking.Service.BL.Constants;
using MatchMaking.Service.DAL.Abstraction.Repositories;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MatchMaking.Service.BL.Services;

public class MatchCompleteHandlerService(
    ILogger<MatchCompleteHandlerService> logger,
    IServiceRepository serviceRepository)
    : IMatchCompleteHandlerService
{
    public async Task HandleMatchCompleteAsync(MatchMakingCompleteMessage message, CancellationToken ct)
    {
        //Store this mapping to quickly access last made matchId of the user
        await serviceRepository.StoreUserIdMatchIdMapping(
            message.UserIds.Select(uid => (uid, message.MatchId))
                .ToArray());

        //Storing match details
        await serviceRepository.StoreMatchDetails(message.MatchId, message.UserIds);
        
        //Releasing the users that are already included in a match from the waitingUsers list,
        //so they are allowed to request for a new match 
        await serviceRepository.ReleaseWaitingUsers(message.UserIds);
    }
}