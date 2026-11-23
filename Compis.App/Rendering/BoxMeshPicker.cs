using System;
using System.Numerics;
using Compis.Core.Geometry.Generators;
using Compis.Core.Objects;
using Compis.Core.Parameters;
using Compis.Core.Scene;

namespace Compis.App.Rendering;

public static class BoxMeshPicker
{
    public static BuildingVolume? PickNearest(
        Vector2 pointerPosition,
        float viewportWidth,
        float viewportHeight,
        Matrix4x4 view,
        Matrix4x4 projection,
        BuildingScene scene)
    {
        if (viewportWidth <= 0 || viewportHeight <= 0 ||
            !Matrix4x4.Invert(view * projection, out Matrix4x4 inverseViewProjection))
            return null;

        float ndcX = pointerPosition.X / viewportWidth * 2f - 1f;
        float ndcY = 1f - pointerPosition.Y / viewportHeight * 2f;
        Vector4 nearClip = Vector4.Transform(new Vector4(ndcX, ndcY, -1f, 1f), inverseViewProjection);
        Vector4 farClip = Vector4.Transform(new Vector4(ndcX, ndcY, 1f, 1f), inverseViewProjection);

        if (MathF.Abs(nearClip.W) < 0.0001f || MathF.Abs(farClip.W) < 0.0001f)
            return null;

        Vector3 rayOrigin = new(nearClip.X / nearClip.W, nearClip.Y / nearClip.W, nearClip.Z / nearClip.W);
        Vector3 farPoint = new(farClip.X / farClip.W, farClip.Y / farClip.W, farClip.Z / farClip.W);
        Vector3 rayDirection = Vector3.Normalize(farPoint - rayOrigin);

        BuildingVolume? nearestVolume = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (BuildingVolume volume in scene.Volumes)
        {
            var mesh = BoxMeshGenerator.Generate(volume.Parameters);
            for (int index = 0; index < mesh.Triangles.Count; index += 3)
            {
                Position3D first = mesh.Vertices[mesh.Triangles[index]];
                Position3D second = mesh.Vertices[mesh.Triangles[index + 1]];
                Position3D third = mesh.Vertices[mesh.Triangles[index + 2]];

                if (!TryIntersectTriangle(
                        rayOrigin,
                        rayDirection,
                        ToVector3(first),
                        ToVector3(second),
                        ToVector3(third),
                        out float distance) || distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                nearestVolume = volume;
            }
        }

        return nearestVolume;
    }

    private static bool TryIntersectTriangle(
        Vector3 rayOrigin,
        Vector3 rayDirection,
        Vector3 first,
        Vector3 second,
        Vector3 third,
        out float distance)
    {
        const float epsilon = 0.000001f;
        Vector3 edgeOne = second - first;
        Vector3 edgeTwo = third - first;
        Vector3 cross = Vector3.Cross(rayDirection, edgeTwo);
        float determinant = Vector3.Dot(edgeOne, cross);

        if (MathF.Abs(determinant) < epsilon)
        {
            distance = 0f;
            return false;
        }

        float inverseDeterminant = 1f / determinant;
        Vector3 offset = rayOrigin - first;
        float barycentricU = Vector3.Dot(offset, cross) * inverseDeterminant;
        if (barycentricU < 0f || barycentricU > 1f)
        {
            distance = 0f;
            return false;
        }

        Vector3 q = Vector3.Cross(offset, edgeOne);
        float barycentricV = Vector3.Dot(rayDirection, q) * inverseDeterminant;
        if (barycentricV < 0f || barycentricU + barycentricV > 1f)
        {
            distance = 0f;
            return false;
        }

        distance = Vector3.Dot(edgeTwo, q) * inverseDeterminant;
        return distance >= 0f;
    }

    private static Vector3 ToVector3(Position3D point) =>
        new((float)point.X, (float)point.Y, (float)point.Z);
}