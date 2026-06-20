# Documentation Strategy

This document explains how Aegis documentation should evolve as the platform grows.

## Goals

- Make Aegis understandable to first-time visitors.
- Keep implementation docs useful for contributors.
- Prepare content for a future public docs website.
- Allow selected docs to be embedded inside the Aegis dashboard.
- Avoid mixing roadmap notes, architecture decisions, and user guides in one place.

## Documentation Layers

### 1. Public Product Docs

Audience:

- New users
- Platform teams evaluating Aegis
- Developers integrating Aegis

Files:

- [Product Overview](../product/product-overview.md)
- [Core Concepts](../concepts/core-concepts-tuple-model.md)
- [User Guide](user-guide.md)
- [API Reference](../reference/api-reference.md)
- [Demo Data Guide](../reference/demo-data.md)

Tone:

- Clear and direct.
- Task-oriented.
- Avoid internal implementation details unless needed.

### 2. Developer and Contributor Docs

Audience:

- Backend contributors
- Frontend contributors
- Maintainers

Files:

- [Development Setup](getting-started-development.md)
- [Architecture Overview](../architecture/README.md)
- [Project Structure](../architecture/project-structure.md)
- [Testing Plan](../architecture/testing-plan.md)
- [Frontend README](../frontend/README.md)

Tone:

- Practical.
- Explain tradeoffs.
- Include commands and file references.

### 3. Operations Docs

Audience:

- SRE
- Platform operators
- Deployment owners

Files:

- [Deployment Operations Guide](deployment-operations-guide.md)
- [Database, Migrations, and Docker](../operations/database-migrations-and-docker.md)
- [Performance Plan](../architecture/performance-plan.md)

Tone:

- Procedure-oriented.
- Include failure modes and recovery steps.

### 4. Architecture Decision Records

Audience:

- Maintainers
- Architects
- Future contributors trying to understand why something exists

Files:

- [ADR directory](../adr)

Tone:

- Historical and decision-focused.
- Do not rewrite ADRs as marketing content.

## Future Docs Website Structure

Suggested navigation:

```text
Home
Getting Started
  - What is Aegis?
  - Quickstart
  - Demo Data
Concepts
  - Stores
  - Authorization Models
  - Relationships
  - Checks
  - Explain
  - RBAC
Guides
  - Model a Document App
  - Model a Support App
  - Debug a Denied Request
  - Migrate from Inline Permissions
API Reference
  - Auth
  - Stores
  - Models
  - Relationships
  - Checks
  - Graph
  - Access Management
Operations
  - Deployment
  - Migrations
  - Logging
  - Metrics
Contributing
  - Development Setup
  - Architecture
  - Testing
```

## Frontend Embedding Plan

The dashboard should embed small, contextual docs rather than the full docs tree.

Recommended embedded pages:

- Store selector help: what stores are and how to choose boundaries.
- Model editor help: model syntax examples.
- Relationship screen help: tuple format and common examples.
- Check screen help: how to interpret decision and reason code.
- Explain screen help: how to read traces.
- Graph screen help: list-users, list-objects, and expand examples.
- Access management help: store-scoped roles versus tenant-scoped users.

Implementation options:

- Load markdown files from bundled static assets.
- Convert selected docs into typed content objects.
- Build a dedicated docs route in the admin dashboard.
- Build a separate docs app later if public docs become large.

## Maintenance Checklist

- [ ] Update API docs whenever a controller route or DTO changes.
- [ ] Update demo data docs whenever development seed data changes.
- [ ] Keep README focused on project orientation, not exhaustive documentation.
- [ ] Keep user-facing docs free of stale implementation details.
- [ ] Add screenshots only after UI stabilizes.
- [ ] Prefer ASCII Markdown for portability.
- [ ] Run link checks before publishing docs externally.

