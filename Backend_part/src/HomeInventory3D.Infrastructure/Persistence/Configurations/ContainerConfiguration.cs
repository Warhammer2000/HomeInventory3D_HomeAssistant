using HomeInventory3D.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeInventory3D.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Container entity.
/// </summary>
public class ContainerConfiguration : IEntityTypeConfiguration<Container>
{
    public void Configure(EntityTypeBuilder<Container> builder)
    {
        builder.ToTable("containers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(c => c.NfcId).HasColumnName("nfc_id").HasMaxLength(100);
        builder.Property(c => c.QrCode).HasColumnName("qr_code").HasMaxLength(500);
        builder.Property(c => c.Location).HasColumnName("location").HasMaxLength(500).IsRequired();
        builder.Property(c => c.Description).HasColumnName("description");

        builder.Property(c => c.WidthMm).HasColumnName("width_mm");
        builder.Property(c => c.HeightMm).HasColumnName("height_mm");
        builder.Property(c => c.DepthMm).HasColumnName("depth_mm");

        builder.Property(c => c.MeshFilePath).HasColumnName("mesh_file_path").HasMaxLength(1000);
        builder.Property(c => c.ThumbnailPath).HasColumnName("thumbnail_path").HasMaxLength(1000);

        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.Property(c => c.LastScannedAt).HasColumnName("last_scanned_at");

        builder.HasIndex(c => c.NfcId).IsUnique().HasFilter("nfc_id IS NOT NULL");
        builder.HasIndex(c => c.QrCode).IsUnique().HasFilter("qr_code IS NOT NULL");
    }
}
