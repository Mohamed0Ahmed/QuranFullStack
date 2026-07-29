# Abwab feature (الأبواب) — doors & sections management

**HOW rules:** `.architecture/UI_STYLE_SYSTEM.md`, `.architecture/FRONTEND_STRUCTURE.md`,
`.architecture/API_INTEGRATION_GUIDELINES.md` (project root). This file is the WHAT.

**Status: Slice B1 (phase 4) — foundation, tree, modal, page shell.** Cards, bulk
mode, move picker, archive view, and sections management are Slice B1 phase 5
(`docs/feature-abwab-doors/plan-slice-b.md` §8); e2e flows are Slice B2. The routes are
`Open` (no auth) per `plan.md` §10 — **do not** include this feature in a `dev → main`
release until write protection lands.

## What this feature does

Renders the `GET api/abwab/tree` snapshot as a tree (and, from phase 5, drill-down
cards) at `/abwab`, and drives the eleven write endpoints — create, edit, move,
reorder, bulk move, bulk archive, archive, restore, and the three section commands.
Optimistic-concurrency conflicts (`409`) are always surfaced, never swallowed or
auto-retried.

## Render chain & key pieces

- `pages/abwab-page/` — the route shell: URL ⇄ state wiring, composes the toolbar,
  tree, side panel, announcer and door modal.
- `components/abwab-toolbar/` — «كل الأبواب» + one tab per section, composing
  `qd-tabs`/`qdTab`. **No** «الأبواب الرئيسية» tab (`plan.md` §5.1). Search input and
  the tree/cards view toggle land in phase 5 (T507/T502) — a control with nothing to
  do yet is a dead control.
- `components/abwab-tree/` — presentational tree (`role="tree"`/`treeitem`, full ARIA,
  roving tabindex) + `abwab-tree-keyboard.controller.ts`, a pure, DOM-free key model
  (RTL-mirrored per the `qd-tabs` precedent: ArrowLeft expands/enters, ArrowRight
  collapses/exits). Renders **flat** (one row per visible node, `aria-level` conveys
  depth) rather than nesting `role="group"` per level.
  Inline reorder editing (click the order number → input, Enter commits, Escape
  reverts) — the actual `reorderDoor` dispatch is phase-5 (T506).
- `components/abwab-side-panel/` — active door + single-door operations (add child,
  edit, archive). **No** relations/protection entries (`plan.md` §5.1). Move and
  bulk join in phase 5 (T505/T503) once their target UI exists.
- `components/abwab-door-modal/` — add/edit door: name/description/ayah-text/alias
  chips (composing the extended `qd-chip` with its `removable` affordance), a dirty
  guard on close, and an inline error surface for the backend message. Composes
  `.qd-modal`/`.qd-modal-backdrop` + `qdModalScrollLock`.
- `components/abwab-announcer/` — one `aria-live="polite"` `role="status"` region for
  operation messages; a feature-scoped stand-in for a toast primitive this one
  feature does not warrant (`plan-slice-b.md` §4.1).
- `state/abwab-snapshot.facade.ts` — owns the tree snapshot, loading/error/empty
  state, `load()`/`refresh()`.
- `state/abwab-tree.builder.ts` — pure: DTO → `AbwabTreeSnapshotVm` (live/archive
  partition, gap-tolerant ordering, per-section filtering, name+alias search).
- `state/abwab-selection.store.ts` — single selection + bulk set, rebinds by id after
  every refresh, dropping ids a write made vanish. Bulk mode is unavailable while the
  archive view is active.
- `state/abwab-write.controller.ts` — every door/section write, the outcome→message
  mapping, and the 409 policy (see Gotchas below).
- `state/abwab-url-sync.ts` — parses/builds the six query keys below, fail-closed.
- `data-access/abwab.api.ts` — the twelve endpoints under `/api/abwab`.
- `models/abwab.models.ts` / `models/abwab.labels.ts` — view models and every Arabic
  string (read via TDZ-safe getters in consumers, never `readonly` field
  initialisers).

## URL contract (`state/abwab-url-sync.ts`)

| Key | Values | Absent means |
|---|---|---|
| `section` | positive int | «كل الأبواب» — every door, including section-less ones |
| `view` | `tree` \| `cards` | `tree` |
| `archive` | `1` | the live view |
| `door` | positive int | no selection |
| `card` | positive int | the top card level |
| `q` | free text | no search |

Fails closed to the defaults on anything invalid. Switching `section`, or turning
`archive` on, clears `door` and `card` (a selection is not meaningful across scopes);
turning `archive` off restores neither.

## Gotchas / invariants (read before changing)

- **Refresh-after-write is an invariant, not an optimization.** Every write
  resequences its scope to `1..N`, which bumps every sibling's `xmin` too — so after
  *any* write, every cached version token in that scope is stale, including rows the
  user never touched. `abwab-write.controller.ts` refetches the snapshot and rebinds
  every cached version (`abwab-selection.store.ts#rebindTo`) after every success.
  Skipping this reproduces spurious `409`s on the very next write.
- **`AbwabTreeDto.version` is diagnostics only.** Per-row `xmin` tokens are the only
  concurrency currency; do not build snapshot-level conflict detection on it.
- **`createDoor` omits `sectionId` from the wire body whenever `parentId` is set** —
  the backend derives the section from the parent and 400s on a stated mismatch.
  `AbwabApi#createDoor` builds the body without the key (not `sectionId: undefined`;
  `JSON.stringify` would hide the bug at the object level but the raw request-testing
  controller would not).
- **Archived doors are read-only.** The archive view (phase 5) offers restore only —
  no edit/move/reorder/add-child/bulk. Any other control on an archived door would be
  dead by definition.
- **Bulk is all-or-nothing.** One stale token fails the whole bulk operation with a
  single `409`. The backend's bulk-conflict response carries no per-door
  identification (verified against `AbwabDoorsController.cs` /
  `ApiMessages.AbwabDoorStaleVersion`), so the locked conflict message names every
  door in the *attempted* selection, and that selection is preserved rather than
  cleared on conflict — a single-door conflict, by contrast, clears just that door's
  now-invalidated selection.
- **`AbwabDoorDto` carries no audit-seed columns on the wire** (no `createdAt`/
  `createdBy`/`approvedAt`/`approvedBy` — verified against the generated model and
  `openapi/swagger.json`). The door modal's tracking-data box shows only what is
  honestly derivable (archive status) or an explicit "not available yet" placeholder
  for added-by/approved-by; it does not fabricate a date the DTO cannot back.
- **Labels use the TDZ getter pattern**, same as `features/words/README.md`: read
  `abwab.labels.ts` consts via component **getters**, never `readonly` field
  initialisers, or they resolve to `undefined` in the bundled test build.
- **Zero dead controls.** Nothing for relations, protection, templates, or the
  «الأبواب الرئيسية» tab, anywhere in this feature.

## Related

- Plan: `docs/feature-abwab-doors/plan-slice-b.md` (Slice B) and
  `docs/feature-abwab-doors/plan.md` (Slice A, backend).
- Design contract: `docs/design-preview/abwab-tree-concept.html`.
- Shared UI primitives: `../../shared/README.md`.
