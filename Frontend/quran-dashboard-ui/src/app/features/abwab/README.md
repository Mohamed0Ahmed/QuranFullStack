# Abwab feature (الأبواب) — doors & sections management

**HOW rules:** `.architecture/UI_STYLE_SYSTEM.md`, `.architecture/FRONTEND_STRUCTURE.md`,
`.architecture/API_INTEGRATION_GUIDELINES.md` (project root). This file is the WHAT.

**Status: Slice B2 complete** — the full page (tree, cards, bulk mode, move, reorder,
search, archive view, sections management, row context menu — Slice B1, phases 4 + 5)
plus the browser e2e flows and the two test-doc amendments
(`docs/feature-abwab-doors/plan-slice-b2.md`). The routes are `Open` (no auth) per
`plan.md` §10 — **do not** include this feature in a `dev → main` release until write
protection lands.

## What this feature does

Renders the `GET api/abwab/tree` snapshot as a tree and as drill-down cards at
`/abwab`, and drives the eleven write endpoints — create, edit, move, reorder, bulk
move, bulk archive, archive, restore, and the three section commands — with
optimistic-concurrency conflicts (`409`) always surfaced, never swallowed or
auto-retried.

## Render chain & key pieces

- `pages/abwab-page/` — the route shell: parses all six URL keys into state, composes
  every child below, and delegates all overlay/dialog orchestration to
  `state/abwab-page-overlays.controller.ts`.
- `state/abwab-page-overlays.controller.ts` — owns open/closed state and the dispatch
  glue for the door modal, single/bulk archive confirm, the move picker, the sections
  modal, and the row context menu. Split out of the page component once composing six
  overlays pushed that file toward the component-TS soft threshold
  (`FRONTEND_STRUCTURE.md`'s Large Page Split guidance) — it holds state/orchestration
  only, no template of its own.
- `components/abwab-toolbar/` — «كل الأبواب» + one tab per section (composing
  `qd-tabs`/`qdTab`, **no** «الأبواب الرئيسية» tab per `plan.md` §5.1), the name+alias
  search box, and the tree/cards view toggle. `hideSectionControls` hides the tabs and
  the view toggle while the archive view is active — they have no live section
  grouping to act on there — leaving only search, which still filters the archive tree.
- `components/abwab-tree/` — presentational tree (`role="tree"`/`treeitem`, full ARIA,
  roving tabindex) + `abwab-tree-keyboard.controller.ts`, a pure, DOM-free key model
  (RTL-mirrored per the `qd-tabs` precedent: ArrowLeft expands/enters, ArrowRight
  collapses/exits). Renders **flat** (one row per visible node, `aria-level` conveys
  depth) rather than nesting `role="group"` per level. Inline reorder editing (click
  the order number → input, Enter commits, Escape reverts) dispatches through
  `reorderDoor`. Right-click (and the keyboard `ContextMenu`/`Shift+F10` path) opens the
  row context menu via `menuRequested`; the menu itself is rendered by the page shell.
- `components/abwab-cards/` — the drill-down grid: `cardId` names only the
  drilled-into parent (not a full path array) — the breadcrumb chain is derived by
  walking `parentId` up from it via `byId`, so the URL never needs an array. Fails
  closed to the root level for an archived or unknown `cardId` (M25/M31).
- `components/abwab-archive-view/` — the archived hierarchy, restore-only
  (`plan.md` §4.5). A-live vs A-arch is read straight off the builder's tree partition
  (`node.depth === 0` ⇒ restorable, `depth > 0` ⇒ parent is archived ⇒ restore disabled
  with «استرجع الأب أولًا») — never re-derived by walking `byId`. No child-count badge:
  every archived door's live-child count is always 0, so the badge would be meaningless.
- `components/abwab-side-panel/` — active door + single-door operations (add child,
  edit, move, archive) plus bulk mode: the toggle, its `.on` state (tint + accent-text
  + hairline, **not** a solid fill — the first allowed-green fix, `plan-slice-b.md` T503),
  and the bulk bar (count, names, bulk move/archive/clear). **No** relations/protection
  entries (`plan.md` §5.1). No reorder button — the tree's own inline number editor is
  the one reorder affordance; a second control doing the same thing would be redundant.
- `components/abwab-move-picker/` — the two-stage destination picker shared by single
  and bulk move: stage one picks a section (including «بلا قسم»), stage two picks a
  destination door in it or «كباب رئيسي». Renders flat, indented by depth, rather than
  a collapsible tree (every door at any depth is already a valid target — see
  Gotchas). `excludedIds` is the moved door(s) plus every descendant, the client half of
  the cycle guard; the server's `409 WouldCycle` stays authoritative.
- `components/abwab-sections-modal/` — list / add / rename / delete-empty. Takes its
  three write functions as inputs (bound by the page to
  `state/abwab-sections.controller.ts`) rather than injecting a service, so its own spec
  exercises the 409/success outcomes without the facade/controller chain. Rename always
  reads the section's row from the live `sections` input at submit time, never a value
  captured when edit mode opened.
- `components/abwab-door-modal/` — add/edit door: name/description/ayah-text/alias
  chips (composing the extended `qd-chip` with its `removable` affordance — the second
  allowed-green fix), a dirty guard on close, and an inline error surface for the
  backend message. Composes `.qd-modal`/`.qd-modal-backdrop` + `qdModalScrollLock`.
- `components/abwab-announcer/` — one `aria-live="polite"` `role="status"` region for
  operation messages; a feature-scoped stand-in for a toast primitive this one
  feature does not warrant (`plan-slice-b.md` §4.1).
- `state/abwab-snapshot.facade.ts` — owns the tree snapshot, loading/error/empty
  state, `load()`/`refresh()`.
- `state/abwab-tree.builder.ts` — pure: DTO → `AbwabTreeSnapshotVm` (live/archive
  partition, gap-tolerant ordering, per-section filtering, name+alias search, and
  `pruneAbwabNodesToVisible` — rebuilds a node list to only the search-visible ids,
  recursing into children, backing the tree/archive-view search filter).
- `state/abwab-selection.store.ts` — single selection + bulk set, rebinds by id after
  every refresh, dropping ids a write made vanish. Bulk mode is unavailable while the
  archive view is active.
- `state/abwab-write.controller.ts` — every door write, plus the section commands
  `state/abwab-sections.controller.ts` delegates to it, the outcome→message mapping,
  and the 409 policy (see Gotchas below) — one policy for both aggregates, not
  duplicated per command.
- `state/abwab-sections.controller.ts` — the section-facing write surface: reads
  `sections` live from the facade snapshot (never cached) and forwards
  create/rename/delete to the shared write controller above.
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
| `card` | positive int (the drilled-into parent — the breadcrumb chain is derived from it, not stored as an array) | the top card level |
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
- **Bulk-archive's confirm count is a union, not a sum.** If one selected door is an
  ancestor of another selected door, summing each door's own live-subtree count
  double-counts their shared descendants. `AbwabWriteController#bulkArchiveConfirmMessage`
  walks every selected subtree into one id set and counts the set.
- **Archived doors are read-only.** The archive view offers restore only — no
  edit/move/reorder/add-child/bulk. Any other control on an archived door would be
  dead by definition. The header "add root" button and its in-tree ghost affordance are
  both hidden while the archive view is active, for the same reason.
- **Restore's descendant set is not previewable.** `isArchived` carries no `deletedAt`,
  so a descendant archived in the same operation and one archived earlier are
  indistinguishable in the snapshot. The UI promises nothing about which descendants
  come back — no preview, no count. Do not "fix" this by guessing a count
  (`plan-slice-b.md` §6.4, R12).
- **The move picker's destination list renders flat, not as a collapsible tree.**
  `plan.md` §4 describes "an expandable door tree"; every door at any depth is already
  a valid "nest anywhere" destination, so a per-node collapse/expand toggle would add
  UI complexity without adding a reachable destination. Recorded as a deliberate
  simplification, not a silent deviation.
- **Bulk is all-or-nothing.** One stale token fails the whole bulk operation with a
  single `409`. The backend's bulk-conflict response carries no per-door
  identification (verified against `AbwabDoorsController.cs` /
  `ApiMessages.AbwabDoorStaleVersion`), so the locked conflict message names every
  door in the *attempted* selection, and that selection is preserved rather than
  cleared on conflict — a single-door conflict, by contrast, clears just that door's
  now-invalidated selection.
- **The section-delete conflict copy the UI actually shows is the backend's, not the
  plan's.** `abwab.labels.ts#sectionDeleteConflict` («القسم يحتوي أبوابًا نشطة») is the
  string `plan-slice-b.md` §2 locks and is used only as a fallback when a 409 carries
  no backend message. In practice `AbwabSectionsController.cs` / `ApiMessages.cs`
  always sends «لا يمكن حذف القسم لاحتوائه على أبواب حالية» on this conflict, and the
  write controller's 409 policy prefers the backend message when present — so that is
  what renders. Reported as a contract-vs-decision conflict rather than silently
  reconciling one string into the other; `abwab-sections.controller.spec.ts`'s M27
  test is pinned against the real backend copy.
- **`AbwabDoorDto` carries no audit-seed columns on the wire** (no `createdAt`/
  `createdBy`/`approvedAt`/`approvedBy` — verified against the generated model and
  `openapi/swagger.json`). The door modal's tracking-data box shows only what is
  honestly derivable (archive status) or an explicit "not available yet" placeholder
  for added-by/approved-by; it does not fabricate a date the DTO cannot back.
- **Labels use the TDZ getter pattern**, same as `features/words/README.md`: read
  `abwab.labels.ts` consts via component **getters**, never `readonly` field
  initialisers, or they resolve to `undefined` in the bundled test build.
- **Zero dead controls.** Nothing for relations, protection, templates, per-node flags,
  or the «الأبواب الرئيسية» tab, anywhere in this feature.
- **A `204 No Content` arrives as a `null` envelope, not `{isSuccess, data}`.** Single-door
  archive (`DELETE api/abwab/doors/{id}`) and a successful section delete
  (`DELETE api/abwab/sections/{id}`) are the two routes that answer 204, and Angular's
  `HttpClient` parses an empty body as `null`. `abwab-write.controller.ts#handleSuccess`
  therefore treats a null response as a payload-less success: only a success is ever a 204,
  since every failure arrives as a 4xx through `catchError`. Dereferencing `response.isSuccess`
  first — which is how this originally shipped — throws, gets swallowed as a transport error,
  and leaves the UI reporting failure while the backend write has committed. Vitest missed it
  because the specs mocked `AbwabApi` with well-formed envelopes; the browser suite caught it.
  Both the null-envelope path and the real 204 flush are now pinned by tests, and
  `abwab-archive.e2e.ts` drives archive through the UI end to end.

## Browser e2e (Slice B2)

`Frontend/quran-dashboard-ui/e2e/abwab-structure.e2e.ts`, `abwab-operations.e2e.ts`,
`abwab-archive.e2e.ts`, and `abwab-url-and-a11y.e2e.ts` drive this page end to end —
sections, root/child doors with alias chips, the dirty guard, inline reorder, single and
bulk move, bulk archive, the row context menu, archive/restore including the
parent-must-restore-first rule and the detach announcement, all six URL query keys, and
the tree's ARIA/roving-tabindex/RTL keyboard model. `e2e/fixtures/abwab.ts` is the shared
sandbox: each test creates its own uniquely-named section over the API, and tears down by
archiving every door it created and then deleting the section — see `e2e/README.md` and
`TESTING_STRATEGY.md` §6 for the residue this leaves in the local dev DB.

## Related

- Plan: `docs/feature-abwab-doors/plan-slice-b.md` (Slice B1/B2 interaction matrix),
  `docs/feature-abwab-doors/plan-slice-b2.md` (Slice B2, this e2e slice), and
  `docs/feature-abwab-doors/plan.md` (Slice A, backend).
- Design contract: `docs/design-preview/abwab-tree-concept.html`.
- Shared UI primitives: `../../shared/README.md`.
