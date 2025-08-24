/*using Domain.Entities;
using Application.Interfaces;
using System.Collections.Concurrent;

namespace Infrastructure.Persistence.InMemory;

public class InMemoryChatRepository : IChatRepository
{
    private readonly ConcurrentDictionary<Guid, ChatMessage> _messages = new();
    
    public Task<ChatMessage?> GetByIdAsync(Guid id)
    {
        _messages.TryGetValue(id, out var message);
        return Task.FromResult(message);
    }
    
    public Task<List<ChatMessage>> GetRoomMessagesAsync(Guid roomId, int limit = 50)
    {
        var messages = _messages.Values
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .OrderBy(m => m.SentAt)
            .ToList();
            
        return Task.FromResult(messages);
    }
    
    public Task AddAsync(ChatMessage message)
    {
        _messages.TryAdd(message.Id, message);
        return Task.CompletedTask;
    }
}
*/