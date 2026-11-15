
using System;
using Avalonia.OpenGL;
using OpenTK;

namespace Compis.App.Rendering;

public sealed class AvaloniaBindingsContext : IBindingsContext
{
    private readonly GlInterface _gl;

    public AvaloniaBindingsContext(GlInterface gl)
    {
        _gl = gl;
    }

    public IntPtr GetProcAddress(string procName)
    {
        return _gl.GetProcAddress(procName);
    }
}
