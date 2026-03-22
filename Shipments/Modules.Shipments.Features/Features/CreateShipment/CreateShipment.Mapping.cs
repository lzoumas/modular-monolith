using Modules.Shipments.Domain.Entities;
using Modules.Shipments.Domain.Enums;
using Modules.Shipments.Features.Features.Shared.Responses;

namespace Modules.Shipments.Features.Features.CreateShipment;

internal static class CreateShipmentMappingExtensions
{
    public static CreateShipmentCommand MapToCommand(this CreateShipmentRequest request)
        => new(request.OrderId,
            request.Address,
            request.Carrier,
            request.ReceiverEmail,
            request.Items);

    public static Shipment MapToShipment(this CreateShipmentCommand command, string shipmentNumber)
        => new Shipment
        {
	        Number = shipmentNumber,
	        OrderId = command.OrderId,
	        Address = command.Address,
	        Carrier = command.Carrier,
	        ReceiverEmail = command.ReceiverEmail,
	        Status = ShipmentStatus.Created,
	        Items = command.Items
		        .Select(x => new ShipmentItem
		        {
			        Product = x.Product,
			        Quantity = x.Quantity
		        }).ToList(),
	        CreatedAt = DateTime.UtcNow,
	        UpdatedAt = null
        };

    public static ShipmentResponse MapToResponse(this Shipment shipment)
        => new(
            shipment.Number,
            shipment.OrderId,
            shipment.Address,
            shipment.Carrier,
            shipment.ReceiverEmail,
            shipment.Status,
            shipment.Items
                .Select(x => new ShipmentItemResponse(x.Product, x.Quantity))
                .ToList()
            );
}
