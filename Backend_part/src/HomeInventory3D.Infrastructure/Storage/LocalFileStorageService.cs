using HomeInventory3D.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace HomeInventory3D.Infrastructure.Storage;

/// <summary>
/// Local filesystem implementation of file storage.
/// </summary>
public class LocalFileStorageService(IOptions<StorageOptions> options) : IFileStorageService
{
    private readonly StorageOptions _options = options.Value;

    public async Task<string> SaveAsync(Stream content, string folder, string fileName, CancellationToken ct)
    {
        var relativePath = Path.Combine(folder, fileName);
        var fullPath = Path.Combine(_options.BasePath, relativePath);

        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);

        return relativePath.Replace('\\', '/');
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct)
    {
        var fullPath = Path.Combine(_options.BasePath, relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    public string GetUrl(string relativePath)
    {
        return $"{_options.BaseUrl.TrimEnd('/')}/files/{relativePath.TrimStart('/')}";
    }
}
