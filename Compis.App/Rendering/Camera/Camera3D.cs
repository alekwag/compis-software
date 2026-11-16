
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
}
