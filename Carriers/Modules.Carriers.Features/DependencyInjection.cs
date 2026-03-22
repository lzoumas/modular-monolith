using Microsoft.Extensions.DependencyInjection;
using Modules.Carriers.PublicApi;

namespace Modules.Carriers.Features;

public static class DependencyInjection
{
    public static IServiceCollection AddCarriersModule(this IServiceCollection services)
    {
        services.AddScoped<ICarrierModuleApi, CarrierModuleApi>();
        
        return services;
    }
}