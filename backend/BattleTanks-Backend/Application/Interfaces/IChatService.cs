using Application.DTOs;

namespace Application.Interfaces;

public interface IChatService
{
    Task<ChatMessageDto> SendMessage(string roomId, string userId, string content);
    Task<List<ChatMessageDto>> GetRoomMessages(string roomId, int limit = 50);
}