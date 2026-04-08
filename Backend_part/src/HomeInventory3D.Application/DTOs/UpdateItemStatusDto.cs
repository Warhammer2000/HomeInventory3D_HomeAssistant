using HomeInventory3D.Domain.Enums;

namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// Data for changing item status.
/// </summary>
public record UpdateItemStatusDto(ItemStatus Status);
