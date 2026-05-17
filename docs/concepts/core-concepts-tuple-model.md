# Core Concepts: Tuple Model & Authorization

---

## 1. The Canonical Tuple

The **tuple** is the fundamental unit of authorization in Aegis:

```
(subject, relation, object)
```

### Anatomy of a Tuple

| Component    | Format        | Example                | Meaning                      |
| ------------ | ------------- | ---------------------- | ---------------------------- |
| **Subject**  | `<type>:<id>` | `user:alice`           | The entity requesting access |
| **Relation** | `<name>`      | `editor`               | The type of relationship     |
| **Object**   | `<type>:<id>` | `document:report-2024` | The resource being accessed  |

### Valid Tuple Examples

```
(user:alice, owner, document:report-2024)
   Alice is the owner of report-2024

(team:engineering, member, user:bob)
   Bob is a member of the engineering team

(team:product, owner, repo:roadmap)
   The product team owns the roadmap repository

(role:admin, has_permission, action:delete_user)
   Admin role has permission to delete users

(tenant:mybiz, contains, store:auth-v1)
   Tenant "mybiz" contains authorization store "auth-v1"
```

---

## 2. Namespacing Convention

Aegis enforces a **strict naming convention** for tuples:

```
Subject and Object MUST follow: <type>:<id>

Where:
   type  = resource type (lowercase, alphanumeric)
   id    = unique identifier (alphanumeric, underscore, dash)
```

### Why This Convention?

1. **Clarity** You immediately know it's a user vs. a team vs. a group
2. **Graph-readiness** Enables future graph traversal (e.g., `team:dev contains user:bob?`)
3. **Auditability** Type information helps in audit logs and logging
4. **Scalability** Avoids namespace collisions as the system grows

### Type Examples

```
user:1234              (internal system user)
user:alice@company.com (email-based identifier)
team:engineering       (team or group)
org:acme-corp          (organization)
role:admin             (role identifier)
document:abc123        (a document)
repo:code-repo-1       (a code repository)
dataset:analytics      (a dataset)
```

---

## 3. Relations (Relation Names)

A **relation** defines the type of relationship between subject and object.

### Common Relations

| Relation      | Typical Use           | Example                                      |
| ------------- | --------------------- | -------------------------------------------- |
| `owner`       | Ultimate authority    | `user:alice owns document:report`            |
| `editor`      | Can modify            | `team:dev can edit repo:code`                |
| `viewer`      | Can read              | `user:bob can view document:report`          |
| `member`      | Membership            | `user:alice is member of team:eng`           |
| `admin`       | Administrative access | `user:admin has admin access to org:acme`    |
| `contributor` | Can commit            | `user:dev1 is contributor to repo:code`      |
| `parent`      | Hierarchy             | `org:parent-company contains org:subsidiary` |

### Custom Relations

You can define **domain-specific relations**:

```
(user:analyst, auditor, dataset:sales)   analyst has auditor access to sales dataset
(user:manager, approver, document:budget)   manager can approve budget documents
```

---

## 4. Effects: Allow vs. Deny

Every tuple has an **effect** that determines its authorization outcome:

### Default: Allow

```json
{
    "subject": "user:alice",
    "relation": "editor",
    "object": "document:report",
    "effect": "allow" // or omit since it defaults to allow
}
```

### Explicit Deny

```json
{
    "subject": "team:contractors",
    "relation": "editor",
    "object": "document:confidential",
    "effect": "deny"
}
```

**Principle:** Explicit deny **always overrides** allow.

```text
If: (user:alice, editor, doc:report) = ALLOW
And: (team:all-users, editor, doc:report) = DENY   More specific deny wins!
Then: user:alice  DENY (because deny overrides allow)
```

---

## 5. Tenant Isolation

Every tuple is **scoped to exactly one tenant**:

```text
Tenant A:
  (user:1, owner, document:10)
  (team:dev, member, user:1)

Tenant B:   Different tenant
  (user:1, owner, document:10)   Different user:1, different document:10
  (team:qa, member, user:2)
```

**Why?** In a multi-tenant SaaS, Tenant A's data must **never** be visible to Tenant B.

All permission checks **require** tenant context:

```http
POST /api/v1/check
X-Tenant-Id: tenant-123

{
  "subject": "user:1",
  "relation": "editor",
  "object": "document:5"
}
```

---

## 6. Authorization Models (Schema)

An **AuthorizationModel** defines the **schema** for a Store:

```json
{
    "schema": "1.0.0",
    "model": {
        "relations": {
            "owner": { "types": ["user", "team"] },
            "editor": { "types": ["user", "team"] },
            "viewer": { "types": ["user", "team"] },
            "member": { "types": ["user"] }
        },
        "types": ["user", "team", "document", "team", "org"]
    }
}
```

**Purpose:** Validates that tuples conform to your domain model.

---

## 7. ReBAC (Relationship-Based Access Control)

ReBAC answers: **"Does this relationship tuple directly grant access?"**

### ReBAC Evaluation

```text
Query: Can user:alice edit document:report?

Step 1: Look for tuple matching (user:alice, editor, document:report)
         Found!  ALLOW_REBAC_DIRECT
```

### ReBAC Use Cases

**Resource ownership and sharing:**

```
(user:alice, owner, document:report)
(user:bob, viewer, document:report)
(user:charlie, editor, document:report)
```

**Team-based access:**

```
(team:engineering, owner, repo:code)
(team:product, viewer, repo:code)
```

**Hierarchical relationships (future graph traversal):**

```
(team:sub-team, parent, team:main-team)
(team:main-team, owner, repo:code)
 Implies: sub-team can access repo:code
```

---

## 8. RBAC (Role-Based Access Control)

RBAC answers: **"Does the subject's role grant this permission?"**

### RBAC Model

```text
User  Role  Permission  allows specific actions

Example:
  user:alice  role:document-editor  permission:document:edit
  user:alice  role:document-editor  permission:document:view
  user:alice  role:document-editor  permission:document:share
```

### RBAC Evaluation

```text
Query: Can user:alice edit a document?

Step 1: Look for tuple (user:alice, editor, document:X)  [ReBAC]
         Not found

Step 2: Check roles of user:alice  [RBAC]
        Found: user:alice has role:editor
        Which includes permission:document:edit
         Found!  ALLOW_RBAC_ROLE
```

### RBAC Use Cases

**System-level permissions:**

```
role:admin  permission:user:delete
role:viewer  permission:document:read
role:admin  permission:audit:export
```

**Fallback when no specific relationship exists:**

```
If user has no specific ReBAC tuple,
Fall back to checking RBAC role permissions
```

---

## 9. Hybrid Decision Flow

Aegis evaluates permissions in a **deterministic sequence**:

```

 Input: (subject, relation, object)
 Tenant Context: tenant-id




    Validate Tuple Input
    Parse subject/object
    Normalize relation


             (Invalid?)
       DENY_INVALID_INPUT

             (Valid)

    Check for Explicit DENY

    (subject, relation, obj)
    effect=DENY?


             YES  DENY_EXPLICIT

             NO

    Check ReBAC Direct Match

    (subject, relation, obj)
    effect=ALLOW?


             YES  ALLOW_REBAC_DIRECT

             NO

    Check RBAC Role Match

    subject's roles have
    required permission?


             YES  ALLOW_RBAC_ROLE

             NO

    Final Decision
     DENY_NOT_FOUND

```

### Decision Codes

| Code                 | Meaning                      | Priority         |
| -------------------- | ---------------------------- | ---------------- |
| `DENY_EXPLICIT`      | Explicit deny tuple matched  | Highest (wins)   |
| `DENY_INVALID_INPUT` | Malformed input              | High             |
| `ALLOW_REBAC_DIRECT` | Direct ReBAC tuple matched   | Medium           |
| `ALLOW_RBAC_ROLE`    | RBAC role permission matched | Medium           |
| `DENY_NOT_FOUND`     | No allow rule found          | Default (lowest) |

---

## 10. Practical Examples

### Example 1: Document Sharing

**Scenario:** Alice shares a document with Bob.

**Tuples created:**

```
Alice owns the document:
  (user:alice, owner, document:report-2024)

Bob gets editor access:
  (user:bob, editor, document:report-2024)
```

**Permission checks:**

```
Check 1: Can user:alice edit document:report-2024?
   Find (user:alice, editor, document:report-2024)
   Find (user:alice, owner, document:report-2024)
   Result: ALLOW (owner typically can edit)

Check 2: Can user:bob edit document:report-2024?
   Find (user:bob, editor, document:report-2024)
   Result: ALLOW

Check 3: Can user:charlie view document:report-2024?
   Find (user:charlie, *, document:report-2024)
   Check RBAC roles for user:charlie
   Result: DENY (not shared, no role)
```

---

### Example 2: Team-Based Access

**Scenario:** Engineering team owns a code repository.

**Tuples:**

```
Team owns repo:
  (team:engineering, owner, repo:aegis-code)

Bob is a member:
  (user:bob, member, team:engineering)

Charlie has explicit viewer access:
  (user:charlie, viewer, repo:aegis-code)

Contractors are explicitly denied:
  (team:contractors, editor, repo:aegis-code, effect=deny)
```

**Permission checks:**

```
Check 1: Can user:bob edit repo:aegis-code?
   Find (user:bob, editor, repo:aegis-code)
   Find (team:engineering, owner, repo:aegis-code)
    AND (user:bob, member, team:engineering)
   Result: ALLOW (via team membership + team ownership)
  [Note: This requires graph traversal, future feature]

Check 2: Can user:charlie view repo:aegis-code?
   Find (user:charlie, viewer, repo:aegis-code)
   Result: ALLOW

Check 3: Can user:contractor edit repo:aegis-code?
   Find explicit deny (team:contractors, editor, repo:aegis-code, effect=deny)
   Result: DENY_EXPLICIT (deny overrides any allow)
```

---

### Example 3: Role-Based Fallback

**Scenario:** Admin role has broad permissions.

**RBAC setup:**

```
role:admin  permission:document:*
role:viewer  permission:document:read
```

**Check without ReBAC match:**

```
Query: Can user:alice read document:anything?

Step 1: Find (user:alice, reader, document:anything)
Step 2: Check if user:alice has role:admin or role:viewer
        role:admin includes permission:document:*
Step 3: Result: ALLOW_RBAC_ROLE
```

---

## 11. Idempotency & Deduplication

Aegis handles **duplicate tuples gracefully**:

```
Request 1: CREATE (user:alice, editor, document:x)   Success
Request 2: CREATE (user:alice, editor, document:x)   OK (idempotent)
Request 3: CREATE (user:alice, editor, document:x)   OK (same tuple)

Result: Only 1 tuple in storage, no duplicates
```

---

## 12. Tenant & Store Relationships

```text
Tenant (isolation boundary)
   Store 1 (authorization context for app/env)
        Relationships (permission tuples)
        AuthorizationModel (schema)
        Audit Logs (decision history)

   Store 2 (authorization context for different app)
        Relationships
        AuthorizationModel
        Audit Logs
```

**Key points:**

- Each tenant is **isolated** from others
- Each tenant can have **multiple stores** (per app, per environment, etc.)
- Each store has its **own relationships and schema**

---

## 13. Type Safety & Validation

Aegis **validates tuple format** on creation:

```http
POST /api/v1/relationships

{
  "subject": "user:alice",       Valid: <type>:<id>
  "relation": "editor",           Valid: non-empty string
  "object": "document:report",    Valid: <type>:<id>
  "effect": "allow"               Valid: allow | deny
}
```

**Invalid inputs are rejected:**

```json
{
  "subject": "invalid",           Missing : separator
  "relation": "",                 Empty relation
  "object": "doc:1:2:3",          Malformed (too many colons)
  "effect": "maybe"               Invalid effect
}
```

---

## Conclusion

The **tuple model** is simple but powerful:

- Easy to understand and explain
- Flexible for many authorization patterns
- Graph-ready (can evolve to transitive evaluation)
- Audit-friendly (clear permission record)

Master the tuple, and you master Aegis authorization.
