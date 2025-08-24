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

    public ValueTask DisposeAsync()
    {
        return _conn.DisposeAsync();
    }
}
