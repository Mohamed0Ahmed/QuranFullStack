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
- **A nested door's section is its parent's, and every write that can change a section must maintain that.**
  Reads group and count by `SectionId` at any depth (`../../Reads/Abwab/README.md`) and nothing re-derives
  it, so this is an invariant the write side owes, not a convention:
  - `CreateAsync` **derives** the section from the parent. A null `sectionId` means *unspecified* — `int?`
    cannot tell an omitted field from an explicit null — and a stated one that disagrees with the parent
    is refused (`AbwabSectionParentMismatchException` → `400`), not silently overwritten.
  - `MoveAsync` and `BulkMoveAsync` **cascade** a section change to the moved door's whole subtree,
    `CascadeSectionToDescendantsAsync`. **Archived descendants included** — they keep their `parent_id`
    through soft-delete, and one left behind would later restore into a section its parent has left.
    A live-only cascade passes every test that ignores archived rows, so the discriminating test asserts
    an archived grandchild's `SectionId`.
  - Descendants keep their `parent_id`, so their sibling scope's membership does not change and they need
    no resequencing. For the same reason a descendant can never collide on the unique index: only subtree
    members share that `parent_id`, so `SaveTranslatingWriteExceptionsAsync(door.Name, …)` still names the
    only row that can actually conflict.
  - Deliberate asymmetry: create **rejects** a disagreeing section, move **ignores** `targetSectionId`
    whenever `targetParentId` is set (plan §4, §13.5). Move's pair describes a destination where the plan
    locks parent-wins; create's body is an authored record, where a disagreement is a caller bug worth
    reporting. Do not "harmonize" one into the other.
- **Aliases are replaced wholesale under the door's own token** and soft-deleted, never hard-deleted.
  `AbwabDoorAlias` deliberately has no `xmin` of its own.
- **Descendant walks share one parent map per operation.** `LoadChildrenByParentAsync` projects
  `(id, parent_id)` once; `CollectDescendantIds` is a pure BFS over it. Calling the loader per door turns
  a bulk operation into one full table read per door.
- **Create needs an explicit transaction**; nothing else does. It is the only path with two
  `SaveChangesAsync` calls (the door, then aliases keyed by its generated id), and EF's implicit
  transaction covers one call only.
- **Two independent root orders, zero coupling.** `OrderValue` is per-scope
  (`(section_id, parent_id)`); `GlobalOrderValue` is a second, independent order over **live root
  doors only** — `NULL` at every depth > 0 and for archived doors. Invariant:
  `global_order_value IS NOT NULL ⟺ (parent_id IS NULL AND deleted_at IS NULL)`.
  `ReorderAsync`'s `scope` (`Section` \| `Global`) picks which order a write renumbers — `Section`
  never touches `GlobalOrderValue`, `Global` never touches `OrderValue` — and every other
  root-affecting write (`CreateAsync`, `MoveAsync`, `BulkMoveAsync`, `DeleteAsync`,
  `BulkArchiveAsync`, `RestoreAsync`) maintains the global sequence alongside its own per-scope
  resequence via `MaintainGlobalOrderAsync`/`ResequenceGlobal`. A root moving between sections
  without leaving the root set (`MoveAsync`/`BulkMoveAsync`) leaves `GlobalOrderValue` untouched —
  membership in the root set changed nothing.
- **`ResequenceGlobal` reads every live root on any root-affecting write** — an accepted cost, not
  a violation of "one parent map per operation" above: the sequence is global by definition, so its
  scope query cannot be narrowed the way a `(section_id, parent_id)` scope query narrows.
- **No `UNIQUE` index on `global_order_value`**, for the same reason `order_value` has none:
  renumbering issues one `UPDATE` per row, and a per-statement unique index would transiently
  violate mid-resequence. Do not "harden" this with a unique index.
- **`MaintainGlobalOrderAsync`'s departures and arrivals are handled asymmetrically**, like the
  per-scope resequence above. Its read still shows pre-`SaveChanges` state: a door being archived
  or moved-to-nested still comes back from the read, so it is dropped via `excludeIds`; a door
  being restored or moved nested→root does **not** come back (the read still shows its old
  `deleted_at`/`parent_id`), so it is appended in code, never inferred from the read.
- **Restore appends, in both spaces.** A restored root goes to the end of its per-scope order
  (existing) and the end of the global sequence (new) — never back to a remembered position, since
  resequencing already destroyed it.

## Related

- Read side: `../../Reads/Abwab/` (`EfAbwabTreeReader`) and its `README.md`.
- Contracts and exception types: `application/QuranDashboard.Application.Abstractions/Abwab/`.
- Handlers: `application/QuranDashboard.Application/Abwab/Commands/`.
- Controllers and status mapping: `api/QuranDashboard.Api/Controllers/Abwab/`
  (`../../../../api/QuranDashboard.Api/Controllers/README.md`).
- Domain entities: `Backend/domain/QuranDashboard.Domain/Abwab/`.
- Tests: `Backend/tests/QuranDashboard.Tests/Abwab/` (writer behavior) and
  `Backend/tests/QuranDashboard.Tests/Smoke/SmokeAbwabWriteTests.cs` (status/envelope contract).
