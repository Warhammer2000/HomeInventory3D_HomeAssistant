using HomeInventory3D.Application.DTOs;

namespace HomeInventory3D.Application.Interfaces;

/// <summary>
/// Abstraction for AI-powered object recognition from images.
/// </summary>
public interface IVisionRecognitionService
{
    /// <summary>
    /// Recognizes items from a rendered image of a segmented object.
    /// </summary>
    Task<List<RecognizedItemDto>> RecognizeItemsAsync(
        Stream photo, string? containerContext, CancellationToken ct);
}
