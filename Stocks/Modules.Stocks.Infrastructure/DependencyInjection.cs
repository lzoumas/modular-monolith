using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Stocks.Infrastructure.Database;

namespace Modules.Stocks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddStocksInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres");

        services.AddDbContext<StocksDbContext>(x => x
            .EnableSensitiveDataLogging()
            .UseNpgsql(postgresConnectionString, npgsqlOptions => 
                npgsqlOptions.MigrationsHistoryTable(DbConsts.MigrationHistoryTableName, DbConsts.StocksSchemaName))
            .UseSnakeCaseNamingConvention()
        );

        return services;
    }
}