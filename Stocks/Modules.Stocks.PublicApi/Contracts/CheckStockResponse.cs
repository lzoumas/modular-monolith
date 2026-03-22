namespace Modules.Stocks.PublicApi.Contracts;

public record CheckStockResponse(bool IsSuccess, string? ErrorMessage = null);