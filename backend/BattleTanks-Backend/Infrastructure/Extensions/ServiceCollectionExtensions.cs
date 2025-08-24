using Application.Interfaces;
using Application.Services;
using Infrastructure.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Background; 
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Infrastructure.SignalR.Abstractions;
using Infrastructure.SignalR.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BattleTanksDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(BattleTanksDbContext).Assembly.FullName)));

        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IPlayerRepository, EfPlayerRepository>();
        services.AddScoped<IGameSessionRepository, EfGameSessionRepository>();
        services.AddScoped<IScoreRepository, EfScoreRepository>();
        services.AddScoped<IChatRepository, EfChatRepository>();

        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<IJwtService, JwtService>();

        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<IConnectionTracker, InMemoryConnectionTracker>();
        services.AddSingleton<IRoomRegistry, InMemoryRoomRegistry>();
        services.AddSingleton<IGameNotificationService, NoOpNotificationService>();

        services.AddSingleton<IMapService, InMemoryMapService>();
        services.AddSingleton<IBulletService, InMemoryBulletService>();
        services.AddSingleton<IMqttService, MqttService>();
        services.AddSingleton<IPowerUpService, InMemoryPowerUpService>();
        services.AddSingleton<BulletSimulationService>();
        services.AddSingleton<ILifeService>(sp => sp.GetRequiredService<BulletSimulationService>());
        services.AddHostedService(sp => sp.GetRequiredService<BulletSimulationService>());
        
        return services;
    }
}
