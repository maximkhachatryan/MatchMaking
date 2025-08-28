namespace MatchMaking.Service.DAL.Abstraction.Repositories;

public interface IServiceRepository
{
    Task StoreUserIdMatchIdMapping(params (string userId, string matchId)[] mappings);
    Task<string?> FindMatchIdByUserId(string userId);
    
    
    Task StoreMatchDetails(string matchId, string[] userIds);
    Task<List<string>> GetMatchDetails(string matchId);
    
    
    Task RegisterWaitingUser(string userId);
    Task ReleaseWaitingUsers(string[] userIds);
    Task<bool> CheckUserIsWaiting(string userId);
}