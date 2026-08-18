[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$baselinePath = Join-Path $repositoryRoot "docs/reference/openapi/aegis-v1.json"
$fixtureDirectory = Join-Path $repositoryRoot "artifacts/openapi/lifecycle"
[System.IO.Directory]::CreateDirectory($fixtureDirectory) | Out-Null

function Read-Baseline {
    return Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json
}

function Write-Fixture([string]$name, $document) {
    $relativePath = "artifacts/openapi/lifecycle/$name.json"
    $fullPath = Join-Path $repositoryRoot $relativePath
    [System.IO.File]::WriteAllText(
        $fullPath,
        ($document | ConvertTo-Json -Depth 100),
        [Text.UTF8Encoding]::new($false))
    return $relativePath
}

function Assert-Classification([string]$name, [string]$candidatePath, [bool]$shouldBreak) {
    $reportPath = "artifacts/openapi/lifecycle/$name.report.json"
    $threw = $false
    try {
        & "$PSScriptRoot/verify-openapi.ps1" `
            -CandidatePath $candidatePath `
            -ReportPath $reportPath `
            -SkipExport
    }
    catch {
        $threw = $true
        if (-not $shouldBreak) { throw }
    }

    if ($shouldBreak -and -not $threw) { throw "Lifecycle fixture '$name' should have been classified as breaking." }
    if (-not $shouldBreak -and $threw) { throw "Lifecycle fixture '$name' should have been accepted." }

    $report = Get-Content -LiteralPath (Join-Path $repositoryRoot $reportPath) -Raw | ConvertFrom-Json
    if ([bool]$report.breaking -ne $shouldBreak) {
        throw "Lifecycle fixture '$name' produced an inconsistent report classification."
    }
    Write-Host "Lifecycle fixture '$name' classified correctly (breaking=$shouldBreak)."
}

$additive = Read-Baseline
$additive.paths | Add-Member -NotePropertyName "/api/v1/contract-lifecycle-probe" -NotePropertyValue ([pscustomobject]@{
    get = [pscustomobject]@{
        responses = [pscustomobject]@{ "200" = [pscustomobject]@{ description = "Contract lifecycle probe" } }
    }
})
Assert-Classification "additive-path" (Write-Fixture "additive-path" $additive) $false

$deprecated = Read-Baseline
$deprecatedPathName = @($deprecated.paths.PSObject.Properties.Name)[0]
$deprecatedPath = $deprecated.paths.$deprecatedPathName
$deprecatedMethod = @($deprecatedPath.PSObject.Properties.Name | Where-Object { $_ -in @("get", "put", "post", "delete", "patch") })[0]
$deprecatedPath.$deprecatedMethod | Add-Member -NotePropertyName "deprecated" -NotePropertyValue $true -Force
Assert-Classification "deprecated-operation" (Write-Fixture "deprecated-operation" $deprecated) $false

$removedPath = Read-Baseline
$removedPathName = @($removedPath.paths.PSObject.Properties.Name)[0]
$removedPath.paths.PSObject.Properties.Remove($removedPathName)
Assert-Classification "removed-path" (Write-Fixture "removed-path" $removedPath) $true

$removedOperation = Read-Baseline
$operationPathName = @($removedOperation.paths.PSObject.Properties.Name)[0]
$operationPath = $removedOperation.paths.$operationPathName
$operationMethod = @($operationPath.PSObject.Properties.Name | Where-Object { $_ -in @("get", "put", "post", "delete", "patch") })[0]
$operationPath.PSObject.Properties.Remove($operationMethod)
Assert-Classification "removed-operation" (Write-Fixture "removed-operation" $removedOperation) $true

$removedSchema = Read-Baseline
$schemaName = @($removedSchema.components.schemas.PSObject.Properties.Name)[0]
$removedSchema.components.schemas.PSObject.Properties.Remove($schemaName)
Assert-Classification "removed-schema" (Write-Fixture "removed-schema" $removedSchema) $true

Write-Host "All OpenAPI lifecycle fixtures passed."
