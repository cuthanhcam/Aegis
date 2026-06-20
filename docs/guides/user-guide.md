# Aegis User Guide

This guide explains how to use Aegis as an authorization platform. It is written for application developers, platform teams, and administrators who need to model, test, and operate authorization.

## Mental Model

Aegis answers authorization questions:

```text
Can <subject> perform <relation> on <object>?
```

Examples:

```text
Can user:anne view document:roadmap?
Can user:agent1 view ticket:INC-1001?
Can user:finance analyze account:acme?
```

Aegis stores three main kinds of information:

- Authorization models define object types and relations.
- Relationship tuples define concrete access facts.
- RBAC data defines roles, permissions, users, and assignments.

At runtime, applications call Aegis check APIs and receive allow or deny decisions.

## Main Workflow

### 1. Create or Select a Store

A store is an isolated authorization workspace. Common store boundaries:

- One store per application.
- One store per product domain.
- One store per tenant.
- One store per environment.

Example stores:

```text
store-docs-default
store-support-default
store-billing-default
store-lab-tenant-dev
store-analytics-tenant-dev
```

In the dashboard, choose the active store from the store selector before using model, relationship, graph, or access-management screens.

### 2. Define an Authorization Model

An authorization model defines which object types exist and which relations are valid.

Example document model:

```text
type user
type team
  define member: [user]
type document
  define owner: [user]
  define editor: [user] or owner
  define viewer: [user] or editor or member from parent
  define parent: [team]
```

This means:

- A document can have owners.
- Editors include direct editors and owners.
- Viewers include direct viewers, editors, and users who are members of the document's parent team.

### 3. Write Relationships

Relationships are facts.

```text
user:anne editor document:roadmap
team:platform member user:bob
team:platform parent document:roadmap
```

For store-scoped APIs, write tuples under a store:

```http
POST /api/v1/stores/{storeId}/relationships
```

Example body:

```json
{
  "subject": "user:anne",
  "relation": "editor",
  "object": "document:roadmap",
  "effect": "allow"
}
```

### 4. Check Access

Use check when an application needs a runtime decision.

```http
POST /api/v1/stores/{storeId}/check
```

```json
{
  "user": "user:anne",
  "relation": "viewer",
  "object": "document:roadmap",
  "consistency": "fully_consistent"
}
```

Typical response:

```json
{
  "allowed": true,
  "decision": "ALLOW",
  "reasonCode": "ALLOW_REBAC_DIRECT"
}
```

### 5. Explain a Decision

Use explain when a user cannot access something, or when a permission was unexpectedly allowed.

```http
POST /api/v1/stores/{storeId}/explain
```

Use the same body as check. The response includes trace information showing which evaluation path matched or failed.

### 6. Explore the Graph

Graph endpoints help answer operational questions:

- Who can view this object?
- Which objects can this user view?
- What does the relationship tree look like?

Endpoints:

```text
POST /api/v1/stores/{storeId}/graph/list-users
POST /api/v1/stores/{storeId}/graph/list-objects
POST /api/v1/stores/{storeId}/graph/expand
```

### 7. Manage Roles and Permissions

Use RBAC administration for coarse-grained access or operational permissions.

Store-scoped access APIs:

```text
GET  /api/v1/stores/{storeId}/roles
POST /api/v1/stores/{storeId}/roles
GET  /api/v1/stores/{storeId}/permissions
POST /api/v1/stores/{storeId}/permissions
POST /api/v1/stores/{storeId}/roles/assign-permission
POST /api/v1/stores/{storeId}/users/{userId}/roles
GET  /api/v1/stores/{storeId}/users/{userId}/roles
```

Tenant-scoped users are managed separately:

```text
GET  /api/v1/tenants/{tenantId}/users
POST /api/v1/tenants/{tenantId}/users
```

## Dashboard Screens

### Overview

Use this screen to see active stores, high-level system status, and shortcuts into common workflows.

### Stores

Use this screen to create, inspect, and switch authorization stores.

### Models

Use this screen to create and validate authorization models. Future versions should add publish, rollback, diff, and assertion runs.

### Relationships

Use this screen to inspect and write relationship tuples. Use filters to narrow by subject, relation, object, or effect.

### Check and Explain

Use this screen to run access checks and view decision traces.

### Graph

Use this screen for list-users, list-objects, and expand queries. Demo stores include working presets:

- Support: `ticket:INC-1001`
- Docs: `document:roadmap`
- Billing: `account:acme`
- Lab: `project:aegis-lab`
- Analytics: `dashboard:quality`

### Access Management

Use this screen to manage store-level roles, permissions, and user role assignments.

### Audit

Use this screen to search authorization decisions and administrative history.

## Integration Patterns

### Service-to-Service Check

Your product service calls Aegis before allowing an operation:

```text
Product API receives request
Product API calls Aegis check
Aegis returns allow or deny
Product API continues or rejects
```

### Background Sync

Your system writes relationship tuples when source-of-truth data changes:

```text
User added to workspace
Application writes user:alice member workspace:acme
Future Aegis checks include that relationship
```

### Debugging Access Issues

Use this sequence:

1. Confirm active store.
2. Confirm authorization model has the relation and object type.
3. Confirm relationship tuple exists.
4. Run check.
5. Run explain.
6. Review audit event.

## Best Practices

- Use stable object ids, such as `document:roadmap` or `ticket:INC-1001`.
- Keep store boundaries clear.
- Prefer relationship tuples for fine-grained resource access.
- Use RBAC for broad operational permissions.
- Use explain traces during development and support workflows.
- Keep demo and production stores separate.
- Treat authorization models like code: review, test, publish, and roll back.

## Common Pitfalls

- Sending a `document:*` object to a support store that only defines `ticket` and `queue`.
- Checking a relation that is not defined in the active authorization model.
- Forgetting that store-scoped APIs validate tenant ownership.
- Mixing tenant-scoped user management with store-scoped role assignments.
- Assuming deny means "no tuple exists"; explicit deny and failed condition can also produce deny.

