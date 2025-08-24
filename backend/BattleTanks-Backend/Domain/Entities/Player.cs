using Domain.ValueObjects;

namespace Domain.Entities;

public class Player
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? GameSessionId { get; private set; }
    public string ConnectionId { get; private set; }
    public Position Position { get; private set; }
    public float Rotation { get; private set; }
    public DateTime LastUpdate { get; private set; }
    public bool IsAlive { get; private set; }
    public int SessionKills { get; private set; }
    public int SessionDeaths { get; private set; }
    public int SessionScore { get; private set; }
    
    // Navegación
    public User User { get; private set; }
    public GameSession? GameSession { get; private set; }
    
    private Player() { } // Para EF Core
    
    public static Player Create(Guid userId, string connectionId)
    {
        return new Player
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ConnectionId = connectionId,
            Position = Position.Zero,
            Rotation = 0,
            LastUpdate = DateTime.UtcNow,
            IsAlive = true,
            SessionKills = 0,
            SessionDeaths = 0,
            SessionScore = 0
        };
    }
    
    public void JoinGameSession(Guid gameSessionId)
    {
        GameSessionId = gameSessionId;
        ResetSessionStats();
    }
    
    public void LeaveGameSession()
    {
        GameSessionId = null;
        ResetSessionStats();
    }
    
    public void UpdatePosition(Position newPosition, float rotation)
    {
        Position = newPosition;
        Rotation = rotation;
        LastUpdate = DateTime.UtcNow;
    }
    
    public void UpdateConnectionId(string connectionId)
    {
        ConnectionId = connectionId;
        LastUpdate = DateTime.UtcNow;
    }
    
    public void RegisterDeath()
    {
        IsAlive = false;
        SessionDeaths++;
        LastUpdate = DateTime.UtcNow;
    }

    public void Respawn(Position position)
    {
        Position = position;
        IsAlive = true;
        LastUpdate = DateTime.UtcNow;
    }
    
    public void AddKill(int points = 100)
    {
        SessionKills++;
        SessionScore += points;
    }

    public void AddScore(int points)
    {
        SessionScore += points;
    }
    
    public void ResetSessionStats()
    {
        SessionKills = 0;
        SessionDeaths = 0;
        SessionScore = 0;
    }
    
    // Propiedades de conveniencia
    public string Username => User?.Username ?? "Unknown";
}