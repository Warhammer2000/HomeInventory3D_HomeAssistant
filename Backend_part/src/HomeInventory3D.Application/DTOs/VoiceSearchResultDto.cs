namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// Voice search response with plain text answer and structured results.
/// </summary>
public record VoiceSearchResultDto(
    string Answer,
    List<VoiceSearchItemDto> Items);

/// <summary>
/// A single item in voice search results.
/// </summary>
public record VoiceSearchItemDto(
    Guid Id,
    Guid ContainerId,
    string Name,
    string ContainerName,
    string ContainerLocation);
