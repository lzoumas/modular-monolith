namespace Modules.Carriers.Domain.Entities;

public class Carrier
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public List<CarrierShipment> Shipments { get; set; } = [];
}