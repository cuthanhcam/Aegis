using Aegis.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Aegis.Infrastructure
{
    public enum PostgresMigrationStartupMode
    {
        Apply,
        Validate,
    }

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
            return InitializePostgresAsync(dataSource, configuration, cancellationToken);
        }

        private static async Task InitializePostgresAsync(
            NpgsqlDataSource dataSource,
            IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            var lockTimeoutSeconds = configuration.GetValue<int?>("Database:Migrations:LockTimeoutSeconds") ?? 30;
            var statementTimeoutSeconds = configuration.GetValue<int?>("Database:Migrations:StatementTimeoutSeconds") ?? 120;
            var modeValue = configuration.GetValue<string>("Database:Migrations:Mode") ?? nameof(PostgresMigrationStartupMode.Apply);
            if (!Enum.TryParse<PostgresMigrationStartupMode>(modeValue, ignoreCase: true, out var mode))
            {
                throw new InvalidOperationException($"Unsupported Database:Migrations:Mode '{modeValue}'. Expected Apply or Validate.");
            }

            var migrationOptions = new PostgresMigrationOptions(
                TimeSpan.FromSeconds(lockTimeoutSeconds),
                TimeSpan.FromSeconds(statementTimeoutSeconds));
            if (mode == PostgresMigrationStartupMode.Validate)
            {
                if (configuration.GetSection("Seed:Development").GetValue<bool>("Enabled"))
                {
                    throw new InvalidOperationException("Development seeding cannot be enabled when Database:Migrations:Mode is Validate.");
                }

                await PostgresMigrationRunner.ValidateReadyAsync(dataSource, migrationOptions.StatementTimeout, cancellationToken);
                return;
            }

            await PostgresMigrationRunner.MigrateAsync(dataSource, migrationOptions, cancellationToken);

            var seedEnabled = configuration.GetSection("Seed:Development").GetValue<bool>("Enabled");
            if (!seedEnabled)
            {
                return;
            }

            await PostgresDevelopmentSeeder.SeedAsync(dataSource, cancellationToken);
        }
    }
}
