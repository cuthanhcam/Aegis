# Core Concepts

Aegis uses a small set of concepts to model authorization.

## Store

A store is an isolated authorization workspace. A store usually maps to an application, tenant, product domain, or environment.

Examples:

```text
store-docs-default
store-support-default
store-billing-default
```

## Subject

A subject is the actor or entity that may receive access.

Examples:

```text
user:anne
team:platform
service:billing-api
```

## Object

An object is the resource being protected.

Examples:

```text
document:roadmap
ticket:INC-1001
account:acme
dashboard:quality
```

## Relation

A relation is the kind of access or relationship between a subject and an object.

Examples:

```text
owner
editor
viewer
assignee
manager
analyst
```

## Tuple

A tuple is a concrete authorization fact:

```text
subject relation object
```

Example:

```text
user:anne editor document:roadmap
```

This says Anne is an editor of the roadmap document.

## Authorization Model

An authorization model defines the object types and relations that are valid in a store.

Example:

```text
type user
type document
  define owner: [user]
  define editor: [user] or owner
  define viewer: [user] or editor
```

This means:

- Owners are editors.
- Editors are viewers.
- A direct viewer can also view the document.

## Check

A check asks whether a subject has a relation to an object:

```text
Can user:anne view document:roadmap?
```

Aegis returns allow or deny.

## Explain

Explain returns the reasoning path behind a decision. Use it when debugging an unexpected allow or deny.

## Graph Queries

Graph queries answer questions beyond a single check:

- Which users can access this object?
- Which objects can this user access?
- What access tree explains this object?

## RBAC

RBAC in Aegis manages coarse-grained roles and permissions. Relationship tuples are preferred for fine-grained resource access; roles are useful for broad administrative or operational permissions.

## More Detail

For a deeper explanation, read [Tuple Model and Authorization](core-concepts-tuple-model.md).

