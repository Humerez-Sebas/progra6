using Domain.Entities;

namespace Application.Interfaces;

public interface IScoreRepository
{
    Task<Score?> GetByIdAsync(Guid id);
    Task<List<Score>> GetByUserIdAsync(Guid userId);
    Task<List<Score>> GetByGameSessionIdAsync(Guid gameSessionId);
    Task<List<Score>> GetTopScoresAsync(int limit = 10);
    Task<List<Score>> GetUserTopScoresAsync(Guid userId, int limit = 10);
    Task AddAsync(Score score);
    Task AddRangeAsync(IEnumerable<Score> scores);
    Task UpdateAsync(Score score);
    Task DeleteAsync(Guid id);
}