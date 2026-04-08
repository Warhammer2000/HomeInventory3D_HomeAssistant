namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// A single vertex with position and optional normal.
/// </summary>
public record MeshVertex(
    float X, float Y, float Z,
    float? NX, float? NY, float? NZ);
