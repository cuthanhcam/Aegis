# Getting Started  Aegis Development Setup

---

## Quick Start (5 minutes)

### Prerequisites

- **.NET 8 SDK** ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Visual Studio 2022** or **VS Code**
- **PostgreSQL 14+** (or use Docker)

---

### Step 1: Clone & Navigate

```bash
cd D:\Workspace\Aegis
git clone <repo-url>
cd Aegis
```

---

### Step 2: Start PostgreSQL

**Option A: Using Docker**

```bash
docker run --name aegis-postgres \
  -e POSTGRES_USER=aegis \
  -e POSTGRES_PASSWORD=aegis123 \
  -e POSTGRES_DB=aegis_dev \
  -p 5432:5432 \
  -d postgres:15
```

**Option B: Local PostgreSQL**

Create database:
```sql
CREATE DATABASE aegis_dev;
```

---

### Step 3: Configure Connection String

Edit `src/Aegis.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=aegis_dev;Username=aegis;Password=aegis123"
  }
}
```

---

### Step 4: Apply Migrations

```bash
cd src/Aegis.Api

# Build migrations
dotnet ef migrations add Initial -p ../Aegis.Infrastructure

# Apply to database
dotnet ef database update
```

---

### Step 5: Run the API

```bash
cd src/Aegis.Api
dotnet run
```

**Expected output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

---

### Step 6: Test the API

```bash
# Open browser or use curl
curl -X POST http://localhost:5000/api/v1/check \
  -H "X-Tenant-Id: tenant-test" \
  -H "Content-Type: application/json" \
  -d '{
    "subject": "user:alice",
    "relation": "viewer",
    "object": "document:test"
  }'

# Response
{
  "allowed": false,
  "decision": "DENY",
  "reasonCode": "DENY_NOT_FOUND"
}
```

---

## Project Structure

```
D:\Workspace\Aegis\
 src/
    Aegis.SharedKernel/       Primitives (Entity, ValueObject, AggregateRoot)
    Aegis.Contracts/          DTOs and API contracts
    Aegis.Domain/             Entities (Relationship, Store, AuthorizationModel)
    Aegis.Authorization/      ReBAC + RBAC engine (NO EF, NO HTTP)
    Aegis.Application/        Use cases, orchestration
    Aegis.Infrastructure/     DbContext, Repository implementations
    Aegis.Api/                HTTP controllers, middleware

 tests/
    Aegis.UnitTests/          Domain, Authorization logic tests
    Aegis.IntegrationTests/   API endpoint, persistence tests

 docs/
    ../product/product-overview.md       What is Aegis
    ../concepts/core-concepts-tuple-model.md   Tuple model deep dive
    ../reference/api-reference.md          Endpoint documentation
    architecture/             Technical architecture

 README.md
```

---

## Development Workflow

### 1. Running Tests

**Unit tests only:**
```bash
dotnet test tests/Aegis.UnitTests
```

**Integration tests:**
```bash
dotnet test tests/Aegis.IntegrationTests
```

**All tests:**
```bash
dotnet test
```

**With coverage:**
```bash
dotnet test /p:CollectCoverage=true
```

---

### 2. Adding a New Feature

**Example: Add "approver" relation support**

1. **Domain Layer**  Add validation to `RelationName` ValueObject
   ```csharp
   // src/Aegis.Domain/ValueObjects/RelationName.cs
   public static bool IsValid(string relation) => relation switch {
       "owner" or "editor" or "viewer" or "approver" => true,
       _ => false
   };
   ```

2. **Application Layer**  Add use case
   ```csharp
   // src/Aegis.Application/Features/Relationships/Commands
   public class CreateApprovalRelationshipCommand : IRequest<CreateApprovalResponse>
   {
       public string Subject { get; set; }
       public string Object { get; set; }
       // ...
   }
   ```

3. **Infrastructure**  No changes (generic repository handles it)

4. **API**  Add controller endpoint
   ```csharp
   // src/Aegis.Api/Controllers/RelationshipsController.cs
   [HttpPost("approvals")]
   public async Task<IActionResult> CreateApproval([FromBody] CreateApprovalRequest req)
   {
       // ...
   }
   ```

5. **Test**  Add unit + integration tests
   ```csharp
   // tests/Aegis.UnitTests/RelationshipTests.cs
   [Fact]
   public void Approver_Relation_Should_Be_Valid()
   {
       var valid = RelationName.TryCreate("approver", out _);
       Assert.True(valid);
   }
   ```

---

### 3. Code Style & Format

**Format code:**
```bash
# Using Roslyn (built-in)
dotnet format
```

**Lint with StyleCop (recommended):**
```bash
# Installed via NuGet
dotnet build --format errors-as-warnings
```

---

### 4. Database Migrations

**Create migration:**
```bash
cd src/Aegis.Infrastructure
dotnet ef migrations add <MigrationName> -p ../Aegis.Api --startup-project ../Aegis.Api
```

**Review generated migration** (in `Migrations/` folder)

**Apply migration:**
```bash
dotnet ef database update -p ../Aegis.Api
```

**Rollback:**
```bash
dotnet ef database update <PreviousMigrationName>
```

---

## Common Tasks

### Task 1: Create a New Tenant

```bash
curl -X POST http://localhost:5000/api/v1/tenants \
  -H "Content-Type: application/json" \
  -d '{
    "name": "acme-corp"
  }'
```

---

### Task 2: Set Up Initial Authorization Store

```bash
curl -X POST http://localhost:5000/api/v1/stores \
  -H "X-Tenant-Id: tenant-123" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "document-service-store"
  }'
```

---

### Task 3: Create Sample Relationships

```bash
# Create relationships
curl -X POST http://localhost:5000/api/v1/relationships \
  -H "X-Tenant-Id: tenant-123" \
  -d '{
    "subject": "user:alice",
    "relation": "owner",
    "object": "document:report-2024"
  }'

curl -X POST http://localhost:5000/api/v1/relationships \
  -H "X-Tenant-Id: tenant-123" \
  -d '{
    "subject": "user:bob",
    "relation": "editor",
    "object": "document:report-2024"
  }'
```

---

### Task 4: Run Permission Check

```bash
curl -X POST http://localhost:5000/api/v1/check \
  -H "X-Tenant-Id: tenant-123" \
  -d '{
    "subject": "user:alice",
    "relation": "owner",
    "object": "document:report-2024"
  }'

# Response
{
  "allowed": true,
  "decision": "ALLOW",
  "reasonCode": "ALLOW_REBAC_DIRECT"
}
```

---

### Task 5: Debug with Explain API

```bash
curl -X POST http://localhost:5000/api/v1/explain \
  -H "X-Tenant-Id: tenant-123" \
  -d '{
    "subject": "user:charlie",
    "relation": "viewer",
    "object": "document:report-2024"
  }'

# Response with full trace
{
  "allowed": false,
  "trace": [
    { "step": "DENY_POLICY", "result": "NOT_MATCHED" },
    { "step": "REBAC_DIRECT", "result": "NOT_MATCHED" },
    { "step": "RBAC", "result": "NOT_MATCHED" },
    { "step": "FINAL", "result": "DENY_NOT_FOUND" }
  ]
}
```

---

## IDEs & Tools

### Visual Studio 2022

1. Open `Aegis.sln`
2. Right-click solution  **Manage NuGet Packages**  Restore
3. **Build**  **Build Solution** (Ctrl+Shift+B)
4. **Debug**  **Start Debugging** (F5)

### VS Code

1. Install extensions:
   - C# Dev Kit
   - REST Client
   - SQLTools (for database exploration)

2. Open folder:
   ```bash
   code D:\Workspace\Aegis
   ```

3. Run tasks from Command Palette:
   - `Tasks: Run Task`  `.NET: Build`
   - Select `Aegis.Api`

4. Test API using `.http` files:
   ```http
   @tenantId = tenant-123
   @apiUrl = http://localhost:5000/api/v1

   ### Check permission
   POST {{apiUrl}}/check
   X-Tenant-Id: {{tenantId}}
   Content-Type: application/json

   {
     "subject": "user:alice",
     "relation": "owner",
     "object": "document:report"
   }
   ```

---

## Useful Scripts

### Run All Tests
```bash
#!/bin/bash
dotnet test --configuration Release --no-build --verbosity normal
```

### Build & Test
```bash
#!/bin/bash
dotnet build
dotnet test
```

### Reset Database
```bash
#!/bin/bash
cd src/Aegis.Api
dotnet ef database drop --force
dotnet ef database update
```

---

## Debugging Tips

### 1. Enable Detailed Logging

Edit `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore": "Information",
      "Aegis.Authorization": "Debug"
    }
  }
}
```

### 2. Use Explain API

Always use `/explain` instead of guessing:

```bash
curl -X POST http://localhost:5000/api/v1/explain \
  -H "X-Tenant-Id: your-tenant" \
  -d '{ "subject": "...", "relation": "...", "object": "..." }'
```

### 3. Check Audit Logs

```bash
curl http://localhost:5000/api/v1/audit-logs \
  -H "X-Tenant-Id: your-tenant"
```

### 4. Inspect Database Directly

Using SQLTools in VS Code:
```sql
SELECT * FROM relationships WHERE tenant_id = 'tenant-123';
SELECT * FROM audit_logs ORDER BY created_at DESC LIMIT 10;
```

---

## Common Issues & Fixes

### Issue: "Connection refused" on Port 5000

**Solution:**
```bash
# Check if port is in use
netstat -ano | findstr :5000

# If occupied, either:
# 1. Kill the process
# 2. Change port in appsettings.Development.json:
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5001"
      }
    }
  }
}
```

---

### Issue: "Database connection failed"

**Solution:**
1. Check PostgreSQL is running
2. Verify connection string in `appsettings.Development.json`
3. Run migrations: `dotnet ef database update`

---

### Issue: "EF Core migration conflicts"

**Solution:**
```bash
# Remove last migration
dotnet ef migrations remove -p ../Aegis.Infrastructure

# Recreate
dotnet ef migrations add <NewName> -p ../Aegis.Infrastructure
```

---

## Next Steps

-  Read [Core Concepts](../concepts/core-concepts-tuple-model.md)
-  Review [API Reference](../reference/api-reference.md)
-  Explore [Architecture Guide](architecture/project-structure.md)
-  Deploy to [Production](deployment-operations-guide.md)

---

**Happy coding! Questions?** Check the project GitHub Discussions or Issues.
