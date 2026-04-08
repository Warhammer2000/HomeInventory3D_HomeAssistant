using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HomeInventory3D.Infrastructure.Mesh;

/// <summary>
/// ImageSharp implementation for rendering top-down wireframe thumbnails.
/// </summary>
public class ImageSharpThumbnailService : IThumbnailService
{
    private static readonly Color BackgroundColor = Color.White;
    private static readonly Color WireframeColor = Color.ParseHex("333333");
    private static readonly Color FillColor = Color.ParseHex("CCCCCC");

    public Task<Stream> RenderTopDownAsync(
        IReadOnlyList<MeshVertex> vertices,
        IReadOnlyList<int> indices,
        int width, int height,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (vertices.Count == 0)
            return Task.FromResult<Stream>(new MemoryStream());

        // Calculate XZ bounds for top-down projection
        var minX = float.MaxValue;
        var minZ = float.MaxValue;
        var maxX = float.MinValue;
        var maxZ = float.MinValue;

        foreach (var v in vertices)
        {
            minX = Math.Min(minX, v.X);
            minZ = Math.Min(minZ, v.Z);
            maxX = Math.Max(maxX, v.X);
            maxZ = Math.Max(maxZ, v.Z);
        }

        var rangeX = Math.Max(maxX - minX, 0.001f);
        var rangeZ = Math.Max(maxZ - minZ, 0.001f);

        // Add padding
        var padding = 10f;
        var drawWidth = width - 2 * padding;
        var drawHeight = height - 2 * padding;

        // Uniform scale to fit
        var scale = Math.Min(drawWidth / rangeX, drawHeight / rangeZ);

        PointF Project(MeshVertex v)
        {
            var x = (v.X - minX) * scale + padding + (drawWidth - rangeX * scale) / 2f;
            var y = (v.Z - minZ) * scale + padding + (drawHeight - rangeZ * scale) / 2f;
            return new PointF(x, y);
        }

        using var image = new Image<Rgba32>(width, height, BackgroundColor);

        image.Mutate(ctx =>
        {
            // Draw filled triangles first (light gray)
            for (var i = 0; i < indices.Count - 2; i += 3)
            {
                ct.ThrowIfCancellationRequested();

                var i0 = indices[i];
                var i1 = indices[i + 1];
                var i2 = indices[i + 2];

                if (i0 >= vertices.Count || i1 >= vertices.Count || i2 >= vertices.Count)
                    continue;

                var p0 = Project(vertices[i0]);
                var p1 = Project(vertices[i1]);
                var p2 = Project(vertices[i2]);

                ctx.FillPolygon(FillColor, p0, p1, p2);
            }

            // Draw wireframe edges (dark)
            for (var i = 0; i < indices.Count - 2; i += 3)
            {
                var i0 = indices[i];
                var i1 = indices[i + 1];
                var i2 = indices[i + 2];

                if (i0 >= vertices.Count || i1 >= vertices.Count || i2 >= vertices.Count)
                    continue;

                var p0 = Project(vertices[i0]);
                var p1 = Project(vertices[i1]);
                var p2 = Project(vertices[i2]);

                ctx.DrawLine(WireframeColor, 0.5f, p0, p1);
                ctx.DrawLine(WireframeColor, 0.5f, p1, p2);
                ctx.DrawLine(WireframeColor, 0.5f, p2, p0);
            }
        });

        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;

        return Task.FromResult<Stream>(stream);
    }
}
