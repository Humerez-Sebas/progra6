namespace Application.DTOs;

public record RoomStateDto(
    string RoomId,
    string RoomCode,
    string Region,
    string Status,
    List<PlayerStateDto> Players
);