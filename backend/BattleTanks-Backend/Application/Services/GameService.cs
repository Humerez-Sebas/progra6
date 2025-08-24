using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class GameService : IGameService
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGameNotificationService _notificationService;

    public GameService(
        IGameSessionRepository gameSessionRepository,
        IPlayerRepository playerRepository,
        IUserRepository userRepository,
        IGameNotificationService notificationService)
    {
        _gameSessionRepository = gameSessionRepository;
        _playerRepository = playerRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
    }

    public async Task<RoomStateDto?> CreateRoom(string userId, CreateRoomDto createRoomDto)
    {
        var session = GameSession.Create(createRoomDto.Name, createRoomDto.MaxPlayers, createRoomDto.IsPublic);
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

        return MapToPlayerStateDto(player);
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

        return player != null ? MapToPlayerStateDto(player) : null;
    }

    public async Task<List<RoomStateDto>> GetActiveRooms()
    {
        var sessions = await _gameSessionRepository.GetActiveSessionsAsync();
        return sessions.Select(MapToRoomStateDto).ToList();
    }

    private RoomStateDto MapToRoomStateDto(GameSession session)
    {
        return new RoomStateDto(
            session.Id.ToString(),
            session.Code,
            session.Status.ToString(),
            session.Players.Select(MapToPlayerStateDto).ToList()
        );
    }

    private PlayerStateDto MapToPlayerStateDto(Player player)
    {
        return new PlayerStateDto(
            player.UserId.ToString(),
            player.Username,
            player.Position.X,
            player.Position.Y,
            player.Rotation,
            player.Health,
            player.IsAlive
        );
    }
}
