using HomeInventory3D.Domain.Entities;
using HomeInventory3D.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeInventory3D.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the ScanSession entity.
/// </summary>
public class ScanSessionConfiguration : IEntityTypeConfiguration<ScanSession>
{
    public void Configure(EntityTypeBuilder<ScanSession> builder)
    {
        builder.ToTable("scan_sessions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.ContainerId).HasColumnName("container_id").IsRequired();
        builder.Property(s => s.ScanType).HasColumnName("scan_type")
            .HasConversion<string>().HasMaxLength(20);

        builder.Property(s => s.PointCloudPath).HasColumnName("point_cloud_path").HasMaxLength(1000);
        builder.Property(s => s.DepthMapPath).HasColumnName("depth_map_path").HasMaxLength(1000);
        builder.Property(s => s.RgbPhotoPath).HasColumnName("rgb_photo_path").HasMaxLength(1000);

        builder.Property(s => s.ItemsDetected).HasColumnName("items_detected").HasDefaultValue(0);
        builder.Property(s => s.ItemsAdded).HasColumnName("items_added").HasDefaultValue(0);
        builder.Property(s => s.ItemsRemoved).HasColumnName("items_removed").HasDefaultValue(0);

        builder.Property(s => s.Status).HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20)
            .HasDefaultValue(ScanStatus.Pending);

        builder.Property(s => s.ErrorMessage).HasColumnName("error_message");
        builder.Property(s => s.ScannedAt).HasColumnName("scanned_at");

        builder.HasOne(s => s.Container)
            .WithMany(c => c.ScanSessions)
            .HasForeignKey(s => s.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
