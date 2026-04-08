using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Interfaces;

namespace HomeInventory3D.Application.Services;

/// <summary>
/// Application service for voice search (Home Assistant / Алиса).
/// </summary>
public class VoiceService(IItemRepository itemRepository)
{
    /// <summary>
    /// Searches items and returns a voice-friendly response.
    /// </summary>
    public async Task<VoiceSearchResultDto> SearchAsync(string query, CancellationToken ct)
    {
        var items = await itemRepository.SearchAsync(query, 5, ct);

        if (items.Count == 0)
        {
            return new VoiceSearchResultDto(
                $"Не нашёл «{query}» ни в одном контейнере.",
                []);
        }

        var resultItems = items.Select(i => new VoiceSearchItemDto(
            i.Id,
            i.Name,
            i.Container.Name,
            i.Container.Location)).ToList();

        var first = resultItems[0];
        var answer = items.Count == 1
            ? $"«{first.Name}» находится в контейнере «{first.ContainerName}», {first.ContainerLocation}."
            : $"«{first.Name}» находится в контейнере «{first.ContainerName}», {first.ContainerLocation}. " +
              $"Всего найдено совпадений: {items.Count}.";

        return new VoiceSearchResultDto(answer, resultItems);
    }
}
