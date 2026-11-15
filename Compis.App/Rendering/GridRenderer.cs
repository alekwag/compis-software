using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;


namespace Compis.App.Rendering;

public sealed class GridRenderer
{
    private int _vertexArray;
    private int _vertexBuffer;
    private int _shaderProgram;
    private int _vertexCount;

    public void Initialize()
    {
        float[] vertices = CreateGridVertices();
        _vertexCount = vertices.Length / 5;

        _vertexArray = GL.GenVertexArray();
        _vertexBuffer = GL.GenBuffer();

        GL.BindVertexArray(_vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);

        GL.BufferData(
            BufferTarget.ArrayBuffer,
            vertices.Length * sizeof(float),
            vertices,
            BufferUsageHint.StaticDraw
        );

        // Position: two floats.
        GL.VertexAttribPointer(
            0, 2, VertexAttribPointerType.Float,
            false, 5 * sizeof(float), 0
        );
        GL.EnableVertexAttribArray(0);

        // Colour: three floats.
        GL.VertexAttribPointer(
            1, 3, VertexAttribPointerType.Float,
            false, 5 * sizeof(float), 2 * sizeof(float)
        );
        GL.EnableVertexAttribArray(1);

        _shaderProgram = CreateShaderProgram();

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    public void Render()
    {
        GL.UseProgram(_shaderProgram);
        GL.BindVertexArray(_vertexArray);

        GL.DrawArrays(
            PrimitiveType.Lines,
            0,
            _vertexCount
        );

        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }

    public void Dispose()
    {
        if (_vertexBuffer != 0)
            GL.DeleteBuffer(_vertexBuffer);

        if (_vertexArray != 0)
            GL.DeleteVertexArray(_vertexArray);

        if (_shaderProgram != 0)
            GL.DeleteProgram(_shaderProgram);
    }

    private static float[] CreateGridVertices()
    {
        var vertices = new List<float>();

        for (int i = -10; i <= 10; i++)
        {
            float position = i / 10f;

            bool isAxis = i == 0;

            float r = isAxis ? 0.78f : 0.27f;
            float g = isAxis ? 0.61f : 0.29f;
            float b = isAxis ? 0.40f : 0.33f;

            // Vertical line.
            AddVertex(vertices, position, -1, r, g, b);
            AddVertex(vertices, position,  1, r, g, b);

            // Horizontal line.
            AddVertex(vertices, -1, position, r, g, b);
            AddVertex(vertices,  1, position, r, g, b);
        }

        return vertices.ToArray();
    }

    private static void AddVertex(
        List<float> vertices,
        float x,
        float y,
        float r,
        float g,
        float b)
    {
        vertices.Add(x);
        vertices.Add(y);
        vertices.Add(r);
        vertices.Add(g);
        vertices.Add(b);
    }

    private static int CreateShaderProgram()
    {
        const string vertexShader = """
            #version 330 core

            layout (location = 0) in vec2 aPosition;
            layout (location = 1) in vec3 aColor;

            out vec3 vertexColor;

            void main()
            {
                gl_Position = vec4(aPosition, 0.0, 1.0);
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

        int vertex = CompileShader(
            ShaderType.VertexShader,
            vertexShader
        );

        int fragment = CompileShader(
            ShaderType.FragmentShader,
            fragmentShader
        );

        int program = GL.CreateProgram();

        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);
        GL.LinkProgram(program);

        GL.GetProgram(
            program,
            GetProgramParameterName.LinkStatus,
            out int success
        );

        if (success == 0)
        {
            string log = GL.GetProgramInfoLog(program);

            GL.DeleteProgram(program);
            GL.DeleteShader(vertex);
            GL.DeleteShader(fragment);

            throw new InvalidOperationException(
                $"Shader linking failed: {log}"
            );
        }

        GL.DetachShader(program, vertex);
        GL.DetachShader(program, fragment);

        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);

        return program;
    }

    private static int CompileShader(
        ShaderType type,
        string source)
    {
        int shader = GL.CreateShader(type);

        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(
            shader,
            ShaderParameter.CompileStatus,
            out int success
        );

        if (success == 0)
        {
            string log = GL.GetShaderInfoLog(shader);

            GL.DeleteShader(shader);

            throw new InvalidOperationException(
                $"Shader compilation failed: {log}"
            );
        }

        return shader;
    }
}
