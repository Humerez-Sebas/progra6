using Application.Interfaces;
using Domain.Entities;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

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
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.GameSession)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Player?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Players
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.GameSession)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<Player?> GetByConnectionIdAsync(string connectionId)
    {
        return await _context.Players
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.GameSession)
            .FirstOrDefaultAsync(p => p.ConnectionId == connectionId);
    }

    public async Task<List<Player>> GetByGameSessionIdAsync(Guid gameSessionId)
    {
        return await _context.Players
            .AsNoTracking()
            .Include(p => p.User)
            .Where(p => p.GameSessionId == gameSessionId)
            .ToListAsync();
    }

    public async Task<Player?> GetActivePlayerByUserIdAsync(Guid userId)
    {
        return await _context.Players
            .AsNoTracking()
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
        var existing = await _context.Players.FindAsync(player.Id);
        if (existing is null)
            throw new KeyNotFoundException("Player not found");

        _context.Entry(existing).CurrentValues.SetValues(player);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<Player> players)
    {
        foreach (var player in players)
        {
            var existing = await _context.Players.FindAsync(player.Id);
            if (existing is null)
                throw new KeyNotFoundException("Player not found");

            _context.Entry(existing).CurrentValues.SetValues(player);
        }

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
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync();

        await _context.BulkDeleteAsync(players);
    }
}