using HomeInventory3D.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory3D.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for the inventory system.
/// </summary>
public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Container> Containers => Set<Container>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<ScanSession> ScanSessions => Set<ScanSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}
