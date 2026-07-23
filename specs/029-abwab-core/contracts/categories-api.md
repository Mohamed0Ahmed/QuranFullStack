# Contract: Categories API

**Feature**: `029-abwab-core` | **Source**: Master Plan §11 (Categories), §7.1, §9, §5.1, §18.3
step 3. Realizes §18.3 only.

## Operations

`add` / `edit` / **single-or-bulk `move`** / `reorder` / `subtree-delete` / `operation-restore` /
`search`. **Explicit action endpoints with expected row/tree revisions — no drag semantics.**
Envelope `ApiResponse<T>`; every mutation DTO carries `ExpectedTimelineGeneration`, expected `xmin`,
and expected `TreeRevision` where structural. One audited ChangeSet per operation on the `028`
kernel (barrier + `AbwabRevisionState` lock). Verb→code mapping is mechanical (§11).

## Rules (from §7.1 / §9)

- **Names**: create/rename/move/template-application/restore-preflight use the **same §5.1
  normalized rule**; active sibling names unique per parent; **roots share one global normalized
  scope across sections**.
- **Root defaulting**: a root created/promoted without an explicit `SectionId` lands in the
  permanent default section and **appends both** `SectionOrder` and `GlobalOrder`. Moving a root
  between sections **preserves `GlobalOrder`** unless a global-reorder is issued in the same audited
  operation.
- **Ordering**: explicit child `SiblingOrder`; root `SectionOrder`/`GlobalOrder` **independent**;
  every reorder tracks all changed rows, validates affected-row counts, and bumps `TreeRevision`
  **once**.
- **Move guards**: reject self-parenting, destination inside the moved subtree, inactive/missing
  destination, and overlapping ancestor/descendant selection in one bulk request. Revalidate under
  the transaction, rewrite `AncestorIds`/`Depth` for **every** descendant, **no partial order
  changes**.
- **Subtree delete/operation-restore**: atomic; one `DeletionOperationId`; locks every affected row
  in deterministic ID order; checks `Deletion` on **every** affected category and `InternalStructure`
  on the surviving/restored parent; **not** an ordinary 24-hour action; dependents become **dormant**
  (generic RESTRICT/no-cascade + dependent-visibility **core fixture**, no relationship/link schema).
  Operation-restore is **parent-first**, revalidating names/orders/protection/revisions; conflicts
  change nothing.
- **RepresentativeQuranExcerpt**: optional **plain string** direct content — no Quran FK, not
  ayah-validated; activates ordinary protection.
- **Ordinary 24-hour window**: gates **only** direct-content edits (Name/Description/SearchAliases/
  RepresentativeQuranExcerpt) and per-selected-category moves; descendants carried as side effects
  get no window; active-window actor = last protected editor or System Owner only, never overriding
  manual/stabilization.
- **Aliases**: add/edit/remove is **category direct-content mutation under `category.edit`** (never
  a borrowed child `add`/`delete` verb); removal is **tracked soft delete**; physical delete
  rejected.
- **`CategoryContentRevision` bump** (§6.4, §8): a category **direct-content** mutation (Name,
  Description, `RepresentativeQuranExcerpt`, and CategorySearchAlias add/edit/remove) bumps its
  owning Category's `CategoryContentRevision` **exactly once** per audited operation. It is a
  reconciliation/logical counter, **distinct from `TreeRevision`** (structural) — a pure move/reorder
  bumps `TreeRevision`, not `CategoryContentRevision`. It has **no dedicated §11 stale code** (content
  concurrency is enforced by `xmin` → `abwab.row_stale` and `ExpectedTimelineGeneration`).
- **Deletion reservation seam**: a Pending request on any affected category would reject the whole
  deletion; request storage does not exist yet, so the seam is **inert** and `032` installs/tests
  the Pending-aware checker before Submit.

## Conflict codes (exact — §11)

`abwab.category_name_conflict`, `abwab.category_alias_conflict`, `abwab.category_cycle`,
`abwab.category_overlapping_move`, `abwab.category_unavailable`, `abwab.category_reserved_by_pending`,
`abwab.manual_protection`, `abwab.ordinary_protection`, `abwab.stabilization_active`,
`abwab.tree_revision_stale`, `abwab.timeline_generation_stale`, `abwab.row_stale`.

## Tests

- Create/promote-root defaulting, global-order preservation on section move, self/descendant/
  overlapping bulk-move rejection, descendant ancestry rewrite, one-`TreeRevision` reorder, and
  concurrent move/reorder conflict → **real-PG/API**.
- Subtree delete/operation-restore: child/parent order, all-row tracked atomicity, protection on
  every affected category, conflict rollback, dormant-dependent core-fixture visibility, versioned
  adapter round-trips.
- Alias soft-delete + physical-delete rejection + `category.edit` authorization; excerpt plain-string
  (no FK / no ayah validation).
- Ordinary-24h gating scope (only direct-content edits/moves) per §9; mock ≡ HTTP parity for every
  code.
