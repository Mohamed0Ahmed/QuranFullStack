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
- `docs/api-reference/index.html` (repo root) — committed static API reference
  (`npm run docs:api`): redocly build-docs plus `scripts/inline-redoc-bundle.mjs`, which
  inlines the pinned local `redoc` bundle so the file opens fully offline.
- Regenerate all three with `Backend/scripts/check-api-contract`; it fails when committed
  output is stale. Vercel builds rely on the committed output (no dotnet in that path).
- Feature `models/*.models.ts` files re-export the generated wire DTOs (aliased to the
  historical local names) and keep UI-only unions, request params, and view models
  hand-written; closed backend vocabularies the spec types as `string` are narrowed there
  via documented `Omit`-overlays.

## Testing (read before running tests)

- **Keep the `VITEST_MAX_FORKS` cap on `npm test`** — without it the run OOMs/freezes the
  machine. **`vitest.config.ts` is ignored by the Angular unit-test builder**, so the cap
  must be set the way `package.json` already sets it; do not "clean it up".
- **jsdom lacks `matchMedia` / `ResizeObserver`** under the builder — guard them in
  components and default to desktop.
- **Browser E2E (opt-in):** `npm run e2e` (headless), `npm run e2e:headed`, `npm run e2e:ui`.
  Chromium only. It boots the Angular dev server *and* the backend `https` profile, so it needs
  mkcert certificates, a migrated local `quran_dashboard`, and a prior
  `dotnet build Backend/QuranDashboard.sln`. Specs live in `e2e/` and MUST be named `*.e2e.ts` —
  a `*.spec.ts` there would be collected by the Vitest builder. See `e2e/README.md`.

## Invariants

- **Mushaf font is Amiri**, not `UthmanicHafs_V22` (which mis-renders U+06DF) — see
  `src/app/features/mushaf/README.md`.
- **Word identity is clean imlaei-simple** (display Uthmani) — mirrors the backend.
- URL-state in explorers/reader is a shareable contract; keep params stable.
