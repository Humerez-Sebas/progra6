using System.Diagnostics;
using Application.DTOs;
using Application.Interfaces;
using Infrastructure.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Background;

public class BulletSimulationService : BackgroundService
{
    private readonly ILogger<BulletSimulationService> _log;
    private readonly IBulletService _bullets;
    private readonly IMapService _map;
    private readonly IHubContext<GameHub> _hub;
    private readonly IServiceScopeFactory _scopeFactory;


    private readonly Dictionary<string, Dictionary<string, int>> _lives = new();

    private const float BulletRadius = 2.5f;
    private const float TankHalfW = 12f;
    private const float TankHalfH = 8f;

    public BulletSimulationService(
        ILogger<BulletSimulationService> log,
        IBulletService bullets,
        IMapService map,
        IHubContext<GameHub> hub,
        IServiceScopeFactory scopeFactory)
    {
        _log = log;
        _bullets = bullets;
        _map = map;
        _hub = hub;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sw = new Stopwatch();
        sw.Start();
        long last = sw.ElapsedMilliseconds;

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var game = scope.ServiceProvider.GetRequiredService<IGameService>();

            var now = sw.ElapsedMilliseconds;
            var dt = Math.Clamp((now - last) / 1000f, 0f, 0.05f);
            last = now;

            Step(dt, game);

            await Task.Delay(16, stoppingToken); // ~60Hz
        }
    }

    private void Step(float dt, IGameService game)
    {
        foreach (var (roomCode, bulletId, b) in _bullets.EnumerateAll().ToList())
        {
            if (!b.IsActive)
            {
                _bullets.Despawn(roomCode, bulletId);
                continue;
            }

            var nx = b.X + MathF.Cos(b.DirectionRadians) * b.Speed * dt;
            var ny = b.Y + MathF.Sin(b.DirectionRadians) * b.Speed * dt;

            var snap = game.GetRoomByCode(roomCode).GetAwaiter().GetResult();
            if (snap is null)
            {
                _bullets.Despawn(roomCode, bulletId);
                _ = _hub.Clients.Group(roomCode).SendAsync("bulletDespawned", bulletId, "room_gone");
                continue;
            }

            var ts = _map.TileSize;
            var (tilesW, tilesH) = _map.GetSize(snap.RoomId);
            var maxX = tilesW * ts;
            var maxY = tilesH * ts;
            
            if (nx < 0 || ny < 0 || nx >= maxX || ny >= maxY)
            {
                _bullets.Despawn(roomCode, bulletId);
                _ = _hub.Clients.Group(roomCode).SendAsync("bulletDespawned", bulletId, "out");
                continue;
            }
            
            var tx = (int)(nx / ts);
            var ty = (int)(ny / ts);
            if (_map.IsSolid(snap.RoomId, tx, ty))
            {
                if (_map.TryDamageDestructible(snap.RoomId, tx, ty, out var updated))
                {
                    _ = _hub.Clients.Group(roomCode).SendAsync("mapTileUpdated", new
                    {
                        roomId = snap.RoomId,
                        x = updated.X,
                        y = updated.Y,
                        type = (int)updated.Type,
                        hp = updated.Hp
                    });
                }

                _bullets.Despawn(roomCode, bulletId);
                _ = _hub.Clients.Group(roomCode).SendAsync("bulletDespawned", bulletId, "wall");
                continue;
            }
            
            var players = snap.Players.ToDictionary(p => p.PlayerId);
            var hitPlayerId = FindHitPlayer(players, nx, ny, b.ShooterId);
            if (hitPlayerId is not null)
            {
                var lives = GetLivesDict(snap.RoomId);
                var l = lives.TryGetValue(hitPlayerId, out var prev) ? prev : 3;
                l = Math.Max(0, l - 1);
                lives[hitPlayerId] = l;

                _ = _hub.Clients.Group(roomCode).SendAsync("playerLifeLost",
                    new PlayerLifeLostDto(hitPlayerId, l, l == 0));

                _bullets.Despawn(roomCode, bulletId);
                _ = _hub.Clients.Group(roomCode).SendAsync("bulletDespawned", bulletId, "hit");

                if (l > 0)
                {
                    var rx = ts * 2f;
                    var ry = ts * 2f;
                    _ = _hub.Clients.Group(roomCode).SendAsync("playerRespawned",
                        new PlayerRespawnedDto(hitPlayerId, rx, ry));
                }
                continue;
            }

            _bullets.UpdateBullet(roomCode, bulletId, b with { X = nx, Y = ny });
        }
    }

    private static string? FindHitPlayer(
        IReadOnlyDictionary<string, PlayerStateDto> players,
        float x, float y,
        string shooterId)
    {
        foreach (var p in players.Values)
        {
            if (p.PlayerId == shooterId) continue;

            float left   = p.X - TankHalfW;
            float right  = p.X + TankHalfW;
            float top    = p.Y - TankHalfH;
            float bottom = p.Y + TankHalfH;

            bool withinX = x >= left  - BulletRadius && x <= right  + BulletRadius;
            bool withinY = y >= top   - BulletRadius && y <= bottom + BulletRadius;

            if (withinX && withinY)
                return p.PlayerId;
        }
        return null;
    }

    private Dictionary<string, int> GetLivesDict(string roomId)
    {
        if (!_lives.TryGetValue(roomId, out var d))
        {
            d = new Dictionary<string, int>();
            _lives[roomId] = d;
        }
        return d;
    }
}
