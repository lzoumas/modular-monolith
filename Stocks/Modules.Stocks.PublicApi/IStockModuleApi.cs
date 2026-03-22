using Modules.Stocks.PublicApi.Contracts;

namespace Modules.Stocks.PublicApi;

public interface IStockModuleApi
{
    Task<CheckStockResponse> CheckStockAsync(
        CheckStockRequest request, 
        CancellationToken cancellationToken = default);
    
    Task<UpdateStockResponse> UpdateStockAsync(
        UpdateStockRequest request,
        CancellationToken cancellationToken = default);
}
