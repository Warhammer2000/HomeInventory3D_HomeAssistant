using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Interfaces;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;

namespace HomeInventory3D.Infrastructure.Mesh;

/// <summary>
/// SharpGLTF implementation for exporting meshes to GLB format.
/// </summary>
public class SharpGltfExportService : IGlbExportService
{
    public Task<Stream> ExportMeshAsync(
        IReadOnlyList<MeshVertex> vertices,
        IReadOnlyList<int> indices,
        string meshName,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var material = new MaterialBuilder("default")
            .WithDoubleSide(true)
            .WithMetallicRoughnessShader()
            .WithBaseColor(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1f));

        var meshBuilder = new MeshBuilder<VertexPositionNormal>(meshName);
        var primitive = meshBuilder.UsePrimitive(material);

        for (var i = 0; i < indices.Count - 2; i += 3)
        {
            ct.ThrowIfCancellationRequested();

            var i0 = indices[i];
            var i1 = indices[i + 1];
            var i2 = indices[i + 2];

            if (i0 >= vertices.Count || i1 >= vertices.Count || i2 >= vertices.Count)
                continue;

            var v0 = ToVertexPositionNormal(vertices[i0]);
            var v1 = ToVertexPositionNormal(vertices[i1]);
            var v2 = ToVertexPositionNormal(vertices[i2]);

            primitive.AddTriangle(v0, v1, v2);
        }

        var sceneBuilder = new SceneBuilder();
        sceneBuilder.AddRigidMesh(meshBuilder, System.Numerics.Matrix4x4.Identity);

        var model = sceneBuilder.ToGltf2();

        var stream = new MemoryStream();
        model.WriteGLB(stream);
        stream.Position = 0;

        return Task.FromResult<Stream>(stream);
    }

    private static VertexPositionNormal ToVertexPositionNormal(MeshVertex v)
    {
        return new VertexPositionNormal(
            new System.Numerics.Vector3(v.X, v.Y, v.Z),
            new System.Numerics.Vector3(v.NX ?? 0, v.NY ?? 0, v.NZ ?? 1));
    }
}
