param(
    [ValidateRange(1, 2147483647)]
    [int]$LockTimeoutSeconds = 30,
    [ValidateRange(1, 2147483647)]
    [int]$StatementTimeoutSeconds = 120
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "tools/Aegis.Migrator/Aegis.Migrator.csproj"

& dotnet restore $project --locked-mode
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& dotnet run `
    --project $project `
    --configuration Release `
    --no-restore `
    -- `
    --lock-timeout-seconds $LockTimeoutSeconds `
    --statement-timeout-seconds $StatementTimeoutSeconds
exit $LASTEXITCODE
