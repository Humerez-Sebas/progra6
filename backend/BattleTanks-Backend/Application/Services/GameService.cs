using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.SignalR.Abstractions;

namespace Application.Services;

public class GameService : IGameService
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGameNotificationService _notificationService;
    private readonly IScoreRepository _scoreRepository;
    private readonly IScoreRegistry _scoreRegistry;
    private readonly IRoomRegistry _roomRegistry;

    public GameService(
        IGameSessionRepository gameSessionRepository,
        IPlayerRepository playerRepository,
        IUserRepository userRepository,
        IGameNotificationService notificationService,
        IScoreRepository scoreRepository,
        IScoreRegistry scoreRegistry,
        IRoomRegistry roomRegistry)
    {
        _gameSessionRepository = gameSessionRepository;
        _playerRepository = playerRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _scoreRepository = scoreRepository;
        _scoreRegistry = scoreRegistry;
        _roomRegistry = roomRegistry;
    }

    public async Task<RoomStateDto?> CreateRoom(string userId, CreateRoomDto createRoomDto)
    {
        var session = GameSession.Create(createRoomDto.Name, createRoomDto.Region, createRoomDto.MaxPlayers, createRoomDto.IsPublic);
        await _gameSessionRepository.AddAsync(session);

        return MapToRoomStateDto(session);
    }

    public async Task<RoomStateDto?> JoinRoom(string userId, string connectionId, JoinRoomDto joinRoomDto)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return null;

        var session = await _gameSessionRepository.GetByCodeAsync(joinRoomDto.RoomCode);
        if (session == null) return null;

        var user = await _userRepository.GetByIdAsync(userGuid);
        if (user == null) return null;

        var existingPlayer = await _playerRepository.GetByUserIdAsync(userGuid);

        if (existingPlayer != null)
        {
            existingPlayer.UpdateConnectionId(connectionId);

            if (!session.TryAddPlayer(existingPlayer))
                return null;

            await _playerRepository.UpdateAsync(existingPlayer);
        }
        else
        {
            var player = Player.Create(userGuid, connectionId);
            if (!session.TryAddPlayer(player))
                return null;

            await _playerRepository.AddAsync(player);
        }

        await _gameSessionRepository.UpdateAsync(session);

        var roomState = MapToRoomStateDto(session);
        await _notificationService.NotifyRoomStateChanged(session.Id.ToString(), roomState);

        return roomState;
    }

    public async Task<bool> LeaveRoom(string userId, string roomId)
    {
        if (!Guid.TryParse(userId, out var userGuid) || !Guid.TryParse(roomId, out var roomGuid))
            return false;

        var session = await _gameSessionRepository.GetByIdAsync(roomGuid);
        if (session == null) return false;

        session.RemovePlayer(userGuid);
        await _gameSessionRepository.UpdateAsync(session);

        await _notificationService.NotifyPlayerLeft(roomId, userId);

        return true;
    }

    public async Task<bool> UpdatePlayerPosition(string roomId, string userId, PlayerPositionDto positionDto)
    {
        if (!Guid.TryParse(userId, out var userGuid) || !Guid.TryParse(roomId, out var roomGuid))
            return false;

        var session = await _gameSessionRepository.GetByIdAsync(roomGuid);
        if (session == null) return false;

        var player = session.GetPlayer(userGuid);
        if (player == null) return false;

        player.UpdatePosition(
            new Domain.ValueObjects.Position(positionDto.X, positionDto.Y),
            positionDto.Rotation
        );

        await _playerRepository.UpdateAsync(player);
        await _notificationService.NotifyPlayerPositionUpdate(roomId, positionDto);

        return true;
    }

    public async Task<PlayerStateDto?> GetPlayerPosition(string roomId, string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid) || !Guid.TryParse(roomId, out var roomGuid))
            return null;

        var session = await _gameSessionRepository.GetByIdAsync(roomGuid);
        if (session == null) return null;

        var player = session.GetPlayer(userGuid);
        if (player == null) return null;

        return MapToPlayerStateDto(player, session.Id);
    }

    public async Task<bool> MovePlayer(string roomId, string userId, string direction)
    {
        if (!Guid.TryParse(userId, out var userGuid) || !Guid.TryParse(roomId, out var roomGuid))
            return false;

        var session = await _gameSessionRepository.GetByIdAsync(roomGuid);
        if (session == null) return false;

        var player = session.GetPlayer(userGuid);
        if (player == null) return false;

        var currentPos = player.Position;
        float newX = currentPos.X;
        float newY = currentPos.Y;

        const float step = 1.0f;
        const float maxCoord = 19.0f;

        switch (direction.ToLower())
        {
            case "up":
                newY = Math.Max(0, newY - step);
                break;
            case "down":
                newY = Math.Min(maxCoord, newY + step);
                break;
            case "left":
                newX = Math.Max(0, newX - step);
                break;
            case "right":
                newX = Math.Min(maxCoord, newX + step);
                break;
            default:
                return false;
        }

        var newPosition = new Domain.ValueObjects.Position(newX, newY);
        player.UpdatePosition(newPosition, player.Rotation);

        await _playerRepository.UpdateAsync(player);

        var positionDto = new PlayerPositionDto(
            userId,
            newX,
            newY,
            player.Rotation,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );

        await _notificationService.NotifyPlayerPositionUpdate(roomId, positionDto);

        return true;
    }

    public async Task<RoomStateDto?> StartGame(string roomId)
    {
        if (!Guid.TryParse(roomId, out var roomGuid))
            return null;

        var session = await _gameSessionRepository.GetByIdAsync(roomGuid);
        if (session == null)
            return null;

        session.StartGame();
        if (session.Status != GameRoomStatus.InProgress)
            return null;

        await _gameSessionRepository.UpdateAsync(session);

        var roomState = MapToRoomStateDto(session);
        await _notificationService.NotifyRoomStateChanged(roomId, roomState);

        return roomState;
    }

    public async Task<RoomStateDto?> GetRoomState(string roomId)
    {
        if (!Guid.TryParse(roomId, out var roomGuid))
            return null;

        var session = await _gameSessionRepository.GetByIdAsync(roomGuid);
        return session != null ? MapToRoomStateDto(session) : null;
    }

    public async Task<RoomStateDto?> GetRoomByCode(string roomCode)
    {
        var session = await _gameSessionRepository.GetByCodeAsync(roomCode);
        return session != null ? MapToRoomStateDto(session) : null;
    }

    public async Task<PlayerStateDto?> GetPlayerState(string roomId, string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid) || !Guid.TryParse(roomId, out var roomGuid))
            return null;

        var session = await _gameSessionRepository.GetByIdAsync(roomGuid);
        var player = session?.GetPlayer(userGuid);

        return player != null ? MapToPlayerStateDto(player, roomGuid) : null;
    }

    public async Task<List<RoomStateDto>> GetActiveRooms(string? region = null)
    {
        var sessions = await _gameSessionRepository.GetActiveSessionsAsync(region);
        return sessions.Select(MapToRoomStateDto).ToList();
    }

    public async Task AwardWallPoints(string roomId, string userId, int points)
    {
        if (!Guid.TryParse(roomId, out var roomGuid) || !Guid.TryParse(userId, out var userGuid)) return;
        var session = await _gameSessionRepository.GetByIdAsync(roomGuid);
        var player = session?.GetPlayer(userGuid);
        if (player is null) return;
        player.AddScore(points);
        await _playerRepository.UpdateAsync(player);
    }

    public async Task RegisterKill(string roomId, string shooterId, string targetId, int points)
    {
        if (!Guid.TryParse(roomId, out var roomGuid) ||
            !Guid.TryParse(shooterId, out var shooterGuid) ||
            !Guid.TryParse(targetId, out var targetGuid)) return;

        var session = await _gameSessionRepository.GetByIdAsync(roomGuid);
        if (session is null) return;
        var shooter = session.GetPlayer(shooterGuid);
        var target = session.GetPlayer(targetGuid);
        if (shooter is null || target is null) return;

        shooter.AddKill(points);
        target.RegisterDeath();
        await _playerRepository.UpdateAsync(shooter);
        await _playerRepository.UpdateAsync(target);
        await _gameSessionRepository.UpdateAsync(session);
    }

    public async Task<RoomStateDto?> EndGame(string roomId)
    {
        if (!Guid.TryParse(roomId, out var roomGuid)) return null;
        var session = await _gameSessionRepository.GetByIdAsync(roomGuid);
        if (session is null) return null;

        var scores = _scoreRegistry.GetScores(roomId);
        var lives = _scoreRegistry.GetLives(roomId);
        foreach (var player in session.Players)
        {
            var pid = player.UserId.ToString();
            if (scores.TryGetValue(pid, out var sc))
            {
                var diff = sc - player.SessionScore;
                if (diff > 0) player.AddScore(diff);
            }
            if (lives.TryGetValue(pid, out var l) && l <= 0 && player.IsAlive)
            {
                player.RegisterDeath();
            }
        }

        session.EndGame();
        await _gameSessionRepository.UpdateAsync(session);

        await _scoreRepository.AddRangeAsync(session.Scores);
        foreach (var score in session.Scores)
        {
            var user = await _userRepository.GetByIdAsync(score.UserId);
            if (user != null)
            {
                user.AddGameResult(score.IsWinner, score.Points, score.Kills);
                await _userRepository.UpdateAsync(user);
            }
        }

        await _roomRegistry.UpsertRoomAsync(
            session.Id.ToString(),
            session.Code,
            session.Name,
            session.MaxPlayers,
            session.IsPublic,
            session.Status.ToString());

        _scoreRegistry.ResetRoom(roomId);

        var roomState = MapToRoomStateDto(session);
        await _notificationService.NotifyRoomStateChanged(roomId, roomState);
        return roomState;
    }

    private RoomStateDto MapToRoomStateDto(GameSession session)
    {
        return new RoomStateDto(
            session.Id.ToString(),
            session.Code,
            session.Region,
            session.Status.ToString(),
            session.Players.Select(p => MapToPlayerStateDto(p, session.Id)).ToList()
        );
    }

    private PlayerStateDto MapToPlayerStateDto(Player player, Guid sessionId)
    {
        var roomId = sessionId.ToString();
        var playerId = player.UserId.ToString();
        var lives = _scoreRegistry.GetLives(roomId, playerId);
        var score = _scoreRegistry.GetScore(roomId, playerId);
        return new PlayerStateDto(
            playerId,
            player.Username,
            player.Position.X,
            player.Position.Y,
            player.Rotation,
            lives > 0 && player.IsAlive,
            lives,
            score
        );
    }
}
