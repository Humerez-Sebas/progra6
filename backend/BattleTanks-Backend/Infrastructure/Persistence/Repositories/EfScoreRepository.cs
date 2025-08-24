using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class EfScoreRepository : IScoreRepository
{
    private readonly BattleTanksDbContext _context;

    public EfScoreRepository(BattleTanksDbContext context)
    {
        _context = context;
    }

    public async Task<Score?> GetByIdAsync(Guid id)
    {
        return await _context.Scores
            .Include(s => s.User)
            .Include(s => s.GameSession)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Score>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Scores
            .Include(s => s.GameSession)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.AchievedAt)
            .ToListAsync();
    }

    public async Task<List<Score>> GetByGameSessionIdAsync(Guid gameSessionId)
    {
        return await _context.Scores
            .Include(s => s.User)
            .Where(s => s.GameSessionId == gameSessionId)
            .OrderByDescending(s => s.Points)
            .ToListAsync();
    }

    public async Task<List<Score>> GetTopScoresAsync(int limit = 10)
    {
        return await _context.Scores
            .Include(s => s.User)
            .Include(s => s.GameSession)
            .OrderByDescending(s => s.Points)
            .ThenByDescending(s => s.AchievedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Score>> GetUserTopScoresAsync(Guid userId, int limit = 10)
    {
        return await _context.Scores
            .Include(s => s.GameSession)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Points)
            .ThenByDescending(s => s.AchievedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task AddAsync(Score score)
    {
        await _context.Scores.AddAsync(score);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Score> scores)
    {
        await _context.Scores.AddRangeAsync(scores);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Score score)
    {
        _context.Scores.Update(score);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var score = await _context.Scores.FindAsync(id);
        if (score != null)
        {
            _context.Scores.Remove(score);
            await _context.SaveChangesAsync();
        }
    }
}