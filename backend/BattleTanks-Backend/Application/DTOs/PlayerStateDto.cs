namespace Application.DTOs;

public record PlayerStateDto(
    string PlayerId,
    string Username,
    float X,
    float Y,
    float Rotation,
    int Health,
    bool IsAlive
);