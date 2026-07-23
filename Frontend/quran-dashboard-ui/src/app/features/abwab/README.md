# Abwab feature (الأبواب) — category tree, protection, audit renders

**HOW rules:** `.architecture/FRONTEND_STRUCTURE.md`, `.architecture/API_INTEGRATION_GUIDELINES.md`
(project root). This file is the WHAT (current truth + shared pattern). **Feature:** `029-abwab-core`.

## What this feature does

The domain frontend vertical slice for the category tree: a virtualized, RTL, keyboard-navigable
tree of sections/categories; a category editor (name/description/excerpt/aliases); a protection
panel (view direct/inherited protection, apply/lift/full-preset); and the §6.3 audit-render
components. It is routed at `/dashboard/abwab` (`abwab.routes.ts`, unguarded — public-browse posture,
composite-read visibility is in-page and cosmetic, see below).

## Layout

- `data-access/` — the core contract, its two implementations, cache, and conflict types.
- `state/` — the tree facade (orchestration) and the composite-read permission mirror.
- `tree/` — the tree page shell and the virtualized tree view.
- `editor/` — the category editor (Reactive Forms).
- `protection/` — the protection panel.
- `audit/` — the §6.3 audit-render components (presentational only).

## `data-access/` — the ONE versioned core contract

- **`abwab-core.port.ts`** (`AbwabCorePort`) — every read/write operation the domain needs. Every
  **read** (`getTreeSnapshot`/`search`/`getProtectionProfile`) carries `TimelineGeneration`/
  `TreeRevision` **from the server**; no method may synthesize one. A **direct** manual-protection
  resolution also carries the record's own identity + concurrency token, which the caller sources
  `ExpectedVersion` from for apply/lift/preset — never a manufactured value.
- **`abwab-core.mock.ts`** (`AbwabCoreMock`) — an in-memory implementation for dev/testing. It
  re-derives its own revision/generation counters internally but returns them **only from reads** —
  mutation results never attach one, matching the real HTTP surface exactly. Not `@Injectable()`: it
  takes plain constructor options (actor/clock/id factory) rather than injected services, so callers
  construct it directly or via a DI `useFactory`.
- **`abwab-http.adapter.ts`** (`AbwabHttpAdapter`) — maps the backend's exact `abwab.*` 409 codes
  (`AbwabConflictResponses.cs`) to a typed `AbwabConflictError` (`abwab-conflict.ts`), with the same
  "never fabricates a revision/generation on a mutation" rule.
- **Mock ↔ HTTP parity** (`abwab-core-parity.spec.ts`) is a unit suite that drives the same
  scenarios through both implementations and asserts identical result shapes and identical
  `abwab.*` codes — this is what keeps the mock a safe stand-in for the backend during development.
- **`abwab-mock-*-ops.ts`** — the mock's per-area operation logic (section/category/alias/move/
  delete-restore/protection), kept out of `abwab-core.mock.ts` itself to stay small and focused;
  `abwab-mock-normalize.ts` is a **UI-only mirror** of the backend §5.1 normalization used only for
  the mock's own conflict simulation — the backend `ArabicNameNormalizer` stays the single source of
  truth for real uniqueness decisions.
- **`abwab-cache.ts`** (`AbwabTreeCache`) — reuses the `028` §14.1 primitive
  (`PersistentCache` over an `IndexedDbKeyValueStore`, falling back to an in-memory store when
  `indexedDB` is unavailable). The tree snapshot is cached under **one stable key** (there is only
  one snapshot resource). `invalidate()` is called after every successful mutation and after every
  `abwab.tree_revision_stale`/`abwab.timeline_generation_stale`/`abwab.row_stale` conflict — there is
  no client-side reconciliation of a stale snapshot, only invalidate + re-fetch.

## `state/abwab-tree.facade.ts` — orchestration, cache rule, and "rollback"

Owns the loaded tree snapshot, selection/expansion, and every mutation, all funneled through one
`runMutation()` that is the single place the cache rule above lives: on success, invalidate the
cached snapshot and reload from the server (the only source of a fresh revision/generation); on a
conflict, do the **same** invalidate + reload. Because nothing is ever applied to the rendered
snapshot ahead of server confirmation, "rollback" is just this re-sync — there is no separate
optimistic-undo path to get wrong. Selection/expansion **survive** a reload as long as the category
still exists in the fresh snapshot (post-mutation context preservation, SC-015); only ids that
genuinely disappeared (e.g. a subtree delete) are pruned.

## Composite-read visibility (`state/abwab-permissions.ts`) — cosmetic only

`AbwabPermissions` mirrors the backend's composite-read redaction table (`tree-read-contract.md`)
from `/me` permissions: tree/search visibility requires **both** `category.view` and `section.view`;
full protection detail additionally requires `protection.view`. **This is entirely non-authoritative
UI polish** — the backend DTO projection (`AbwabCompositeReadRedactor`) is the sole authority, and a
hidden action invoked directly is still rejected server-side. The protection panel enforces the same
rule from the other direction: when `canView` is false it renders **nothing** protection-specific,
which is safe by construction because a caller without `protection.view` is only ever handed a
redacted profile with no type/scope/actor/source-ancestor to leak in the first place.

## `tree/` — no drag, RTL, virtualized

`abwab-tree-view.component.ts` renders a virtualized (`cdk-virtual-scroll-viewport`), RTL,
keyboard-navigable category tree. Every mutation is an **explicit action** (a button), never drag:
expand/collapse, select, move-up/move-down (sibling reorder), "move to…" (opens the destination
picker in the page shell), and delete are all discrete emitted events — see
`e2e/abwab/core-slice.spec.ts` for the no-drag browser proof and `check:no-drag` for the static source
gate. RTL keyboard follows the WAI-ARIA treeview pattern's mirrored assignment: `ArrowLeft` expands
("inward"), `ArrowRight` collapses. `abwab-tree-node.ts` flattens the snapshot into only the
currently-**visible** rows (a node's descendants show iff every ancestor up to the root is
expanded), which is what makes the tree cheap to virtualize regardless of total size.
`abwab-tree-page.component.ts` is the page shell composing the tree view, editor, and protection
panel over the facade.

## `editor/category-editor.component.ts` — reused Reactive Forms, no edit-session lock

Reuses the `028` `@angular/forms` Reactive Forms package. **Explicit save only** — there is no
autosave and no "start editing session" call, so opening (or switching) the editor never itself
emits a mutation or locks the category against another editor; the only thing that can conflict is
the save itself, via the ordinary `abwab.row_stale`/`abwab.timeline_generation_stale` mutation
conflict. Existing-alias edit/remove each carry the **alias's own** `Version` (from
`CategorySnapshotDto.aliases`) as `expectedVersion` — never the category's.

## `protection/protection-panel.component.ts`

Views direct/inherited protection (type/scope, the resolving source ancestor, the server-derived
expiry) and drives apply/lift/full-preset, gated by `protection.view` as described above. Each
changed-scope preset type needs its **own** active record's `Version`; a type with no active record
(newly inserted by the preset) is never version-checked by the write path, so `0` there is an inert
placeholder, not a manufactured expectation (`manual-protection-contract.md`).

## `audit/` — §6.3 render components (presentational only, no fetch)

Pure view models + presentational components for the five §6.3 payloads
(`audit-render-contract.md`): category **create** (complete new-state, empty fields shown as
`غير محدد`, order fields included), category **edit** (non-color field-diff marker, order fields
included), bulk **move** (nested descendants, sibling-order side effects grouped by affected
parent/order scope), subtree **delete/restore** (dormant-dependent labels/counts), and
**manual-protection** (changed direct/inherited effects). **There is no standalone "ordering"
render component** — ordering is folded into bulk-move and category-edit, per §6.3. `029` defines
and renders only the *shape* of these payloads over whatever changeset a caller supplies; it does
not build the main audit page, pagination, or fetch real audit records — that read model is `033`'s.
Fixture data in tests is synthetic Arabic only (source-safe) — never real Quran text.

## Related

- Backend: `Backend/api/QuranDashboard.Api/Abwab/README.md`,
  `Backend/application/QuranDashboard.Application/Abwab/README.md`.
- Contracts: `specs/029-abwab-core/contracts/tree-read-contract.md`,
  `audit-render-contract.md`, `manual-protection-contract.md`.
- Reused `028` primitives: `../../core/README.md` (§14.1 cache/foundation), `../../shared/README.md`.
- Playwright source suite: `e2e/abwab/core-slice.spec.ts`.
