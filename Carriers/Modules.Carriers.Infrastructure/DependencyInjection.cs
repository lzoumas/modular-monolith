using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Carriers.Infrastructure.Database;

namespace Modules.Carriers.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCarriersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres");

        services.AddDbContext<CarriersDbContext>(x => x
            .EnableSensitiveDataLogging()
            .UseNpgsql(postgresConnectionString, npgsqlOptions => 
                npgsqlOptions.MigrationsHistoryTable(DbConsts.MigrationHistoryTableName, DbConsts.CarriersSchemaName))
            .UseSnakeCaseNamingConvention()
        );

        return services;
    }
}