---
title: Operating Aegis in production
description: Production-readiness guidance for SLOs, telemetry, deployments, migrations, recovery, security, and incidents.
category: operations
audience: [operator, platform-engineer, security-engineer]
status: published
last_updated: 2026-08-16
series: operations
order: 2
---

# Operating Aegis in production

An authorization service sits on other products' request paths. Its operational contract must be stricter than “the process is running.” This article defines evidence for deploying, observing, recovering, and supporting Aegis.

## Service-level thinking

Measure user-visible authorization behavior, separating single checks, batches, explain, graph, and administration. Candidate indicators include availability, p50/p95/p99 latency, system error rate, budget exhaustion, stale model/cache observations, dependency latency, database saturation, outbox age, and audit lag. Set targets after representative testing.

An HTTP `200` deny is not a service failure. A quick incorrect allow is much worse than latency. Monitoring separates decision outcomes from correctness and service signals.

## Health and telemetry

Liveness says the process can continue without checking dependencies. Readiness says the instance can safely serve its workload. PostgreSQL loss makes authoritative work unready; optional Redis loss should degrade latency, not correctness.

Correlate request, actor class, tenant-safe scope, store, model, decision trace, database, cache, and audit/outbox spans. Keep metric labels bounded; subject/object IDs are not dimensions. Exclude tokens, secrets, raw context, unrestricted tuples, and policy source from ordinary telemetry.

## Capacity and failure testing

Test steady checks, batches, hot tenants/objects, broad groups, deep rewrites, cycles, writes during reads, cold/lost cache, slow/failing database, outbox backlog/duplicates, rolling shutdown, cancellation, and adversarial bounded inputs. Record dataset, configuration, version, infrastructure, duration, percentiles, errors, and saturation.

## Deployment and migration

Promote one immutable signed artifact. Validate environment configuration without printing secrets. Containers run non-root with explicit resources and minimal contents.

Database changes follow expand/contract: add compatible schema, deploy compatible code, backfill observably, switch after verification, then remove old schema after rollback expires. Migration execution has locking, timeout, checksum, owner, and recovery instructions.

## Backup, recovery, and runbooks

Define RPO/RTO for models, relationships, audits, and configuration. Prove backups by restoring into isolation, checking schema/counts, running golden decisions, reconciling audit/outbox data, and recording elapsed time. Redis caches are reconstructible, not authoritative backups.

Use the [PostgreSQL backup and restore drill](./postgres-backup-restore.md) for the repeatable repository rehearsal and the evidence required from a staging-sized managed restore.

Runbooks cover latency/errors, unexpected decision-pattern changes, database saturation, cache failure/invalidation lag, outbox/audit backlog, bad model activation, identity/key failure, suspected tenant exposure, and failed release/migration. Each alert links symptoms to safe mitigation, ownership, and recovery verification.

## Release checklist

- [ ] Unit, integration, architecture, contract, golden decision, and isolation suites pass.
- [ ] Dependency, secret, license, static, container, and SBOM policies pass.
- [ ] OpenAPI and generated client match the candidate.
- [ ] Migration runs on a production-like restored snapshot.
- [ ] Load/failure results meet the SLO envelope.
- [ ] Dashboards, alerts, runbooks, notes, and rollback instructions are current.
- [ ] Canary and rollback evidence exists.
- [ ] Engineering, security, and operations approve release.

Decision correctness and tenant isolation take precedence over availability. When a safe result cannot be established, downstream enforcement fails closed according to contract. Preserve evidence and separate known facts from hypotheses during incidents.

## Continue reading

Use [Deployment](./deployment.md) for current commands, [API reference](../reference/api-reference.md) for endpoints, and the [backend plan](../../temp/backend-product-readiness-plan.md) for implementation phases.
