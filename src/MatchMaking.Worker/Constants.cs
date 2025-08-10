namespace MatchMaking.Worker;

public static class Constants
{
    public const string KafkaRequestTopic = "matchmaking.request";
    public const string KafkaCompleteTopic = "matchmaking.complete";
    public const string RedisKeyWaitingUsers = "matchmaking.worker.waitingusers";
}