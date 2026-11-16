
using System;
using System.Collections.Generic;
using System.Numerics;
using Compis.Core.Geometry.Generators;
using Compis.Core.Parameters;
using Compis.Core.Scene;
using Silk.NET.OpenGL;

namespace Compis.App.Rendering;

public sealed class BuildingVolumeRenderer
{
    private readonly GL _gl;

    private uint _vertexArray;
    private uint _vertexBuffer;
    private uint _indexBuffer;
    private uint _shaderProgram;

    private int _viewLocation;
    private int _projectionLocation;
    private int _vertexPassLocation;
    private int _pointSizeLocation;

    public bool ShowVertices { get; set; } = true;
    public bool ShowEdges { get; set; } = true;
    public bool ShowFaces { get; set; } = true;

    public BuildingVolumeRenderer(GL gl)
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
        _vertexPassLocation =
            _gl.GetUniformLocation(_shaderProgram, "vertexPass");
        _pointSizeLocation =
            _gl.GetUniformLocation(_shaderProgram, "pointSize");

        _gl.BindVertexArray(0);
    }

    public unsafe void Render(
        BuildingScene scene,
        Matrix4x4 view,
        Matrix4x4 projection)
    {
        if (scene.Volumes.Count == 0)
            return;

        var vertices = new List<float>();
        var triangleIndices = new List<uint>();
        var wireVertices = new List<float>();
        var wireTriangleIndices = new List<uint>();

        foreach (var volume in scene.Volumes)
        {
            var mesh = BoxMeshGenerator.Generate(volume.Parameters);
            uint vertexOffset = (uint)(vertices.Count / 6);

            foreach (var vertex in mesh.Vertices)
            {
                vertices.Add((float)vertex.X);
                vertices.Add((float)vertex.Y);
                vertices.Add((float)vertex.Z);

                // Warm architectural material colour.
                vertices.Add(0.72f);
                vertices.Add(0.53f);
                vertices.Add(0.34f);
            }

            foreach (int triangleIndex in mesh.Triangles)
            {
                triangleIndices.Add(vertexOffset + (uint)triangleIndex);
            }

            for (int edgeIndex = 0; edgeIndex < mesh.Edges.Count; edgeIndex += 2)
            {
                Position3D start = mesh.Vertices[mesh.Edges[edgeIndex]];
                Position3D end = mesh.Vertices[mesh.Edges[edgeIndex + 1]];
                AddThickEdge(wireVertices, wireTriangleIndices, start, end);
            }
        }

        float[] vertexData = vertices.ToArray();

        _gl.BindVertexArray(_vertexArray);
        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            _vertexBuffer);
        _gl.BindBuffer(
            BufferTargetARB.ElementArrayBuffer,
            _indexBuffer);
        _gl.UseProgram(_shaderProgram);
        _gl.UniformMatrix4(
            _viewLocation,
            1,
            false,
            (float*)&view);

        _gl.UniformMatrix4(
            _projectionLocation,
            1,
            false,
            (float*)&projection);

        if (ShowFaces)
        {
            UploadVertexData(vertices);
            UploadIndexData(triangleIndices);
            _gl.Uniform1(_vertexPassLocation, 0);
            _gl.DrawElements(
                PrimitiveType.Triangles,
                (uint)triangleIndices.Count,
                DrawElementsType.UnsignedInt,
                (void*)0);
        }

        if (ShowEdges)
        {
            UploadVertexData(wireVertices);
            UploadIndexData(wireTriangleIndices);
            _gl.Uniform1(_vertexPassLocation, 0);
            _gl.DrawElements(
                PrimitiveType.Triangles,
                (uint)wireTriangleIndices.Count,
                DrawElementsType.UnsignedInt,
                (void*)0);
        }

        if (ShowVertices)
        {
            UploadVertexData(vertices);
            _gl.Uniform1(_vertexPassLocation, 1);
            _gl.Uniform1(_pointSizeLocation, 10f);
            _gl.PointSize(10f);
            _gl.DrawArrays(PrimitiveType.Points, 0, (uint)(vertexData.Length / 6));
            _gl.PointSize(1f);
            _gl.Uniform1(_vertexPassLocation, 0);
        }

        _gl.BindVertexArray(0);
        _gl.UseProgram(0);
    }

    private unsafe void UploadVertexData(List<float> vertices)
    {
        fixed (float* vertexPointer = vertices.ToArray())
        {
            _gl.BufferData(
                BufferTargetARB.ArrayBuffer,
                (nuint)(vertices.Count * sizeof(float)),
                vertexPointer,
                BufferUsageARB.DynamicDraw);
        }
    }

    private unsafe void UploadIndexData(List<uint> indices)
    {
        fixed (uint* indexPointer = indices.ToArray())
        {
            _gl.BufferData(
                BufferTargetARB.ElementArrayBuffer,
                (nuint)(indices.Count * sizeof(uint)),
                indexPointer,
                BufferUsageARB.DynamicDraw);
        }
    }

    private static void AddThickEdge(
        List<float> vertices,
        List<uint> indices,
        Position3D startPosition,
        Position3D endPosition)
    {
        Vector3 start = new((float)startPosition.X, (float)startPosition.Y, (float)startPosition.Z);
        Vector3 end = new((float)endPosition.X, (float)endPosition.Y, (float)endPosition.Z);
        Vector3 direction = end - start;
        if (direction.LengthSquared() < 0.000001f)
            return;

        direction = Vector3.Normalize(direction);
        Vector3 reference = MathF.Abs(Vector3.Dot(direction, Vector3.UnitZ)) > 0.9f
            ? Vector3.UnitY
            : Vector3.UnitZ;
        Vector3 side = Vector3.Normalize(Vector3.Cross(direction, reference));
        Vector3 up = Vector3.Normalize(Vector3.Cross(direction, side));
        const float radius = 0.045f;
        Vector3 colour = new(0.94f, 0.72f, 0.38f);
        uint firstVertex = (uint)(vertices.Count / 6);

        for (int endIndex = 0; endIndex < 2; endIndex++)
        {
            Vector3 center = endIndex == 0 ? start : end;
            for (int sideIndex = 0; sideIndex < 4; sideIndex++)
            {
                float angle = sideIndex * MathF.Tau / 4;
                Vector3 radial = (side * MathF.Cos(angle) + up * MathF.Sin(angle)) * radius;
                Vector3 point = center + radial;
                vertices.Add(point.X);
                vertices.Add(point.Y);
                vertices.Add(point.Z);
                vertices.Add(colour.X);
                vertices.Add(colour.Y);
                vertices.Add(colour.Z);
            }
        }

        for (uint sideIndex = 0; sideIndex < 4; sideIndex++)
        {
            uint next = (sideIndex + 1) % 4;
            uint startA = firstVertex + sideIndex;
            uint startB = firstVertex + next;
            uint endA = firstVertex + 4 + sideIndex;
            uint endB = firstVertex + 4 + next;

            indices.Add(startA);
            indices.Add(startB);
            indices.Add(endA);
            indices.Add(startB);
            indices.Add(endB);
            indices.Add(endA);
        }
    }

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
            uniform float pointSize;

            out vec3 vertexColor;
            out vec3 worldPosition;

            void main()
            {
                gl_Position = projection * view * vec4(aPosition, 1.0);
                gl_PointSize = pointSize;
                vertexColor = aColor;
                worldPosition = aPosition;
            }
            """;

        const string fragmentShader = """
            #version 330 core

            in vec3 vertexColor;
            in vec3 worldPosition;
            uniform int vertexPass;
            out vec4 FragColor;

            void main()
            {
                if (vertexPass == 1)
                {
                    vec2 point = gl_PointCoord * 2.0 - 1.0;
                    if (dot(point, point) > 1.0)
                        discard;
                    FragColor = vec4(1.0, 0.82, 0.28, 1.0);
                }
                else
                {
                    vec3 normal = normalize(cross(dFdx(worldPosition), dFdy(worldPosition)));
                    vec3 lightDirection = normalize(vec3(-0.45, -0.65, 0.8));
                    float diffuse = max(dot(normal, lightDirection), 0.0);
                    float lighting = 0.38 + 0.62 * diffuse;
                    FragColor = vec4(vertexColor * lighting, 1.0);
                }
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
                $"Building shader linking failed: {log}");
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
                $"Building shader compilation failed: {log}");
        }

        return shader;
    }
}
