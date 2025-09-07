using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IGameSessionRepository
{
    Task<GameSession?> GetByIdAsync(Guid id);
    Task<GameSession?> GetByCodeAsync(string code);
    Task<List<GameSession>> GetActiveSessionsAsync(string? region = null);
    Task<List<GameSession>> GetSessionsByStatusAsync(GameRoomStatus status);
    Task<GameSession?> GetSessionWithPlayersAsync(Guid id);
    Task AddAsync(GameSession session);
    Task UpdateAsync(GameSession session);
    Task DeleteAsync(Guid id);

    Task<(List<GameSession> Items, int Total)> GetSessionsPagedAsync(bool onlyPublic, int page, int pageSize, GameRoomStatus? status = null, string? region = null);
}
