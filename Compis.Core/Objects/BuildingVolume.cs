
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Compis.Core.Parameters;

namespace Compis.Core.Objects;

public sealed class BuildingVolume : INotifyPropertyChanged
{
    public Guid Id { get; }

    public string Name { get; set; }

    public Dimensions3D Dimensions { get; private set; }

    public Position3D Position { get; private set; }

    public Rotation3D Rotation { get; private set; }

    public Scale3D Scale { get; private set; }

    public BoxParameters Parameters => new(Dimensions, Position, Rotation, Scale);

    public string Category { get; set; }

    public string Function { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public BuildingVolume(
        string name,
        Dimensions3D dimensions,
        Position3D position)
    {
        Id = Guid.NewGuid();

        Name = name;
        Dimensions = dimensions;
        Position = position;
        Rotation = Rotation3D.Zero;
        Scale = Scale3D.One;

        Category = "Building Mass";
        Function = "Unassigned";
    }

    public void Resize(Dimensions3D dimensions)
    {
        if (Dimensions == dimensions)
            return;

        Dimensions = dimensions;
        OnPropertyChanged(nameof(Dimensions));
    }

    public void MoveTo(Position3D position)
    {
        if (Position == position)
            return;

        Position = position;
        OnPropertyChanged(nameof(Position));
    }

    public void RotateTo(Rotation3D rotation)
    {
        if (Rotation == rotation)
            return;

        Rotation = rotation;
        OnPropertyChanged(nameof(Rotation));
    }

    public void ScaleTo(Scale3D scale)
    {
        if (Scale == scale)
            return;

        Scale = scale;
        OnPropertyChanged(nameof(Scale));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
