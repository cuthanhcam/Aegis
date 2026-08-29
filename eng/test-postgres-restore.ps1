param(
    [string]$ReportPath = "artifacts/database/postgres-restore-drill.json"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not [System.IO.Path]::IsPathRooted($ReportPath)) {
    $ReportPath = Join-Path $repoRoot $ReportPath
}

$reportDirectory = Split-Path -Parent $ReportPath
New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null
$suffix = [Guid]::NewGuid().ToString("N").Substring(0, 12)
$sourceContainer = "aegis-restore-source-$suffix"
$targetContainer = "aegis-restore-target-$suffix"
$dumpPath = Join-Path $reportDirectory "aegis-$suffix.dump"
$password = "aegis-local-restore-drill"
$startedAt = [DateTimeOffset]::UtcNow
$previousConnection = [Environment]::GetEnvironmentVariable("ConnectionStrings__Aegis")

function Invoke-Docker {
    param([string[]]$Arguments)
    $output = & docker @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Docker command failed: docker $($Arguments -join ' ')`n$($output -join [Environment]::NewLine)"
    }
}

function Wait-Postgres {
    param([string]$Container)
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        & docker exec $Container pg_isready -U postgres -d aegis *> $null
        if ($LASTEXITCODE -eq 0) {
            return
        }

        Start-Sleep -Seconds 1
    }

    throw "PostgreSQL container $Container did not become ready."
}

function Get-MappedPort {
    param([string]$Container)
    $mapping = (& docker port $Container 5432/tcp).Trim()
    if ($LASTEXITCODE -ne 0 -or $mapping -notmatch ':(\d+)$') {
        throw "Could not resolve PostgreSQL port for $Container."
    }

    return $Matches[1]
}

function Invoke-Psql {
    param([string]$Container, [string]$Sql)
    $result = $Sql | & docker exec -i $Container psql -X -v ON_ERROR_STOP=1 -U postgres -d aegis -At
    if ($LASTEXITCODE -ne 0) {
        throw "psql failed in $Container."
    }

    return $result
}

try {
    & docker info *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker is required for the isolated restore drill."
    }

    Invoke-Docker @("run", "--detach", "--name", $sourceContainer, "--env", "POSTGRES_PASSWORD=$password", "--env", "POSTGRES_DB=aegis", "--publish", "127.0.0.1::5432", "postgres:16-alpine")
    Wait-Postgres $sourceContainer

    Get-ChildItem (Join-Path $repoRoot "src/Aegis.Infrastructure/Persistence/Migrations/*.sql") |
        Sort-Object Name |
        ForEach-Object { Invoke-Psql $sourceContainer (Get-Content $_.FullName -Raw) | Out-Null }

    $seedSql = @"
INSERT INTO stores (id, tenant_id, name, created_at, updated_at)
VALUES ('restore-store', 'restore-tenant', 'Restore Drill', NOW(), NOW());
INSERT INTO authorization_models (id, store_id, schema_version, model, created_at, state, revision)
VALUES ('restore-model', 'restore-store', '1.1', E'type user\n\ntype document\n  relations\n    define viewer: [user]', NOW(), 'Published', 1);
INSERT INTO relationships (id, tenant_id, store_id, subject, relation, object_ref, effect, created_at, updated_at)
VALUES (gen_random_uuid(), 'restore-tenant', 'restore-store', 'user:anne', 'viewer', 'document:roadmap', 'Allow', NOW(), NOW());
INSERT INTO assertion_sets (store_id, authorization_model_id, revision, assertions_json, updated_at)
VALUES ('restore-store', 'restore-model', 1, '[]'::jsonb, NOW());
INSERT INTO audit_events (id, tenant_id, store_id, action, subject, relation, object_ref, decision, reason_code, created_at)
VALUES (gen_random_uuid(), 'restore-tenant', 'restore-store', 'check', 'user:anne', 'viewer', 'document:roadmap', 'Allow', 'RELATIONSHIP_MATCH', NOW());
"@
    Invoke-Psql $sourceContainer $seedSql | Out-Null

    Invoke-Docker @("exec", $sourceContainer, "pg_dump", "-U", "postgres", "-d", "aegis", "--format=custom", "--file=/tmp/aegis.dump")
    Invoke-Docker @("cp", "${sourceContainer}:/tmp/aegis.dump", $dumpPath)
    $dumpHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $dumpPath).Hash.ToLowerInvariant()

    Invoke-Docker @("run", "--detach", "--name", $targetContainer, "--env", "POSTGRES_PASSWORD=$password", "--env", "POSTGRES_DB=aegis", "--publish", "127.0.0.1::5432", "postgres:16-alpine")
    Wait-Postgres $targetContainer
    Invoke-Docker @("cp", $dumpPath, "${targetContainer}:/tmp/aegis.dump")
    Invoke-Docker @("exec", $targetContainer, "pg_restore", "-U", "postgres", "-d", "aegis", "--exit-on-error", "/tmp/aegis.dump")

    $countsOutput = Invoke-Psql $targetContainer @"
SELECT 'stores=' || COUNT(*) FROM stores WHERE id = 'restore-store';
SELECT 'models=' || COUNT(*) FROM authorization_models WHERE store_id = 'restore-store';
SELECT 'relationships=' || COUNT(*) FROM relationships WHERE store_id = 'restore-store';
SELECT 'assertion_sets=' || COUNT(*) FROM assertion_sets WHERE store_id = 'restore-store';
SELECT 'audit_events=' || COUNT(*) FROM audit_events WHERE store_id = 'restore-store';
SELECT 'authorization_fixture=' || COUNT(*) FROM relationships WHERE tenant_id = 'restore-tenant' AND store_id = 'restore-store' AND subject = 'user:anne' AND relation = 'viewer' AND object_ref = 'document:roadmap' AND effect = 'Allow';
"@
    $counts = @{}
    foreach ($line in $countsOutput) {
        $parts = $line -split '=', 2
        $counts[$parts[0]] = [int64]$parts[1]
    }

    foreach ($required in @("stores", "models", "relationships", "assertion_sets", "audit_events", "authorization_fixture")) {
        if ($counts[$required] -ne 1) {
            throw "Restore verification failed for $required; expected 1, observed $($counts[$required])."
        }
    }

    $targetPort = Get-MappedPort $targetContainer
    [Environment]::SetEnvironmentVariable("ConnectionStrings__Aegis", "Host=127.0.0.1;Port=$targetPort;Database=aegis;Username=postgres;Password=$password")
    $reconciliationPath = Join-Path $reportDirectory "postgres-restore-reconciliation-$suffix.json"
    & (Join-Path $repoRoot "eng/reconcile-store-constraints.ps1") -Validate -ReportPath $reconciliationPath
    if ($LASTEXITCODE -ne 0) {
        throw "Restored database reconciliation failed."
    }

    $completedAt = [DateTimeOffset]::UtcNow
    $report = [ordered]@{
        startedAt = $startedAt.ToString("O")
        completedAt = $completedAt.ToString("O")
        durationSeconds = [Math]::Round(($completedAt - $startedAt).TotalSeconds, 3)
        postgresImage = "postgres:16-alpine"
        dumpSha256 = $dumpHash
        counts = $counts
        reconciliationReport = [System.IO.Path]::GetFileName($reconciliationPath)
        restoreVerified = $true
    }
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $ReportPath -Encoding utf8
    Write-Host "PostgreSQL restore drill report written to $ReportPath"
}
finally {
    [Environment]::SetEnvironmentVariable("ConnectionStrings__Aegis", $previousConnection)
    if (Test-Path -LiteralPath $dumpPath) {
        Remove-Item -LiteralPath $dumpPath -Force
    }

    & docker rm --force $sourceContainer $targetContainer *> $null
}
