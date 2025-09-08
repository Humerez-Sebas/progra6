using Domain.Enums;

namespace Domain.Entities;

public class GameSession
{
    private readonly List<Player> _players = new();
    private readonly List<Score> _scores = new();

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public int MaxPlayers { get; private set; }
    public GameRoomStatus Status { get; set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsPublic { get; private set; }
    public string Region { get; private set; } = "global";

    public IReadOnlyList<Player> Players => _players.AsReadOnly();
    public IReadOnlyList<Score> Scores => _scores.AsReadOnly();

    private GameSession() { }

    public static GameSession Create(string name, string region, int maxPlayers = 4, bool isPublic = true)
    {
        return new GameSession
        {
            Id = Guid.NewGuid(),
            Code = GenerateRoomCode(),
            Name = name,
            Region = region,
            MaxPlayers = maxPlayers,
            IsPublic = isPublic,
            Status = GameRoomStatus.Waiting,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public bool TryAddPlayer(Player player)
    {
        if (_players.Count >= MaxPlayers) return false;
        if (_players.Any(p => p.UserId == player.UserId)) return false;

        player.JoinGameSession(Id);
        _players.Add(player);
        return true;
    }

    public void RemovePlayer(Guid userId)
    {
        var player = _players.FirstOrDefault(p => p.UserId == userId);
        if (player != null)
        {
            player.LeaveGameSession();
            _players.Remove(player);
        }
    }

    public Player? GetPlayer(Guid userId) => _players.FirstOrDefault(p => p.UserId == userId);

    public Player? GetPlayerByConnectionId(string connectionId) =>
        _players.FirstOrDefault(p => p.ConnectionId == connectionId);

    public void StartGame()
    {
        if (Status == GameRoomStatus.Waiting && _players.Count >= 2)
        {
            Status = GameRoomStatus.InProgress;
            StartedAt = DateTime.UtcNow;

            foreach (var player in _players)
                player.ResetSessionStats();
        }
    }

    public void EndGame()
    {
        if (Status == GameRoomStatus.InProgress)
        {
            Status = GameRoomStatus.Finished;
            EndedAt = DateTime.UtcNow;
            CreateFinalScores();
        }
    }

    public TimeSpan? GameDuration => StartedAt.HasValue && EndedAt.HasValue
        ? EndedAt.Value - StartedAt.Value
        : null;

    private void CreateFinalScores()
    {
        if (!_players.Any()) return;

        var winner = _players.OrderByDescending(p => p.SessionScore).First();

        foreach (var player in _players)
        {
            var score = Score.Create(
                player.UserId,
                Id,
                player.SessionScore,
                player.SessionKills,
                player.SessionDeaths,
                GameDuration ?? TimeSpan.Zero,
                player.Id == winner.Id
            );

            _scores.Add(score);
        }
    }

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }
}
