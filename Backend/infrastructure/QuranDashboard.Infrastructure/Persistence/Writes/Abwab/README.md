# Abwab write path

**Layer:** Infrastructure · write seam · **HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`, `API_GUIDELINES.md`

## What this area does

`Persistence/Writes/` is the repository's **first** write area, and this folder is its only occupant:
`EfAbwabSectionsWriter` and `EfAbwabDoorsWriter` back the eleven `/api/abwab` write endpoints
(`api/QuranDashboard.Api/Controllers/Abwab/`). The read sibling is `../../Reads/Abwab/`. Conventions
established here are the precedent for every later write feature — change them deliberately, not
incidentally.

## Key pieces

- `EfAbwabSectionsWriter` — create / rename / delete-empty. Implements `IAbwabSectionsWriter`.
- `EfAbwabDoorsWriter` — create / edit / move / reorder / bulk-move / bulk-archive / archive / restore.
  Implements `IAbwabDoorsWriter`.

## Conventions and invariants (read before changing)

- **One seam per aggregate, no EF types cross it.** Application never references EF Core, so each writer
  catches `DbUpdateConcurrencyException` and `PostgresException` `23505` itself and rethrows the plain
  types in `Application.Abstractions/Abwab/` (`AbwabStaleVersionException`, `AbwabDuplicateNameException`,
  …). **Every** `SaveChangesAsync` in this folder goes through one of the two translating helpers —
  a bare save is how a raw EF exception reaches the global handler as a 500 instead of a 409.
  - `SaveTranslatingWriteExceptionsAsync` — writes that put a row **into** the unique index's live scope
    (create, edit, move, bulk-move, restore): both a stale token and a duplicate name are reachable.
  - `SaveTranslatingConcurrencyAsync` — writes that only move a row **out** of it (archive, reorder,
    section delete): a duplicate-name violation is structurally impossible, so only the token can fail.
- **Optimistic concurrency is Postgres `xmin`.** The client's last-seen token is applied as
  `db.Entry(x).Property(x => x.Version).OriginalValue`, never `CurrentValue` — overriding `CurrentValue`
  would compare the row against the value the writer's own query just re-read, which can never conflict.
  Bulk writes set it per row, which is what makes them all-or-nothing: one stale row fails the batch.
- **Every write leaves its sibling scope at `1..N`.** Use the shared `Resequence` helper. Two traps, both
  already handled and both easy to reintroduce:
  - A scope query hits the **database**, which still shows a moved/archived door's OLD `section_id` /
    `parent_id` until `SaveChanges`. Exclude the rows you are about to move, or they get renumbered as
    if they never left.
  - When a move's destination **is** the door's current scope, "existing live count + 1" counts the door
    twice and yields `{1..N-1, N+1}`. Read the destination with the door excluded, then renumber
    destination-plus-door together. `MoveAsync` and `BulkMoveAsync` both do this;
    `AbwabDoorWriteBehaviorTests` has a discriminating test for each.
  - Restore is the only write that puts a row **back into** a scope, so it renumbers too. Its scope was
    left at `1..N-1` by the archive that removed the door.
- **Archive claims a subtree; restore returns exactly what that archive claimed.** `ArchiveSubtreeAsync`
  only touches **live** descendants, so a descendant archived earlier by a separate operation is not part
  of the claim. `RestoreAsync` therefore matches descendants on the archive's own `deleted_at` timestamp,
  captured before the door's is cleared. **Do not widen this back to "all archived descendants"** — that
  resurrects rows the user archived deliberately.
- **Restore detaches a door whose section was archived meanwhile.** A section can only be archived once it
  holds no live doors, and sections have no restore route in this slice, so refusing would strand the door
  permanently. It is moved to "outside every section" (`section_id = null`) — a first-class state, plan
  §R8 — along with everything restored with it, because a nested door always inherits its parent's section.
  The detach is **reported, not silent**: `RestoreAsync` returns `AbwabRestoredDoorDto`, whose
  `DetachedFromArchivedSection` is the caller's only signal. A null `section_id` on its own is ambiguous —
  a door that never belonged to a section looks identical — and the caller does not hold the prior state.
- **Aliases are replaced wholesale under the door's own token** and soft-deleted, never hard-deleted.
  `AbwabDoorAlias` deliberately has no `xmin` of its own.
- **Descendant walks share one parent map per operation.** `LoadChildrenByParentAsync` projects
  `(id, parent_id)` once; `CollectDescendantIds` is a pure BFS over it. Calling the loader per door turns
  a bulk operation into one full table read per door.
- **Create needs an explicit transaction**; nothing else does. It is the only path with two
  `SaveChangesAsync` calls (the door, then aliases keyed by its generated id), and EF's implicit
  transaction covers one call only.

## Related

- Read side: `../../Reads/Abwab/` (`EfAbwabTreeReader`) and its `README.md`.
- Contracts and exception types: `application/QuranDashboard.Application.Abstractions/Abwab/`.
- Handlers: `application/QuranDashboard.Application/Abwab/Commands/`.
- Controllers and status mapping: `api/QuranDashboard.Api/Controllers/Abwab/`
  (`../../../../api/QuranDashboard.Api/Controllers/README.md`).
- Domain entities: `Backend/domain/QuranDashboard.Domain/Abwab/`.
- Tests: `Backend/tests/QuranDashboard.Tests/Abwab/` (writer behavior) and
  `Backend/tests/QuranDashboard.Tests/Smoke/SmokeAbwabWriteTests.cs` (status/envelope contract).
