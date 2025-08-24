/*using Domain.Entities;
using Application.Interfaces;
using System.Collections.Concurrent;

namespace Infrastructure.Persistence.InMemory;

public class InMemoryPlayerRepository : IPlayerRepository
{
    private readonly ConcurrentDictionary<Guid, Player> _players = new();
    
    public Task<Player?> GetByIdAsync(Guid id)
    {
        _players.TryGetValue(id, out var player);
        return Task.FromResult(player);
    }
    
    public Task<Player?> GetByUserIdAsync(string userId)
    {
        var player = _players.Values.FirstOrDefault(p => p.UserId == userId);
        return Task.FromResult(player);
    }
    
    public Task<Player?> GetByConnectionIdAsync(string connectionId)
    {
        var player = _players.Values.FirstOrDefault(p => p.ConnectionId == connectionId);
        return Task.FromResult(player);
    }
    
    public Task AddAsync(Player player)
    {
        _players.TryAdd(player.Id, player);
        return Task.CompletedTask;
    }
    
    public Task UpdateAsync(Player player)
    {
        _players.TryUpdate(player.Id, player, player);
        return Task.CompletedTask;
    }
    
    public Task DeleteAsync(Guid id)
    {
        _players.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}*/