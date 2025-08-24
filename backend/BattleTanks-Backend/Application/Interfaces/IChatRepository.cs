using Domain.Entities;

namespace Application.Interfaces;

public interface IChatRepository
{
    Task<ChatMessage?> GetByIdAsync(Guid id);
    Task<List<ChatMessage>> GetRoomMessagesAsync(Guid roomId, int limit = 50);
    Task AddAsync(ChatMessage message);
}