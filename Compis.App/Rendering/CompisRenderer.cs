
using Avalonia.OpenGL;
using Silk.NET.OpenGL;
using Compis.App.Rendering.Camera;

namespace Compis.App.Rendering;

public sealed class CompisRenderer
{
    private GL? _gl;
    private GridRenderer? _grid;

    private readonly Camera3D _camera = new();

    public void Initialize(GlInterface gl)
    {
        _gl = GL.GetApi(
            procName => gl.GetProcAddress(procName)
        );

        _grid = new GridRenderer(_gl);
        _grid.Initialize();
    }

    public void Render(int framebuffer, int width, int height)
    {
        if (_gl is null || _grid is null)
            return;

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)framebuffer);
        _gl.Viewport(0, 0, (uint)width, (uint)height);

        _gl.Enable(EnableCap.DepthTest);

        _gl.ClearColor(0.125f, 0.137f, 0.153f, 1.0f);

        _gl.Clear(
            ClearBufferMask.ColorBufferBit |
            ClearBufferMask.DepthBufferBit
        );

        float aspectRatio = (float)width / height;

        var view = _camera.GetViewMatrix();
        var projection = _camera.GetProjectionMatrix(aspectRatio);

        _grid.Render(view, projection);
    }

    public void Dispose()
    {
        _grid?.Dispose();
        _gl?.Dispose();

        _grid = null;
        _gl = null;
    }
}
