# Deployment & Operations Guide Aegis Authorization Platform

---

## Overview

This guide covers deploying Aegis to production, managing infrastructure, and operating it at scale.

---

## 1. Pre-Deployment Checklist

- [ ] Run full test suite: `dotnet test --configuration Release`
- [ ] All unit tests pass (Coverage >80%)
- [ ] All integration tests pass
- [ ] Build succeeds: `dotnet build -c Release`
- [ ] Database migrations reviewed and tested
- [ ] Environment variables documented
- [ ] SSL/TLS certificates provisioned
- [ ] Backup strategy defined
- [ ] Monitoring & alerting configured
- [ ] Disaster recovery plan in place

---

## 2. Environment Configuration

### Required Environment Variables

Create `.env` or configure via platform (Docker, K8s, Azure):

```env
# Database
DB_CONNECTION_STRING=Host=postgres.prod.internal;Port=5432;Database=aegis;Username=postgres;Password=***

# API
API_PORT=5000
API_ENVIRONMENT=Production

# Security
JWT_SECRET=<generate-strong-random-value>
JWT_ISSUER=aegis.company.com
JWT_AUDIENCE=aegis-clients

# Logging
LOG_LEVEL=Information
LOG_OUTPUT=json

# Monitoring
APPLICATIONINSIGHTS_CONNECTION_STRING=...

# Feature Flags
FEATURE_EXPLAIN_API_ENABLED=true
FEATURE_AUDIT_LOGS_ENABLED=true
FEATURE_RBAC_ENABLED=true
```

### appsettings.Production.json

```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.EntityFrameworkCore": "Warning"
        },
        "ApplicationInsights": {
            "LogLevel": {
                "Default": "Information"
            }
        }
    },
    "Kestrel": {
        "Endpoints": {
            "Http": {
                "Url": "http://0.0.0.0:5000"
            }
        }
    },
    "ConnectionStrings": {
        "DefaultConnection": "${DB_CONNECTION_STRING}"
    },
    "Jwt": {
        "Secret": "${JWT_SECRET}",
        "Issuer": "${JWT_ISSUER}",
        "Audience": "${JWT_AUDIENCE}",
        "ExpirationMinutes": 60
    }
}
```

---

## 3. Database Setup

### PostgreSQL Requirements

- **Version:** PostgreSQL 14+
- **Extensions:** uuid-ossp (for UUID generation)
- **Resources:**
    - Development: 1vCPU, 2GB RAM
    - Production: 4+ vCPU, 16+ GB RAM, 100+ GB storage

### Create PostgreSQL Instance

**Option A: AWS RDS**

```bash
aws rds create-db-instance \
  --db-instance-identifier aegis-prod \
  --db-instance-class db.t3.medium \
  --engine postgres \
  --engine-version 15.3 \
  --master-username postgres \
  --master-user-password <secure-password> \
  --allocated-storage 100 \
  --backup-retention-period 30 \
  --multi-az \
  --publicly-accessible false
```

**Option B: Azure Database for PostgreSQL**

```bash
az postgres server create \
  --name aegis-prod \
  --resource-group aegis-rg \
  --location eastus \
  --admin-user dbadmin \
  --admin-password <secure-password> \
  --sku-name B_Gen5_2 \
  --storage-size 51200
```

**Option C: Docker in Production**

```bash
docker run -d \
  --name aegis-postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=<secure-password> \
  -e POSTGRES_DB=aegis \
  -v aegis-postgres-data:/var/lib/postgresql/data \
  -p 5432:5432 \
  --restart unless-stopped \
  postgres:15 \
  -c ssl=on \
  -c ssl_cert_file=/etc/ssl/certs/server.crt \
  -c ssl_key_file=/etc/ssl/private/server.key
```

### Apply Migrations

```bash
# In CI/CD pipeline
dotnet ef database update \
  --project src/Aegis.Infrastructure \
  --startup-project src/Aegis.Api \
  --configuration Release
```

### Database Backup Strategy

**Daily automated backups:**

```bash
#!/bin/bash
# backup-aegis-db.sh

BACKUP_DIR="/backups/aegis"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
DB_HOST="postgres.prod.internal"
DB_NAME="aegis"

pg_dump -h $DB_HOST -U postgres -F c -b $DB_NAME > $BACKUP_DIR/aegis_$TIMESTAMP.dump

# Keep only last 30 days of backups
find $BACKUP_DIR -name "aegis_*.dump" -mtime +30 -delete

# Upload to S3
aws s3 cp $BACKUP_DIR/aegis_$TIMESTAMP.dump s3://aegis-backups/
```

Schedule with cron:

```bash
0 2 * * * /scripts/backup-aegis-db.sh
```

---

## 4. Docker Deployment

### Build Docker Image

Create `Dockerfile` in repository root:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy and restore
COPY ["src/", "src/"]
RUN dotnet restore "src/Aegis.Api/Aegis.Api.csproj"

# Build
RUN dotnet build "src/Aegis.Api/Aegis.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "src/Aegis.Api/Aegis.Api.csproj" -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD dotnet /app/HealthCheck.dll

EXPOSE 5000
ENTRYPOINT ["dotnet", "Aegis.Api.dll"]
```

### Build & Push

```bash
# Build
docker build -t aegis:latest .
docker tag aegis:latest myregistry.azurecr.io/aegis:latest

# Push to container registry
docker push myregistry.azurecr.io/aegis:latest
```

### Run in Docker

```bash
docker run -d \
  --name aegis-api \
  -e DB_CONNECTION_STRING="Host=postgres;Port=5432;..." \
  -e JWT_SECRET="<secret>" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -p 5000:5000 \
  --restart unless-stopped \
  aegis:latest
```

---

## 5. Kubernetes Deployment

### Helm Chart

Create `helm/values.yaml`:

```yaml
image:
    repository: myregistry.azurecr.io/aegis
    tag: latest
    pullPolicy: IfNotPresent

replicaCount: 3

resources:
    requests:
        memory: "256Mi"
        cpu: "100m"
    limits:
        memory: "512Mi"
        cpu: "500m"

service:
    type: LoadBalancer
    port: 80
    targetPort: 5000

ingress:
    enabled: true
    className: nginx
    hosts:
        - host: aegis.company.com
          paths:
              - path: /
                pathType: Prefix

env:
    - name: ASPNETCORE_ENVIRONMENT
      value: Production
    - name: ConnectionStrings__DefaultConnection
      valueFrom:
          secretKeyRef:
              name: aegis-secrets
              key: db-connection-string

secrets:
    - name: aegis-secrets
      data:
          db-connection-string: <base64-encoded>
          jwt-secret: <base64-encoded>
```

### Deploy

```bash
# Create secrets
kubectl create secret generic aegis-secrets \
  --from-literal=db-connection-string='...' \
  --from-literal=jwt-secret='...'

# Install Helm chart
helm install aegis ./helm

# Verify
kubectl get pods -l app=aegis
kubectl logs -f deployment/aegis
```

---

## 6. Monitoring & Observability

### Application Insights Integration

Add to `Program.cs`:

```csharp
builder.Services
    .AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    })
    .AddLogging(logging =>
    {
        logging.AddApplicationInsights();
        logging.SetMinimumLevel(LogLevel.Information);
    });
```

### Key Metrics to Monitor

```
 Authorization decision latency (P50, P95, P99)
 Relationship CRUD latency
 Database connection pool usage
 Request error rate
 JWT token validation failures
 Tenant context resolution errors
 Cache hit rate (if caching implemented)
 Audit log backlog
```

### Sample Queries (Application Insights KQL)

**Check authorization latency:**

```kusto
customMetrics
| where name == "AuthorizationCheckLatencyMs"
| summarize
    P50=percentile(value, 50),
    P95=percentile(value, 95),
    P99=percentile(value, 99),
    Avg=avg(value)
    by bin(timestamp, 5m)
```

**Monitor error rate:**

```kusto
requests
| where url contains "/api/v1"
| summarize
    TotalRequests=count(),
    FailedRequests=countif(success == false),
    ErrorRate=100.0*countif(success==false)/count()
    by url, bin(timestamp, 1h)
```

---

## 7. Security Hardening

### API Security Headers

Add middleware in `Program.cs`:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    await next();
});
```

### Database Security

```sql
-- Create read-only user for app
CREATE ROLE aegis_app WITH LOGIN PASSWORD 'app-password';
GRANT CONNECT ON DATABASE aegis TO aegis_app;
GRANT USAGE ON SCHEMA public TO aegis_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO aegis_app;

-- Restrict direct table access
REVOKE ALL ON relationships FROM public;
REVOKE ALL ON users FROM public;
```

### JWT Token Security

```json
{
    "Jwt": {
        "Secret": "<min-256-character-random-value>",
        "ExpirationMinutes": 60,
        "RefreshTokenExpirationDays": 7,
        "ValidateIssuer": true,
        "ValidateAudience": true,
        "ValidateLifetime": true,
        "ClockSkew": 0
    }
}
```

### Rate Limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1000,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

---

## 8. Scaling Considerations

### Horizontal Scaling

Aegis is **stateless** by design, so it scales horizontally:

```text
Load Balancer
 Aegis Pod 1
 Aegis Pod 2
 Aegis Pod 3
 Aegis Pod N

All pods share:
 PostgreSQL (single writer, read replicas optional)
 Redis Cache (optional, for performance)
 Audit Log Store (same DB as relationships)
```

### Performance Tuning

**Database indexes (critical):**

```sql
-- Already in migrations, but verify:
CREATE INDEX ix_relationships_tenant_subject
  ON relationships(tenant_id, subject);

CREATE INDEX ix_relationships_tenant_object
  ON relationships(tenant_id, object);

CREATE INDEX ix_relationships_tenant_relation
  ON relationships(tenant_id, relation);

-- Composite for permission checks
CREATE INDEX ix_relationships_tenant_subject_relation_object
  ON relationships(tenant_id, subject, relation, object);
```

**Connection pooling (appsettings.json):**

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Host=postgres;Database=aegis;Username=user;Password=pwd;Max Pool Size=100"
    }
}
```

**Caching (optional):**

```csharp
// Cache frequently checked relationships
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
```

---

## 9. Operations & Maintenance

### Daily Operations

**Morning health check:**

```bash
#!/bin/bash

# Check API health
HEALTH=$(curl -s http://aegis.prod.internal/health)
echo "API Health: $HEALTH"

# Check database connectivity
psql -h postgres.prod.internal -U postgres -c "SELECT version();"

# Review error logs from last 24h
kubectl logs -l app=aegis --since=24h --timestamps=true | grep ERROR
```

**Evening audit:**

```bash
# Export audit logs
curl http://aegis.prod.internal/api/v1/audit-logs?limit=10000 \
  -o audit_`date +%Y%m%d`.json

# Backup database
/scripts/backup-aegis-db.sh

# Check disk usage
df -h /var/lib/postgresql/data
```

### Common Admin Tasks

**Add new tenant:**

```bash
curl -X POST http://aegis.prod.internal/api/v1/admin/tenants \
  -H "Authorization: Bearer <admin-token>" \
  -d '{ "name": "new-customer" }'
```

**Create authorization store:**

```bash
curl -X POST http://aegis.prod.internal/api/v1/stores \
  -H "X-Tenant-Id: tenant-123" \
  -H "Authorization: Bearer <token>" \
  -d '{ "name": "microservice-authz" }'
```

**Export relationships (backup):**

```bash
curl http://aegis.prod.internal/api/v1/relationships?pageSize=10000 \
  -H "X-Tenant-Id: tenant-123" \
  -H "Authorization: Bearer <token>" \
  > relationships_backup_`date +%Y%m%d_%H%M%S`.json
```

---

## 10. Disaster Recovery

### Backup & Restore Procedure

**Backup:**

```bash
pg_dump -h postgres.prod.internal -U postgres aegis > aegis_backup_$(date +%Y%m%d_%H%M%S).sql
gzip aegis_backup_*.sql
aws s3 cp aegis_backup_*.sql.gz s3://aegis-backups/
```

**Restore:**

```bash
# 1. Create new DB
createdb aegis_restored

# 2. Restore from backup
psql aegis_restored < aegis_backup_2026-04-07_120000.sql

# 3. Verify data integrity
psql aegis_restored -c "SELECT COUNT(*) FROM relationships;"

# 4. Switch DNS (with downtime window)
# Update connection strings to point to restored DB
# Restart Aegis pods
```

### RTO & RPO Targets

| Scenario                    | RTO                           | RPO                  |
| --------------------------- | ----------------------------- | -------------------- |
| Database node failure       | 5 mins (failover to replica)  | 0 (sync replication) |
| Database corruption         | 30 mins (restore from backup) | 1 day                |
| Complete datacenter failure | 1 hour (failover region)      | 1 hour               |

---

## 11. Troubleshooting

### Issue: High Authorization Check Latency

**Investigation:**

```bash
# Check database slow query log
psql -h postgres -U postgres aegis -c "SELECT * FROM pg_stat_statements ORDER BY mean_exec_time DESC LIMIT 10;"

# Check connection pool exhaustion
SELECT count(*) FROM pg_stat_activity;
```

**Solutions:**

- Increase `Max Pool Size` in connection string
- Add database indexes (see Scaling section)
- Implement relationship caching in Redis
- Scale horizontally (add more replicas of Aegis)

---

### Issue: JWT Token Validation Failures

**Check logs:**

```bash
kubectl logs -f deployment/aegis | grep JWT
```

**Verify token:**

```bash
# Decode JWT at jwt.io or use:
jwt decode <token>
```

**Common causes:**

- Token expired (increase `ExpirationMinutes`)
- Invalid signature (verify `JWT_SECRET` matches issuer)
- Wrong issuer/audience (check `Jwt:Issuer` and `Jwt:Audience`)

---

### Issue: Out of Disk Space

**Diagnose:**

```bash
df -h
du -sh /var/lib/postgresql/data
```

**Clean up:**

```bash
# Archive old audit logs
psql -U postgres aegis -c "DELETE FROM audit_logs WHERE created_at < now() - interval '90 days';"

# Reindex (compact database)
psql -U postgres aegis -c "REINDEX DATABASE aegis;"

# Expand storage (cloud-specific)
# AWS: aws rds modify-db-instance --db-instance-identifier aegis-prod --allocated-storage 200
```

---

## 12. Upgrade Procedure

### Version Upgrade Steps

```bash
# 1. Test migrations on staging
dotnet ef database update --project src/Aegis.Infrastructure --startup-project src/Aegis.Api

# 2. Build new image
docker build -t aegis:v2.0.0 .

# 3. Blue-green deployment (zero downtime)
kubectl set image deployment/aegis aegis=myregistry/aegis:v2.0.0 --record

# 4. Monitor rollout
kubectl rollout status deployment/aegis

# 5. If issues, rollback
kubectl rollout undo deployment/aegis

# 6. Verify application functionality
curl http://aegis.prod.internal/api/v1/check \
  -X POST -H "X-Tenant-Id: test" \
  -d '{"subject":"test","relation":"test","object":"test"}'
```

---

## 13. Support & Runbooks

**Contact:**

- **On-call:** Use PagerDuty integration
- **Escalation:** Aegis Platform Team
- **Docs:** [Product Overview](../product/product-overview.md)

**Useful links:**

- [API Reference](../reference/api-reference.md)
- [Core Concepts](../concepts/core-concepts-tuple-model.md)
- [Getting Started](getting-started-development.md)

---

**Last Updated:** April 2026 | Version: 1.0
