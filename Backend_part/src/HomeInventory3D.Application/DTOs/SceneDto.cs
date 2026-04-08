namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// Full 3D scene data for Unity client — container mesh + all items with positions.
/// </summary>
public record SceneDto(
    ContainerDto Container,
    List<ItemDto> Items);
