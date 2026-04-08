namespace HomeInventory3D.Application.Interfaces;

/// <summary>
/// Abstraction for file storage (local filesystem or MinIO).
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a file and returns the relative path.
    /// </summary>
    Task<string> SaveAsync(Stream content, string folder, string fileName, CancellationToken ct);

    /// <summary>
    /// Deletes a file by its relative path.
    /// </summary>
    Task DeleteAsync(string relativePath, CancellationToken ct);

    /// <summary>
    /// Returns the full URL for a relative path.
    /// </summary>
    string GetUrl(string relativePath);
}
