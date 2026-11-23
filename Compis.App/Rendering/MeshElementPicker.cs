using System;
using System.Collections.Generic;
using System.Numerics;
using Compis.Core.Geometry.Generators;
using Compis.Core.Objects;
using Compis.Core.Parameters;
using Compis.Core.Scene;

namespace Compis.App.Rendering;

public static class MeshElementPicker
{
    private const float PickTolerancePixels = 12f;

    private static readonly int[][] Faces =
    [
        [0, 1, 2, 3],
        [4, 5, 6, 7],
        [0, 1, 5, 4],
        [2, 3, 7, 6],
        [0, 4, 7, 3],
        [1, 2, 6, 5]
    ];

    public static MeshElementSelection? Pick(
        Vector2 pointer,
        float width,
        float height,
        Matrix4x4 view,
        Matrix4x4 projection,
        BuildingScene scene,
        MeshElementMode mode)
    {
        if (mode == MeshElementMode.Object || width <= 0 || height <= 0)
            return null;

        Matrix4x4 viewProjection = view * projection;
        if (!TryGetRay(pointer, width, height, viewProjection,
                out Vector3 rayOrigin, out Vector3 rayDirection))
            return null;

        MeshElementSelection? nearest = null;
        float nearestScore = mode == MeshElementMode.Face
            ? float.PositiveInfinity
            : PickTolerancePixels;

        foreach (BuildingVolume volume in scene.Volumes)
        {
            var mesh = BoxMeshGenerator.Generate(volume.Parameters);

            if (mode == MeshElementMode.Vertex)
            {
                for (int vertexIndex = 0; vertexIndex < mesh.Vertices.Count; vertexIndex++)
                {
                    Vector3 vertex = ToVector3(mesh.Vertices[vertexIndex]);
                    if (!TryProject(vertex, viewProjection, width, height, out Vector2 screen))
                        continue;

                    float screenDistance = Vector2.Distance(pointer, screen);
                    float rayDistance = Vector3.Dot(vertex - rayOrigin, rayDirection);
                    if (screenDistance < nearestScore && rayDistance >= 0f)
                    {
                        nearestScore = screenDistance;
                        nearest = CreateSelection(volume, mode, vertexIndex, [vertexIndex], mesh.Vertices);
                    }
                }
            }
            else if (mode == MeshElementMode.Edge)
            {
                for (int edgeIndex = 0; edgeIndex < mesh.Edges.Count / 2; edgeIndex++)
                {
                    int firstIndex = mesh.Edges[edgeIndex * 2];
                    int secondIndex = mesh.Edges[edgeIndex * 2 + 1];
                    if (!TryProject(ToVector3(mesh.Vertices[firstIndex]), viewProjection, width, height, out Vector2 start) ||
                        !TryProject(ToVector3(mesh.Vertices[secondIndex]), viewProjection, width, height, out Vector2 end))
                        continue;

                    float screenDistance = DistanceToSegment(pointer, start, end);
                    Vector3 center = (ToVector3(mesh.Vertices[firstIndex]) + ToVector3(mesh.Vertices[secondIndex])) * 0.5f;
                    float rayDistance = Vector3.Dot(center - rayOrigin, rayDirection);
                    if (screenDistance < nearestScore && rayDistance >= 0f)
                    {
                        nearestScore = screenDistance;
                        nearest = CreateSelection(volume, mode, edgeIndex, [firstIndex, secondIndex], mesh.Vertices);
                    }
                }
            }
            else
            {
                for (int triangleIndex = 0; triangleIndex < mesh.Triangles.Count; triangleIndex += 3)
                {
                    Vector3 first = ToVector3(mesh.Vertices[mesh.Triangles[triangleIndex]]);
                    Vector3 second = ToVector3(mesh.Vertices[mesh.Triangles[triangleIndex + 1]]);
                    Vector3 third = ToVector3(mesh.Vertices[mesh.Triangles[triangleIndex + 2]]);
                    if (!TryIntersectTriangle(rayOrigin, rayDirection, first, second, third, out float distance) ||
                        distance >= nearestScore)
                        continue;

                    int faceIndex = triangleIndex / 6;
                    nearestScore = distance;
                    nearest = CreateSelection(volume, mode, faceIndex, Faces[faceIndex], mesh.Vertices);
                }
            }
        }

        return nearest;
    }

    public static bool TryGetViewPlanePoint(
        Vector2 pointer,
        float width,
        float height,
        Matrix4x4 view,
        Matrix4x4 projection,
        Vector3 planePoint,
        Vector3 planeNormal,
        out Vector3 intersection)
    {
        intersection = default;
        if (!TryGetRay(pointer, width, height, view * projection,
                out Vector3 rayOrigin, out Vector3 rayDirection))
            return false;

        float denominator = Vector3.Dot(rayDirection, planeNormal);
        if (MathF.Abs(denominator) < 0.0001f)
            return false;

        float distance = Vector3.Dot(planePoint - rayOrigin, planeNormal) / denominator;
        if (distance < 0f)
            return false;

        intersection = rayOrigin + rayDirection * distance;
        return true;
    }

    private static MeshElementSelection CreateSelection(
        BuildingVolume volume,
        MeshElementMode mode,
        int elementIndex,
        IReadOnlyList<int> vertexIndices,
        IReadOnlyList<Position3D> vertices)
    {
        Vector3 centroid = Vector3.Zero;
        foreach (int vertexIndex in vertexIndices)
            centroid += ToVector3(vertices[vertexIndex]);
        centroid /= vertexIndices.Count;
        return new MeshElementSelection(volume, mode, elementIndex, new List<int>(vertexIndices).ToArray(), centroid);
    }

    private static bool TryGetRay(
        Vector2 pointer,
        float width,
        float height,
        Matrix4x4 viewProjection,
        out Vector3 rayOrigin,
        out Vector3 rayDirection)
    {
        rayOrigin = default;
        rayDirection = default;
        if (!Matrix4x4.Invert(viewProjection, out Matrix4x4 inverse))
            return false;

        float x = pointer.X / width * 2f - 1f;
        float y = 1f - pointer.Y / height * 2f;
        Vector4 near = Vector4.Transform(new Vector4(x, y, -1f, 1f), inverse);
        Vector4 far = Vector4.Transform(new Vector4(x, y, 1f, 1f), inverse);
        if (MathF.Abs(near.W) < 0.0001f || MathF.Abs(far.W) < 0.0001f)
            return false;

        rayOrigin = new Vector3(near.X, near.Y, near.Z) / near.W;
        Vector3 rayEnd = new(far.X / far.W, far.Y / far.W, far.Z / far.W);
        Vector3 direction = rayEnd - rayOrigin;
        if (direction.LengthSquared() < 0.000001f)
            return false;
        rayDirection = Vector3.Normalize(direction);
        return true;
    }

    private static bool TryProject(Vector3 point, Matrix4x4 viewProjection, float width, float height, out Vector2 screen)
    {
        Vector4 clip = Vector4.Transform(new Vector4(point, 1f), viewProjection);
        if (clip.W <= 0.0001f || clip.Z < -clip.W || clip.Z > clip.W)
        {
            screen = default;
            return false;
        }

        screen = new Vector2(
            (clip.X / clip.W + 1f) * 0.5f * width,
            (1f - clip.Y / clip.W) * 0.5f * height);
        return true;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.LengthSquared();
        if (lengthSquared < 0.000001f)
            return Vector2.Distance(point, start);

        float amount = Math.Clamp(Vector2.Dot(point - start, segment) / lengthSquared, 0f, 1f);
        return Vector2.Distance(point, start + segment * amount);
    }

    private static bool TryIntersectTriangle(
        Vector3 origin,
        Vector3 direction,
        Vector3 first,
        Vector3 second,
        Vector3 third,
        out float distance)
    {
        Vector3 edgeOne = second - first;
        Vector3 edgeTwo = third - first;
        Vector3 cross = Vector3.Cross(direction, edgeTwo);
        float determinant = Vector3.Dot(edgeOne, cross);
        if (MathF.Abs(determinant) < 0.000001f)
        {
            distance = 0f;
            return false;
        }

        float inverse = 1f / determinant;
        Vector3 offset = origin - first;
        float u = Vector3.Dot(offset, cross) * inverse;
        if (u < 0f || u > 1f)
        {
            distance = 0f;
            return false;
        }

        Vector3 q = Vector3.Cross(offset, edgeOne);
        float v = Vector3.Dot(direction, q) * inverse;
        if (v < 0f || u + v > 1f)
        {
            distance = 0f;
            return false;
        }

        distance = Vector3.Dot(edgeTwo, q) * inverse;
        return distance >= 0f;
    }

    private static Vector3 ToVector3(Position3D point) =>
        new((float)point.X, (float)point.Y, (float)point.Z);
}