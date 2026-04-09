using HomeInventory3D.Application.BackgroundJobs;
using HomeInventory3D.Application.Interfaces;
using HomeInventory3D.Infrastructure.BackgroundJobs;
using HomeInventory3D.Infrastructure.Persistence;
using HomeInventory3D.Infrastructure.Persistence.Repositories;
using HomeInventory3D.Infrastructure.Storage;
using HomeInventory3D.Infrastructure.Mesh;
using HomeInventory3D.Infrastructure.Meshy;
using HomeInventory3D.Infrastructure.Vision;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeInventory3D.Infrastructure;

/// <summary>
/// Registers all Infrastructure layer services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // EF Core + PostgreSQL
        services.AddDbContext<InventoryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IContainerRepository, ContainerRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IScanSessionRepository, ScanSessionRepository>();

        // File storage
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // Claude Vision
        services.Configure<ClaudeOptions>(configuration.GetSection(ClaudeOptions.SectionName));
        services.AddHttpClient<IVisionRecognitionService, ClaudeVisionService>();

        // Meshy AI Image-to-3D
        services.Configure<MeshyOptions>(configuration.GetSection(MeshyOptions.SectionName));
        services.AddHttpClient<IImageTo3DService, MeshyImageTo3DService>();

        // Mesh processing pipeline
        services.AddScoped<IMeshProcessingService, AssimpMeshProcessingService>();
        services.AddScoped<IGlbExportService, SharpGltfExportService>();
        services.AddScoped<IThumbnailService, ImageSharpThumbnailService>();

        // Background processing
        services.AddSingleton<IScanProcessingChannel, ScanProcessingChannel>();
        services.AddHostedService<ScanProcessingWorker>();

        return services;
    }
}
