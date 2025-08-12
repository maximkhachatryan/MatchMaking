namespace MatchMaking.Common.Messages;

public record MatchMakingCompleteMessage(string MatchId, string[] UserIds);