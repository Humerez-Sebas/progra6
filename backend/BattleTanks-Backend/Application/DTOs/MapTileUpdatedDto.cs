namespace Application.DTOs;

public record MapTileUpdatedDto(
    string RoomId,
    int X,
    int Y,
    MapTileType Type,
    int Hp
);