using HomeInventory3D.Application.Interfaces;
using HomeInventory3D.Application.Services;
using HomeInventory3D.Api.Hubs;

namespace HomeInventory3D.Api.Configuration;

/// <summary>
/// Registers API layer services and Application services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        // Application services
        services.AddScoped<ContainerService>();
        services.AddScoped<ItemService>();
        services.AddScoped<ScanService>();
        services.AddScoped<VoiceService>();

        // SignalR notification service
        services.AddScoped<IInventoryNotificationService, SignalRNotificationService>();

        return services;
    }
}
