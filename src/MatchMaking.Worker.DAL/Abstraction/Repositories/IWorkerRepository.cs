namespace MatchMaking.Worker.DAL.Abstraction.Repositories;

public interface IWorkerRepository
{
    Task LineUpUser(string userId);
    Task<string[]?> PopUsers(int count);
}