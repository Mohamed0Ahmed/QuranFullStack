# Quran Dashboard UI

Angular 20 (standalone components + Signals) frontend for the Quran Dashboard
(المنهج القرآني) — an **Arabic-first (RTL)**, scholarly/calm admin dashboard.

> HOW to work here (rules): `.architecture/FRONTEND_STRUCTURE.md`,
> `.architecture/UI_STYLE_SYSTEM.md`, `.architecture/API_INTEGRATION_GUIDELINES.md`, plus
> `../../PRODUCT.md` and `../../DESIGN.md`. This file is the WHAT (current truth + map).

## Feature map

```text
src/app/core       app-wide: ApiResponse, interceptors, cache, layout shell, routes, theme  → core/README.md
src/app/features
  words            Roots/Lemmas/Stems/WordTypes/Unique-Words explorers                       → words/README.md
  mushaf           page-by-page Mushaf reader + ayah/word study context                      → mushaf/README.md
  dashboard        home
src/app/shared     pagination, skeletons, safe-html, deep-link, breakpoints
src/styles         SCSS tokens/themes/components (see UI_STYLE_SYSTEM.md)
```

## Run / build

```bash
npm install
npm run start:https      # or scripts/qd-ui — dev server at https://localhost:4200
ng build                 # production build → dist/
```

Local HTTPS needs `mkcert localhost` in the project root (see `Backend/scripts/README.md`).

## Generated API contract (types from the backend OpenAPI spec)

- `openapi/swagger.json` — committed OpenAPI spec exported from the backend by
  `Backend/scripts/export-swagger` (offline; no running server).
- `src/app/core/api/generated/` — committed payload DTO interfaces generated from that spec
  (`npm run generate:api`, ng-openapi-gen, models-only: `generate:api` prunes the output to `models/` via `scripts/prune-generated-api.mjs`, so no service/fn files are kept). Never hand-edit generated files.
- `npm run docs:api` — builds a static human-browsable API reference into
  `docs/api-reference/index.html` (redocly build-docs plus `scripts/inline-redoc-bundle.mjs`,
  which inlines the pinned local `redoc` bundle so the file opens fully offline). The output is
  **not committed**: nobody regenerated it on change, which made it stale data wearing a
  contract's clothes. Run it when you want to browse the API; delete it afterwards or leave it,
  it is a local artifact either way.
- Regenerate the spec and the client with `Backend/scripts/check-api-contract`; it fails when
  committed output is stale. Vercel builds rely on the committed spec and client (no dotnet in
  that path); the browsable reference was never part of the build.
- Feature `models/*.models.ts` files re-export the generated wire DTOs (aliased to the
  historical local names) and keep UI-only unions, request params, and view models
  hand-written; closed backend vocabularies the spec types as `string` are narrowed there
  via documented `Omit`-overlays.

## Testing (read before running tests)

**The lanes:** `npm run test:fast`, `test:feature:abwab|auth|dashboard|mushaf|words`,
`test:authorization`, `test:composition`, `test:shared`, `test:full`, plus `typecheck:app`,
`typecheck:spec`, `typecheck`, `build:verify`, and the composite `test:pre-pr`
(`typecheck` → `build:verify` → `test:full`). `npm run test:gates` is the structural check on the
lane definitions and is not part of `test:pre-pr` — run it whenever a spec is added, moved,
renamed, or deleted, or an `include` pattern changes.

**`testing/README.md` is the contract**: what each of the nine named `angular.json`
configurations selects, what every command does, and what `test:gates` proves. Which lane to run
and when is `../../TESTING_STRATEGY.md` §4 and §5.

- **Keep the `VITEST_MAX_FORKS` cap on `npm test`** — without it the run OOMs/freezes the
  machine. Every `test:*` lane delegates to `npm test`, so the cap and the run timeout live in
  that one script; do not inline them per lane. **A `vitest.config.ts` would be ignored** — the
  Angular unit-test builder starts Vitest with `config: false` — so the cap must stay where
  `package.json` already sets it; do not "clean it up".
- **jsdom lacks `matchMedia` / `ResizeObserver` / `requestIdleCallback`** under the builder —
  guard them in components and default to desktop.
- **`src/test-setup.ts` owns an `afterEach` safety net** that runs after every spec's own
  hooks: it unstubs Vitest-stubbed globals, restores real timers, restores spies, clears
  `localStorage`/`sessionStorage`, removes `data-theme` from `<html>`, and clears the inline
  `body` `overflow`. Specs therefore stub browser globals with `vi.stubGlobal`, never with a
  direct `window.x = …` / `Object.defineProperty` assignment — a direct assignment survives a
  test that throws and leaks into the next one. The net deliberately does **not** polyfill
  `matchMedia`, reset `TestBed`, or wipe `body` children.
- **Inside that `afterEach`, `vi.unstubAllGlobals()` must stay before `vi.useRealTimers()`.**
  The first `useRealTimers`/`useFakeTimers` call builds the fake-timer clock and permanently
  captures which timer APIs exist on `globalThis` at that moment; with the order swapped a
  still-installed `requestIdleCallback` stub gets faked for the rest of the file, and
  `src/app/core/navigation/idle-preload.strategy.spec.ts` loses its fallback branch.
- **The `console.warn` filter in `src/test-setup.ts` is a plain assignment on purpose**
  (`src/test-setup.ts:10-15`): it swallows jsdom's unfixable `[cdkFocusInitial]` warning and
  delegates every other warning to the captured original. Rewriting it as
  `vi.spyOn(console, 'warn')` would put it under the `vi.restoreAllMocks()` on line 21, which
  removes it after the first test of every file and brings the noise back. It is the one global
  the safety net deliberately does not restore — not a leak.
- **A spec that appends a fixture host to `document.body` removes it itself**, in a file-local
  `afterEach` that calls `fixture.destroy()` first and `fixture.nativeElement.remove()` second
  (see `src/app/shared/ui/context-menu/context-menu.component.spec.ts`) — Angular teardown
  needs the host still attached.
- **Browser E2E (opt-in):** `npm run e2e` (headless), `npm run e2e:headed`, `npm run e2e:ui`.
  Chromium only. It boots the Angular dev server *and* the backend `https` profile, so it needs
  mkcert certificates, a migrated local `quran_dashboard`, and a prior
  `dotnet build Backend/QuranDashboard.sln`. Specs live in `e2e/` and MUST be named `*.e2e.ts` —
  **not** because the Vitest gate would collect them (it globs with `cwd` at `src/` and cannot
  see outside it) but because `playwright.config.ts` matches `/.*\.e2e\.ts$/`, so a `*.spec.ts`
  there is run by nothing at all while looking like coverage. `npm run e2e` runs two Playwright
  projects in sequence — `default` (2 workers), then every `abwab-*.e2e.ts` at `--workers=1`,
  since a `Global`-scope Abwab reorder resequences every live root and can race a second worker.
  See `e2e/README.md` and `testing/README.md`.

## Invariants

- **Mushaf font is Amiri**, not `UthmanicHafs_V22` (which mis-renders U+06DF) — see
  `src/app/features/mushaf/README.md`.
- **Word identity is clean imlaei-simple** (display Uthmani) — mirrors the backend.
- URL-state in explorers/reader is a shareable contract; keep params stable.
