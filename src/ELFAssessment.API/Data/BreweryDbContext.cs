using Microsoft.EntityFrameworkCore;

namespace ELFAssessment.API.Data;

public sealed class BreweryDbContext : DbContext
{
    public BreweryDbContext(DbContextOptions<BreweryDbContext> options) : base(options) { }

    public DbSet<BreweryEntity> Breweries => Set<BreweryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BreweryEntity>(entity =>
        {
            entity.ToTable("Breweries");
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.City);
            entity.HasIndex(e => e.BreweryType);
        });
    }
}
