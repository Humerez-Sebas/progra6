using System.Threading.Tasks;

namespace Infrastructure.Interfaces;

public interface IRedisService
{
    Task SaveAsync(string key, string value);
}
