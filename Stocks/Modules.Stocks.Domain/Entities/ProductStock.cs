namespace Modules.Stocks.Domain.Entities;

public class ProductStock
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = null!;
    public int AvailableQuantity { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}