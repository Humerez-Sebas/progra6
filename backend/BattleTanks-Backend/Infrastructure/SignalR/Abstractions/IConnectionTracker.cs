namespace Infrastructure.SignalR.Abstractions;

public interface IConnectionTracker
{
    void Set(string connectionId, string roomId, string roomCode, string userId, string username);
    bool TryGet(string connectionId, out (string RoomId, string RoomCode, string UserId, string Username) info);
    void Remove(string connectionId);
    int CountByRoom(string roomCode);
}
