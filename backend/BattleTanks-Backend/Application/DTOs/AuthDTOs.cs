namespace Application.DTOs;

public record RegisterDto(
    string Username,
    string Email,
    string Password,
    string ConfirmPassword
);

public record LoginDto(
    string UsernameOrEmail,
    string Password
);

public record UserDto(
    string Id,
    string Username,
    string Email,
    int GamesPlayed,
    int GamesWon,
    int TotalScore,
    double WinRate,
    DateTime CreatedAt
);