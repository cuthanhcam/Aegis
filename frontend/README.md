# Aegis frontend workspace

> Lifecycle: legacy-frozen as of 2026-08-16

The current admin dashboard remains available as a workflow and API-parity reference while backend product-readiness work proceeds. It is not the foundation of the future Aegis console.

Allowed changes are limited to critical security fixes, broken builds, and compatibility fixes required to keep reference workflows usable. New product capabilities and visual refactors require an explicit exception. Do not delete the workspace until route/API fixtures, screenshots, and workflow knowledge are captured and the removal decision has a rollback path.

See [ADR 0004](../docs/decisions/0004-freeze-legacy-frontend.md) and the [frontend rewrite plan](../temp/frontend-rewrite-plan.md).

## Existing verification

```powershell
pnpm install --frozen-lockfile
pnpm typecheck
pnpm lint
pnpm build
```

The future replacement must add automated component, accessibility, contract, and end-to-end tests before cutover.
