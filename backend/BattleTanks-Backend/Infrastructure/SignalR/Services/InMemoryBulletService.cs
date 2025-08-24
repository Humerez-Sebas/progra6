using System.Collections.Concurrent;
using Application.DTOs;
using Application.Interfaces;

namespace Infrastructure.SignalR.Services;

public class InMemoryBulletService : IBulletService
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, BulletStateDto>> _byRoom = new();
    private readonly ConcurrentDictionary<(string roomCode, string shooterId), DateTime> _lastShot = new();

    private const int MaxActivePerShooter = 2;
    private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(500);

    public bool TrySpawn(string roomCode, string roomId, string shooterId, float x, float y, float dir, float speed, out BulletStateDto state)
    {
        state = default!;
        var now = DateTime.UtcNow;

        var last = _lastShot.GetOrAdd((roomCode, shooterId), DateTime.MinValue);
        if (now - last < MinInterval) return false;

        var activeCount = CountActiveByShooter(roomCode, shooterId);
        if (activeCount >= MaxActivePerShooter) return false;

        var bulletId = Guid.NewGuid().ToString();
        state = new BulletStateDto(
            bulletId,
            roomId,
            shooterId,
            x, y,
            dir,
            Math.Clamp(speed, 10f, 1200f),
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            true
        );

        var room = _byRoom.GetOrAdd(roomCode, _ => new ConcurrentDictionary<string, BulletStateDto>());
        room[bulletId] = state;
        _lastShot[(roomCode, shooterId)] = now;
        return true;
    }

    public IReadOnlyDictionary<string, BulletStateDto> GetRoomBullets(string roomCode)
        => _byRoom.TryGetValue(roomCode, out var r) ? r : new ConcurrentDictionary<string, BulletStateDto>();

    public IEnumerable<(string roomCode, string bulletId, BulletStateDto state)> EnumerateAll()
    {
        foreach (var kv in _byRoom)
            foreach (var b in kv.Value)
                yield return (kv.Key, b.Key, b.Value);
    }

    public bool TryGetRoom(string roomCode, out IReadOnlyDictionary<string, BulletStateDto> roomDict)
    {
        if (_byRoom.TryGetValue(roomCode, out var r))
        {
            roomDict = r;
            return true;
        }
        roomDict = new ConcurrentDictionary<string, BulletStateDto>();
        return false;
    }

    public void UpdateBullet(string roomCode, string bulletId, BulletStateDto newState)
    {
        if (_byRoom.TryGetValue(roomCode, out var r))
            r[bulletId] = newState;
    }

    public void Despawn(string roomCode, string bulletId)
    {
        if (_byRoom.TryGetValue(roomCode, out var r))
            r.TryRemove(bulletId, out _);
    }

    public int CountActiveByShooter(string roomCode, string shooterId)
    {
        if (!_byRoom.TryGetValue(roomCode, out var r)) return 0;
        return r.Values.Count(v => v.IsActive && v.ShooterId == shooterId);
    }

    public DateTime LastShotAt(string roomCode, string shooterId)
        => _lastShot.TryGetValue((roomCode, shooterId), out var d) ? d : DateTime.MinValue;
}
