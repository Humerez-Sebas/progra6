using Application.DTOs;

namespace Application.Interfaces;

public interface IBulletService
{
    bool TrySpawn(string roomCode, string roomId, string shooterId, float x, float y, float dir, float speed, out BulletStateDto state);
    
    IReadOnlyDictionary<string, BulletStateDto> GetRoomBullets(string roomCode);
    IEnumerable<(string roomCode, string bulletId, BulletStateDto state)> EnumerateAll();
    bool TryGetRoom(string roomCode, out IReadOnlyDictionary<string, BulletStateDto> roomDict);
    
    void UpdateBullet(string roomCode, string bulletId, BulletStateDto newState);
    void Despawn(string roomCode, string bulletId);
    
    int CountActiveByShooter(string roomCode, string shooterId);
    DateTime LastShotAt(string roomCode, string shooterId);
}