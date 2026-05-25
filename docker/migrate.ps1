param(
    [string]$JwtSecret
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($JwtSecret))
{
    $JwtSecret = $env:JWT_SECRET
}

if ([string]::IsNullOrWhiteSpace($JwtSecret))
{
    $JwtSecret = Read-Host 'Enter JWT secret for the dev migration run'
}

if ([string]::IsNullOrWhiteSpace($JwtSecret))
{
    throw 'JWT secret is required.'
}

$env:JWT_SECRET = $JwtSecret

& docker compose -f docker/docker-compose.yml -f docker/docker-compose.development.yml up --build migrate
