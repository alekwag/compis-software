using System.Collections.Generic;
using System.Numerics;
using Compis.Core.Objects;

namespace Compis.App.Rendering;

public enum MeshElementMode
{
    Object,
    Vertex,
    Edge,
    Face
}

public sealed record MeshElementSelection(
    BuildingVolume Volume,
    MeshElementMode Mode,
    int Index,
    IReadOnlyList<int> VertexIndices,
    Vector3 WorldCentroid);