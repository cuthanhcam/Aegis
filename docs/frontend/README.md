# Aegis Frontend Direction - Enterprise Control Plane UX

Last updated: 2026-05-22

This folder tracks the frontend strategy for Aegis as a realtime, permission-aware control plane for authorization management, debugging, auditing, and policy workflows.

The frontend should feel closer to LaunchDarkly, Linear, GitHub, and Datadog than to a typical marketing-site dashboard.

## Product Direction

Aegis should evolve into a modern enterprise control-plane frontend with:

- dense but readable enterprise UI
- workflow-first navigation
- realtime operational visibility
- keyboard-first productivity
- permission-aware interactions
- scalable feature-driven architecture
- strong information hierarchy
- reusable component primitives

The product is not just a dashboard. It should behave like an operational system for authorization and policy work.

## Recommended Frontend Stack

Target stack for the new frontend workspace:

- Next.js
- React
- TypeScript
- Tailwind CSS
- shadcn/ui
- Radix UI
- TanStack Query
- TanStack Table
- Zustand
- cmdk

The current reference frontend is Vite-based, which is still useful as a reference implementation for IA and component patterns. The target architecture should use the stack above unless a deliberate project decision says otherwise.

## UX Operating System

Define consistent interaction rules across the application:

- single click selects a row
- double click opens details
- right click opens a context menu
- ESC closes drawers and modals
- CMD+K opens global search and command palette
- Enter opens the selected item

The entire frontend should feel operationally consistent.

## Design Tokens And Density System

Introduce a formal design language for typography, spacing, colors, and density.

### Typography Scale

- 12px = metadata
- 13px = table body
- 14px = standard UI text
- 16px = section headers
- 20px = page titles

### Spacing Scale

- 4
- 8
- 12
- 16
- 24
- 32

### Semantic Colors

- success
- warning
- danger
- info
- muted
- surface
- border

### Density Modes

- compact
- comfortable

Density support is especially important for data tables and audit views.

## Enterprise Table Standards

Tables are a core interaction surface in Aegis and should become shared primitives.

### Core Features

- sorting
- filtering
- pagination
- sticky headers
- row selection
- inline actions

### Advanced Features

- multi-sort
- saved filters
- saved views
- column persistence
- virtualization
- bulk actions
- inline editing

## Realtime Event Architecture

The frontend should adopt an event-driven architecture.

Examples of events:

- relationship.created
- relationship.deleted
- model.updated
- audit.created

Frontend responsibilities:

- subscribe to events
- invalidate caches
- patch UI state in realtime
- display live activity

Required realtime UX:

- live status indicators
- activity feeds
- optimistic updates
- synchronization states
- connection indicators

## Enterprise UX Layer

Standardize loading, empty, error, and validation behavior.

### Loading States

Prefer:

- skeletons
- partial loading
- optimistic rendering

Avoid:

- global blocking spinners

### Empty States

Provide actionable empty states.

Examples:

- No relationships found
- Create your first tuple

### Error Recovery

Support:

- retry actions
- reconnect flows
- inline error recovery

### Validation

Prefer field-level validation over modal-based error handling.

## Keyboard-First Productivity

Enterprise users expect fast keyboard workflows.

Required features:

- command palette
- keyboard shortcuts
- focus management
- keyboard table navigation

Examples:

- CMD+K = search
- G + R = relationships
- ESC = close drawer
- Arrow keys = navigate rows

## Activity And Audit Visualization

Auditability is a major strength of Aegis, so the frontend should make history easy to scan and inspect.

Reusable components should cover:

- activity timelines
- event streams
- operation history
- before/after diff viewers

Examples of events to visualize:

- relationship added
- model updated
- role assigned
- permission revoked

## Permission-Aware UI

Authorization should affect both API access and visible UI actions.

Frontend requirements:

- hide unauthorized actions
- disable restricted controls
- filter menus dynamically
- protect routes
- enforce admin-only management actions

The UI should reflect RBAC boundaries directly.

## Global Search And Command Architecture

Implement a unified command and search experience.

Search targets:

- stores
- models
- relationships
- users
- roles
- audit events

Command capabilities:

- create relationship
- switch store
- open audit
- jump to model

Recommended behavior:

- fuzzy search
- recent items
- keyboard navigation
- pinned actions

## Observability And Explain Visualization

The explain/check flow should become a primary UX differentiator for Aegis.

Potential features:

- authorization trace visualization
- graph-based permission exploration
- relationship traversal display
- denial reasoning
- dependency inspection

This can become one of the most valuable product views in the app.

## Architectural Direction

Aegis should behave as:

LaunchDarkly for authorization.

Key characteristics:

- graph-oriented
- policy-driven
- realtime
- audit-heavy
- operational
- admin-centric

The frontend should optimize for operational workflows rather than marketing-style presentation.

## 1. What The Reference Frontend Already Gets Right

The reference workspace in `temp/Aegis-refactor/frontend` is a solid base because it already has:

- a monorepo workspace with `pnpm` and `turbo`
- React 19 + TypeScript + Vite
- TanStack Query for server state
- Ant Design plus Pro Components for enterprise UI density
- an app shell with sidebar, top bar, and protected routes
- domain slices that are already organized around workflows

## 2. What Must Be Adapted For Current Aegis

The reference frontend still needs Aegis-specific alignment:

- rename and reorganize features around the current backend surface
- make the API client match the live `/api/v1` routes exactly
- keep management screens behind the stronger admin policy
- reflect store-first terminology where the backend requires a `storeId`
- treat check/explain as the central authoring and debugging flow
- add better support for the current RBAC, ReBAC, and ABAC model boundaries
- align visual density and interaction rules to an enterprise control-plane product

## 3. Current Backend Reality

The current backend exposes these major groups:

- authentication: `auth/login`, `auth/refresh`, `auth/me`
- authorization checks: `check`, `explain`, `stores/{storeId}/check`, `stores/{storeId}/explain`
- relationship and model management
- RBAC management: roles, permissions, users
- audit and presets
- graph-style queries for users, objects, and expansion

The frontend should use that shape as the source of truth.

## 4. Recommended Frontend Information Architecture

Start with a control-plane shell and feature-driven routing:

- `stores` for store lifecycle and active context
- `models` for authorization models and model history
- `relationships` for tuple management and filtering
- `assertions` for model assertions
- `graph` for list/expand exploration
- `test-console` for permission checks and explain traces
- `access` for RBAC role/user/permission administration
- `audit` for change history and event review
- `presets` for reusable configuration bundles
- `profile` for identity, session, and environment details

## 5. Feature Modules To Prioritize

The feature-first structure should be organized around workflow rather than generic page folders:

- stores
- models
- relationships
- assertions
- graph
- test-console
- access
- audit
- presets
- profile

Each feature should own its page-level UI, API hooks, local state, and small helper components.

## 6. Reusable UI Patterns To Build First

Prioritize the primitives that will be reused across all views:

- app shell with left nav, header, search, and store selector
- table wrapper with filtering, sorting, pagination, and empty/loading states
- drawer/side-inspector pattern for row detail and inline editing
- command palette or global search entry point
- status badges and operation timeline components
- context menu actions for row-level operations

## 7. Information Architecture Guidance

Structure the app around operational workflows:

- global shell and workspace selection
- store-level exploration and management
- model and policy authoring
- tuple and relationship management
- permission debugging and explainability
- RBAC administration
- audit and activity review
- reusable presets and templates

The most important screens should be the ones that help operators understand and debug authorization outcomes fast.

## 8. Implementation Priorities

Phase 1:

- create the production frontend workspace under `frontend/`
- wire the typed API client to the current backend contracts
- implement auth/session and tenant/store context
- build the shell, navigation, and route guards
- implement stores, relationships, models, and check/explain views

Phase 2:

- build RBAC administration views for roles, permissions, and users
- add audit and presets screens
- add graph exploration with progressive disclosure
- extract shared table, drawer, badge, and form patterns into `@aegis/ui`

Phase 3:

- add realtime refresh for audit and relationship changes
- add global search and command palette
- add keyboard shortcuts and density controls
- polish mobile and narrow-layout behavior

## 9. Recommended Additions To The Frontend Strategy

Add and maintain these sections as the frontend work matures:

1. Design Tokens And Density System
2. Enterprise Table Standards
3. Realtime Event Architecture
4. Keyboard And Command UX
5. Permission-Aware Interaction Rules
6. Activity And Audit Visualization
7. Global Search Architecture
8. UX Consistency Rules
9. Loading, Error, And Empty State Standards
10. Observability And Explain Visualization

## 10. Frontend Guardrails

- do not let UI state and server cache blur together
- keep backend contracts typed through one API client package
- do not expose management actions without admin role checks
- preserve tenant/store scoping in every data request
- favor dense, readable enterprise layouts over marketing-style pages
- prefer reusable primitives over one-off page-only widgets

## 11. Notes To Remember

- `check` and `explain` are the user-facing core loop for Aegis
- `storeId` is the primary scope for resource management
- RBAC admin endpoints are privileged and should be hidden for non-admin users
- the current `@aegis/ui` package is only a small starting point, not a finished design system
- the frontend should feel like an operational system, not a static documentation site
