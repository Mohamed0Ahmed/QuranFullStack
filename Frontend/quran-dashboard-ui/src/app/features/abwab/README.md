# Abwab feature (الأبواب) — doors & sections management

This README is the nearest owner of current Abwab page behavior. Structural thresholds, shared
visual primitives, and API integration rules remain in the canonical documents linked under
`Related`; the rules below are the feature-local contract.

## Scope and access

Abwab provides the public doors tree and drill-down cards at `/abwab`, public relation reads,
the public template list/detail/tree at `/abwab/templates`, and the authoring flows for doors,
sections, relations, and templates.

Backend authorization is final for every write. Frontend capability checks only shape the UX:
affordances receive the relevant capability, page/controller handlers recheck it, and write
controllers check once more before dispatch. They do not authorize a request.

- A write `401` starts login once and never retries the mutation.
- A write `403` refreshes access without retrying; the page then closes or disables stale write
  state.
- Anonymous and read-only visitors retain the tree, cards, archive, relation reads, template
  list/detail, and template-tree rendering. Archive restore stays visible but disabled with an
  explanation when restore permission is absent, because hiding it would make the hierarchy
  misleading.

## What this feature does

The feature renders the tree snapshot, door relations, and reusable template subtrees, and owns
the door, section, relation, and template write families. Optimistic-concurrency conflicts
(`409`) are always surfaced and are never swallowed or automatically retried. The public methods
in `data-access/abwab.api.ts` and `data-access/abwab-templates.api.ts` are the current route list;
do not duplicate endpoint totals here.

## Component and ownership map

The repository-wide file thresholds live in `FRONTEND_STRUCTURE.md`. When a local split boundary
is named below, preserve that responsibility boundary rather than splitting to satisfy a count.
Shared geometry and primitive mechanics live in `UI_STYLE_SYSTEM.md`.

### Page shell and overlay ownership

- `pages/abwab-page/` is the route composition shell. It supplies settled URL state to
  `state/abwab-reveal.controller.ts`, which owns reveal target/sequence/mark state, its hold
  timer, URL handoff, and scrolling. The shell keeps derived render state, TDZ-safe label getters,
  host-only focus handoff, and thin event wrappers.
- `state/abwab-modal-url.controller.ts` owns URL-modal reconciliation: overlay kind and subject,
  the live-door guard, and the two reconciliation directions. It is page-provided and deliberately
  has no `Router` or `ActivatedRoute`; the page feeds it URL state and the interactions controller
  performs URL writes, preventing a second router owner.
- `state/abwab-page-interactions.controller.ts` orchestrates toolbar, tree, panel, context-menu,
  modal, archive, restore, permission rechecks, URL merge/replace, and focus callbacks outside the
  route shell.
- `state/abwab-page-overlays.controller.ts` is page-provided, never root-scoped, and owns the flat
  family of door/archive/move/sections/relations/context-menu overlay signals and dispatch glue.
  Keep that cohesive family together. If it reaches the architecture hard threshold or an overlay
  acquires its own URL state, split the URL-backed overlays into a controller beside
  `abwab-modal-url.controller.ts`.
- `components/abwab-modal-restore/` is the presentational control for reopening or discarding a
  retained overlay. It reads no URL and owns only its focus entry point. Both halves are shared
  `qdAction` owners — `secondary` for restore, `icon-only` for the `×` discard — so the pair keeps
  one focus ring, one hover, and a `--qd-hit-target-min` target on the discard that a `×` sized by
  padding alone never had. The local SCSS is now only the joined-pair geometry (the shared inner
  radii and the removed inner border) plus the retained-overlay tint the two halves share.

### Toolbar, search, tree, and counts

- `components/abwab-toolbar/` owns «كل الأبواب», one tab per section, name/alias search, and the
  tree/cards toggle. There is no «الأبواب الرئيسية» tab. Archive mode hides section controls and
  the view toggle but retains search, because archive has no live section grouping; without a tab
  strip, archive also has no root-count badge.
- **The component renders two rows, not one** (Phase 10). Row 1 is the `.qd-toolbar` proper and
  holds only search (`qd-toolbar__filters`) and the tree/cards toggle (`qd-toolbar__actions`) —
  that slot vocabulary is a single-row contract, and a variable-length section collection inside it
  was what made the sections compete with the controls for width. Row 2 is the section tablist on
  its own, below the toolbar, as `qd-tabs layout="tracks"` with `--qd-tabs-track-floor: 8.5rem`.
  Tracks sizing means the sections are equal-width, fill the row, wrap to a further row as sections
  are added, and never grow a horizontal or nested scroller — replacing the old behaviour where a
  fourth section flipped the strip to `--scrollable`. Because track width is a floor, not an item
  intrinsic width, a three- or four-digit count badge no longer reflows the strip.
- **`hideSectionControls()` gates both rows' section-owned parts independently.** It wraps the
  tablist (all of row 2) and the view toggle (inside row 1) in two separate `@if`s. The `@if` sits
  *outside* `<qd-tabs>` rather than inside a sections wrapper, so archive mode drops row 2 entirely
  instead of leaving an empty, gap-bearing row. Row 1 always renders, because search survives
  archive mode.
- Search has view-specific semantics:
  - The tree marks matches in place and hides nothing; zero matches leave the hierarchy visible
    with a zero count.
  - Cards filter the current displayed depth from unpruned nodes, so drillability and child counts
    still come from the real node. Archive search filters through
    `pruneAbwabNodesToVisible`. The door pickers remain filtering surfaces.
  - Empty versus no-match copy is decided from `AbwabSearchResult.isFiltering` and whether the
    unfiltered level contains data. `searchAbwabNodes` trims the query, so whitespace alone is not
    filtering and must not produce no-match copy.
  - Since Phase 8 the running mode is **stated in visible copy**: the search is a `qd-form-field`
    whose helper is `searchScopeHintTree` / `searchScopeHintCards` / `searchScopeHintArchive`,
    linked to the input through the field's generated `aria-describedby`. The three strings are
    deliberately distinct — one shared hint would be the
    first step towards the shared search algorithm this feature does not have.
  - The visible match count sits beside the input. An always-mounted hidden `role="status"` speaks
    the settled count once, 500 ms after typing stops; clearing speaks nothing. It does not use
    `qd-abwab-announcer`, whose channel is for one-shot reveal/write messages.
  - Search expansion is derived, not stored: the rendered set is manual expansion plus the current
    `searchExpandedIds` from `autoExpandedIds`. Replacing that set per query closes branches opened
    only by an older query and restores manual expansion when search clears. A branch derived open
    by the current query cannot be collapsed until that query stops deriving it.
  - Tab badges count root doors through `rootCountBySectionId`. The separate
    `doorsInScopeCount` counts doors at any depth. Visible Latin digits are `aria-hidden`; the
    visible title and tab `aria-label` carry the root-scope counted-noun phrase from
    `ROOT_DOOR_FORMS` so the two totals remain distinguishable.
- `components/abwab-tree/` is a flat `role="tree"`/`treeitem` ARIA tree: one visible row per
  node, depth through `aria-level`, and an RTL-mirrored keyboard model in
  `abwab-tree-keyboard.controller.ts`
  (ArrowLeft expands/enters; ArrowRight collapses/exits). Since Phase 8 that movement is **not
  this feature's code**: `shared/ui/hierarchy/hierarchy-keyboard.directive.ts` (F16) owns row
  flattening, Arrow/Home/End resolution, direction mirroring and roving DOM focus, and the local
  controller is a thin adapter that adds only the door-domain keys (Enter selects, Space toggles
  bulk, ContextMenu/Shift+F10 open the row menu). The directive finds a row by the neutral
  `data-qd-hierarchy-id` attribute, never by an abwab test id, and the archive tree runs the same
  owner. Indentation is bounded at six levels (`--abwab-tree-depth-budget`) so a deep branch
  cannot push the name column off the row. The row follows roving tabindex.
  Reorder is the deliberate exception: its real button joins the roving order because it is the
  only keyboard reorder path; relation and hover actions stay at `tabindex="-1"`.
- Inline door reorder commits only on Enter; blur and Escape cancel. Blur must not write a
  half-entered order. The `＋` and `⋯` actions are **always visible** (D46 — a hover-only row
  action is unreachable by touch and invisible to anyone scanning the row; the old
  `visibility: hidden` reveal is gone). They
  disappear in bulk mode and remain outside the tab order. Every row control — chevron, `＋`, `⋯`
  — is the shared `qdAction="row-action"` owner sized by a local `--qd-action-size`.
- **`qd-hit-target` is applied to the chevron and to nothing else in a row.** The utility grows a
  control symmetrically in *both* axes to `--qd-hit-target-min`, which has one safe shape and one
  unsafe one:
  - **Safe — asymmetric neighbours.** Only the chevron's box is expanded; the things beside it
    (bulk checkbox, order chip, order input, template root marker, relations chip) are not. The
    expansion is invented area, so lifting the un-expanded neighbour above it costs the chevron
    nothing real. Block-axis containment comes from the row: every Abwab row carrying a hit-target
    is itself at least `--qd-hit-target-min` tall (`--qd-control-lg` at Compact), so a chevron can
    never be hit from the row above or below. Inline containment comes from paint order, and
    because equal-`z-index` positioned siblings paint in document order, which rule applies depends
    on where the neighbour sits: one that **follows** the chevron needs only `position: relative`
    (order chip, order input, template marker); one that **precedes** it needs
    `position: relative` **plus `z-index: 1`** (the bulk checkbox). These are paint-order rules
    only — no geometry, spacing, or visual treatment changes.
  - **Unsafe — symmetric neighbours; do not use `qd-hit-target` here.** `＋` and `⋯` sit
    `--qd-space-1` (4px) apart with 20px faces, and *both* boxes expand 12px per side. The overlap
    is `12 + 12 − 4 = 20px` straddling the gap and reaching 8px into each button's face. The area
    is conserved: `z-index` does not remove the theft, it only elects which button suffers it —
    and electing `＋` is the worse outcome, because `⋯` renders for every reader while `＋` is
    permission-gated, and `onAddChildClick` selects the door and opens an authoring dialog whereas
    `onMoreClick` only opens a menu. **The tell is that both operands are expanded, not that the
    ladder got deep.** When two expanded boxes meet, fix the geometry, never the stacking.
- **The `＋`/`⋯` pair grows in the block axis only:** `.abwab-tree__act` and
  `.abwab-template-tree__act` carry `min-block-size: var(--qd-hit-target-min)` and no utility.
  Only the *inline* axis was ever contested — the row is already at least `--qd-hit-target-min`
  tall, so a full-height button steals nothing from the rows above or below, and a 20px inline
  width keeps the two boxes from ever meeting. The result is a `20×44` target at zero inline cost
  and with no stacking rule. Do not "simplify" this to the visible 20px height: a `20×20` target is
  off the `32/40/48` control scale, and it reads Plan §1.4's Compact clause as if it repealed
  Phase 8 task 4 and D46, which are unqualified. On WCAG 2.2 SC 2.5.8 the pair depends on the
  *spacing exception* rather than on size: 24px-diameter circles centred on two 20px faces `4px`
  apart are exactly **tangent** (radii `12 + 12` = centre distance `24`), so they do not intersect
  and the exception applies — but with no margin at all, which is the second reason the block axis
  carries the target. Widening the gap to 24px or growing both faces to 44px would also remove the
  overlap, but each spends 20–48px of inline room per row on two secondary icons and breaks the
  Golden row density the chevron and order chip set. Compact keeps the visible `--qd-control-lg`
  (48px) shape, which the local `block-size` already wins over the 44px floor.
- **Both tree rows therefore carry no block padding**, and that is deliberate rather than an
  oversight to tidy up. Under `box-sizing: border-box` with `align-items: center`, the row's
  content box is sized by its tallest child, which since the change above is the 44px action — so
  `padding-block: var(--qd-space-1)` would push the border box to **52px**, an ~18% density loss on
  a data-dense workspace. With the padding gone the row measures exactly `--qd-hit-target-min`
  (44px) above Compact and `--qd-control-lg` (48px) at Compact, which is precisely the row height
  Phase 8 task 1 asks for at Compact. Row separation comes from the hover/selected background and
  the inline-start thread, not from padding. Re-adding block padding to `.abwab-tree__row` or
  `.abwab-template-tree__row` silently re-inflates every row; change the action's target instead if
  the height ever needs to move. `.abwab-tree__header` keeps its own block padding — it is a column
  header strip, not a row.
  `⋯`, right-click, and ContextMenu/Shift+F10 all emit
  `menuRequested` with a pointer or focused-row anchor for the shared `qd-context-menu`. The doors
  and templates pages each compose that shared shell with their own operations; templates also
  keep their root-versus-node item swap.
- Every live tree row renders the «علاقات» control, including zero. The control shows the relation
  count, and zero is also distinguished by a dashed muted border, so absence is not conveyed by
  color or mistaken for “not checked.” It remains outside the roving order; in bulk mode it
  toggles that row instead of opening relations. Cards and archive omit it because archived
  relation counts and the corresponding live-row control do not apply there. Outside bulk mode it
  emits `relationsRequested`, and the page selects the door before opening relations.
- Branch rows expose three live-only values from `abwab-tree.builder.ts`: direct children, total
  descendants, and deepest nesting. A presentational header names them while each badge keeps its
  full Arabic `aria-label`. Width priority is name, order, actions, direct children,
  descendant/depth, then relation flag; only the name shrinks. At phone/tablet widths descendant
  and depth values disappear with their matching headers and grid tracks, while direct children
  remains. If the tree/page reaches an architecture hard threshold, split the tree row and the
  page archive branch respectively; row styles move with the row.

### Cards, archive, panel, move, restore, and sections

- `components/abwab-cards/` renders the bounded shared doors grid: the level is `qd-grid
  qd-grid--doors` (`14–20rem`, at most four columns, single column at Compact), so the track sizes
  live in `_tokens.scss` and not in this stylesheet; no local `grid-template-columns` rule survives.
  Cards keep drill-down and selection only and gain no
  context menu. It treats `cardId` as the current parent, not a stored breadcrumb array;
  it derives ancestors through `parentId`/`byId` and fails closed to root for unknown or archived
  ids. Empty/no-match state stays below the breadcrumb so a filtered drilled level keeps a way
  back. Each card is a real button named by the door; its bulk checkbox is a sibling, never nested
  inside the button. Cards intentionally have no context menu: adding an untested third consumer
  is not implied by tree/workshop parity.
- `components/abwab-archive-view/` uses the builder partition directly: depth-zero archived doors
  are restorable; deeper doors remain disabled with «استرجع الأب أولًا» until their archived
  parent returns. It has no child-count badge because an archived door has no live children.
- `components/abwab-side-panel/` owns the selected door actions and bulk controls, and every one
  of them is the shared `qdAction` owner (archive is the shared `danger` variant), so Abwab
  control geometry matches Access and its stylesheet is layout-only. The panel names itself as
  one `role="group"` because below Wide it *is* the page's selected-door action bar. Each write
  affordance requires its exact capability, while relation reads remain public. Reorder exists
  only in the tree's inline editor. Add-child is available from the panel, the row `＋`, and the
  row menu. Bulk mode and its selection are unavailable in archive.
- `components/abwab-move-picker/` is the shared single/bulk destination picker:
  - A persistent section tab strip swaps the destination list in place. The main-door option
    «كباب رئيسي (أعلى الشجرة)» stays with destinations, not sections; there is no “no section”
    destination because every door belongs to a section. The strip composes the `qd-tabs`
    tablist/tabpanel and RTL keyboard contract owned by `UI_STYLE_SYSTEM.md`.
  - Single selection starts from the door's section, and a same-section bulk selection can do the
    same. A cross-section bulk selection starts with no active section, a prompt instead of
    destinations, and disabled confirm because no `targetSectionId` exists.
  - Changing section clears the chosen parent and manual expansion; the search query remains so a
    user can search across sections. The tree opens collapsed, manual chevrons remain keyboard
    controls, and search derives matching paths open without mutating manual expansion.
  - Its search field and both pickers' search fields are the shared `qdControl` geometry, and
    every picker chevron is `qdAction="row-action"` + `qd-hit-target` — the pickers keep their own
    row semantics and exclusion rules, only the control geometry is shared (D22/D46). The move
    picker's destination button is the same `row-action` owner with layout-only local overrides
    (`flex`, start alignment, `color: inherit` so the picked row's own state colour still reaches
    the label); its rows follow the `--qd-hit-target-min` row floor and rise to `--qd-control-lg`
    at Compact, so a destination is a real touch target at every width. Its no-match copy is the
    shared `qd-empty-state` owner, not a hand-rolled `role="status"` paragraph. The section
    prompt stays a local `role="region"` paragraph because it is the `aria-controls` target of the
    section tab strip, which the F12 owner's fixed `role="status"` cannot express.
  - The main-door option remains available even when search has no match or cycle exclusions remove
    every root. `excludedIds` contains the moved door(s) and every descendant; this prevents an
    offered client-side cycle while the Backend's `409 WouldCycle` remains authoritative. A branch
    with no selectable child reads as a leaf rather than exposing a chevron that opens nothing.
- `components/abwab-door-restore-modal/` is the archived-door restore confirmation, distinct from
  retained-overlay restore. A root whose section was retired requires a live destination because
  sections have no restore route and the Backend refuses the old section; a child returns under
  its live parent in that parent's current section. Failure stays inline and is also announced
  because the message is inserted inside an already-focused `role="alertdialog"`.
- `components/abwab-sections-modal/` owns create, rename, reorder, and delete-empty UI. Because it
  is a static sibling, component drafts reset on every open; otherwise discard would hide rather
  than clear them. A typed create name or changed rename draft is dirty; merely opening rename is
  not. Rename and order submit against the current row from the live `sections` input. The modal
  receives all four write functions from `state/abwab-sections.controller.ts` instead of injecting
  a service, preserving its direct outcome tests.
  Order editing commits on Enter, cancels on blur/Escape, and stops Escape propagation so it does
  not close the dialog or write retained-modal URL state. An open order edit is not unsaved work.
  Keep dirty/Escape/busy behavior together; if a split becomes required, extract rename-draft
  machinery into a child form.

### Shared authoring, templates, relations, and announcements

- `components/abwab-door-fields-form/` composes `qd-form-field` + `qdControl` (F06), so the
  label/helper/error ids, the required marker and the invalid state are the shared field's and the
  ayah hint is its helper. `testIdPrefix` still names every control, but the `id`/`for` pair is now
  the field's generated per-instance one. It owns the presentational name, description, ayah text,
  aliases, dirty tracking, and inline error fields shared by doors and template nodes. It injects
  and dispatches nothing; each shell owns its write. The same door vocabulary is intentional
  because a template node becomes a door. `testIdPrefix` preserves each host's identifiers.
- `components/abwab-door-modal/` owns the door shell, context, dirty-confirm strip, and write
  dispatch; its guard stays in the footer so it cannot scroll away.
  `components/abwab-template-node-modal/` owns the template-node shell and receives its submit
  function from `AbwabTemplatesController`.
- `components/abwab-door-picker/` is the searchable expandable picker shared by relations and
  template copy. Selection remains consumer-owned. An excluded door stays visible at its true
  depth with a reason tag and disabled selection; hiding it or its subtree would remove valid
  descendants and conceal why the row cannot be selected. `single` mode uses radio semantics,
  and unmatched search uses picker-owned no-match copy rather than the host's truly-empty copy.
- `components/abwab-template-tree/` uses the door-tree visual language but intentionally renders
  a list, not `role="tree"` (G20); chevrons, order, the marked root, add/menu, and inline add remain
  its authoring language, while the retained Gotchas own the ARIA reason. Its row actions follow the
  same D46 rule as the doors tree — always visible `qdAction="row-action"`, never revealed on
  hover — and the same hit-target split: the chevron expands in both axes, the `＋`/`⋯` pair grows
  in the block axis only, and Compact raises the visible control through
  `--abwab-template-tree-row-control: var(--qd-control-lg)`, the Compact block this component
  previously lacked entirely. The copy modal is
  live-door checkbox multi-select with search expansion and one all-or-nothing apply.
- `pages/abwab-templates-page/` owns the template list/editor, node/template actions, row menu,
  confirms, and page-scoped overlays. Template list/tree caches remain root-scoped. Template
  delete stays in the page-provided `abwab-templates-page-delete.controller.ts`; node modal/menu/
  delete/copy state stays in `state/abwab-templates-overlays.controller.ts`. This route has no
  URL-owned overlay state.
- `components/abwab-relations-modal/` owns four non-empty display groups (similar, opposite, more
  general, less general), type/direction authoring, and an expandable picker with one multi-target
  add. Direction wording is mode-specific and always from the anchor's side: door mode chooses
  targets («المحدد أقل/أكثر شمولية») while anchor-pick mode chooses the anchor
  («الباب المختار أكثر/أقل شمولية»), so sharing one label pair would reverse one stored meaning.
  The picker chevron remains a focusable `aria-expanded` control; search expansion is not its
  keyboard replacement. Read/add/delete functions are inputs bound by the page to
  `state/abwab-relations.controller.ts`.
- A blocked relation identity is `(pair, type)`, not direction, through `linkedIds`. It is disabled
  in anchor-pick mode because “all selected targets already link to this candidate” is not visible
  evidence. A linked door name and delete are separate chip controls. The modal emits reveal for
  the other door and remains tree-, URL-, and scope-agnostic. Keep display, authoring, and the
  nested delete-confirm trap together; if a split becomes required, extract the add form so
  trap-yield behavior stays with its dialog.
- `components/abwab-announcer/` is the single polite feature status channel. A failure reaches
  exactly one live region:
  - A pre-mounted reserved alert, or an alert inserted into a plain dialog, announces itself and
    DROPs the announcer.
  - A new alert inside an already-focused `role="alertdialog"`, or a write with no error surface,
    KEEPs the announcer.
  `announceFailure` in `state/abwab-write.controller.ts` and
  `state/abwab-templates.controller.ts` owns that choice; changing a surface shape requires
  changing its flag in the same edit or failure will speak twice or not at all. Every success uses
  the announcer exactly once through `successAnnouncement`.

### State, cache, URL, data, and labels

- `state/abwab-snapshot.facade.ts` owns the snapshot plus loading/error/empty state and
  `load()`/`refresh()`.
- `state/abwab-tree.builder.ts` is the pure DTO-to-view builder: live/archive partition,
  gap-tolerant ordering, section scope, root/scope counts, search, and archive pruning. One walk
  produces `matchedIds`, `autoExpandedIds`, and `visibleIds` and uses one push/pop ancestor stack
  rather than allocating a path per edge. The allocation strategy must not alter those exact result
  sets.
- `state/abwab-selection.store.ts` owns single/bulk selection and rebinds ids after refresh,
  dropping vanished doors. Scope clearing is centralized here: `setSectionScope` clears the bulk
  set but keeps bulk mode; `setArchiveViewActive` clears it and exits bulk mode. This covers both
  tab and reveal-driven scope changes and prevents writes against off-scope doors.
- `state/abwab-write.controller.ts` owns all door writes, delegated section writes, outcome
  messages, announcements, refresh, and one shared `409` policy. `state/abwab-sections.controller.ts`
  reads sections live from the snapshot and forwards its four commands rather than caching rows or
  duplicating conflict handling.
- `state/abwab-relations.controller.ts` owns relation read/cache/mapping only and forwards writes
  to the shared controller. Its cache key is door id under the snapshot ETag
  (`snapshotValidator` = boot id plus tree generation):
  - Any validator movement evicts every relation entry; a null validator serves none. `304` and a
    failed snapshot refresh keep the snapshot and validator together.
  - `loadFor` may use cache; `refetchFor` forces the post-write modal read because the snapshot
    refresh is fire-and-forget and may not have landed.
  - Any future narrower invalidation must still evict on door rename because cached partner names
    and their order change without a relation-count signal. The Backend Abwab reads README records
    this requirement.
  - Never key this cache by `AbwabTreeDto.version`: that diagnostic value ignores relation writes,
    so it would serve stale lists after the writes that matter.
- `state/abwab-templates.facade.ts` is the root-scoped template list/selected-tree cache;
  `refresh` always refetches and a failure preserves the previous value.
  `state/abwab-templates.controller.ts` owns template writes, their refresh targets, and
  announcements; it intentionally does not use `AbwabWriteController` because the retained
  Gotchas define the different aggregate boundary.
- `state/abwab-url-sync.ts` is the single fail-closed owner of the seven query keys documented
  below.
- `data-access/abwab.api.ts` owns doors/sections/relations routes and
  `data-access/abwab-templates.api.ts` owns template routes. Keep the route families separate and
  take their public methods as the live list.
- `models/abwab.models.ts` owns view models. `models/abwab.labels.ts` owns Arabic strings, which
  consumers read through getters rather than readonly field initializers, which can observe the
  label module inside its temporal dead zone.
## URL contract (`state/abwab-url-sync.ts`)

| Key | Values | Absent means |
|---|---|---|
| `section` | positive int | «كل الأبواب» — every door |
| `view` | `tree` \| `cards` | `tree` |
| `archive` | `1` | the live view |
| `door` | positive int | no selection |
| `card` | positive int (the drilled-into parent — the breadcrumb chain is derived from it, not stored as an array) | the top card level |
| `q` | free text | no search |
| `modal` | one of `create` \| `child` \| `edit` \| `move` \| `sections` \| `relations`, bare while the overlay is open and suffixed `-closed` while it is retained and restorable; plus the one id-carrying form `relations-<id>-closed` | no restorable overlay |

`modal` is the only key with a cross-key rule: the four door-dependent kinds (`child`,
`edit`, `move`, `relations`) parse to nothing unless the **same** ParamMap carries a valid
`door`, because the plain forms name an overlay and never a subject — their subject is
derived from `door=` plus the snapshot, which is also why a plain `-closed` **follows** a
later selection. `create` and `sections` are door-independent. Restoring is stricter still
than parsing: a door-dependent kind also needs its door to be **live**, since `byId` holds
archived nodes and the `door=` effect checks presence only. A dead or invalid key is inert and is
not rewritten. A write key that is otherwise valid but lacks the now-resolved permission is
different: only the `modal` key is stripped, preserving the public page and every other query
key. The relations modal is read-only-capable and therefore remains restorable for every visitor.

**The one exception: `relations-<id>-closed` (ux-slice-l).** A reveal points `door=` at the
target while the overlay it just closed still belongs to the *source*, so for that one state
the key carries its subject itself:

| Form | Subject |
|---|---|
| `<kind>` | `door=` (open state — always) |
| `<kind>-closed` | `door=`, and it follows a later selection |
| `relations-<id>-closed` | door `<id>`, **pinned** — selecting another door does not move it |

Fail-closed rules: the id must be a
positive integer; an id on the **open** form is invalid (an open overlay's subject is always
`door=`, and a diverged subject there is exactly what `canOpen` forbids); an id on any other
kind is invalid (only the relations modal has a reveal). Unlike the plain forms, the
id-carrying one does **not** require a valid `door=` in the same ParamMap — that is the whole
point. Restorability is checked against the carried id instead: live and unarchived, or the
key sits inert exactly as a dead `door=` already does. Restoring writes `door=<id>` plus the
bare open key in one patch, so the open state never carries an id and every invariant above
holds again.

**One key, one retained state — decided, not accidental.** The key is single-valued, so
whatever writes it next wins: opening any modal overwrites a carried key (its restore control
vanishes for good — closing the new modal retains *that* modal's plain `-closed`, it never
resurrects the id-carrying one), a second reveal overwrites with the new source, and a section
switch or archive-on clears it with everything else. Both overwrite orders are part of the URL-state
contract so this stays a decision rather than an artifact.

**`modal` selects an overlay, never a data scope, and it enters no *caching* identity.** It
is not part of any cache key or ETag — the carried id in `relations-<id>-closed` is a restore
subject and nothing more, and specifically **not** a cache input: the snapshot read
is one unparameterized root-scoped tree GET, and the relations read is keyed by **door id and
the tree validator only** (`state/abwab-relations.controller.ts`). This is the one row of this
table a future caching design must **not** pick up — adding the key to a scope or cache input
would be a contract change, not an optimisation. Both caching layers that have since landed
honor it: the tree validator is a server-side generation counter keyed on nothing from the
URL, the snapshot read is still one unparameterized tree GET, and the relations cache is
keyed on that same server validator — never on `modal`, and never on which overlay asked.

**Restoring reopens the overlay, not a draft.** The key encodes *which* overlay is
restorable and nothing else; a reopened door modal is pristine from the snapshot, the same
way the words frames are entity views rather than saved forms. Serialising form state into
the URL is not a missing feature here, it is the thing this contract declines to do.

Which overlays are in the contract is itself a rule: only the four true modals in their
**single-subject** modes. The bulk move picker, the bulk relations anchor-pick, both
archive confirms and the row context menu never write the key — their subjects are
`bulkSet` (deliberately not URL state), a destructive confirmation that must be
re-initiated rather than restored, and a transient position.

**Reveal-in-tree writes the keys above, and it *rewrites* `modal` rather than clearing
it.** A relation chip's name reveals that door in the doors tree, and every state it can
be in is folded into **one** `buildAbwabQueryParams` patch, so there is one navigation and
no race: `door` always; `modal` always — as `relations-<id>-closed` carrying the **source**
anchor's id, so the restore control reopens the door the user came from rather than the one
they landed on (the id-carrying form and why it exists are above, under `relations-<id>-closed`).
Only a null anchor, unreachable in door mode, emits `modal: null`. Until ux-slice-l this patch
discarded the key outright, because a plain `relations-closed` follows `door=` — which this
same patch is pointing at the *target* — and would have offered the target's relations; the
id-carrying form is what removed that ambiguity. `section` **only when a section tab is active
and it is not the target's**
(«كل الأبواب» already shows every door, so narrowing to the target's tab there would be
gratuitous — and an explicit `door` in the same change survives the scope-invalidation
clear, which is what makes the cross-section case one navigation instead of two);
and `view: 'tree'` when the cards drill is open, since the item is reveal-in-*tree*.

`q` is **not** touched. Slice D cleared it because a filtering tree could leave the reveal's
target pruned; ux-slice-l removed the pruning, so the premise is gone — the target is on
screen under any query, and discarding the user's search would be a second action they did
not ask for. A reveal during a search keeps the marks and the count exactly as they were.

Three things about it are load-bearing and easy to undo by accident:

- **The mark and the scroll are keyed off the param emission, not the click.** The rows
  that must exist for either to mean anything are rendered by the change detection that
  emission triggers — and in the cross-section, cards and search cases they do not exist
  before it at all.
- **The ancestor chain is *seeded* into the tree's manual expansion, not forced.** A forced
  set is unioned with manual toggles and cannot be collapsed, so a reveal routed through it
  would lock the target's ancestors open for the rest of the session. `expandSeedIds` merges
  once and hands the chevrons straight back to the user. The input carries reveal seeds
  only: search auto-expansion moved to the tree's derived `searchExpandedIds` input, read
  inside a `computed` union and replaced wholesale per query. The page must still return the
  shared `NO_IDS` for an empty seed set, or the tree's merge effect re-runs on every tick;
  it does the same for the search input only to spare re-render churn.
- **The highlight is an outline, never a tint** — `--qd-selected-bg` *is*
  `--qd-accent-tint`, and the reveal always lands on the row it just selected, so a tint
  would be invisible by construction. See `UI_STYLE_SYSTEM.md` §17 "Reveal highlight".

The archived/missing guard is defensively unreachable — the relations read hides any
relation whose endpoint is archived, and the archive view offers no relations entry point —
and exists anyway, so an impossible state is a visible non-action with an announcement
rather than a silent broken reveal.

**`/abwab/templates` carries no URL state at all** — no selected-template key, no expanded
set. Deliberate: every key above is a documented contract with a fail-closed parse and a
scope-invalidation rule, and the workshop has no deep link anyone asked for. Entering the
route always starts with nothing selected. **Revisited when `modal` was added
and retained:** the workshop's overlays are template-editor working state
whose own subjects — the selected template, the editor node — are not URL state either, so
a `modal` key there would restore an overlay onto nothing. Adding one would also fire the
split trigger this README records for that route ("a URL-state contract arriving on this
route") for no user benefit. This is a decision, not an oversight.

Fails closed to the defaults on anything invalid. `section` is additionally validated for
**existence**, settle-gated on the snapshot: an id not in `snapshot.sections` falls back to
«كل الأبواب» and the URL is rewritten by replace — only the `section` key, because the null
scope is a superset of any selection and clearing `modal` would slam shut a sections modal
that just deleted the active section (`abwab-page.component.ts`, the section-fallback
effect). A hand-entered `archive=1&door=<live id>` fails `door` closed to `null` on
parse-in, mirroring the clear the in-app archive toggle already performs. Switching
`section`, or turning `archive` on, clears `door`, `card` **and `modal`** (neither a selection nor an overlay
over it is meaningful across scopes — the rule stays uniform for the door-independent
kinds too, because a scope switch is a context change and one rule beats a per-kind
table); turning `archive` off restores none of them. An explicit `door`/`card`/`modal` in
the same change overrides the clear.

Opening a restorable overlay is a history **push**; closing it retains it as
`<kind>-closed` by **replace**, so the closed state is not its own Back target; restoring
pushes again, so Back returns to the closed state; the restore control's X clears the key
by replace. Back past an X-clear therefore surfaces an *earlier* retained entry if one
exists: the restore control reappears, no overlay reopens. The reveal is the one path that
**rewrites** the key by push rather than replace — it is a navigation to a different door in
its own right, so Back must undo it, and undoing it restores the relations modal on the
source door along with `door=`. Since
ux-slice-l the reveal *retains* rather than discards — see `relations-<id>-closed` above —
so the source's relations are also one click away from the restore control, and while the
cache is warm reopening them costs no additional read. This mirrors the words overlay's contract
(`core/navigation/detail-overlay/detail-overlay-history.service.ts`), which is where the
shape was proven — abwab does not share that service, and deliberately did not generalise
it.

**A URL-driven close bypasses the door and sections modals' unsaved-changes confirm.**
Closing by gesture (Escape, backdrop, the modal's own button) goes through each modal's
`requestClose()` and raises the discard confirm when the form is dirty; a close inferred
from the URL — browser Back, a `-closed` emission, a scope switch — goes through the
overlay's `close` setter and drops the draft silently. Deliberate, and the direct
consequence of the URL being the single source of truth: a URL that says the overlay is
closed closes it, and restoring hands back a pristine overlay, never the draft. This stays an
explicit URL-state decision.

**The URL is the single source of truth for the selection.** `AbwabPageComponent` clears
`AbwabSelectionStore` whenever a param emission carries no `door`, and every path that
selects a door (row click, `＋`, `⋯`, right-click, the keyboard menu key) writes `door=<id>`
before acting. Without that, the invalidation above would hold in the URL and silently fail
in the store — leaving the side panel offering edit/move/archive on a door that is no longer
in scope, which is exactly what §6.2's M22 cell forbids.

## Gotchas / invariants (read before changing)

- **The move picker's open-reset must keep `open()` as its ONLY tracked dependency.**
  `abwab-move-picker.component.ts` resets the picker in an `effect` and reads `movedSectionIds`
  inside `untracked`. `AbwabPageOverlaysController.moveSectionIds`
  (`state/abwab-page-overlays.controller.ts`) is a `computed` that rebuilds a fresh array via
  `.map().filter()` on every `byId()` snapshot change, so tracking it would let a refresh landing
  mid-pick re-run the reset and silently discard a stage-two choice the user had already made. The
  caller sets the moved ids *before* opening the picker, so the untracked read still sees the right
  ones. Removing the `untracked` wrapper compiles and passes type-checking.
- **Each page declares exactly one named Golden page intent, and the shell is the only gutter
  owner (Phase 8, D01/D02).** `abwab-page.component.html:2` composes
  `qd-page-shell qd-page-shell--full-data` and `abwab-templates-page.component.html:2` composes
  `qd-page-shell qd-page-shell--split-workspace`; both replaced the earlier
  `qd-container qd-page-frame` pair, which Phase 11 deleted outright. `.qd-page-shell` owns the
  `16 / 24 / 32 / 40px` inline gutter
  and `box-sizing: border-box` (load-bearing for the viewport reservation below), while
  `.qd-page` keeps block rhythm only. The local `__frame` classes supply the column flex context
  and the bottom gap above the footer that the retired frame class used to carry; neither adds a
  second inline gutter. Each route has exactly one shell and no surviving
  `qd-container`/`qd-page-frame`/`qd-explorer-frame`.
- **The doors page (`abwab-page.component`) reserves a full viewport (Slice B2, T801-T802) — the
  templates page does not.** `.abwab-page__frame` adds `min-block-size: calc(100dvh -
  var(--qd-navbar-block-size))` on top of the page shell; abwab-local for now, see
  `UI_STYLE_SYSTEM.md` §17 "Viewport reservation" for the arithmetic, the `border-box`
  prerequisite and the generalization trigger. The reservation only bounds the frame — filling it
  is a four-link chain: `.abwab-page__layout` (`flex: 1; min-block-size: 0`) →
  `.abwab-page__main` (`align-self: stretch`) → `.qd-card.abwab-page__tree-card` (`flex: 1;
  min-block-size: 0`, replacing the old fixed `min-height: 20rem`). `.abwab-page__layout` keeps
  `align-items: flex-start` (not `stretch`) because `.abwab-page__side` is `position: sticky` and
  a stretched row would zero out its scroll travel. Scoped to the doors page's tree/cards/archive
  card only — `abwab-templates-page.component`'s editor panel keeps its own `min-block-size:
  22rem` and is out of this phase's scope.
- **The side panel is the named `18rem` rail at Wide and the sticky selected-door action bar
  below it (Phase 8, F20).** `.abwab-page__side` composes `qd-page-rail qd-page-rail--m`, so
  `--qd-rail-m` is the only place the width is written. At Wide it stays a sticky column whose
  `inset-block-start` is re-based onto `--qd-navbar-block-size` (below); at Medium and Compact the
  layout stacks and the same element becomes a bottom-anchored chrome bar (sticky
  `inset-block-end: 0`, hairline, safe-area padding), with the panel's own stylesheet laying its
  two boxes out as one row of controls. Medium is a designed mode, not a squeezed Wide: the tree
  takes the full width, and the tree's secondary counts (total descendants, deepest nesting) drop
  with their headers and grid tracks at `<= 1079px`. A short viewport (`max-height: 32rem`) drops
  the sticky positioning entirely so the bar cannot eat the page.
- **`.qd-navbar` is sticky and goes inert while any modal dialog is open (Slice B2, T901/T904).** Two intentional behavior changes shipped with this phase,
  both deliberate and both recorded here: (1) the navbar is keyboard-unreachable while any
  of abwab's six modals — now including `abwab-sections-modal` and `abwab-move-picker` (T905,
  below) — is open, same doctrine `app.ts` already applies to the global words overlay; (2) both
  those two modals now lock body scroll like the other four, so the page no longer scrolls behind
  them. See `.architecture/UI_STYLE_SYSTEM.md` §17 "Chrome-inert rule".
- **`abwab-sections-modal` and `abwab-move-picker` carry `qdModalScrollLock` as of T905** — the
  two abwab modals that previously held no lock at all. Every abwab modal now participates
  uniformly in the chrome-inert rule above; do not add a seventh abwab modal without it.
- **All six AUTHORING modals share one shell, and it is not negotiable per modal (Slice C).**
  Scoped deliberately: `abwab-door-restore-modal` is a CONFIRMATION, not an authoring modal, and
  composes the shared `qd-confirm-dialog` primitive (`shared/ui/confirm-dialog/`,
  UI_STYLE_SYSTEM §17) instead — a form plus its dirty guard is a different contract from a
  yes/no decision. Do not migrate confirmations onto this shell, or authoring modals off it.
  Since Phase 7 every authoring one is a
  `qd-modal-shell` — `variant="wide"` for sections, relations, template-copy and the move picker,
  `variant="form"` for the door and template-node modals — and the shell owns `role="dialog"`,
  `aria-modal="true"`, `aria-labelledby`, the scroll lock, Escape-to-close, the focus trap and the
  Compact `94dvh` sheet. The local SCSS is layout-only. Consequences worth knowing before
  changing one:
  - **No modal states a height, and none nests a scroller.** The shell body is the single scroller;
    the four inner `max-block-size` caps that existed before Slice C are deleted. Adding one back
    re-creates the §17 specificity trap the caps were.
  - **Authoring modals never stack with each other**, so the four that nest no
    `qd-confirm-dialog` trap unconditionally — the door and template-node modals' dirty-discard
    strip is a `role="alertdialog"` region inside `__foot` with no trap of its own, so it does
    not qualify. The one
    permitted nesting is a **confirmation dialog above exactly one authoring modal**, and the
    host yields while it is open. No modal here binds a raw `cdkTrapFocus` any more:
    `qd-modal-shell` registers open shells in a stack and enables the topmost trap only, so a
    confirm above an authoring modal leaves exactly one live trap by construction, and
    `[trapFocus]="false"` is the explicit suspend switch when a consumer needs one (the words dialogs'
    `drawerTrapEnabled` pattern, applied). Two live traps fight over focus, so a second nesting
    level — or a confirmation above a confirmation — is still forbidden, and a modal that grows
    a nested confirm must make its trap conditional in the same change.
  - **Auto-capture is aimed, not corrected after the fact.** Four modals want a control the trap
    would not pick on its own: the door and template-node modals want the name field, the
    relations and copy modals the picker search. Each of those two targets carries
    `cdkFocusInitial` — in `abwab-door-fields-form` and `abwab-door-picker` respectively, so two
    attributes serve all four modals — which is what the trap's own capture reads, so a modal
    opens with **one** focus move. The queued `focusFirstField()` / `focusSearch()` calls cover a
    capture that resolves before the target renders. Focus **return** is not this feature's
    concern: `qd-modal-shell` captures the pre-open `activeElement` and restores it synchronously
    on close, and `cdkTrapFocusAutoCapture` is deliberately absent so nothing restores a second
    time on its own schedule. Sections and the move picker want the trap's default first
    tabbable and mark nothing. For the move picker that default is not "the first control in the
    DOM": its section strip is a roving-tabindex tablist, so every cell but the active one is
    `tabindex="-1"` and the trap lands on the section the move starts from — which is the
    behaviour wanted, reached without a `cdkFocusInitial`.
  - **Shallow modals render with empty space** below their content, because the shell holds its
    named width and block geometry rather than shrinking to content. That is §17's "zero resize"
    trade, accepted deliberately; do not "fix" it back to content height.
- **`.qd-navbar` sits on `--qd-z-mobile-nav` (45), not `--qd-z-sticky` (5) — the rung its own
  dropdown declares, because sticky positioning makes the navbar's own
  rung a ceiling for everything inside it.** `position: sticky` unconditionally creates a
  stacking context (every engine, regardless of `z-index`), so a sticky element's descendants
  can never paint above what the element's own rung permits, no matter their own declared
  z-index. Putting the navbar on `--qd-z-sticky` — the reflexive "lowest rung" choice — would
  have clamped `.qd-nav__menu` and the Compact navigation sheet down to 5, breaking three real
  surfaces (verified against every `--qd-z-*` consumer): the dropdown loses to the
  `detail-modal-shell` restore control (40); the navigation sheet would paint under page popovers
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
  confined to one scope at all. **A section reorder is the same shape, table-wide**: sections
  have one order space (not per-scope like doors), so `EfAbwabSectionsWriter.ReorderAsync`
  resequences every live section on every call (`Writes/Abwab/README.md`) — the second
  whole-scope resequencer in this feature, after the doors' `Global` reorder.
  `abwab-write.controller.ts` refetches the whole snapshot and
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
  coherent rather than a side effect.
- **`AbwabTreeDto.version` is diagnostics only.** Per-row `xmin` tokens are the only
  concurrency currency; do not build snapshot-level conflict detection on it. It is **also
  not the cache validator**: the `ETag` these reads now carry is a server-side generation
  counter (backend process memory, no row data). Three distinct jobs — `version` describes,
  `xmin` detects conflicts, the `ETag` validates a cached representation — and mixing any two
  is how a diagnostics field quietly becomes a correctness one.
- **The facades hold an `If-None-Match` validator beside the value it validates, as one unit.**
  `AbwabSnapshotFacade` holds one; `AbwabTemplatesFacade` holds one for the list and one keyed
  by the selected template's id, so a validator never travels to a different template. Value
  and validator are written together on a `200`, kept together on a failure and on a `304`, and
  dropped together when the value is cleared (`clearSelection`).
- **A `304` means "keep the current value", not an error.** Angular delivers it on the error
  channel — only `2xx` counts as ok — so each facade checks `err.status === 304` **before** its
  generic error branch: loading ends, the value and validator stay, and **no error is set**.
  A `304` therefore never shows a banner, and a real failure still does.
- **The route-entry `load()` stays unconditional** (`abwab-page.component.ts`). With a validator
  held it costs a `304` and zero body bytes rather than a full snapshot; there is no TTL and no
  second cache layer in front of the facades on either end.
- **The archive view is a partition of the cached snapshot, not a cacheable resource.**
  `archivedRoots` is built client-side from the same tree entry, and toggling it issues no
  request at all. Do not give it a cache key, a validator, or a route of its own.
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
  come back — no preview, no count. Do not "fix" this by guessing a count.
- **The move picker's destination list collapses; it does not nest.** Two separate
  decisions that read as one, and the second reversed the first's earlier reading:
  - *Flat, not nested* — still true. Rows are indented by depth rather than rendered
    as a nested tree, because every door at any depth is already a valid "nest
    anywhere" destination: depth is presentation here, not structure.
  - *Collapsed, not pre-expanded* — **reversed (ux-slice-m)**. This entry used to argue
    that a collapse/expand toggle "would add UI complexity without adding a reachable
    destination", and rendered every door in the section at once. What that misses is
    the cost on the other side: a section of any size arrives as a wall of rows to
    scroll past, every one of them a destination the user did not ask to see. The list
    now opens on the section's root doors and the user expands what they want.
    The toggle is `abwab-door-picker`'s chevron contract, mirrored — not imported.
- **Five Abwab surfaces stopped truncating under D35, and five deliberately did not.** A truncated
  name may only elide behind `[title]` when a rung of the Golden §8.1 disclosure ladder carries the
  full value. The relations modal's bulk target chips, the sections modal's row names, the template
  tree's node names, the side panel's active door name, and the templates page's editor heading all
  had `title` as their *only* disclosure and now wrap instead (`min-inline-size: 0;
  overflow-wrap: anywhere`, no `.qd-truncate`, no `[title]`, and no added `tabindex`). Two of those
  are permission-conditional and were resolved on their *read-only* branch: the sections row loses
  its Rename/Delete buttons and the template-tree row loses its `tabindex` when the visitor cannot
  open the context menu, so the writable layout is not evidence that the read-only one is reachable.
  The five that keep `.qd-truncate` + `[title]` each have a real owner: `abwab-tree` and
  `abwab-archive-view` rows are `role="treeitem"` with a roving tabindex, `abwab-cards`' card and
  crumbs and the templates *list* item truncate inside a `button`, the move picker's two row kinds
  truncate inside a `button` carrying `aria-label`, and `abwab-door-picker`'s row name is named by
  its own focusable check control. `abwab-toolbar`'s `[title]` sits on an `aria-hidden` count inside
  a focusable tab and discloses nothing that is hidden.
- **The move picker and `abwab-door-picker` look alike and are still separate — on purpose.**
  Since ux-slice-m the move picker has a search, a chevron, truncated names, and a
  collapsed-by-default tree, which is most of what the door picker offers, and the
  question "why not share one component?" is a fair one to keep asking. What differs
  is the part a shared component would have to fork anyway: the door picker's row is a
  **checkbox/radio pick from a set**, the move picker's is a **single destination**
  with a pinned non-door option (the main-door option) above it and a whole-subtree cycle
  exclusion inside it; the move picker also has no loading/error status to render,
  because its tree arrives with the page snapshot rather than from a fetch of its own.
  Merging them means a component with two selection models, an optional pinned row, an
  optional status tier, and a `variant` input to switch between them — the shape that
  reads as reuse and behaves as two components in a trench coat. Revisit this if a
  **third** consumer appears: two similar lists are a coincidence, three are a pattern.
- **Bulk is all-or-nothing.** One stale token fails the whole bulk operation with a
  single `409`. The backend's bulk-conflict response carries no per-door
  identification (verified against `AbwabDoorsController.cs` /
  `ApiMessages.AbwabDoorStaleVersion`), so the locked conflict message names every
  door in the *attempted* selection, and that selection is preserved rather than
  cleared on conflict — a single-door conflict, by contrast, clears just that door's
  now-invalidated selection.
- **The section-delete conflict copy is the backend's, and the client holds no constant for
  it.** Deleting a section that still holds live doors answers `409` with
  «لا يمكن حذف القسم لاحتوائه على أبواب حالية» (`ApiMessages.AbwabSectionHasLiveDoors`,
  `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs:117`), and the write controller's 409
  policy renders the backend message whenever the response carries one —
  `backendMessage ?? ABWAB_LABELS.writeConflictFallback` (`state/abwab-write.controller.ts:43`).
  So this string is never authored client-side and **no constant for it exists in
  `abwab.labels.ts`**, deliberately: a client copy could only ever be reached if the backend
  omitted its own message, which is dead code dressed as a safeguard, and the generic
  `writeConflictFallback` already covers that case. The frontend keeps a verbatim copy of the
  shipped backend string; this paragraph's sync rule requires re-verifying the pair whenever either
  file changes.
- **`AbwabDoorDto` carries no audit-seed columns on the wire** (no `createdAt`/
  `createdBy`/`approvedAt`/`approvedBy` — verified against the generated model and
  `openapi/swagger.json`). No surface may render an authored-by, approved-by, or
  created-at value for a door: there is nothing behind it to fabricate one from.
- **Overlay state is page-scoped; caches are app-scoped.** `AbwabPageOverlaysController` is
  provided by `AbwabPageComponent`, not `providedIn: 'root'` — the same split
  `features/words/state/*-detail.controller.ts` makes ("Not `providedIn: 'root'`: … each
  overlay adapter provides its own component-scoped instance"). Root scope would outlive
  `/abwab`, and the page renders every dialog **outside** its loading/error guard, so a
  left-open modal would paint again on re-entry before any data loads. The snapshot facade
  and the selection store stay root-scoped on purpose; only the overlay state is per-page.
  **Audit item 11 refined this invariant; it did not break it.** The overlay *objects* are
  still page-scoped and still die with the page — what now survives in the URL is the fact
  that an overlay was closed and can be asked back. That is precisely the shape that makes
  re-entry safe: the danger was never "the URL remembered something", it was a modal
  painting itself over an empty page before any data loads. A retained `<kind>-closed`
  paints nothing on arrival; it renders one control, and reopening waits for the same
  settle point the `door=` deep link waits for. The restore is explicit, and the guard
  refuses it outright when the subject is archived or gone.
- **Loading/empty/error surfaces are composed, not hand-rolled, and since Phase 8 they reach the
  five F12 owners directly.** Every text-only loading, empty, and error site across `abwab-page`,
  `abwab-templates-page`, the door fields form, both pickers, the sections, restore, relations and
  template copy modals composes `qd-skeleton-rows`/`qd-panel-skeleton` (loading), `qd-empty-state`
  (empty) or `qd-error-state` (error) — `UI_STYLE_SYSTEM.md` §17. **The `qd-state` compatibility
  adapter has zero Abwab consumers**, and `npm run check:golden-ui` enforces the zero-consumer
  baseline. `severity` is not decoration: a failed *read* the user
  can retry (`abwab-page`'s snapshot load, the templates list, the selected template's own load,
  the copy modal's doors load, the relations read) is `severity="read"` and stays on the polite
  path, and a failed *write* is the `role="alert"` `severity="write"` that never clears the draft.
  The relations modal is the one surface that **binds** `severity` instead of fixing it, because a
  single `errorMessage` signal carries both its read failure (`status() === 'error'`, retryable)
  and the add-relation write failure (`status()` stays `ready`). `addDoorRelations` deliberately
  does not set `announceFailure`, so that write's only live region is this element — pinning it to
  `read` would silence the failure, and pinning it to `write` would make an ordinary retryable
  read shout. The binding is the same expression that decides `actionLabel`. Each migrated site kept its host `data-testid` **and** passes the adapter's
  old inner ids (`qd-state-error` / `qd-state-empty` / `qd-state-action`) through `testId` /
  `actionTestId`, so no external assertion moved with the owner. **The relations modal's own read is one of
  them**: it holds a `'loading' | 'ready' | 'error'` status, renders `qd-skeleton-rows` while a
  fetch is out, and reaches the empty state or the count chip only once the list has actually
  answered. Which read runs at all is decided by the anchor's snapshot `relationCount` — a
  zero-count door issues **no request**, and the count is read untracked so a post-write snapshot
  refresh cannot reset the open draft. The fetched list overrules a disagreeing count; the count
  only chooses between asking and not asking. **The single `actionLabel` retry §17 permits is
  carried by the feature's transport reads and by nothing else** — the doors page's own
  snapshot-load failure and the copy modal's doors-load failure (both wired to
  `AbwabSnapshotFacade.load()`), the relations modal's own load failure (`retryLoad()`), and
  the templates list's load failure (`AbwabTemplatesFacade.loadList()`); `grep -rn actionLabel`
  under this feature is the inventory, and each hit must be a read the user has no other
  recovery from. The relations modal's **write** errors deliberately carry no retry: the
  add and remove controls are themselves that retry, and §17 allows one action per error. A
  **relation delete confirms first** (`qd-confirm-dialog`, `tone: 'danger'`, nested above the open
  modal): the body names **both** doors and the relation's display group, and states that the
  delete removes the relation from both ends — telling the user it is removed from both sides is
  empty wording without the two
  names. That dialog owns its own write error, so a failed delete lands beside the decision that
  caused it instead of on the modal's shared line, and it stays open, busy, until the write
  resolves — which is also what closes the double-dispatch hole the bare chip had. **Every write
  dispatched from a modal now closes that hole the same way, with a busy signal per write rather
  than one per modal**: the door modal's submit (`saveBusy`, so a double-clicked create makes one
  door, not two), the sections modal's add (`addBusy`) and its delete confirm (`deleteBusy`), the
  copy modal's apply (`applyBusy`, so a second click on the confirm cannot duplicate the
  template's children under every selected door — and while it is in flight the modal also
  refuses Escape/backdrop/cancel dismissal, `abwab-template-copy-modal.component.ts`'s `close()`
  guard), the template-node modal's submit (`saveBusy`,
  `abwab-template-node-modal.component.ts` — the door modal's twin, closed late), and the
  relation-delete confirm above. Each
  one reads its signal first and returns, then sets it, then clears it in the subscribe
  callback. Any
  successful load clears the message, so a recovered failure no longer sticks for the life of the
  open modal. The templates page's
  «اختر قالبًا» now means only what it says: `AbwabTemplatesFacade.selectedLoading` covers the
  per-template fetch window (null `selectedTemplate` throughout, since `select()` writes the id
  first), and the detail region renders `qd-skeleton-rows` there instead.
- **A skeleton's `rowTemplate` sizes its columns, never its rows.** `qd-skeleton-rows` defaults
  to a 0.75rem bar, so a call-site whose loaded row is taller must say so with a
  `--qd-skeleton-h` override on the host — the doors tree does (1.5rem, giving 32px pitch against
  the gapless tree's measured 32px row), the templates list does (3.75rem), and the relations
  modal's group skeleton does (1.25rem, standing in for a heading over a chip line). The primitive's
  own inter-row `gap` is **not** parameterized, so *n* skeleton rows always land one gap short of
  *n* gapless loaded rows; that residual is the primitive's, not the call-site's, and closing it
  means changing `shared/ui/skeleton/`.
- **The stats bar (item 17, Slice B2, T1001-T1004) is two `qd-result-count` instances above the
  toolbar, both derived from the existing snapshot — no backend call added.** «كل الأبواب» is
  **total live doors**, counted frontend-side (`countLiveAbwabDoors`, `abwab-tree.builder.ts`);
  the second is **doors in the currently open tab**, reading the backend-computed
  `AbwabTreeSectionDto.doorsInScopeCount` for a specific section tab, or falling back to the same
  live-only total on «كل الأبواب» itself (`countAbwabDoorsInOpenScope`). **The two numbers are
  live-only by definition — the same choice every other count in this feature makes.** They now
  DO reconcile, but only because **no live door can sit in a retired section** — the reader
  emits `doorsInScopeCount` for live sections only, so Σ over the listed sections equals the
  live total exactly while that holds, and two write guards are what hold it: a section delete
  is refused while live doors remain (`ApiMessages.AbwabSectionHasLiveDoors`) and restoring a
  root whose section was retired demands a live destination
  (`RestoreDoorOutcome.SectionRequired`). It is not structural — loosen either guard and the
  sum breaks. The stance is unchanged for a different reason again: summing sections
  to get the total would recompute client-side what the backend already answered, and fork from
  its definition of "in scope at any depth" the moment the two drift. Do not "fix" this by
  summing sections instead of counting live doors, and do not add a test asserting the two sum —
  it would be redundant, not impossible. Both stats stay mounted through loading/error/loaded and
  through every tab switch (never conditionally unmounted), matching every other §17 composition
  in this feature — an unmounting stat would move the toolbar under it exactly the way the old
  per-branch loaders used to (§4.6-adjacent). Neither label goes through `countPhrase`: the shared
  component renders a "label: N" data-display line (the four words explorers' own precedent), not
  a counted-noun sentence, so the bare-count rule below does not reach it.
  **Item 19's tab badge (below) answers a different question and must never be asserted to agree
  with either stat** — «12» beside the toolbar and «3» on a tab are both correct at once, because
  one counts all depths and the other counts root doors only; no test or doc may treat them as the
  same number reused twice.
- **Counted door labels go through the Arabic number forms.** `archiveConfirm` and
  `movePickerTitleBulk` share one helper covering singular («باب واحد»), dual («بابين»),
  3–10 («N أبواب») and 11+ («N بابًا»). Do not interpolate a bare count into new copy —
  «سيتم أرشفة 1 بابًا» is wrong Arabic and this product is Arabic-first.
- **Labels use the TDZ getter pattern**, same as `features/words/README.md`: read
  `abwab.labels.ts` consts via component **getters**, never `readonly` field
  initialisers, which can observe the label module inside its temporal dead zone.
- **No misleading write controls.** Nothing for protection or the «الأبواب
  الرئيسية» tab appears anywhere in this feature. Relations became real controls with
  `abwab-relations`, and **templates became real with `abwab-templates`**: «القوالب» in the
  doors header routes to a publicly readable workshop backed by nine endpoints. Write controls
  appear only with their exact capability; row and section order values become read-only data
  otherwise. The sole disabled write-shaped control is archive restore without its permission,
  and it carries an accessible Arabic explanation rather than implying that it can succeed.
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
- **Applying copies the template root's direct children, never the root itself** (the ux-slice-g
  reversal; `Backend/infrastructure/.../Persistence/Writes/Abwab/README.md` holds the axiom, and
  `EfAbwabTemplateApplyWriter` implements it — the file carries no comment, and under the
  workspace comment ban it may not). Each target gains N new
  children, one per the root's direct children, each with its own subtree; the copy modal's
  description and preview (`abwab-template-copy-modal.component.ts`, `abwab.labels.ts`) state
  this before the write. An empty-root template (no live children — the default state of every
  newly created template) is refused server-side with a `400`; the modal's `hasElements`
  affordance only makes that legible before the confirm click, it is not the guarantee.
- **A template copy is detached at birth.** No `templateId` column, no provenance, no
  back-link, nothing marking a door as template-derived. Editing or deleting the template
  later never touches earlier copies, and the copy modal's preview says so *before* the write
  because it is the expectation this feature is most likely to invite wrongly. Do not add a
  badge, a count, or an "update all copies" path.
- **The copy modal's confirm count is the number of targets, always — never a union.**
  Selecting a door and its own descendant produces two independent copies. This is the
  deliberate opposite of bulk-archive's union count above, where archiving an ancestor already
  claims its descendants; applying a template claims nothing. Do not "fix" one into the other.
- **There is one door picker, `abwab-door-picker`, and both modals compose it.** The duplicated
  picker became a component. Selection stays **consumer-owned** — the picker renders
  what `pickedIds` says and emits `toggled`, so the relations modal keeps its single-anchor rule
  in bulk mode and the copy modal its multi-select, and the picker knows about neither. Existing
  `data-testid`s survive through `testIdPrefix`, preserving each consumer's identifiers. Do not
  re-fork it for a third caller;
  add an input. **Consumer-owned selection is not consumer-owned *affordance*:** the picker
  still has to render a control that tells the truth about how many doors are choosable, which
  is what `single` is for. Anchor-pick selection is select-only for the same reason — a radio
  group offers no click-the-selected-one-to-clear gesture, so the component does not invent one.
- **The template tree renders a list, not `role="tree"`.** `AbwabTreeComponent` earns that
  role with a full RTL-mirrored keyboard model (`abwab-tree-keyboard.controller.ts`); claiming
  the role without the arrow-key model would promise a navigation contract the workshop does
  not implement. `aria-level` still conveys depth and every keyboard affordance has a real focus target.
  Reusing `AbwabTreeComponent` itself was rejected up front, not discovered mid-work: it is
  typed on `AbwabNode` and carries selection/bulk/roving-tabindex/URL concerns this page has
  none of. **ux-slice-g adds a third row-menu path —
  `ContextMenu`/`Shift+F10`, alongside `⋯` and right-click** — without moving this line: the row
  `<div>` itself catches the key. A row with an authorized root or node context menu is its own
  `tabindex="0"` target, so a leaf whose only capability is edit or delete still reaches the
  row-anchored keyboard menu; read-only rows do not become tab stops. `⋯` remains mouse-only at
  `tabindex="-1"`, and no arrow-key navigation is added. Adding a menu key is not the same
  contract as arrow-key navigation between rows, so the role stays unclaimed.
- **The M10/M33 `sectionId` defense-in-depth stays in the door modal's shell**, not in the
  extracted `abwab-door-fields-form`. The form has no concept of a section and must not
  acquire one; the shell is the layer that decides *whether* a section applies. The shell now
  also owns a real section `<select>`, shown in exactly one case — a root create from
  «كل الأبواب», where there is no parent to derive from and no active tab to read. The backend
  refuses that write without a section, so the selector turns a 400 into a choice. It stays in
  the shell for the same reason the null-ing does; the form is still untouched. **With no live
  section to choose the selector is replaced by a hint** (`doorModalNoSectionsHint`, the same copy
  the restore modal uses) and the create stays blocked — an empty control answered by an error
  after submit is a dead end, and both surfaces owe the same answer to the same state.
- **A `204 No Content` arrives as a `null` envelope, not `{isSuccess, data}`.** Single-door
  archive (`DELETE api/abwab/doors/{id}`), a successful section delete
  (`DELETE api/abwab/sections/{id}`), a relation delete
  (`DELETE api/abwab/relations/{id}`), and **both templates deletes**
  (`DELETE api/abwab/templates/{id}`, `DELETE api/abwab/template-nodes/{id}`) are the routes
  that answer 204, and Angular's
  `HttpClient` parses an empty body as `null`. `abwab-write.controller.ts#handleSuccess`
  therefore treats a null response as a payload-less success: only a success is ever a 204,
  since every failure arrives as a 4xx through `catchError`. Dereferencing `response.isSuccess`
  first throws, gets swallowed as a transport error, and leaves the UI reporting failure while the
  backend write has committed. Every delete route shares this handling requirement.

## Browser E2E

`e2e/abwab-permissions.e2e.ts` is the one retained Abwab journey. It verifies that public Abwab and
template navigation remain available, write controls stay hidden, a URL-restored create overlay is
not exposed, and an anonymous write receives the Backend's `401` envelope. It creates no sandbox and
leaves no domain residue. The `abwab` Playwright project remains single-worker and is documented in
`e2e/README.md`.

## Decisions that reversed mid-series

Four decisions in this feature were made, shipped, and then reversed by the user during the UX
slice series. Each line below states what holds **now** and names the code that implements it.
They are recorded because the reversal is the part a reader cannot recover: the code shows the
current answer but not that an earlier, opposite one was deliberately abandoned — and re-deriving
the earlier answer from first principles is exactly the mistake to avoid. **Anchors here are
symbol and selector names, not line numbers**, deliberately: this is the section most likely to
outlive its line numbers, and every previous set of numbers in it had drifted.

- **Inline reorder: Enter is the only commit; blur and Escape both abandon the edit.** The first
  implementation committed on blur. `abwab-tree.component.html`'s order input binds
  `(blur)="cancelOrderEdit(node.id)"`, and `AbwabTreeComponent.cancelOrderEdit` guards on
  the same id as the commit, so the blur that follows an Enter commit is a no-op. This aligns the
  reorder editor with the workshop's inline authoring rows, which were already Enter-only.
- **The علاقات flag is always rendered, dimmed at zero, and clickable.** It was previously
  render-only-when-`> 0` and deliberately a non-control. It is now a real `<button>` —
  `.abwab-tree__flag` in `abwab-tree.component.html` — carrying `[attr.tabindex]="-1"` so the
  row's roving tabindex survives, an `.abwab-tree__flag--empty` modifier at zero
  (`abwab-tree.component.scss`), a visible relation count, and an
  Arabic `aria-label`. The archive view and cards are **not** part of this reversal: an archived
  door's visible relation count is always 0 there.
- **Apply copies the template root's direct children, never the root itself.** The original axiom
  was "the template root becomes a new child of each target". `EfAbwabTemplateApplyWriter`
  implements the rule that replaced it — `ApplyAsync` finds the single parentless node, refuses
  a template whose root has no live children (`AbwabTemplateEmptyException`), and creates one
  door per `rootChildren[i]` per target — and `Persistence/Writes/Abwab/README.md` holds the
  axiom. The
  consequence worth keeping in mind: the collision surface is N names per target, not one, so the
  `409` names every colliding `(target, child)` pair.
- **الأبواب is a hover dropdown — الرئيسية / قوالب الأبواب / الأرشيف.** `abwab.routes.ts`
  previously recorded the opposite: the workshop is reached from the doors page header, "not the
  sidebar", and adding a nav entry "would put an item in the nav nobody asked for". It is now
  data-driven through `ABWAB_MENU_ITEMS` in `core/navigation/nav-menu.ts`, and the navbar renders
  every dropdown from that data with no hand-rolled branch left. The middle item's label is
  «قوالب الأبواب» — **not** «القوالب», which is `templatesButton`, the doors-page header button.

## Related

- This README owns current Abwab page behavior; no historical plan is required for ordinary work.
- Structure and split thresholds: [`FRONTEND_STRUCTURE.md`](../../../../.architecture/FRONTEND_STRUCTURE.md).
- Shared visual primitives: [`UI_STYLE_SYSTEM.md`](../../../../.architecture/UI_STYLE_SYSTEM.md).
- API integration rules: [`API_INTEGRATION_GUIDELINES.md`](../../../../.architecture/API_INTEGRATION_GUIDELINES.md).
- Shared UI ownership: [`shared/README.md`](../../shared/README.md).
- Server-side snapshot/relation-cache companion:
  [`Persistence/Reads/Abwab/README.md`](../../../../../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/README.md).
- Planning-artifact lifecycle: [`docs/README.md`](../../../../../../docs/README.md).
