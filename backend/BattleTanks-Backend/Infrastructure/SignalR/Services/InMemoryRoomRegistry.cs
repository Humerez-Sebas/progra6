using System.Collections.Concurrent;
using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Infrastructure.SignalR.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.SignalR.Services;

internal sealed class InMemoryRoomRegistry : IRoomRegistry
{
    private sealed class Room
    {
        public string RoomId { get; init; } = default!;
        public string RoomCode { get; init; } = default!;
        public string Name { get; set; } = "";
        public int MaxPlayers { get; set; }
        public bool IsPublic { get; set; }
        public string Status { get; set; } = GameRoomStatus.Waiting.ToString();
        public ConcurrentDictionary<string, PlayerStateDto> Players { get; } = new();
        public int NextSpawn { get; set; }
    }

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMapService _map;

    private readonly ConcurrentDictionary<string, Room> _byId = new();
    private readonly ConcurrentDictionary<string, string> _codeToId = new();
    private readonly ConcurrentDictionary<string, (string roomCode, string userId)> _connIndex = new();

    public InMemoryRoomRegistry(IServiceScopeFactory scopeFactory, IMapService map)
    {
        _scopeFactory = scopeFactory;
        _map = map;
    }

    public Task UpsertRoomAsync(string roomId, string roomCode, string name, int maxPlayers, bool isPublic, string status)
    {
        var room = _byId.AddOrUpdate(
            roomId,
            _ => new Room { RoomId = roomId, RoomCode = roomCode, Name = name, MaxPlayers = maxPlayers, IsPublic = isPublic, Status = status },
            (_, r) => { r.Name = name; r.MaxPlayers = maxPlayers; r.IsPublic = isPublic; r.Status = status; return r; }
        );
        _codeToId[roomCode] = roomId;
        return Task.CompletedTask;
    }

    public Task<RoomSnapshot?> GetByIdAsync(string roomId)
    {
        return Task.FromResult(_byId.TryGetValue(roomId, out var r) ? ToSnapshot(r) : null);
    }

    public Task<RoomSnapshot?> GetByCodeAsync(string roomCode)
    {
        if (_codeToId.TryGetValue(roomCode, out var id) && _byId.TryGetValue(id, out var r))
            return Task.FromResult<RoomSnapshot?>(ToSnapshot(r));
        return Task.FromResult<RoomSnapshot?>(null);
    }

    public async Task JoinAsync(string roomCode, string userId, string username, string connectionId)
    {
        var room = await EnsureRoomByCodeAsync(roomCode);
        var spawns = _map.GetSpawnPoints(room.RoomId);
        var spawn = spawns[room.NextSpawn % spawns.Length];
        room.NextSpawn++;
        var state = new PlayerStateDto(userId, username, spawn.x, spawn.y, 0, 100, true);
        room.Players[userId] = state;
        _connIndex[connectionId] = (roomCode, userId);
    }

    public Task<(string? roomCode, string? userId)> LeaveByConnectionAsync(string connectionId)
    {
        if (!_connIndex.TryRemove(connectionId, out var info))
            return Task.FromResult<(string?, string?)>((null, null));

        if (_codeToId.TryGetValue(info.roomCode, out var roomId) && _byId.TryGetValue(roomId, out var room))
            room.Players.TryRemove(info.userId, out _);

        return Task.FromResult((info.roomCode, info.userId));
    }

    public Task<IReadOnlyCollection<PlayerStateDto>> GetPlayersByIdAsync(string roomId)
    {
        if (_byId.TryGetValue(roomId, out var r))
            return Task.FromResult<IReadOnlyCollection<PlayerStateDto>>(r.Players.Values.ToArray());
        return Task.FromResult<IReadOnlyCollection<PlayerStateDto>>(Array.Empty<PlayerStateDto>());
    }

    public Task UpdatePlayerPositionAsync(string roomCode, string userId, float x, float y, float rotation)
    {
        if (_codeToId.TryGetValue(roomCode, out var roomId) &&
            _byId.TryGetValue(roomId, out var room) &&
            room.Players.TryGetValue(userId, out var state))
        {
            var newState = state with { X = x, Y = y, Rotation = rotation };
            room.Players[userId] = newState;
        }
        return Task.CompletedTask;
    }

    public Task<PlayerStateDto?> AddHealthAsync(string roomCode, string userId, int amount)
    {
        if (_codeToId.TryGetValue(roomCode, out var roomId) &&
            _byId.TryGetValue(roomId, out var room) &&
            room.Players.TryGetValue(userId, out var state))
        {
            var newState = state with { Health = state.Health + amount };
            room.Players[userId] = newState;
            return Task.FromResult<PlayerStateDto?>(newState);
        }
        return Task.FromResult<PlayerStateDto?>(null);
    }

    private async Task<Room> EnsureRoomByCodeAsync(string roomCode)
    {
        if (_codeToId.TryGetValue(roomCode, out var id) && _byId.TryGetValue(id, out var cached))
            return cached;

        using var scope = _scopeFactory.CreateScope();
        var sessions = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var session = await sessions.GetByCodeAsync(roomCode) ?? throw new InvalidOperationException("room_not_found");

        var room = _byId.GetOrAdd(session.Id.ToString(), _ =>
            new Room
            {
                RoomId = session.Id.ToString(),
                RoomCode = session.Code,
                Name = session.Name,
                MaxPlayers = session.MaxPlayers,
                IsPublic = session.IsPublic,
                Status = session.Status.ToString()
            });

        _codeToId[room.RoomCode] = room.RoomId;
        return room;
    }

    private static RoomSnapshot ToSnapshot(Room r) =>
        new RoomSnapshot(
            r.RoomId,
            r.RoomCode,
            r.Name,
            r.MaxPlayers,
            r.IsPublic,
            r.Status,
            r.Players
        );
}
