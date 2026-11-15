
using Compis.Core.Parameters;

namespace Compis.Core.Objects;

public sealed class BuildingVolume
{
    public Guid Id { get; }

    public string Name { get; set; }

    public Dimensions3D Dimensions { get; private set; }

    public Position3D Position { get; private set; }

    public string Category { get; set; }

    public string Function { get; set; }

    public BuildingVolume(
        string name,
        Dimensions3D dimensions,
        Position3D position)
    {
        Id = Guid.NewGuid();

        Name = name;
        Dimensions = dimensions;
        Position = position;

        Category = "Building Mass";
        Function = "Unassigned";
    }

    public void Resize(Dimensions3D dimensions)
    {
        Dimensions = dimensions;
    }

    public void MoveTo(Position3D position)
    {
        Position = position;
    }
}
