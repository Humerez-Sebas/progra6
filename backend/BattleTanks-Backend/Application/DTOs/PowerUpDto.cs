namespace Application.DTOs;

public enum PowerUpType
{
    ExtraLife
}

public record PowerUpDto(
    string Id,
    string RoomId,
    PowerUpType Type,
    float X,
    float Y
);
