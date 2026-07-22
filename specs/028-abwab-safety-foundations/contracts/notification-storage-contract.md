# Contract: Durable Notification Storage (Story 6)

**Source**: Master Plan §18.2 step 6 / exit. **Storage only** — this feature exposes **no**
public port, endpoint, mock, HTTP adapter, or UI.

## Obligations

- Provide a **recipient / source / idempotency** schema with **read state**.
- Provide a **transaction-capable persistence writer** that **joins a caller's domain
  transaction** (so a notification cannot commit for a rolled-back action).
- Provide a **low-level recipient/read-state repository**.
- **Unique source identity** prevents duplicate notifications.
- Notification **read state is kept outside** product audit/restore.

## Ownership boundaries

- **No** public notification port/endpoint/mock/HTTP adapter/UI here.
- `032` owns notification **surfaces** and the normal **event matrix**.
- `033` calls this **storage writer** for restore events.

## Test anchors

- Writer joins a caller's transaction (rolled-back caller → no notification row).
- Two writes with the same source identity → duplicate prevented.
- Read state is outside product audit/restore.
- No accidental public notification surface is introduced by `028`.
