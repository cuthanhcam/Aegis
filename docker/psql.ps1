param(
    [string]$Database = 'aegis',
    [string]$User = 'postgres',
    [string]$Password = $env:POSTGRES_PASSWORD
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Password))
{
    $Password = 'postgres'
}

$env:PGPASSWORD = $Password

& docker compose -f docker/docker-compose.yml -f docker/docker-compose.development.yml exec postgres psql -U $User -d $Database
