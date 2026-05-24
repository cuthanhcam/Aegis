# Frontend Architecture Review (2026-05-23)

## Scope

This review assessed the active frontend workspace at `frontend/` against production-grade architecture criteria:

- folder consistency
- separation of concerns
- feature vs layer organization
- reusable components
- API/service structure
- state and routing organization
- shared hooks/utilities/assets
- naming, scalability, maintainability, and DX

## Current Assessment

## What is strong already

- Monorepo layout (`apps/`, `packages/`) is clean and scalable.
- Clear package boundaries: `api-client`, `types`, and shared `ui`.
- App is already feature-first under `apps/admin-dashboard/src/features`.
- Routing is protected and shell composition is centralized.
- Server state uses TanStack Query consistently.

## Issues identified

- Some missing backend capabilities were handled by temporary frontend gating.
- Route definitions were verbose and page imports were centralized in one large file.
- A feature-specific component (`OnboardingWizard`) lived in `shared/components`, reducing ownership clarity.
- Backend contract alignment for assertions/changes/assign-role-to-user was incomplete.

## Refactors Applied

## 1) Backend contract and endpoint exposure

Added backend endpoints to remove temporary frontend limitations:

- `POST /api/v1/tenants/{tenantId}/users/{userId}/roles`
- `GET /api/v1/stores/{storeId}/relationships/changes`
- `GET /api/v1/stores/{storeId}/assertions/{authorizationModelId}`
- `POST /api/v1/stores/{storeId}/assertions/{authorizationModelId}`

## 2) API client alignment

Updated `frontend/packages/api-client/src/index.ts` to:

- re-enable `readChanges`
- re-enable `readAssertions` / `writeAssertions`
- re-enable `assignRoleToUser`
- keep tenant-scoped management routes and store graph/explain alignment

## 3) Feature ownership cleanup

Moved onboarding UI from shared to profile feature scope:

- from: `src/shared/components/OnboardingWizard.tsx`
- to: `src/features/profile/components/OnboardingWizard.tsx`

## 4) Routing structure cleanup

Introduced route config manifest:

- `src/app/routes/route-config.tsx`

Then updated `AppRoutes` to map over `protectedRoutes` instead of hardcoding each route.

## 5) UI flow re-enabled

Re-enabled and connected previously gated screens:

- Assertions page read/write flows
- Access page assign-role-to-user flow
- Audit page store changes tab

## Target Architecture (recommended baseline)

```text
apps/admin-dashboard/src/
  app/
    providers/
    routes/
      AppRoutes.tsx
      route-config.tsx
  features/
    <feature>/
      pages/
      components/
      api/         # next extraction target for query/mutation hooks
      state/       # optional local state
  shared/
    api/
    hooks/
    layouts/
    ui/
    utils/
```

## OSS-Style Conventions Adopted

- each feature has a single entrypoint file (`features/<name>/index.ts`) for clean imports
- route metadata drives both router definitions and sidebar navigation
- page components stay slim and delegate data access to feature-local `api/` hooks
- shared layout owns shell behavior only; feature logic stays in features
- feature-specific UI stays inside the feature folder instead of `shared/components`

This keeps the project easier to scan, reduces import noise, and makes future feature extraction straightforward.

## Next Increment (recommended)

- Extract page-level query/mutation logic into `features/*/api` hooks modules.
- Add `features/*/index.ts` barrel exports for cleaner imports.
- Add route metadata (label, icon, required role) in route config and derive sidebar navigation from the same source.
- Expand `@aegis/ui` with table/form primitives currently duplicated in feature pages.

## Conclusion

The frontend now follows a stronger professional baseline:

- backend-compatible
- cleaner feature ownership
- more maintainable routing
- improved scalability path without disruptive rewrites
