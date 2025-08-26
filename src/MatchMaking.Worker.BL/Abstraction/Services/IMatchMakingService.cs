using MatchMaking.Common.Messages;

namespace MatchMaking.Worker.BL.Abstraction.Services;

public interface IMatchMakingService
{
    Task HandleRequestAsync(MatchMakingRequestMessage message, CancellationToken ct);
}