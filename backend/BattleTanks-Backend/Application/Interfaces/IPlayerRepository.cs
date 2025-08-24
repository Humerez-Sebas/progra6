using Domain.Entities;

namespace Application.Interfaces;

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(Guid id);
    Task<Player?> GetByUserIdAsync(Guid userId);
    Task<Player?> GetByConnectionIdAsync(string connectionId);
    Task<List<Player>> GetByGameSessionIdAsync(Guid gameSessionId);
    Task<Player?> GetActivePlayerByUserIdAsync(Guid userId);
    Task AddAsync(Player player);
    Task UpdateAsync(Player player);
    Task DeleteAsync(Guid id);
    Task DeleteByUserIdAsync(Guid userId);
}