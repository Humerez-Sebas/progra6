using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class EfGameSessionRepository : IGameSessionRepository
{
    private readonly BattleTanksDbContext _context;

    public EfGameSessionRepository(BattleTanksDbContext context)
    {
        _context = context;
    }

    public async Task<GameSession?> GetByIdAsync(Guid id)
    {
        return await _context.GameSessions
            .Include(gs => gs.Players).ThenInclude(p => p.User)
            .Include(gs => gs.Scores)
            .FirstOrDefaultAsync(gs => gs.Id == id);
    }

    public async Task<GameSession?> GetByCodeAsync(string code)
    {
        return await _context.GameSessions
            .Include(gs => gs.Players).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(gs => gs.Code == code);
    }

    public async Task<List<GameSession>> GetActiveSessionsAsync()
    {
        return await _context.GameSessions
            .Include(gs => gs.Players).ThenInclude(p => p.User)
            .Where(gs => gs.Status == GameRoomStatus.Waiting || gs.Status == GameRoomStatus.InProgress)
            .OrderByDescending(gs => gs.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<GameSession>> GetSessionsByStatusAsync(GameRoomStatus status)
    {
        return await _context.GameSessions
            .Include(gs => gs.Players).ThenInclude(p => p.User)
            .Where(gs => gs.Status == status)
            .OrderByDescending(gs => gs.CreatedAt)
            .ToListAsync();
    }

    public async Task<GameSession?> GetSessionWithPlayersAsync(Guid id)
    {
        return await _context.GameSessions
            .Include(gs => gs.Players).ThenInclude(p => p.User)
            .Include(gs => gs.Scores)
            .FirstOrDefaultAsync(gs => gs.Id == id);
    }

    public async Task AddAsync(GameSession session)
    {
        await _context.GameSessions.AddAsync(session);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(GameSession session)
    {
        _context.GameSessions.Update(session);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var session = await _context.GameSessions.FindAsync(id);
        if (session != null)
        {
            _context.GameSessions.Remove(session);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<(List<GameSession> Items, int Total)> GetSessionsPagedAsync(bool onlyPublic, int page, int pageSize, GameRoomStatus? status = null)
    {
        var query = _context.GameSessions
            .Include(gs => gs.Players).ThenInclude(p => p.User)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(gs => gs.Status == status.Value);

        if (onlyPublic)
            query = query.Where(gs => gs.IsPublic);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(gs => gs.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }
}
