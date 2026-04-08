using HomeInventory3D.Application.DTOs;

namespace HomeInventory3D.Application.Interfaces;

/// <summary>
/// Exports mesh geometry to GLB (glTF 2.0 binary) format.
/// </summary>
public interface IGlbExportService
{
    /// <summary>
    /// Creates a .glb file from vertices and indices.
    /// </summary>
    Task<Stream> ExportMeshAsync(
        IReadOnlyList<MeshVertex> vertices,
        IReadOnlyList<int> indices,
        string meshName,
        CancellationToken ct);
}
