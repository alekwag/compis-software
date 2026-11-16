
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using Compis.App.Rendering;
using Compis.Core.Scene;
using Compis.Core.Objects;
using System.Numerics;
using Compis.Core.Parameters;

namespace Compis.App.Controls;

public sealed class ViewportControl : OpenGlControlBase, ICustomHitTest
{
    private readonly CompisRenderer _renderer = new();

    private GizmoAxis _dragAxis = GizmoAxis.None;

    private Position3D _dragStartPosition = new(0, 0, 0);

    private float _dragStartAxisParameter;
    private Rotation3D _dragStartRotation = Rotation3D.Zero;
    private Scale3D _dragStartScale = Scale3D.One;
    private Vector3 _dragAxisDirection;
    private Vector3 _dragRotationBasisU;
    private Vector3 _dragRotationBasisV;
    private TransformMode _dragMode;
    private float _lastRotationAngle;
    private float _rotationDragTotal;


    public static readonly StyledProperty<BuildingScene?> SceneProperty =
        AvaloniaProperty.Register<ViewportControl, BuildingScene?>(
            nameof(Scene));

    public BuildingScene? Scene
    {
        get => GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    public static readonly StyledProperty<BuildingVolume?> SelectedVolumeProperty =
    AvaloniaProperty.Register<ViewportControl, BuildingVolume?>(
        nameof(SelectedVolume));

    public BuildingVolume? SelectedVolume
    {
        get => GetValue(SelectedVolumeProperty);
        set => SetValue(SelectedVolumeProperty, value);
    }

    public static readonly StyledProperty<TransformMode> TransformModeProperty =
        AvaloniaProperty.Register<ViewportControl, TransformMode>(
            nameof(TransformMode), TransformMode.Translate);

    public TransformMode TransformMode
    {
        get => GetValue(TransformModeProperty);
        set => SetValue(TransformModeProperty, value);
    }

    public static readonly StyledProperty<TransformSpace> TransformSpaceProperty =
        AvaloniaProperty.Register<ViewportControl, TransformSpace>(
            nameof(TransformSpace), TransformSpace.Global);

    public TransformSpace TransformSpace
    {
        get => GetValue(TransformSpaceProperty);
        set => SetValue(TransformSpaceProperty, value);
    }

    public static readonly StyledProperty<bool> ShowVerticesProperty =
        AvaloniaProperty.Register<ViewportControl, bool>(nameof(ShowVertices), true);

    public bool ShowVertices
    {
        get => GetValue(ShowVerticesProperty);
        set => SetValue(ShowVerticesProperty, value);
    }

    public static readonly StyledProperty<bool> ShowEdgesProperty =
        AvaloniaProperty.Register<ViewportControl, bool>(nameof(ShowEdges), true);

    public bool ShowEdges
    {
        get => GetValue(ShowEdgesProperty);
        set => SetValue(ShowEdgesProperty, value);
    }

    public static readonly StyledProperty<bool> ShowFacesProperty =
        AvaloniaProperty.Register<ViewportControl, bool>(nameof(ShowFaces), true);

    public bool ShowFaces
    {
        get => GetValue(ShowFacesProperty);
        set => SetValue(ShowFacesProperty, value);
    }

    private bool _rendererInitialized;
    private bool _isDragging;
    private bool _isPanning;
    private Point _lastPointerPosition;
    private BuildingVolume? _observedSelectedVolume;

    public ViewportControl()
    {
        Focusable = true;
        ClipToBounds = true;

        PointerTouchPadGestureMagnify += OnTrackpadMagnify;
        PointerTouchPadGestureRotate += OnTrackpadRotate;
        PointerTouchPadGestureSwipe += OnTrackpadSwipe;
    }

    public bool HitTest(Point point) =>
        new Rect(0, 0, Bounds.Width, Bounds.Height).Contains(point);

    protected override void OnOpenGlInit(GlInterface gl)
    {
        base.OnOpenGlInit(gl);

        try
        {
            _renderer.Initialize(gl);
            _rendererInitialized = true;
            Console.WriteLine("Silk.NET renderer initialised.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Renderer error: {ex}");
        }
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        double scaling =
            TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;

        int width = Math.Max(1, (int)Math.Ceiling(Bounds.Width * scaling));
        int height = Math.Max(1, (int)Math.Ceiling(Bounds.Height * scaling));

        if (_rendererInitialized)
            _renderer.Render(fb, width, height);
    }

    // Mouse: drag to orbit, Shift + drag to pan.
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var properties = e.GetCurrentPoint(this).Properties;

        if (!properties.IsLeftButtonPressed &&
            !properties.IsMiddleButtonPressed)
            return;

        Point pointer = e.GetPosition(this);

        if (properties.IsLeftButtonPressed && SelectedVolume is not null)
        {
            float aspectRatio = (float)Bounds.Width / (float)Bounds.Height;

            var view = _renderer.GetViewMatrix();
            var projection = _renderer.GetProjectionMatrix(aspectRatio);

            Vector3 origin = new(
                (float)SelectedVolume.Position.X,
                (float)SelectedVolume.Position.Y,
                (float)SelectedVolume.Position.Z);
            GetGizmoAxes(SelectedVolume.Rotation, TransformSpace,
                out Vector3 axisX, out Vector3 axisY, out Vector3 axisZ);

            Vector2 pointerPosition = new((float)pointer.X, (float)pointer.Y);
            _dragAxis = TransformMode == TransformMode.Rotate
                ? GizmoAxisPicker.PickRotationAxis(
                    pointerPosition, (float)Bounds.Width, (float)Bounds.Height,
                    origin, view, projection, axisX, axisY, axisZ)
                : GizmoAxisPicker.PickAxis(
                    pointerPosition, (float)Bounds.Width, (float)Bounds.Height,
                    origin, view, projection, axisX, axisY, axisZ);

            if (_dragAxis != GizmoAxis.None)
            {
                _dragStartPosition = SelectedVolume.Position;
                _dragStartRotation = SelectedVolume.Rotation;
                _dragStartScale = SelectedVolume.Scale;
                _dragMode = TransformMode;
                _dragAxisDirection = GetAxisDirection(_dragAxis, axisX, axisY, axisZ);
                GetRotationPlaneBasis(_dragAxis, axisX, axisY, axisZ,
                    out _dragRotationBasisU, out _dragRotationBasisV);

                if (_dragMode == TransformMode.Rotate)
                {
                    if (GizmoAxisPicker.TryGetRotationAngle(
                            pointerPosition, (float)Bounds.Width, (float)Bounds.Height,
                            origin, _dragAxisDirection,
                            _dragRotationBasisU, _dragRotationBasisV,
                            view, projection, out float startAngle))
                    {
                        _lastRotationAngle = startAngle;
                        _rotationDragTotal = 0f;
                        _isDragging = true;
                    }
                }
                else
                {
                    _dragStartAxisParameter = GizmoAxisPicker.GetAxisParameter(
                        pointerPosition,
                        (float)Bounds.Width,
                        (float)Bounds.Height,
                        origin,
                        _dragAxisDirection,
                        view,
                        projection);
                    _isDragging = true;
                }

                if (_isDragging)
                {
                    e.Pointer.Capture(this);
                    e.Handled = true;
                    return;
                }

                _dragAxis = GizmoAxis.None;
            }
        }

        _isDragging = true;
        _isPanning = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        _lastPointerPosition = pointer;

        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_isDragging)
            return;

        Point current = e.GetPosition(this);

        // Moving a building along a selected axis.
        if (_dragAxis != GizmoAxis.None && SelectedVolume is not null)
        {
            float aspectRatio = (float)Bounds.Width / (float)Bounds.Height;

            var view = _renderer.GetViewMatrix();
            var projection = _renderer.GetProjectionMatrix(aspectRatio);

            Vector3 origin = new(
                (float)_dragStartPosition.X,
                (float)_dragStartPosition.Y,
                (float)_dragStartPosition.Z);

            Vector2 pointerPosition = new((float)current.X, (float)current.Y);

            if (_dragMode == TransformMode.Rotate)
            {
                if (!GizmoAxisPicker.TryGetRotationAngle(
                        pointerPosition, (float)Bounds.Width, (float)Bounds.Height,
                        origin, _dragAxisDirection,
                        _dragRotationBasisU, _dragRotationBasisV,
                        view, projection, out float currentAngle))
                    return;

                float step = currentAngle - _lastRotationAngle;
                if (step > MathF.PI)
                    step -= MathF.Tau;
                else if (step < -MathF.PI)
                    step += MathF.Tau;

                _rotationDragTotal += step;
                _lastRotationAngle = currentAngle;
                float deltaDegrees = _rotationDragTotal * (180f / MathF.PI);

                Rotation3D rotation = _dragAxis switch
                {
                    GizmoAxis.X => new Rotation3D(_dragStartRotation.X + deltaDegrees, _dragStartRotation.Y, _dragStartRotation.Z),
                    GizmoAxis.Y => new Rotation3D(_dragStartRotation.X, _dragStartRotation.Y + deltaDegrees, _dragStartRotation.Z),
                    GizmoAxis.Z => new Rotation3D(_dragStartRotation.X, _dragStartRotation.Y, _dragStartRotation.Z + deltaDegrees),
                    _ => _dragStartRotation
                };
                SelectedVolume.RotateTo(rotation);
            }
            else
            {
                float currentParameter = GizmoAxisPicker.GetAxisParameter(
                    pointerPosition,
                    (float)Bounds.Width,
                    (float)Bounds.Height,
                    origin,
                    _dragAxisDirection,
                    view,
                    projection);

                float movement = currentParameter - _dragStartAxisParameter;
                if (_dragMode == TransformMode.Scale)
                {
                    float factor = MathF.Max(0.01f, 1f + movement / 3f);
                    Scale3D scale = _dragAxis switch
                    {
                        GizmoAxis.X => new Scale3D(_dragStartScale.X * factor, _dragStartScale.Y, _dragStartScale.Z),
                        GizmoAxis.Y => new Scale3D(_dragStartScale.X, _dragStartScale.Y * factor, _dragStartScale.Z),
                        GizmoAxis.Z => new Scale3D(_dragStartScale.X, _dragStartScale.Y, _dragStartScale.Z * factor),
                        _ => _dragStartScale
                    };
                    SelectedVolume.ScaleTo(scale);
                }
                else
                {
                    Vector3 newPosition = origin + _dragAxisDirection * movement;
                    SelectedVolume.MoveTo(new Position3D(
                        newPosition.X,
                        newPosition.Y,
                        newPosition.Z));
                }
            }

            RequestNextFrameRendering();

            e.Handled = true;
            return;
        }

        // Existing camera navigation.
        double dx = current.X - _lastPointerPosition.X;
        double dy = current.Y - _lastPointerPosition.Y;

        _lastPointerPosition = current;

        if (_isPanning)
        {
            _renderer.Pan((float)(-dx * 0.02), (float)(dy * 0.02));
        }
        else
        {
            _renderer.Orbit((float)(-dx * 0.008), (float)(-dy * 0.008));
        }

        RequestNextFrameRendering();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        _isDragging = false;
        _isPanning = false;
        _dragAxis = GizmoAxis.None;

        e.Pointer.Capture(null);
    }

    // Trackpad and mouse wheel scrolling.
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
{
    base.OnPointerWheelChanged(e);

    if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
    {
        _renderer.Zoom((float)(e.Delta.Y * 0.15));
    }
    else
    {
        _renderer.Pan((float)(-e.Delta.X * 0.2), (float)(e.Delta.Y * 0.2));
    }

    RequestNextFrameRendering();

    e.Handled = true;
}

    // Native trackpad pinch.
    private void OnTrackpadMagnify(
    object? sender,
    PointerDeltaEventArgs e)
{
    _renderer.Zoom((float)(e.Delta.Y * 2.0));
    RequestNextFrameRendering();
    e.Handled = true;
}

    // Native trackpad rotation.
    private void OnTrackpadRotate(object? sender, PointerDeltaEventArgs e)
    {
        _renderer.Orbit((float)(e.Delta.X * 0.01), 0);
        RequestNextFrameRendering();
        e.Handled = true;
    }

    private void OnTrackpadSwipe(object? sender, PointerDeltaEventArgs e)
    {
        _renderer.Orbit((float)(-e.Delta.X * 0.008), (float)(-e.Delta.Y * 0.008));
        RequestNextFrameRendering();
        e.Handled = true;
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        if (_rendererInitialized)
        {
            _renderer.Dispose();
            _rendererInitialized = false;
        }

        base.OnOpenGlDeinit(gl);
    }

    protected override void OnPropertyChanged(
    AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SceneProperty)
        {
            _renderer.Scene = change.GetNewValue<BuildingScene?>();
            RequestNextFrameRendering();
        }
        else if (change.Property == SelectedVolumeProperty)
        {
            if (_observedSelectedVolume is not null)
                _observedSelectedVolume.PropertyChanged -= OnSelectedVolumeChanged;

            _observedSelectedVolume = change.GetNewValue<BuildingVolume?>();
            if (_observedSelectedVolume is not null)
                _observedSelectedVolume.PropertyChanged += OnSelectedVolumeChanged;

            _renderer.SelectedVolume =
                _observedSelectedVolume;

            RequestNextFrameRendering();
        }
        else if (change.Property == TransformModeProperty)
        {
            _renderer.TransformMode = change.GetNewValue<TransformMode>();
            RequestNextFrameRendering();
        }
        else if (change.Property == TransformSpaceProperty)
        {
            _renderer.TransformSpace = change.GetNewValue<TransformSpace>();
            RequestNextFrameRendering();
        }
        else if (change.Property == ShowVerticesProperty)
        {
            _renderer.ShowVertices = change.GetNewValue<bool>();
            RequestNextFrameRendering();
        }
        else if (change.Property == ShowEdgesProperty)
        {
            _renderer.ShowEdges = change.GetNewValue<bool>();
            RequestNextFrameRendering();
        }
        else if (change.Property == ShowFacesProperty)
        {
            _renderer.ShowFaces = change.GetNewValue<bool>();
            RequestNextFrameRendering();
        }
    }

    private void OnSelectedVolumeChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BuildingVolume.Position)
            or nameof(BuildingVolume.Rotation)
            or nameof(BuildingVolume.Scale)
            or nameof(BuildingVolume.Dimensions))
        {
            RequestNextFrameRendering();
        }
    }

    private static void GetGizmoAxes(
        Rotation3D rotation,
        TransformSpace space,
        out Vector3 axisX,
        out Vector3 axisY,
        out Vector3 axisZ)
    {
        Quaternion orientation = space == TransformSpace.Local
            ? Quaternion.CreateFromYawPitchRoll(
                (float)(rotation.Y * Math.PI / 180.0),
                (float)(rotation.X * Math.PI / 180.0),
                (float)(rotation.Z * Math.PI / 180.0))
            : Quaternion.Identity;

        axisX = Vector3.Transform(Vector3.UnitX, orientation);
        axisY = Vector3.Transform(Vector3.UnitY, orientation);
        axisZ = Vector3.Transform(Vector3.UnitZ, orientation);
    }

    private static Vector3 GetAxisDirection(
        GizmoAxis axis,
        Vector3 axisX,
        Vector3 axisY,
        Vector3 axisZ) => axis switch
    {
        GizmoAxis.X => axisX,
        GizmoAxis.Y => axisY,
        GizmoAxis.Z => axisZ,
        _ => Vector3.Zero
    };

    private static void GetRotationPlaneBasis(
        GizmoAxis axis,
        Vector3 axisX,
        Vector3 axisY,
        Vector3 axisZ,
        out Vector3 basisU,
        out Vector3 basisV)
    {
        (basisU, basisV) = axis switch
        {
            GizmoAxis.X => (axisY, axisZ),
            GizmoAxis.Y => (axisZ, axisX),
            GizmoAxis.Z => (axisX, axisY),
            _ => (Vector3.UnitX, Vector3.UnitY)
        };
    }


    

}
