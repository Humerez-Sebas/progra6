using Application.DTOs;
using Application.Interfaces;

namespace Infrastructure.SignalR.Services;

public class NoOpNotificationService : IGameNotificationService
{
    public Task NotifyPlayerPositionUpdate(string roomId, PlayerPositionDto position) => Task.CompletedTask;
    public Task NotifyPlayerJoined(string roomId, PlayerStateDto player) => Task.CompletedTask;
    public Task NotifyPlayerLeft(string roomId, string playerId) => Task.CompletedTask;
    public Task NotifyRoomStateChanged(string roomId, RoomStateDto roomState) => Task.CompletedTask;
    public Task NotifyChatMessage(string roomId, ChatMessageDto message) => Task.CompletedTask;
}
