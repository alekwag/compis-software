namespace Compis.Core.Parameters;

public sealed record MaterialColor(float Red, float Green, float Blue)
{
    public static MaterialColor Default { get; } = new(0.72f, 0.53f, 0.34f);
}