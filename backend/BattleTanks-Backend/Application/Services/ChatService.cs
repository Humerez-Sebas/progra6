using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services;

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IGameNotificationService _notificationService;

    public ChatService(
        IChatRepository chatRepository,
        IGameSessionRepository gameSessionRepository,
        IGameNotificationService notificationService)
    {
        _chatRepository = chatRepository;
        _gameSessionRepository = gameSessionRepository;
        _notificationService = notificationService;
    }

    public async Task<ChatMessageDto> SendMessage(string roomId, string userId, string content)
    {
        if (!Guid.TryParse(roomId, out var roomGuid))
            throw new ArgumentException("Invalid room ID format");

        if (!Guid.TryParse(userId, out var userGuid))
            throw new ArgumentException("Invalid user ID format");

        var session = await _gameSessionRepository.GetByIdAsync(roomGuid);
        if (session == null)
            throw new InvalidOperationException("Game session not found");

        var player = session.GetPlayer(userGuid);
        if (player == null)
            throw new InvalidOperationException("Player not in session");

        var message = ChatMessage.CreateUserMessage(
            session.Id,
            userGuid,
            player.Username,
            content
        );

        await _chatRepository.AddAsync(message);

        var messageDto = MapToChatMessageDto(message);
        await _notificationService.NotifyChatMessage(roomId, messageDto);

        return messageDto;
    }

    public async Task<List<ChatMessageDto>> GetRoomMessages(string roomId, int limit = 50)
    {
        if (!Guid.TryParse(roomId, out var roomGuid))
            throw new ArgumentException("Invalid room ID format");

        var messages = await _chatRepository.GetRoomMessagesAsync(roomGuid, limit);
        return messages.Select(MapToChatMessageDto).ToList();
    }

    private ChatMessageDto MapToChatMessageDto(ChatMessage message)
    {
        return new ChatMessageDto(
            message.Id.ToString(),
            message.UserId,
            message.Username,
            message.Content,
            message.Type.ToString(),
            message.SentAt
        );
    }
}
