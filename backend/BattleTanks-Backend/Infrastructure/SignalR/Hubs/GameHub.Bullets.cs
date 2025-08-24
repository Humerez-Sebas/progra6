using Application.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.SignalR.Hubs;

public partial class GameHub : Hub
{
    public async Task<string?> SpawnBullet(float x, float y, float rotation, float speed)
    {
        if (!_tracker.TryGet(Context.ConnectionId, out var info))
            throw new HubException("not_in_room");

        var dir = NormalizeAngle(rotation);

        if (!_bulletService.TrySpawn(info.RoomCode, info.RoomId, info.UserId, x, y, dir, speed, out var state))
            return null;

        await Clients.Group(info.RoomCode).SendAsync("bulletSpawned", state);
        return state.BulletId;
    }

    public Task ReportHit(BulletHitReportDto dto)
    {
        return Task.CompletedTask;
    }
}