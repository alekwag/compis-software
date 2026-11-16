
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using Compis.App.Rendering;
using Compis.Core.Scene;

namespace Compis.App.Controls;

public sealed class ViewportControl : OpenGlControlBase, ICustomHitTest
{
    private readonly CompisRenderer _renderer = new();

    public static readonly StyledProperty<BuildingScene?> SceneProperty =
        AvaloniaProperty.Register<ViewportControl, BuildingScene?>(
            nameof(Scene));

    public BuildingScene? Scene
    {
        get => GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    private bool _rendererInitialized;
    private bool _isDragging;
    private bool _isPanning;
    private Point _lastPointerPosition;

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

    if (properties.IsLeftButtonPressed || properties.IsMiddleButtonPressed)
    {
        _isDragging = true;
        _isPanning = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        _lastPointerPosition = e.GetPosition(this);

        e.Pointer.Capture(this);
        e.Handled = true;
    }
}

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_isDragging)
            return;

        Point current = e.GetPosition(this);

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
}
}
