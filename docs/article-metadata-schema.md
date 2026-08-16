# Article metadata schema

Aegis documentation is written as durable, publishable articles. Frontmatter makes each article discoverable without coupling Markdown to a specific site generator.

## Required fields

```yaml
---
title: Deterministic authorization decisions
description: How Aegis evaluates an access request predictably and explains the result.
category: concepts
audience: [application-developer, security-engineer]
status: published
last_updated: 2026-08-16
---
```

| Field          | Rule                                                                          |
| -------------- | ----------------------------------------------------------------------------- |
| `title`        | Specific, sentence case, useful outside the navigation tree                   |
| `description`  | 80–180 characters describing the reader outcome                               |
| `category`     | `product`, `concepts`, `architecture`, `guides`, `reference`, or `operations` |
| `audience`     | One or more known audience identifiers                                        |
| `status`       | `draft`, `review`, `published`, or `deprecated`                               |
| `last_updated` | ISO date of the last material verification                                    |

Optional fields are `prerequisites`, `series`, `order`, `tags`, and `replaces`. Audience identifiers are `evaluator`, `application-developer`, `platform-engineer`, `security-engineer`, `operator`, `frontend-engineer`, and `backend-engineer`.

## Article contract

An article normally contains an outcome-oriented introduction, prerequisites, a mental model, a concrete scenario, architecture and security implications, failure modes, a verification checklist, and next reading. Use `##` for primary sections and `###` for subsections. Code fences require a language. Mermaid diagrams need an adjacent prose explanation.

Contract examples must be checked against OpenAPI or integration tests. Operational commands must be exercised in a supported environment. Security-sensitive articles require review when identity, tenant scope, cache identity, model lifecycle, or audit behavior changes.
