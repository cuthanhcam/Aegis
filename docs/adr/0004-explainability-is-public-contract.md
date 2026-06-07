# ADR 0004: Explainability Is a Public Contract

## Status

Accepted

## Context

Aegis exposes `/explain` and trace-like information. For an OSS authorization platform, explainability is required for debugging, trust, and audit review.

## Decision

Explainability is a public API contract, not only internal debug logging.

Decision explanations should be structured and versioned within the public API.

## Consequences

- Reason codes must be stable inside an API version.
- Explain responses should identify the deciding stage.
- Graph traversal stops such as cycle detection and max depth must be visible.
- API changes to explanation shape require compatibility review.
