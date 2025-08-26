using MatchMaking.Service.BL.Models;

namespace MatchMaking.Service.BL.Abstraction.Services;

public interface IMatchMakingService
{
    Task SearchMatch(string userId);
    Task<MatchDto> RetrieveMatchInformation(string userId);
}