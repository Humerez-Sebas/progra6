/*using Domain.Entities;
using Domain.Enums;
using Application.Interfaces;
using System.Collections.Concurrent;

namespace Infrastructure.Persistence.InMemory;

public class InMemoryGameSessionRepository : IGameSessionRepository
{
    private readonly ConcurrentDictionary<Guid, GameRoom> _rooms = new();
    
    public Task<GameRoom?> GetByIdAsync(Guid id)
    {
        _rooms.TryGetValue(id, out var room);
        return Task.FromResult(room);
    }
    
    public Task<GameRoom?> GetByCodeAsync(string code)
    {
        var room = _rooms.Values.FirstOrDefault(r => r.Code == code);
        return Task.FromResult(room);
    }
    
    public Task<List<GameRoom>> GetActiveRoomsAsync()
    {
        var activeRooms = _rooms.Values
            .Where(r => r.Status != GameRoomStatus.Finished)
            .ToList();
        return Task.FromResult(activeRooms);
    }
    
    public Task AddAsync(GameRoom room)
    {
        _rooms.TryAdd(room.Id, room);
        return Task.CompletedTask;
    }
    
    public Task UpdateAsync(GameRoom room)
    {
        _rooms.TryUpdate(room.Id, room, room);
        return Task.CompletedTask;
    }
    
    public Task DeleteAsync(Guid id)
    {
        _rooms.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}*/