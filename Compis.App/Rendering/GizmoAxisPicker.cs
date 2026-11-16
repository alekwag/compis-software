
using System;
using System.Numerics;

namespace Compis.App.Rendering;

public enum GizmoAxis
{
    None,
    X,
    Y,
    Z
}

public static class GizmoAxisPicker
{
    private const float AxisLength = 3f;
    private const float PickTolerancePixels = 10f;
    private const float RotationRingRadius = 2.2f;
    private const int RotationRingSegments = 64;

    public static GizmoAxis PickAxis(
        Vector2 mousePosition,
        float viewportWidth,
        float viewportHeight,
        Vector3 origin,
        Matrix4x4 view,
        Matrix4x4 projection,
        Vector3 axisX,
        Vector3 axisY,
        Vector3 axisZ)
    {
        if (viewportWidth <= 0 || viewportHeight <= 0)
            return GizmoAxis.None;

        Matrix4x4 viewProjection = view * projection;
        float closestDistance = PickTolerancePixels;
        GizmoAxis selectedAxis = GizmoAxis.None;

        TestAxis(
            mousePosition, viewportWidth, viewportHeight, origin,
            axisX, GizmoAxis.X, viewProjection,
            ref closestDistance, ref selectedAxis);

        TestAxis(
            mousePosition, viewportWidth, viewportHeight, origin,
            axisY, GizmoAxis.Y, viewProjection,
            ref closestDistance, ref selectedAxis);

        TestAxis(
            mousePosition, viewportWidth, viewportHeight, origin,
            axisZ, GizmoAxis.Z, viewProjection,
            ref closestDistance, ref selectedAxis);

        return selectedAxis;
    }

    public static GizmoAxis PickRotationAxis(
        Vector2 mousePosition,
        float viewportWidth,
        float viewportHeight,
        Vector3 origin,
        Matrix4x4 view,
        Matrix4x4 projection,
        Vector3 axisX,
        Vector3 axisY,
        Vector3 axisZ)
    {
        if (viewportWidth <= 0 || viewportHeight <= 0)
            return GizmoAxis.None;

        Matrix4x4 viewProjection = view * projection;
        float closestDistance = PickTolerancePixels;
        GizmoAxis selectedAxis = GizmoAxis.None;

        TestRing(mousePosition, viewportWidth, viewportHeight, origin,
            axisY, axisZ, GizmoAxis.X, viewProjection,
            ref closestDistance, ref selectedAxis);
        TestRing(mousePosition, viewportWidth, viewportHeight, origin,
            axisZ, axisX, GizmoAxis.Y, viewProjection,
            ref closestDistance, ref selectedAxis);
        TestRing(mousePosition, viewportWidth, viewportHeight, origin,
            axisX, axisY, GizmoAxis.Z, viewProjection,
            ref closestDistance, ref selectedAxis);

        return selectedAxis;
    }

    public static bool TryGetRotationAngle(
        Vector2 mousePosition,
        float viewportWidth,
        float viewportHeight,
        Vector3 origin,
        Vector3 axisDirection,
        Vector3 basisU,
        Vector3 basisV,
        Matrix4x4 view,
        Matrix4x4 projection,
        out float angle)
    {
        angle = 0f;
        if (viewportWidth <= 0 || viewportHeight <= 0 ||
            !TryGetRay(mousePosition, viewportWidth, viewportHeight,
                view * projection, out Vector3 rayOrigin, out Vector3 rayDirection))
            return false;

        axisDirection = Vector3.Normalize(axisDirection);
        float denominator = Vector3.Dot(rayDirection, axisDirection);
        if (MathF.Abs(denominator) < 0.0001f)
            return false;

        float rayParameter = Vector3.Dot(origin - rayOrigin, axisDirection) / denominator;
        if (rayParameter < 0f)
            return false;

        Vector3 radial = rayOrigin + rayDirection * rayParameter - origin;
        float coordinateU = Vector3.Dot(radial, basisU);
        float coordinateV = Vector3.Dot(radial, basisV);
        if (coordinateU * coordinateU + coordinateV * coordinateV < 0.0001f)
            return false;

        angle = MathF.Atan2(coordinateV, coordinateU);
        return true;
    }

    private static void TestRing(
        Vector2 mousePosition,
        float viewportWidth,
        float viewportHeight,
        Vector3 origin,
        Vector3 basisU,
        Vector3 basisV,
        GizmoAxis axis,
        Matrix4x4 viewProjection,
        ref float closestDistance,
        ref GizmoAxis selectedAxis)
    {
        Vector2 previous = default;
        bool hasPrevious = false;

        for (int index = 0; index <= RotationRingSegments; index++)
        {
            float angle = index * MathF.Tau / RotationRingSegments;
            Vector3 point = origin + RotationRingRadius *
                (basisU * MathF.Cos(angle) + basisV * MathF.Sin(angle));

            if (!TryProject(point, viewProjection, viewportWidth, viewportHeight,
                    out Vector2 projected))
            {
                hasPrevious = false;
                continue;
            }

            if (hasPrevious)
            {
                float distance = DistanceToSegment(mousePosition, previous, projected);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    selectedAxis = axis;
                }
            }

            previous = projected;
            hasPrevious = true;
        }
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.LengthSquared();
        if (lengthSquared <= float.Epsilon)
            return Vector2.Distance(point, start);

        float parameter = Math.Clamp(
            Vector2.Dot(point - start, segment) / lengthSquared,
            0f,
            1f);
        return Vector2.Distance(point, start + segment * parameter);
    }

    private static bool TryGetRay(
        Vector2 mousePosition,
        float viewportWidth,
        float viewportHeight,
        Matrix4x4 viewProjection,
        out Vector3 rayOrigin,
        out Vector3 rayDirection)
    {
        rayOrigin = default;
        rayDirection = default;
        if (!Matrix4x4.Invert(viewProjection, out Matrix4x4 inverse))
            return false;

        float ndcX = 2f * mousePosition.X / viewportWidth - 1f;
        float ndcY = 1f - 2f * mousePosition.Y / viewportHeight;
        Vector4 nearPoint = Vector4.Transform(new Vector4(ndcX, ndcY, -1f, 1f), inverse);
        Vector4 farPoint = Vector4.Transform(new Vector4(ndcX, ndcY, 1f, 1f), inverse);
        if (MathF.Abs(nearPoint.W) < 0.0001f || MathF.Abs(farPoint.W) < 0.0001f)
            return false;

        rayOrigin = new Vector3(nearPoint.X, nearPoint.Y, nearPoint.Z) / nearPoint.W;
        Vector3 rayEnd = new Vector3(farPoint.X, farPoint.Y, farPoint.Z) / farPoint.W;
        Vector3 rayVector = rayEnd - rayOrigin;
        if (rayVector.LengthSquared() < 0.0001f)
            return false;

        rayDirection = Vector3.Normalize(rayVector);
        return true;
    }

    private static void TestAxis(
        Vector2 mousePosition,
        float viewportWidth,
        float viewportHeight,
        Vector3 axisOrigin,
        Vector3 axisDirection,
        GizmoAxis axis,
        Matrix4x4 viewProjection,
        ref float closestDistance,
        ref GizmoAxis selectedAxis)
    {
        Vector3 segmentEnd = axisOrigin + axisDirection * AxisLength;
        if (!TryProject(axisOrigin, viewProjection, viewportWidth, viewportHeight, out Vector2 start) ||
            !TryProject(segmentEnd, viewProjection, viewportWidth, viewportHeight, out Vector2 end))
            return;

        Vector2 segment = end - start;
        float segmentLengthSquared = segment.LengthSquared();
        if (segmentLengthSquared <= float.Epsilon)
            return;

        float segmentParameter = Math.Clamp(
            Vector2.Dot(mousePosition - start, segment) / segmentLengthSquared,
            0f,
            1f);
        Vector2 closestPoint = start + segment * segmentParameter;
        float distance = Vector2.Distance(mousePosition, closestPoint);

        if (distance < closestDistance)
        {
            closestDistance = distance;
            selectedAxis = axis;
        }
    }

    private static bool TryProject(
        Vector3 position,
        Matrix4x4 viewProjection,
        float viewportWidth,
        float viewportHeight,
        out Vector2 screenPosition)
    {
        Vector4 clip = Vector4.Transform(new Vector4(position, 1f), viewProjection);
        if (clip.W <= 0.0001f || clip.Z < -clip.W || clip.Z > clip.W)
        {
            screenPosition = default;
            return false;
        }

        float inverseW = 1f / clip.W;
        float ndcX = clip.X * inverseW;
        float ndcY = clip.Y * inverseW;

        screenPosition = new Vector2(
            (ndcX + 1f) * 0.5f * viewportWidth,
            (1f - ndcY) * 0.5f * viewportHeight);
        return true;
    }


    public static float GetAxisParameter(
        Vector2 mousePosition,
        float viewportWidth,
        float viewportHeight,
        Vector3 origin,
        Vector3 axisDirection,
        Matrix4x4 view,
        Matrix4x4 projection)
    {
        Matrix4x4 viewProjection = view * projection;

        if (!Matrix4x4.Invert(viewProjection, out Matrix4x4 inverse))
            return 0f;

        float ndcX = 2f * mousePosition.X / viewportWidth - 1f;
        float ndcY = 1f - 2f * mousePosition.Y / viewportHeight;

        Vector4 nearPoint = Vector4.Transform(
            new Vector4(ndcX, ndcY, -1f, 1f), inverse);

        Vector4 farPoint = Vector4.Transform(
            new Vector4(ndcX, ndcY, 1f, 1f), inverse);

        if (MathF.Abs(nearPoint.W) < 0.0001f ||
            MathF.Abs(farPoint.W) < 0.0001f)
            return 0f;

        Vector3 rayOrigin = new(
            nearPoint.X / nearPoint.W,
            nearPoint.Y / nearPoint.W,
            nearPoint.Z / nearPoint.W);

        Vector3 rayEnd = new(
            farPoint.X / farPoint.W,
            farPoint.Y / farPoint.W,
            farPoint.Z / farPoint.W);

        Vector3 rayDirection =
            Vector3.Normalize(rayEnd - rayOrigin);

        axisDirection = Vector3.Normalize(axisDirection);

        Vector3 offset = rayOrigin - origin;

        float a = Vector3.Dot(rayDirection, rayDirection);
        float b = Vector3.Dot(rayDirection, axisDirection);
        float c = Vector3.Dot(axisDirection, axisDirection);
        float d = Vector3.Dot(rayDirection, offset);
        float e = Vector3.Dot(axisDirection, offset);

        float denominator = a * c - b * b;

        if (MathF.Abs(denominator) < 0.0001f)
            return e / c;

        float rayParameter = (b * e - c * d) / denominator;
        float axisParameter = (a * e - b * d) / denominator;

        if (rayParameter < 0f)
            axisParameter = e / c;

        return axisParameter;
    }

}
