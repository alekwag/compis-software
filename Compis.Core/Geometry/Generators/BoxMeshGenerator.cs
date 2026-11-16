
using System.Numerics;
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

        Vector3 position = new(
            (float)volume.Position.X,
            (float)volume.Position.Y,
            (float)volume.Position.Z);
        Vector3 scale = new(
            (float)volume.Scale.X,
            (float)volume.Scale.Y,
            (float)volume.Scale.Z);
        Quaternion rotation = Quaternion.CreateFromYawPitchRoll(
            DegreesToRadians(volume.Rotation.Y),
            DegreesToRadians(volume.Rotation.X),
            DegreesToRadians(volume.Rotation.Z));

        Position3D Transform(double x, double y, double z)
        {
            Vector3 local = new((float)x, (float)y, (float)z);
            Vector3 transformed = Vector3.Transform(local * scale, rotation) + position;
            return new Position3D(transformed.X, transformed.Y, transformed.Z);
        }

        var vertices = new List<Position3D>
        {
            Transform(-halfWidth, -halfDepth, 0),
            Transform(halfWidth, -halfDepth, 0),
            Transform(halfWidth, halfDepth, 0),
            Transform(-halfWidth, halfDepth, 0),

            Transform(-halfWidth, -halfDepth, height),
            Transform(halfWidth, -halfDepth, height),
            Transform(halfWidth, halfDepth, height),
            Transform(-halfWidth, halfDepth, height)
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

    private static float DegreesToRadians(double degrees) =>
        (float)(degrees * Math.PI / 180.0);
}
