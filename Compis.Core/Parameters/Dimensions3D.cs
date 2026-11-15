
namespace Compis.Core.Parameters;

public sealed record Dimensions3D
{
    public double Width { get; }
    public double Depth { get; }
    public double Height { get; }

    public Dimensions3D(double width, double depth, double height)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (depth <= 0)
            throw new ArgumentOutOfRangeException(nameof(depth));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        Width = width;
        Depth = depth;
        Height = height;
    }
}
