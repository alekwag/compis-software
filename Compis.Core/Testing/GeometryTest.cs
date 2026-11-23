
using System.Numerics;
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

        var mesh = BoxMeshGenerator.Generate(building.Parameters);

        Console.WriteLine($"Vertices: {mesh.Vertices.Count}");
        Console.WriteLine($"Triangles: {mesh.Triangles.Count / 3}");
        if (mesh.Edges.Count != 24)
            throw new InvalidOperationException($"Expected 12 box edges, got {mesh.Edges.Count / 2}.");


        var transformProbe = new BuildingVolume(
            "Transform Probe",
            new Dimensions3D(2, 2, 2),
            new Position3D(10, 20, 30));

        AssertVertex(
            BoxMeshGenerator.Generate(transformProbe.Parameters).Vertices[0],
            9, 19, 30,
            "untransformed box");

        transformProbe.ScaleTo(new Scale3D(2, 1, 1));
        AssertVertex(
            BoxMeshGenerator.Generate(transformProbe.Parameters).Vertices[0],
            8, 19, 30,
            "scaled box");

        transformProbe.ScaleTo(Scale3D.One);
        transformProbe.RotateTo(new Rotation3D(0, 0, 90));
        AssertVertex(
            BoxMeshGenerator.Generate(transformProbe.Parameters).Vertices[0],
            11, 19, 30,
            "rotated box");

        transformProbe = new BuildingVolume(
            "Editable Mesh Probe",
            new Dimensions3D(2, 2, 2),
            new Position3D(0, 0, 0));
        transformProbe.MoveMeshVertices([0], new Position3D(0, 0, 1));
        var editedMesh = BoxMeshGenerator.Generate(transformProbe.Parameters);
        AssertVertex(editedMesh.Vertices[0], -1, -1, 1, "edited vertex");
        AssertVertex(editedMesh.Vertices[1], 1, -1, 0, "untouched vertex");

        transformProbe.SetMeshVertexWorldPositions(
            new Dictionary<int, Vector3> { [0] = new Vector3(-1, -1, 2) });
        AssertVertex(
            BoxMeshGenerator.Generate(transformProbe.Parameters).Vertices[0],
            -1, -1, 2,
            "world-space vertex edit");

        var faceProbe = new BuildingVolume(
            "Face Edit Probe",
            new Dimensions3D(2, 2, 2),
            new Position3D(0, 0, 0));
        faceProbe.MoveMeshVertices([4, 5, 6, 7], new Position3D(0, 0, 1));
        var faceMesh = BoxMeshGenerator.Generate(faceProbe.Parameters);
        foreach (int topVertex in new[] { 4, 5, 6, 7 })
            AssertVertex(faceMesh.Vertices[topVertex],
                (float)faceMesh.Vertices[topVertex].X,
                (float)faceMesh.Vertices[topVertex].Y,
                3,
                "moved face vertex");
        AssertVertex(faceMesh.Vertices[0], -1, -1, 0, "unselected face vertex");

        var rotatedProbe = new BuildingVolume(
            "Rotated Edit Probe",
            new Dimensions3D(2, 2, 2),
            new Position3D(5, 6, 7));
        rotatedProbe.ScaleTo(new Scale3D(2, 1, 1));
        rotatedProbe.RotateTo(new Rotation3D(0, 0, 90));
        Position3D originalVertex = BoxMeshGenerator.Generate(rotatedProbe.Parameters).Vertices[0];
        Vector3 targetVertex = new((float)originalVertex.X, (float)originalVertex.Y, (float)originalVertex.Z + 1);
        rotatedProbe.SetMeshVertexWorldPositions(new Dictionary<int, Vector3> { [0] = targetVertex });
        AssertVertex(BoxMeshGenerator.Generate(rotatedProbe.Parameters).Vertices[0],
            targetVertex.X, targetVertex.Y, targetVertex.Z,
            "vertex edit with rotation and scale");

        var originalColor = faceProbe.Color;
        faceProbe.SetColor(new MaterialColor(0.1f, 0.6f, 0.9f));
        if (faceProbe.Parameters.Color == originalColor || faceProbe.Parameters.Color != faceProbe.Color)
            throw new InvalidOperationException("Box material color was not stored in its parameters.");

        Console.WriteLine("Transform, component-edit, and material checks passed.");
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
