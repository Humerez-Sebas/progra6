using Domain.Enums;

namespace Application.DTOs;

public class RoomsQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool OnlyPublic { get; set; } = true;
    public GameRoomStatus? Status { get; set; }
}

