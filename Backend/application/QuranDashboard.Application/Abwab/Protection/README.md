# Abwab protection resolver (application) — `029` US2

`ProtectionResolver` resolves `ManualProtection` (direct/inherited) and the ordinary
24-hour protection window for a category, given raw context fetched through
`IManualProtectionReadPort` (`Backend/application/QuranDashboard.Application.Abstractions/Abwab/Core/`).
It builds no writer/mutation surface — read-only resolution only, consumed by `029` US3's
writers and `031`+ protection UI.

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
