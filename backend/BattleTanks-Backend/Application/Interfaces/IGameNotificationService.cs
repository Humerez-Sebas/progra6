using Application.DTOs;

namespace Application.Interfaces;

public interface IGameNotificationService
{
    Task NotifyPlayerPositionUpdate(string roomId, PlayerPositionDto position);
    Task NotifyPlayerJoined(string roomId, PlayerStateDto player);
    Task NotifyPlayerLeft(string roomId, string playerId);
    Task NotifyRoomStateChanged(string roomId, RoomStateDto roomState);
    Task NotifyChatMessage(string roomId, ChatMessageDto message);
}