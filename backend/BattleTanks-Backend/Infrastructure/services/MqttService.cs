using System.Threading.Tasks;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Client;

namespace Infrastructure.Services;

public class MqttService : IMqttService, IAsyncDisposable
{
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;
    private readonly string _prefix;

    public MqttService(IConfiguration configuration)
    {
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        var host = configuration.GetValue<string>("MqttSettings:Host") ?? "localhost";
        var port = configuration.GetValue<int>("MqttSettings:Port", 1883);
        _prefix = configuration.GetValue<string>("MqttSettings:TopicPrefix") ?? "battletanks";

        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .Build();
    }

    private async Task EnsureConnectedAsync()
    {
        if (!_client.IsConnected)
        {
            await _client.ConnectAsync(_options);
        }
    }

    public async Task PublishAsync(string topic, string payload)
    {
        await EnsureConnectedAsync();
        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"{_prefix}/{topic}")
            .WithPayload(payload)
            .Build();
        await _client.PublishAsync(message);
    }

    public async ValueTask DisposeAsync()
    {
        if (_client.IsConnected)
            await _client.DisconnectAsync();
        _client.Dispose();
    }
}
