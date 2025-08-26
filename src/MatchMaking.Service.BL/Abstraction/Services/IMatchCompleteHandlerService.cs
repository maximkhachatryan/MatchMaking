using MatchMaking.Common.Messages;

namespace MatchMaking.Service.BL.Abstraction.Services;

public interface IMatchCompleteHandlerService
{
    Task HandleMatchCompleteAsync(MatchMakingCompleteMessage message, CancellationToken ct);
}