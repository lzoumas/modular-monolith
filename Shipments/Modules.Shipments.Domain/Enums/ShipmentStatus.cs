namespace Modules.Shipments.Domain.Enums;

public enum ShipmentStatus
{
    Created,
    Processing,
    Dispatched,
    InTransit,
    WaitingCustomer,
    Delivered,
    Cancelled
}