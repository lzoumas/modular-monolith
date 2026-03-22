namespace Modules.Carriers.PublicApi.Contracts;

public record CreateCarrierShipmentRequest(
    string OrderId, 
    Address Address, 
    string Carrier, 
    string ReceiverEmail, 
    List<CarrierShipmentItem> Items);