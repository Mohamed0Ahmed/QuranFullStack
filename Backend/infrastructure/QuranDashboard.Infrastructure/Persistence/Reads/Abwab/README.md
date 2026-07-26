# Abwab reads (infrastructure) — `029`

EF-backed implementations of the two `029` read ports (`Application.Abstractions/Abwab/Core/`).

## What is here

- **`EfAbwabCoreReadPort`** — `IAbwabCoreReadPort`. Builds the versioned `AbwabTreeSnapshotDto`
  (sections + the `كل الأبواب` category projection, independent root orders, ancestry/depth) and
  category search over normalized name + aliases. Always reads the **full** product, protection
  detail included via `AbwabProtectionSummaryProjector` — permission redaction is a separate,
  later backend projection step (`Application/Abwab/Tree/AbwabCompositeReadRedactor.cs`); this port
  never redacts.
- **`EfManualProtectionReadPort`** — `IManualProtectionReadPort`. Fetches the raw context
  `ProtectionResolver` resolves from: the target category (including a **soft-deleted** one — no
  `IsDeleted` filter, so an authorized viewer/lifter can still address it by immutable `CategoryId`),
  the active `ManualProtection` candidates along its current `AncestorIds`, and the revision-state
  row.

- **`EfAbwabRelationshipReadPort`** (`030`) — `IAbwabRelationshipReadPort`. Two projections over
  `abwab_category_relationships`: the per-category actionable list, and the dormant counts for an
  affected-category set. Both apply the **same derived dormancy rule** — a row whose endpoint
  category is soft-deleted is filtered out of the actionable list and counted as dormant — so a
  category operation-restore re-exposes the identical rows with no relationship-side write.
  `includeDeleted` widens the list to soft-deleted relationships (needed to drive restore); dormancy
  filtering still applies on top of it.

- **`EfAbwabTemplateReadPort`** (`030`) — `IAbwabTemplateReadPort`. Three projections over the
  DoorTemplate aggregate: the template list (with active node counts), the template detail (nodes
  ordered by explicit `SiblingOrder`, each with its aliases, plus the current `TreeRevision` the
  apply command needs), and the **separate template-history** projection. History is deliberately its
  own port method rather than a main product-audit read, because §6.3 keeps template CRUD out of the
  main log entirely — it selects only events carrying the `template.history.` action prefix.
  The history projection is **capped at `IAbwabTemplateReadPort.MaxHistoryEntries` (100)** and reports
  truncation through `TemplateHistoryDto.HasMore` — never silently. One row beyond the cap is fetched
  to decide that flag, so learning it costs no second scan. A template's history grows without bound
  as it is edited and every entry carries a full before/after tree, so an uncapped read would return a
  payload no caller can bound.
  **Known cost, recorded not hidden**: that projection matches two substrings against the append-only
  `abwab_audit_events.payload` text column, so the *scan* is unindexable and degrades as the log
  grows even though the *response* is bounded. The fix is first-class indexed action-kind/aggregate-id
  columns on the audit event, but that table is `028` kernel substrate and the audit read model is
  `033`'s — neither is `030`'s to reshape. Frozen in T075's budget file (constant 2 queries, p95 6 ms)
  and handed to `033` with that split stated.

## Query budget

`EfManualProtectionReadPort.GetProtectionContextAsync` issues a constant **3 SQL queries** regardless
of tree depth — it reads the denormalized `AncestorIds` column directly instead of walking parent
links, so there is no N+1. The measured baseline and the deep-tree proof live in
`Application/Abwab/Protection/README.md` (§"Query budget (deep-tree)"), since that is where the
resolver consuming this port is documented.

## Related

- Port contracts: `Application.Abstractions/Abwab/README.md`.
- Domain entities: `Domain/Abwab/README.md`.
- Redaction: `Application/Abwab/README.md` (`AbwabCompositeReadRedactor`).
- Contracts: `specs/029-abwab-core/contracts/tree-read-contract.md`.
