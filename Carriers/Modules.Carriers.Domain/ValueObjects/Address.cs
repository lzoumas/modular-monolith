namespace Modules.Carriers.Domain.ValueObjects;

public class Address
{
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Zip { get; set; } = null!;
}