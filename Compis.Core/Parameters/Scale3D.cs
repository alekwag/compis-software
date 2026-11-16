namespace Compis.Core.Parameters;

public sealed record Scale3D(double X, double Y, double Z)
{
    public static Scale3D One { get; } = new(1, 1, 1);
}