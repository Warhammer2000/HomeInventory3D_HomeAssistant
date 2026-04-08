using HomeInventory3D.Application.DTOs;

namespace HomeInventory3D.Application.Interfaces;

/// <summary>
/// Parses 3D files and extracts individual meshes with geometry data.
/// </summary>
public interface IMeshProcessingService
{
    /// <summary>
    /// Parses a 3D file (.obj, .ply, .glb) and returns extracted meshes with vertices, indices, and bounding boxes.
    /// </summary>
    Task<MeshProcessingResult> ProcessFileAsync(string filePath, CancellationToken ct);
}
