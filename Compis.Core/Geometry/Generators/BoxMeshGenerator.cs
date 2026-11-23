
using System.Numerics;
using Compis.Core.Geometry.Meshes;
using Compis.Core.Parameters;

namespace Compis.Core.Geometry.Generators;

public static class BoxMeshGenerator
{
    public static MeshData Generate(BoxParameters parameters)
    {
        double halfWidth = parameters.Dimensions.Width / 2;
        double halfDepth = parameters.Dimensions.Depth / 2;
        double height = parameters.Dimensions.Height;

        Vector3 position = new(
            (float)parameters.Position.X,
            (float)parameters.Position.Y,
            (float)parameters.Position.Z);
        Vector3 scale = new(
            (float)parameters.Scale.X,
            (float)parameters.Scale.Y,
            (float)parameters.Scale.Z);
        Quaternion rotation = Quaternion.CreateFromYawPitchRoll(
            DegreesToRadians(parameters.Rotation.Y),
            DegreesToRadians(parameters.Rotation.X),
            DegreesToRadians(parameters.Rotation.Z));

        Position3D Transform(double x, double y, double z, int vertexIndex)
        {
            Position3D offset = parameters.VertexOffsets is { Count: > 0 } offsets &&
                vertexIndex < offsets.Count
                    ? offsets[vertexIndex]
                    : new Position3D(0, 0, 0);
            Vector3 local = new(
                (float)(x + offset.X),
                (float)(y + offset.Y),
                (float)(z + offset.Z));
            Vector3 transformed = Vector3.Transform(local * scale, rotation) + position;
            return new Position3D(transformed.X, transformed.Y, transformed.Z);
        }

        var vertices = new List<Position3D>
        {
            Transform(-halfWidth, -halfDepth, 0, 0),
            Transform(halfWidth, -halfDepth, 0, 1),
            Transform(halfWidth, halfDepth, 0, 2),
            Transform(-halfWidth, halfDepth, 0, 3),

            Transform(-halfWidth, -halfDepth, height, 4),
            Transform(halfWidth, -halfDepth, height, 5),
            Transform(halfWidth, halfDepth, height, 6),
            Transform(-halfWidth, halfDepth, height, 7)
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

        int[] edges =
        [
            0, 1, 1, 2, 2, 3, 3, 0,
            4, 5, 5, 6, 6, 7, 7, 4,
            0, 4, 1, 5, 2, 6, 3, 7
        ];

        return new MeshData(vertices, triangles, edges);
    }

    private static float DegreesToRadians(double degrees) =>
        (float)(degrees * Math.PI / 180.0);
}
