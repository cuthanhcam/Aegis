# Architecture Overview

Aegis is built around a simple principle: authorization decisions should be centralized, deterministic, and explainable.

## High-Level Flow

```text
Application
  -> Aegis API
  -> Authorization service
  -> Authorization engine
  -> Relationship/model/RBAC data
  -> Decision + optional trace
```

## Main Components

### API

The API exposes store, model, relationship, check, graph, access-management, audit, health, and metrics endpoints.

### Authorization Engine

The engine evaluates allow and deny decisions. It understands:

- Relationship tuples.
- Authorization models.
- Role permissions.
- Request context.
- Explain traces.

### Persistence

PostgreSQL stores durable authorization data. Redis can be used for cache-backed runtime behavior.

### Dashboard

The dashboard provides a visual interface for managing stores, models, relationships, access management, graph queries, checks, explain traces, and audit data.

## Design Principles

- Store-scoped APIs are preferred for new integrations.
- Tenant boundaries must be enforced on every store-scoped request.
- Explicit deny has priority over allow.
- Explainability is part of the product contract.
- API behavior should be deterministic and observable.

