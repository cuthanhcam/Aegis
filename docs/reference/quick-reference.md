# Quick Reference Aegis Cheat Sheet

---

## Tuple Format

```
(subject, relation, object)

Format rules:
   subject: <type>:<id>          (e.g., user:alice, team:dev)
   relation: <name>              (e.g., owner, editor, viewer)
   object: <type>:<id>           (e.g., document:report, repo:code)
```

---

## Check Permission (Most Common)

```bash
curl -X POST http://localhost:5000/api/v1/check \
  -H "X-Tenant-Id: tenant-123" \
  -H "Content-Type: application/json" \
  -d '{
    "subject": "user:alice",
    "relation": "editor",
    "object": "document:report"
  }'
```

**Response:**

```json
{
    "allowed": true,
    "decision": "ALLOW",
    "reasonCode": "ALLOW_REBAC_DIRECT"
}
```

---

## Debug Permission Decision

```bash
curl -X POST http://localhost:5000/api/v1/explain \
  -H "X-Tenant-Id: tenant-123" \
  -d '{
    "subject": "user:bob",
    "relation": "editor",
    "object": "document:confidential"
  }'
```

**Response:**

```json
{
    "allowed": false,
    "trace": [
        {
            "step": "CHECK_DENY",
            "result": "MATCHED",
            "details": "Explicit deny found"
        },
        { "step": "FINAL", "result": "DENY" }
    ]
}
```

---

## Create Relationship (Allow)

```bash
curl -X POST http://localhost:5000/api/v1/relationships \
  -H "X-Tenant-Id: tenant-123" \
  -d '{
    "subject": "user:alice",
    "relation": "owner",
    "object": "document:report",
    "effect": "allow"
  }'
```

---

## Create Explicit Deny

```bash
curl -X POST http://localhost:5000/api/v1/relationships \
  -H "X-Tenant-Id: tenant-123" \
  -d '{
    "subject": "team:contractors",
    "relation": "editor",
    "object": "document:confidential",
    "effect": "deny"
  }'
```

Note: Deny **always overrides** allow.

---

## List Relationships

```bash
# All relationships for a tenant
curl "http://localhost:5000/api/v1/relationships" \
  -H "X-Tenant-Id: tenant-123"

# Filter by subject
curl "http://localhost:5000/api/v1/relationships?subject=user:alice" \
  -H "X-Tenant-Id: tenant-123"

# Filter by object
curl "http://localhost:5000/api/v1/relationships?object=document:report" \
  -H "X-Tenant-Id: tenant-123"

# Filter by relation
curl "http://localhost:5000/api/v1/relationships?relation=owner" \
  -H "X-Tenant-Id: tenant-123"
```

---

## Delete Relationship

```bash
curl -X DELETE "http://localhost:5000/api/v1/relationships/{id}" \
  -H "X-Tenant-Id: tenant-123"

# Or batch delete by filter
curl -X DELETE "http://localhost:5000/api/v1/relationships?subject=user:alice" \
  -H "X-Tenant-Id: tenant-123"
```

---

## RBAC: Create Role

```bash
curl -X POST http://localhost:5000/api/v1/roles \
  -H "X-Tenant-Id: tenant-123" \
  -d '{
    "name": "document-editor",
    "description": "Can edit documents"
  }'
```

---

## RBAC: Assign Role to User

```bash
curl -X POST http://localhost:5000/api/v1/user-roles \
  -H "X-Tenant-Id: tenant-123" \
  -d '{
    "userId": "user-uuid-1",
    "roleId": "role-uuid-1"
  }'
```

---

## RBAC: Add Permission to Role

```bash
# First create permission (system-level, no tenant)
curl -X POST http://localhost:5000/api/v1/permissions \
  -d '{"name": "document:edit", "description": "Can edit documents"}'

# Then assign to role
curl -X POST "http://localhost:5000/api/v1/roles/{roleId}/permissions/{permissionId}" \
  -H "X-Tenant-Id: tenant-123"
```

---

## View Audit Logs

```bash
curl "http://localhost:5000/api/v1/audit-logs?limit=100" \
  -H "X-Tenant-Id: tenant-123"
```

**Response:**

```json
{
    "data": [
        {
            "timestamp": "2026-04-07T10:30:00Z",
            "action": "RELATIONSHIP_CREATED",
            "subject": "user:alice",
            "relation": "editor",
            "object": "document:report",
            "initiatedBy": "admin:system"
        }
    ]
}
```

---

## Create User

```bash
curl -X POST http://localhost:5000/api/v1/users \
  -H "X-Tenant-Id: tenant-123" \
  -d '{
    "username": "alice",
    "email": "alice@company.com",
    "password": "secure-password"
  }'
```

---

## Create Store (Authorization Context)

```bash
curl -X POST http://localhost:5000/api/v1/stores \
  -H "X-Tenant-Id: tenant-123" \
  -d '{"name": "document-service-store"}'
```

---

## Authorization Decision Codes

| Code                 | Meaning                     |
| -------------------- | --------------------------- |
| `ALLOW_REBAC_DIRECT` | Direct tuple match (ReBAC)  |
| `ALLOW_RBAC_ROLE`    | User role has permission    |
| `DENY_EXPLICIT`      | Explicit deny tuple matched |
| `DENY_NOT_FOUND`     | No allow rule matched       |
| `DENY_INVALID_INPUT` | Malformed request           |

---

## Common Tuple Patterns

### Resource Ownership

```
(user:alice, owner, document:report)     Alice owns the report
(team:dev, owner, repo:code)             Dev team owns the codebase
```

### Resource Sharing

```
(user:bob, editor, document:report)      Bob can edit the report
(user:charlie, viewer, document:report)  Charlie can view the report
```

### Team Membership

```
(user:alice, member, team:engineering)   Alice is in engineering team
(user:bob, admin, team:engineering)      Bob is admin of team
```

### Hierarchies

```
(team:sub, parent, team:main)            Sub-team is part of main team
(user:alice, member, team:sub)           Alice is in sub-team
                                         Can access main-team resources (future feature)
```

---

## Headers & Context

**Required tenant header:**

```
X-Tenant-Id: tenant-123
```

**Optional authentication header:**

```
Authorization: Bearer <jwt-token>
```

**Response headers:**

```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 2026-04-07T11:30:00Z
```

---

## Development Commands

```bash
# Start API (from src/Aegis.Api/)
dotnet run

# Run tests
dotnet test

# Run unit tests only
dotnet test tests/Aegis.UnitTests

# Run integration tests only
dotnet test tests/Aegis.IntegrationTests

# Format code
dotnet format

# Apply migrations
dotnet ef database update -p ../Aegis.Infrastructure

# Create migration
dotnet ef migrations add MigrationName -p ../Aegis.Infrastructure
```

---

## Database Queries (SQL)

### List all relationships

```sql
SELECT * FROM relationships
WHERE tenant_id = 'tenant-123'
ORDER BY created_at DESC;
```

### Find specific relationship

```sql
SELECT * FROM relationships
WHERE tenant_id = 'tenant-123'
  AND subject = 'user:alice'
  AND relation = 'editor'
  AND object = 'document:report';
```

### Count relationships per tenant

```sql
SELECT tenant_id, COUNT(*) as count
FROM relationships
GROUP BY tenant_id;
```

### List audit events for a subject

```sql
SELECT * FROM audit_logs
WHERE tenant_id = 'tenant-123'
  AND subject = 'user:alice'
ORDER BY timestamp DESC;
```

---

## Environment Setup

```bash
# Database connection string
export DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=aegis_dev;Username=postgres;Password=password"

# JWT configuration
export JWT_SECRET="<generate-256-char-random-value>"
export JWT_ISSUER="aegis.company.com"
export JWT_AUDIENCE="aegis-clients"

# API port
export API_PORT=5000

# Environment
export ASPNETCORE_ENVIRONMENT=Development
```

---

## Docker Quick Start

```bash
# Start PostgreSQL
docker run -d \
  --name aegis-postgres \
  -e POSTGRES_USER=aegis \
  -e POSTGRES_PASSWORD=aegis123 \
  -e POSTGRES_DB=aegis_dev \
  -p 5432:5432 \
  postgres:15

# Build Aegis image
docker build -t aegis:latest .

# Run Aegis API
docker run -d \
  --name aegis-api \
  -e DB_CONNECTION_STRING="Host=aegis-postgres;Database=aegis_dev;Username=aegis;Password=aegis123" \
  -p 5000:5000 \
  --link aegis-postgres \
  aegis:latest
```

---

## Kubernetes Quick Deploy

```bash
# Create secret
kubectl create secret generic aegis-secrets \
  --from-literal=db-connection-string='...' \
  --from-literal=jwt-secret='...'

# Install with Helm
helm install aegis ./helm \
  --set image.tag=latest

# Check status
kubectl get pods -l app=aegis

# View logs
kubectl logs -f deployment/aegis
```

---

## Troubleshooting

### API not responding

```bash
# Check if API is running
curl http://localhost:5000/health

# Check logs
dotnet run --verbosity debug
```

### Database connection failed

```bash
# Verify PostgreSQL is running
psql -h localhost -U postgres -c "SELECT version();"

# Check connection string format
# Should be: "Host=localhost;Port=5432;Database=aegis_dev;Username=user;Password=pwd"
```

### Permission check unexpected result

```bash
# Always use /explain to debug
curl -X POST http://localhost:5000/api/v1/explain \
  -H "X-Tenant-Id: tenant-123" \
  -d '{"subject":"user:x","relation":"y","object":"z"}'

# Check audit logs for changes
curl http://localhost:5000/api/v1/audit-logs -H "X-Tenant-Id: tenant-123"
```

### JWT token validation failed

```bash
# Verify token
jwt decode <token>

# Check issuer & audience match config
# Check expiration: exp claim should be in future
```

---

## Common Mistakes

- **Mistake:** Using wrong tenant header
- **Fix:** Always include `X-Tenant-Id` header

- **Mistake:** Expecting ALLOW when relationship does not exist
- **Fix:** Default is DENY (principle of least privilege)

- **Mistake:** Subject/object format: `user: alice` (with space or no colon)
- **Fix:** Use format `user:alice` (no spaces, colon required)

- **Mistake:** Creating the same relationship twice and assuming it is not idempotent
- **Fix:** The operation is idempotent; repeated calls return a consistent result

- **Mistake:** Comparing RBAC and ReBAC as equivalent models
- **Fix:** ReBAC is primary; RBAC is fallback; both are evaluated

---

## Links & Resources

- [Product Overview](../product/product-overview.md): High-level overview
- [Core Concepts](../concepts/core-concepts-tuple-model.md): Deep dive
- [API Reference](api-reference.md): Complete endpoint docs
- [Architecture](../architecture/README.md): System design
- [Getting Started](../guides/getting-started-development.md): Setup guide
- [Deployment](../guides/deployment-operations-guide.md): Production deployment

---

**Last Updated:** April 2026 | Version: 1.0
