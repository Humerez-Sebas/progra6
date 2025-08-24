using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Interfaces;

namespace Infrastructure.SignalR.Services;

public class InMemoryPowerUpService : IPowerUpService
{
    private readonly IMapService _map;
    private readonly IMqttService _mqtt;
    private readonly IRedisService _redis;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PowerUpDto>> _byRoom = new();
    private readonly Random _rand = new();

    public InMemoryPowerUpService(IMapService map, IMqttService mqtt, IRedisService redis)
    {
        _map = map;
        _mqtt = mqtt;
        _redis = redis;
    }

    public PowerUpDto SpawnRandom(string roomCode, string roomId)
    {
        var (width, height) = _map.GetSize(roomId);
        var tileSize = _map.TileSize;
        int tx, ty;
        do
        {
            tx = _rand.Next(width);
            ty = _rand.Next(height);
        } while (_map.IsSolid(roomId, tx, ty));

        var powerUp = new PowerUpDto(
            Guid.NewGuid().ToString(),
            roomId,
            PowerUpType.ExtraLife,
            (tx + 0.5f) * tileSize,
            (ty + 0.5f) * tileSize
        );

        var room = _byRoom.GetOrAdd(roomCode, _ => new ConcurrentDictionary<string, PowerUpDto>());
        room[powerUp.Id] = powerUp;

        var payload = JsonSerializer.Serialize(powerUp);
        _ = _mqtt.PublishAsync("powerups/spawned", payload);
        _ = _redis.SaveAsync("powerups", payload);
        return powerUp;
    }

    public IReadOnlyCollection<PowerUpDto> GetActive(string roomCode)
    {
        return _byRoom.TryGetValue(roomCode, out var room)
            ? room.Values.ToArray()
            : Array.Empty<PowerUpDto>();
    }

    public bool TryConsume(string roomCode, string userId, float x, float y, out PowerUpDto? powerUp)
    {
        powerUp = null;
        if (!_byRoom.TryGetValue(roomCode, out var room)) return false;
        var tileSize = _map.TileSize;
        foreach (var kvp in room)
        {
            var p = kvp.Value;
            if (Math.Abs(p.X - x) <= tileSize / 2f && Math.Abs(p.Y - y) <= tileSize / 2f)
            {
                room.TryRemove(kvp.Key, out _);
                powerUp = p;
                var payload = JsonSerializer.Serialize(new { roomCode, userId, powerUpId = p.Id });
                _ = _mqtt.PublishAsync("powerups/consumed", payload);
                _ = _redis.SaveAsync("powerups", payload);
                return true;
            }
        }
        return false;
    }
}
