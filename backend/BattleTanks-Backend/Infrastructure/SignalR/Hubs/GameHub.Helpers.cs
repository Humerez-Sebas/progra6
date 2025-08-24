using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.SignalR.Hubs;

public partial class GameHub : Hub
{
    protected (string? UserId, string? Username) GetUser()
    {
        var userId = Context.User?.FindFirst("user_id")?.Value
                  ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.FindFirst("username")?.Value
                    ?? Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        return (userId, username);
    }

    protected static float Clamp(float v, float min, float max) => MathF.Max(min, MathF.Min(max, v));

    protected static float NormalizeAngle(float radians)
    {
        if (float.IsNaN(radians) || float.IsInfinity(radians)) return 0f;
        var twoPi = MathF.Tau;
        var r = radians % twoPi;
        if (r < 0) r += twoPi;
        return r;
    }
}
