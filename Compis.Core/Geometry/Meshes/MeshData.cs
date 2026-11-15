using Compis.Core.Parameters;

namespace Compis.Core.Geometry.Meshes;

public sealed class MeshData
{
    public IReadOnlyList<Position3D> Vertices { get; }
    public IReadOnlyList<int> Triangles { get; }

    public MeshData(
        IReadOnlyList<Position3D> vertices,
        IReadOnlyList<int> triangles)
    {
        Vertices = vertices;
        Triangles = triangles;
    }
}
