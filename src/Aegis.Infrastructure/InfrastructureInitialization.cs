using Aegis.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Aegis.Infrastructure
{
    public static class InfrastructureInitialization
    {
        public static Task InitializeAegisInfrastructureAsync(
            this IServiceProvider services,
            IConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            var provider = configuration.GetSection("Storage").GetValue<string>("Provider") ?? "InMemory";
            if (!provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            using var scope = services.CreateScope();
            var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
            return PostgresMigrationRunner.MigrateAsync(dataSource, cancellationToken);
        }
    }
}
