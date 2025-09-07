using System.Collections.Concurrent;
using Application.Interfaces;

namespace Infrastructure.Services;

public class InMemoryScoreRegistry : IScoreRegistry
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> _scores = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> _lives = new();

    public int AddScore(string roomId, string playerId, int amount)
    {
        var room = _scores.GetOrAdd(roomId, _ => new ConcurrentDictionary<string, int>());
        return room.AddOrUpdate(playerId, amount, (_, prev) => prev + amount);
    }

    public int GetScore(string roomId, string playerId)
    {
        var room = _scores.GetOrAdd(roomId, _ => new ConcurrentDictionary<string, int>());
        return room.TryGetValue(playerId, out var s) ? s : 0;
    }

    public IReadOnlyDictionary<string, int> GetScores(string roomId)
    {
        return _scores.TryGetValue(roomId, out var scores)
            ? new Dictionary<string, int>(scores)
            : new Dictionary<string, int>();
    }

    public int AddLife(string roomId, string playerId, int amount)
    {
        var room = _lives.GetOrAdd(roomId, _ => new ConcurrentDictionary<string, int>());
        return room.AddOrUpdate(playerId, 3 + amount, (_, prev) => prev + amount);
    }

    public int GetLives(string roomId, string playerId)
    {
        var room = _lives.GetOrAdd(roomId, _ => new ConcurrentDictionary<string, int>());
        return room.TryGetValue(playerId, out var l) ? l : 3;
    }

    public IReadOnlyDictionary<string, int> GetLives(string roomId)
    {
        return _lives.TryGetValue(roomId, out var lives)
            ? new Dictionary<string, int>(lives)
            : new Dictionary<string, int>();
    }

    public void ResetRoom(string roomId)
    {
        _scores.TryRemove(roomId, out _);
        _lives.TryRemove(roomId, out _);
    }
}
