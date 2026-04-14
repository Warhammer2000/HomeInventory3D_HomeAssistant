namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// Request body for POST /api/voice/search-and-notify.
/// </summary>
public record VoiceSearchNotifyRequestDto
{
    public required string Query { get; init; }
}
