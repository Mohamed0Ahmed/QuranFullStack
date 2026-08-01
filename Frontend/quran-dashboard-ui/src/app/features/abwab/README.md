# Abwab feature (الأبواب) — doors & sections management

**HOW rules:** `.architecture/UI_STYLE_SYSTEM.md`, `.architecture/FRONTEND_STRUCTURE.md`,
`.architecture/API_INTEGRATION_GUIDELINES.md` (project root). This file is the WHAT.

**Status: Slice B2 complete**, plus the superset's global order, door relations, and the
templates workshop — the full page (tree, cards, bulk mode, move, reorder, search, archive
view, sections management, row context menu — Slice B1, phases 4 + 5), the browser e2e flows
and the two test-doc amendments (`docs/feature-abwab-doors/plan-slice-b2.md`), the relations
modal with its three entry points, and `/abwab/templates`. The routes are `Open` (no auth)
per `plan.md` §10 — **do not** include this feature in a `dev → main` release until write
protection lands; that block now covers **seven** more write-capable routes (template
create/delete, node add/edit/reorder/delete, and the apply).

## What this feature does

Renders the `GET api/abwab/tree` snapshot as a tree and as drill-down cards at `/abwab`,
reads a door's relations from `GET api/abwab/doors/{doorId}/relations`, authors reusable door
subtrees at `/abwab/templates`, and drives the **twenty** write endpoints — create, edit,
move, reorder, bulk move, bulk archive, archive, restore, the three section commands,
relations add/delete, template create/delete, template-node add/edit/reorder/delete, and the
apply — with optimistic-concurrency conflicts (`409`) always surfaced, never swallowed or
auto-retried. **Twenty-four** endpoints in all across the two data-access files (fifteen +
nine), four of them reads.

## Render chain & key pieces

- `pages/abwab-page/` — the route shell: parses all six URL keys into state, composes
  every child below, and delegates all overlay/dialog orchestration to
  `state/abwab-page-overlays.controller.ts`.
- `state/abwab-page-overlays.controller.ts` — owns open/closed state and the dispatch
  glue for the door modal, single/bulk archive confirm, the move picker, the sections
  modal, the relations modal (open/closed + anchor + mode only), and the row context
  menu. Split out of the page component once composing six
  overlays pushed that file toward the component-TS soft threshold
  (`FRONTEND_STRUCTURE.md`'s Large Page Split guidance) — it holds state/orchestration
  only, no template of its own. **Provided by `AbwabPageComponent`, not
  `providedIn: 'root'`** — see the Gotchas below.
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
  the order number → input) dispatches through `reorderDoor`, and **Enter is the only
  commit — blur and Escape both cancel.** Blur used to commit; that made clicking away
  from a half-typed number resequence a scope the user never confirmed, and it is the one
  grammar in this feature where an unconfirmed value could be written. Enter-only matches
  the workshop's two inline authoring rows, which already commit on Enter with no submit
  button (see below). Saying "Enter commits, Escape reverts" and staying silent on blur is
  what let the two drift apart, so all three are named here. Rows carry the contract's two hover actions (`abwab-tree-concept.html:114`,
  `:436-443`): `＋` (add child) and `⋯` (open the row menu), revealed on hover and on the
  selected row, hidden in bulk mode, and kept out of the tab order so the roving-tabindex
  invariant holds. `⋯`, right-click, and the keyboard `ContextMenu`/`Shift+F10` path all
  emit `menuRequested` **with an anchor point** — the pointer position for the mouse paths,
  the focused row's rect for the keyboard one — and the page shell composes the shared
  `qd-context-menu` (`../../shared/ui/context-menu/`) there, projecting its own operation
  buttons in (Slice A, phase 6 — both `abwab-page` and `abwab-templates-page` compose it now,
  each keeping only its own page-specific items and, for the templates workshop, the
  root-vs-node item swap).
  **Every** row carries the «علاقات» flag, and it is a control: dimmed to a muted hairline at
  zero, accent-tinted once the door has relations, and clicking it emits `relationsRequested`,
  which the page turns into select-the-door-then-open-the-relations-modal. It renders at zero
  too because a flag that only appears when there is something to see cannot answer "does this
  door have relations?" — the absent state was indistinguishable from a door the user had not
  looked at. Like the two row actions it is `tabindex="-1"`, so the roving-tabindex invariant
  holds, and it is inert in bulk mode, where a row click means "toggle this door". The archive
  view and the cards still render no flag, since an archived door's visible relation count is
  always 0 (see the derivation in the Gotchas).
  A branch row carries **three** count badges: direct live children (the design contract's
  own, `abwab-tree-concept.html:107`), total live descendants at any depth, and the deepest
  live nesting below the door (`ع3` = three levels). The last two are a commissioned
  extension of the contract, not a missed line. All three are live-only, both derivations are
  memoized on the node by `abwab-tree.builder.ts`, and each carries its own Arabic
  `aria-label`, since on screen they are bare numerals — a chain of three and a fan of three
  both show `3` descendants but `ع3` vs `ع1`, and the label is where that meaning lives.
  **Row width priority, widest to narrowest: name > order pill > actions > children count >
  descendants/depth badges > flag.** The name is the only shrinkable item (`.qd-truncate`);
  everything else is `flex: none`. Below `$qd-bp-tablet-max` the descendants and depth badges
  are dropped rather than everything being squeezed — the contract's own children count
  survives at every width.
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
  edit, move, relations, archive) plus bulk mode: the toggle, its `.on` state (tint +
  accent-text + hairline, **not** a solid fill — the first allowed-green fix,
  `plan-slice-b.md` T503), and the bulk bar (count, names, bulk move/relations/archive/
  clear). **No** protection entry (`plan.md` §5.1). No reorder button — the tree's own inline number editor is
  the one reorder affordance; a second control doing the same thing would be redundant.
  This panel is the second of the contract's three add-child paths; the tree row's own
  `＋` and the row menu are the other two.
- `components/abwab-move-picker/` — the two-stage destination picker shared by single
  and bulk move: stage one picks a section (including «بلا قسم»), stage two picks a
  destination door in it or «كباب رئيسي». Renders flat, indented by depth, rather than
  a collapsible tree (every door at any depth is already a valid target — see
  Gotchas). `excludedIds` is the moved door(s) plus every descendant, the client half of
  the cycle guard; the server's `409 WouldCycle` stays authoritative.
- `components/abwab-sections-modal/` — list / add / rename / delete-empty, with full
  dialog semantics and a dirty guard as of Slice C (a typed section name or an altered
  rename draft raises the door modal's confirm strip; an opened-but-unedited rename is
  not dirty). **Its drafts live on the component, and the page hosts it as a static
  sibling, so it must reset them on open** — unlike the door modal, whose drafts sit in
  a child that `@if (open())` destroys. Skip that reset and «تجاهل التغييرات» hides the
  draft instead of discarding it. Takes its three write functions as inputs (bound by the page to
  `state/abwab-sections.controller.ts`) rather than injecting a service, so its own spec
  exercises the 409/success outcomes without the facade/controller chain. Rename always
  reads the section's row from the live `sections` input at submit time, never a value
  captured when edit mode opened.
- `components/abwab-door-fields-form/` — the four authoring fields shared by a door and
  a template node (name/description/ayah-text/alias chips, composing the extended
  `qd-chip` with its `removable` affordance — the second allowed-green fix), their dirty
  tracking, and the inline error surface. Presentational: it injects nothing, and its
  `testIdPrefix` input is what keeps the door modal's ids byte-identical through the
  extraction. Its field labels are the door's in **both** shells, deliberately — a
  template node exists to become a door, and the locked requirement is the *same*
  authoring modal, not a parallel vocabulary.
- `components/abwab-door-modal/` — the door's shell around that form: title, context
  line, the tracking-data box, the dirty guard's confirm strip, and the write dispatch.
  On the shared modal shell like the other five (see "All six modals share one shell"
  below); the guard strip renders in `__foot`, where it cannot scroll away.
- `components/abwab-door-picker/` — the one searchable, expandable door picker, composed
  by the relations and copy modals. Selection is consumer-owned: it renders `pickedIds`
  and emits `toggled`, and `excludedIds` hides a door **without** hiding its subtree,
  since a door may relate to its own ancestor. `testIdPrefix` keeps each host's existing
  testids. Rows compose `.qd-check-row`/`.qd-checkbox`/`.qd-truncate`, so it states no
  geometry of its own. Two things the picker owns that consumer-owned selection cannot
  cover: `single` picks the **affordance** (radio, not checkbox — a checkbox promises
  "pick any number" and anchor-pick mode takes one), and an unmatched search renders the
  picker's own «لا يوجد باب مطابق» rather than the host's `emptyMessage`, because "your
  query found nothing" and "there is nothing to pick" are different answers and only one
  of them is true when doors are one keystroke away.
- `components/abwab-template-node-modal/` — the template node's shell around the same
  form. Its submit is a **function input** bound by the workshop page to
  `AbwabTemplatesController` (the `abwab-sections-modal` precedent), and it renders no
  tracking box: a node has no archive status and no audit seed to show, which is why the
  door modal's box stayed in *its* shell rather than becoming a flag on the form.
- `components/abwab-template-tree/` — the workshop's editor tree: the doors tree's
  *language* (chevrons at any depth, an order chip, the root marked `◆` with a bold
  name, hover `＋`/`⋯`, the inline «إضافة عنصر…» row) but not its component. It renders
  a list rather than `role="tree"` — see Gotchas.
- `components/abwab-template-copy-modal/` — «نسخ إلى أبواب…»: the preview block, a
  live-doors-only expandable picker with checkbox multi-select and search auto-expand,
  and one all-or-nothing apply. Takes the doors tree and the apply function as inputs.
- `pages/abwab-templates-page/` — the `/abwab/templates` shell: the template list with
  «+ قالب جديد», the editor panel, the node/template actions, the row context menu, and
  the two confirms. It owns the overlay state itself (page-scoped) while the caches stay
  root-scoped. **Its TS sits just over the 300-line soft threshold** — deliberately, not
  yet split: ~22 of those lines are the one-line label getters the TDZ rule mandates
  (its SCSS dropped back under the 200-line threshold once Slice A phase 6 moved the row
  context menu's markup/styling onto the shared `qd-context-menu`), the page carries no
  URL state at all (unlike
  `abwab-page`, whose six URL keys were half of what forced its overlay controller out),
  and the page has no spec of its own, so an extraction here would be an unpinned
  refactor of the one file nothing verifies. **The trigger that forces the split** is a
  sixth overlay, a URL-state contract arriving on this route, or crossing the 400-line
  hard threshold — at which point the overlay signals and their handlers move to a
  page-scoped `abwab-templates-overlays.controller.ts` on the `abwab-page` precedent.
- `components/abwab-relations-modal/` — the door's relations: four display groups
  (تشابه · تضاد · «أبواب أكثر شمولية» · «أبواب أقل شمولية», empty ones dropped), the type
  segment, the direction pill with its live preview, and an expandable/searchable door
  picker that adds N targets in **one** call. Takes its read and its two writes as
  function inputs (bound by the page to `state/abwab-relations.controller.ts`), the
  `abwab-sections-modal` precedent. `anchorPickMode` inverts the picker for the bulk
  entry: the selected doors become the fixed target list and the picker single-selects
  the anchor, so the add call keeps one shape. Direction is always stated from the
  anchor's side — «أعم/أخص» appears nowhere in the copy. **The direction pill has two
  copies, one per mode**, because «المحدد» names whichever side the picker chooses and the
  modes choose opposite sides: door mode picks targets («المحدد أقل/أكثر شمولية»),
  anchor-pick mode picks the anchor («الباب المختار أكثر/أقل شمولية»). Sharing one pair
  makes the label state the opposite of what the row stores in one of the two modes. The
  picker's expand chevron is a real tab stop with `aria-expanded` — search auto-expand is a
  convenience, not the keyboard path to a nested door.
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
- `state/abwab-relations.controller.ts` — the relations-facing surface, built the same
  way: it owns only what is relation-specific (the per-door fetch and the wire↔domain
  mapping of both enums) and forwards both writes to the shared write controller, so the
  409 policy and the refresh-after-write invariant stay in one place for all three
  aggregates.
- `state/abwab-templates.facade.ts` — the template list and the selected template's tree,
  on the snapshot facade's contract (`refresh` always refetches; a failure leaves the
  previous value in place). Root-scoped: it is a cache.
- `state/abwab-templates.controller.ts` — every templates write, its refresh target, and
  the announcement. **Not** `AbwabWriteController` — see Gotchas.
- `state/abwab-url-sync.ts` — parses/builds the six query keys below, fail-closed.
- `data-access/abwab.api.ts` — the fifteen doors/sections/relations endpoints under
  `/api/abwab`; `data-access/abwab-templates.api.ts` — the nine templates endpoints.
  Two files, not one: a separate route family, and nine of them.
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

**`/abwab/templates` carries no URL state at all** — no selected-template key, no expanded
set. Deliberate: every key above is a documented contract with a fail-closed parse and a
scope-invalidation rule, and the workshop has no deep link anyone asked for. Entering the
route always starts with nothing selected.

Fails closed to the defaults on anything invalid. Switching `section`, or turning
`archive` on, clears `door` and `card` (a selection is not meaningful across scopes);
turning `archive` off restores neither.

**The URL is the single source of truth for the selection.** `AbwabPageComponent` clears
`AbwabSelectionStore` whenever a param emission carries no `door`, and every path that
selects a door (row click, `＋`, `⋯`, right-click, the keyboard menu key) writes `door=<id>`
before acting. Without that, the invalidation above would hold in the URL and silently fail
in the store — leaving the side panel offering edit/move/archive on a door that is no longer
in scope, which is exactly what §6.2's M22 cell forbids.

## Gotchas / invariants (read before changing)

- **Both pages are full-bleed on the shared page frame, not the reading-measure container.**
  `abwab-page.component.html:2` and `abwab-templates-page.component.html:2` compose
  `qd-container qd-page-frame` (Slice B2, T703) — the frame that used to be `qd-explorer-frame`,
  words-only (`styles/README.md`). `box-sizing: border-box` on the frame is load-bearing for the
  later viewport reservation (item 4), not decorative. Verified in the browser: `.abwab-page__layout`
  is its own flex **row** nested inside the frame's column-flex context with `gap: 0` — no visual
  conflict, `.abwab-page__layout`'s own `margin-block-start` supplies the gap from the header. The
  frame's fixed `padding-block-end` (sized for words' mobile stat bars) leaves the same bottom gap
  above the footer on both abwab pages that the five explorer pages already carry unconditionally
  (not media-gated) — not a new imbalance, just the shared class's existing trait extended here.
- **The doors page (`abwab-page.component`) reserves a full viewport (Slice B2, T801-T802) — the
  templates page does not.** `.abwab-page__frame` adds `min-block-size: calc(100dvh -
  var(--qd-navbar-block-size))` on top of the shared `.qd-page-frame`; abwab-local for now, see
  `UI_STYLE_SYSTEM.md` §17 "Viewport reservation" for the arithmetic, the `border-box`
  prerequisite and the generalization trigger. The reservation only bounds the frame — filling it
  is a four-link chain: `.abwab-page__layout` (`flex: 1; min-block-size: 0`) →
  `.abwab-page__main` (`align-self: stretch`) → `.qd-card.abwab-page__tree-card` (`flex: 1;
  min-block-size: 0`, replacing the old fixed `min-height: 20rem`). `.abwab-page__layout` keeps
  `align-items: flex-start` (not `stretch`) because `.abwab-page__side` is `position: sticky` and
  a stretched row would zero out its scroll travel. Scoped to the doors page's tree/cards/archive
  card only — `abwab-templates-page.component`'s editor panel keeps its own `min-block-size:
  22rem` and is out of this phase's scope.
- **`.qd-navbar` is sticky and goes inert while any modal dialog is open (Slice B2, T901/T904).**
  `.abwab-page__side`'s own sticky `top` is re-based onto `--qd-navbar-block-size`
  (`abwab-page.component.scss`) so it sits flush under the now-always-visible chrome instead of
  under the old scrolled-away navbar. Two intentional behavior changes ship with this phase, both
  named per `docs/feature-ux-slice-b/plan.md` §9: (1) the navbar is keyboard-unreachable while any
  of abwab's six modals — now including `abwab-sections-modal` and `abwab-move-picker` (T905,
  below) — is open, same doctrine `app.ts` already applies to the global words overlay; (2) both
  those two modals now lock body scroll like the other four, so the page no longer scrolls behind
  them. See `.architecture/UI_STYLE_SYSTEM.md` §17 "Chrome-inert rule".
- **`abwab-sections-modal` and `abwab-move-picker` carry `qdModalScrollLock` as of T905** — the
  two abwab modals that previously held no lock at all. Every abwab modal now participates
  uniformly in the chrome-inert rule above; do not add a seventh abwab modal without it.
- **All six modals share one shell, and it is not negotiable per modal (Slice C).** Every one is
  `.qd-modal.qd-modal--fixed` with `__head`/`__body`/`__foot`, `role="dialog"`,
  `aria-modal="true"`, an `aria-labelledby` pointing at its own `<h3>`, `qdModalScrollLock`,
  Escape-to-close, and `cdkTrapFocus cdkTrapFocusAutoCapture`. Consequences worth knowing before
  changing one:
  - **No modal states a height, and none nests a scroller.** `__body` is the single scroller;
    the four inner `max-block-size` caps that existed before Slice C are deleted. Adding one back
    re-creates the §17 specificity trap the caps were.
  - **The traps are unconditional**, unlike the words dialogs' `drawerTrapEnabled` pattern. That
    rule governs *nesting*, and abwab modals never stack — under the entity-detail overlay or
    each other. A future change that makes them nest must revisit this.
  - **Auto-capture is corrected where it lands wrong.** The door and template-node modals focus
    the name field through `abwab-door-fields-form.focusFirstField()`; the relations and copy
    modals focus the picker search through `AbwabDoorPickerComponent.focusSearch()`. Both are
    queued as a task so they land *after* the trap's own capture. Sections and the move picker
    keep plain auto-capture. Note this is verifiable only in a browser — jsdom gives every
    element a zero-size box, so the CDK's focusable check rejects the target and auto-capture
    never fires there.
  - **Shallow modals render with empty space** below their content, because `--fixed` is a fixed
    `min(92dvh, 44rem)`. That is §17's "zero resize" trade, accepted deliberately; do not "fix"
    it back to content height.
- **`.qd-navbar` sits on `--qd-z-mobile-nav` (45), not `--qd-z-sticky` (5) — the rung its own
  dropdown and mobile menu already declare, because sticky positioning makes the navbar's own
  rung a ceiling for everything inside it.** `position: sticky` unconditionally creates a
  stacking context (every engine, regardless of `z-index`), so a sticky element's descendants
  can never paint above what the element's own rung permits, no matter their own declared
  z-index. Putting the navbar on `--qd-z-sticky` — the reflexive "lowest rung" choice — would
  have clamped `.dropdown-menu` and `.mobile-menu` down to 5, breaking three real surfaces
  (verified against every `--qd-z-*` consumer): the dropdown loses to the `detail-modal-shell`
  restore control (40); `.mobile-menu`, a full-screen overlay, would paint under page popovers
  (30); and page popovers would paint *over* the sticky navbar itself on a scrolled page — a
  failure mode that didn't exist before the navbar was sticky. `--qd-z-mobile-nav` fixes all
  three while staying below `--qd-z-menu-backdrop`/`--qd-z-menu`/`--qd-z-modal-backdrop`, so a
  `qd-context-menu` and any modal still paint above the chrome. See
  `.architecture/UI_STYLE_SYSTEM.md` §17 "Sticky app chrome" for the full reasoning and the live
  verification of all four cases.
- **Refresh-after-write is an invariant, not an optimization.** Every write
  resequences its scope to `1..N`, which bumps every sibling's `xmin` too. A root-affecting
  write additionally maintains the global order (below) in the same request, which resequences
  **every live root everywhere** — so after any such write, the stale version tokens are not
  confined to one scope at all. `abwab-write.controller.ts` refetches the whole snapshot and
  rebinds every cached version (`abwab-selection.store.ts#rebindTo`) after every success
  regardless of scope, so no frontend code changes because of this — but it does mean a narrower,
  scope-only refresh would no longer be safe. Skipping the refresh reproduces spurious `409`s on
  the very next write.
- **"Dropping ids that vanished" means archived, not missing — for the bulk set.** The §4.6
  rebind rule reads as if a vanished door leaves the snapshot; none does. An archive is a soft
  delete, and `abwab-tree.builder.ts` sets **every** door into `byId`, archived ones included
  (it builds `archivedRoots` through the same `build()`), so the naive `if (node)` test never
  fired in production: a just-archived door stayed in the bulk set with a **freshly rebound**
  version. Nothing looked stale, and the next bulk submit sent it — the writer loads live rows
  only, the count mismatches, and the whole all-or-nothing operation 404s on the generic
  «الباب غير موجود». `rebindTo` therefore tests `node && !node.isArchived` for the bulk set, and
  `AbwabWriteController` filters the refs again at submit. The **single** selection keeps the
  missing-only rule on purpose: the archive-confirm flow and the URL's scope invalidation
  already clear it, and the archive view needs it to survive so restore has a subject.
- **Two independent root orders: superset vs section.** Root doors carry a second, independent
  order, `globalOrderValue`, used **only** by «كل الأبواب» (the superset —
  `activeSectionId() === null`); every section tab keeps ordering and editing by `orderValue`, and
  nested doors at any depth always render/edit `orderValue` regardless of which tab is active.
  `abwab-page.component.ts` derives `orderScope` (`'global' | 'section'`) from `activeSectionId()`
  and passes it to both `qd-abwab-tree` and `qd-abwab-cards`; the inline reorder editor commits
  against whichever space the row is currently displaying, and the `reorderDoor` wire body carries
  an explicit `scope` — `ABWAB_ORDER_SCOPE_TO_WIRE` in `abwab.models.ts` is the only place that
  maps it to the backend's numeric `AbwabReorderScope`. A `Section` write never touches the
  superset's order and a `Global` write never touches any section's order.
- **The move picker's destination list follows the superset's global order, not a per-section
  re-sort.** `AbwabMovePickerComponent` builds its flat destination list straight from
  `liveRoots`, so once the superset sorts by `globalOrderValue` the picker's destination order
  does too, even when picking a destination within one section. Deliberate: the picker is a
  destination list, not an ordered outline, so following the superset's own order there is
  coherent — pinned by a spec case in `abwab-move-picker.component.spec.ts`, not a side effect.
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
  plan's.** `plan-slice-b.md` §2 locks «القسم يحتوي أبوابًا نشطة», but
  `AbwabSectionsController.cs` / `ApiMessages.cs` always send «لا يمكن حذف القسم لاحتوائه
  على أبواب حالية» on this conflict, and the write controller's 409 policy prefers the
  backend message whenever one is present. The plan's string therefore **never renders**,
  and no constant for it exists in `abwab.labels.ts` — a "fallback" that can only be
  reached when the backend omits its own message would be dead code dressed as a
  safeguard, and the generic `writeConflictFallback` already covers that case. Reported as
  a contract-vs-decision conflict rather than silently reconciling one string into the
  other; the M27 test is pinned against the real backend copy.
- **`AbwabDoorDto` carries no audit-seed columns on the wire** (no `createdAt`/
  `createdBy`/`approvedAt`/`approvedBy` — verified against the generated model and
  `openapi/swagger.json`). The door modal's tracking-data box shows only what is
  honestly derivable (archive status) or an explicit "not available yet" placeholder
  for added-by/approved-by; it does not fabricate a date the DTO cannot back.
- **Overlay state is page-scoped; caches are app-scoped.** `AbwabPageOverlaysController` is
  provided by `AbwabPageComponent`, not `providedIn: 'root'` — the same split
  `features/words/state/*-detail.controller.ts` makes ("Not `providedIn: 'root'`: … each
  overlay adapter provides its own component-scoped instance"). Root scope would outlive
  `/abwab`, and the page renders every dialog **outside** its loading/error guard, so a
  left-open modal would paint again on re-entry before any data loads. The snapshot facade
  and the selection store stay root-scoped on purpose; only the overlay state is per-page.
- **Loading/empty/error surfaces are composed, not hand-rolled.** Every text-only loading,
  empty, and error site across `abwab-page`, `abwab-templates-page`, the template copy modal,
  and the relations modal now composes `qd-skeleton-rows`/`qd-panel-skeleton` (loading) or
  `qd-state` (empty/error) — `UI_STYLE_SYSTEM.md` §17. Only two error sites carry the single
  `actionLabel` retry §17 permits — the doors page's own snapshot-load failure and the copy
  modal's doors-load failure, both wired to `AbwabSnapshotFacade.load()` — because those are
  the two transport reads abwab previously offered no recovery from at all. The templates page's
  «اختر قالبًا» now means only what it says: `AbwabTemplatesFacade.selectedLoading` covers the
  per-template fetch window (null `selectedTemplate` throughout, since `select()` writes the id
  first), and the detail region renders `qd-skeleton-rows` there instead.
- **A skeleton's `rowTemplate` sizes its columns, never its rows.** `qd-skeleton-rows` defaults
  to a 0.75rem bar, so a call-site whose loaded row is taller must say so with a
  `--qd-skeleton-h` override on the host — the doors tree does (1.5rem, giving 32px pitch against
  the gapless tree's measured 32px row), and the templates list does (3.75rem). The primitive's
  own inter-row `gap` is **not** parameterized, so *n* skeleton rows always land one gap short of
  *n* gapless loaded rows; that residual is the primitive's, not the call-site's, and closing it
  means changing `shared/ui/skeleton/`.
- **The stats bar (item 17, Slice B2, T1001-T1004) is two `qd-result-count` instances above the
  toolbar, both derived from the existing snapshot — no backend call added.** «كل الأبواب» is
  **total live doors**, counted frontend-side (`countLiveAbwabDoors`, `abwab-tree.builder.ts`);
  the second is **doors in the currently open tab**, reading the backend-computed
  `AbwabTreeSectionDto.doorsInScopeCount` for a specific section tab, or falling back to the same
  live-only total on «كل الأبواب» itself (`countAbwabDoorsInOpenScope`). **The two numbers are
  live-only by definition — the same choice every other count in this feature makes — and are
  deliberately not reconcilable by arithmetic**: `AbwabNode.sectionId` is `number | null`, so a
  live door can belong to no section, meaning Σ `doorsInScopeCount` over every section can sit
  below the total. Do not "fix" this by summing sections instead of counting live doors, and do
  not add a test asserting the two sum. Both stats stay mounted through loading/error/loaded and
  through every tab switch (never conditionally unmounted), matching every other §17 composition
  in this feature — an unmounting stat would move the toolbar under it exactly the way the old
  per-branch loaders used to (§4.6-adjacent). Neither label goes through `countPhrase`: the shared
  component renders a "label: N" data-display line (the four words explorers' own precedent), not
  a counted-noun sentence, so the bare-count rule below does not reach it.
- **Counted door labels go through the Arabic number forms.** `archiveConfirm` and
  `movePickerTitleBulk` share one helper covering singular («باب واحد»), dual («بابين»),
  3–10 («N أبواب») and 11+ («N بابًا»). Do not interpolate a bare count into new copy —
  «سيتم أرشفة 1 بابًا» is wrong Arabic and this product is Arabic-first.
- **Labels use the TDZ getter pattern**, same as `features/words/README.md`: read
  `abwab.labels.ts` consts via component **getters**, never `readonly` field
  initialisers, or they resolve to `undefined` in the bundled test build.
- **Zero dead controls, and now no exception.** Nothing for protection or the «الأبواب
  الرئيسية» tab, anywhere in this feature. Relations became real controls with
  `abwab-relations`, and **templates became real with `abwab-templates`**: «القوالب» in the
  doors header routes to a workshop backed by nine live endpoints, so the rule holds by the
  entry existing rather than by its absence. The tree's relations flag used to be the one
  deliberate non-control — a chip with no tab stop and no handler. It is a button now
  (Slice D), so what the rule protects is stated positively instead: **every affordance that
  looks pressable is**, and the only things this feature renders as inert are pure data: the
  row's count badges.
- **Relations get no entry point and no flag in the archive view — derived, not decided.** Every
  archived door's visible relation count is always 0 (the backend hides a relation whose endpoint
  is archived, `Reads/Abwab/README.md`), so a flag there would be permanently absent and a menu
  entry would open an always-empty modal. Same derivation this README already makes for the
  archive view's child-count badge. Nobody should "add it back for symmetry". Cards render no
  flag either — the tree contract renders only `protected` there.
- **Relation writes carry no `version` and still refresh.** They touch no door row, so no `xmin`
  moves and no stale-token 409 is reachable; the only 409 those routes produce is the duplicate
  pair. They still go through `abwab-write.controller.ts` rather than around it, because they
  change `relationCount` on **two** rows of the snapshot and the refresh-after-write invariant is
  what keeps those honest. Do not add a token "for consistency" with the door writes.
- **The templates controller is deliberately not `AbwabWriteController`.** That controller's
  core invariant is refresh-the-doors-snapshot-and-rebind-every-version-token; templates carry
  no version tokens (nothing on the nine routes sends one) and are not in that snapshot. Two
  different refresh targets, so two controllers. What the two must **not** fork is the 409
  policy, so both call the module-scope `toAbwabWriteFailure` in `abwab-write.controller.ts` —
  one status→outcome mapping, two refresh targets. Do not "reunify" them by making the
  templates writes go through the doors controller; do not copy the policy either.
- **The workshop's two inline authoring rows commit on Enter, with no submit button.**
  «+ قالب جديد» opens a naming field in the list, signposted «اسم القالب… (Enter)», and the
  tree's quick-add row works the same way («إضافة عنصر… (Enter)») — both from the approved
  contract. The page's two *confirm* surfaces do have buttons; these two do not, and adding one
  to either alone would fork the page's own idiom.
- **The workshop never names one template and writes to another.** `selectedTemplate()` is
  `null` unless the loaded template's own `id` equals the selected id, and every write in the
  page (apply, quick-add, add-node, delete-template) takes its id **off that object**, never
  from `selectedTemplateId()`. Two sources for "which template?" is what would let a failed
  switch — the list highlighting B, the editor and the copy preview still showing A — send B's
  id to apply; and because a copy is detached at birth (below), there is no provenance to trace
  the wrong copies back by. This is the one place where the `AbwabSnapshotFacade` "leave the
  previous value in place on failure" contract does **not** carry over: that facade owns a
  single resource, this one changes resource on every `select()`. A refresh of the *same*
  template does not trip the check, so writes never blink the editor.
- **The apply refreshes nothing, on purpose.** It writes door rows, but
  `AbwabPageComponent.ngOnInit` calls `facade.load()` on every entry, so returning to `/abwab`
  is what makes the copies visible. Coupling the workshop to the doors facade would buy a
  fetch nobody sees. The doors snapshot *is* fetched when the copy modal opens — the picker
  is its only consumer, and the workshop is reachable directly by URL, so it cannot assume
  `/abwab` was visited first.
- **A template copy is detached at birth.** No `templateId` column, no provenance, no
  back-link, nothing marking a door as template-derived. Editing or deleting the template
  later never touches earlier copies, and the copy modal's preview says so *before* the write
  because it is the expectation this feature is most likely to invite wrongly. Do not add a
  badge, a count, or an "update all copies" path.
- **The copy modal's confirm count is the number of targets, always — never a union.**
  Selecting a door and its own descendant produces two independent copies. This is the
  deliberate opposite of bulk-archive's union count above, where archiving an ancestor already
  claims its descendants; applying a template claims nothing. Do not "fix" one into the other.
- **There is one door picker, `abwab-door-picker`, and both modals compose it.** The debt that
  kept them apart is paid: the relations and copy modals each have a behavior spec, and the
  duplicated picker became a component. Selection stays **consumer-owned** — the picker renders
  what `pickedIds` says and emits `toggled`, so the relations modal keeps its single-anchor rule
  in bulk mode and the copy modal its multi-select, and the picker knows about neither. Existing
  `data-testid`s survive through `testIdPrefix`, which is what made the extraction provably
  behavior-preserving (both specs passed it unedited). Do not re-fork it for a third caller;
  add an input. **Consumer-owned selection is not consumer-owned *affordance*:** the picker
  still has to render a control that tells the truth about how many doors are choosable, which
  is what `single` is for. Anchor-pick selection is select-only for the same reason — a radio
  group offers no click-the-selected-one-to-clear gesture, so the component does not invent one.
- **The template tree renders a list, not `role="tree"`.** `AbwabTreeComponent` earns that
  role with a full RTL-mirrored keyboard model (`abwab-tree-keyboard.controller.ts`); claiming
  the role without the arrow-key model would promise a navigation contract the workshop does
  not implement. `aria-level` still conveys depth and every control is a real tab stop.
  Reusing `AbwabTreeComponent` itself was rejected up front, not discovered mid-work: it is
  typed on `AbwabNode` and carries selection/bulk/roving-tabindex/URL concerns this page has
  none of, plus a spec suite pinned to that behavior.
- **The M10/M33 `sectionId` defense-in-depth stays in the door modal's shell**, not in the
  extracted `abwab-door-fields-form`. The form has no concept of a section and must not
  acquire one; the shell is the layer that decides *whether* a section applies.
- **A `204 No Content` arrives as a `null` envelope, not `{isSuccess, data}`.** Single-door
  archive (`DELETE api/abwab/doors/{id}`), a successful section delete
  (`DELETE api/abwab/sections/{id}`), a relation delete
  (`DELETE api/abwab/relations/{id}`), and **both templates deletes**
  (`DELETE api/abwab/templates/{id}`, `DELETE api/abwab/template-nodes/{id}`) are the routes
  that answer 204, and Angular's
  `HttpClient` parses an empty body as `null`. `abwab-write.controller.ts#handleSuccess`
  therefore treats a null response as a payload-less success: only a success is ever a 204,
  since every failure arrives as a 4xx through `catchError`. Dereferencing `response.isSuccess`
  first — which is how this originally shipped — throws, gets swallowed as a transport error,
  and leaves the UI reporting failure while the backend write has committed. Vitest missed it
  because the specs mocked `AbwabApi` with well-formed envelopes; the browser suite caught it.
  Both the null-envelope path and the real 204 flush are now pinned by tests, and
  `abwab-archive.e2e.ts` drives archive through the UI end to end. The relation delete rides on
  that same already-pinned `handleSuccess` branch but has no test of its own
  (`docs/TESTING_DEBT.md`).

## Browser e2e (Slice B2)

`Frontend/quran-dashboard-ui/e2e/abwab-structure.e2e.ts`, `abwab-operations.e2e.ts`,
`abwab-archive.e2e.ts`, `abwab-url-and-a11y.e2e.ts`, and `abwab-global-order.e2e.ts` drive this
page end to end — sections, root/child doors with alias chips, the dirty guard, inline reorder,
single and bulk move, bulk archive, the row context menu, archive/restore including the
parent-must-restore-first rule and the detach announcement, all six URL query keys, the tree's
ARIA/roving-tabindex/RTL keyboard model, both halves of the section-delete contract (409 while a
live door remains, and the `204 No Content` success once its doors are archived), and the
superset/section order independence (a `Global` reorder leaves a section's `orderValue` untouched
and vice versa). Because a `Global` reorder resequences every live root in the database, these
five specs run in their own single-worker Playwright project — see `e2e/README.md`.

`e2e/fixtures/abwab.ts` is the shared sandbox: each test creates its own uniquely-named
section over the API, and tears down by archiving **every live door in that section** — not
only the ids it handed out, since flows create doors through the UI too — and then deleting
the now-empty section. Teardown re-reads each door's version immediately before archiving it:
every write resequences the scope, so archiving from one up-front snapshot succeeds once and
then `409`s silently for the rest, which is what previously left live sandbox doors and
undeleted sandbox sections behind. See `e2e/README.md` and `TESTING_STRATEGY.md` §6 for the
residue that legitimately remains.

## Related

- Plan: `docs/feature-abwab-doors/plan-slice-b.md` (Slice B1/B2 interaction matrix),
  `docs/feature-abwab-doors/plan-slice-b2.md` (Slice B2, this e2e slice), and
  `docs/feature-abwab-doors/plan.md` (Slice A, backend).
- Design contracts: `docs/design-preview/abwab-tree-concept.html`,
  `abwab-relations-concept.html`, and `abwab-templates-concept.html`.
- Shared UI primitives: `../../shared/README.md`.
