namespace Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastLoginAt { get; private set; }
    
    // Estadísticas del jugador
    public int GamesPlayed { get; private set; }
    public int GamesWon { get; private set; }
    public int TotalScore { get; private set; }
    public int PlayersEliminated { get; private set; }
    
    // Relación con Score
    private readonly List<Score> _scores = new();
    public IReadOnlyList<Score> Scores => _scores.AsReadOnly();
    
    private User() { } // Para EF Core
    
    public static User Create(string username, string email, string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            GamesPlayed = 0,
            GamesWon = 0,
            TotalScore = 0,
            PlayersEliminated = 0
        };
    }
    
    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }
    
    public void AddGameResult(bool won, int score, int kills)
    {
        GamesPlayed++;
        if (won)
        {
            GamesWon++;
        }
        TotalScore += score;
        PlayersEliminated += kills;
    }
    
    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }
    
    public void UpdateEmail(string newEmail)
    {
        Email = newEmail.ToLowerInvariant();
    }
    
    public double WinRate => GamesPlayed > 0 ? (double)GamesWon / GamesPlayed : 0;
}