namespace Application.DTOs;

public record RoomStateDto(
    string RoomId,
    string RoomCode,
    string Status,
    List<PlayerStateDto> Players
);