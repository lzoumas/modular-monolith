using Modules.Shipments.Domain.Enums;
using Modules.Shipments.Domain.ValueObjects;

namespace Modules.Shipments.Domain.Entities;

public class Shipment
{
    public Guid Id { get; set; }

    public required string Number { get; set; }

    public required string OrderId { get; set; }

    public required Address Address { get; set; }

    public required string Carrier { get; set; }

    public required string ReceiverEmail { get; set; }

    public required ShipmentStatus Status { get; set; }

    public required List<ShipmentItem> Items { get; set; } = [];

    public required DateTime CreatedAt { get; set; }

    public required DateTime? UpdatedAt { get; set; }
}