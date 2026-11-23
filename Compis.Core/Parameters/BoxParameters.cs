namespace Compis.Core.Parameters;

public sealed record BoxParameters(
    Dimensions3D Dimensions,
    Position3D Position,
    Rotation3D Rotation,
    Scale3D Scale,
    MaterialColor Color,
    IReadOnlyList<Position3D>? VertexOffsets = null);