namespace Modules.Stocks.PublicApi.Contracts;

public record UpdateStockResponse(bool IsSuccess, string? ErrorMessage = null);