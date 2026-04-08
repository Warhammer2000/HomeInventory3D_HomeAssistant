namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// A single mesh extracted from a 3D scene with geometry data and bounding box.
/// </summary>
public record ExtractedMeshData(
    string Name,
    IReadOnlyList<MeshVertex> Vertices,
    IReadOnlyList<int> Indices,
    float CenterX, float CenterY, float CenterZ,
    float BboxMinX, float BboxMinY, float BboxMinZ,
    float BboxMaxX, float BboxMaxY, float BboxMaxZ);
