using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Shipments.Infrastructure.Database;

namespace Modules.Shipments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShipmentsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres");

        services.AddDbContext<ShipmentsDbContext>(x => x
            .EnableSensitiveDataLogging()
            .UseNpgsql(postgresConnectionString, npgsqlOptions => 
                npgsqlOptions.MigrationsHistoryTable(DbConsts.MigrationHistoryTableName, DbConsts.ShipmentsSchemaName))
            .UseSnakeCaseNamingConvention()
        );

        return services;
    }
}