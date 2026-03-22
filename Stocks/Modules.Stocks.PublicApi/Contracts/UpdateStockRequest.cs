namespace Modules.Stocks.PublicApi.Contracts;

public record UpdateStockRequest(
    List<ProductStock> Products,
    StockOperation Operation);