using MatchMaking.Worker.DAL.Abstraction.Repositories;
using MatchMaking.Worker.DAL.Constants;
using StackExchange.Redis;

namespace MatchMaking.Worker.DAL.Repositories;

public class WorkerRepository(IConnectionMultiplexer redis) : IWorkerRepository
{
    private readonly IDatabase _db = redis.GetDatabase();

    public Task LineUpUser(string userId)
        => _db.ListRightPushAsync(MatchMakingWorkerRedisKeys.WaitingRequests, userId);

    public async Task<string[]?> PopUsers(int count)
    {
        var res = await _db.ScriptEvaluateAsync(
            LuaScripts.LPopNItemsIfExist,
            keys: [MatchMakingWorkerRedisKeys.WaitingRequests],
            values: [count.ToString()]
        );

        if (res.IsNull)
            return null;

        var result = (RedisResult[])res!;
        return result.Select(x => x.ToString()).ToArray();
    }
}