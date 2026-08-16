# Aegis agent guide

Aegis is a security-sensitive authorization platform. Preserve deterministic access decisions, tenant/store isolation, auditability, and explain traces. Never weaken authorization checks to make a test pass.

## Repository map

- `src`: .NET backend and application layers.
- `tests`: backend tests; mirror production boundaries when adding coverage.
- `frontend`: pnpm/Turborepo React and TypeScript workspace.
- `docker`: local infrastructure.
- `docs`: product, API, architecture, and operations documentation.
- `ref`: reference material; do not modify unless explicitly requested.

Use the SDK pinned by `global.json`. For backend changes, restore/build from the solution root and run the closest affected test project before broader tests. For frontend changes, use the pnpm version declared in `frontend/package.json` and run the relevant Turbo tasks. Keep contracts synchronized across backend, frontend, tests, and docs when an API or authorization behavior changes.
