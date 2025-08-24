using Domain.Enums;

namespace Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid UserId { get; private set; }
    public string Username { get; private set; }
    public string Content { get; private set; }
    public MessageType Type { get; private set; }
    public DateTime SentAt { get; private set; }

    private ChatMessage() { }

    public static ChatMessage CreateUserMessage(Guid roomId, Guid userId, string username, string content)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            UserId = userId,
            Username = username,
            Content = content,
            Type = MessageType.User,
            SentAt = DateTime.UtcNow
        };
    }

    public static ChatMessage CreateSystemMessage(Guid roomId, string content)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            UserId = Guid.Empty,
            Username = "System",
            Content = content,
            Type = MessageType.System,
            SentAt = DateTime.UtcNow
        };
    }
}
