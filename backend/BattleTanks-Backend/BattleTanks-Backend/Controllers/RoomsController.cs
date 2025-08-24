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

    public RoomsController(
        IGameService gameService,
        IGameSessionRepository gameSessionRepository,
        IRoomRegistry roomRegistry)
    {
        _gameService = gameService;
        _gameSessionRepository = gameSessionRepository;
        _roomRegistry = roomRegistry;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveRooms(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool onlyPublic = true)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new { success = false, message = "Invalid pagination parameters" });

        var (items, total) = await _gameSessionRepository.GetActiveSessionsPagedAsync(onlyPublic, page, pageSize);

        // Enriquecer cada room con los jugadores actuales desde RoomRegistry
        var roomsTasks = items.Select(async s =>
        {
            var playersNow = await _roomRegistry.GetPlayersByIdAsync(s.Id.ToString());

            var players = playersNow.Count > 0
                ? playersNow.ToList()
                : s.Players.Select(p => new PlayerStateDto(
                        p.UserId.ToString(),
                        p.Username,
                        p.Position.X,
                        p.Position.Y,
                        p.Rotation,
                        p.Health,
                        p.IsAlive))
                  .ToList();

            return new RoomStateDto(
                s.Id.ToString(),
                s.Code,
                s.Status.ToString(),
                players
            );
        });

        var rooms = await Task.WhenAll(roomsTasks);

        return Ok(new
        {
            success = true,
            page,
            pageSize,
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
            room = new RoomStateDto(
                room.RoomId,
                room.RoomCode,
                room.Status,
                snap.Players.Values.ToList()
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

    private string? GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var _) ? userIdClaim : null;
    }
}
