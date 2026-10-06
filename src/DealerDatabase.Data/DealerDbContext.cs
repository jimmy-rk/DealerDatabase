using DealerDatabase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DealerDatabase.Data;

public class DealerDbContext(DbContextOptions<DealerDbContext> options) : DbContext(options)
{
    public DbSet<Dealer> Dealers => Set<Dealer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Dealer>(entity =>
        {
            entity.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(200);
        });
    }
}
