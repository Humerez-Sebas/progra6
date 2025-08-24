using Application.DTOs;
using Infrastructure.SignalR.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.SignalR.Hubs;

public partial class GameHub : Hub
{
    public async Task SendChat(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        if (!_tracker.TryGet(Context.ConnectionId, out var info)) throw new HubException("not_in_room");

        var msg = new ChatMessageDto(
            Guid.NewGuid().ToString(),
            Guid.Parse(info.UserId),
            info.Username,
            content.Trim(),
            "User",
            DateTime.UtcNow
        );

        await Clients.Group(info.RoomCode).SendAsync("chatMessage", msg);
    }
}
