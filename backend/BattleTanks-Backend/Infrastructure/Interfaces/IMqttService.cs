using System.Threading.Tasks;

namespace Infrastructure.Interfaces;

public interface IMqttService
{
    Task PublishAsync(string topic, string payload);
}
