using System.Collections.Concurrent;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.SignalR.Services;

public class InMemoryMapService : IMapService
{
    private readonly ConcurrentDictionary<string, MapSnapshotDto> _maps = new();

    private readonly int _defaultTileSize;
    private readonly int _defaultWidthTiles;
    private readonly int _defaultHeightTiles;

    public int TileSize => _defaultTileSize;

    public InMemoryMapService(IConfiguration cfg)
    {
        var mapWidthPx  = cfg.GetValue<int?>("GameSettings:MapWidth")  ?? cfg.GetValue<int>("GameSettings:MapWidthPx", 800);
        var mapHeightPx = cfg.GetValue<int?>("GameSettings:MapHeight") ?? cfg.GetValue<int>("GameSettings:MapHeightPx", 600);
        _defaultTileSize    = cfg.GetValue<int>("GameSettings:TileSize", 40);
        _defaultWidthTiles  = Math.Max(1, mapWidthPx  / _defaultTileSize);
        _defaultHeightTiles = Math.Max(1, mapHeightPx / _defaultTileSize);
    }

    public Task<MapSnapshotDto> GetSnapshotAsync(string roomId)
    {
        var snap = _maps.GetOrAdd(roomId, _ => GenerateDefault(roomId));
        return Task.FromResult(snap);
    }

    public Task SetSnapshotAsync(MapSnapshotDto snapshot)
    {
        var tileSize = snapshot.TileSize > 0 ? snapshot.TileSize : _defaultTileSize;
        _maps[snapshot.RoomId] = snapshot with { TileSize = tileSize };
        return Task.CompletedTask;
    }

    public (int width, int height) GetSize(string roomId)
    {
        var snap = _maps.GetOrAdd(roomId, _ => GenerateDefault(roomId));
        return (snap.Width, snap.Height);
    }

    public bool TryGetTile(string roomId, int tx, int ty, out MapTileDto tile)
    {
        tile = default!;
        if (!_maps.TryGetValue(roomId, out var snap)) return false;
        if (tx < 0 || ty < 0 || tx >= snap.Width || ty >= snap.Height) return false;
        // tiles no están indexados por default; indexamos en memoria para O(1)
        // (como estamos in-memory, una búsqueda lineal también funcionaría, pero hacemos cache local)

        // simple búsqueda lineal (suficiente por ahora):
        var found = snap.Tiles.FirstOrDefault(t => t.X == tx && t.Y == ty);
        if (found is null)
        {
            // si no hay tile registrado, se asume Empty hp=0
            tile = new MapTileDto(tx, ty, MapTileType.Empty, 0);
            return true;
        }

        tile = found;
        return true;
    }

    public bool IsSolid(string roomId, int tx, int ty)
    {
        if (!TryGetTile(roomId, tx, ty, out var t)) return true;
        return t.Type == MapTileType.WallIndestructible || (t.Type == MapTileType.WallDestructible && t.Hp > 0);
    }

    public bool TryDamageDestructible(string roomId, int tx, int ty, out MapTileDto updated)
    {
        updated = default!;
        if (!_maps.TryGetValue(roomId, out var snap)) return false;

        var idx = snap.Tiles.FindIndex(t => t.X == tx && t.Y == ty);
        if (idx < 0)
        {
            return false;
        }

        var t = snap.Tiles[idx];
        if (t.Type != MapTileType.WallDestructible || t.Hp <= 0) return false;

        var newHp = Math.Max(0, t.Hp - 1);
        var newType = newHp > 0 ? t.Type : MapTileType.Empty;
        var newTile = new MapTileDto(tx, ty, newType, newHp);
        snap.Tiles[idx] = newTile;
        
        _maps[roomId] = snap with { Tiles = snap.Tiles };

        updated = newTile;
        return true;
    }

    public (float x, float y)[] GetSpawnPoints(string roomId)
    {
        var (w, h) = GetSize(roomId);
        var ts = TileSize;
        return new (float, float)[]
        {
            (ts * 1.5f, ts * 1.5f),
            (ts * (w - 1.5f), ts * 1.5f),
            (ts * 1.5f, ts * (h - 1.5f)),
            (ts * (w - 1.5f), ts * (h - 1.5f))
        };
    }

    private MapSnapshotDto GenerateDefault(string roomId)
    {
        var tiles = new List<MapTileDto>();
        
        for (int x = 0; x < _defaultWidthTiles; x++)
        {
            tiles.Add(new MapTileDto(x, 0, MapTileType.WallIndestructible, 0));
            tiles.Add(new MapTileDto(x, _defaultHeightTiles - 1, MapTileType.WallIndestructible, 0));
        }
        for (int y = 0; y < _defaultHeightTiles; y++)
        {
            tiles.Add(new MapTileDto(0, y, MapTileType.WallIndestructible, 0));
            tiles.Add(new MapTileDto(_defaultWidthTiles - 1, y, MapTileType.WallIndestructible, 0));
        }
        
        for (int x = 2; x < _defaultWidthTiles - 2; x += 2)
        {
            for (int y = 2; y < _defaultHeightTiles - 2; y += 2)
            {
                tiles.Add(new MapTileDto(x, y, MapTileType.WallDestructible, 2));
            }
        }

        var centerX = _defaultWidthTiles / 2;
        var centerY = _defaultHeightTiles / 2;
        tiles.Add(new MapTileDto(centerX, centerY, MapTileType.WallIndestructible, 0));
        tiles.Add(new MapTileDto(centerX - 1, centerY, MapTileType.WallIndestructible, 0));
        tiles.Add(new MapTileDto(centerX + 1, centerY, MapTileType.WallIndestructible, 0));
        tiles.Add(new MapTileDto(centerX, centerY - 1, MapTileType.WallIndestructible, 0));
        tiles.Add(new MapTileDto(centerX, centerY + 1, MapTileType.WallIndestructible, 0));

        return new MapSnapshotDto(roomId, _defaultWidthTiles, _defaultHeightTiles, _defaultTileSize, tiles);
    }
}
