
using Compis.Core.Objects;

namespace Compis.Core.Scene;

public sealed class BuildingScene
{
    private readonly List<BuildingVolume> _volumes = [];

    public IReadOnlyList<BuildingVolume> Volumes => _volumes;

    public void AddVolume(BuildingVolume volume)
    {
        ArgumentNullException.ThrowIfNull(volume);

        if (_volumes.Any(v => v.Id == volume.Id))
            throw new InvalidOperationException(
                "This building volume is already in the scene.");

        _volumes.Add(volume);
    }

    public bool RemoveVolume(Guid id)
    {
        var volume = _volumes.FirstOrDefault(v => v.Id == id);

        if (volume is null)
            return false;

        return _volumes.Remove(volume);
    }

    public BuildingVolume? FindVolume(Guid id)
    {
        return _volumes.FirstOrDefault(v => v.Id == id);
    }

    public void Clear()
    {
        _volumes.Clear();
    }
}
