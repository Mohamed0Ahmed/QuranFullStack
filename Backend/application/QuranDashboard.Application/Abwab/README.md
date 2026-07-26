# Abwab application handlers — Sections, Categories, Tree (`029`), Relationships + Templates (`030`)

**Layer:** Application · **Features:** `029-abwab-core`, `030-abwab-relationships-templates` ·
**HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`

Use-case handlers implementing `IAbwabCoreReadPort`/`IAbwabCoreWritePort`
(`Application.Abstractions/Abwab/Core/`). Manual protection (resolver + apply/lift/preset writers)
has its own README — `Protection/README.md` — since it is the larger of the two US2/US3 concerns;
this file covers Sections, Categories, Tree, and `030`'s Templates and Relationships.

## `Tree/`

- **`AbwabCoreWriteHandler`** — the single Application-owned implementation of
  `IAbwabCoreWritePort`, composing the per-area handlers below. It carries no business logic itself —
  it is the thin seam the API layer (and the future frontend core mock, for parity) both target.
- **`GetAbwabTreeSnapshotHandler`** / **`SearchAbwabCategoriesHandler`** — the read side: the
  `كل الأبواب` projection over independent root orders, ancestry/depth, and category search over
  normalized name + aliases (never `Description`).
- **`AbwabCompositeReadRedactor`** — the backend DTO projection enforcing `category.view` /
  `section.view` / `protection.view` redaction. Tree/search requires **both** `category.view` and
  `section.view`, else the whole read is denied (no partial response). Full manual-protection
  metadata (type/scope/actor/source-ancestor) additionally requires `protection.view`; without it
  only the two generic booleans on `CategoryProtectionSummaryDto` survive. This is the **only** place
  redaction happens — the read ports and `AbwabProtectionSummaryProjector` always build the full
  product, and the frontend never re-derives or hides fields on its own authority.

## `Sections/`

- **`SectionWriterHandler`** — add/edit/reorder/delete-empty, mapping DB constraint violations to
  `abwab.section_name_conflict` / `abwab.section_not_empty`, and guarding the permanent-default
  section against rename/delete/duplicate (`abwab.permanent_default_section`) while still allowing it
  to be reordered.

## `Categories/`

- **`CategoryContentHandler`** — create/edit direct content (name, description,
  `RepresentativeQuranExcerpt`). Create defaults a root with no `SectionId` into the permanent
  default section and appends both root orders; name/uniqueness revalidation runs under the same
  transaction (§5.1). Create starts `CategoryContentRevision` at 0 (does not bump); edit bumps it
  exactly once.
- **`CategoryAliasHandler`** — alias add/edit/remove as `category.edit` direct content (never a
  borrowed alias-specific `add`/`delete` verb); removal is tracked soft delete, physical delete is
  rejected by the `028` `SavingChanges` guard; each mutation also bumps `CategoryContentRevision`
  once.
- **`CategoryOrderingHandler`** — single/bulk **move** (cycle/self/overlap/inactive-destination
  guards, `AncestorIds`/`Depth` rewrite for every descendant, independent root orders, global-order
  **preserved** on a section move) and **reorder** (siblings / section-roots / global-roots). Both
  bump `TreeRevision` exactly once per atomic operation, never `CategoryContentRevision`.
- **`CategorySubtreeHandler`** — atomic subtree soft-delete + parent-first operation-restore: one
  `DeletionOperationId` and one `TreeRevision` bump per operation, `Deletion` protection checked on
  every affected category and `InternalStructure` on the surviving/restored parent,
  deterministic (`CategoryId`-ordered) locking so concurrent operations can't deadlock or interleave.
  Dependent dormancy is a generic RESTRICT/no-cascade schema property proved by a core test fixture —
  `029` defines no relationship/link schema. The `IDeletionReservationChecker` seam is consulted but
  stays inert until `032`.
- **`CategoryGroupedCreation`** (`030` T064) — the extracted, **behavior-preserving** in-transaction
  creation core: parent lookup, `InternalStructure` protection gate, §5.1 normalization, name-conflict
  guard, and order allocation. `CategoryContentHandler.AddAsync` and the template application handler
  are its **only** two callers, so template application writes real categories through the accepted
  `029` writer instead of a second one. One instance per audited operation: it tracks the categories
  it has created and the sibling orders/names it has claimed **in memory**, because a grouped creation
  allocates orders and rejects in-group name collisions before anything is saved — a per-row database
  query cannot see rows that are added but not yet committed. A parent created inside the same
  operation is deliberately **not** re-resolved for protection: its row does not exist yet, and it
  inherits only from ancestors already checked when it was created.
- **`CategoryProtectionGate`** (shared by the handlers above) — see `Protection/README.md`.

## `Templates/` (`030`)

- **`AbwabTemplateWriteHandler`** — the single Application-owned implementation of
  `IAbwabTemplateWritePort`, composing the four handlers below; no business logic of its own.
- **`TemplateAggregateHandler`** — `template.add` creates **only** the aggregate;
  `template.delete`/`template.restore` own its lifecycle. Delete/restore are tracked soft
  delete/restore, and a row already in the requested state is refused as `abwab.row_stale` rather
  than answered as done.
- **`TemplateNodeHandler`** — create/reparent/reorder/remove as **grouped structural** operations and
  edit as content-only. Structural operations guard the expected `TemplateRevision`, rewrite affected
  sibling orders **atomically** in one pass (so no intermediate state holds two nodes on one order),
  and bump `TemplateRevision` **exactly once** however many rows they touched. Reparent rejects
  self-parenting and any destination inside the moved node's descendant tree
  (`abwab.template_cycle`), validating the parent chain from the destination upward over rows read
  **inside** the transaction. Both destination paths — node **add** and node **reparent** — also
  require the destination to be **active**: the in-transaction read carries soft-deleted rows, and a
  node placed under a removed one would be an unreachable orphan (there is no node-restore command,
  the detail read hides it, and the application recursion never reaches it), so a removed destination
  is `abwab.row_stale`. The owning template is resolved inside the operation, never before it:
  a lookup outside the barrier would attach a pre-transaction row to the change tracker and let
  identity resolution hand the in-transaction guards stale values.
- **`TemplateAliasHandler`** — alias add/edit/remove/restore as `template.edit`-owned internals
  (§5.2 names internal removal under `template.edit`; alias **restore** is the same
  internal, aggregate-scoped operation and maps there too — the mechanical completion recorded in
  `contracts/templates-api.md`). Removal is tracked soft delete; physical delete is rejected by the
  `028` `SavingChanges` guard, so alias history is never lost.
- **`TemplateApplicationHandler`** — applies **one** template to **one** target category inside
  **one** audited operation: one ChangeSet, one `TreeRevision` bump. Every template root becomes a
  **direct child** of the target, the recursion copies the strict allowlist only (name, representative
  excerpt, description, aliases, order, structure), and uniqueness/protection/target state/
  concurrency/order allocation are all revalidated **inside** that transaction — any failure
  rolls the whole application back with no partial tree. Real rows are written through
  `CategoryGroupedCreation`; the `029` writer is never forked. Copied **aliases** are the one
  exception to "everything through the `029` handler": they are added straight to
  `ICategorySearchAliasWriteStore` instead of through `CategoryAliasHandler`, because that handler
  calls `CategoryProtectionGate.StartWindow` on every add — and §9 forbids an application from
  creating or restarting an ordinary 24-hour window on the target or on any created category. Going
  through it would start one per copied alias.
- **`TemplateAuditViews` / `TemplateHistory`** — the §6.3 payload shapes. Template **application** is
  main-log eligible and stores the **frozen** template snapshot taken at application time, so a later
  template edit cannot change that rendering. Template **CRUD** carries the `template.history.` action
  prefix and renders only in the separate template-history view — **0** main product-audit rows. The
  before/after trees are projected from the same in-memory node list the operation mutates, because
  re-querying after the mutation would miss rows that are added but not yet saved. The history
  payload carries **`ChangedFields`** beside `ChangedNodes` (§6.3 requires both): each entry is
  `(TemplateNodeId?, Field, Before, After)`, with a null node id meaning the template header. It is
  **derived** from the two stored snapshots inside `TemplateHistoryAuditView` rather than supplied by
  the handlers, so no handler can publish a diff that disagrees with the trees stored beside it.
- **`CategoryTreeGuards`** — the pure cycle/overlap/inactive-destination/name-revalidation checks the
  ordering and subtree handlers share.

## `Relationships/` (`030`)

- **`RelationshipWriterHandler`** — the single `IAbwabRelationshipWritePort` implementation:
  add/edit/delete/restore, each one audited ChangeSet on the `028` executor. It bumps **neither**
  `TreeRevision` nor any content counter — a relationship is an adjunct, not tree structure.
  Duplicate rejection (`abwab.relationship_duplicate`) and directional **cycle** rejection
  (`abwab.relationship_cycle`) both run **inside** the transaction, after the barrier/revision lock;
  because every Abwab writer serializes on that lock, the in-transaction check is the race-safe
  guard and the filtered unique index is the DB backstop. An explicit direct **A→C stays legal**
  alongside A→B→C — only reachability back to the proposed broader endpoint is refused. Restore
  revalidates both, which is what makes a **restore collision** fail instead of producing a second
  active row.
- **`RelationshipProtectionGate`** — the §7.3 endpoint-protection gate over the resolved target set
  (**proposed** on add, **current ∪ proposed** on edit, **stored** on delete/restore) resolved
  through the `029` `ProtectionResolver` for `ManualProtectionType.Relationship`. Direct **or**
  inherited protection on **any** target blocks the **entire** mutation, and it runs before any row
  is touched — that is what stops an edit escaping protection by dropping the protected endpoint.
- **`RelationshipShape`** — the one place a submitted `(type, first, second)` triple becomes storable
  columns (mutual canonicalization / directional orientation).
- **`RelationshipAuditViews` + `RelationshipEndpointViewBuilder`** — the §6.3 specialized relationship
  payload: type/shape, both endpoints with their **historical** section/path frozen at operation
  time, and before/after on an edit. The payload carries **structure only**; the Broader/Narrower
  inverse label and the type labels are derived for display by the render component, never stored.
  The payload carries **no protection-blocker facet**: applicable `Relationship` protection on any
  target aborts the mutation before a ChangeSet exists, so no committed relationship event can carry
  one. A blocked attempt surfaces as the `abwab.manual_protection` conflict, not as an audit row.

### Relationship invariants (read before changing)

- **The ordinary 24-hour window is never read, started, or restarted** by a relationship mutation
  (§9, §2.1) — it is neither started by nor blocked by these writers.
- A **targeted-row expectation failure** — stale `xmin` *or* an unaddressable relationship id — maps
  to `abwab.row_stale`. Rows are never physically deleted, so a missing id can only come from a
  forged/stale reference; §11 defines no separate relationship-not-found code and none is invented.
- **A row that is already in the requested state is refused, never reported as done.** Editing a
  soft-deleted relationship, deleting an already-deleted one, and restoring an already-active one all
  map to `abwab.row_stale` and write no ChangeSet — answering success for a mutation that changed
  nothing would report an action that never happened. The protection gate still runs first, so a
  protected endpoint fails closed with `abwab.manual_protection` either way.
- A **dormant** relationship (any endpoint soft-deleted) is **not actionable**: after the protection
  gate, every mutation revalidates that all endpoints are active and otherwise fails with
  `abwab.category_unavailable`.
- The `029` `CategorySubtreeHandler` is **not modified** to know about relationships: dormancy falls
  out of the RESTRICT schema property plus the read projection.

## Related

- Ports/commands/DTOs these handlers implement: `Application.Abstractions/Abwab/README.md`.
- Domain entities: `Domain/Abwab/README.md`.
- Manual protection resolver + writers: `Protection/README.md`.
- API endpoints calling these handlers: `Api/Abwab/README.md`.
- Contracts: `specs/029-abwab-core/contracts/sections-api.md`, `categories-api.md`,
  `tree-read-contract.md`; `specs/030-abwab-relationships-templates/contracts/relationships-api.md`,
  `relationship-dormancy-contract.md`, `templates-api.md`, `template-application-contract.md`,
  `audit-render-contract.md`.
