# Shared (cross-feature primitives)

Reusable Angular primitives shared across features. If logic or UI is feature-owned, it does not belong here.

## What lives here

- `layout/` — the responsive-band contract. `breakpoints.contract.json` is the **single neutral
  source** for Compact `≤767` / Medium `768–1079` / Wide `≥1080` / Wide-plus `≥1440`;
  `breakpoints.ts` imports it and derives every media-query string plus `qdBandForWidth()` and
  `qdIsWidePlus()`, and `tailwind.config.js` requires the same JSON for its named screens. It is a
  contract, not a mirror: `../../styles/_breakpoints.scss` restates the numbers only because Sass
  cannot import JSON, and `npm run check:golden-ui` fails when the restatement drifts.
  `QD_BP_PHONE_MAX_QUERY` / `QD_BP_TABLET_MAX_QUERY` / `QD_BP_DESKTOP_MIN_QUERY` remain as aliases
  for unmigrated call-sites, but they now resolve to the Golden bands — the tablet ceiling is
  `1079` and the desktop floor is `1080`, not `1023`/`1024`. Wide-plus is a measure enhancement
  only: `qdBandForWidth(1440)` is deliberately `'wide'`, and `widePlusIsStructural` in the contract
  says so, because a fourth structural composition is exactly the drift the band vocabulary exists
  to prevent.
- `ui/tabs/` — `qd-tabs` (the app-wide tablist) + the `qdTab` directive. `qd-tabs` owns no
  selection state: consumers project their own `<a routerLink>`/`<button>` tab elements marked
  with `qdTab [selected]="…"` and their own click/routerLink; `qd-tabs` supplies the
  `role="tablist"` wrapper and RTL-aware roving-tabindex keyboard nav (Arrow/Home/End) over
  them. Phase 3 added: a per-instance `instanceId` on the tablist, a generated per-tab `id`, an
  optional `panelId`/`disabledReasonId` pair, count-driven layout (`qd-tabs--segmented` at three
  tabs or fewer, `qd-tabs--scrollable` at four or more — D30), and selected-tab
  scroll-into-view. The generated attributes are **fallbacks applied in `ngAfterViewInit` and only
  when the element carries none** — `abwab-move-picker` and `access-admin-page` bind their own
  `id`/`aria-controls`, and a host binding would have removed theirs. See `UI_STYLE_SYSTEM.md`
  §17 and §20.1.
- `ui/chip/` — `qd-chip`, the one selectable/informational chip (button or anchor, optional
  trailing count) with an optional `variant` (`filter` / `taxonomy` / `alias`; `plain` default adds
  no class, so existing call-sites are untouched). It owns the **interactive** chip families only:
  static lifecycle, membership and count badges have no interaction and are semantic classes
  (`.qd-badge--lifecycle-*`, `.qd-badge--membership-owner`, `.qd-count-chip` in `_components.scss`)
  with no Angular owner — F17 keeps lifecycle and Owner membership separate, and Unknown never maps
  to Disabled. See `UI_STYLE_SYSTEM.md` §17 and §20.5.
- `ui/context-menu/` — `qd-context-menu`, the one row/node context-menu shell (both Abwab pages'
  row menus). Since Phase 7 it is a **thin specialization of F15**: it owns only the
  `position: fixed; inset: 0` backdrop and the `role="menu"` box, and delegates placement,
  keyboard and focus to `qdFloatingLayer="action-menu"` anchored by `anchorPoint` (the raw
  pointer position). Placement, block-axis flip, inline clamp, the `min(60vh, 24rem)` cap,
  Escape/Tab/outside dismissal, arrow traversal with wrap, type-ahead and focus return to the
  opener are therefore the same code every menu, listbox and picker runs (D33/D34); the old
  `context-menu-placement.ts` module was folded into `floating-layer-placement.ts` as the
  zero-size `pointerAnchorRect()` anchor and deleted. `menuTestId` / `backdropTestId` inputs keep
  each page's test ids byte-identical. Items are projected content (`<ng-content>`): the
  primitive knows nothing about doors or template nodes, and the item hover/focus/**danger**
  styling is the one shared rule shared by `.qd-floating-layer__item` and `.qd-context-menu__item`
  in `_components.scss` (D50 — template delete and Abwab delete cannot drift apart), not this
  component's own stylesheet, since content the *consumer* projects sits outside the primitive's
  emulated-encapsulation boundary. See `UI_STYLE_SYSTEM.md` §17.
- `ui/ayah-card/` — `qdAyahCard` (attribute component, host class `qd-ayah-card`), the one
  presentation-only flat frame for ayah-shaped list items (recessed warm card background
  `--qd-ayah-card-bg`, hairline border, control radius, compact padding/gap; no shadow, no
  alternating fill — the recessed tone makes the card read as a distinct card on the near-white
  surfaces it sits on; dark keeps its current surface tone). It takes no domain model, text,
  formatter, route, or output — callers keep their own semantic wrapper (article/li), Quran
  renderer, and navigation. Consumers: Words `ayah-matches-list`, Mushaf `similar-ayahs-card`
  items and `mutashabihat-groups-card` occurrences. See `UI_STYLE_SYSTEM.md` §17.
- `ui/action/` — `qdAction`, the F05 action **directive** on a native `button`/`a`. Variants
  `primary | secondary | tertiary | danger | icon-only | toolbar | row-action`, sizes `sm|md|lg`
  mapped to the `32/40/48` control scale, and a `busy` input that sets `aria-busy` and reveals a
  spinner in an icon slot that is **reserved from the moment `busy` is bound at all**, so going
  busy cannot resize the control. It never touches `disabled`: native disabled stays with the
  call-site, and no button semantics are grafted onto an anchor. Styling is the global
  `.qd-action*` family in `_components.scss`. See `UI_STYLE_SYSTEM.md` §19.
- `ui/form-field/` — `qd-form-field` + the `qdControl` directive (F06). The field owns the
  `label`/`helper`/`error` structure and generates the per-instance ids; the directive, applied to
  the projected native `input`/`select`/`textarea`, resolves the field through DI and binds `id`,
  `aria-describedby` and `aria-invalid` from it. A `qdControl` with no field parent stays a plain
  control with its own optional `invalid` input — it never borrows another field's ids. Validation,
  options and domain copy stay with the feature.
- `ui/refreshing-indicator/` — `qd-refreshing-indicator` (F12 *refreshing*): a flat 2px track with
  one solid green segment, rendered only while `active`, `aria-hidden`, and carrying **no** status,
  alert, dialog role or live region. The refreshed region keeps its content and owns the
  `aria-busy` announcement; add `.qd-refreshing-region` to that region so the absolutely positioned
  track anchors to it and adds no geometry.
- `ui/empty-state/` — `qd-empty-state` (F12 *empty*): `role="status"`, one message, at most one
  action. Optional `reserve` keeps the box mounted and its message quiet until it lands.
- `ui/error-state/` — `qd-error-state` (F12 *error/notFound*): `severity="read"` (default) renders
  a scoped retry block with **no** alert role — a failed read is announced through the workspace's
  own polite region — while `severity="write"` is the only `role="alert"`, and it never clears the
  draft. `reserve` keeps a permanently mounted alert region quiet while empty so a later failure
  lands in an element that already existed.
- `ui/notice/` — `qd-notice` (F12 *notice*): a permanently mounted `role="status"`/`aria-live`
  announcer with **zero idle geometry** (D41) — the body only exists while a message does.
  `tone` is `success` (mutation success semantics) or `info`; failures belong to `qd-error-state`.
- `ui/state/` — `qd-state`, the **temporary compatibility adapter** for the five owners above. It
  keeps its `variant`/`message`/`actionLabel`/`reserve` inputs, its `action` output, its selector
  and its `qd-state-*` test ids, and translates them: `loading` → `qd-panel-skeleton shape="text"`,
  `empty` → `qd-empty-state`, `error` → `qd-error-state severity="write"` (the legacy variant has
  always been `role="alert"`, and weakening that would change 28 call-sites' announcing). It owns
  no role, live region or state styling of its own — `npm run check:golden-ui` fails if its
  template regains one — and it may not gain a consumer. `reserve` (default off) is the §N3
  no-layout-shift box: `grep -rn '\[reserve\]' src/app/` for the current consumers, and see §17's
  note on `reserve` under `@if`. See `UI_STYLE_SYSTEM.md` §17 and §19.
- `ui/skeleton/` — `qd-skeleton-rows`, renders N skeleton rows into a caller-supplied
  `grid-template-columns` string so loading rows match loaded rows exactly; plus the pure
  `splitGridTemplateColumns` helper it's built on.
- `ui/explorer-panel-skeleton/` — `qd-panel-skeleton` (class `ExplorerPanelSkeletonComponent`),
  the generalized loading skeleton for explorer/detail panels, with a `shape` input
  (`'lines' | 'rows' | 'panel' | 'text'`; default `'lines'` reproduces the original six-line panel
  skeleton). `shape="text"` is the D40 **single-value text loader** — a `.qd-loading-state` region
  with a visible label, `role="status"`, `aria-live="polite"` and `aria-busy` — and it lives here
  rather than in a sixth async component because loading is one owner with two shapes: a surface
  with a known final shape must use a content-shaped skeleton, and only a single-value region may
  use the text loader. `testId` lets a legacy call-site keep its own id (the adapter passes
  `qd-state-loading`). The `qd-explorer-panel-skeleton` selector is kept as a thin alias on the
  same component for existing call-sites.
- `ui/result-count/` — `qd-result-count` (class `ExplorerResultCountComponent`), the one-line
  "label: N" stat that holds its line across loading/error/loaded rather than resizing the
  toolbar around it (Feature 026, US4; Slice B2, T1001 promoted it here from `features/words/`
  once abwab became a second consumer — `FRONTEND_STRUCTURE.md`'s "genuinely reused across
  features" bar). `qd-explorer-result-count` is kept as a thin alias selector on the same
  component so the four words explorer call-sites (Unique Words, Roots, Lemmas, Stems) and their
  spec kept working untouched through the move — the same dual-selector mechanism as
  `ui/explorer-panel-skeleton/`. Its own labels (`result-count.labels.ts`) are read through a
  TDZ-safe **getter**, not a `readonly` field — a `readonly` field resolves to `undefined` in the
  bundled test build (temporal dead zone), the same rule `features/words/README.md` and
  `features/abwab/README.md` state for their own `*.labels.ts` files. See
  `UI_STYLE_SYSTEM.md` §17 "`qd-result-count`".
- `ui/detail-modal-shell/` — `qd-detail-modal-shell`, the presentation-only accessible
  dialog shell of the global detail overlay (Feature 029). Since Phase 7 it is a **thin adapter
  over `qd-modal-shell` `variant="overlay"`**: the base shell supplies the backdrop, the `46rem`
  named width, the Compact sheet, the single body scroller, shell-owned padding, the focus trap,
  the reference-counted scroll lock and the Escape/backdrop dismissal routes, while the adapter
  keeps everything that is genuinely overlay furniture — Back (depth > 1) and the optional
  `kindLabel` chip in the header lead slot, the always-reserved `countText` meta and Close in the
  header extra slot, the fixed restore control shown while a retained stack is closed (focused
  after Close), and the polite live regions for title/status. It keeps `[returnFocus]="false"`
  because it owns focus itself: Back never leaves focus on the document — a pop restores the
  invoking link inside the dialog when it survived the re-render, else Close. It owns no entity,
  API, URL, or history state. The count's box is always reserved, so an arriving count cannot
  shift the header; the count sits outside the heading and both live regions (it would otherwise
  double-announce) and is wired through the base shell's `describedById`.
  See `.architecture/UI_STYLE_SYSTEM.md` §17 for the full contract.
- `ui/confirm-dialog/` — `qd-confirm-dialog`, the house confirmation dialog, now a **thin adapter
  over `qd-modal-shell` `variant="confirm"`** (Phase 3). Its selector, inputs, outputs and
  `testIdPrefix`-derived test ids are unchanged; the shell supplies the backdrop, the `30rem`
  named width, the single body scroller, the focus trap, the scroll lock and the dismissal routes,
  and the adapter keeps `role="alertdialog"`, its two actions and its copy. Body content is
  projected, so a consumer composes whatever the decision needs while the primitive keeps the
  framing, the roles, the focus trap and the dismissal routes. It does **not** replace an
  authoring-modal shell: those own a form and its dirty state. Two invariants that are not
  visible from the call site and must not be "fixed":
  - **Initial focus is the CANCEL button, deliberately** (`confirm-dialog.component.ts`
    `focusCancel`). A confirm dialog interrupts, so the answer a reflexive Enter produces has to
    be the safe one. Moving initial focus to the confirm button turns every destructive dialog
    into a one-keystroke accident.
  - **`busy` disables both buttons, not just confirm.** A decision in flight must not be
    double-fired, and cancelling mid-write would leave the caller's state ambiguous.
- `ui/modal-scroll-lock/` — `qdModalScrollLock` directive + `ScrollLockService`, the
  **reference-counted** body scroll lock (Feature 029): overlapping layers (responsive
  drawer + global overlay) each acquire/release; the body unlocks only when the last
  holder releases. Never lock `document.body` directly. Since Phase 3 the lock is held by
  **token**, not by a bare counter: `hold()` returns an idempotent `ScrollLockHandle`, so a layer
  that releases twice (an explicit close followed by destroy) can no longer decrement a *different*
  layer's hold and unlock the page under a still-open dialog. `acquire()`/`release()` remain as the
  legacy anonymous LIFO pair for the call-sites that still use them (`top-navbar`), with
  byte-identical behaviour. `ScrollLockService.isLocked` (Slice
  B2, T904) is a public signal derived from the same lock count — `.qd-navbar`
  (`core/layout/top-navbar/`) reads it to go `[inert]`/`[aria-hidden]` while any modal dialog
  holds the lock, so this is the one piece of state the chrome-inert rule reads; do not add a
  second "any modal open" service (`.architecture/UI_STYLE_SYSTEM.md` §17 "Chrome-inert
  rule"). Which surfaces hold the lock is not a list to maintain here — it is whatever holds
  `ScrollLockService`'s lock: `grep -rn qdModalScrollLock src/app/` **plus** every mounted
  `qd-modal-shell`, which holds a token while it is open. Note that
  `qd-confirm-dialog` applies it, so **every confirm in the app** is a holder and makes the
  chrome inert.
- `ui/data-table/` — `qd-data-table` (F09), the domain-free mounted table shell. Its frozen
  renderer vocabulary is exactly `standard | wide-columns | grouped-rows`; it owns lifecycle,
  table/list ARIA counts, selection state, pagination placement, the virtual-scroll path and the
  no-`ResizeObserver` fallback. Consumers supply `rowId` plus projected
  `headerTemplate`/`rowTemplate`/`compactRowTemplate` and lifecycle/pagination templates, so domain
  columns and actions never enter shared UI. Compact renders semantic list cards; non-Compact
  renders table rows, tracked by the supplied identity. Grouped rows are display-only even when a
  caller sets `selectable`. `qd-sortable-header` owns the native sort button and exposes
  `aria-sort` only while active. `table-scrollbar-gutter-sync.ts` is the shared geometry helper;
  feature compatibility paths may re-export it but must not fork the implementation.
- `ui/hierarchy/` — `qdHierarchyKeyboard` (F16), the domain-free tree/hierarchy keyboard owner:
  `flattenQdHierarchyRows` turns any `{ id, children }` shape into the visible-row list,
  `resolveQdHierarchyIntent` resolves ArrowUp/Down/Home/End plus the **logical** branch arrows
  (inline-start enters/expands, inline-end collapses/exits, mirrored by the host's `dir`), and the
  directive adds roving DOM focus by looking a row up through the neutral
  `data-qd-hierarchy-id` attribute on its own host subtree. It owns **only** movement: activation,
  selection, bulk and menu keys stay with the feature, and `resolveQdHierarchyIntent` returns
  `none` for every one of them. Consumers: Abwab's live tree and archive tree, through
  `features/abwab/components/abwab-tree/abwab-tree-keyboard.controller.ts`, which is a thin
  adapter and not a second implementation — its spec asserts the two resolvers agree on every
  movement key. It knows nothing about doors, roles, ARIA or selection: the consumer still
  declares `role="tree"`/`treeitem` (or `role="list"` where the hierarchy is a list, as the
  template tree deliberately is) and owns its own expansion state.
- `ui/result-list/` — `qdResultList` + `qdResultItem` (F10), the native-role directive pair for
  every non-table result collection: `role="list"`/`role="listitem"` (D25), an optional
  `listVariant` (`linked` / `display-only` / `master` / `event` / `quran-result`), the logical
  selected thread through `.qd-is-selected` (D26), `aria-current` for the selected master row, and
  optional `aria-posinset`/`aria-setsize`. It adds **no** `tabindex`: a row is focusable only when
  the consumer made it a real control (§8.1 disclosure ladder). Quran result rows keep their own
  renderer inside this frame (G11).
- `ui/details-workspace/` — `qd-details-workspace` (F11), the projected details anatomy: identity,
  metadata, actions, an optional tab zone, a permanently mounted polite status slot, exactly one
  body scroller, and an optional footer. It carries **no** feature data — every zone is
  `<ng-content>` — and it namespaces `identityId`, `statusId`, `tabId(key)` and `panelId(key)` per
  instance (D31) so an inline panel and the global overlay body cannot collide. `layout="no-selection"`
  renders the designed prompt instead of collapsing the split.
- `ui/modal-shell/` — `qd-modal-shell` (F14 base), the one dialog shell: four named widths
  (`confirm` 30rem / `form` 38rem / `wide` 52rem / `overlay` 46rem — D48, and no fifth), a Compact
  full-bleed `94dvh` sheet, shell-owned padding with header and footer outside the single body
  scroller (D49), a CDK focus trap, `dismissed` carrying its route
  (`close` / `escape` / `backdrop`) so a dirty consumer can refuse a route without losing the close
  button, focus return to the opener, and a reference-counted scroll-lock hold released on close
  **and** on destroy. Marking the route content behind it inert stays with the app shell
  (`app.ts` reads the overlay state; the navbar reads `ScrollLockService.isLocked`) — this shell
  does not reach outside itself to set `inert`. Four rules are not visible from the call site:
  - **Escape is consumed while the shell is topmost, dismissing or not.** `dismissOnEscape="false"`
    refuses the *route*, not the *key*: the handler still stops propagation and prevents the default,
    or the key reaches an ancestor drawer (`(keydown.escape)`) or the navbar's document-level
    listener and closes the wrong surface.
  - **Backdrop dismissal needs the whole pointer sequence on the backdrop.** The press target is
    recorded on `pointerdown`/`mousedown` and compared with the click target, so a drag-select that
    began inside the body and was released over the backdrop does not discard the draft. A
    programmatic click with no recorded press (keyboard, tests) still dismisses.
  - **The shell owns which trap is enabled, not the consumer.** Open shells register in an internal
    stack and only the topmost enables its `cdkTrapFocus`, so nesting a confirm inside an authoring
    modal cannot leave two live traps. `[trapFocus]="false"` is the explicit suspend switch a
    consumer uses when it hosts a nested decision of its own (the shape the Abwab
    `[cdkTrapFocus]="deleteConfirmId() === null"` dialogs migrate onto in Phase 7).
  - **Focus return has exactly one owner — this shell.** It captures the pre-open `activeElement`,
    drives initial focus through the trap itself, and restores **synchronously** on close and on
    destroy-while-open. `cdkTrapFocusAutoCapture` is deliberately absent: it would restore a second
    time on its own schedule. A consumer that wants to place focus itself sets `[returnFocus]="false"`
    and owns the placement end to end.

  Phase 7 added the three knobs the migrated consumers needed and nothing more: `describedById`
  (an `aria-describedby` target such as the overlay's reserved count), `dialogTestId` (keeps a
  consumer's pre-existing dialog test id when it is not the `testIdPrefix`), `flushBody` (the body
  brings its own details-workspace chrome and scroller, so the shell contributes no second padding
  ring), and the `[qdModalShellHeaderLead]` / `[qdModalShellHeaderSupport]` slots beside the
  existing `[qdModalShellHeaderExtra]` / `[qdModalShellFooter]`. A header with neither a visible
  title nor a Close renders bare — the labelling heading stays, the chrome does not.
- `ui/floating-layer/` — `qdFloatingLayer` (F15 base) plus the pure `floating-layer-placement.ts`
  helper. One keyboard script for `action-menu` / `select-listbox` / `searchable-picker` /
  `disclosure-popover` / `tooltip` (D33): Escape closes and returns focus, Arrow/Home/End walk the
  **enabled** items with scroll-into-view, type-ahead accumulates inside a 600ms window, Tab closes
  without preventing the move, and an outside pointer press closes without stealing focus. Items are
  found by ARIA role (`menuitem`/`option`), never by a shared option model, so each feature keeps its
  own hierarchy. `placeFloatingLayer()` is pure and unit-pinned per branch: block-axis flip, inline
  clamp, `min(60vh, 24rem)` cap, `position: fixed` — never document flow (D34). The computed
  coordinate is written to `left` because a viewport coordinate has no logical form; the *choice* of
  edge is direction-aware, which is what RTL needs.

  Phase 7 gave it the two anchors and the one ownership rule the remaining consumers needed:
  `anchorPoint` places the layer against a raw pointer coordinate (a zero-size `pointerAnchorRect()`
  with no anchor gap) so `qd-context-menu` runs the same arithmetic as every anchored picker;
  `controlElement` names an **external** combobox field that keeps DOM focus while the layer moves
  its `aria-activedescendant` cursor, which is what lets the Words association filter stay typable
  while its list is navigable; and on destroy the layer returns focus to its anchor only when it had
  actually taken focus and still holds it, so a consumer that deliberately placed focus on close
  keeps it. Four rules that are easy to "simplify" wrongly:
  - **One option model per variant** (catalog F15 §16). `action-menu` roves real DOM focus and never
    writes `aria-activedescendant`; `select-listbox` and `searchable-picker` keep DOM focus on the
    layer (or on the picker's own field) and move an `aria-activedescendant` cursor instead, cleared
    when the layer closes or its variant changes. Running both at once is what made a picker's search
    field unreachable.
  - **The activedescendant cursor is visible, and it is not the selection.** The cursored item carries
    `data-qd-floating-cursor="true"`, styled in `_components.scss` as neutral `--qd-surface-quiet` —
    the same rest/hover neutral, because green means *selected*, never "where the arrow keys are".
    On an already-selected item the cursor reads as a focus ring instead of a second fill. Without
    this a sighted keyboard user arrowing through a picker sees nothing move: DOM focus never leaves
    the field.
  - **Key handling is scoped by event target.** Inside a text input, textarea or `contenteditable`,
    printable keys, Home/End and the caret arrows belong to the field; only ArrowUp/ArrowDown drive
    the option cursor. Space is the same rule read from the other side: it extends a type-ahead
    already in progress, and otherwise belongs to the focused item (APG), so it can still activate a
    menu item. An empty or whitespace-only prefix never matches — `startsWith('')` matches
    everything.
  - **The rem half of the cap is a rem.** `floatingMaxBlockSize()` takes the root font size and the
    directive supplies the live one (`resolveRootFontSize()`), so the inline `max-block-size` and the
    `--qd-floating-max-block-size` token cannot disagree at a non-16px root.
- `ui/pagination/` — reusable pagination component, windowing helpers, labels, and tests. Its
  geometry is fixed in every state (D42): the jump input is always
  `--qd-pagination-jump-inline-size` (`6rem`) and no longer widens on focus, Go is **always
  mounted** and only toggles `disabled` (D43), and Compact controls take
  `--qd-hit-target-min` (D45). `jumpSubmittable` is deliberately "parses to a number", not "is in
  range": an out-of-range page must stay submittable, because submitting is what surfaces the
  reserved-line range error instead of a dead control. Ids for the jump input, its error line and
  its live region are per instance (D44), and every page change announces the new **result range**
  through that instance's own polite region. `Load more` (Access audit) is a separate capability and
  gets none of this API.
- `ui/placeholder-page/` — generic placeholder page that reads its title from route data.
- `ui/safe-html/` — HTML sanitizing pipe for trusted API-backed markup display.
- `url/` — deep-link helpers; today `deep-link-href.ts` builds href strings from path + query params.

## Boundary

- `shared/` is for primitives reused by two or more features, or generic UI helpers with no
  domain ownership.
- `../core/README.md` owns app-wide singletons and cross-cutting boundaries such as navigation,
  interceptors, caching, and theme.
- `../features/` owns routeable pages, facades, feature models, and any Quran-domain behavior.
- Do not move Words- or Mushaf-specific state, labels, or selectors here just to reduce imports.

## Invariants

- Breakpoint values live in `layout/breakpoints.contract.json` and nowhere else; `breakpoints.ts`,
  `tailwind.config.js`, and `../../styles/_breakpoints.scss` all resolve to it, and
  `npm run check:golden-ui` enforces that.
- `ui/state/` is a **compatibility adapter that may not grow**. New code consumes the canonical
  async owners; `npm run check:golden-ui` fails when its template call-site count rises above the
  recorded baseline, and the baseline may only fall (`../../../FRONTEND_UI_RULES.md` §8). The same
  check fails when the adapter's template declares a `role`, `aria-live`, `aria-busy` or a `qd-*`
  class of its own: it delegates, it does not re-implement.
- The five async concepts are five owners with five different geometry contracts. Skeleton reserves
  the final shape of what it replaces; refreshing adds nothing but its 2px track; empty and error
  own their content region; notice is zero-height until it speaks. A shared owner that starts
  reserving for a concept it does not own is how the ~6.5rem blank Access band came back.
- Every dialog resolves to one of the four `qd-modal-shell` widths or the Compact sheet. A
  call-site may not introduce a fifth geometry; `npm run check:golden-ui` reads
  `modal-shell.component.scss` and fails when the set of `.qd-modal-shell--*` width classes is
  anything other than `confirm/form/wide/overlay`.
- There is **one** menu-item treatment, and one destructive variant of it. `.qd-floating-layer__item`
  and `.qd-context-menu__item` are declared as a single rule set in `_components.scss` (hover,
  `:focus-visible`, `[aria-disabled]`, `--danger`, and the Compact `44px` target), so the Abwab row
  menus and the template menus cannot drift apart again (D50). A feature that needs a destructive
  item adds `--danger`; it never writes its own danger hover.
- A shared owner that generates an id **falls back** rather than overwrites. `qdTab` writes its
  generated `id`/`aria-controls` only when the element has none, because two features already bind
  their own and a host binding resolving to `null` removes a template-set attribute.
- `safe-html` sanitizes HTML; it does not bypass Angular security.
- `ui/skeleton/grid-template-columns.ts` splits a `grid-template-columns` string on top-level
  whitespace only (`depth === 0`, `grid-template-columns.ts:22`), so a parenthesised function such
  as `minmax(0, 1fr)` stays one track — but `repeat(n, …)` collapses to a single track instead of
  expanding to `n`. Skeleton `rowTemplate` inputs must therefore be explicit space-separated track
  lists, never `repeat()`, or the skeleton renders the wrong column count.
- Browser-only helpers here keep SSR/test guards where needed (`matchMedia`, `document.body`, and similar).

## Related

- App-wide boundaries: `../core/README.md`
- Current feature patterns: `../features/words/README.md`, `../features/mushaf/README.md`
