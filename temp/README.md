# Aegis product-readiness planning pack

> Status: planning baseline
> Reviewed: 2026-08-16
> Scope: `src`, `tests`, `frontend`, `docker`, `docs`, and the reference DNA in Forge, Lens of Charlie, and learn-dotnet

This directory contains the working plan for taking Aegis from a capable authorization prototype to a production-grade platform. It deliberately separates backend and frontend delivery so that the authorization runtime can be hardened without being coupled to the console rewrite.

## Documents

| Document | Purpose |
| --- | --- |
| [Current-state assessment](./current-state-assessment.md) | Evidence-based review, strengths, gaps, risks, and architectural decisions required before implementation |
| [Backend delivery plan](./backend-product-readiness-plan.md) | Backend phases, work packages, acceptance criteria, dependencies, and release gates |
| [Frontend rewrite plan](./frontend-rewrite-plan.md) | New product direction, information architecture, target code structure, phased migration, and quality gates |
| [Execution tracker](./execution-tracker.md) | A single operational checklist for sequencing, ownership, evidence, and release decisions |

## Planning principles

1. Authorization correctness is the product. A feature is not complete if tenant isolation, deterministic decisions, audit evidence, and explainability are not demonstrated.
2. Backend and frontend have independent release trains. They share versioned contracts, generated clients, fixtures, and end-to-end scenarios—not implementation coupling.
3. The existing frontend remains available until the replacement satisfies route-level parity and migration gates. New product work targets the replacement only after its foundation phase.
4. “Enterprise” means predictable behavior under failure, observable operations, explicit ownership, accessible workflows, and safe change management. It is not a visual theme.
5. Every phase ends with executable evidence: tests, performance results, threat-model updates, runbooks, screenshots, or recovery drills.

## Recommended delivery order

Begin with Backend Phase B0 and Frontend Phase F0 in parallel. Freeze unstable API shapes through a contract baseline before building the new console data layer. The first production candidate is reached only when backend Phase B4 and frontend Phase F4 both pass their release gates. Later scale, ecosystem, and governance phases can then proceed independently.
