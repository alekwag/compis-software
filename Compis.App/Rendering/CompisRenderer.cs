
using Avalonia.OpenGL;
using Silk.NET.OpenGL;
using Compis.App.Rendering.Camera;
using Compis.Core.Scene;

namespace Compis.App.Rendering;

public sealed class CompisRenderer
{
    private GL? _gl;
    private GridRenderer? _grid;
    private BuildingVolumeRenderer? _buildingRenderer;

    private readonly Camera3D _camera = new();

    public BuildingScene? Scene { get; set; }

    public void Initialize(GlInterface gl)
    {
        _gl = GL.GetApi(
            procName => gl.GetProcAddress(procName)
        );

        _grid = new GridRenderer(_gl);
        _grid.Initialize();

        _buildingRenderer = new BuildingVolumeRenderer(_gl);
        _buildingRenderer.Initialize();
    }

    public void Render(int framebuffer, int width, int height)
    {
        if (_gl is null || _grid is null || _buildingRenderer is null)
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

        if (Scene is not null)
        {
            _buildingRenderer.Render(Scene, view, projection);
        }
    }

    public void Dispose()
    {
        _buildingRenderer?.Dispose();
        _grid?.Dispose();
        _gl?.Dispose();

        _buildingRenderer = null;
        _grid = null;
        _gl = null;
    }

    public void Orbit(float deltaYaw, float deltaPitch)
    {
        _camera.Orbit(deltaYaw, deltaPitch);
    }

    public void Pan(float deltaX, float deltaY)
    {
        _camera.Pan(deltaX, deltaY);
    }

    public void Zoom(float amount)
    {
        _camera.Zoom(amount);
    }
}
