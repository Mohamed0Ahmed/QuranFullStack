# Abwab write path

**Layer:** Infrastructure · write seam · **HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`, `API_GUIDELINES.md`

## What this area does

`Persistence/Writes/` is the repository's **first** write area, and this folder is its only occupant:
five writers back the twenty-one `/api/abwab` write endpoints
(`api/QuranDashboard.Api/Controllers/Abwab/`). The read sibling is `../../Reads/Abwab/`. Conventions
established here are the precedent for every later write feature — change them deliberately, not
incidentally.

## Key pieces

- `EfAbwabSectionsWriter` — create / rename / reorder / delete-empty. Implements `IAbwabSectionsWriter`.
- `EfAbwabDoorsWriter` — create / edit / move / reorder / bulk-move / bulk-archive / archive / restore.
  `InvalidatingAbwabDoorsWriter` (`../../../Caching/Abwab/`) decorates it one-for-one, so a signature
  change here — `RestoreAsync` gaining its destination `sectionId`, for instance — is a change there too.
  Implements `IAbwabDoorsWriter`.
- `EfAbwabRelationsWriter` — add N relations in one call / soft-delete one. Implements
  `IAbwabRelationsWriter`.
- `EfAbwabTemplatesWriter` — template create / soft-delete, and node add / edit / reorder / delete.
  Implements `IAbwabTemplatesWriter`.
- `EfAbwabTemplateApplyWriter` — copies a template subtree into N target doors. Implements
  `IAbwabTemplateApplyWriter`. **The one writer whose seam crosses two aggregates** — see below.
- `AbwabAliasNormalization` — the one alias rule, shared. Not a writer.

## Conventions and invariants (read before changing)

- **One seam per aggregate, no EF types cross it.** Application never references EF Core, so each
  writer catches `DbUpdateConcurrencyException` and `PostgresException` `23505` itself and rethrows
  the plain types in `Application.Abstractions/Abwab/` (`AbwabStaleVersionException`,
  `AbwabDuplicateNameException`, `AbwabRelationDuplicateException`, …). **A save that can raise
  `DbUpdateConcurrencyException` or a `23505` MUST translate it** — through one of the helpers below,
  or inline where the answer is `false` rather than an exception. An untranslated save is how a raw EF
  exception reaches the global handler as a 500 instead of a 409. What decides whether a save needs
  this is what EF can raise, not what the client sent: an entity mapped `IsRowVersion()`
  (`../../Configurations/Abwab/`) puts `xmin` in every UPDATE's WHERE clause whether or not the writer
  sets `OriginalValue`.
  - `SaveTranslatingWriteExceptionsAsync` — writes that put a row **into** the unique index's live scope
    (create, edit, move, bulk-move, restore): both a stale token and a duplicate name are reachable.
    Its `name` parameter — in `EfAbwabDoorsWriter` and in `EfAbwabSectionsWriter` — is **inert**:
    `AbwabDuplicateNameException` carries no name and its message names no row
    (`Application.Abstractions/Abwab/AbwabDuplicateNameException.cs`), and `BulkMoveAsync` already
    passes `null`. Drop it or use it; do not write a caller that assumes the `409` says which row.
  - `SaveTranslatingConcurrencyAsync` — writes that only move a row **out** of it (archive, reorder,
    section delete): a duplicate-name violation is structurally impossible, so only the token can fail.
  - `EfAbwabRelationsWriter.SaveTranslatingDuplicateAsync` — its **own** third helper, deliberately not
    one of the two above. Those are keyed to the doors/sections duplicate-**name** message and would
    report the wrong constraint entirely; the relations index is `(door_a_id, door_b_id, relation_type)`.
    Relation writes carry no version token (see below), so a stale-token branch would be unreachable code.
  - `EfAbwabTemplatesWriter.SaveTranslatingDuplicateNameAsync` — same shape, keyed to the **node** name
    under its parent. No stale-token branch either: no templates route carries a version token.
  - `EfAbwabTemplateApplyWriter.SaveTranslatingDuplicateNameAsync` — here the duplicate genuinely **is**
    a door name, the inverse of the relations case. It is still its own helper because it is only ever
    a race backstop: the pre-check below already refused every collision it could see.
  - **The two soft-delete saves translate INLINE, not through a helper.**
    `EfAbwabRelationsWriter.DeleteAsync` and `EfAbwabTemplatesWriter.DeleteAsync` catch
    `DbUpdateConcurrencyException` around their own save and answer `false`, because a concurrent
    delete winning is not a caller error — the row is gone either way. See the delete bullets below.
  - **Saves with nothing to raise, so nothing to translate.** `EfAbwabDoorsWriter.CreateAsync` and
    `EditAsync` flush the alias diff in a second, bare save inside their own transaction, and
    `EfAbwabTemplatesWriter.CreateAsync` saves the template then its root node the same way. The alias
    flushes are safe **while** `abwab_door_aliases` carries no version token and no unique index
    (`../../Configurations/Abwab/AbwabDoorAliasConfiguration.cs`) and `ReplaceAliasesAsync` only ever
    soft-deletes; **add a per-door unique alias index and the two alias flushes become 500s where the
    contract says 409** — route them through the helper in the same change. The template pair is safe
    because both are inserts into a template row generated moments earlier in the same transaction, so
    neither the one-live-root index nor the `(template_id, parent_node_id, name)` sibling-name index
    (`../../Configurations/Abwab/AbwabTemplateNodeConfiguration.cs:81-88`) has an existing row to
    collide with.
  - **`EfAbwabTemplatesWriter.ReorderNodeAsync` and `DeleteNodeAsync` are a GAP, not a decision.**
    `AbwabTemplateNode` is row-versioned
    (`../../Configurations/Abwab/AbwabTemplateNodeConfiguration.cs:68-69`), so a lost race on their bare
    save raises `DbUpdateConcurrencyException` and the global handler answers `500`. No templates
    route carries a version token, so there is no stale-token `409` to map it to — the honest outcome
    has to be chosen before the save is wrapped.
- **The one exception to "per aggregate" is a use-case seam.** `EfAbwabTemplateApplyWriter` reads
  `abwab_template_nodes` and writes `abwab_doors`, so it belongs to neither aggregate's writer. That
  is the rule bending to `BACKEND_STRUCTURE.md` §4's own instruction to "split large repositories by
  aggregate, feature, read model, **or use case**": the copy is a use case, and `EfAbwabDoorsWriter`
  is already past that section's 600-line hard threshold — the split it owes is tracked as
  `docs/TESTING_DEBT.md` row J1 — so hanging it there was never available. Every other writer here
  stays one-aggregate.
- **Every writer interface here is DI-wrapped by an invalidating decorator, and that is not
  optional.** `Infrastructure/Caching/Abwab/Invalidating*Writer` wraps each of the five interfaces and
  bumps the cache generation the write's data belongs to: sections / doors / relations / **apply** bump
  the tree, the templates writer bumps templates. Two rules make it correct rather than merely present:
  - **The bump is in `finally`, not on success.** Several writes here run multiple saves on implicit
    transactions, so a thrown translated exception does not prove nothing committed. Bumping after a
    failed write costs one spurious refetch; not bumping after a partially committed one serves stale
    data.
  - **The bump happens after the inner writer returns — i.e. after its commit — and before the handler
    resumes**, so a client that has just written can never be answered `304` or handed a pre-write
    body. This ordering is by construction, not by discipline; no handler or controller calls the
    invalidator.
  - **A sixth writer, or a new method on any of these five, MUST be added to its decorator.** The
    compile error is the guard — an interface cannot grow without the decorator failing to build — and
    the `finally` bump is the line to check in review. A writer registered without its decorator would
    silently reintroduce stale reads with every test still green.
- **Aliases are normalized once, at the write seam, by `AbwabAliasNormalization`.** Trim, drop the
  empties, de-duplicate — and every alias write in this folder goes through it: the doors writer's
  `ReplaceAliasesAsync` diff, the template node writes that store a `text[]`, and the apply's alias
  inserts. It is one helper because the alternative is measurable, not theoretical: template nodes
  once stored `"  دمج  "` and `""` verbatim while the copy silently dropped them, so a template and
  the doors copied from it disagreed about their own aliases. A stricter rule later (case folding,
  Unicode normalization) has to land in one place or that divergence comes back.
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
- **`EfAbwabSectionsWriter.ReorderAsync` resequences the WHOLE table, not a scope — sections have
  one order space, not `(section_id, parent_id)`.** Every reorder therefore stales every other live
  section's `xmin`, exactly like a `Global` door reorder; the frontend's refresh-after-write
  invariant (`features/abwab/README.md`) is what keeps the next write correct, not anything here.
  **The ordered read tie-breaks on `Id`, not a bare `OrderBy(OrderValue)`** — a deliberate deviation
  from `EfAbwabDoorsWriter`'s section-scope reorder, because `EfAbwabTreeReader` tie-breaks the same
  way (`../../Reads/Abwab/README.md`) and `CreateAsync`'s `count(live) + 1` alongside `DeleteAsync`'s
  non-resequencing delete leave duplicate `OrderValue`s reachable today. Matching the reader's own
  order is what makes "position 3" mean the third row on screen even while a duplicate exists — the
  reorder also heals the sequence back to `1..N` whenever it runs. That duplicate-`OrderValue`
  condition is not fixed here (`docs/TESTING_DEBT.md` rows F1/F2); it is worked around.
- **Archive claims a subtree; restore returns exactly what that archive claimed.** `ArchiveSubtreeAsync`
  only touches **live** descendants, so a descendant archived earlier by a separate operation is not part
  of the claim. `RestoreAsync` therefore matches descendants on the archive's own `deleted_at` timestamp,
  captured before the door's is cleared. **Do not widen this back to "all archived descendants"** — that
  resurrects rows the user archived deliberately.
- **Every door row carries a section, and restore is the only write that may change one without a move.**
  `RestoreAsync` takes a destination `sectionId`, and resolves it in two quite different ways:
  - **A root** keeps its stored section when the caller states nothing and that section is still live. If
    the section was retired meanwhile — legal, since a section is archivable once it holds no LIVE doors,
    and sections have no restore route — the stored value is not a destination and the caller is refused
    (`AbwabSectionRequiredException` → `400`). A stated live section always wins, which is what makes such
    a door restorable at all. Do NOT reintroduce the old detach-to-`section_id = null` behavior: nothing
    is "outside every section" any more, and the column forbids it.
  - **A child** derives from its live parent's **CURRENT** section, read fresh — never the value stored on
    the archived row. If an ancestor was re-sectioned while this door sat archived under a separate,
    earlier archive, the stored value points at the section the parent has left: present, wrong, and
    invisible to a `NOT NULL` column. A stated section that disagrees with the parent is refused exactly
    as on create (`AbwabSectionParentMismatchException` → `400`).
  - A root restore that lands in a different section is a re-section like any other and runs through
    `CascadeSectionToDescendantsAsync` — **not** a loop bounded by what the restore itself gave back. The
    restore loop only claims rows carrying this archive's own timestamp; the cascade must also reach the
    rows it does not restore, or a separately-archived descendant keeps the old section and resurfaces
    wrong on its own later restore. `AbwabDoorWriteBehaviorTests` pins both halves.
  - **All of it is gated on the door actually having been archived.** Restore of a door that is already
    live resolves no destination, re-sections nothing, renumbers nothing, and touches neither the
    per-scope nor the global sequence — a `sectionId` in the body is ignored, not honored. It never left
    a scope, so there is nothing to give back. **Do not relax this gate:** re-sectioning a live door is
    `MoveAsync`'s job, and a second route to it would owe `MoveAsync`'s whole contract (compacting the
    scope it leaves, the root-membership rules for the global sequence) while looking like it owed
    none. Pinned by `RestoreAsync_LiveRoot_*` in `AbwabDoorWriteBehaviorTests`.
- **A nested door's section is its parent's, and every write that can change a section must maintain that.**
  Reads group and count by `SectionId` at any depth (`../../Reads/Abwab/README.md`) and nothing re-derives
  it, so this is an invariant the write side owes, not a convention:
  - `CreateAsync` **derives** the section from the parent. A null `sectionId` means *unspecified* — `int?`
    cannot tell an omitted field from an explicit null — and a stated one that disagrees with the parent
    is refused (`AbwabSectionParentMismatchException` → `400`), not silently overwritten.
  - **At root scope there is no parent to derive from, so an unstated section has no answer and is
    refused** (`AbwabSectionRequiredException` → `400`). This covers create, `MoveAsync`, and
    `BulkMoveAsync` alike — `ResolveCreateSectionAsync` and `ResolveTargetSectionAsync` both return a
    non-nullable `int`, so no write path can reach `SaveChanges` with a section-less door. **Both move
    paths check the doors FIRST and resolve the section second** — `MoveAsync` returns `null` for a
    missing door and `BulkMoveAsync` throws `AbwabNotFoundException`, each before its own
    `ResolveTargetSectionAsync` call — so a request that names an unknown door AND omits the root
    section answers `404`, not `400`. Whether that is the intended order is still open and nothing
    discriminates the two (`docs/TESTING_DEBT.md` row C1); comments in
    `AbwabDoorWriteBehaviorTests.cs` and `SmokeAbwabWriteTests.cs` still describe the bulk path the
    other way round.
  - `MoveAsync` and `BulkMoveAsync` **cascade** a section change to the moved door's whole subtree,
    `CascadeSectionToDescendantsAsync`. **Archived descendants included** — they keep their `parent_id`
    through soft-delete, and one left behind would later restore into a section its parent has left.
    A live-only cascade passes every test that ignores archived rows, so the discriminating test asserts
    an archived grandchild's `SectionId`.
  - The same all-rows rule governs the **cycle guard**, not just the cascade.
    `EfAbwabDoorsWriter.LoadChildrenByParentAsync` selects every `AbwabDoors` row with
    no `DeletedAtUtc` filter, and `EnsureNotCycle` walks that same map. Filtering it to live rows
    would let a move nest a door under its own descendant whenever the connecting node is archived —
    `parent_id` survives soft-delete, so the cycle is real but invisible to a live-only map.
  - Descendants keep their `parent_id`, so their sibling scope's membership does not change and they need
    no resequencing. For the same reason a descendant can never collide on the unique index: only subtree
    members share that `parent_id`, so the cascade has no duplicate-name violation to report in the
    first place.
  - Deliberate asymmetry: create **rejects** a disagreeing section, move **ignores** `targetSectionId`
    whenever `targetParentId` is set (plan §4, §13.5). Move's pair describes a destination where the plan
    locks parent-wins; create's body is an authored record, where a disagreement is a caller bug worth
    reporting. Do not "harmonize" one into the other.
- **Aliases are replaced wholesale under the door's own token** and soft-deleted, never hard-deleted.
  `AbwabDoorAlias` deliberately has no `xmin` of its own.
- **Descendant walks share one parent map per operation.** `LoadChildrenByParentAsync` projects
  `(id, parent_id)` once; `CollectDescendantIds` is a pure BFS over it. Calling the loader per door turns
  a bulk operation into one full table read per door.
- **Any write whose result spans more than one `SaveChangesAsync` opens an explicit transaction** —
  EF's implicit transaction covers one call only. The paths that do: `EfAbwabDoorsWriter.CreateAsync`
  and `EditAsync` (the door row, then its aliases keyed by its generated id),
  `EfAbwabTemplatesWriter.CreateAsync` (the template, then its root node), and
  `EfAbwabTemplateApplyWriter.ApplyAsync`, whose one-save-per-level shape is the apply bullet below.
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
- **The column was backfilled by hand-written SQL inside its own migration** — the one sanctioned
  deviation from "never hand-write a migration" in this area. `Migrations/20260729105806_AddAbwabGlobalOrderValue.cs:28`
  appends a `migrationBuilder.Sql(...)` to the EF-generated `Up()`; nothing in the `.Designer.cs`
  or the model snapshot was touched. It exists because existing roots had no order to derive from
  and an EF operation cannot express the numbering. A schema-only replay against an empty database
  makes that call a no-op, so no test covers it — the migration file is the record.
- **No `UNIQUE` index on `global_order_value`**, for the same reason `order_value` has none:
  renumbering issues one `UPDATE` per row, and a per-statement unique index would transiently
  violate mid-resequence. Do not "harden" this with a unique index.
- **`MaintainGlobalOrderAsync`'s departures and arrivals are handled asymmetrically**, like the
  per-scope resequence above. Its read still shows pre-`SaveChanges` state: a door being archived
  or moved-to-nested still comes back from the read, so it is dropped via `excludeIds`; a door
  being restored or moved nested→root does **not** come back (the read still shows its old
  `deleted_at`/`parent_id`), so it is appended in code, never inferred from the read.
- **Relations are attached to doors, not to structure — and no door write touches them.** Move,
  reorder, archive, bulk-archive, restore, and every section write leave `abwab_door_relations`
  completely alone. A relation whose endpoint is archived becomes invisible by the read side's join,
  not by a write here (`../../Reads/Abwab/README.md`, dormancy). Two consequences worth stating
  because both are tempting to "fix":
  - **Relations never block archiving**, and archiving never cascades into them. There is no
    "delete this door's relations" step anywhere in `EfAbwabDoorsWriter`, deliberately — restore
    would then have nothing to bring back.
  - **Restore re-adds nothing.** The rows were never deleted, so a revive path would be a second,
    redundant write.
- **Relation writes carry no version token.** They touch `abwab_door_relations` only, so no door's
  `xmin` moves and there is nothing for a stale-token 409 to compare. The relation row still has its
  own `xmin`, mapped `IsRowVersion()`
  (`../../Configurations/Abwab/AbwabDoorRelationConfiguration.cs:69-70`): **no route carries it and no
  writer overrides its `OriginalValue`**, but EF still compares it on the soft-delete UPDATE, which is
  what `DeleteAsync`'s concurrency catch answers. Do not add a `version` to the delete body "for
  consistency": a token nothing checks is a lie in the contract.
- **Add is all-or-nothing, like bulk move/archive.** One call carries the anchor, the type, an
  optional direction, and N targets; any refusal — self (`400`), unknown id (`404`), archived endpoint
  (`400`), duplicate pair (`409`) — fails the whole batch before `SaveChanges`. `GuardAgainstExistingAsync`
  runs the duplicate check up front purely so the `409` can **name** the colliding doors; `23505` names
  no row. The catch in the save helper stays as the race backstop, with no names.
- **The canonical pair is the writer's job.** Every row is stored `door_a_id < door_b_id` via
  `Math.Min`/`Math.Max` **for all three types**, directional included, and `broader_door_id` carries the
  direction (`NOT NULL` exactly for `Comprehensiveness`). That is what makes "delete from either side
  deletes the row" structural rather than handler logic, and what makes A-more-than-B and
  B-more-than-A the same row — i.e. a duplicate — which a `(source, target, type)` index could not
  express. Flipping a direction is delete + re-add; there is no update path.
- **Delete is soft, and nothing revives.** `DeleteAsync` sets `deleted_at`/`updated_at` and returns a
  `bool`; a missing or already-deleted row is `false`, not an exception (the `IAbwabSectionsWriter`
  convention). Re-adding the same pair creates a **new** row — the partial unique index filters on
  `deleted_at IS NULL`, so the old one no longer occupies the pair. A `DbUpdateConcurrencyException`
  here means a concurrent delete won; the row is gone either way, so it reports `false` rather than 500.

- **A template is a door subtree, and applying it copies the root's DIRECT CHILDREN — never the root
  itself.** Each of the root's children becomes a new child of every target, recursively, with its own
  subtree beneath it, all four authoring fields. The response is **N created doors per target**
  (`IReadOnlyList<AbwabDoorDto>`, type unchanged, meaning changed), not one root door per target.
  Sibling order is carried through **almost** verbatim: level 1 lands at `nextOrder + i` (`i` = the
  child's index in the template's own `(OrderValue, Id)` order); every level below keeps its verbatim
  `OrderValue`. What the copy therefore does **not** need, each a mechanism that would be wrong here
  rather than merely unused:
  - **no global-order maintenance** — a copy is never a root, so it never joins that sequence;
  - **no resequencing** — every insert appends into a scope it either just created or is the newest
    member of; the level-1 offset is what keeps this true when N children land in one save, so every
    touched scope is `1..N` by construction;
  - **no per-node section resolution** — the section is read once off each target and carried down the
    whole subtree, which is the cascade invariant above stated directly instead of re-derived.
- **The copy descends one level per `SaveChanges`, inside one transaction.** `AbwabDoor` has no parent
  navigation property, so a child's `ParentId` can only be set once its parent's generated id exists.
  Level-order inserts are the consequence; the enclosing transaction is what keeps the batch
  all-or-nothing, and each level's alias rows are flushed with the next level's doors. **Do not
  "optimize" this into a single save** — it would need a navigation property this entity deliberately
  does not have.
- **Applying is all-or-nothing, and the collision is per child name, not root name.** The target's live
  children are checked against the root's **direct child names** before anything is inserted, so the
  `409` can name every `(target, child)` pair that blocked it, ordered by the caller's target order
  then the template's own sibling order; the template's own `(template_id, parent_node_id, name)`
  unique index still makes an internal collision among those children unrepresentable, which is what
  confines the failure to comprehensible pairs. An archived target is refused `400`; an empty target
  list is refused `400` — the "never a root door" rule, since no wire shape expresses root-level
  application — and an **empty-root template** (no live children) is refused a third, distinct `400`
  **before any target is read**: the template's emptiness does not depend on which doors were picked,
  so refusing it first is the cheaper and more honest refusal.
- **A copy is detached at birth.** No provenance column, no back-link. Editing or deleting the template
  later never touches doors copied from it, and no door write consults a template. Do not add a link
  "so copies can be updated" — the modal's own copy promises the opposite.
- **Template deletion is soft and touches one row — but its nodes stop being addressable.** Both
  reads filter by the template's own `deleted_at`, so cascading into node rows would write rows
  nothing looks at. The three node writes keyed by `nodeId` alone (edit, reorder, delete) still join
  that flag and answer `404` once the template is gone: `/api/abwab` ships without authentication, so
  a node id is enough to reach a write, and a write that succeeded where the read answers `404` would
  be an asymmetry with no caller. Node deletion, by
  contrast, **does** claim the node's subtree — a template child has no meaning without its parent —
  and resequences the remaining siblings. The root refuses deletion and reordering alike: deleting the
  template is the way, and a single root has no siblings to order among.

## Related

- Read side: `../../Reads/Abwab/` (`EfAbwabTreeReader`, `EfAbwabRelationsReader`,
  `EfAbwabTemplatesReader`) and its `README.md`.
- Contracts and exception types: `application/QuranDashboard.Application.Abstractions/Abwab/`.
- Handlers: `application/QuranDashboard.Application/Abwab/Commands/`.
- Controllers and status mapping: `api/QuranDashboard.Api/Controllers/Abwab/`
  (`../../../../api/QuranDashboard.Api/Controllers/README.md`).
- Domain entities: `Backend/domain/QuranDashboard.Domain/Abwab/`.
- Tests: `Backend/tests/QuranDashboard.Tests/Abwab/` (writer behavior) and
  `Backend/tests/QuranDashboard.Tests/Smoke/SmokeAbwabWriteTests.cs` (status/envelope contract).
  **`EfAbwabRelationsWriter` and `EfAbwabTemplatesWriter` have none of either** — both features wrote
  no tests by decision. `EfAbwabTemplateApplyWriter` has one behavior test and no smoke coverage:
  `Backend/tests/QuranDashboard.Tests/Abwab/AbwabTemplateApplyBehaviorTests.cs` pins the target's
  section carrying to every copied depth, which `docs/TESTING_DEBT.md` row 7 records as the one paid
  obligation of that row; the offsets, the aliases, all-or-nothing across N targets and the
  `(target, child)` `409` are still open, so the apply path stays the highest-value gap of the set —
  it is the only path in the repository that creates door rows outside
  `EfAbwabDoorsWriter.CreateAsync`. No Abwab route is dispatched by the smoke sweep except
  `api/abwab/tree`; every other one is catalogued `ParityOnly` in
  `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs`.
  The relations gaps and what pays them are in `docs/TESTING_DEBT.md`; the templates rows land with
  that feature's frontend slice.
