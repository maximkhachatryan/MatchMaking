using MatchMaking.Service.BL.Constants;
using MatchMaking.Service.DAL.Abstraction.Repositories;
using StackExchange.Redis;

namespace MatchMaking.Service.DAL.Repositories;

public class ServiceRepository(IConnectionMultiplexer redis) : IServiceRepository
{
    private readonly IDatabase _db = redis.GetDatabase();


    public Task StoreUserIdMatchIdMapping(params (string userId, string matchId)[] mappings)
    {
        var hashEntries = mappings.Select(m => new HashEntry(m.userId, m.matchId)).ToArray();
        return _db.HashSetAsync(MatchMakingServiceRedisKeys.UserMatchHashKey, hashEntries);
    }

    public async Task<string?> FindMatchIdByUserId(string userId)
    {
        var matchId = await _db.HashGetAsync(MatchMakingServiceRedisKeys.UserMatchHashKey, userId);
        return matchId.IsNullOrEmpty ? null : matchId.ToString();
    }


    public Task StoreMatchDetails(string matchId, string[] userIds)
    {
        var matchUsersKey = string.Format(MatchMakingServiceRedisKeys.MatchUsersListKey, matchId);
        var redisUserIds = userIds.Select(u => (RedisValue)u).ToArray();
        return _db.ListRightPushAsync(matchUsersKey, redisUserIds);
    }

    public async Task<List<string>> GetMatchDetails(string matchId)
    {
        var result = await _db.ListRangeAsync(string.Format(MatchMakingServiceRedisKeys.MatchUsersListKey, matchId));
        return result.Select(u => u.ToString()).ToList();
    }


    public Task RegisterWaitingUser(string userId)
    {
        return _db.SetAddAsync(MatchMakingServiceRedisKeys.WaitingUsersSetKey, userId);
    }

    public Task ReleaseWaitingUsers(string[] userIds)
    {
        var redisUserIds = userIds.Select(u => (RedisValue)u).ToArray();
        return _db.SetRemoveAsync(MatchMakingServiceRedisKeys.WaitingUsersSetKey, redisUserIds);
    }

    public Task<bool> CheckUserIsWaiting(string userId)
    {
        return _db.SetContainsAsync(MatchMakingServiceRedisKeys.WaitingUsersSetKey, userId);
    }
}