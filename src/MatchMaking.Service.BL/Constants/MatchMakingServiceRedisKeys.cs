namespace MatchMaking.Service.BL.Constants;

public static class MatchMakingServiceRedisKeys
{
    public const string MatchUsersListKey = "MatchMakingService:match:{0}:users";
    public const string UserMatchHashKey = "MatchMakingService:user_match";
    public const string WaitingUsersSetKey = "MatchMakingService:waiting-users";
}