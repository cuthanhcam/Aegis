using Aegis.Infrastructure.Persistence;
using Npgsql;

const string connectionEnvironmentVariable = "ConnectionStrings__Aegis";
var lockTimeout = TimeSpan.FromSeconds(30);
var statementTimeout = TimeSpan.FromMinutes(2);

for (var index = 0; index < args.Length; index++)
{
    var argument = args[index];
    if (!string.Equals(argument, "--lock-timeout-seconds", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(argument, "--statement-timeout-seconds", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine($"Unsupported argument: {argument}");
        return 64;
    }

    if (++index >= args.Length || !int.TryParse(args[index], out var seconds) || seconds <= 0)
    {
        Console.Error.WriteLine($"{argument} requires a positive integer value.");
        return 64;
    }

    if (string.Equals(argument, "--lock-timeout-seconds", StringComparison.OrdinalIgnoreCase))
    {
        lockTimeout = TimeSpan.FromSeconds(seconds);
    }
    else
    {
        statementTimeout = TimeSpan.FromSeconds(seconds);
    }
}

var connectionString = Environment.GetEnvironmentVariable(connectionEnvironmentVariable);
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine($"Environment variable {connectionEnvironmentVariable} is required.");
    return 64;
}

await using var dataSource = NpgsqlDataSource.Create(connectionString);
await PostgresMigrationRunner.MigrateAsync(
    dataSource,
    new PostgresMigrationOptions(lockTimeout, statementTimeout));
await PostgresMigrationRunner.ValidateReadyAsync(dataSource, statementTimeout);
Console.WriteLine("Aegis database migrations applied and schema readiness verified.");
return 0;
