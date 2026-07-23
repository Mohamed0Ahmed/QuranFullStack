# Abwab core contracts (application abstractions) — `029`

**Layer:** Application.Abstractions · **Feature:** `029-abwab-core` · **HOW rules:**
`Backend/.architecture/CLEAN_ARCHITECTURE.md`

Ports, commands, and DTOs the `029` core vertical is built from. This project has no
implementation — `QuranDashboard.Application` implements the handlers, `QuranDashboard.Infrastructure`
implements the EF-backed ports/adapters, and `QuranDashboard.Api`/the (future) frontend core mock both
target the same interfaces so backend and frontend stay in parity by construction. The root-level
`Abwab/*.cs` files here (`AbwabWriteRequest`, `AbwabConflictCodes`, `IAbwabWriteExecutor`, the
`Abwab*Exception` family, `IServerClock`, …) are the `028` write-kernel contracts; `029` only adds
under `Core/` and `Restore/`.

## `Core/` — the read/write port contracts

- **`IAbwabCoreReadPort`** — tree snapshot + category search. Every actionable read DTO
  (`AbwabTreeSnapshotDto`, `CategorySearchResultDto`) carries `ExpectedTimelineGeneration`; a
  mutation command echoes it back so callers can prove they mutated against the version they read.
  The port always returns the **full** product shape — permission-based redaction is a separate,
  later projection (`Application/Abwab/Tree/AbwabCompositeReadRedactor.cs`), never done in the port
  or its EF implementation.
- **`IAbwabCoreWritePort`** — every section/category/protection mutation (add/edit/reorder/delete,
  single/bulk move, subtree delete/operation-restore, alias add/edit/remove, manual protection
  apply/lift/full-preset). Every command under `Core/Commands/` carries `ExpectedTimelineGeneration`
  and, where the target is a specific row/tree state, the expected `xmin`/`TreeRevision`.
- **`IManualProtectionReadPort`** — protection context fetch for the resolver, including by
  immutable `CategoryId` for a **soft-deleted** target (never filters by `IsDeleted`) — the narrow
  security-surface exception that lets an authorized viewer/lifter still address a deleted category.
- **`ISectionWriteStore` / `ICategoryTreeStore` / `ICategorySearchAliasWriteStore` /
  `IManualProtectionWriteStore`** — the narrow persistence seams the Application handlers use instead
  of talking to EF directly (Dependency Inversion — Application depends on these, Infrastructure
  implements them).
- **`IDeletionReservationChecker`** — the reservation seam a subtree delete consults before
  proceeding. `029` ships `InertDeletionReservationChecker` (always "not reserved"); `032` replaces
  it with a Pending-aware checker that can map to `abwab.category_reserved_by_pending`. The seam
  exists so `032` never has to touch the delete handler itself.
- **`ManualProtectionResolution`** — the one shared, pure direct/inherited resolution rule (walks
  `AncestorIds` outward for the nearest `Subtree`-scoped ancestor). Both `ProtectionResolver`
  (single-category, `Application/Abwab/Protection/`) and `AbwabProtectionSummaryProjector` (batch,
  used by the tree/search composite read) call this same function so the rule is never duplicated
  or allowed to drift between the single-read and batch-read paths.
- DTOs (`*SnapshotDto`, `*ProfileDto`, `*SummaryDto`, …) are the wire/contract shapes the frontend
  core mock and HTTP adapter both target for parity (`specs/029-abwab-core/contracts/`).

## `Restore/` — the §8 registry contracts

`IAbwabRestoreAdapter<TSnapshot>` (capture/reconstruct a versioned, schema-tagged snapshot) and
`IAbwabRestoreAdapterDescriptor` (the DI-discoverable `PersistedType`/`SnapshotSchemaVersion` used by
the static registry test). Implementations, the registered adapter list, and the acceptance status
live in `Infrastructure/Abwab/Restore/README.md` — this folder only defines the shape.

## Related

- Domain entities: `QuranDashboard.Domain/Abwab/README.md`.
- Application handlers implementing these ports: `QuranDashboard.Application/Abwab/README.md`.
- EF-backed read ports: `Infrastructure/Persistence/Reads/Abwab/README.md`.
- Restore adapters: `Infrastructure/Abwab/Restore/README.md`.
- API surface consuming these ports: `Api/Abwab/README.md`.
