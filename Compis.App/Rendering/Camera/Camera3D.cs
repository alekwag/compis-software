
using System;
using System.Numerics;

namespace Compis.App.Rendering.Camera;

public sealed class Camera3D
{
    public Vector3 Position { get; private set; }

    public Vector3 Target { get; private set; }

    public Vector3 Up { get; private set; }

    public float FieldOfView { get; set; } = 45f;

    public float NearPlane { get; set; } = 0.1f;

    public float FarPlane { get; set; } = 1000f;

    public Camera3D()
    {
        Position = new Vector3(15f, -15f, 12f);
        Target = Vector3.Zero;
        Up = Vector3.UnitZ;
    }

    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(
            Position,
            Target,
            Up
        );
    }

    public Matrix4x4 GetProjectionMatrix(float aspectRatio)
    {
        return Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI * FieldOfView / 180f,
            aspectRatio,
            NearPlane,
            FarPlane
        );
    }

    public void SetPosition(Vector3 position)
    {
        Position = position;
    }

    public void SetTarget(Vector3 target)
    {
        Target = target;
    }


    public void Orbit(float deltaYaw, float deltaPitch)
    {
        Vector3 offset = Position - Target;

        float radius = offset.Length();

        float yaw = MathF.Atan2(offset.Y, offset.X);
        float pitch = MathF.Asin(offset.Z / radius);

        yaw += deltaYaw;
        pitch = Math.Clamp(
            pitch + deltaPitch,
            -1.45f,
            1.45f
        );

        Position = Target + new Vector3(
            radius * MathF.Cos(pitch) * MathF.Cos(yaw),
            radius * MathF.Cos(pitch) * MathF.Sin(yaw),
            radius * MathF.Sin(pitch)
        );
    }

    public void Zoom(float amount)
    {
        Vector3 offset = Position - Target;
        float distance = offset.Length();

        distance = Math.Clamp(distance * MathF.Exp(-amount), 1f, 500f);

        Position = Target + Vector3.Normalize(offset) * distance;
    }

    public void Pan(float deltaX, float deltaY)
    {
        Vector3 forward = Vector3.Normalize(Target - Position);
        Vector3 right = Vector3.Normalize(
            Vector3.Cross(forward, Up)
        );

        Vector3 movement =
            right * deltaX +
            Up * deltaY;

        Position += movement;
        Target += movement;
    }


    public void ZoomTowards(float amount)
    {
        Vector3 offset = Position - Target;
        float distance = offset.Length();

        distance = Math.Clamp(
            distance * MathF.Exp(-amount),
            1f,
            500f
        );

        Position = Target + Vector3.Normalize(offset) * distance;
    }


}
