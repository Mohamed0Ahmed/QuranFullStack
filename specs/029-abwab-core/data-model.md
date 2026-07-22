# Data Model: Abwab Core — Sections, Categories, Tree, and Protection

**Feature**: `029-abwab-core` | **Date**: 2026-07-23 | **Source**: Master Plan §18.3 (grounded in
§7.1, §7.2, §5.1, §9, §11)

This model covers the **sections / categories / tree / protection** domain owned by §18.3. It
reuses the `028` substrate (tracked ChangeSet UoW, `AbwabWriteBarrier`, singleton
`AbwabRevisionState` with `TreeRevision`/`AuditHeadSequence`/`TimelineGeneration`, server clock,
soft-delete + append-only audit) and defines **no Quran foreign key** — `RepresentativeQuranExcerpt`
is a plain string, and the first Abwab Quran FK remains owned by later Kits. Field lists follow
§7.1/§7.2; codes follow §11.

## Domain entities

### Section (Story 1)

- **Fields**: `SectionId`, `Name`, `NormalizedName`, `SortOrder`, `IsPermanentDefault`,
  soft-delete metadata, `Version` (`xmin`).
- **Rules**: Active normalized section names are **unique**. Exactly **one permanent default row**
  is seeded (`أبواب غير مصنفة`, `IsPermanentDefault = true`); it may be **reordered but not
  renamed, deleted, or duplicated** — violations map to `abwab.permanent_default_section`. A
  non-default section may be deleted **only when it has no active root categories** (else
  `abwab.section_not_empty`); a normalized-name collision maps to `abwab.section_name_conflict`.
- **State transitions**: created → (reorder | rename | soft-delete when empty). The permanent
  default is reorder-only.

### Category (Story 1 shape; Story 3 writers)

- **Fields**: `CategoryId`, `Name`, `NormalizedName`, optional `RepresentativeQuranExcerpt`
  (**plain string, no Quran FK, not ayah-validated**), optional `Description`, `ParentCategoryId`,
  `SectionId`, `SiblingOrder`, `SectionOrder`, `GlobalOrder`, `AncestorIds`, `Depth`,
  ordinary-protection actor/time fields, `CategoryContentRevision`, soft-delete metadata,
  `Version` (`xmin`).
- **Shape rules**:
  - **Root**: `ParentCategoryId = null`, non-null `SectionId`, null `SiblingOrder`, non-null
    `SectionOrder` **and** `GlobalOrder`, `AncestorIds = []`, `Depth = 0`.
  - **Descendant**: non-null `ParentCategoryId`, null `SectionId`, non-null `SiblingOrder`, null
    `SectionOrder`/`GlobalOrder`; `AncestorIds` is root-to-parent excluding self;
    `Depth = AncestorIds.Length`.
- **Uniqueness**: active sibling normalized names unique per parent; **all roots share one global
  normalized-name scope across sections**. Create, rename, move, template application, and restore
  preflight use the **same** §5.1 rule; collisions map to `abwab.category_name_conflict`.
- **Ordering**: every child has explicit `SiblingOrder`; root `SectionOrder` and `GlobalOrder` are
  **independent**. Creating/promoting a root without an explicit `SectionId` places it in the
  permanent default section and appends both root orders. Moving a root between sections
  **preserves `GlobalOrder`** unless a global-reorder command is issued in the same audited
  operation. Every reorder tracks all changed rows, validates affected-row counts, and bumps
  `TreeRevision` **once**.
- **Content-revision bump** (§6.4, §8): a category **direct-content** mutation (Name, Description,
  `RepresentativeQuranExcerpt`, and CategorySearchAlias add/edit/remove) bumps the owning Category's
  `CategoryContentRevision` **exactly once** per audited operation. It is a reconciliation/logical
  counter, **distinct from `TreeRevision`** (structural): a pure move/reorder bumps `TreeRevision`,
  **not** `CategoryContentRevision`. It has **no dedicated §11 stale code** — content-edit
  concurrency is enforced by `xmin` (`abwab.row_stale`) and `ExpectedTimelineGeneration`.
- **Move rules**: reject self-parenting (`abwab.category_cycle`), a destination inside the moved
  subtree, inactive/missing destinations (`abwab.category_unavailable`), and overlapping
  ancestor/descendant selections in one bulk request (`abwab.category_overlapping_move`).
  Revalidate under the transaction, rewrite `AncestorIds`/`Depth` for **every** descendant, return
  a safe 409 with **no partial order changes**.
- **RepresentativeQuranExcerpt**: audited/restorable direct category content; **not** parsed as
  Quran identity, **not** validated as a whole ayah, never canonical Quran source; activates
  ordinary protection as direct content.

### Category subtree deletion / operation-restore (Story 3)

- **Delete**: soft-deletes the selected category and its entire currently active subtree
  **atomically**, records one `DeletionOperationId`, bumps `TreeRevision` once. Checks `Deletion`
  protection on **every** affected category and `InternalStructure` on the surviving parent. It is
  **not** an ordinary 24-hour action. Locks every affected row in **deterministic ID order**.
- **Dormant dependents**: attached links/notes/highlights/relationship rows are **not
  cascade-deleted**; they become **dormant** (ordinary reads/mutations require active category
  endpoints) and reappear only on restore. In `029` this is proven only through a generic
  RESTRICT/no-cascade + dependent-visibility **core fixture** — **no forward relationship/link
  schema dependency**.
- **Reservation seam**: any Pending request for any affected category would reject the whole
  deletion (`abwab.category_reserved_by_pending`). Because request storage does not exist yet, `029`
  exposes an **inert integration seam**; `032` installs and tests the Pending-aware checker before
  Submit.
- **Operation-restore**: restores exactly the categories soft-deleted by the chosen
  `DeletionOperationId`, **parent-first and atomically**; revalidates parent existence, normalized
  names, all three order scopes, `Deletion`/`InternalStructure` protection, and every row/tree
  revision. Conflicts change nothing.

### CategorySearchAlias (Story 1 shape; Story 3 writers)

- **Fields**: `CategorySearchAliasId`, `CategoryId`, `Value`, `NormalizedValue`, soft-delete
  metadata, `Version`.
- **Rules**: duplicate active normalized aliases **within one category** are rejected
  (`abwab.category_alias_conflict`). Aliases are **separately owned rows** — not categories, not
  part of category-name uniqueness, not globally unique. Primary category search covers normalized
  name + aliases (Description is not in the primary search contract). Add/edit/remove is **category
  direct-content mutation authorized by `category.edit`** (never a borrowed child `add`/`delete`
  verb); **removal is tracked soft delete**, physical delete is rejected.

### ManualProtection (Story 2 storage/resolver; Story 3 writers)

- **Fields**: `ManualProtectionId`, `CategoryId`, typed `ProtectionType`, typed `ProtectionScope`,
  applied/lifted actor + timestamps, active/soft-delete state, `Version`.
- **Types**: `CategoryData`, `InternalStructure`, `QuranContent`, `Deletion`, `Relationship`.
  **Scopes**: `CategoryOnly`, `Subtree`.
- **Rules**: a **filtered unique index permits exactly one active record per
  `(CategoryId, ProtectionType)`**; `ProtectionScope` is the current scope on that record. Applying
  the same active type/scope is **idempotent and creates no ChangeSet**. A scope change requires
  Expected Version and is one audited reversible edit; competing scope changes return
  `abwab.manual_protection_scope_conflict` rather than creating CategoryOnly+Subtree duplicates.
  Apply/lift is one tracked, audited, reversible ChangeSet. **Inheritance is evaluated from current
  `AncestorIds`** (no descendant snapshot). Effective reads and authorized lifts address a
  **soft-deleted** category by **immutable `CategoryId`**, so deletion cannot hide or strand a
  protection (that narrow surface does not expose the deleted category to ordinary commands).
- **"Full protection" (five-type preset)**: carries one selected `CategoryOnly`/`Subtree` scope and
  **atomically idempotent-upserts all five typed records** to that scope (never persisted as a
  sixth type). Same-scope records are unchanged; each different-scope record requires its Expected
  Version and becomes an audited scope edit; missing types are inserted. If all five already match,
  the command is an **idempotent success with no ChangeSet**. Any stale/constraint/protection
  failure **rolls back all five**. Each type may later be lifted independently.

### Ordinary protection (24-hour window) (Story 3)

- **Carried by**: the ordinary-protection actor/time fields on Category.
- **Rules**: gates **only** direct-content edits (Name/Description/SearchAliases/
  RepresentativeQuranExcerpt) and per-selected-category moves, and **starts/restarts** the window
  on the target (§9). Descendants carried as side effects get **no** window. Active window: last
  protected editor or System Owner only; this "last editor/Owner allowed" **never** overrides
  manual protection or stabilization. Conflicts map to `abwab.ordinary_protection`.

## Technical / read state (reused from `028`)

### AbwabRevisionState (singleton — reused)

- **Relevant fields for `029`**: `TreeRevision` (bumped **once** per atomic structural operation),
  plus `AuditHeadSequence`/`TimelineGeneration`/`Version` owned by the `028` kernel.
- **Rules**: these are current concurrency/reconciliation state, **not inverse-restored**; a
  rollback leaves them unchanged. `029` writers take the barrier and this row's lock and carry
  expected `TreeRevision`/`ExpectedTimelineGeneration`/`xmin`.

### AbwabTreeSnapshot (read model)

- **Content**: a **versioned complete snapshot** (not a paged hierarchy) with
  generation/revision/schema/server-time plus sections and categories; supports the `كل الأبواب`
  projection over independent root orders and category search over normalized name + aliases.
- **Composite-read redaction**: `category.view` + `section.view` are required for the tree/search;
  without `protection.view` only generic server-derived action-blocked / effective-manual-protection
  flags are exposed, **omitting** ManualProtection type/scope/actor/time/direct/inherited/
  source-ancestor. Full metadata and the dedicated effective-protection read require
  `protection.view`. **Backend DTO projection enforces all redaction** — not frontend hiding.

## Versioned restore adapters (accepted for `033`)

- Exactly **three** registered adapters (§8, keyed by persisted type; duplicate registrations fail
  CI): **Section**, **Category**, and **ManualProtection**, each **versioned and round-trip tested**.
  **Order is a tested facet, not a fourth adapter**: `SiblingOrder`/`SectionOrder`/`GlobalOrder` +
  one-`TreeRevision` semantics round-trip **within the Category adapter**, and section order within
  the **Section** adapter. The Section and Category snapshots are accepted in Story 1; the
  ManualProtection adapter in Story 2 (**before** any protected writer). All **three** are **marked
  accepted for `033`** at the feature exit. Snapshots exclude `xmin`, logical revision counters
  (`TreeRevision`, `CategoryContentRevision`), cache state, and realtime cursors (§6.3, §6.4, §8).

## Invariant summary (verification anchors)

| Invariant | Entity | Enforced by | Story |
|-----------|--------|-------------|-------|
| Exactly 1 permanent default section, reorder-only | Section | migration seed + `abwab.permanent_default_section` | 1 |
| Active root names globally unique; sibling names unique per §5.1 | Category | filtered unique indexes + `abwab.category_name_conflict` | 1/3 |
| Alias unique per category, separately owned | CategorySearchAlias | filtered unique index + `abwab.category_alias_conflict` | 1/3 |
| Read/search/snapshot only; no mutation surface | AbwabTreeSnapshot | absence of mutation endpoints/UI | 1 |
| Section/Category/Order restore snapshots round-trip | restore adapters | versioned round-trip tests | 1 |
| One active ManualProtection per `(CategoryId, type)` | ManualProtection | filtered unique index | 2 |
| Inheritance from current `AncestorIds`; source ancestor + server expiry | ManualProtection | resolver + server clock | 2 |
| Deep-tree resolution within budget | ManualProtection | real-PG query-budget test | 2 |
| ManualProtection adapter accepted before protected writers | restore adapters | ordering gate | 2 |
| One audited UoW; expected generation/xmin/TreeRevision | Category/Section | `028` kernel + writer tests | 3 |
| One `TreeRevision` bump per atomic reorder; independent root orders preserved | Category | reorder/move tests | 3 |
| One `CategoryContentRevision` bump per direct-content mutation; 0 on pure move/reorder | Category | real-PG content-revision test | 3 |
| Self/descendant/overlapping move rejected; ancestry rewritten | Category | cycle guards + `abwab.category_*` | 3 |
| Atomic subtree delete/restore; protection on every affected; dormant seam | Category | deterministic-lock + core fixture | 3 |
| Section codes + permanent-default code exact across layers | Section | `abwab.section_*` mapping tests | 3 |
| Alias mutation = `category.edit`; tracked soft delete | CategorySearchAlias | authorization + soft-delete tests | 3 |
| Idempotent same-scope apply (no ChangeSet); scope conflict code | ManualProtection | `abwab.manual_protection_scope_conflict` | 3 |
| Full five-type preset all-or-nothing; per-scope Expected Version | ManualProtection | preset rollback tests | 3 |
| Soft-deleted target protection view/lift by immutable ID | ManualProtection | narrow security-path test | 2/3 |
| Ordinary 24h gates only direct-content edits/moves | ordinary protection | §9 gate tests | 3 |
| Composite-read redaction is backend DTO projection | AbwabTreeSnapshot | projection + parity tests | 3/4 |
| Reservation seam inert; `032` installs Pending checker | Category delete | seam fixture | 3 |
| Exactly 3 adapters versioned + accepted for `033`; Order is a facet, not a registration | restore adapters | round-trip acceptance + §8 registry test (duplicate/missing fails CI) | 1/2/3 |
| Mock/HTTP parity, no-drag, RTL, large-tree, context-preserve | frontend slice | Playwright/source suite | 4 |
