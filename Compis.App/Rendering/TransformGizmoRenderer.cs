
using System;
using System.Collections.Generic;
using System.Numerics;
using Compis.Core.Parameters;
using Silk.NET.OpenGL;

namespace Compis.App.Rendering;

public sealed class TransformGizmoRenderer
{
    private readonly GL _gl;

    private uint _vertexArray;
    private uint _vertexBuffer;
    private uint _indexBuffer;
    private uint _shaderProgram;

    private int _viewLocation;
    private int _projectionLocation;

    private const int Segments = 16;

    private const float AxisLength = 3f;
    private const float ShaftRadius = 0.075f;
    private const float ArrowRadius = 0.24f;
    private const float ArrowLength = 0.65f;
    private const float RotationRingRadius = 2.2f;

    public TransformGizmoRenderer(GL gl)
    {
        _gl = gl;
    }

    public unsafe void Initialize()
    {
        _vertexArray = _gl.GenVertexArray();
        _vertexBuffer = _gl.GenBuffer();
        _indexBuffer = _gl.GenBuffer();

        _gl.BindVertexArray(_vertexArray);

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            _vertexBuffer);

        _gl.BindBuffer(
            BufferTargetARB.ElementArrayBuffer,
            _indexBuffer);

        _gl.VertexAttribPointer(
            0, 3, VertexAttribPointerType.Float,
            false, 6 * sizeof(float), (void*)0);

        _gl.EnableVertexAttribArray(0);

        _gl.VertexAttribPointer(
            1, 3, VertexAttribPointerType.Float,
            false, 6 * sizeof(float), (void*)(3 * sizeof(float)));

        _gl.EnableVertexAttribArray(1);

        _shaderProgram = CreateShaderProgram();

        _viewLocation =
            _gl.GetUniformLocation(_shaderProgram, "view");

        _projectionLocation =
            _gl.GetUniformLocation(_shaderProgram, "projection");

        _gl.BindVertexArray(0);
    }

    public unsafe void Render(
        Position3D position,
        Rotation3D rotation,
        TransformMode mode,
        TransformSpace space,
        Matrix4x4 view,
        Matrix4x4 projection)
    {
        var vertices = new List<float>();
        var indices = new List<uint>();

        Vector3 origin = new(
            (float)position.X,
            (float)position.Y,
            (float)position.Z);

        Vector3 xColour = new(0.90f, 0.20f, 0.18f);
        Vector3 yColour = new(0.25f, 0.80f, 0.30f);
        Vector3 zColour = new(0.25f, 0.50f, 1.0f);
        Quaternion orientation = space == TransformSpace.Local
            ? Quaternion.CreateFromYawPitchRoll(
                DegreesToRadians(rotation.Y),
                DegreesToRadians(rotation.X),
                DegreesToRadians(rotation.Z))
            : Quaternion.Identity;
        Vector3 axisX = Vector3.Transform(Vector3.UnitX, orientation);
        Vector3 axisY = Vector3.Transform(Vector3.UnitY, orientation);
        Vector3 axisZ = Vector3.Transform(Vector3.UnitZ, orientation);

        switch (mode)
        {
            case TransformMode.Rotate:
                AddRing(vertices, indices, origin, axisY, axisZ, xColour);
                AddRing(vertices, indices, origin, axisZ, axisX, yColour);
                AddRing(vertices, indices, origin, axisX, axisY, zColour);
                break;
            case TransformMode.Scale:
                AddScaleAxis(vertices, indices, origin, axisX, xColour);
                AddScaleAxis(vertices, indices, origin, axisY, yColour);
                AddScaleAxis(vertices, indices, origin, axisZ, zColour);
                break;
            default:
                AddArrow(vertices, indices, origin, axisX, xColour);
                AddArrow(vertices, indices, origin, axisY, yColour);
                AddArrow(vertices, indices, origin, axisZ, zColour);
                break;
        }

        float[] vertexData = vertices.ToArray();
        uint[] indexData = indices.ToArray();

        _gl.BindVertexArray(_vertexArray);

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            _vertexBuffer);

        fixed (float* vertexPointer = vertexData)
        {
            _gl.BufferData(
                BufferTargetARB.ArrayBuffer,
                (nuint)(vertexData.Length * sizeof(float)),
                vertexPointer,
                BufferUsageARB.DynamicDraw);
        }

        _gl.BindBuffer(
            BufferTargetARB.ElementArrayBuffer,
            _indexBuffer);

        fixed (uint* indexPointer = indexData)
        {
            _gl.BufferData(
                BufferTargetARB.ElementArrayBuffer,
                (nuint)(indexData.Length * sizeof(uint)),
                indexPointer,
                BufferUsageARB.DynamicDraw);
        }

        _gl.UseProgram(_shaderProgram);

        _gl.UniformMatrix4(
            _viewLocation, 1, false, (float*)&view);

        _gl.UniformMatrix4(
            _projectionLocation, 1, false, (float*)&projection);

        // Keep the gizmo visible over the selected building.
        _gl.Disable(EnableCap.DepthTest);

        _gl.DrawElements(
            PrimitiveType.Triangles,
            (uint)indexData.Length,
            DrawElementsType.UnsignedInt,
            (void*)0);

        _gl.Enable(EnableCap.DepthTest);

        _gl.BindVertexArray(0);
        _gl.UseProgram(0);
    }

    private static void AddArrow(
        List<float> vertices,
        List<uint> indices,
        Vector3 origin,
        Vector3 direction,
        Vector3 colour)
    {
        direction = Vector3.Normalize(direction);

        Vector3 reference =
            MathF.Abs(Vector3.Dot(direction, Vector3.UnitZ)) > 0.9f
                ? Vector3.UnitY
                : Vector3.UnitZ;

        Vector3 right = Vector3.Normalize(
            Vector3.Cross(direction, reference));

        Vector3 up = Vector3.Normalize(
            Vector3.Cross(direction, right));

        Vector3 shaftEnd =
            origin + direction * (AxisLength - ArrowLength);

        Vector3 arrowTip =
            origin + direction * AxisLength;

        AddCylinder(
            vertices, indices,
            origin, shaftEnd,
            right, up,
            ShaftRadius, colour);

        AddCone(
            vertices, indices,
            shaftEnd, arrowTip,
            right, up,
            ArrowRadius, colour);
    }

    private static void AddScaleAxis(
        List<float> vertices,
        List<uint> indices,
        Vector3 origin,
        Vector3 direction,
        Vector3 colour)
    {
        Vector3 reference =
            MathF.Abs(Vector3.Dot(direction, Vector3.UnitZ)) > 0.9f
                ? Vector3.UnitY
                : Vector3.UnitZ;
        Vector3 right = Vector3.Normalize(Vector3.Cross(direction, reference));
        Vector3 up = Vector3.Normalize(Vector3.Cross(direction, right));
        Vector3 handleCenter = origin + direction * AxisLength;

        AddCylinder(
            vertices, indices,
            origin, handleCenter - direction * 0.18f,
            right, up,
            ShaftRadius, colour);
        AddCube(vertices, indices, handleCenter, 0.18f, colour);
    }

    private static void AddRing(
        List<float> vertices,
        List<uint> indices,
        Vector3 origin,
        Vector3 basisU,
        Vector3 basisV,
        Vector3 colour)
    {
        const int tubeSegments = 8;
        const float tubeRadius = 0.035f;
        Vector3 normal = Vector3.Normalize(Vector3.Cross(basisU, basisV));
        uint start = (uint)(vertices.Count / 6);

        for (int ringIndex = 0; ringIndex <= Segments; ringIndex++)
        {
            float angle = ringIndex * MathF.Tau / Segments;
            Vector3 radial = basisU * MathF.Cos(angle) + basisV * MathF.Sin(angle);
            Vector3 center = origin + radial * RotationRingRadius;

            for (int tubeIndex = 0; tubeIndex <= tubeSegments; tubeIndex++)
            {
                float tubeAngle = tubeIndex * MathF.Tau / tubeSegments;
                Vector3 offset = tubeRadius *
                    (radial * MathF.Cos(tubeAngle) + normal * MathF.Sin(tubeAngle));
                AddVertex(vertices, center + offset, colour);
            }
        }

        for (int ringIndex = 0; ringIndex < Segments; ringIndex++)
        {
            for (int tubeIndex = 0; tubeIndex < tubeSegments; tubeIndex++)
            {
                uint first = start + (uint)(ringIndex * (tubeSegments + 1) + tubeIndex);
                uint nextRing = first + (uint)(tubeSegments + 1);

                indices.Add(first);
                indices.Add(nextRing);
                indices.Add(first + 1);
                indices.Add(first + 1);
                indices.Add(nextRing);
                indices.Add(nextRing + 1);
            }
        }
    }

    private static void AddCube(
        List<float> vertices,
        List<uint> indices,
        Vector3 center,
        float halfSize,
        Vector3 colour)
    {
        uint start = (uint)(vertices.Count / 6);
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    AddVertex(vertices, center + new Vector3(x, y, z) * halfSize, colour);
                }
            }
        }

        uint[] triangles =
        [
            0, 1, 3, 0, 3, 2,
            4, 6, 7, 4, 7, 5,
            0, 4, 5, 0, 5, 1,
            2, 3, 7, 2, 7, 6,
            0, 2, 6, 0, 6, 4,
            1, 5, 7, 1, 7, 3
        ];

        foreach (uint triangle in triangles)
            indices.Add(start + triangle);
    }

    private static void AddCylinder(
        List<float> vertices,
        List<uint> indices,
        Vector3 start,
        Vector3 end,
        Vector3 right,
        Vector3 up,
        float radius,
        Vector3 colour)
    {
        uint startOffset = (uint)(vertices.Count / 6);

        for (int i = 0; i < Segments; i++)
        {
            float angle = i * MathF.Tau / Segments;

            Vector3 radial =
                (right * MathF.Cos(angle) +
                 up * MathF.Sin(angle)) * radius;

            AddVertex(vertices, start + radial, colour);
            AddVertex(vertices, end + radial, colour);
        }

        for (int i = 0; i < Segments; i++)
        {
            uint current = startOffset + (uint)(i * 2);
            uint next = startOffset + (uint)(((i + 1) % Segments) * 2);

            indices.Add(current);
            indices.Add(next);
            indices.Add(current + 1);

            indices.Add(next);
            indices.Add(next + 1);
            indices.Add(current + 1);
        }
    }

    private static void AddCone(
        List<float> vertices,
        List<uint> indices,
        Vector3 baseCentre,
        Vector3 tip,
        Vector3 right,
        Vector3 up,
        float radius,
        Vector3 colour)
    {
        uint baseOffset = (uint)(vertices.Count / 6);

        for (int i = 0; i < Segments; i++)
        {
            float angle = i * MathF.Tau / Segments;

            Vector3 radial =
                (right * MathF.Cos(angle) +
                 up * MathF.Sin(angle)) * radius;

            AddVertex(vertices, baseCentre + radial, colour);
        }

        uint tipIndex = (uint)(vertices.Count / 6);
        AddVertex(vertices, tip, colour);

        for (int i = 0; i < Segments; i++)
        {
            uint current = baseOffset + (uint)i;
            uint next = baseOffset + (uint)((i + 1) % Segments);

            indices.Add(current);
            indices.Add(next);
            indices.Add(tipIndex);
        }
    }

    private static void AddVertex(
        List<float> vertices,
        Vector3 position,
        Vector3 colour)
    {
        vertices.Add(position.X);
        vertices.Add(position.Y);
        vertices.Add(position.Z);

        vertices.Add(colour.X);
        vertices.Add(colour.Y);
        vertices.Add(colour.Z);
    }

    private static float DegreesToRadians(double degrees) =>
        (float)(degrees * Math.PI / 180.0);

    public void Dispose()
    {
        if (_indexBuffer != 0)
            _gl.DeleteBuffer(_indexBuffer);

        if (_vertexBuffer != 0)
            _gl.DeleteBuffer(_vertexBuffer);

        if (_vertexArray != 0)
            _gl.DeleteVertexArray(_vertexArray);

        if (_shaderProgram != 0)
            _gl.DeleteProgram(_shaderProgram);
    }

    private uint CreateShaderProgram()
    {
        const string vertexShader = """
            #version 330 core

            layout (location = 0) in vec3 aPosition;
            layout (location = 1) in vec3 aColor;

            uniform mat4 view;
            uniform mat4 projection;

            out vec3 vertexColor;

            void main()
            {
                gl_Position = projection * view * vec4(aPosition, 1.0);
                vertexColor = aColor;
            }
            """;

        const string fragmentShader = """
            #version 330 core

            in vec3 vertexColor;
            out vec4 FragColor;

            void main()
            {
                FragColor = vec4(vertexColor, 1.0);
            }
            """;

        uint vertex = CompileShader(
            ShaderType.VertexShader, vertexShader);

        uint fragment = CompileShader(
            ShaderType.FragmentShader, fragmentShader);

        uint program = _gl.CreateProgram();

        _gl.AttachShader(program, vertex);
        _gl.AttachShader(program, fragment);
        _gl.LinkProgram(program);

        _gl.GetProgram(program, GLEnum.LinkStatus, out int success);

        if (success == 0)
        {
            string log = _gl.GetProgramInfoLog(program);

            _gl.DeleteProgram(program);
            _gl.DeleteShader(vertex);
            _gl.DeleteShader(fragment);

            throw new InvalidOperationException(
                $"Gizmo shader linking failed: {log}");
        }

        _gl.DetachShader(program, vertex);
        _gl.DetachShader(program, fragment);

        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);

        return program;
    }

    private uint CompileShader(ShaderType type, string source)
    {
        uint shader = _gl.CreateShader(type);

        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, GLEnum.CompileStatus, out int success);

        if (success == 0)
        {
            string log = _gl.GetShaderInfoLog(shader);

            _gl.DeleteShader(shader);

            throw new InvalidOperationException(
                $"Gizmo shader compilation failed: {log}");
        }

        return shader;
    }
}
