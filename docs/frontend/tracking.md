# Frontend Tracking

Last updated: 2026-05-23

## Status

| Area                  | State       | Notes                                                                              |
| --------------------- | ----------- | ---------------------------------------------------------------------------------- |
| Workspace review      | Done        | Reference frontend exists under `temp/Aegis-refactor/frontend`.                    |
| Backend mapping       | Done        | API client and management/store routes are aligned with the live backend contract. |
| UI shell              | In progress | Shell is stable; route config manifest introduced for easier growth.               |
| Design tokens         | Not started | Need a formal density, typography, and color token system.                         |
| Shared design system  | In progress | Shared package exists; next step is extracting repeated table/form primitives.     |
| Enterprise tables     | Not started | Data tables need filtering, sorting, bulk actions, and saved views.                |
| Realtime data flow    | Not started | Keep deferred until event transport strategy is finalized.                         |
| Command palette       | Not started | Global search and keyboard-first navigation still need implementation.             |
| Explain visualization | Not started | Check/explain should become a first-class operational UX.                          |

## Work To Do

1. Create the real `frontend/` workspace from the reference.
2. Define the Aegis domain IA and route map.
3. Replace API assumptions with the current `/api/v1` contract.
4. Build auth, tenant/store context, and route guards.
5. Introduce a design token and density system.
6. Implement the core control-plane screens:
    - Stores
    - Models
    - Relationships
    - Check / Explain
    - RBAC admin
    - Audit
    - Presets
    - Graph explorer
7. Add the enterprise primitives:
    - data table wrapper
    - drawer inspector
    - command palette
    - status badges
    - activity timeline
8. Add permission-aware UI guards and admin-only action handling.
9. Add realtime refresh for audit and relationship events.
10. Promote repeated UI patterns into shared packages.
11. Add explain visualization and graph-based permission exploration.

## Backend Contract Notes

- management endpoints require the `authorization_admin` role
- tenant scope must be preserved on every request
- check/explain are the primary user-facing authorization flows
- graph and admin views need to respect current backend route names exactly
- the frontend should treat live backend permissions as the source of truth for UI access

## Recently Completed (2026-05-23)

- exposed backend endpoints for:
    - assign role to user
    - store relationship changes feed
    - assertions read/write
- re-enabled frontend flows for access assignments, assertions, and store changes
- moved onboarding component into `features/profile/components` for cleaner feature ownership
- introduced `app/routes/route-config.tsx` to centralize protected route definitions
- published architecture review:
    - `docs/frontend/architecture-review-2026-05-23.md`

## Known Gaps In The Reference Frontend

- some client paths are still based on the refactor workspace and must be audited against the live backend
- the shared UI package currently looks thin and needs real reusable primitives
- realtime transport is not yet implemented
- search and command palette behavior is still mostly a design requirement rather than a complete feature
- there is no completed tokens/density standard yet
- explain/check trace visualization still needs design and implementation work
