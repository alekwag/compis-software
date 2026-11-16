
using System;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Compis.App.Rendering;
using Avalonia.Controls;

namespace Compis.App.Controls;

public sealed class ViewportControl : OpenGlControlBase
{
    private readonly CompisRenderer _renderer = new();
    private bool _rendererInitialized;

    public ViewportControl()
    {
        Console.WriteLine("VIEWPORT CONTROL CREATED");
    }
    protected override void OnOpenGlInit(GlInterface gl)
    {
        base.OnOpenGlInit(gl);

        Console.WriteLine("VIEWPORT INITIALISING");

        try
        {
            _renderer.Initialize(gl);
            _rendererInitialized = true;

            Console.WriteLine("OPENTK INITIALISED SUCCESSFULLY");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OPENTK ERROR: {ex}");
        }
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        Console.WriteLine("VIEWPORT RENDERING");

        double scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;

        int width = (int)Math.Ceiling(Bounds.Width * scaling);
        int height = (int)Math.Ceiling(Bounds.Height * scaling);

        if (_rendererInitialized)
        {
            _renderer.Render(fb, width, height);
        }
        else
        {
            gl.BindFramebuffer(0x8D40, fb);
            gl.Viewport(0, 0, width, height);
            gl.ClearColor(0.125f, 0.137f, 0.153f, 1.0f);
            gl.Clear(0x00004000 | 0x00000100);
        }
        Console.WriteLine($"Viewport: {Bounds.Width} x {Bounds.Height}");
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
    
}
