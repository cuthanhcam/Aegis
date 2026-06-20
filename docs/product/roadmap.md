# Aegis Roadmap

This roadmap describes the major platform capabilities planned for Aegis. It is intentionally high-level and suitable for public documentation. Detailed internal implementation checklists can live separately in planning notes.

## Current Focus

Aegis is moving from a capable authorization console toward a full authorization platform.

The current foundation includes:

- Store-scoped authorization APIs.
- Authorization models and validation.
- Relationship tuple management.
- Runtime check, explain, batch-check, and graph APIs.
- RBAC administration.
- Audit events.
- Metrics and health checks.
- React admin dashboard.

## Near-Term Priorities

### 1. Model Lifecycle

Planned capabilities:

- Draft, validated, published, archived, and deprecated model states.
- Publish and rollback workflows.
- Model version diff.
- Assertion runner.
- Assertion run history.
- Regression testing before publish.

Why it matters:

Authorization models should be treated like production code. Teams need to test policy changes before making them active.

### 2. Relationship Data Pipeline

Planned capabilities:

- Revision tokens.
- Idempotent writes.
- Bulk import.
- Bulk export.
- Cursor-based change stream.
- Dry-run validation.

Why it matters:

Real systems often need to sync thousands or millions of authorization facts from source systems.

### 3. Enterprise Access and API Keys

Planned capabilities:

- Organizations and memberships.
- Service accounts.
- API keys.
- Store-level admin permissions.
- SSO-ready auth abstractions.

Why it matters:

Aegis needs secure machine-to-machine integration and clear administrative boundaries.

### 4. Audit and Compliance

Planned capabilities:

- Richer audit schema.
- Admin activity logs.
- Audit export.
- Retention policies.
- Explain trace snapshots.
- Investigation-friendly queries.

Why it matters:

Authorization platforms must help teams answer who changed access, who checked access, and why a decision happened.

### 5. Webhooks and Events

Planned capabilities:

- Webhook subscriptions.
- Outbox-backed delivery.
- Delivery logs.
- Retry and dead-letter handling.
- HMAC signatures.

Why it matters:

External systems should be able to react to model, relationship, and security events.

### 6. Quotas, Rate Limits, and Usage

Planned capabilities:

- Tenant-level and API-key-level rate limits.
- Usage metrics.
- Quota policies.
- Monthly usage summaries.

Why it matters:

Multi-tenant platforms need protection from noisy clients and visibility into usage.

### 7. Observability and Operations

Planned capabilities:

- OpenTelemetry tracing.
- Detailed authorization metrics.
- Better readiness checks.
- Startup configuration validation.
- Production-like deployment templates.

Why it matters:

Operators need to detect slow checks, failing dependencies, and configuration problems quickly.

### 8. API and Documentation Hardening

Planned capabilities:

- Consistent cursor pagination.
- Consistent error envelope.
- Strong request validation.
- Complete OpenAPI examples.
- SDK-generation readiness.
- Public documentation site structure.

Why it matters:

Great APIs are predictable, documented, and easy to integrate.

## Roadmap Philosophy

Aegis should remain:

- Deterministic: same input and state should produce the same decision.
- Explainable: decisions should be debuggable.
- Tenant-safe: stores and tenants must remain isolated.
- API-first: every dashboard workflow should map cleanly to backend APIs.
- Operations-friendly: production behavior should be observable and recoverable.

