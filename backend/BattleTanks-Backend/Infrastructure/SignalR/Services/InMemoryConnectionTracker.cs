using System.Collections.Concurrent;
using Infrastructure.SignalR.Abstractions;

namespace Infrastructure.SignalR.Services;

public class InMemoryConnectionTracker : IConnectionTracker
{
    private readonly ConcurrentDictionary<string, (string RoomId, string RoomCode, string UserId, string Username)> _map = new();

    public void Set(string connectionId, string roomId, string roomCode, string userId, string username)
        => _map[connectionId] = (roomId, roomCode, userId, username);

    public bool TryGet(string connectionId, out (string RoomId, string RoomCode, string UserId, string Username) info)
        => _map.TryGetValue(connectionId, out info);

    public void Remove(string connectionId)
        => _map.TryRemove(connectionId, out _);

    public int CountByRoom(string roomCode)
        => _map.Values.Count(v => v.RoomCode == roomCode);
}
