
using System;
using System.Collections.Generic;
using System.Numerics;
using Silk.NET.OpenGL;

namespace Compis.App.Rendering;

public sealed class GridRenderer
{
    private readonly GL _gl;

    private uint _vertexArray;
    private uint _vertexBuffer;
    private uint _shaderProgram;
    private uint _vertexCount;

    private int _viewLocation;
    private int _projectionLocation;

    public GridRenderer(GL gl)
    {
        _gl = gl;
    }

    public unsafe void Initialize()
    {
        float[] vertices = CreateGridVertices();
        _vertexCount = (uint)(vertices.Length / 6);

        _vertexArray = _gl.GenVertexArray();
        _vertexBuffer = _gl.GenBuffer();

        _gl.BindVertexArray(_vertexArray);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);

        fixed (float* vertexPointer = vertices)
        {
            _gl.BufferData(
                BufferTargetARB.ArrayBuffer,
                (nuint)(vertices.Length * sizeof(float)),
                vertexPointer,
                BufferUsageARB.StaticDraw
            );
        }

        _gl.VertexAttribPointer(
            0, 3, VertexAttribPointerType.Float,
            false, 6 * sizeof(float), (void*)0
        );
        _gl.EnableVertexAttribArray(0);

        _gl.VertexAttribPointer(
            1, 3, VertexAttribPointerType.Float,
            false, 6 * sizeof(float), (void*)(3 * sizeof(float))
        );
        _gl.EnableVertexAttribArray(1);

        _shaderProgram = CreateShaderProgram();

        _viewLocation = _gl.GetUniformLocation(_shaderProgram, "view");
        _projectionLocation = _gl.GetUniformLocation(_shaderProgram, "projection");

        _gl.BindVertexArray(0);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }

    public unsafe void Render(Matrix4x4 view, Matrix4x4 projection)
    {
        _gl.UseProgram(_shaderProgram);

        _gl.UniformMatrix4(
            _viewLocation,
            1,
            false,
            (float*)&view
        );

        _gl.UniformMatrix4(
            _projectionLocation,
            1,
            false,
            (float*)&projection
        );

        _gl.BindVertexArray(_vertexArray);

        _gl.DrawArrays(
            PrimitiveType.Lines,
            0,
            _vertexCount
        );

        _gl.BindVertexArray(0);
        _gl.UseProgram(0);
    }

    public void Dispose()
    {
        if (_vertexBuffer != 0)
            _gl.DeleteBuffer(_vertexBuffer);

        if (_vertexArray != 0)
            _gl.DeleteVertexArray(_vertexArray);

        if (_shaderProgram != 0)
            _gl.DeleteProgram(_shaderProgram);
    }

    private static float[] CreateGridVertices()
    {
        var vertices = new List<float>();

        const int gridSize = 50;

        for (int i = -gridSize; i <= gridSize; i++)
        {
            float position = i;

            bool isAxis = i == 0;

            float r = isAxis ? 0.78f : 0.27f;
            float g = isAxis ? 0.61f : 0.29f;
            float b = isAxis ? 0.40f : 0.33f;

            AddVertex(vertices, position, -gridSize, 0, r, g, b);
            AddVertex(vertices, position, gridSize, 0, r, g, b);

            AddVertex(vertices, -gridSize, position, 0, r, g, b);
            AddVertex(vertices, gridSize, position, 0, r, g, b);
        }

        return vertices.ToArray();
    }

    private static void AddVertex(
        List<float> vertices,
        float x,
        float y,
        float z,
        float r,
        float g,
        float b)
    {
        vertices.Add(x);
        vertices.Add(y);
        vertices.Add(z);
        vertices.Add(r);
        vertices.Add(g);
        vertices.Add(b);
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

        uint vertex = CompileShader(ShaderType.VertexShader, vertexShader);
        uint fragment = CompileShader(ShaderType.FragmentShader, fragmentShader);

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

            throw new InvalidOperationException($"Shader linking failed: {log}");
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

            throw new InvalidOperationException($"Shader compilation failed: {log}");
        }

        return shader;
    }
}
