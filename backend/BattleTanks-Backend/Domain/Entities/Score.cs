namespace Domain.Entities;

public class Score
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid GameSessionId { get; private set; }
    public int Points { get; private set; }
    public int Kills { get; private set; }
    public int Deaths { get; private set; }
    public TimeSpan GameDuration { get; private set; }
    public DateTime AchievedAt { get; private set; }
    public bool IsWinner { get; private set; }
    
    // Navegación
    public User User { get; private set; }
    public GameSession GameSession { get; private set; }
    
    private Score() { } // Para EF Core
    
    public static Score Create(
        Guid userId, 
        Guid gameSessionId, 
        int points, 
        int kills, 
        int deaths, 
        TimeSpan gameDuration, 
        bool isWinner)
    {
        return new Score
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GameSessionId = gameSessionId,
            Points = points,
            Kills = kills,
            Deaths = deaths,
            GameDuration = gameDuration,
            AchievedAt = DateTime.UtcNow,
            IsWinner = isWinner
        };
    }
    
    public double KillDeathRatio => Deaths > 0 ? (double)Kills / Deaths : Kills;
}