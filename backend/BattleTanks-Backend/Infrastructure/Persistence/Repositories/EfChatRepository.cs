using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class EfChatRepository : IChatRepository
{
    private readonly BattleTanksDbContext _context;

    public EfChatRepository(BattleTanksDbContext context)
    {
        _context = context;
    }

    public async Task<ChatMessage?> GetByIdAsync(Guid id)
    {
        return await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<ChatMessage>> GetRoomMessagesAsync(Guid roomId, int limit = 50)
    {
        return await _context.ChatMessages
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    public async Task AddAsync(ChatMessage message)
    {
        await _context.ChatMessages.AddAsync(message);
        await _context.SaveChangesAsync();
    }
}