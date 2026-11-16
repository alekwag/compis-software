namespace Compis.Core.Parameters;

public sealed record Rotation3D(double X, double Y, double Z)
{
    public static Rotation3D Zero { get; } = new(0, 0, 0);
}