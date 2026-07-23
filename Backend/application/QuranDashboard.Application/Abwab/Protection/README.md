# Abwab protection (application) — `029` US2 resolver + US3 writers

`ProtectionResolver` resolves `ManualProtection` (direct/inherited) and the ordinary
24-hour protection window for a category, given raw context fetched through
`IManualProtectionReadPort` (`Backend/application/QuranDashboard.Application.Abstractions/Abwab/Core/`).
It is read-only; the direct/inherited inheritance rule itself now lives as the shared, pure
`ManualProtectionResolution.Resolve(...)` in `Application.Abstractions/Abwab/Core/` so both this
resolver (single-category, read-port-backed) and the Infrastructure-side batch composite-read
projector (`AbwabProtectionSummaryProjector`, tree/search snapshot) apply the exact same rule
without duplicating it across layers.

## US3 writers (this folder)

- `ManualProtectionWriterHandler` — apply/lift a single type. Same-scope apply is idempotent
  (`AbwabAuditedOperationOutcome.NoOp`, no ChangeSet); a scope change requires the existing
  record's Expected Version and becomes one audited edit; any mismatch (including a caller with
  no version at all) maps to `abwab.manual_protection_scope_conflict` — never the generic
  `abwab.row_stale` for this one operation (§11 gives it its own code). Lift requires the row's
  Expected Version and maps a mismatch to `abwab.row_stale`.
- `FullProtectionPresetHandler` — the five-type preset. One selected scope idempotently
  upserts all five `ManualProtectionType` records for a category in one operation delegate (one
  transaction): matching-scope records are untouched, missing types are inserted, differing-scope
  records require their own Expected Version or the whole command rolls back
  (`abwab.manual_protection_scope_conflict`) — nothing partially applies. All five already
  matching is an idempotent no-ChangeSet success.
- `CategoryProtectionGate` (`Application/Abwab/Categories/`) is the shared consumer: it calls
  `ProtectionResolver` for the manual-protection block (`abwab.manual_protection`) and, for
  direct-content edits/moves only, additionally checks the ordinary 24-hour window (last
  protected editor or an active System Owner only, else `abwab.ordinary_protection`) and starts/
  restarts that window on success. Pure reorder, subtree delete, and operation-restore never
  consult or touch the ordinary window (§9) — only `CategoryProtectionGate.
  EnsureNotManuallyProtectedAsync` (manual-only, no window) applies there.
- Both writer commands (`ApplyManualProtectionCommand`, `LiftManualProtectionCommand`,
  `ApplyFullProtectionPresetCommand`) live in `Application.Abstractions/Abwab/Core/Commands/`
  alongside `IAbwabCoreWritePort`, not in this folder — the port contract is what the (future)
  US4 frontend core mock mirrors for parity.

## Resolution

- **Direct**: the target category itself carries an active `ManualProtection` of the
  requested type — `SourceCategoryId` is the category itself.
- **Inherited**: no direct record, but the **nearest** ancestor (walking `AncestorIds`
  from parent outward) carries an active record of that type with `ProtectionScope.Subtree`
  — a `CategoryOnly`-scoped ancestor record never inherits to descendants.
- Inheritance is evaluated from the category's **current** `AncestorIds` column (denormalized on
  the row), never a stored descendant snapshot — a move changes resolution immediately.
- **Action classification** (`ProtectionActionClassification`): `ManuallyProtected` (direct or
  inherited manual protection — always wins) > `OrdinaryWindowActive` (the category's own
  24-hour window, server-clock-derived: `OrdinaryProtectionLastEditedAtUtc + 24h > IServerClock.UtcNow`)
  > `Unprotected`. The last-editor/System-Owner exemption from the ordinary window is a **US3
  writer** concern, not resolved here — this resolver reports protection presence only.
- `ResolveProfileAsync` resolves all five `ManualProtectionType` values plus the ordinary window
  in one call; `ResolveTypeAsync` resolves a single type. Both share the same read-port fetch and
  therefore the same query cost.
- Soft-deleted categories resolve normally: the read port never filters by `IsDeleted`, so an
  effective-protection read or an authorized lift can still address a category by its immutable
  `CategoryId` after deletion (§7.2 narrow security surface) — see `IManualProtectionReadPort`.

## Query budget (deep-tree)

`EfManualProtectionReadPort.GetProtectionContextAsync` issues a **constant 3 SQL queries**
regardless of tree depth (category-by-id lookup, active-protection-candidates lookup keyed by
`CategoryId` + current `AncestorIds`, revision-state lookup) — it reads the denormalized
`AncestorIds` column directly and never walks parent links row-by-row, so there is no N+1.

**Measured baseline**: 3 queries, confirmed identical at depth 5 and depth 200 on real
PostgreSQL (`Backend/tests/QuranDashboard.Tests/Abwab/Protection/DeepTreeBudgetTests.cs`).
The test asserts a budget of baseline + 2 (5) as a stable margin over the measured number,
not an invented threshold (§18.3 fixes no numeric limit). Cited by the feature completion
report (T078).
