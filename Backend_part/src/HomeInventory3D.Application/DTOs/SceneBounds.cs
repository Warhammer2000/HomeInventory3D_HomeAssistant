namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// Axis-aligned bounding box of the entire 3D scene.
/// </summary>
public record SceneBounds(
    float MinX, float MinY, float MinZ,
    float MaxX, float MaxY, float MaxZ)
{
    public float SizeX => Math.Max(MaxX - MinX, 0.001f);
    public float SizeY => Math.Max(MaxY - MinY, 0.001f);
    public float SizeZ => Math.Max(MaxZ - MinZ, 0.001f);
}
