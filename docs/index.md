# Aegis Documentation Index

This is the full documentation index for Aegis. Articles follow the [metadata and content contract](article-metadata-schema.md). Active implementation sequencing lives in the [product-readiness planning pack](../temp/README.md).

## Architecture

- [System Architecture](architecture/system-architecture.md)
- [Frontend Console Architecture](architecture/frontend-console.md)
- [Legacy Architecture Overview](architecture.md)
- [Backend Runtime Inventory](architecture/backend-runtime-inventory.md)
- [Store Deletion Consistency](architecture/store-deletion-consistency.md)
- [Store Constraint Reconciliation Runbook](operations/store-constraint-reconciliation.md)
- [PostgreSQL Backup and Restore Drill](operations/postgres-backup-restore.md)
- [Architecture Decision Records](decisions/README.md)

## Product

- [Product Overview](product/product-overview.md)
- [Roadmap](product/roadmap.md)

## Concepts

- [Core Concepts](concepts/README.md)
- [Tuple Model and Authorization](concepts/core-concepts-tuple-model.md)
- [Deterministic Authorization Decisions](concepts/deterministic-authorization.md)
- [Tenant and Store Isolation](concepts/tenant-store-isolation.md)

## Guides

- [User Guide](guides/user-guide.md)
- [Dashboard Guide](guides/dashboard-guide.md)
- [API Integration Guide](guides/api-integration.md)
- [Local Development Guide](guides/getting-started-development.md)
- [Engineering Governance](development/engineering-governance.md)

## Reference

- [API Reference](reference/api-reference.md)
- [Quick Reference](reference/quick-reference.md)
- [Demo Data Guide](reference/demo-data.md)

## Operations

- [Deployment Guide](operations/deployment.md)
- [Operating Aegis in Production](operations/production-readiness.md)

## Top Questions

- What is Aegis? See [Product Overview](product/product-overview.md).
- How does the tuple model work? See [Core Concepts](concepts/README.md).
- How does Aegis keep decisions repeatable? See [Deterministic Authorization Decisions](concepts/deterministic-authorization.md).
- How is tenant data isolated? See [Tenant and Store Isolation](concepts/tenant-store-isolation.md).
- How do I use the dashboard? See [Dashboard Guide](guides/dashboard-guide.md).
- How do I call Aegis from an app? See [API Integration Guide](guides/api-integration.md).
- Which demo objects should I test? See [Demo Data Guide](reference/demo-data.md).
- Which endpoint should I call? See [API Reference](reference/api-reference.md).
