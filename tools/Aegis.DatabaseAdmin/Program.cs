using System.Text.Json;
using Aegis.Infrastructure.Persistence;
using Npgsql;

const string connectionEnvironmentVariable = "ConnectionStrings__Aegis";
var validate = false;
var reportPath = Path.Combine("artifacts", "database", "store-constraint-reconciliation.json");
for (var index = 0; index < args.Length; index++)
{
    if (string.Equals(args[index], "--validate", StringComparison.OrdinalIgnoreCase))
    {
        validate = true;
        continue;
    }

    if (string.Equals(args[index], "--report", StringComparison.OrdinalIgnoreCase))
    {
        if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            Console.Error.WriteLine("--report requires a value.");
            return 64;
        }

        reportPath = args[index];
        continue;
    }

    Console.Error.WriteLine($"Unsupported argument: {args[index]}");
    return 64;
}

var connectionString = Environment.GetEnvironmentVariable(connectionEnvironmentVariable);
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine($"Environment variable {connectionEnvironmentVariable} is required.");
    return 64;
}

await using var dataSource = NpgsqlDataSource.Create(connectionString);
var service = new StoreConstraintReconciliationService(dataSource);
var report = await service.AuditAsync(validate);
var fullReportPath = Path.GetFullPath(reportPath);
Directory.CreateDirectory(Path.GetDirectoryName(fullReportPath)!);
await File.WriteAllTextAsync(
    fullReportPath,
    JsonSerializer.Serialize(report, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));

Console.WriteLine($"Store constraint reconciliation report written to {fullReportPath}");
Console.WriteLine($"Violations: {report.TotalViolations}; validation completed: {report.ValidationCompleted}");
return report.TotalViolations == 0 && (!validate || report.ValidationCompleted) ? 0 : 2;
