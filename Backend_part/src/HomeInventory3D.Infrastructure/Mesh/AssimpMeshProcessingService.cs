using Assimp;
using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace HomeInventory3D.Infrastructure.Mesh;

/// <summary>
/// AssimpNet implementation for parsing 3D files and extracting individual meshes.
/// </summary>
public class AssimpMeshProcessingService(
    ILogger<AssimpMeshProcessingService> logger) : IMeshProcessingService
{
    private static readonly HashSet<string> SupportedExtensions = [".obj", ".ply", ".glb", ".gltf", ".fbx", ".3ds"];

    public Task<MeshProcessingResult> ProcessFileAsync(string filePath, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (!SupportedExtensions.Contains(ext))
            throw new InvalidOperationException($"Unsupported file format: {ext}");

        if (!File.Exists(filePath))
            throw new FileNotFoundException("3D scan file not found", filePath);

        using var context = new AssimpContext();
        var scene = context.ImportFile(filePath,
            PostProcessSteps.Triangulate |
            PostProcessSteps.GenerateNormals |
            PostProcessSteps.JoinIdenticalVertices |
            PostProcessSteps.OptimizeMeshes);

        if (scene is null || !scene.HasMeshes)
            throw new InvalidOperationException("Failed to parse 3D file or file contains no meshes");

        logger.LogInformation("Parsed {FilePath}: {MeshCount} meshes", filePath, scene.MeshCount);

        var extractedMeshes = new List<ExtractedMeshData>(scene.MeshCount);

        // Scene-level bounding box accumulators
        var sceneMinX = float.MaxValue;
        var sceneMinY = float.MaxValue;
        var sceneMinZ = float.MaxValue;
        var sceneMaxX = float.MinValue;
        var sceneMaxY = float.MinValue;
        var sceneMaxZ = float.MinValue;

        for (var meshIndex = 0; meshIndex < scene.MeshCount; meshIndex++)
        {
            ct.ThrowIfCancellationRequested();

            var assimpMesh = scene.Meshes[meshIndex];
            if (assimpMesh.VertexCount == 0) continue;

            var meshData = ExtractMesh(assimpMesh, scene, meshIndex);
            extractedMeshes.Add(meshData);

            // Expand scene bounds
            sceneMinX = Math.Min(sceneMinX, meshData.BboxMinX);
            sceneMinY = Math.Min(sceneMinY, meshData.BboxMinY);
            sceneMinZ = Math.Min(sceneMinZ, meshData.BboxMinZ);
            sceneMaxX = Math.Max(sceneMaxX, meshData.BboxMaxX);
            sceneMaxY = Math.Max(sceneMaxY, meshData.BboxMaxY);
            sceneMaxZ = Math.Max(sceneMaxZ, meshData.BboxMaxZ);
        }

        var sceneBounds = new SceneBounds(sceneMinX, sceneMinY, sceneMinZ, sceneMaxX, sceneMaxY, sceneMaxZ);

        logger.LogInformation("Extracted {Count} meshes, scene bounds: ({MinX},{MinY},{MinZ})-({MaxX},{MaxY},{MaxZ})",
            extractedMeshes.Count,
            sceneBounds.MinX, sceneBounds.MinY, sceneBounds.MinZ,
            sceneBounds.MaxX, sceneBounds.MaxY, sceneBounds.MaxZ);

        return Task.FromResult(new MeshProcessingResult(extractedMeshes, sceneBounds));
    }

    private static ExtractedMeshData ExtractMesh(Assimp.Mesh assimpMesh, Scene scene, int meshIndex)
    {
        var name = !string.IsNullOrWhiteSpace(assimpMesh.Name)
            ? assimpMesh.Name
            : GetMaterialName(scene, assimpMesh.MaterialIndex) ?? $"Mesh_{meshIndex}";

        // Extract vertices
        var vertices = new MeshVertex[assimpMesh.VertexCount];
        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var minZ = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;
        var maxZ = float.MinValue;

        for (var i = 0; i < assimpMesh.VertexCount; i++)
        {
            var v = assimpMesh.Vertices[i];
            float? nx = null, ny = null, nz = null;

            if (assimpMesh.HasNormals)
            {
                var n = assimpMesh.Normals[i];
                nx = n.X;
                ny = n.Y;
                nz = n.Z;
            }

            vertices[i] = new MeshVertex(v.X, v.Y, v.Z, nx, ny, nz);

            minX = Math.Min(minX, v.X);
            minY = Math.Min(minY, v.Y);
            minZ = Math.Min(minZ, v.Z);
            maxX = Math.Max(maxX, v.X);
            maxY = Math.Max(maxY, v.Y);
            maxZ = Math.Max(maxZ, v.Z);
        }

        // Extract triangle indices
        var indices = new List<int>(assimpMesh.FaceCount * 3);
        foreach (var face in assimpMesh.Faces)
        {
            if (face.IndexCount != 3) continue; // skip non-triangles
            indices.Add(face.Indices[0]);
            indices.Add(face.Indices[1]);
            indices.Add(face.Indices[2]);
        }

        var centerX = (minX + maxX) / 2f;
        var centerY = (minY + maxY) / 2f;
        var centerZ = (minZ + maxZ) / 2f;

        return new ExtractedMeshData(
            name, vertices, indices,
            centerX, centerY, centerZ,
            minX, minY, minZ,
            maxX, maxY, maxZ);
    }

    private static string? GetMaterialName(Scene scene, int materialIndex)
    {
        if (!scene.HasMaterials || materialIndex < 0 || materialIndex >= scene.MaterialCount)
            return null;

        var mat = scene.Materials[materialIndex];
        return !string.IsNullOrWhiteSpace(mat.Name) ? mat.Name : null;
    }
}
