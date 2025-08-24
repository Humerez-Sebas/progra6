namespace Application.DTOs;

public record PlayerLifeLostDto(
    string TargetPlayerId,
    int LivesAfter,
    bool Eliminated
);

public record PlayerRespawnedDto(
    string PlayerId,
    float X,
    float Y
);
