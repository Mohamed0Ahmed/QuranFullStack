# Abwab Notification Storage (028 US6)

Durable, transaction-joining, duplicate-safe notification **storage** for the Abwab substrate.
This area owns persistence only. It is the storage that `032` (notification surfaces + the
normal event matrix) and `033` (restore-event emission) build on.

## What is here

- `NotificationStorageWriter` — the transaction-capable persistence writer. It **joins the
  caller's domain transaction**: it writes through the caller's scoped `QuranDashboardDbContext`
  and never begins, commits, or rolls back a transaction of its own. A rolled-back caller
  therefore leaves **no** notification row (a notification can never commit for a rolled-back
  action). Duplicate prevention is by **unique source identity** (the idempotency key): a second
  write with the same source identity is deduplicated (`DuplicateIgnored`), backed hard by the
  unique index on `source_identity`.
- `NotificationReadStateRepository` — the low-level recipient/read-state repository. Read state
  is a **plain mutable table kept outside product audit/restore**: read/unread toggles are not
  routed through the ChangeSet/audit kernel and save directly.
- `NotificationWriteRequest` / `NotificationWriteResult` / `NotificationWriteOutcome` — the
  writer's input/output shapes (infrastructure-local; not a public port).
- `NotificationStorageDependencyInjection` — registers the concrete writer + repository as
  scoped so they share the request `DbContext`. No interface/port is registered.

Domain entities live in `QuranDashboard.Domain/Abwab/Notifications/`
(`NotificationRecord`, `NotificationReadState`); EF configurations live in
`Persistence/Configurations/Abwab/` alongside the rest of the Abwab substrate; the schema is
created by the `AddAbwabNotificationStorage` migration.

## Boundaries / invariants (must not break)

- **Storage only — no public notification surface.** 028 introduces **no** public notification
  port (Application.Abstractions interface), endpoint (Api controller/endpoint), mock, HTTP
  adapter, or frontend UI. `032` owns surfaces and the event matrix; `033` calls the storage
  writer for restore events. This boundary is enforced by a source-scan gate,
  `Backend/tests/QuranDashboard.Tests/Abwab/Notifications/NotificationBoundaryGuardTests.cs`
  (T070) — it fails if a notification controller/port/HTTP adapter/mock/UI appears.
- **Transaction-join.** The writer must never own its own transaction; it enlists in the
  caller's unit of work so rollback removes the notification.
- **Unique source identity.** The `source_identity` unique index is the dedup backstop; keep it.
- **Read state is outside audit/restore.** `NotificationReadState` is a plain mutable table:
  not `IAbwabAuditable`, no append-only trigger, no restricted-role revoke. Do **not** route it
  through the ChangeSet/audit kernel or make it append-only.
- **No Quran foreign key.** Neither table references any Quran table. `NotificationReadState`
  references `NotificationRecord` (a within-Abwab FK only). The FK-prohibition guard stays green.

## Verified by (real-PostgreSQL tests, `Backend/tests/.../Abwab/Notifications/`)

- `TransactionJoinTests` — rolled-back caller → 0 rows; committed caller → 1 row (T065).
- `DedupAndReadStateTests` — dedup by source identity + unique-index backstop; read state is
  not audited, does not advance the audit head, and is mutable (T066).
- `NotificationBoundaryGuardTests` — no public notification surface is introduced (T070).
