using System.Threading.Tasks;

namespace Infrastructure.Interfaces;

public interface IRedisService
{
    Task SaveAsync(string key, string value);
    Task SetTokenAsync(string token, TimeSpan expiration);
    Task<bool> IsTokenValidAsync(string token);
    Task RemoveTokenAsync(string token);
}
