using Application.DTOs;

namespace Application.Interfaces;

public interface IMapService
{
    Task<MapSnapshotDto> GetSnapshotAsync(string roomId);
    Task SetSnapshotAsync(MapSnapshotDto snapshot);
    int TileSize { get; }
    (int width, int height) GetSize(string roomId);

    // 🔽 NUEVO: utilidades para colisiones/daño
    bool TryGetTile(string roomId, int tx, int ty, out MapTileDto tile);
    bool TryDamageDestructible(string roomId, int tx, int ty, out MapTileDto updated);
    bool IsSolid(string roomId, int tx, int ty);

    (float x, float y)[] GetSpawnPoints(string roomId);
}