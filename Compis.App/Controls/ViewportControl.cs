
using System;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;

namespace Compis.App.Controls;

public sealed class ViewportControl : OpenGlControlBase
{
    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        gl.BindFramebuffer(0x8D40, fb);

        gl.Viewport(
            0,
            0,
            Math.Max(1, (int)Bounds.Width),
            Math.Max(1, (int)Bounds.Height)
        );

        gl.ClearColor(0.125f, 0.137f, 0.153f, 1.0f);
        gl.Clear(0x00004000 | 0x00000100);

        // Temporary test: clear the viewport to a dark blue-grey.
    }
}
