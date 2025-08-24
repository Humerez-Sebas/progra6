using Application.DTOs;

namespace Infrastructure.SignalR.Abstractions;

public record RoomSnapshot(
    string RoomId,
    string RoomCode,
    string Name,
    int MaxPlayers,
    bool IsPublic,
    string Status,
    IReadOnlyDictionary<string, PlayerStateDto> Players
);

public interface IRoomRegistry
{
    Task UpsertRoomAsync(string roomId, string roomCode, string name, int maxPlayers, bool isPublic, string status);
    Task<RoomSnapshot?> GetByIdAsync(string roomId);
    Task<RoomSnapshot?> GetByCodeAsync(string roomCode);
    Task JoinAsync(string roomCode, string userId, string username, string connectionId);
    Task<(string? roomCode, string? userId)> LeaveByConnectionAsync(string connectionId);
    Task<IReadOnlyCollection<PlayerStateDto>> GetPlayersByIdAsync(string roomId);
}
