
using Avalonia.OpenGL;
using Silk.NET.OpenGL;
using Compis.App.Rendering.Camera;
using Compis.Core.Scene;
using Compis.Core.Objects;
using System.Numerics;

namespace Compis.App.Rendering;

public sealed class CompisRenderer
{
    private GL? _gl;
    private GridRenderer? _grid;
    private BuildingVolumeRenderer? _buildingRenderer;

    private readonly Camera3D _camera = new();

    public BuildingScene? Scene { get; set; }

    public BuildingVolume? SelectedVolume { get; set; }
    public TransformMode TransformMode { get; set; } = TransformMode.Translate;
    public TransformSpace TransformSpace { get; set; } = TransformSpace.Global;
    public MeshElementMode MeshElementMode { get; set; } = MeshElementMode.Object;
    public bool ShowVertices { get; set; } = true;
    public bool ShowEdges { get; set; } = true;
    public bool ShowFaces { get; set; } = true;
    public MeshElementSelection? MeshElementSelection { get; set; }
    private TransformGizmoRenderer? _gizmo;

    public void Initialize(GlInterface gl)
    {
        _gl = GL.GetApi(
            procName => gl.GetProcAddress(procName)
        );

        _grid = new GridRenderer(_gl);
        _grid.Initialize();

        _buildingRenderer = new BuildingVolumeRenderer(_gl);
        _buildingRenderer.Initialize();

        _gizmo = new TransformGizmoRenderer(_gl);
        _gizmo.Initialize();
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
            _buildingRenderer.ShowVertices = ShowVertices;
            _buildingRenderer.ShowEdges = ShowEdges;
            _buildingRenderer.ShowFaces = ShowFaces;
            _buildingRenderer.Render(Scene, view, projection, MeshElementSelection);
        }

        if (SelectedVolume is not null && MeshElementMode == MeshElementMode.Object)
        {
            _gizmo?.Render(
                SelectedVolume.Position,
                SelectedVolume.Rotation,
                TransformMode,
                TransformSpace,
                view,
                projection);
        }
        else if (SelectedVolume is not null &&
                 MeshElementMode != MeshElementMode.Object &&
                 MeshElementSelection is { } selection &&
                 selection.Volume == SelectedVolume)
        {
            Vector3 gizmoPosition = selection.Mode == MeshElementMode.Vertex
                ? new Vector3((float)SelectedVolume.Position.X,
                    (float)SelectedVolume.Position.Y,
                    (float)SelectedVolume.Position.Z)
                : selection.WorldCentroid;
            var componentPosition = new Compis.Core.Parameters.Position3D(
                gizmoPosition.X, gizmoPosition.Y, gizmoPosition.Z);
            _gizmo?.Render(
                componentPosition,
                SelectedVolume.Rotation,
                TransformMode,
                TransformSpace,
                view,
                projection);
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

        _gizmo?.Dispose();
        _gizmo = null;
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

    public Matrix4x4 GetViewMatrix() => _camera.GetViewMatrix();

    public Matrix4x4 GetProjectionMatrix(float aspectRatio) =>
        _camera.GetProjectionMatrix(aspectRatio);

    public Vector3 CameraPosition => _camera.Position;
    public Vector3 CameraForward => Vector3.Normalize(_camera.Target - _camera.Position);
    }
