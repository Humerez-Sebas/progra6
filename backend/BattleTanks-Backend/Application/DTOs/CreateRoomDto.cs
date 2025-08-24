namespace Application.DTOs;

public record CreateRoomDto(
    string Name,
    int MaxPlayers = 4,
    bool IsPublic = true,
    string? CreatorName = null
);
