using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class EfPlayerRepository : IPlayerRepository
{
    private readonly BattleTanksDbContext _context;

    public EfPlayerRepository(BattleTanksDbContext context)
    {
        _context = context;
    }

    public async Task<Player?> GetByIdAsync(Guid id)
    {
        return await _context.Players
            .Include(p => p.User)
            .Include(p => p.GameSession)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Player?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Players
            .Include(p => p.User)
            .Include(p => p.GameSession)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<Player?> GetByConnectionIdAsync(string connectionId)
    {
        return await _context.Players
            .Include(p => p.User)
            .Include(p => p.GameSession)
            .FirstOrDefaultAsync(p => p.ConnectionId == connectionId);
    }

    public async Task<List<Player>> GetByGameSessionIdAsync(Guid gameSessionId)
    {
        return await _context.Players
            .Include(p => p.User)
            .Where(p => p.GameSessionId == gameSessionId)
            .ToListAsync();
    }

    public async Task<Player?> GetActivePlayerByUserIdAsync(Guid userId)
    {
        return await _context.Players
            .Include(p => p.User)
            .Include(p => p.GameSession)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.GameSessionId != null);
    }

    public async Task AddAsync(Player player)
    {
        await _context.Players.AddAsync(player);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Player player)
    {
        _context.Players.Update(player);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var player = await _context.Players.FindAsync(id);
        if (player != null)
        {
            _context.Players.Remove(player);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteByUserIdAsync(Guid userId)
    {
        var players = await _context.Players
            .Where(p => p.UserId == userId)
            .ToListAsync();

        _context.Players.RemoveRange(players);
        await _context.SaveChangesAsync();
    }
}