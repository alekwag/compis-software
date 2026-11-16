
using System;
using System.Collections.Generic;
using System.Numerics;
using Compis.Core.Geometry.Generators;
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
        var indices = new List<uint>();

        foreach (var volume in scene.Volumes)
        {
            var mesh = BoxMeshGenerator.Generate(volume);

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
                indices.Add(vertexOffset + (uint)triangleIndex);
            }
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
            _viewLocation,
            1,
            false,
            (float*)&view);

        _gl.UniformMatrix4(
            _projectionLocation,
            1,
            false,
            (float*)&projection);

        _gl.DrawElements(
            PrimitiveType.Triangles,
            (uint)indexData.Length,
            DrawElementsType.UnsignedInt,
            (void*)0);

        _gl.BindVertexArray(0);
        _gl.UseProgram(0);
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
