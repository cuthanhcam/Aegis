# ADR 0004: Freeze the current frontend as a parity reference

- Status: Accepted
- Date: 2026-08-16
- Owners: Aegis maintainers

## Context

The current dashboard demonstrates broad workflows, but large pages, global styling, duplicate primitives, and a concentrated API client make it unsuitable as the foundation of the next console. Removing it immediately would also remove working examples and parity evidence while backend contracts are still being hardened.

## Decision

The existing `frontend/apps/admin-dashboard` is legacy-frozen. Only critical security, build, or compatibility fixes are accepted. Backend product readiness and governed contracts take priority. A future replacement starts in a new application boundary and imports no legacy UI internals.

Before deletion, maintainers capture route/API parity, representative fixtures, screenshots, and approved exceptions. Deletion is a dedicated reversible change after replacement or an explicit product decision that Aegis ships API-only.

## Consequences

- No cosmetic refactors or new product features are added to the legacy dashboard by default.
- Documentation may still use it for existing workflows while clearly labeling its lifecycle.
- Generated contracts and backend stability precede new frontend implementation.
- Source remains available until its knowledge is captured.

## Validation

The frontend README records the freeze. The execution tracker keeps discovery, replacement, cutover, and removal as explicit gated work.
