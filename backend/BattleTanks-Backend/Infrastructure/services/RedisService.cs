using System.Text.Json;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Infrastructure.Services;

public class RedisService : IRedisService, IAsyncDisposable
{
    private readonly ConnectionMultiplexer _conn;

    public RedisService(IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        _conn = ConnectionMultiplexer.Connect(cs);
    }

    public Task SaveAsync(string key, string value)
    {
        var db = _conn.GetDatabase();
        return db.ListRightPushAsync(key, value);
    }

    public Task SetTokenAsync(string token, TimeSpan expiration)
    {
        var db = _conn.GetDatabase();
        return db.StringSetAsync($"jwt:{token}", "1", expiration);
    }

    public async Task<bool> IsTokenValidAsync(string token)
    {
        var db = _conn.GetDatabase();
        return await db.KeyExistsAsync($"jwt:{token}");
    }

    public Task RemoveTokenAsync(string token)
    {
        var db = _conn.GetDatabase();
        return db.KeyDeleteAsync($"jwt:{token}");
    }

    public ValueTask DisposeAsync()
    {
        return _conn.DisposeAsync();
    }
}
