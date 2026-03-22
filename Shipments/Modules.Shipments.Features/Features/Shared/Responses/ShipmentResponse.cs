using Modules.Shipments.Domain.Enums;
using Modules.Shipments.Domain.ValueObjects;

namespace Modules.Shipments.Features.Features.Shared.Responses;

public sealed record ShipmentResponse(
    string Number,
    string OrderId,
    Address Address,
    string Carrier,
    string ReceiverEmail,
    ShipmentStatus Status,
    List<ShipmentItemResponse> Items);
