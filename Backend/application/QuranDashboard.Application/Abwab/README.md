# Abwab application handlers — Sections, Categories, Tree (`029`)

**Layer:** Application · **Feature:** `029-abwab-core` · **HOW rules:**
`Backend/.architecture/CLEAN_ARCHITECTURE.md`

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

## Related

- Ports/commands/DTOs these handlers implement: `Application.Abstractions/Abwab/README.md`.
- Domain entities: `Domain/Abwab/README.md`.
- Manual protection resolver + writers: `Protection/README.md`.
- API endpoints calling these handlers: `Api/Abwab/README.md`.
- Contracts: `specs/029-abwab-core/contracts/sections-api.md`, `categories-api.md`,
  `tree-read-contract.md`.
