# API Integration Guide

This guide explains how applications integrate with Aegis.

## Basic Flow

An application calls Aegis before performing a protected action:

```text
User requests action
Application asks Aegis for a check
Aegis returns allow or deny
Application continues or rejects the action
```

## Authenticate

Use `/api/v1/auth/login` in local development to obtain an access token.

Production deployments should use the configured authentication mode for your environment.

## Choose a Store

All new integrations should prefer store-scoped APIs:

```text
/api/v1/stores/{storeId}/...
```

The store isolates authorization models, relationships, roles, permissions, and graph queries.

## Run a Check

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

The response uses the Aegis envelope:

```json
{
  "success": true,
  "data": {
    "allowed": true,
    "decision": "ALLOW",
    "reasonCode": "ALLOW_REBAC_DIRECT"
  },
  "error": null
}
```

## Debug With Explain

```http
POST /api/v1/stores/{storeId}/explain
```

Use the same request body as check. Explain returns the decision plus trace details.

## Write Relationships

Write relationship tuples when source-of-truth data changes.

```http
POST /api/v1/stores/{storeId}/relationships
```

```json
{
  "subject": "user:anne",
  "relation": "editor",
  "object": "document:roadmap",
  "effect": "allow"
}
```

Delete a relationship:

```http
DELETE /api/v1/stores/{storeId}/relationships
```

```json
{
  "subject": "user:anne",
  "relation": "editor",
  "object": "document:roadmap"
}
```

## Query the Graph

Use graph endpoints for discovery and support tools:

```text
POST /api/v1/stores/{storeId}/graph/list-users
POST /api/v1/stores/{storeId}/graph/list-objects
POST /api/v1/stores/{storeId}/graph/expand
```

## Use Compatibility Endpoints

Aegis exposes OpenFGA-style compatibility endpoints for familiar request shapes:

```text
POST /api/v1/stores/{storeId}/check/compat
POST /api/v1/stores/{storeId}/batch-check/compat
POST /api/v1/stores/{storeId}/graph/list-users/compat
POST /api/v1/stores/{storeId}/graph/list-objects/compat
POST /api/v1/stores/{storeId}/graph/expand/compat
```

Compatibility endpoints return direct compatibility payloads instead of the Aegis envelope.

## Recommended Integration Pattern

- Keep product identity in your identity provider.
- Keep product data in your product database.
- Write relationship tuples to Aegis when product access changes.
- Call Aegis checks at authorization boundaries.
- Use explain and audit for support workflows.
- Prefer stable ids such as `document:roadmap`, `ticket:INC-1001`, or `account:acme`.

