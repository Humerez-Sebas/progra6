using System;

namespace Infrastructure.Interfaces;

public interface ILifeService
{
    int AddLife(string roomId, string playerId, int amount = 1);
    int GetLives(string roomId, string playerId);
}

