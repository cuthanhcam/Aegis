# ADR 0003: Support all-in-one first and preserve deployment profiles

- Status: Accepted
- Date: 2026-08-16
- Owners: Aegis maintainers

## Context

Self-hosted evaluators and smaller installations benefit from one service containing decision APIs, administration, account/session workflows, demo onboarding, and console assets. Larger installations may need independently scaled runtime, control-plane, and worker processes.

## Decision

The first production topology is an all-in-one backend with PostgreSQL and optional Redis. Internal composition must preserve four future profiles:

- `all-in-one`: complete self-hosted and evaluation experience;
- `runtime`: latency-sensitive check, batch, and explain endpoints;
- `control-plane`: identity, tenant/store administration, model authoring, and credentials;
- `worker`: outbox, webhook, retention, and asynchronous processing.

Profiles are architectural seams, not separate deployables until evidence justifies operational separation. Demo identity and seed behavior remain explicitly development/evaluation-only.

## Consequences

- One service remains the easiest supported installation.
- Hosting registrations must avoid hidden cross-profile dependencies.
- PostgreSQL/Redis can be shared infrastructure, while extracted services must own schema, credentials, migrations, and namespaces.
- .NET Aspire may later model and observe these resources; adopting Aspire does not require microservices.

## Validation

Future composition tests will boot supported profiles and verify endpoint/module availability. Until profiles exist, the all-in-one host is the only supported production composition.
