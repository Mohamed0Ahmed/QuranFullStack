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
