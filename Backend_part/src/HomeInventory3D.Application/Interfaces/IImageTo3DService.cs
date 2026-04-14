namespace HomeInventory3D.Application.Interfaces;

/// <summary>
/// Generates a 3D model (.glb) from a photo using AI.
/// </summary>
public interface IImageTo3DService
{
    /// <summary>
    /// Submits a photo with an optional object prompt, waits for 3D generation, returns GLB stream.
    /// Reports progress (0-100) via the progress callback.
    /// </summary>
    Task<Stream> GenerateModelAsync(Stream imageStream, string? objectPrompt,
        IProgress<int>? progress, CancellationToken ct);
}
