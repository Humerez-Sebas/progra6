using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using System.Collections.Generic;
using System.Linq;
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
            .AsNoTracking()
            .Include(gs => gs.Players).ThenInclude(p => p.User)
            .Include(gs => gs.Scores)
            .FirstOrDefaultAsync(gs => gs.Id == id);
    }

    public async Task<GameSession?> GetByCodeAsync(string code)
    {
        return await _context.GameSessions
            .AsNoTracking()
            .Include(gs => gs.Players).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(gs => gs.Code == code);
    }

    public async Task<List<GameSession>> GetActiveSessionsAsync(string? region = null)
    {
        var query = _context.GameSessions
            .AsNoTracking()
            .Include(gs => gs.Players).ThenInclude(p => p.User)
            .Where(gs => gs.Status == GameRoomStatus.Waiting || gs.Status == GameRoomStatus.InProgress);

        if (!string.IsNullOrEmpty(region))
            query = query.Where(gs => gs.Region == region);

        return await query
            .OrderByDescending(gs => gs.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<GameSession>> GetSessionsByStatusAsync(GameRoomStatus status)
    {
        return await _context.GameSessions
            .AsNoTracking()
            .Include(gs => gs.Players).ThenInclude(p => p.User)
            .Where(gs => gs.Status == status)
            .OrderByDescending(gs => gs.CreatedAt)
            .ToListAsync();
    }

    public async Task<GameSession?> GetSessionWithPlayersAsync(Guid id)
    {
        return await _context.GameSessions
            .AsNoTracking()
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
        var affected = await _context.GameSessions
            .Where(gs => gs.Id == session.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(gs => gs.Status, session.Status)
                .SetProperty(gs => gs.StartedAt, session.StartedAt)
                .SetProperty(gs => gs.EndedAt, session.EndedAt));

        if (affected == 0)
            throw new KeyNotFoundException("Game session not found");
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

    public async Task<(List<GameSession> Items, int Total)> GetSessionsPagedAsync(bool onlyPublic, int page, int pageSize, GameRoomStatus? status = null, string? region = null)
    {
        var query = _context.GameSessions
            .AsNoTracking()
            .Include(gs => gs.Players).ThenInclude(p => p.User)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(gs => gs.Status == status.Value);

        if (onlyPublic)
            query = query.Where(gs => gs.IsPublic);

        if (!string.IsNullOrEmpty(region))
            query = query.Where(gs => gs.Region == region);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(gs => gs.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }
}
