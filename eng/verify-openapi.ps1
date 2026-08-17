[CmdletBinding()]
param(
    [string]$BaselinePath = "docs/reference/openapi/aegis-v1.json",
    [string]$CandidatePath = "artifacts/openapi/aegis-v1.candidate.json",
    [string]$ReportPath = "artifacts/openapi/contract-diff.json"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$baseline = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $BaselinePath))
$candidate = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $CandidatePath))
$report = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $ReportPath))

if (-not (Test-Path -LiteralPath $baseline)) { throw "OpenAPI baseline '$baseline' does not exist." }

& "$PSScriptRoot/export-openapi.ps1" -OutputPath $CandidatePath

function Get-PropertyNames($value) {
    if ($null -eq $value) { return @() }
    return @($value.PSObject.Properties.Name)
}

$baselineJson = Get-Content -LiteralPath $baseline -Raw
$candidateJson = Get-Content -LiteralPath $candidate -Raw
$baselineDocument = $baselineJson | ConvertFrom-Json
$candidateDocument = $candidateJson | ConvertFrom-Json
$httpMethods = @("get", "put", "post", "delete", "patch", "options", "head", "trace")
$removedPaths = @(Get-PropertyNames $baselineDocument.paths | Where-Object { $_ -notin (Get-PropertyNames $candidateDocument.paths) })
$removedSchemas = @(Get-PropertyNames $baselineDocument.components.schemas | Where-Object { $_ -notin (Get-PropertyNames $candidateDocument.components.schemas) })
$removedOperations = @()

foreach ($path in (Get-PropertyNames $baselineDocument.paths)) {
    if ($path -in $removedPaths) { continue }
    $baselinePathItem = $baselineDocument.paths.$path
    $candidatePathItem = $candidateDocument.paths.$path
    foreach ($method in $httpMethods) {
        if ($null -ne $baselinePathItem.$method -and $null -eq $candidatePathItem.$method) {
            $removedOperations += "$($method.ToUpperInvariant()) $path"
        }
    }
}

$sha256 = [System.Security.Cryptography.SHA256]::Create()
try {
    $baselineHash = [BitConverter]::ToString($sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes($baselineJson))).Replace("-", "").ToLowerInvariant()
    $candidateHash = [BitConverter]::ToString($sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes($candidateJson))).Replace("-", "").ToLowerInvariant()
}
finally { $sha256.Dispose() }

$breakingChanges = @($removedPaths | ForEach-Object { "removed path: $_" }) +
    @($removedOperations | ForEach-Object { "removed operation: $_" }) +
    @($removedSchemas | ForEach-Object { "removed schema: $_" })
$result = [ordered]@{
    baseline = $BaselinePath
    candidate = $CandidatePath
    baselineSha256 = $baselineHash
    candidateSha256 = $candidateHash
    identical = $baselineHash -eq $candidateHash
    breaking = $breakingChanges.Count -gt 0
    breakingChanges = $breakingChanges
    summary = [ordered]@{
        baselinePaths = (Get-PropertyNames $baselineDocument.paths).Count
        candidatePaths = (Get-PropertyNames $candidateDocument.paths).Count
        removedPaths = $removedPaths.Count
        removedOperations = $removedOperations.Count
        removedSchemas = $removedSchemas.Count
    }
}

[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($report)) | Out-Null
[System.IO.File]::WriteAllText($report, ($result | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
Write-Host "OpenAPI contract report written to $report"

if ($result.breaking) { throw "Breaking OpenAPI changes detected. Review '$report'." }
if (-not $result.identical) { Write-Warning "The OpenAPI candidate differs additively or structurally from the committed baseline." }
