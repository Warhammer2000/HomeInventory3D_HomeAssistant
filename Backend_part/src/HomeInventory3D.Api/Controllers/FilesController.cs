using HomeInventory3D.Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HomeInventory3D.Api.Controllers;

/// <summary>
/// Serves uploaded files (photos, meshes, point clouds).
/// </summary>
[ApiController]
[Route("[controller]")]
public class FilesController(IOptions<StorageOptions> storageOptions) : ControllerBase
{
    private readonly StorageOptions _options = storageOptions.Value;

    /// <summary>
    /// Serve a file by its relative path.
    /// </summary>
    [HttpGet("{**path}")]
    public IActionResult GetFile(string path)
    {
        var fullPath = Path.Combine(_options.BasePath, path);
        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        var contentType = GetContentType(fullPath);
        return PhysicalFile(fullPath, contentType);
    }

    private static string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".glb" => "model/gltf-binary",
            ".gltf" => "model/gltf+json",
            ".obj" => "text/plain",
            ".ply" => "application/octet-stream",
            ".usdz" => "model/vnd.usdz+zip",
            _ => "application/octet-stream"
        };
    }
}
