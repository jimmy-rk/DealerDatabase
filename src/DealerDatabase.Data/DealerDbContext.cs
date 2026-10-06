using DealerDatabase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DealerDatabase.Data;

public class DealerDbContext(DbContextOptions<DealerDbContext> options) : DbContext(options)
{
    public DbSet<Dealer> Dealers => Set<Dealer>();
    public DbSet<DealerSicCode> DealerSicCodes => Set<DealerSicCode>();
    public DbSet<DealerOfficer> DealerOfficers => Set<DealerOfficer>();
    public DbSet<DealerFcaPermission> DealerFcaPermissions => Set<DealerFcaPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Dealer>(entity =>
        {
            entity.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasMany(d => d.SicCodes)
                .WithOne(s => s.Dealer!)
                .HasForeignKey(s => s.DealerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(d => d.Officers)
                .WithOne(o => o.Dealer!)
                .HasForeignKey(o => o.DealerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(d => d.FcaPermissions)
                .WithOne(p => p.Dealer!)
                .HasForeignKey(p => p.DealerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DealerSicCode>(entity =>
        {
            entity.Property(s => s.Code).IsRequired().HasMaxLength(50);
            // Ensure each Dealer cannot have duplicate SIC codes
            entity.HasIndex(s => new { s.DealerId, s.Code }).IsUnique();
        });

        modelBuilder.Entity<DealerOfficer>(entity =>
        {
            entity.Property(o => o.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(o => new { o.DealerId, o.Name }).IsUnique();
        });

        modelBuilder.Entity<DealerFcaPermission>(entity =>
        {
            entity.Property(p => p.Permission).IsRequired().HasMaxLength(200);
            entity.HasIndex(p => new { p.DealerId, p.Permission }).IsUnique();
        });
    }
}
