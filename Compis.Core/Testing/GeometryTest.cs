
using Compis.Core.Objects;
using Compis.Core.Parameters;
using Compis.Core.Geometry.Generators;

namespace Compis.Core.Testing;

public static class GeometryTest
{
    public static void Run()
    {
        var building = new BuildingVolume(
            "Test Building",
            new Dimensions3D(10, 8, 3),
            new Position3D(0, 0, 0)
        );

        var mesh = BoxMeshGenerator.Generate(building);

        Console.WriteLine($"Vertices: {mesh.Vertices.Count}");
        Console.WriteLine($"Triangles: {mesh.Triangles.Count / 3}");

        var transformProbe = new BuildingVolume(
            "Transform Probe",
            new Dimensions3D(2, 2, 2),
            new Position3D(10, 20, 30));

        AssertVertex(
            BoxMeshGenerator.Generate(transformProbe).Vertices[0],
            9, 19, 30,
            "untransformed box");

        transformProbe.ScaleTo(new Scale3D(2, 1, 1));
        AssertVertex(
            BoxMeshGenerator.Generate(transformProbe).Vertices[0],
            8, 19, 30,
            "scaled box");

        transformProbe.ScaleTo(Scale3D.One);
        transformProbe.RotateTo(new Rotation3D(0, 0, 90));
        AssertVertex(
            BoxMeshGenerator.Generate(transformProbe).Vertices[0],
            11, 19, 30,
            "rotated box");

        Console.WriteLine("Transform checks passed.");
    }

    private static void AssertVertex(
        Position3D vertex,
        double expectedX,
        double expectedY,
        double expectedZ,
        string label)
    {
        const double tolerance = 0.0001;

        if (Math.Abs(vertex.X - expectedX) > tolerance ||
            Math.Abs(vertex.Y - expectedY) > tolerance ||
            Math.Abs(vertex.Z - expectedZ) > tolerance)
        {
            throw new InvalidOperationException(
                $"{label} vertex mismatch: {vertex}");
        }
    }
}
