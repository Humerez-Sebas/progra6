namespace Application.DTOs;

public record ChatMessageDto(
    string MessageId,
    Guid UserId,
    string Username,
    string Content,
    string Type,
    DateTime SentAt
);
