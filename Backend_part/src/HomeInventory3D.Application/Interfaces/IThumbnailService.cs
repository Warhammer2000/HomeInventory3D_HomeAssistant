using HomeInventory3D.Application.DTOs;

namespace HomeInventory3D.Application.Interfaces;

/// <summary>
/// Renders 2D thumbnail previews of 3D meshes.
/// </summary>
public interface IThumbnailService
{
    /// <summary>
    /// Renders a top-down orthographic wireframe projection of a mesh.
    /// </summary>
    Task<Stream> RenderTopDownAsync(
        IReadOnlyList<MeshVertex> vertices,
        IReadOnlyList<int> indices,
        int width, int height,
        CancellationToken ct);
}
