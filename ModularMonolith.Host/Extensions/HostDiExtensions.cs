using Carter;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using ModularMonolith.Host.Seeding;

namespace ModularMonolith.Host.Extensions;

public static class HostDiExtensions
{
    public static IServiceCollection AddWebHostInfrastructure(this IServiceCollection services)
    {
        services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen();

        services.AddCarter();
        
        services.AddScoped<SeedService>();
        
        services.Configure<JsonOptions>(opt =>
        {
            opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return services;
    }

    public static void AddHostLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, loggerConfig) => 
            loggerConfig.ReadFrom.Configuration(context.Configuration));
    }
}