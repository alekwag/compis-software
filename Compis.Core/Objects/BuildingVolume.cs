
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using Compis.Core.Parameters;

namespace Compis.Core.Objects;

public sealed class BuildingVolume : INotifyPropertyChanged
{
    private IReadOnlyList<Position3D> _vertexOffsets = Enumerable.Range(0, 8)
        .Select(_ => new Position3D(0, 0, 0))
        .ToArray();

    public Guid Id { get; }

    public string Name { get; set; }

    public Dimensions3D Dimensions { get; private set; }

    public Position3D Position { get; private set; }

    public Rotation3D Rotation { get; private set; }

    public Scale3D Scale { get; private set; }

    public MaterialColor Color { get; private set; }

    public BoxParameters Parameters => new(Dimensions, Position, Rotation, Scale, Color, _vertexOffsets);

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
        Color = MaterialColor.Default;

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

    public void SetColor(MaterialColor color)
    {
        if (Color == color)
            return;

        Color = color;
        OnPropertyChanged(nameof(Color));
        OnPropertyChanged(nameof(Parameters));
    }

    public void MoveMeshVertices(IEnumerable<int> vertexIndices, Position3D localDelta)
    {
        var offsets = _vertexOffsets.ToArray();
        bool changed = false;

        foreach (int vertexIndex in vertexIndices.Distinct())
        {
            if ((uint)vertexIndex >= (uint)offsets.Length)
                continue;

            Position3D current = offsets[vertexIndex];
            offsets[vertexIndex] = new Position3D(
                current.X + localDelta.X,
                current.Y + localDelta.Y,
                current.Z + localDelta.Z);
            changed = true;
        }

        if (!changed)
            return;

        _vertexOffsets = offsets;
        OnPropertyChanged(nameof(Parameters));
    }

    public void SetMeshVertexWorldPositions(
        IReadOnlyDictionary<int, Vector3> worldPositions)
    {
        if (worldPositions.Count == 0)
            return;

        Vector3 position = new((float)Position.X, (float)Position.Y, (float)Position.Z);
        Vector3 scale = new((float)Scale.X, (float)Scale.Y, (float)Scale.Z);
        if (MathF.Abs(scale.X) < 0.0001f || MathF.Abs(scale.Y) < 0.0001f || MathF.Abs(scale.Z) < 0.0001f)
            return;

        Quaternion rotation = Quaternion.CreateFromYawPitchRoll(
            DegreesToRadians(Rotation.Y),
            DegreesToRadians(Rotation.X),
            DegreesToRadians(Rotation.Z));
        Quaternion inverseRotation = Quaternion.Conjugate(rotation);
        Position3D[] offsets = _vertexOffsets.ToArray();

        foreach ((int vertexIndex, Vector3 worldPosition) in worldPositions)
        {
            if ((uint)vertexIndex >= 8)
                continue;

            Vector3 transformedLocal = Vector3.Transform(worldPosition - position, inverseRotation);
            Vector3 local = new(
                transformedLocal.X / scale.X,
                transformedLocal.Y / scale.Y,
                transformedLocal.Z / scale.Z);
            Vector3 basePosition = GetBaseVertex(vertexIndex);

            offsets[vertexIndex] = new Position3D(
                local.X - basePosition.X,
                local.Y - basePosition.Y,
                local.Z - basePosition.Z);
        }

        _vertexOffsets = offsets;
        OnPropertyChanged(nameof(Parameters));
    }

    private Vector3 GetBaseVertex(int index)
    {
        float halfWidth = (float)(Dimensions.Width * 0.5);
        float halfDepth = (float)(Dimensions.Depth * 0.5);
        float z = index < 4 ? 0f : (float)Dimensions.Height;

        return (index % 4) switch
        {
            0 => new Vector3(-halfWidth, -halfDepth, z),
            1 => new Vector3(halfWidth, -halfDepth, z),
            2 => new Vector3(halfWidth, halfDepth, z),
            _ => new Vector3(-halfWidth, halfDepth, z)
        };
    }

    private static float DegreesToRadians(double degrees) =>
        (float)(degrees * Math.PI / 180.0);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
