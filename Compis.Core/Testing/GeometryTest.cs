
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
    }
}
