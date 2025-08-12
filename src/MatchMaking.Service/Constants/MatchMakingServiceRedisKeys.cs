namespace MatchMaking.Service.Constants;

public static class MatchMakingServiceRedisKeys
{
    public const string MatchUsersListKey = "match:{0}:users";
    public const string UserMatchHashKey = "user_match_map";
    public const string WaitingUsersSetKey = "waiting:users";
}