namespace Application.DTOs;

public record PlayerPositionDto(
    string PlayerId,
    float X,
    float Y,
    float Rotation,
    long Timestamp
);