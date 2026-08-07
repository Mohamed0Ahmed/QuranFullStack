# Abwab feature (الأبواب) — doors & sections management

**HOW rules:** `.architecture/UI_STYLE_SYSTEM.md`, `.architecture/FRONTEND_STRUCTURE.md`,
`.architecture/API_INTEGRATION_GUIDELINES.md` (project root). This file is the WHAT.

**Status: shipped.** The full doors page (tree, cards, bulk mode, move, reorder, search, archive
view, sections management, row context menu), the superset's global order, door relations, the
templates workshop at `/abwab/templates`, and the browser e2e flows are all in. The UX slice
series that followed rewrote search, reveal, the move picker, the confirms and the row menu;
this file is the current record of all of it, so read it rather than reconstructing the order
the pieces arrived in.

**Read access is public; write access is protected by the Backend.** The four Abwab reads and
their routes remain available without authentication. The twenty-one write endpoints require the
exact typed permission defined by the Backend. The frontend reads the Phase 7 access store only to
shape UX: each write affordance receives a capability boolean, page/controller handlers recheck it,
and the write controllers make the final frontend check before issuing a request. None of those
checks authorizes a request; a handcrafted write still receives the Backend's own denial.

When a write receives `401`, the shared coordinator starts the login flow once and never retries
the mutation. When it receives `403`, it refreshes access, then the page closes or disables stale
write state without retrying. Anonymous and read-only visitors keep the tree, cards, archive,
relations reading, template list/detail, and template-tree rendering. Archive restore remains a
disabled, explained control for visitors without restore permission so the hierarchy stays legible.

## What this feature does

Renders the `GET api/abwab/tree` snapshot as a tree and as drill-down cards at `/abwab`,
reads a door's relations from `GET api/abwab/doors/{doorId}/relations`, authors reusable door
subtrees at `/abwab/templates`, and drives the **twenty-one** write endpoints — create, edit,
move, reorder, bulk move, bulk archive, archive, restore, the four section commands (create,
rename, reorder, delete),
relations add/delete, template create/delete, template-node add/edit/reorder/delete, and the
apply — with optimistic-concurrency conflicts (`409`) always surfaced, never swallowed or
auto-retried. **Twenty-five** endpoints in all across the two data-access files (sixteen +
nine), four of them reads.

## Render chain & key pieces

- `pages/abwab-page/` — the route shell: parses all seven URL keys into state, composes
  every child below, and delegates overlay/dialog state to
  `state/abwab-page-overlays.controller.ts`, URL-modal reconciliation to
  `state/abwab-modal-url.controller.ts`, reveal state to
  `state/abwab-reveal.controller.ts`, and user-event/URL-write orchestration to
  `state/abwab-page-interactions.controller.ts`. **Its TS sits at 375 lines, below the
  400-line hard threshold but above the 300-line soft threshold.** Four cohesive extractions
  now leave only route synchronization, derived render state, label getters required by the TDZ
  rule, host-only focus handoff, and the thin wrappers that supply that host focus. The reveal
  controller owns its target/sequence/mark signals, hold timer, URL handoff, and scroll; the page
  continues to feed settled URL state into it so the existing cross-section and cards-view
  behavior stays intact.
- `state/abwab-modal-url.controller.ts` — the page-side machinery for the
  seventh key: which overlay the URL currently owns (kind **and** subject), whether a
  parsed key may be acted on (the live-door guard), and the two halves of reconciliation.
  Same boundary as the overlays controller — **no `Router`/`ActivatedRoute`**; the page
  feeds URL values in while the interactions controller writes keys. Page-provided, not root.
- `state/abwab-page-interactions.controller.ts` — the page-provided event orchestrator for
  toolbar, tree, side-panel, context-menu, modal, archive, restore, and URL actions. It keeps
  the existing permission rechecks, controller dispatches, URL merge/replace semantics, and
  focus callbacks intact while keeping those event paths out of the route shell. **Its TS sits
  at 373 lines, below the 400-line state-service soft threshold.**
- `components/abwab-modal-restore/` — the retained overlay's restore control: the label
  naming the overlay and the hairline-joined discard X. Presentational, page-driven; it
  reads no URL and owns no state beyond its own focus entry point.
- `state/abwab-page-overlays.controller.ts` — owns open/closed state and the dispatch
  glue for the door modal, single/bulk archive confirm, the move picker, the sections
  modal, the relations modal (open/closed + anchor + mode only), and the row context
  menu. Split out of the page component once composing six
  overlays pushed that file toward the component-TS soft threshold
  (`FRONTEND_STRUCTURE.md`'s Large Page Split guidance) — it holds state/orchestration
  only, no template of its own. **Provided by `AbwabPageComponent`, not
  `providedIn: 'root'`** — see the Gotchas below.
  **It now sits at 479 lines, just over the 400-line soft threshold for state services**,
  and is kept there deliberately: it is one flat family of overlay signals with their
  open/close handlers and no branching logic, so a split would divide by overlay count
  rather than by responsibility.
  **The trigger that forces the split** is the same one the templates workshop carries —
  crossing the 600-line hard threshold, or an overlay arriving that owns URL state of its
  own, at which point the URL-backed overlays move to their own controller beside
  `abwab-modal-url.controller.ts`.
- `components/abwab-toolbar/` — «كل الأبواب» + one tab per section (composing
  `qd-tabs`/`qdTab`, **no** «الأبواب الرئيسية» tab), the name+alias
  search box, and the tree/cards view toggle. `hideSectionControls` hides the tabs and
  the view toggle while the archive view is active — they have no live section
  grouping to act on there — leaving only search, which still filters the archive tree
  (so the archive view never grows a root-count badge either — there is no tab strip
  there to carry one).
  - **One search box, two behaviours, deliberately (ux-slice-l).** In the **tree** a query
    *marks* matching rows in place (a 1px inset accent ring) and hides nothing; every door
    stays where the user last saw it, and a zero-match query leaves the full tree with a zero
    count rather than collapsing into «لا توجد أبواب بعد.», which was a lie about the data. In
    **cards** and the **archive** the same query still *filters* — those are flat browsing
    surfaces where a filter costs no structure — but they filter differently: the **archive**
    prunes its tree to the visible ids (`pruneAbwabNodesToVisible`), while **cards** receive
    the unpruned roots plus `visibleIds`/`isFiltering` and filter the level currently on
    screen, at **every** depth, so a drilled level obeys the query too and each card's
    drillability and child count still come from the real node rather than a pruned copy. The
    door picker's own search also stays a filter. The split is per view, not per query, and
    lives here rather than in the placeholder, which would otherwise have to be
    view-dependent.
  - **No surface lies about which nothing it is showing.** The tree's answer above is the
    same rule the other two now apply: cards and the archive each choose between «nothing
    here» and «nothing matched» from `AbwabSearchResult.isFiltering` **and** whether the
    unfiltered level held anything at all — a filter over an already-empty level is still
    «nothing here». It is never `q !== ''`: `searchAbwabNodes` trims, so a whitespace-only
    query is not filtering and the surface still reads its plain empty message
    («لا توجد أبواب مؤرشفة.» in the archive) instead of claiming a filter ran.
  - **The match count sits beside the input**, not in the stats row: it answers the query, so
    it belongs to the control that asked. It is two elements — a live `aria-hidden` span that
    updates per keystroke, and an always-mounted visually-hidden `role="status"` region that
    speaks the settled count **once, 500 ms after typing stops**. A status region bound
    straight to the count would announce once per typed character. Clearing the query empties
    the region immediately and announces nothing. Deliberately **not** routed through
    `qd-abwab-announcer`, whose channel is one-shot reveal/write messages.
  - **Matched ancestors are seeded open, not forced**, so a branch search opened is
    collapsible at once and survives clearing the query. The consequence — seeds accumulate,
    so broadening then narrowing leaves the earlier branches open — is accepted and intended;
    expansion is the user's state once seeded, and rewinding it per keystroke would fight
    them.
  - **Item 19's root-count badge** renders `.qd-tabs__count` at the call-site on every
    tab, composing `qd-tabs`'s backing class rather than adding a directive input —
    `qdTab` stays a host-bindings-only directive and cannot project a child span. Each
    badge also carries a visible `title` with the same root-scope phrase its
    `aria-label` speaks (`abwab-toolbar.component.html`), so the scope is readable by
    eye, not only by convention.
    **Root doors only** (`state/abwab-tree.builder.ts`'s `rootCountBySectionId`), a
    different question from item 17's shipped `doorsInScopeCount` stat below, which
    counts at any depth. Visible digits are Latin and `aria-hidden`; the tab's own
    `aria-label` carries the counted-noun phrase (`ROOT_DOOR_FORMS`
    in `models/abwab.labels.ts`) so the two numbers are distinguishable in the
    accessible layer, not only by convention.
- `components/abwab-tree/` — presentational tree (`role="tree"`/`treeitem`, full ARIA,
  roving tabindex). **Its TS sits at 356 lines and its host page's template at 312, both
  just over their 300-line soft thresholds** and both kept there deliberately: the tree's
  TS is one row-rendering component plus the flag/bulk/order handlers that must stay with
  the row they act on, and the page template is a composition root whose length is child
  elements and their bindings, not logic. The hard thresholds (400 for both) are the
  trigger; the tree's split, when it comes, is the row into its own component, and the
  page's is the archive branch into a sibling template. **The tree's SCSS sits at 235
  lines, over the 200-line soft threshold** — the relation flag's two-state rendering and
  the bulk/order affordances are row styling that belongs beside the row; the 300-line
  hard threshold is the trigger, and the split follows the TS's (the row's styles leave
  with the row).
  Also here: `abwab-tree-keyboard.controller.ts`, a pure, DOM-free key model
  (RTL-mirrored per the `qd-tabs` precedent: ArrowLeft expands/enters, ArrowRight
  collapses/exits). Renders **flat** (one row per visible node, `aria-level` conveys
  depth) rather than nesting `role="group"` per level. Inline reorder editing (activate
  the order number → input) dispatches through `reorderDoor`, and **Enter is the only
  commit — blur and Escape both cancel.** The order number is a real `<button>` with an
  Arabic `aria-label`, and it is the one row control that joins the row's roving tabindex
  (`rovingId() === node.id ? 0 : -1`) instead of being pinned to `-1` like the flag and the
  two hover actions — that is the only keyboard path to reorder in this feature, so do not
  "align" it to `-1`. Blur used to commit; that made clicking away
  from a half-typed number resequence a scope the user never confirmed, and it is the one
  grammar in this feature where an unconfirmed value could be written. Enter-only matches
  the workshop's two inline authoring rows, which already commit on Enter with no submit
  button (see below). Saying "Enter commits, Escape reverts" and staying silent on blur is
  what let the two drift apart, so all three are named here. Rows carry the design contract's
  two hover actions: `＋` (add child) and `⋯` (open the row menu), revealed on hover and on the
  selected row, hidden in bulk mode, and kept out of the tab order so the roving-tabindex
  invariant holds. `⋯`, right-click, and the keyboard `ContextMenu`/`Shift+F10` path all
  emit `menuRequested` **with an anchor point** — the pointer position for the mouse paths,
  the focused row's rect for the keyboard one — and the page shell composes the shared
  `qd-context-menu` (`../../shared/ui/context-menu/`) there, projecting its own operation
  buttons in (Slice A, phase 6 — both `abwab-page` and `abwab-templates-page` compose it now,
  each keeping only its own page-specific items and, for the templates workshop, the
  root-vs-node item swap).
  **Every** row carries the «علاقات» flag, and it is a control: accent-tinted once the door has
  relations, dimmed to a muted hairline at zero, and clicking it emits `relationsRequested`,
  which the page turns into select-the-door-then-open-the-relations-modal. It renders at zero
  too because a flag that only appears when there is something to see cannot answer "does this
  door have relations?" — the absent state was indistinguishable from a door the user had not
  looked at. **Two non-colour differentiators carry that answer**, because the tint alone did
  not: the flag shows the door's relation count beside its label, and the zero state's border
  is **dashed** as well as muted (`.abwab-tree__flag--empty` in `abwab-tree.component.scss`).
  Like the two row actions it is `tabindex="-1"`, so the roving-tabindex invariant
  holds, and in bulk mode it toggles the row's own bulk selection rather than opening
  relations, where a row click means "toggle this door". The archive
  view and the cards still render no flag, since an archived door's visible relation count is
  always 0 (see the derivation in the Gotchas).
  A branch row carries **three** count badges: direct live children (the design contract's
  own), total live descendants at any depth, and the deepest
  live nesting below the door. The last two are a commissioned extension of the contract, not
  a missed line. All three are live-only and both derivations are memoized on the node by
  `abwab-tree.builder.ts`.
  Since slice J an **`aria-hidden` header row names the three columns** — «مباشر» / «الكل» /
  «عمق» — sitting outside the `role="tree"` element and sharing the rows' grid tracks through
  a frame → tree → row subgrid chain (`UI_STYLE_SYSTEM.md` §17, "Header over badge columns").
  The header is presentational: **each badge's Arabic `aria-label` remains the accessible
  layer**, and those labels are deliberately fuller than the visible ones, which are
  abbreviated to keep the fixed badge columns from eating the name. The depth badge is a bare
  numeral now that a column names it — the «ع» prefix it used to carry existed only because
  nothing else distinguished it from a fourth count.
  **Row width priority, widest to narrowest: name > order pill > actions > children count >
  descendants/depth badges > flag.** The name is the only shrinkable item (`.qd-truncate`)
  and its measured budget is a rule in §17's truncation entry, not a remembered figure —
  re-measure it whenever the row's leading or trailing furniture changes. Below
  `$qd-bp-tablet-max` the descendants and depth badges drop **with their header labels, in the
  same media query as the grid tracks they occupy**, rather than everything being squeezed —
  the contract's own children count, and «مباشر» above it, survive at every width.
- `components/abwab-cards/` — the drill-down grid: `cardId` names only the
  drilled-into parent (not a full path array) — the breadcrumb chain is derived by
  walking `parentId` up from it via `byId`, so the URL never needs an array. Fails
  closed to the root level for an archived or unknown `cardId` (M25/M31).
  **The empty / no-results state lives inside this component, below the breadcrumb** — not in
  a page-level guard around it, which would unmount the breadcrumb and leave a drilled user
  with a zero-match query no in-page way back out. Which of the two messages it shows follows
  the `isFiltering` rule above, read against the level currently on screen.
  **A card is a real `<button>` and its accessible name is the door's name**; the bulk
  checkbox is a **sibling** of that button inside `.abwab-cards__cell`, never nested inside it
  — a checkbox inside a button is not a control, and the wrapper cell carries no click handler
  of its own, so the two never fight over one activation. **Has no row
  context menu** — unlike the doors tree and, since ux-slice-g, the templates workshop tree.
  Recorded as an open decision for a later slice, not an oversight to close here: a third menu
  consumer landing in the same slice as the second would land in a slice whose test posture
  cannot cover either.
- `components/abwab-archive-view/` — the archived hierarchy, restore-only.
  A-live vs A-arch is read straight off the builder's tree partition
  (`node.depth === 0` ⇒ restorable, `depth > 0` ⇒ parent is archived ⇒ restore disabled
  with «استرجع الأب أولًا») — never re-derived by walking `byId`. No child-count badge:
  every archived door's live-child count is always 0, so the badge would be meaningless.
- `components/abwab-side-panel/` — active door + single-door operations (add child,
  edit, move, relations, archive) plus bulk mode: the toggle, its `.on` state (tint +
  accent-text + hairline, **not** a solid fill — the first allowed-green fix), and the bulk bar
  (count, names, bulk move/relations/archive/clear). Each write action is rendered only for its
  exact capability; relations reading remains available without a relation-write capability. No reorder button — the tree's own inline number editor is
  the one reorder affordance; a second control doing the same thing would be redundant.
  This panel is the second of the contract's three add-child paths; the tree row's own
  `＋` and the row menu are the other two.
- `components/abwab-move-picker/` — the one-screen destination picker shared by single
  and bulk move. A **persistent section strip** sits in the modal's `__head`, showing every
  section at once; picking one swaps the destination list below it in place — the make-it-a-root
  option «كباب رئيسي (أعلى الشجرة)» (`asMainDoorOption`, the *main-door option* everywhere below)
  plus that section's doors — with no navigation step. The two-stage flow it replaced
  (ux-slice-m) hid the sections behind a «تغيير القسم» control, so the one thing a mover
  needs to see, which section they are aiming at, was the one thing the modal never showed.
  The main-door option stays in the doors area: it is a destination, not a section. There is
  no «بلا قسم» — every door belongs to a section, so "no section" is not a destination.
  The strip is `qd-tabs` at `layout="grid"` (§17), so it is a real tablist: `role="tab"` cells
  over a `role="tabpanel"` destination list, roving tabindex, RTL-aware Arrow/Home/End, and
  five 150 px columns that wrap — the ~15-section ceiling is three rows, which is why it needs
  no scroller. The active cell is marked by the primitive's tint/accent border **plus** bold,
  never colour alone. Names truncate with `.qd-truncate` + a mandatory `[title]`.
  The strip auto-selects when the selection already answers which section it is (the door's own
  for a single move, the shared one when a bulk selection agrees). **A bulk selection spanning
  sections has no such answer**: the strip opens with no cell marked, the destination list is
  replaced by a prompt, and `confirm` is disabled — with no section there is no
  `targetSectionId` to send, and a button that looks live and does nothing is worse than a
  disabled one. Switching sections drops any destination already picked, since a parent from
  the section just left is not a destination in the one arrived at — and drops expansion with it,
  so the new section opens collapsed like any other.
  **The destination list opens COLLAPSED**: the active section's root doors and nothing else,
  branches opened by hand. Not even the moved door's own parent chain is pre-opened — a move is a
  choice of a new home, and seeding the old one puts the answer the user is moving away from at the
  top of the list. Branch rows carry a chevron mirroring `abwab-door-picker`'s contract (focusable,
  `aria-expanded`, Arabic expand/collapse `aria-label`); leaves keep the element for alignment only,
  with no tab stop and nothing to announce. A branch whose every child is excluded by the cycle
  guard reads as a leaf — a chevron opening onto nothing is a worse answer than no chevron. The row
  is a wrapper holding two sibling buttons (chevron, pick), because a chevron button cannot nest
  inside a row button. Depth is still a flat indent, not nesting: every door at any depth is a valid
  target (see Gotchas).
  **Search** filters the active section's tree and forces every matching path open, so a deep match
  is never filtered in and then hidden by the collapse it was meant to see past. That expansion is
  *derived, never written into the expanded set* — which is what makes clearing the query safe in
  both directions: it neither leaves the tree open behind the user nor collapses branches they
  opened by hand. The query **survives a section change** (unlike expansion): it is a filter over
  whichever section is active, so hopping the strip with a query typed is how a user finds a door
  whose section they have forgotten. A query matching nothing says so and still offers the
  main-door option, which is pinned outside the filtered tree. This mirrors
  `abwab-door-picker`'s search contract
  (relations / template-copy) without importing it — see Gotchas on why the two stay separate.
  If the cycle guard excludes every root a section has, the panel is still not empty: the
  main-door option is pinned above the tree and no exclusion can remove it.
  `excludedIds` is the moved door(s) plus every descendant, the client half of
  the cycle guard; the server's `409 WouldCycle` stays authoritative.
- `components/abwab-door-restore-modal/` — confirms restoring an ARCHIVED DOOR, on
  `qd-confirm-dialog`. Not `abwab-modal-restore`, which reopens a minimized overlay. For a root
  whose section was retired meanwhile (`AbwabNode.sectionRetired`) it demands a live destination:
  sections have no restore route, so the old one cannot be reinstated, and the backend refuses
  the write without one — the archive view's button would otherwise produce an unresolvable 400.
  A child has no question to answer; it returns under its live parent, in that parent's current
  section. Success announces «استُرجع الباب» through the existing aria-live announcer; a failure
  keeps the modal open with the message inline **and is also announced**, because that message is
  inserted inside the already-focused confirm `role="alertdialog"` — see the announcer entry below
  for why that placement decides which region speaks.
- `components/abwab-sections-modal/` — list / add / rename / reorder / delete-empty, with full
  dialog semantics and a dirty guard as of Slice C (a typed section name or an altered
  rename draft raises the door modal's confirm strip; an opened-but-unedited rename is
  not dirty). **Its drafts live on the component, and the page hosts it as a static
  sibling, so it must reset them on open** — unlike the door modal, whose drafts sit in
  a child that `@if (open())` destroys. Skip that reset and «تجاهل التغييرات» hides the
  draft instead of discarding it. **Its TS sits at 305 lines, just over the 300-line soft
  threshold** — the Escape/dirty-strip handling and the per-write busy signals
  landed on one component; the hard threshold (400) is the trigger, and the split is the
  rename-draft machinery into a child form. Takes its four write functions as inputs (bound by the page to
  `state/abwab-sections.controller.ts`) rather than injecting a service, so its own spec
  exercises the 409/success outcomes without the facade/controller chain. Rename always
  reads the section's row from the live `sections` input at submit time, never a value
  captured when edit mode opened.
  - **The order editor reuses the tree's editor grammar**: activate the
    order chip → an `<input type="number" min="1">`, **Enter commits, blur and Escape
    both cancel**, seeded from and submitted against the section's *live* row exactly
    like rename. Its own `editingOrderId` signal is separate from `editingId` — an open
    order edit is **not** unsaved work, so it does not raise the dirty guard. Its trigger
    is a real `<button>`, as the doors tree's order chip now is too — this modal's rows
    already carried real buttons for rename/delete, so it never copied the click-only
    `<span>` the tree used to render, and the tree has since been brought up to the same
    bar. **The Escape guard is mandatory, not cosmetic**: the dialog itself binds
    `(keydown.escape)="requestClose()"`, so the editor's keydown handler opens with
    `event.stopPropagation()` — without it, Escape-to-cancel-an-edit would close the
    whole modal (and write `modal=sections-closed` to the URL, post-Slice-E).
- `components/abwab-door-fields-form/` — the four authoring fields shared by a door and
  a template node (name/description/ayah-text/alias chips, composing the extended
  `qd-chip` with its `removable` affordance — the second allowed-green fix), their dirty
  tracking, and the inline error surface. Presentational: it injects nothing, and its
  `testIdPrefix` input is what keeps the door modal's ids byte-identical through the
  extraction. Its field labels are the door's in **both** shells, deliberately — a
  template node exists to become a door, and the locked requirement is the *same*
  authoring modal, not a parallel vocabulary.
- `components/abwab-door-modal/` — the door's shell around that form: title, context
  line, the dirty guard's confirm strip, and the write dispatch.
  On the shared modal shell like the other five (see "All six modals share one shell"
  below); the guard strip renders in `__foot`, where it cannot scroll away.
- `components/abwab-door-picker/` — the one searchable, expandable door picker, composed
  by the relations and copy modals. Selection is consumer-owned: it renders `pickedIds`
  and emits `toggled`, and `excludedIds` **disables** a door without hiding it or its subtree —
  it renders as a non-selectable row at its true depth with an `excludedTag` chip naming why,
  since a door may relate to its own ancestor. `testIdPrefix` keeps each host's existing
  testids. Rows compose `.qd-check-row`/`.qd-checkbox`/`.qd-truncate`, so it states no
  geometry of its own. Two things the picker owns that consumer-owned selection cannot
  cover: `single` picks the **affordance** (radio, not checkbox — a checkbox promises
  "pick any number" and anchor-pick mode takes one), and an unmatched search renders the
  picker's own «لا يوجد باب مطابق لبحثك.» rather than the host's `emptyMessage`, because "your
  query found nothing" and "there is nothing to pick" are different answers and only one
  of them is true when doors are one keystroke away.
- `components/abwab-template-node-modal/` — the template node's shell around the same
  form. Its submit is a **function input** bound by the workshop page to
  `AbwabTemplatesController` (the `abwab-sections-modal` precedent): the shared form
  dispatches nothing itself, so each shell owns the write its own entity needs.
- `components/abwab-template-tree/` — the workshop's editor tree: the doors tree's
  *language* (chevrons at any depth, an order chip, the root marked `◆` with a bold
  name, hover `＋`/`⋯`, the inline «إضافة عنصر…» row) but not its component. It renders
  a list rather than `role="tree"` — see Gotchas.
- `components/abwab-template-copy-modal/` — «نسخ إلى أبواب…»: the preview block, a
  live-doors-only expandable picker with checkbox multi-select and search auto-expand,
  and one all-or-nothing apply. Takes the doors tree and the apply function as inputs.
- `pages/abwab-templates-page/` — the `/abwab/templates` shell: the template list with
  «+ قالب جديد», the editor panel, the node/template actions, the row context menu, and
  the two confirms. Caches stay root-scoped while overlay state is page-scoped. The
  template-delete confirm flow (its confirming/busy/error signals and the F-92 guard
  semantics) remains in the component-provided `abwab-templates-page-delete.controller.ts`.
  The node modal, context menu, node-delete confirm, copy-modal state, and their guarded
  handlers live in the page-provided `state/abwab-templates-overlays.controller.ts`.
  That extraction brings the shell to 292 lines, below the 300-line soft threshold, without
  giving this route URL state or changing its selected-template/cache boundary. The page's
  own spec (`abwab-templates-page.component.spec.ts`) pins the resulting behavior.
- `components/abwab-relations-modal/` — the door's relations: four display groups
  (تشابه · تضاد · «أبواب أكثر شمولية» · «أبواب أقل شمولية», empty ones dropped), the type
  segment, the direction pill with its live preview, and an expandable/searchable door
  picker that adds N targets in **one** call. **Its TS sits at 396 lines, over the
  300-line soft threshold** — deliberately: one dialog owns display, authoring and the
  nested delete confirm, and splitting the confirm out would separate the trap-yield
  logic from the trap it yields. The hard threshold (400) is the trigger; the split,
  when it comes, is the add-form (type segment + direction + picker wiring) into a child
  component. Takes its read and its two writes as
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
  **"Already linked" is computed per `(pair, type)` with no direction term**
  (`abwab-relations-modal.component.ts`, `linkedIds`), so flipping the type segment re-computes
  which rows are blocked and flipping the direction pill does not. It is deliberately **empty in
  anchor-pick mode**: there the flag would have to mean "all N selected targets already relate to
  this candidate anchor", a condition the user cannot see on screen and would read as a bug.
  **Each related door's name is a control** (Slice D): it composes `qd-chip`'s
  `labelClickable` opt-in, so one chip carries two independent controls — reveal that door in
  the tree, or remove the relation — and emits `revealRequested` with the *other* door's id.
  The modal knows nothing about the tree, the URL, or scope; the page owns all of that.
- `components/abwab-announcer/` — one `aria-live="polite"` `role="status"` region for
  operation messages; a feature-scoped stand-in for a toast primitive this one
  feature does not warrant.
  **A write failure reaches exactly one live region, never two.** It is announced here only when
  the operation has no reliably-announcing error surface of its own; otherwise the surface owns it
  and the announcer stays silent, because `qd-state variant="error"` already carries
  `role="alert"`. Two surface shapes reliably announce on their own. A **reserved region** —
  `[reserve]="true"` rendered **unconditionally**, so the empty alert element exists before the
  message lands and text insertion announces; the template-copy modal has this shape
  (`abwab-template-copy-modal.component.html`), kept visually quiet while empty by
  `.qd-state--reserve-empty`. An **alert inserted into a plain `role="dialog"`** — a
  `role="alert"` created at failure time inside an open non-alert dialog announces on insertion;
  the door form, the sections modal's create/rename strip and the relations modal have this shape
  (`[reserve]` under their `@if` reserves nothing — the announcing comes from the insertion).
  Both shapes DROP the announcer. What does **not** reliably announce is an alert created inside
  an already-focused `role="alertdialog"` — so the archive and bulk-archive confirms, the restore
  modal, section delete's confirm and relation delete's confirm all KEEP it. Writes with no
  surface at all — move, bulk move, and the tree's inline reorder — obviously keep it; the
  announcer is their only channel.
  The switch is `announceFailure`, set per operation in `state/abwab-write.controller.ts` and
  `state/abwab-templates.controller.ts` (template apply DROPs into the copy modal's reserved
  region; every other templates-side failure KEEPs) so the decision is readable in one place
  rather than re-derived per call site. **If you give a KEEP surface one of the two announcing
  shapes, or take either shape away from a DROP surface, flip its flag in the same change** —
  otherwise a failure is announced twice, or not at all.
  **Success is one policy, not two:** every write announces a short polite phrase here on success
  — `successAnnouncement`, declared per operation in the same two controllers, with bulk phrases
  counting their doors via `countPhrase`.
- `state/abwab-snapshot.facade.ts` — owns the tree snapshot, loading/error/empty
  state, `load()`/`refresh()`.
- `state/abwab-tree.builder.ts` — pure: DTO → `AbwabTreeSnapshotVm` (live/archive
  partition, gap-tolerant ordering, per-section filtering, name+alias search, and
  `pruneAbwabNodesToVisible` — rebuilds a node list to only the search-visible ids,
  recursing into children, backing the **archive** search filter; cards do not use it, see
  above). One walk feeds
  two presentations: the tree reads `matchedIds`/`autoExpandedIds` to mark and seed, the
  filtering views read `visibleIds` — the archive through the prune, cards directly. The walk
  carries a single push/pop ancestor stack rather
  than allocating a path array per edge; the builder spec's exact-set cases are the fence
  that keeps its output identical.
- `state/abwab-selection.store.ts` — single selection + bulk set, rebinds by id after
  every refresh, dropping ids a write made vanish. Bulk mode is unavailable while the
  archive view is active. **A scope change empties the bulk set, and the rule lives in the
  store, not at the call sites**: `setArchiveViewActive` and `setSectionScope` are fed from
  the page's one URL subscription, so both entry paths into a section change (the tabs, and
  a reveal that writes `section` itself) are covered by one rule and a bulk operation can
  never carry doors that are no longer in the visible scope. The two are not symmetric —
  turning the archive view on also **exits** bulk mode, because bulk is forbidden there
  (`setBulkMode` refuses while it is active), while a section change only clears the set and
  leaves the mode the user turned on alone.
- `state/abwab-write.controller.ts` — every door write, plus the section commands
  `state/abwab-sections.controller.ts` delegates to it, the outcome→message mapping,
  and the 409 policy (see Gotchas below) — one policy for both aggregates, not
  duplicated per command.
- `state/abwab-sections.controller.ts` — the section-facing write surface: reads
  `sections` live from the facade snapshot (never cached) and forwards
  create/rename/reorder/delete to the shared write controller above — the same four writes
  the sections modal takes as inputs.
- `state/abwab-relations.controller.ts` — the relations-facing surface, built the same
  way: it owns only what is relation-specific (the per-door fetch, **its cache**, and the
  wire↔domain mapping of both enums) and forwards both writes to the shared write
  controller, so the 409 policy and the refresh-after-write invariant stay in one place for
  all three aggregates. **The cache is a `doorId → list` map whose identity is the snapshot
  ETag** the facade exposes as `snapshotValidator` — `bootId + tree generation`, the server's
  own answer to "which tree is this", moved by every write that can alter any relation list.
  When it moves, **every** entry is dropped; a null validator (no snapshot identity held)
  serves nothing from the map. A `304` and a failed refresh both keep it, because both keep
  the snapshot and its validator as one unit. `loadFor` is the cache-aware read; `refetchFor`
  is the forced one the modal uses after a write, because the write's own snapshot refetch is
  fire-and-forget and has usually not landed when the modal reloads.
  - **Rename pin — binding on any future finer-grained invalidation.** A narrower rule must
    still evict on **door rename**: a cached list embeds the partner's name and its ordering,
    and a rename changes both while no count moves anywhere to signal it. Today's
    clear-everything-on-validator-change covers this for free, so the guard spec in
    `abwab-relations.controller.spec.ts` is labeled a regression guard rather than proof — this
    sentence is what binds the requirement, and `Persistence/Reads/Abwab/README.md` carries it
    on the server side.
  - **Why not `AbwabTreeDto.version`:** it is diagnostics-only (below) and is *factually blind*
    to relation writes — `GetSnapshotVersionAsync` reads sections/doors/aliases only, so an add
    or a delete moves the ETag and leaves `version` untouched. A version-keyed cache would serve
    stale lists on exactly the writes that matter most.
- `state/abwab-templates.facade.ts` — the template list and the selected template's tree,
  on the snapshot facade's contract (`refresh` always refetches; a failure leaves the
  previous value in place). Root-scoped: it is a cache.
- `state/abwab-templates.controller.ts` — every templates write, its refresh target, and
  the announcement. **Not** `AbwabWriteController` — see Gotchas.
- `state/abwab-url-sync.ts` — parses/builds the seven query keys below, fail-closed.
- `data-access/abwab.api.ts` — the doors/sections/relations endpoints under
  `/api/abwab`; `data-access/abwab-templates.api.ts` — the templates endpoints. Each file's
  own public methods are the list; do not restate either length here. Two files, not one:
  a separate route family, and enough of them to carry their own file.
- `models/abwab.models.ts` / `models/abwab.labels.ts` — view models and every Arabic
  string (read via TDZ-safe getters in consumers, never `readonly` field
  initialisers).

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

Fail-closed rules, all pinned in `abwab-url-sync.spec.ts`'s negative table: the id must be a
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
switch or archive-on clears it with everything else. Both overwrite orders are pinned in the
page spec so this stays a decision rather than an artifact.

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
  once and hands the chevrons straight back to the user. Search auto-expansion arrives on the
  same input now (ux-slice-l); the page unions the two sources and must return the shared
  `NO_IDS` when both are empty, or the tree's merge effect re-runs on every tick.
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
source door along with `door=` (pinned since ux-slice-l in both the page spec and
`e2e/abwab-relations.e2e.ts`; before that the designed path had no coverage at all). Since
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
closed closes it, and restoring hands back a pristine overlay, never the draft. Pinned in
the page spec so it stays a decision.

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
  under the old scrolled-away navbar. Two intentional behavior changes shipped with this phase,
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
  Every authoring one is
  `.qd-modal.qd-modal--fixed` with `__head`/`__body`/`__foot`, `role="dialog"`,
  `aria-modal="true"`, an `aria-labelledby` pointing at its own `<h3>`, `qdModalScrollLock`,
  Escape-to-close, and `cdkTrapFocus cdkTrapFocusAutoCapture` — the trap conditional in exactly
  the two that nest a `qd-confirm-dialog`, see below. Consequences worth knowing before
  changing one:
  - **No modal states a height, and none nests a scroller.** `__body` is the single scroller;
    the four inner `max-block-size` caps that existed before Slice C are deleted. Adding one back
    re-creates the §17 specificity trap the caps were.
  - **Authoring modals never stack with each other**, so the four that nest no
    `qd-confirm-dialog` trap unconditionally — the door and template-node modals' dirty-discard
    strip is a `role="alertdialog"` region inside `__foot` with no trap of its own, so it does
    not qualify. The one
    permitted nesting is a **confirmation dialog above exactly one authoring modal**, and the
    host yields while it is open — **two modals do this now, not one**: the sections modal binds
    `[cdkTrapFocus]="deleteConfirmId() === null"` for its section-delete confirm and the
    relations modal `[cdkTrapFocus]="pendingDelete() === null"` for its relation-delete confirm,
    so in each case the confirm's own trap is the only live one (the words dialogs'
    `drawerTrapEnabled` pattern, applied). Two live traps fight over focus, so a second nesting
    level — or a confirmation above a confirmation — is still forbidden, and a modal that grows
    a nested confirm must make its trap conditional in the same change.
  - **Auto-capture is aimed, not corrected after the fact.** Four modals want a control the trap
    would not pick on its own: the door and template-node modals want the name field, the
    relations and copy modals the picker search. Each of those two targets carries
    `cdkFocusInitial` — in `abwab-door-fields-form` and `abwab-door-picker` respectively, so two
    attributes serve all four modals — which is what the trap's own capture reads, so a modal
    opens with **one** focus move. The queued `focusFirstField()` / `focusSearch()` calls stay
    behind it: they are the only path in jsdom, and they cover a capture that resolves before the
    target renders. Do not "simplify" this by dropping `cdkTrapFocusAutoCapture` — the CDK stores
    the previously focused element *only* inside auto-capture, so dropping it silently stops focus
    returning to the trigger on close. Sections and the move picker want the trap's default first
    tabbable and mark nothing. For the move picker that default is not "the first control in the
    DOM": its section strip is a roving-tabindex tablist, so every cell but the active one is
    `tabindex="-1"` and the trap lands on the section the move starts from — which is the
    behaviour wanted, reached without a `cdkFocusInitial`. Where focus lands is verifiable only in a browser: jsdom gives every
    element a zero-size box, so the CDK's focusable check rejects every target, auto-capture never
    moves focus there, and its "not focusable" warning is filtered in `src/test-setup.ts` as the
    pure noise it is.
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
  coherent — pinned by a spec case in `abwab-move-picker.component.spec.ts`, not a side effect.
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
  `writeConflictFallback` already covers that case. The M27 test pins the frontend's verbatim copy
  of the shipped backend string, so frontend drift fails loudly; the backend literal
  (`ApiMessages.cs:117`) is pinned by no backend test, so a backend copy edit is caught only by
  this paragraph's sync rule — re-verify the pair whenever either file changes.
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
- **Loading/empty/error surfaces are composed, not hand-rolled.** Every text-only loading,
  empty, and error site across `abwab-page`, `abwab-templates-page`, the template copy modal,
  and the relations modal now composes `qd-skeleton-rows`/`qd-panel-skeleton` (loading) or
  `qd-state` (empty/error) — `UI_STYLE_SYSTEM.md` §17. **The relations modal's own read is one of
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
  initialisers, or they resolve to `undefined` in the bundled test build.
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
  not implement. `aria-level` still conveys depth and every keyboard affordance has a real focus target.
  Reusing `AbwabTreeComponent` itself was rejected up front, not discovered mid-work: it is
  typed on `AbwabNode` and carries selection/bulk/roving-tabindex/URL concerns this page has
  none of, plus a spec suite pinned to that behavior. **ux-slice-g adds a third row-menu path —
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
  first — which is how this originally shipped — throws, gets swallowed as a transport error,
  and leaves the UI reporting failure while the backend write has committed. Vitest missed it
  because the specs mocked `AbwabApi` with well-formed envelopes; the browser suite caught it.
  Both the null-envelope path and the real 204 flush are now pinned by tests, and
  `abwab-archive.e2e.ts` drives archive through the UI end to end. The relation delete rides on
  that same already-pinned `handleSuccess` branch but has **no test of its own at the controller
  seam**: `abwab-write.controller.spec.ts` has no `deleteRelation` case, and the modal-level
  cover in `abwab-relations-modal.component.spec.ts` runs against a mocked delete function that
  never reaches this branch. Anyone touching the 204 handling or the relations routes should
  assume this half is unpinned.

## Browser e2e (Slice B2)

**Every `e2e/abwab-*.e2e.ts` spec belongs to this feature — that glob is the rule, not a list
kept here.** It is also literally the membership test: `playwright.config.ts`'s `abwab` project
is `testMatch: /abwab-.*\.e2e\.ts$/` and the `default` project `testIgnore`s the same pattern, so
naming a spec `abwab-*` is what enrolls it. `e2e/README.md` carries the inventory and what each
one owns; do not re-enumerate them here, and do not read any list in this file as complete.

Between them those specs drive this
page end to end — sections, root/child doors with alias chips, the dirty guard, inline reorder,
single and bulk move, bulk archive, the row context menu, archive/restore including the
parent-must-restore-first rule and the retired-section restore that demands a destination, all
seven URL query keys including a
restorable overlay's reload/Back-Forward round trip, the tree's
ARIA/roving-tabindex/RTL keyboard model, both halves of the section-delete contract (409 while a
live door remains, and the `204 No Content` success once its doors are archived), and the
superset/section order independence (a `Global` reorder leaves a section's `orderValue` untouched
and vice versa). Because a `Global` reorder resequences every live root in the database, the
whole `abwab` project runs single-worker — see `e2e/README.md`.

`e2e/fixtures/abwab.ts` is the shared sandbox: each test creates its own uniquely-named
section over the API, and tears down by archiving **every live door in that section** — not
only the ids it handed out, since flows create doors through the UI too — and then deleting
the now-empty section. Teardown re-reads each door's version immediately before archiving it:
every write resequences the scope, so archiving from one up-front snapshot succeeds once and
then `409`s silently for the rest, which is what previously left live sandbox doors and
undeleted sandbox sections behind. See `e2e/README.md` and `TESTING_STRATEGY.md` §11 for the
residue that legitimately remains.

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

- Planning history: the feature's plans and the UX slice series that followed were swept per the
  planning-artifact lifecycle rule (`CLAUDE.md`) and live in git history. **This file is the
  current record** — it is where a decision those plans made should be read from now.
- Design contracts: the static comps this feature was drawn against were adopted and then deleted;
  the shipped tree, relations modal, and templates workshop are the contract now, with the token
  and component vocabulary in `.architecture/UI_STYLE_SYSTEM.md`.
- Shared UI primitives: `../../shared/README.md`.
