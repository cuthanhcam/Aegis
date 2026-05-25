param(
    [string]$JwtSecret,
    [switch]$KeepData
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($JwtSecret))
{
    $JwtSecret = $env:JWT_SECRET
}

if ([string]::IsNullOrWhiteSpace($JwtSecret))
{
    $JwtSecret = Read-Host 'Enter JWT secret for the dev reset run'
}

if ([string]::IsNullOrWhiteSpace($JwtSecret))
{
    throw 'JWT secret is required.'
}

$confirmation = Read-Host 'This will stop the Docker stack and reset the database volume. Type RESET to continue'
if ($confirmation -ne 'RESET')
{
    Write-Host 'Reset cancelled.'
    exit 1
}

$env:JWT_SECRET = $JwtSecret

& docker compose -f docker/docker-compose.yml -f docker/docker-compose.development.yml down

if (-not $KeepData)
{
    & docker compose -f docker/docker-compose.yml -f docker/docker-compose.development.yml down -v
}

& docker compose -f docker/docker-compose.yml -f docker/docker-compose.development.yml up --build migrate
