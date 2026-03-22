using Microsoft.Extensions.DependencyInjection;
using Modules.Stocks.PublicApi;

namespace Modules.Stocks.Features;

public static class DependencyInjection
{
    public static IServiceCollection AddStocksModule(this IServiceCollection services)
    {
        services.AddScoped<IStockModuleApi, StockModuleApi>();
        
        return services;
    }
}