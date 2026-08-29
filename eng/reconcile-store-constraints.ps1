param(
    [switch]$Validate,
    [string]$ReportPath = "artifacts/database/store-constraint-reconciliation.json"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "tools/Aegis.DatabaseAdmin/Aegis.DatabaseAdmin.csproj"
if (-not [System.IO.Path]::IsPathRooted($ReportPath)) {
    $ReportPath = Join-Path $repoRoot $ReportPath
}

& dotnet restore $project --locked-mode
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$arguments = @("run", "--project", $project, "--configuration", "Release", "--no-restore", "--", "--report", $ReportPath)
if ($Validate) {
    $arguments += "--validate"
}

& dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
