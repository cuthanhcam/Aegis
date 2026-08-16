[CmdletBinding()]
param([string]$OutputPath = "artifacts/openapi/aegis-v1.json")

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$resolvedOutput = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputPath))
$previousOutput = $env:AEGIS_OPENAPI_OUTPUT

try {
    $env:AEGIS_OPENAPI_OUTPUT = $resolvedOutput
    dotnet test "$repositoryRoot/tests/Aegis.IntegrationTests/Aegis.IntegrationTests.csproj" `
        --configuration Release `
        --filter "FullyQualifiedName~ApiContractGovernanceTests.OpenApiV1_IsResolvableAndCanBeExported"
    if ($LASTEXITCODE -ne 0) { throw "OpenAPI export test failed with exit code $LASTEXITCODE." }
    if (-not (Test-Path -LiteralPath $resolvedOutput)) { throw "OpenAPI export did not create '$resolvedOutput'." }
    Write-Host "OpenAPI v1 exported to $resolvedOutput"
}
finally { $env:AEGIS_OPENAPI_OUTPUT = $previousOutput }
