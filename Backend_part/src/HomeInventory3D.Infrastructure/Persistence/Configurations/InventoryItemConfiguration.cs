using HomeInventory3D.Domain.Entities;
using HomeInventory3D.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeInventory3D.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the InventoryItem entity.
/// </summary>
public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");

        builder.Property(i => i.ContainerId).HasColumnName("container_id").IsRequired();
        builder.Property(i => i.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
        builder.Property(i => i.Tags).HasColumnName("tags").HasDefaultValueSql("'{}'");
        builder.Property(i => i.Description).HasColumnName("description");

        builder.Property(i => i.PositionX).HasColumnName("position_x");
        builder.Property(i => i.PositionY).HasColumnName("position_y");
        builder.Property(i => i.PositionZ).HasColumnName("position_z");

        builder.Property(i => i.BboxMinX).HasColumnName("bbox_min_x");
        builder.Property(i => i.BboxMinY).HasColumnName("bbox_min_y");
        builder.Property(i => i.BboxMinZ).HasColumnName("bbox_min_z");
        builder.Property(i => i.BboxMaxX).HasColumnName("bbox_max_x");
        builder.Property(i => i.BboxMaxY).HasColumnName("bbox_max_y");
        builder.Property(i => i.BboxMaxZ).HasColumnName("bbox_max_z");

        builder.Property(i => i.RotationX).HasColumnName("rotation_x");
        builder.Property(i => i.RotationY).HasColumnName("rotation_y");
        builder.Property(i => i.RotationZ).HasColumnName("rotation_z");

        builder.Property(i => i.PhotoPath).HasColumnName("photo_path").HasMaxLength(1000);
        builder.Property(i => i.MeshFilePath).HasColumnName("mesh_file_path").HasMaxLength(1000);
        builder.Property(i => i.ThumbnailPath).HasColumnName("thumbnail_path").HasMaxLength(1000);

        builder.Property(i => i.Confidence).HasColumnName("confidence");
        builder.Property(i => i.RecognitionSource).HasColumnName("recognition_source")
            .HasConversion<string>().HasMaxLength(50);

        builder.Property(i => i.Status).HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20)
            .HasDefaultValue(ItemStatus.Present);

        builder.Property(i => i.CreatedAt).HasColumnName("created_at");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(i => i.Container)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.ContainerId).HasDatabaseName("idx_items_container");
        builder.HasIndex(i => i.Status).HasDatabaseName("idx_items_status");
        builder.HasIndex(i => i.Name)
            .HasDatabaseName("idx_items_name_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
        builder.HasIndex(i => i.Tags)
            .HasDatabaseName("idx_items_tags")
            .HasMethod("gin");
    }
}
