namespace Application.DTOs;

public enum MapTileType { Empty = 0, WallIndestructible = 1, WallDestructible = 2 }

public record MapTileDto(int X, int Y, MapTileType Type, int Hp);
// Hp: usar 2 sólo si Type == WallDestructible; 0 en otros casos.

public record MapSnapshotDto(string RoomId, int Width, int Height, int TileSize, List<MapTileDto> Tiles);