namespace Domain.ValueObjects;

public record Position(float X, float Y)
{
    public static Position Zero => new(0, 0);
    
    public float DistanceTo(Position other) =>
        MathF.Sqrt(MathF.Pow(X - other.X, 2) + MathF.Pow(Y - other.Y, 2));
    
    public bool IsValid(float maxX, float maxY) =>
        X >= 0 && Y >= 0 && X <= maxX && Y <= maxY;
}