
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using Compis.App.Rendering;
using Compis.Core.Geometry.Generators;
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
    private bool _isDraggingMeshElement;
    private bool _isDraggingMeshGizmo;
    private GizmoAxis _meshGizmoAxis;
    private TransformMode _meshGizmoMode;
    private Vector3 _meshGizmoAxisDirection;
    private Vector3 _meshGizmoBasisU;
    private Vector3 _meshGizmoBasisV;
    private Vector3 _meshGizmoOrigin;
    private float _meshGizmoStartAxisParameter;
    private float _meshGizmoLastAngle;
    private float _meshGizmoTotalAngle;
    private Vector3 _meshDragStartPlanePoint;
    private Vector3 _meshDragCameraForward;
    private Vector3 _meshTransformPivot;
    private Point _meshDragStartPointer;
    private readonly Dictionary<int, Vector3> _meshDragStartPositions = [];
    private MeshElementSelection? _selectedMeshElement;


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

    public static readonly StyledProperty<MeshElementMode> MeshElementModeProperty =
        AvaloniaProperty.Register<ViewportControl, MeshElementMode>(
            nameof(MeshElementMode), MeshElementMode.Object);

    public MeshElementMode MeshElementMode
    {
        get => GetValue(MeshElementModeProperty);
        set => SetValue(MeshElementModeProperty, value);
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

        if (properties.IsLeftButtonPressed && TryStartMeshGizmoDrag(e, pointer))
            return;

        if (properties.IsLeftButtonPressed &&
            MeshElementMode != MeshElementMode.Object &&
            Scene is not null)
        {
            float selectionAspect = (float)Bounds.Width / (float)Bounds.Height;
            var selectionView = _renderer.GetViewMatrix();
            var selectionProjection = _renderer.GetProjectionMatrix(selectionAspect);
            MeshElementSelection? selection = MeshElementPicker.Pick(
                new Vector2((float)pointer.X, (float)pointer.Y),
                (float)Bounds.Width,
                (float)Bounds.Height,
                selectionView,
                selectionProjection,
                Scene,
                MeshElementMode);

            if (selection is not null)
            {
                SelectedVolume = selection.Volume;
                _selectedMeshElement = selection;
                _renderer.MeshElementSelection = selection;
                _meshDragCameraForward = _renderer.CameraForward;
                _meshTransformPivot = selection.Mode == MeshElementMode.Vertex
                    ? new Vector3(
                        (float)selection.Volume.Position.X,
                        (float)selection.Volume.Position.Y,
                        (float)selection.Volume.Position.Z)
                    : selection.WorldCentroid;
                _meshDragStartPointer = pointer;
                _meshDragStartPositions.Clear();

                var selectedMesh = BoxMeshGenerator.Generate(selection.Volume.Parameters);
                foreach (int vertexIndex in selection.VertexIndices)
                {
                    Position3D vertex = selectedMesh.Vertices[vertexIndex];
                    _meshDragStartPositions[vertexIndex] = new Vector3(
                        (float)vertex.X,
                        (float)vertex.Y,
                        (float)vertex.Z);
                }

                if (MeshElementPicker.TryGetViewPlanePoint(
                        new Vector2((float)pointer.X, (float)pointer.Y),
                        (float)Bounds.Width,
                        (float)Bounds.Height,
                        selectionView,
                        selectionProjection,
                    _meshTransformPivot,
                        _meshDragCameraForward,
                        out _meshDragStartPlanePoint))
                {
                    _isDragging = true;
                    _isDraggingMeshElement = true;
                    e.Pointer.Capture(this);
                }

                e.Handled = true;
                return;
            }

            _selectedMeshElement = null;
            _renderer.MeshElementSelection = null;
        }

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

        if (properties.IsLeftButtonPressed && Scene is not null)
        {
            float aspectRatio = (float)Bounds.Width / (float)Bounds.Height;
            BuildingVolume? hitVolume = BoxMeshPicker.PickNearest(
                new Vector2((float)pointer.X, (float)pointer.Y),
                (float)Bounds.Width,
                (float)Bounds.Height,
                _renderer.GetViewMatrix(),
                _renderer.GetProjectionMatrix(aspectRatio),
                Scene);

            if (hitVolume is not null)
            {
                SelectedVolume = hitVolume;
            }
        }

        // Keep viewport navigation active when a drag starts on a box surface.
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

        if (_isDraggingMeshGizmo && _selectedMeshElement is not null && SelectedVolume is not null)
        {
            if (!TryGetMeshGizmoUpdatedPositions(current, out Dictionary<int, Vector3> positions))
                return;

            SelectedVolume.SetMeshVertexWorldPositions(positions);
            RefreshSelectedMeshElement();
            RequestNextFrameRendering();
            e.Handled = true;
            return;
        }

        if (_isDraggingMeshElement && _selectedMeshElement is not null && SelectedVolume is not null)
        {
            float aspectRatio = (float)Bounds.Width / (float)Bounds.Height;
            var view = _renderer.GetViewMatrix();
            var projection = _renderer.GetProjectionMatrix(aspectRatio);
            if (!MeshElementPicker.TryGetViewPlanePoint(
                    new Vector2((float)current.X, (float)current.Y),
                    (float)Bounds.Width,
                    (float)Bounds.Height,
                    view,
                    projection,
                    _meshTransformPivot,
                    _meshDragCameraForward,
                    out Vector3 planePoint))
                return;

            Vector3 delta = planePoint - _meshDragStartPlanePoint;
            var updatedPositions = new Dictionary<int, Vector3>(_meshDragStartPositions.Count);
            if (TransformMode == TransformMode.Translate)
            {
                foreach ((int vertexIndex, Vector3 startPosition) in _meshDragStartPositions)
                    updatedPositions[vertexIndex] = startPosition + delta;
            }
            else if (TransformMode == TransformMode.Rotate)
            {
                float angle = (float)(current.X - _meshDragStartPointer.X) * 0.01f;
                Quaternion rotation = Quaternion.CreateFromAxisAngle(_meshDragCameraForward, angle);
                foreach ((int vertexIndex, Vector3 startPosition) in _meshDragStartPositions)
                {
                    Vector3 relative = startPosition - _meshTransformPivot;
                    updatedPositions[vertexIndex] = _meshTransformPivot + Vector3.Transform(relative, rotation);
                }
            }
            else
            {
                float factor = MathF.Max(0.01f, MathF.Exp(-(float)(current.Y - _meshDragStartPointer.Y) * 0.01f));
                foreach ((int vertexIndex, Vector3 startPosition) in _meshDragStartPositions)
                {
                    Vector3 relative = startPosition - _meshTransformPivot;
                    updatedPositions[vertexIndex] = _meshTransformPivot + relative * factor;
                }
            }

            SelectedVolume.SetMeshVertexWorldPositions(updatedPositions);
            RequestNextFrameRendering();
            e.Handled = true;
            return;
        }

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
        _isDraggingMeshElement = false;
        _isDraggingMeshGizmo = false;
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
            {
                _observedSelectedVolume.PropertyChanged += OnSelectedVolumeChanged;
            }

            if (_selectedMeshElement?.Volume != _observedSelectedVolume)
            {
                _selectedMeshElement = null;
                _renderer.MeshElementSelection = null;
            }

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
        else if (change.Property == MeshElementModeProperty)
        {
            _selectedMeshElement = null;
            _renderer.MeshElementSelection = null;
            _renderer.MeshElementMode = change.GetNewValue<MeshElementMode>();
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
            or nameof(BuildingVolume.Dimensions)
            or nameof(BuildingVolume.Parameters)
            or nameof(BuildingVolume.Color))
        {
            if (e.PropertyName == nameof(BuildingVolume.Parameters))
                RefreshSelectedMeshElement();
            RequestNextFrameRendering();
        }
    }

    private bool TryStartMeshGizmoDrag(PointerPressedEventArgs e, Point pointer)
    {
        if (MeshElementMode == MeshElementMode.Object ||
            _selectedMeshElement is null ||
            SelectedVolume is null ||
            _selectedMeshElement.Volume != SelectedVolume)
            return false;

        RefreshSelectedMeshElement();
        if (_selectedMeshElement is null)
            return false;

        float aspectRatio = (float)Bounds.Width / (float)Bounds.Height;
        Matrix4x4 view = _renderer.GetViewMatrix();
        Matrix4x4 projection = _renderer.GetProjectionMatrix(aspectRatio);
        GetGizmoAxes(SelectedVolume.Rotation, TransformSpace,
            out Vector3 axisX, out Vector3 axisY, out Vector3 axisZ);
        Vector3 origin = _meshTransformPivot;
        _meshGizmoOrigin = origin;
        Vector2 pointerPosition = new((float)pointer.X, (float)pointer.Y);

        GizmoAxis axis = TransformMode == TransformMode.Rotate
            ? GizmoAxisPicker.PickRotationAxis(pointerPosition,
                (float)Bounds.Width, (float)Bounds.Height,
                origin, view, projection, axisX, axisY, axisZ)
            : GizmoAxisPicker.PickAxis(pointerPosition,
                (float)Bounds.Width, (float)Bounds.Height,
                origin, view, projection, axisX, axisY, axisZ);
        if (axis == GizmoAxis.None)
            return false;

        _meshTransformPivot = _selectedMeshElement.Mode == MeshElementMode.Vertex
            ? new Vector3((float)SelectedVolume.Position.X,
                (float)SelectedVolume.Position.Y,
                (float)SelectedVolume.Position.Z)
            : origin;
        _meshGizmoAxis = axis;
        _meshGizmoMode = TransformMode;
        _meshGizmoAxisDirection = GetAxisDirection(axis, axisX, axisY, axisZ);
        GetRotationPlaneBasis(axis, axisX, axisY, axisZ,
            out _meshGizmoBasisU, out _meshGizmoBasisV);

        var mesh = BoxMeshGenerator.Generate(SelectedVolume.Parameters);
        _meshDragStartPositions.Clear();
        foreach (int vertexIndex in _selectedMeshElement.VertexIndices)
        {
            Position3D vertex = mesh.Vertices[vertexIndex];
            _meshDragStartPositions[vertexIndex] = new Vector3(
                (float)vertex.X, (float)vertex.Y, (float)vertex.Z);
        }

        if (_meshGizmoMode == TransformMode.Rotate)
        {
            if (!GizmoAxisPicker.TryGetRotationAngle(pointerPosition,
                    (float)Bounds.Width, (float)Bounds.Height,
                    origin, _meshGizmoAxisDirection,
                    _meshGizmoBasisU, _meshGizmoBasisV,
                    view, projection, out _meshGizmoLastAngle))
                return false;

            _meshGizmoTotalAngle = 0f;
        }
        else
        {
            _meshGizmoStartAxisParameter = GizmoAxisPicker.GetAxisParameter(
                pointerPosition, (float)Bounds.Width, (float)Bounds.Height,
                origin, _meshGizmoAxisDirection, view, projection);
        }

        _isDragging = true;
        _isDraggingMeshGizmo = true;
        _isDraggingMeshElement = false;
        e.Pointer.Capture(this);
        e.Handled = true;
        return true;
    }

    private bool TryGetMeshGizmoUpdatedPositions(
        Point pointer,
        out Dictionary<int, Vector3> positions)
    {
        positions = new Dictionary<int, Vector3>(_meshDragStartPositions.Count);
        if (_selectedMeshElement is null || _meshDragStartPositions.Count == 0)
            return false;

        float aspectRatio = (float)Bounds.Width / (float)Bounds.Height;
        Matrix4x4 view = _renderer.GetViewMatrix();
        Matrix4x4 projection = _renderer.GetProjectionMatrix(aspectRatio);
        Vector2 pointerPosition = new((float)pointer.X, (float)pointer.Y);

        if (_meshGizmoMode == TransformMode.Rotate)
        {
            if (!GizmoAxisPicker.TryGetRotationAngle(pointerPosition,
                    (float)Bounds.Width, (float)Bounds.Height,
                    _meshGizmoOrigin, _meshGizmoAxisDirection,
                    _meshGizmoBasisU, _meshGizmoBasisV,
                    view, projection, out float currentAngle))
                return false;

            float step = currentAngle - _meshGizmoLastAngle;
            if (step > MathF.PI)
                step -= MathF.Tau;
            else if (step < -MathF.PI)
                step += MathF.Tau;
            _meshGizmoTotalAngle += step;
            _meshGizmoLastAngle = currentAngle;
            Quaternion rotation = Quaternion.CreateFromAxisAngle(
                _meshGizmoAxisDirection, _meshGizmoTotalAngle);

            foreach ((int vertexIndex, Vector3 startPosition) in _meshDragStartPositions)
            {
                Vector3 relative = startPosition - _meshTransformPivot;
                positions[vertexIndex] = _meshTransformPivot + Vector3.Transform(relative, rotation);
            }
            return true;
        }

        float currentParameter = GizmoAxisPicker.GetAxisParameter(
            pointerPosition, (float)Bounds.Width, (float)Bounds.Height,
            _meshGizmoOrigin, _meshGizmoAxisDirection,
            view, projection);
        float movement = currentParameter - _meshGizmoStartAxisParameter;

        foreach ((int vertexIndex, Vector3 startPosition) in _meshDragStartPositions)
        {
            if (_meshGizmoMode == TransformMode.Translate)
            {
                positions[vertexIndex] = startPosition + _meshGizmoAxisDirection * movement;
                continue;
            }

            float factor = MathF.Max(0.01f, 1f + movement / 3f);
            Vector3 relative = startPosition - _meshTransformPivot;
            float axisComponent = Vector3.Dot(relative, _meshGizmoAxisDirection);
            positions[vertexIndex] = _meshTransformPivot +
                relative + _meshGizmoAxisDirection * axisComponent * (factor - 1f);
        }

        return true;
    }

    private void RefreshSelectedMeshElement()
    {
        if (_selectedMeshElement is null || SelectedVolume is null ||
            _selectedMeshElement.Volume != SelectedVolume)
            return;

        var mesh = BoxMeshGenerator.Generate(SelectedVolume.Parameters);
        Vector3 centroid = Vector3.Zero;
        foreach (int vertexIndex in _selectedMeshElement.VertexIndices)
        {
            Position3D vertex = mesh.Vertices[vertexIndex];
            centroid += new Vector3((float)vertex.X, (float)vertex.Y, (float)vertex.Z);
        }

        centroid /= _selectedMeshElement.VertexIndices.Count;
        _selectedMeshElement = _selectedMeshElement with { WorldCentroid = centroid };
        _renderer.MeshElementSelection = _selectedMeshElement;
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
