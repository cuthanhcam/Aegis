# Demo Data Guide

Aegis development seeding creates a realistic dataset for trying the dashboard and API.

The seed data is intended for local development, demos, UI validation, and integration testing. It is not production data.

## Demo Tenants

| Tenant | Purpose |
| --- | --- |
| `default` | Main demo tenant for docs, billing, and support examples |
| `tenant-dev` | Developer tenant for lab and analytics examples |

## Demo Users

Common seeded users:

| User | Tenant | Typical Role |
| --- | --- | --- |
| `user:admin` | `default` | Admin and owner examples |
| `user:anne` | `default` | Document editor |
| `user:bob` | `default` | Document viewer through team membership |
| `user:carol` | `default` | Deny example |
| `user:finance` | `default` | Billing analyst |
| `user:lead` | `default` | Support manager |
| `user:agent1` | `default` | Support agent |
| `user:agent2` to `user:agent8` | `default` | Additional support agents |
| `user:reviewer1` to `user:reviewer6` | `default` | Document reviewers |
| `user:dev` | `tenant-dev` | Lab maintainer and analytics owner |
| `user:intern` | `tenant-dev` | Lab viewer and analytics analyst |

## Demo Stores

| Store | Tenant | Domain |
| --- | --- | --- |
| `store-docs-default` | `default` | Document collaboration |
| `store-billing-default` | `default` | Billing account access |
| `store-support-default` | `default` | Support tickets and queues |
| `store-lab-tenant-dev` | `tenant-dev` | Developer sandbox projects |
| `store-analytics-tenant-dev` | `tenant-dev` | Analytics dashboards |

## Working Check Examples

### Documents

Store:

```text
store-docs-default
```

Useful tuples:

```text
user:admin owner document:roadmap
user:anne editor document:roadmap
user:carol viewer document:roadmap deny
user:bob viewer document:design-spec
```

Try:

```json
{
  "user": "user:anne",
  "relation": "viewer",
  "object": "document:roadmap"
}
```

### Billing

Store:

```text
store-billing-default
```

Useful tuples:

```text
user:admin admin account:acme
user:finance analyst account:acme
user:finance analyst account:customer-1
```

Try:

```json
{
  "user": "user:finance",
  "relation": "viewer",
  "object": "account:acme"
}
```

### Support

Store:

```text
store-support-default
```

Useful tuples:

```text
user:agent1 assignee ticket:INC-1001
user:lead manager queue:enterprise
user:agent2 assignee ticket:INC-1101
```

Try:

```json
{
  "user": "user:agent1",
  "relation": "viewer",
  "object": "ticket:INC-1001"
}
```

### Lab

Store:

```text
store-lab-tenant-dev
```

Useful tuples:

```text
user:dev maintainer project:aegis-lab
user:intern viewer project:aegis-lab
```

Try:

```json
{
  "user": "user:intern",
  "relation": "viewer",
  "object": "project:aegis-lab"
}
```

### Analytics

Store:

```text
store-analytics-tenant-dev
```

Useful tuples:

```text
user:dev owner dashboard:executive
user:intern analyst dashboard:quality
```

Try:

```json
{
  "user": "user:intern",
  "relation": "viewer",
  "object": "dashboard:quality"
}
```

## Graph Explorer Presets

The dashboard Graph screen uses store-aware presets:

| Store Family | list-users Object | list-objects User | Object Type |
| --- | --- | --- | --- |
| Docs | `document:roadmap` | `user:anne` | `document` |
| Billing | `account:acme` | `user:finance` | `account` |
| Support | `ticket:INC-1001` | `user:agent1` | `ticket` |
| Lab | `project:aegis-lab` | `user:intern` | `project` |
| Analytics | `dashboard:quality` | `user:intern` | `dashboard` |

## Notes

- Seed data is idempotent and can be re-run safely in development.
- Demo data is intentionally varied so every major dashboard screen has something to display.
- If a graph query returns `type_not_found`, confirm the active store matches the object type in the request.

