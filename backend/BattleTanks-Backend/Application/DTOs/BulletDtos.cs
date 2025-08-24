namespace Application.DTOs;

public record BulletSpawnDto(
    string RoomId,
    string ShooterId,
    float X,
    float Y,
    float DirectionRadians,
    float Speed
);

public record BulletStateDto(
    string BulletId,
    string RoomId,
    string ShooterId,
    float X,
    float Y,
    float DirectionRadians,
    float Speed,
    long SpawnTimestamp,
    bool IsActive
);

public record BulletHitReportDto(
    string BulletId,
    string TargetPlayerId,
    float HitX,
    float HitY,
    long Timestamp
);

public record PlayerHitDto(
    string BulletId,
    string TargetPlayerId,
    string ShooterId,
    int Damage,
    int TargetHealthAfter,
    bool TargetIsAlive
);
