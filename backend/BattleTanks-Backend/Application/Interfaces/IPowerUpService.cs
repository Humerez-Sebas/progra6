using Application.DTOs;

namespace Application.Interfaces;

public interface IPowerUpService
{
    PowerUpDto SpawnRandom(string roomCode, string roomId);
    IReadOnlyCollection<PowerUpDto> GetActive(string roomCode);
    bool TryConsume(string roomCode, string userId, float x, float y, out PowerUpDto? powerUp);
}
