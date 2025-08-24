using System.Security.Claims;
using Application.DTOs;
using Application.Interfaces;
using Infrastructure.SignalR.Abstractions;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.SignalR.Hubs;

public partial class GameHub : Hub
{
    private readonly IConnectionTracker _tracker;
    private readonly IRoomRegistry _rooms;
    private readonly IMapService _map;
    private readonly IBulletService _bulletService;
    private readonly IPowerUpService _powerUps;
    private readonly ILifeService _lifeService;

    public GameHub(IConnectionTracker tracker, IRoomRegistry rooms, IMapService map, IBulletService bullets, IPowerUpService powerUps, ILifeService lifeService)
    {
        _tracker = tracker;
        _rooms = rooms;
        _map = map;
        _bulletService = bullets;
        _powerUps = powerUps;
        _lifeService = lifeService;
    }
    
    public async Task JoinRoom(string roomCode, string? username = null)
    {
        var userId = Context.User?.FindFirst("user_id")?.Value
                     ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? Guid.NewGuid().ToString();

        var uname = Context.User?.FindFirst("username")?.Value
                    ?? Context.User?.FindFirst(ClaimTypes.Name)?.Value
                    ?? (string.IsNullOrWhiteSpace(username) ? $"Player-{userId[..8]}" : username.Trim());

        try
        {
            await _rooms.JoinAsync(roomCode, userId, uname, Context.ConnectionId);
        }
        catch
        {
            throw new HubException("room_not_found");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

        var roomSnap = await _rooms.GetByCodeAsync(roomCode);
        _tracker.Set(Context.ConnectionId, roomSnap!.RoomId, roomCode, userId, uname);
        await Clients.Group(roomCode).SendAsync("playerJoined", new { userId, username = uname });

        await Clients.Caller.SendAsync("roomSnapshot", new
        {
            roomId = roomSnap.RoomId,
            roomCode = roomSnap.RoomCode,
            players = roomSnap.Players.Values.ToArray()
        });

        var mapSnap = await _map.GetSnapshotAsync(roomSnap.RoomId);
        await Clients.Caller.SendAsync("mapSnapshot", mapSnap);

        var powerUps = _powerUps.GetActive(roomCode);
        if (powerUps.Count == 0)
        {
            var spawned = _powerUps.SpawnRandom(roomCode, roomSnap.RoomId);
            powerUps = new[] { spawned };
            await Clients.Group(roomCode).SendAsync("powerUpSpawned", spawned);
        }
        await Clients.Caller.SendAsync("powerUpsSnapshot", powerUps);
    }


    public async Task LeaveRoom()
    {
        var left = await _rooms.LeaveByConnectionAsync(Context.ConnectionId);
        if (!string.IsNullOrEmpty(left.roomCode) && !string.IsNullOrEmpty(left.userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, left.roomCode!);
            _tracker.Remove(Context.ConnectionId);
            await Clients.Group(left.roomCode!).SendAsync("playerLeft", left.userId!);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try { await LeaveRoom(); } catch { }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task UpdatePosition(PlayerPositionDto position)
    {
        if (!_tracker.TryGet(Context.ConnectionId, out var info)) throw new HubException("not_in_room");

        if (float.IsNaN(position.X) || float.IsInfinity(position.X) ||
            float.IsNaN(position.Y) || float.IsInfinity(position.Y) ||
            float.IsNaN(position.Rotation) || float.IsInfinity(position.Rotation)) return;

        var fixedDto = new PlayerPositionDto(
            info.UserId,
            position.X,
            position.Y,
            position.Rotation,
            position.Timestamp > 0 ? position.Timestamp : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );

        await _rooms.UpdatePlayerPositionAsync(info.RoomCode, info.UserId, position.X, position.Y, position.Rotation);

        await Clients.Group(info.RoomCode).SendAsync("playerMoved", fixedDto);

        if (_powerUps.TryConsume(info.RoomCode, info.UserId, position.X, position.Y, out var pu))
        {
            var newLives = _lifeService.AddLife(info.RoomId, info.UserId, 1);
            await Clients.Group(info.RoomCode).SendAsync("powerUpCollected", new { powerUpId = pu!.Id, userId = info.UserId });
            await Clients.Group(info.RoomCode).SendAsync("playerLifeLost", new PlayerLifeLostDto(info.UserId, newLives, false));
            var spawned = _powerUps.SpawnRandom(info.RoomCode, info.RoomId);
            await Clients.Group(info.RoomCode).SendAsync("powerUpSpawned", spawned);
        }
    }
}
