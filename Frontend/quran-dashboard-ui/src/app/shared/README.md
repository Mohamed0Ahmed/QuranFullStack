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
- `ui/state/` — `qd-state`, the one empty/loading/error presentation; backed by the existing
  `.qd-empty-state`/`.qd-loading-state`/`.qd-error-state` classes. See `UI_STYLE_SYSTEM.md` §17.
- `ui/skeleton/` — `qd-skeleton-rows`, renders N skeleton rows into a caller-supplied
  `grid-template-columns` string so loading rows match loaded rows exactly; plus the pure
  `splitGridTemplateColumns` helper it's built on.
- `ui/explorer-panel-skeleton/` — `qd-panel-skeleton` (class `ExplorerPanelSkeletonComponent`),
  the generalized loading skeleton for explorer/detail panels, with a `shape` input
  (`'lines' | 'rows' | 'panel'`; default `'lines'` reproduces the original six-line panel
  skeleton). The `qd-explorer-panel-skeleton` selector is kept as a thin alias on the same
  component for existing call-sites.
- `ui/modal-scroll-lock/` — directive that locks body scrolling while a modal is mounted.
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
- Browser-only helpers here keep SSR/test guards where needed (`matchMedia`, `document.body`, and similar).

## Related

- App-wide boundaries: `../core/README.md`
- Current feature patterns: `../features/words/README.md`, `../features/mushaf/README.md`
