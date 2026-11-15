
using Avalonia.OpenGL;
using OpenTK.Graphics.OpenGL4;

namespace Compis.App.Rendering;

public sealed class CompisRenderer
{
    private readonly GridRenderer _grid = new();

    public void Initialize(GlInterface gl)
    {
        GL.LoadBindings(new AvaloniaBindingsContext(gl));

        _grid.Initialize();
    }

    public void Render(int framebuffer, int width, int height)
    {
        GL.BindFramebuffer(
            FramebufferTarget.Framebuffer,
            framebuffer
        );

        GL.Viewport(0, 0, width, height);

        GL.ClearColor(0.125f, 0.137f, 0.153f, 1.0f);

        GL.Clear(
            ClearBufferMask.ColorBufferBit |
            ClearBufferMask.DepthBufferBit
        );

        _grid.Render();
    }

    public void Dispose()
    {
        _grid.Dispose();
    }
}
