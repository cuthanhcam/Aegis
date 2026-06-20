# Dashboard Guide

The Aegis dashboard is the visual workspace for managing authorization data and debugging decisions.

## Active Store

Most dashboard screens operate on the active store. Choose the active store first, then work with models, relationships, checks, graph queries, access management, and audit.

If a request fails with a type or relation error, confirm that the active store matches the object you are testing.

## Stores

Use Stores to create and select authorization workspaces.

Common store boundaries:

- One store per application.
- One store per tenant.
- One store per environment.
- One store per product domain.

## Models

Use Models to define the authorization schema for a store.

A model defines:

- Object types.
- Relations.
- Direct users that can hold a relation.
- Derived relations, such as viewer from editor.

Example:

```text
type user
type document
  define owner: [user]
  define editor: [user] or owner
  define viewer: [user] or editor
```

## Relationships

Use Relationships to manage concrete access facts.

Example:

```text
user:anne editor document:roadmap
```

This means Anne is an editor of the roadmap document.

## Check and Explain

Use Check to ask whether access should be allowed.

Use Explain when:

- A user cannot access something they should access.
- A user can access something they should not access.
- You need to understand which tuple or relation caused a decision.

## Graph

Use Graph to inspect relationship reachability:

- `list-users`: find users that have a relation to an object.
- `list-objects`: find objects a user can access.
- `expand`: view the access tree for an object relation.

Seeded graph examples:

| Store | Object |
| --- | --- |
| `store-docs-default` | `document:roadmap` |
| `store-support-default` | `ticket:INC-1001` |
| `store-billing-default` | `account:acme` |
| `store-lab-tenant-dev` | `project:aegis-lab` |
| `store-analytics-tenant-dev` | `dashboard:quality` |

## Access Management

Use Access Management for store-scoped RBAC:

- Create roles.
- Create permissions.
- Assign permissions to roles.
- Assign roles to users.

Users are tenant-scoped. Roles and permissions are store-scoped.

## Audit

Use Audit to inspect authorization decisions and operational activity. Audit is especially useful when paired with Explain.

## Troubleshooting

### A graph query returns `type_not_found`

The object type is not defined in the active store's model. For example, `document:roadmap` belongs to the docs store, not the support store.

### A check returns deny

Confirm:

1. The active store is correct.
2. The model defines the object type.
3. The model defines the relation.
4. A tuple or role assignment exists.
5. The request context satisfies any condition.

