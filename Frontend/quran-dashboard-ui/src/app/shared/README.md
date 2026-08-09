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
  them. See `UI_STYLE_SYSTEM.md` §17.
- `ui/chip/` — `qd-chip`, the one selectable/informational chip (button or anchor, optional
  trailing count). See `UI_STYLE_SYSTEM.md` §17.
- `ui/context-menu/` — `qd-context-menu`, the one row/node context-menu shell (Slice A, both
  Abwab pages' row menus). Owns a `position: fixed; inset: 0` backdrop and a positioned
  `role="menu"` box (`[x, y]` via a `position` input), both keyed off the shared `--qd-z-*`
  layer scale, plus a document-level `keydown.escape` dismissal (`dismissed` output) —
  document-level so Escape works whether or not focus sits inside the menu (the menu now
  takes focus on open, but a pointer-opened menu can lose it again to the page). `menuTestId` / `backdropTestId` inputs keep each
  page's test ids byte-identical through the extraction. Items are projected content
  (`<ng-content>`): the primitive knows nothing about doors or template nodes, and the item
  hover/focus/danger styling lives in the global `.qd-context-menu__item` classes
  (`_components.scss`), not this component's own stylesheet, since content the *consumer*
  projects sits outside the primitive's emulated-encapsulation boundary. Since ux-slice-l it
  **owns its own placement**: it measures its box after render, pins the box's inline-start
  edge to the anchor so the box grows in the reading direction, flips when it would cross the
  far viewport edge, and clamps both axes to an 8 px margin — so a caller passes a raw pointer
  position and the primitive decides where the menu actually lands — the math is the pure
  `placeContextMenu()`/`resolveMenuDirection()` in `context-menu-placement.ts`, unit-pinned
  per branch in `context-menu-placement.spec.ts`. It also **manages focus**:
  first `role="menuitem"` focused on open, ArrowDown/ArrowUp traversal with wrap, and focus
  returned to the opener on close unless something else already claimed it. See
  `UI_STYLE_SYSTEM.md` §17 for the full placement and focus contract.
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
  dialog shell of the global detail overlay (Feature 029): RTL `role="dialog"` +
  `aria-modal`, labelled heading, CDK focus trap with auto-capture, Escape/backdrop
  dismissal, Back (depth > 1)/Close header actions, the optional `kindLabel` chip and
  `countText` meta beside the title (Feature 030), the fixed restore control shown while
  a retained stack is closed (focused after Close), polite live regions for title/status,
  and reference-counted scroll locking. It is the only enabled focus trap while it is
  open: the Words drawers suspend theirs off `DetailOverlayHistoryService.isOpen`. Back
  never leaves focus on the document — a pop restores the invoking link inside the dialog
  when it survived the re-render, else Close, else the heading.
  It owns no entity, API, URL, or history state.
  Its geometry is fixed on both axes and the count's box is always reserved, so no state
  change resizes the dialog or shifts the header; the count sits outside the heading and
  both live regions (it would otherwise double-announce) and is wired via
  `aria-describedby`. See `.architecture/UI_STYLE_SYSTEM.md` §17 for the full contract.
- `ui/confirm-dialog/` — `qd-confirm-dialog`, the house confirmation dialog. Body content is
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
  holder releases. Never lock `document.body` directly. `ScrollLockService.isLocked` (Slice
  B2, T904) is a public signal derived from the same lock count — `.qd-navbar`
  (`core/layout/top-navbar/`) reads it to go `[inert]`/`[aria-hidden]` while any modal dialog
  holds the lock, so this is the one piece of state the chrome-inert rule reads; do not add a
  second "any modal open" service (`.architecture/UI_STYLE_SYSTEM.md` §17 "Chrome-inert
  rule"). Which surfaces hold the lock is not a list to maintain here — it is whatever holds
  `ScrollLockService`'s lock: `grep -rn qdModalScrollLock src/app/` **plus**
  `detail-modal-shell.component.ts:63`, which acquires it imperatively in an effect with no
  directive in its template, so the grep alone under-reports by one. Note that
  `qd-confirm-dialog` applies it, so **every confirm in the app** is a holder and makes the
  chrome inert.
- `ui/pagination/` — reusable pagination component, windowing helpers, labels, and tests.
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
