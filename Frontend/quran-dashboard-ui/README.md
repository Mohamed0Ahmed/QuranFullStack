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

## Testing (read before running tests)

- **Keep the `VITEST_MAX_FORKS` cap on `npm test`** — without it the run OOMs/freezes the
  machine. **`vitest.config.ts` is ignored by the Angular unit-test builder**, so the cap
  must be set the way `package.json` already sets it; do not "clean it up".
- **jsdom lacks `matchMedia` / `ResizeObserver`** under the builder — guard them in
  components and default to desktop.

## Invariants

- **Mushaf font is Amiri**, not `UthmanicHafs_V22` (which mis-renders U+06DF) — see
  `src/app/features/mushaf/README.md`.
- **Word identity is clean imlaei-simple** (display Uthmani) — mirrors the backend.
- URL-state in explorers/reader is a shareable contract; keep params stable.
