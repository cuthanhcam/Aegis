# Aegis Product Overview

Aegis is a centralized authorization platform for modern applications. It helps teams model access, answer permission questions, debug decisions, and audit authorization behavior across services.

## The Problem

In many systems, authorization logic is scattered:

```text
Controller checks one rule
Database query checks another rule
Background job checks a third rule
Frontend hides a button with its own rule
```

Over time this creates drift:

- Different services answer the same access question differently.
- Permission changes are hard to review.
- Support teams cannot explain why access was denied.
- Auditors cannot easily reconstruct who had access to what.
- Product teams avoid fine-grained permissions because they are too expensive to maintain.

## The Aegis Approach

Aegis gives applications one place to ask authorization questions:

```text
Can user:anne view document:roadmap?
Can user:agent1 view ticket:INC-1001?
Can user:finance view account:acme?
```

Applications call Aegis at runtime. Aegis evaluates the request using authorization models, relationship tuples, role assignments, and optional context. It returns a deterministic allow or deny decision.

## Core Capabilities

### Stores

A store is an isolated authorization workspace. Use stores to separate applications, environments, product domains, or tenants.

Examples:

```text
store-docs-default
store-support-default
store-billing-default
```

### Authorization Models

Authorization models define object types and relations.

Example:

```text
type user
type document
  define owner: [user]
  define editor: [user] or owner
  define viewer: [user] or editor
```

This model says owners are editors, and editors are viewers.

### Relationships

Relationships are concrete authorization facts:

```text
user:anne editor document:roadmap
user:agent1 assignee ticket:INC-1001
user:finance analyst account:acme
```

### Checks

Checks answer access questions:

```http
POST /api/v1/stores/{storeId}/check
```

```json
{
  "user": "user:anne",
  "relation": "viewer",
  "object": "document:roadmap"
}
```

### Explain

Explain returns the decision trace so developers and support teams can understand why a request was allowed or denied.

```http
POST /api/v1/stores/{storeId}/explain
```

### Graph Queries

Graph APIs answer broader relationship questions:

- Which users can access this object?
- Which objects can this user access?
- What does the relationship tree look like?

```text
POST /api/v1/stores/{storeId}/graph/list-users
POST /api/v1/stores/{storeId}/graph/list-objects
POST /api/v1/stores/{storeId}/graph/expand
```

### RBAC Administration

Aegis includes role and permission management for coarse-grained access:

- Roles
- Permissions
- Users
- Role assignments
- Store-scoped role permissions

### Audit

Aegis records authorization decisions and administrative activity so teams can investigate access behavior over time.

## Evaluation Model

Aegis is designed around deterministic evaluation:

```text
1. Explicit deny
2. Relationship-based allow
3. Role-based allow
4. Conditional/context-aware checks
5. Default deny
```

The exact behavior depends on the active model, tuples, RBAC data, and request context.

## Example Use Cases

### Document Collaboration

Model document owners, editors, viewers, teams, and parent folders.

Example question:

```text
Can user:anne edit document:roadmap?
```

### Support Platform

Model support tickets, assigned agents, queues, and managers.

Example question:

```text
Can user:agent1 view ticket:INC-1001?
```

### Billing Console

Model account admins, analysts, and account viewers.

Example question:

```text
Can user:finance view account:acme?
```

### SaaS Administration

Model tenant-specific stores and administrative roles.

Example question:

```text
Can user:admin manage roles in store-docs-default?
```

## Who Uses Aegis?

### Application Developers

Use Aegis APIs to make permission checks and write relationship data.

### Platform Teams

Define store boundaries, standard authorization models, and integration patterns.

### Support Teams

Use explain traces and audit logs to investigate access issues.

### Security and Compliance Teams

Use audit records to understand access behavior and administrative changes.

## What Aegis Is Not

Aegis is focused on authorization.

It is not:

- A user identity provider.
- A replacement for your login system.
- A database encryption product.
- A business workflow engine.

It can integrate with identity providers and product systems, but its job is to decide access.

## Current Maturity

Aegis is under active development. It currently supports local development and realistic demos with:

- PostgreSQL and Redis.
- Development seed data.
- API and dashboard workflows.
- Unit and integration tests.
- Store-scoped authorization APIs.

See [Roadmap](roadmap.md) for planned platform improvements.

## Next Steps

- Learn the [Core Concepts](../concepts/core-concepts-tuple-model.md).
- Follow the [User Guide](../guides/user-guide.md).
- Run the [Development Setup](../guides/getting-started-development.md).
- Try the [Demo Data](../reference/demo-data.md).
- Explore the [API Reference](../reference/api-reference.md).

