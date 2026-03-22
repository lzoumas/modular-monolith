using Microsoft.EntityFrameworkCore;
using Modules.Carriers.Domain.Entities;

namespace Modules.Carriers.Infrastructure.Database;

public class CarriersDbContext(DbContextOptions<CarriersDbContext> options) : DbContext(options)
{
    public DbSet<Carrier> Carriers { get; set; }
    public DbSet<CarrierShipment> CarrierShipments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(DbConsts.CarriersSchemaName);

        modelBuilder.Entity<Carrier>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasMany(x => x.Shipments)
                .WithOne(x => x.Carrier)
                .HasForeignKey(x => x.CarrierId);
        });

        modelBuilder.Entity<CarrierShipment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrderId).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();

            entity.OwnsOne(x => x.ShippingAddress, address =>
            {
                address.Property(x => x.Street).IsRequired();
                address.Property(x => x.City).IsRequired();
                address.Property(x => x.Zip).IsRequired();
            });
        });
    }
}