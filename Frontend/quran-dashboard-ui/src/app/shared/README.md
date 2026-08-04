# Shared (cross-feature primitives)

Reusable Angular primitives shared across features. If logic or UI is feature-owned, it does not belong here.

## What lives here

- `layout/` — shared layout constants; today this is the canonical breakpoint mirror for the app
  TypeScript side (`breakpoints.ts`).
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
  layer scale, plus a document-level `keydown.escape` dismissal (`dismissed` output) — added
  because none of the four paths that open the menu puts focus inside it, so an
  element-bound handler could never fire. `menuTestId` / `backdropTestId` inputs keep each
  page's test ids byte-identical through the extraction. Items are projected content
  (`<ng-content>`): the primitive knows nothing about doors or template nodes, and the item
  hover/focus/danger styling lives in the global `.qd-context-menu__item` classes
  (`_components.scss`), not this component's own stylesheet, since content the *consumer*
  projects sits outside the primitive's emulated-encapsulation boundary. Since ux-slice-l it
  **owns its own placement**: it measures its box after render, extends toward inline-start,
  flips on either viewport edge, and clamps both axes to an 8 px margin — so a caller passes a
  raw pointer position and the primitive decides where the menu actually lands. It still does
  **not** manage focus into the menu — see `UI_STYLE_SYSTEM.md` §17 for that gap and for the
  full placement contract.
- `ui/ayah-card/` — `qdAyahCard` (attribute component, host class `qd-ayah-card`), the one
  presentation-only flat frame for ayah-shaped list items (recessed warm card background
  `--qd-ayah-card-bg`, hairline border, control radius, compact padding/gap; no shadow, no
  alternating fill — the recessed tone makes the card read as a distinct card on the near-white
  surfaces it sits on; dark keeps its current surface tone). It takes no domain model, text,
  formatter, route, or output — callers keep their own semantic wrapper (article/li), Quran
  renderer, and navigation. Consumers: Words `ayah-matches-list`, Mushaf `similar-ayahs-card`
  items and `mutashabihat-groups-card` occurrences. See `UI_STYLE_SYSTEM.md` §17.
- `ui/state/` — `qd-state`, the one empty/loading/error presentation; backed by the existing
  `.qd-empty-state`/`.qd-loading-state`/`.qd-error-state` classes. Carries an additive `reserve`
  input (default off) for the §N3 no-layout-shift box; the abwab modals and pages opt in where an
  error must not shift the layout under it — `grep -rn '\[reserve\]' src/app/` for the current
  consumers, and see §17's note on `reserve` under `@if`. See `UI_STYLE_SYSTEM.md` §17.
- `ui/skeleton/` — `qd-skeleton-rows`, renders N skeleton rows into a caller-supplied
  `grid-template-columns` string so loading rows match loaded rows exactly; plus the pure
  `splitGridTemplateColumns` helper it's built on.
- `ui/explorer-panel-skeleton/` — `qd-panel-skeleton` (class `ExplorerPanelSkeletonComponent`),
  the generalized loading skeleton for explorer/detail panels, with a `shape` input
  (`'lines' | 'rows' | 'panel'`; default `'lines'` reproduces the original six-line panel
  skeleton). The `qd-explorer-panel-skeleton` selector is kept as a thin alias on the same
  component for existing call-sites.
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
  rule"). Which surfaces hold the lock is not a list to maintain here — it is whatever applies
  `qdModalScrollLock`, so `grep -rn qdModalScrollLock src/app/` is the answer. Note that
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

- Breakpoints in `layout/breakpoints.ts` must stay in sync with `../../styles/_breakpoints.scss`.
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
