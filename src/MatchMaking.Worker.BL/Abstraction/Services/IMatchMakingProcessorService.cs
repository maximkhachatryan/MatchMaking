using MatchMaking.Common.Messages;

namespace MatchMaking.Worker.BL.Abstraction.Services;

public interface IMatchMakingProcessorService
{
    Task ProcessRequestAsync(MatchMakingRequestMessage message, CancellationToken ct);
}