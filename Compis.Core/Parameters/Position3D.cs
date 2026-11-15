
namespace Compis.Core.Parameters;

public sealed record Position3D
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public Position3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
