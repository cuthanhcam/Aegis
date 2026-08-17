[CmdletBinding()]
param(
    [string]$OpenApiPath = "docs/reference/openapi/aegis-v1.json",
    [string]$OutputPath = "artifacts/generated-client"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$openApi = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OpenApiPath))
$output = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputPath))

dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw "Local .NET tool restore failed." }

dotnet kiota generate -l TypeScript -d $openApi -c AegisApiClient -o $output --clean-output --exclude-backward-compatible
if ($LASTEXITCODE -ne 0) { throw "Kiota TypeScript generation failed." }

$packageJson = '{"private":true,"type":"module","dependencies":{"@microsoft/kiota-bundle":"1.0.0-preview.103"},"devDependencies":{"typescript":"7.0.2"}}'
$tsconfig = '{"compilerOptions":{"esModuleInterop":true,"forceConsistentCasingInFileNames":true,"lib":["ES2022","DOM"],"module":"NodeNext","moduleResolution":"NodeNext","noEmit":true,"skipLibCheck":true,"strict":true,"target":"ES2022"},"include":["./**/*.ts"]}'
[System.IO.File]::WriteAllText((Join-Path $output "package.json"), $packageJson, [Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $output "tsconfig.json"), $tsconfig, [Text.UTF8Encoding]::new($false))

Push-Location $output
try {
    npm install --package-lock-only --ignore-scripts
    if ($LASTEXITCODE -ne 0) { throw "Generated-client lock creation failed." }
    npm ci --ignore-scripts
    if ($LASTEXITCODE -ne 0) { throw "Generated-client dependency restore failed." }
    npx tsc --noEmit
    if ($LASTEXITCODE -ne 0) { throw "Generated TypeScript client failed strict compilation." }
}
finally { Pop-Location }

Write-Host "Generated Aegis TypeScript client compiled successfully in $output"
