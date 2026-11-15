
using Compis.Core.Objects;
using Compis.Core.Geometry.Meshes;
using Compis.Core.Parameters;

namespace Compis.Core.Geometry.Generators;

public static class BoxMeshGenerator
{
    public static MeshData Generate(BuildingVolume volume)
    {
        double halfWidth = volume.Dimensions.Width / 2;
        double halfDepth = volume.Dimensions.Depth / 2;
        double height = volume.Dimensions.Height;

        double x = volume.Position.X;
        double y = volume.Position.Y;
        double z = volume.Position.Z;

        var vertices = new List<Position3D>
        {
            new(x - halfWidth, y - halfDepth, z),
            new(x + halfWidth, y - halfDepth, z),
            new(x + halfWidth, y + halfDepth, z),
            new(x - halfWidth, y + halfDepth, z),

            new(x - halfWidth, y - halfDepth, z + height),
            new(x + halfWidth, y - halfDepth, z + height),
            new(x + halfWidth, y + halfDepth, z + height),
            new(x - halfWidth, y + halfDepth, z + height)
        };

        var triangles = new List<int>
        {
            // Bottom
            0, 2, 1,
            0, 3, 2,

            // Top
            4, 5, 6,
            4, 6, 7,

            // Front
            0, 1, 5,
            0, 5, 4,

            // Back
            2, 3, 7,
            2, 7, 6,

            // Left
            0, 4, 7,
            0, 7, 3,

            // Right
            1, 2, 6,
            1, 6, 5
        };

        return new MeshData(vertices, triangles);
    }
}
