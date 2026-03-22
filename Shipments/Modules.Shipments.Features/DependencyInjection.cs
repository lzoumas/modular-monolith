using Carter;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Shipments.Features;

public static class DependencyInjection
{
    public static IServiceCollection AddShipmentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(config => 
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });
        
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        
        return services;
    }
}