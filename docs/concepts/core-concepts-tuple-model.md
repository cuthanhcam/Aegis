# Tuple Model and Authorization

Aegis models authorization as relationships between subjects and objects.

## Tuple Shape

The basic unit is a tuple:

```text
subject relation object
```

Example:

```text
user:anne editor document:roadmap
```

This means `user:anne` has the `editor` relation on `document:roadmap`.

## Subjects

A subject is an actor or group-like entity.

Examples:

```text
user:anne
user:agent1
team:platform
service:billing-api
```

## Objects

An object is the protected resource.

Examples:

```text
document:roadmap
ticket:INC-1001
account:acme
project:aegis-lab
dashboard:quality
```

## Relations

A relation describes how a subject is connected to an object.

Examples:

```text
owner
editor
viewer
assignee
manager
analyst
maintainer
```

## Authorization Models

Authorization models define which object types and relations are valid in a store.

Example:

```text
type user
type document
  define owner: [user]
  define editor: [user] or owner
  define viewer: [user] or editor
```

This model means:

- A `user` can be an `owner` of a `document`.
- A `user` can be an `editor` of a `document`.
- Owners are also editors.
- Editors are also viewers.

## Derived Access

Derived access lets one relation imply another.

If the model says:

```text
define viewer: [user] or editor
```

Then this tuple:

```text
user:anne editor document:roadmap
```

allows this check:

```text
Can user:anne view document:roadmap?
```

## Store Scope

Stores isolate authorization data. A tuple in one store does not grant access in another store.

Example:

```text
store-docs-default
  user:anne editor document:roadmap

store-support-default
  user:agent1 assignee ticket:INC-1001
```

`document:roadmap` belongs to the docs model. `ticket:INC-1001` belongs to the support model. Mixing the object type and store will fail model validation or graph evaluation.

## Checks

Applications use checks to ask whether access is allowed:

```http
POST /api/v1/stores/{storeId}/check
```

```json
{
  "user": "user:anne",
  "relation": "viewer",
  "object": "document:roadmap"
}
```

## Explain

Explain uses the same request body as check:

```http
POST /api/v1/stores/{storeId}/explain
```

It returns the decision plus trace information so teams can understand why a request was allowed or denied.

## Relationship Writes

Applications write tuples when product access changes:

```http
POST /api/v1/stores/{storeId}/relationships
```

```json
{
  "subject": "user:anne",
  "relation": "editor",
  "object": "document:roadmap",
  "effect": "allow"
}
```

## Explicit Deny

Aegis supports explicit deny tuples:

```text
user:carol viewer document:roadmap deny
```

Explicit deny has priority over allow.

## RBAC and Tuples

Use relationship tuples for fine-grained resource access.

Use RBAC for broad administrative or operational permissions:

- Store administrator.
- Support manager.
- Billing analyst.
- Document reviewer.

Both approaches can be used together.

## Naming Recommendations

- Use stable ids.
- Use lowercase type names.
- Keep object types aligned with the store model.
- Prefer readable ids for demos and support workflows.

Good examples:

```text
user:anne
document:roadmap
ticket:INC-1001
account:acme
```

