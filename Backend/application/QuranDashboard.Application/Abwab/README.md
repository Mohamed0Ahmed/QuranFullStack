# Abwab application handlers — Sections, Categories, Tree (`029`), Relationships (`030`)

**Layer:** Application · **Features:** `029-abwab-core`, `030-abwab-relationships-templates` ·
**HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`

Use-case handlers implementing `IAbwabCoreReadPort`/`IAbwabCoreWritePort`
(`Application.Abstractions/Abwab/Core/`). Manual protection (resolver + apply/lift/preset writers)
has its own README — `Protection/README.md` — since it is the larger of the two US2/US3 concerns;
this file covers Sections, Categories, and Tree.

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
- **`CategoryProtectionGate`** (shared by the handlers above) — see `Protection/README.md`.
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
  `tree-read-contract.md`.
