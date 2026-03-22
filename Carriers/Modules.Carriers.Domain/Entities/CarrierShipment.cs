using Modules.Carriers.Domain.ValueObjects;

namespace Modules.Carriers.Domain.Entities;

public class CarrierShipment
{
    public Guid Id { get; set; }
    public Guid CarrierId { get; set; }
    public string OrderId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public Carrier Carrier { get; set; } = null!;
    public Address ShippingAddress { get; set; } = null!;
}