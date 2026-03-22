namespace Modules.Shipments.Domain.ValueObjects;

public class Address
{
    public required string Street { get; set; }
    public required string City { get; set; }
    public required string Zip { get; set; }
}