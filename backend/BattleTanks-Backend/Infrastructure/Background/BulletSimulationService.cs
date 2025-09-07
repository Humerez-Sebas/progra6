using System.Diagnostics;
using Application.DTOs;
using Application.Interfaces;
using Infrastructure.SignalR.Abstractions;
using Infrastructure.SignalR.Hubs;
using Domain.Enums;
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
    private readonly IRoomRegistry _rooms;
    private readonly IHubContext<GameHub> _hub;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IScoreRegistry _scoreRegistry;

    private const float BulletRadius = 2.5f;
    private const float TankHalfW = 12f;
    private const float TankHalfH = 8f;

    public BulletSimulationService(
        ILogger<BulletSimulationService> log,
        IBulletService bullets,
        IMapService map,
        IRoomRegistry rooms,
        IHubContext<GameHub> hub,
        IServiceScopeFactory scopeFactory,
        IScoreRegistry scoreRegistry)
    {
        _log = log;
        _bullets = bullets;
        _map = map;
        _rooms = rooms;
        _hub = hub;
        _scopeFactory = scopeFactory;
        _scoreRegistry = scoreRegistry;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sw = new Stopwatch();
        sw.Start();
        long last = sw.ElapsedMilliseconds;

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = sw.ElapsedMilliseconds;
            var dt = Math.Clamp((now - last) / 1000f, 0f, 0.05f);
            last = now;

            Step(dt);

            await Task.Delay(16, stoppingToken); // ~60Hz
        }
    }

    private void Step(float dt)
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
            
            var snap = _rooms.GetByCodeAsync(roomCode).GetAwaiter().GetResult();
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

                    var sc = _scoreRegistry.AddScore(snap.RoomId, b.ShooterId, 50);
                    _ = _hub.Clients.Group(roomCode).SendAsync("playerScored",
                        new PlayerScoredDto(b.ShooterId, sc));
                    InvokeGameService(game => game.AwardWallPoints(snap.RoomId, b.ShooterId, 50));
                }

                _bullets.Despawn(roomCode, bulletId);
                _ = _hub.Clients.Group(roomCode).SendAsync("bulletDespawned", bulletId, "wall");
                continue;
            }
            
            var hitPlayerId = FindHitPlayer(snap.Players, nx, ny, b.ShooterId);
            if (hitPlayerId is not null)
            {
                var l = _scoreRegistry.AddLife(snap.RoomId, hitPlayerId, -1);

                _ = _hub.Clients.Group(roomCode).SendAsync("playerLifeLost",
                    new PlayerLifeLostDto(hitPlayerId, l, l == 0));

                var sc = _scoreRegistry.AddScore(snap.RoomId, b.ShooterId, 150);
                _ = _hub.Clients.Group(roomCode).SendAsync("playerScored",
                    new PlayerScoredDto(b.ShooterId, sc));
                InvokeGameService(game => game.RegisterKill(snap.RoomId, b.ShooterId, hitPlayerId, 150));

                _bullets.Despawn(roomCode, bulletId);
                _ = _hub.Clients.Group(roomCode).SendAsync("bulletDespawned", bulletId, "hit");

                if (l > 0)
                {
                    var spawns = _map.GetSpawnPoints(snap.RoomId);
                    var rnd = new Random();
                    var spawn = spawns[rnd.Next(spawns.Length)];
                    _ = _rooms.UpdatePlayerPositionAsync(roomCode, hitPlayerId, spawn.x, spawn.y, 0f);
                    _ = _hub.Clients.Group(roomCode).SendAsync("playerRespawned",
                        new PlayerRespawnedDto(hitPlayerId, spawn.x, spawn.y));
                }
                else
                {
                    var remaining = _scoreRegistry.GetLives(snap.RoomId).Values.Count(v => v > 0);
                    if (remaining <= 1)
                    {
                        var scores = _scoreRegistry.GetScores(snap.RoomId);
                        var winner = scores.OrderByDescending(k => k.Value).FirstOrDefault().Key ?? hitPlayerId;
                        InvokeGameService(async game => await game.EndGame(snap.RoomId));
                        var final = scores.Select(kvp => new PlayerScoreDto(kvp.Key, kvp.Value)).ToList();
                        _ = _hub.Clients.Group(roomCode).SendAsync("gameEnded",
                            new GameEndedDto(winner, final));
                    }
                }
                continue;
            }

            _bullets.UpdateBullet(roomCode, bulletId, b with { X = nx, Y = ny });
        }
    }

    private void InvokeGameService(Func<IGameService, Task> action)
    {
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var game = scope.ServiceProvider.GetRequiredService<IGameService>();
            await action(game);
        });
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

}