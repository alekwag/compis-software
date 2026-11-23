namespace Compis.Core.Parameters;

public sealed record BuildingVolumeSnapshot(
    string Name,
    Dimensions3D Dimensions,
    Position3D Position,
    Rotation3D Rotation,
    Scale3D Scale,
    MaterialColor Color,
    string Category,
    string Function,
    IReadOnlyList<Position3D> VertexOffsets);
