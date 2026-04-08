namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// Result of parsing a 3D file: all extracted meshes and the scene bounding box.
/// </summary>
public record MeshProcessingResult(
    List<ExtractedMeshData> Meshes,
    SceneBounds SceneBounds);
