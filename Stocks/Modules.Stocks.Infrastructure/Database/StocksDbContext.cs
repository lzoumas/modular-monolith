using Microsoft.EntityFrameworkCore;
using Modules.Stocks.Domain.Entities;

namespace Modules.Stocks.Infrastructure.Database;

public class StocksDbContext(DbContextOptions<StocksDbContext> options) : DbContext(options)
{
    public DbSet<ProductStock> ProductStocks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(DbConsts.StocksSchemaName);

        modelBuilder.Entity<ProductStock>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ProductName).IsUnique();
            
            entity.Property(x => x.ProductName).IsRequired();
            entity.Property(x => x.AvailableQuantity).IsRequired();
            entity.Property(x => x.LastUpdatedAt).IsRequired();
        });
    }
}