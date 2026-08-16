[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [switch] $SkipRestore
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repositoryRoot 'Aegis.sln'

Push-Location $repositoryRoot
try {
    if (-not $SkipRestore) {
        dotnet restore $solution --locked-mode
        if ($LASTEXITCODE -ne 0) { throw 'Backend restore failed.' }
    }

    dotnet build $solution --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Backend build failed.' }

    dotnet test $solution --configuration $Configuration --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) { throw 'Backend tests failed.' }
}
finally {
    Pop-Location
}
