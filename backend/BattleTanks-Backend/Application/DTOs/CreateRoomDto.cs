namespace Application.DTOs;

public record CreateRoomDto(
    string Name,
    string Region = "global",
    int MaxPlayers = 4,
    bool IsPublic = true,
    string? CreatorName = null
);
