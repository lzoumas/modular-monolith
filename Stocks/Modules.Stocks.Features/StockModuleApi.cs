using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Stocks.Infrastructure.Database;
using Modules.Stocks.PublicApi;
using Modules.Stocks.PublicApi.Contracts;

namespace Modules.Stocks.Features;

internal sealed class StockModuleApi(
    StocksDbContext dbContext,
    ILogger<StockModuleApi> logger) : IStockModuleApi
{
    public async Task<CheckStockResponse> CheckStockAsync(
        CheckStockRequest request, 
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Checking stock for {Count} products", request.Products.Count);

        var productIds = request.Products.Select(x => x.ProductId).ToList();
        
        var stocks = await dbContext.ProductStocks
            .Where(x => productIds.Contains(x.ProductName))
            .ToDictionaryAsync(x => x.ProductName, x => x.AvailableQuantity, cancellationToken);

        foreach (var product in request.Products)
        {
            if (!stocks.TryGetValue(product.ProductId, out var availableQuantity))
            {
                logger.LogWarning("Product {ProductId} not found in stock", product.ProductId);
                return new CheckStockResponse(false, $"Product {product.ProductId} not found in stock");
            }

            if (availableQuantity >= product.Quantity)
            {
                continue;
            }
            
            logger.LogWarning(
                "Insufficient stock for product {ProductId}. Required: {Required}, Available: {Available}", 
                product.ProductId, product.Quantity, availableQuantity);
                
            return new CheckStockResponse(
                false, 
                $"Insufficient stock for product {product.ProductId}. Required: {product.Quantity}, Available: {availableQuantity}");
        }

        logger.LogInformation("Stock check passed for all products");
        
        return new CheckStockResponse(true);
    }

    public async Task<UpdateStockResponse> UpdateStockAsync(
        UpdateStockRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Updating stock for {Count} products. Operation: {Operation}", 
            request.Products.Count,
            request.Operation);

        var productIds = request.Products.Select(x => x.ProductId).ToList();
        
        var stocks = await dbContext.ProductStocks
            .Where(x => productIds.Contains(x.ProductName))
            .ToDictionaryAsync(x => x.ProductName, x => x, cancellationToken);

        var notFoundProducts = productIds
            .Where(id => !stocks.ContainsKey(id))
            .ToList();

        if (notFoundProducts.Any())
        {
            var errorMessage = $"Products not found in stock: {string.Join(", ", notFoundProducts)}";
            logger.LogWarning(errorMessage);
            return new UpdateStockResponse(false, errorMessage);
        }

        foreach (var product in request.Products)
        {
            var stock = stocks[product.ProductId];
            var newQuantity = request.Operation switch
            {
                StockOperation.Increase => stock.AvailableQuantity + product.Quantity,
                StockOperation.Decrease => stock.AvailableQuantity - product.Quantity,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(request.Operation), 
                    request.Operation, 
                    "Invalid stock operation")
            };

            if (newQuantity < 0)
            {
                var errorMessage = $"Insufficient stock for product {product.ProductId}. " +
                                 $"Available: {stock.AvailableQuantity}, " +
                                 $"Requested change: {product.Quantity}";
                logger.LogWarning(errorMessage);
                return new UpdateStockResponse(false, errorMessage);
            }

            stock.AvailableQuantity = newQuantity;
            stock.LastUpdatedAt = DateTime.UtcNow;
            
            logger.LogInformation(
                "Updated stock for product {ProductId}. Old quantity: {OldQuantity}, New quantity: {NewQuantity}", 
                product.ProductId,
                stock.AvailableQuantity,
                newQuantity);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Stock update completed successfully");
        
        return new UpdateStockResponse(true);
    }
}