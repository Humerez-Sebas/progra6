using System.Collections.Generic;

namespace Application.Interfaces;

public interface IScoreRegistry
{
    int AddScore(string roomId, string playerId, int amount);
    int GetScore(string roomId, string playerId);
    IReadOnlyDictionary<string, int> GetScores(string roomId);

    int AddLife(string roomId, string playerId, int amount);
    int GetLives(string roomId, string playerId);
    IReadOnlyDictionary<string, int> GetLives(string roomId);

    void ResetRoom(string roomId);
}
