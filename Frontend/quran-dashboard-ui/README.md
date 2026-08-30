# Quran Dashboard UI

Angular 20 (standalone components + Signals) frontend for the Quran Dashboard
(المنهج القرآني) — an **Arabic-first (RTL)**, scholarly/calm admin dashboard.

> This file documents operational run/build commands and generated API artifacts.

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
