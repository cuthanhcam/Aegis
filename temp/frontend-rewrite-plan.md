# Frontend rewrite plan

> Workstream: Frontend
> Strategy: replacement application delivered by vertical slices
> Design direction: quiet, precise, operational, trustworthy

## Product thesis

The new Aegis console is an authorization workbench, not a generic administration dashboard. Its primary job is to help a platform engineer answer four questions quickly:

1. What policy and relationship data is active?
2. Why was this access decision made?
3. What will change if I publish this model or relationship mutation?
4. Is the authorization system healthy and safe to operate?

The interface should combine Forge's efficient tool-workspace DNA with Lens of Charlie's restraint and content hierarchy. It should avoid decorative gradients, glass effects, dense collections of colorful cards, and hidden hover-only controls. The strongest color is reserved for semantic status and the current action.

## Experience architecture

```text
Organization / tenant switcher
  └─ Store workspace
      ├─ Overview
      ├─ Model
      │   ├─ Editor
      │   ├─ Versions and diff
      │   └─ Validation and assertions
      ├─ Relationships
      ├─ Playground
      │   ├─ Check and explain
      │   └─ Graph explorer
      ├─ Activity
      │   ├─ Decision audit
      │   └─ Change history
      └─ Settings
          ├─ Access
          ├─ Credentials
          └─ Store configuration
```

Presets become contextual actions inside Model, Playground, or onboarding rather than a top-level destination. Profile and global administration live in the account/tenant switcher. This reduces the navigation from an implementation inventory to a user-task model.

## Layout and visual language

- Use semantic design tokens for canvas, surface, elevated surface, text, muted text, border, focus, success, warning, danger, and decision states. Components never consume raw brand colors.
- Provide neutral light and dark themes; do not implement dark mode as simple inversion.
- Use a 1200–1440px operational canvas, a 680–760px reading/help measure, responsive gutters, and density modes for data-heavy tables.
- Use one calm application frame: slim global bar, collapsible store navigation, contextual page header, and a working canvas. Avoid nested card walls.
- Treat model editing and explain traces as dedicated workbenches with resizable panes, persistent keyboard-accessible controls, and URL-addressable state.
- Motion is short feedback only; honor reduced motion. Focus is always visible. Essential controls remain available on touch and narrow screens.

## Target code structure

```text
frontend/
├─ apps/
│  ├─ console/                 # replacement product
│  └─ admin-dashboard/         # legacy, frozen except critical fixes
├─ packages/
│  ├─ api-client/              # generated transport + thin runtime adapter
│  ├─ contracts/               # generated schemas and domain-safe types
│  ├─ design-system/           # tokens, primitives, compositions, docs/tests
│  ├─ authorization-model/     # parser/editor/diff domain utilities
│  ├─ observability/           # safe client telemetry boundary
│  ├─ test-fixtures/           # API and authorization scenarios
│  └─ tooling/                 # shared TS, ESLint, Vitest, Playwright config
└─ package.json                # one `pnpm check` quality gate
```

Inside `apps/console/src`, use `app` for composition, `routes` for lazy route boundaries, `features` for user capabilities, `entities` for stable domain representations, and `shared` only for genuinely cross-feature infrastructure. A feature owns its queries, mutations, schemas, state machine, view components, and tests. Barrel files must not create circular feature dependencies.

## State and data rules

- Server state lives in TanStack Query; query keys always include tenant, store, and relevant model version.
- URL state owns shareable filters, selected tabs, trace identifiers, and playground inputs that are safe to expose.
- Local component state owns transient presentation. A small external store is permitted only for true cross-route client state.
- Forms parse untrusted input through runtime schemas. TypeScript types alone are not validation.
- The generated client owns HTTP shapes. Feature adapters translate them into UI/domain models and standardize errors.
- Mutations declare invalidation explicitly and provide idempotency keys where supported. Optimistic updates are limited to actions with safe reconciliation.
- Never persist access tokens in localStorage. Identity/session transport follows the backend threat model.

## Phase F0 — Discovery, parity ledger, and rewrite fence

**Outcome:** the team knows what must be preserved and where new code belongs.

- Record every existing route, user job, API call, permission, loading/empty/error state, keyboard flow, and known defect.
- Capture screenshots and representative fixtures for each route at desktop and mobile widths.
- Add `apps/console` with independent routing and a feature flag or separate development entry point.
- Freeze the legacy app to critical fixes. New capabilities require an explicit exception until their replacement slice exists.
- Define browser support, accessibility target (WCAG 2.2 AA), localization readiness, telemetry privacy, and performance budgets.

**Exit gate:** parity ledger is approved; the replacement boots in CI; legacy and new applications can coexist without shared mutable internals.

## Phase F1 — Foundations and design system

**Outcome:** every later slice starts from a coherent interaction and quality system.

- Implement semantic tokens, typography, spacing, elevation, status, focus, motion, density, and responsive contracts.
- Build primitives and compositions: button, field, select, dialog, drawer, command menu, tabs, table, pagination, code editor frame, diff, callout, toast, skeleton, empty state, error state, destructive confirmation, and permission boundary.
- Build the shell, tenant/store switcher, breadcrumbs, page header, route error boundary, not-found, session-expired, forbidden, and offline/degraded states.
- Add Storybook or an equivalent component harness only if it participates in visual and accessibility tests; otherwise keep a lean internal gallery route.
- Configure Vitest/Testing Library, Playwright, axe, visual regression, bundle analysis, and `pnpm check`.

**Exit gate:** both themes, narrow widths, keyboard-only navigation, screen-reader landmarks, and forced/reduced-motion modes pass the foundation checklist; no raw color or spacing values appear in feature code.

## Phase F2 — Read-only product spine

**Outcome:** users can safely inspect the platform through the new console.

- Deliver authentication/session bootstrap, tenant/store selection, overview, model versions, relationship listing, and activity/audit browsing.
- Use the generated client and consistent query keys from the first route.
- Introduce filter/search patterns, cursor pagination, saved safe preferences, copyable identifiers, timestamps with timezone clarity, and deep links.
- Show freshness, active model version, request/trace IDs, permission limits, and degraded dependency status where relevant.

**Exit gate:** read-only routes meet contract, accessibility, responsive, visual, error, empty, stale-data, and performance tests. No legacy API helper is imported.

## Phase F3 — Core authoring workbench

**Outcome:** model and relationship changes are understandable before they become active.

- Build model draft/editor with schema-aware validation, diagnostics, version diff, unsaved-change protection, and explicit publish/rollback workflow.
- Integrate assertions as a pre-publication gate with grouped results, precise failures, and fixture import/export.
- Build relationship create/delete/bulk workflows with preview, validation, idempotency, partial-failure reporting, and safe destructive confirmation.
- Use server capabilities for authoritative validation; client validation improves feedback but never claims authorization correctness.

**Exit gate:** every mutation has audit evidence, retry behavior, concurrency behavior, destructive safeguards, and end-to-end coverage. A user can recover from refresh, conflict, validation failure, and expired session without losing explainable state.

## Phase F4 — Decision and graph workbench

**Outcome:** Aegis's defining capability—understanding access—feels clearer than raw API inspection.

- Combine check and explain into a guided request builder with subject, relation, object, contextual tuples, model version, and reusable local drafts.
- Render the evaluation trace as a semantic tree/timeline with decision, stage, rule, evidence, depth, cache/version context, and copyable JSON.
- Build graph exploration with bounded expansion, progressive loading, list/table fallback, keyboard navigation, and explicit truncation/depth states.
- Link audit records, model versions, relationships, and decision traces without losing tenant/store context.

**Exit gate:** golden authorization scenarios render the same outcome and evidence as the backend; large traces and graphs stay within performance budgets; all information has a non-visual representation.

## Phase F5 — Administration and migration

**Outcome:** the replacement owns all daily workflows and legacy removal is reversible.

- Deliver users, roles, permissions, service credentials, tenant settings, profile, onboarding, and contextual templates.
- Add role-aware navigation and controls for usability while preserving server-side enforcement as authoritative.
- Execute route-by-route pilot, compare support signals and telemetry, publish migration notes, then redirect legacy routes.
- Retain a rollback window. Delete the legacy app only after usage, parity, and support gates pass and the rollback period ends.

**Exit gate:** parity ledger is complete or exceptions are signed; no production link enters the legacy app; generated contracts and end-to-end flows pass against a release-candidate backend.

## Phase F6 — Product leverage

After cutover, consider command palette, policy review workflow, collaborative draft links, organization-wide search, audit export, guided incident mode, and embedded documentation. Each addition must preserve the calm navigation model and cannot turn the home screen into a card catalog.

## Frontend quality budgets

| Dimension | Initial release gate |
| --- | --- |
| Accessibility | WCAG 2.2 AA automated checks plus keyboard and screen-reader review for critical flows |
| Performance | Route-level budgets agreed in F0; lazy-load Monaco and graph rendering; no silent budget regression |
| Reliability | Every route defines loading, empty, error, forbidden, stale, and retry behavior |
| Security | No token in persistent browser storage; CSP-compatible implementation; untrusted model/trace content rendered safely |
| Contracts | Generated client is clean; OpenAPI breaking-change and fixture tests pass |
| Responsiveness | Core workflows remain operable at 320px; dense workbenches may reflow but cannot hide essential actions |
| Observability | Errors include safe correlation; telemetry excludes secrets, tokens, tuple context, and policy source unless explicitly approved |

## Rewrite anti-patterns

- Recreating every old route before validating the new information architecture.
- Copying legacy 500-line pages into new folders and calling the result modular.
- Building a design system as a large speculative component library.
- Encoding permissions only in navigation visibility.
- Treating a successful HTTP response as a complete mutation without cache reconciliation and audit feedback.
- Visualizing explain/graph data without a text alternative, truncation policy, or performance budget.
