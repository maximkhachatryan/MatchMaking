using MatchMaking.Common.Messages;
using MatchMaking.Service.BL.Abstraction.Services;
using MatchMaking.Service.BL.Constants;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MatchMaking.Service.BL.Services;

public class MatchCompleteHandlerService : IMatchCompleteHandlerService
{
    private readonly IDatabase _db;
    private readonly ILogger<MatchCompleteHandlerService> _logger;
    
    public MatchCompleteHandlerService(IConnectionMultiplexer redis,
        ILogger<MatchCompleteHandlerService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }
    
    public async Task HandleMatchCompleteAsync(MatchMakingCompleteMessage message, CancellationToken ct)
    {
        var hashEntries = message.UserIds.Select(uid => new HashEntry(uid, message.MatchId)).ToArray();
        await _db.HashSetAsync(MatchMakingServiceRedisKeys.UserMatchHashKey, hashEntries);
        var matchUsersKey = string.Format(MatchMakingServiceRedisKeys.MatchUsersListKey, message.MatchId);
        var redisUserIds = message.UserIds.Select(u => (RedisValue)u).ToArray();
        await _db.ListRightPushAsync(matchUsersKey, redisUserIds);
        await _db.SetRemoveAsync(MatchMakingServiceRedisKeys.WaitingUsersSetKey, redisUserIds);
    }
}