namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// Physical properties predicted by Claude Vision for realistic Unity physics simulation.
/// </summary>
public record PhysicsPropertiesDto(
    float MassKg,
    float RealSizeCm,
    string ColliderType,
    float Bounciness,
    float Friction,
    bool IsFragile,
    string MaterialType)
{
    /// <summary>Default physics for unrecognized objects.</summary>
    public static PhysicsPropertiesDto Default => new(0.15f, 10f, "box", 0.2f, 0.5f, false, "plastic");
}
