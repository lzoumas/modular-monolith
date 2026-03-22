using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Carriers.Domain.Entities;
using Modules.Carriers.Infrastructure.Database;
using Modules.Carriers.PublicApi;
using Modules.Carriers.PublicApi.Contracts;
using Address = Modules.Carriers.Domain.ValueObjects.Address;

namespace Modules.Carriers.Features;

internal sealed class CarrierModuleApi(
    CarriersDbContext dbContext,
    ILogger<CarrierModuleApi> logger) : ICarrierModuleApi
{
    public async Task CreateShipmentAsync(CreateCarrierShipmentRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating shipment for order {OrderId}", request.OrderId);

        var carrier = await dbContext.Carriers
            .FirstOrDefaultAsync(x => x.Name == request.Carrier && x.IsActive, cancellationToken);

        if (carrier is null)
        {
            throw new InvalidOperationException($"Active carrier with Name {request.Carrier} not found");
        }

        var shipment = CreateCarrierShipment(request, carrier);

        dbContext.CarrierShipments.Add(shipment);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created shipment {ShipmentId} for order {OrderId}", shipment.Id, request.OrderId);
    }

    private static CarrierShipment CreateCarrierShipment(CreateCarrierShipmentRequest request, Carrier carrier)
    {
        return new CarrierShipment
        {
            Id = Guid.NewGuid(),
            CarrierId = carrier.Id,
            OrderId = request.OrderId,
            CreatedAt = DateTime.UtcNow,
            ShippingAddress = new Address
            {
                Street = request.Address.Street,
                City = request.Address.City,
                Zip = request.Address.Zip
            }
        };
    }
}