using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Application.Interfaces;
using Application.DTOs;
using Infrastructure.SignalR.Abstractions;
using System.Security.Claims;

namespace BattleTanks_Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IRoomRegistry _roomRegistry;
    private readonly IScoreRegistry _scoreRegistry;

    public RoomsController(
        IGameService gameService,
        IGameSessionRepository gameSessionRepository,
        IRoomRegistry roomRegistry,
        IScoreRegistry scoreRegistry)
    {
        _gameService = gameService;
        _gameSessionRepository = gameSessionRepository;
        _roomRegistry = roomRegistry;
        _scoreRegistry = scoreRegistry;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveRooms([FromQuery] RoomsQuery query)
    {
        if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            return BadRequest(new { success = false, message = "Invalid pagination parameters" });

        var (items, total) = await _gameSessionRepository.GetSessionsPagedAsync(query.OnlyPublic, query.Page, query.PageSize, query.Status, query.Region);

        // Enriquecer cada room con los jugadores actuales desde RoomRegistry
        var roomsTasks = items.Select(async s =>
        {
            var roomId = s.Id.ToString();
            var playersNow = await _roomRegistry.GetPlayersByIdAsync(roomId);

            var players = playersNow.Count > 0
                ? playersNow.Select(p =>
                    {
                        var lives = _scoreRegistry.GetLives(roomId, p.PlayerId);
                        var score = _scoreRegistry.GetScore(roomId, p.PlayerId);
                        return p with { Lives = lives, Score = score, IsAlive = lives > 0 && p.IsAlive };
                    }).ToList()
                : s.Players.Select(p =>
                    {
                        var pid = p.UserId.ToString();
                        var lives = _scoreRegistry.GetLives(roomId, pid);
                        var score = _scoreRegistry.GetScore(roomId, pid);
                        return new PlayerStateDto(
                            pid,
                            p.Username,
                            p.Position.X,
                            p.Position.Y,
                            p.Rotation,
                            lives > 0 && p.IsAlive,
                            lives,
                            score);
                    }).ToList();

            return new RoomStateDto(
                s.Id.ToString(),
                s.Code,
                s.Region,
                s.Status.ToString(),
                players
            );
        });

        var rooms = await Task.WhenAll(roomsTasks);

        return Ok(new
        {
            success = true,
            page = query.Page,
            pageSize = query.PageSize,
            total,
            items = rooms
        });
    }

    [HttpGet("{roomId}")]
    public async Task<ActionResult<RoomStateDto>> GetRoom(string roomId)
    {
        var room = await _gameService.GetRoomState(roomId);
        if (room == null)
            return NotFound(new { success = false, message = "Room not found" });

        var snap = await _roomRegistry.GetByIdAsync(roomId);
        if (snap != null && snap.Players.Any())
        {
            var players = snap.Players.Values
                .Select(p =>
                {
                    var lives = _scoreRegistry.GetLives(roomId, p.PlayerId);
                    var score = _scoreRegistry.GetScore(roomId, p.PlayerId);
                    return p with { Lives = lives, Score = score, IsAlive = lives > 0 && p.IsAlive };
                })
                .ToList();
            room = new RoomStateDto(
                room.RoomId,
                room.RoomCode,
                room.Region,
                room.Status,
                players
            );
        }

        return Ok(room);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<RoomStateDto>> CreateRoom([FromBody] CreateRoomDto createRoomDto)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(new { success = false, message = "Invalid user token" });

        var room = await _gameService.CreateRoom(userId, createRoomDto);
        if (room == null)
            return BadRequest(new { success = false, message = "Could not create room" });

        await _roomRegistry.UpsertRoomAsync(
            room.RoomId, room.RoomCode, createRoomDto.Name,
            createRoomDto.MaxPlayers, createRoomDto.IsPublic, room.Status);

        return CreatedAtAction(nameof(GetRoom), new { roomId = room.RoomId }, room);
    }

    [HttpPost("join")]
    [Authorize]
    public async Task<ActionResult<RoomStateDto>> JoinRoom([FromBody] JoinRoomDto joinRoomDto)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(new { success = false, message = "Invalid user token" });

        var room = await _gameService.JoinRoom(userId, string.Empty, joinRoomDto);
        if (room == null)
            return BadRequest(new { success = false, message = "Could not join room" });

        return Ok(room);
    }

    [HttpPost("{roomId}/start")]
    [Authorize]
    public async Task<ActionResult<RoomStateDto>> StartGame(string roomId)
    {
        var room = await _gameService.StartGame(roomId);
        if (room == null)
            return BadRequest(new { success = false, message = "Could not start game" });

        var snap = await _roomRegistry.GetByIdAsync(roomId);
        if (snap != null)
        {
            await _roomRegistry.UpsertRoomAsync(
                room.RoomId,
                room.RoomCode,
                snap.Name,
                snap.MaxPlayers,
                snap.IsPublic,
                room.Status);
        }

        return Ok(room);
    }

    [HttpPost("{roomId}/end")]
    [Authorize]
    public async Task<ActionResult<RoomStateDto>> EndGame(string roomId)
    {
        var room = await _gameService.EndGame(roomId);
        if (room == null)
            return BadRequest(new { success = false, message = "Could not end game" });

        return Ok(room);
    }

    private string? GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var _) ? userIdClaim : null;
    }
}
