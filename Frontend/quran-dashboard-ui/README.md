# Quran Dashboard UI

Angular 20 (standalone components + Signals) frontend for the Quran Dashboard
(المنهج القرآني) — an **Arabic-first (RTL)**, scholarly/calm admin dashboard.

> HOW to work here: use `.architecture/FRONTEND_STRUCTURE.md` for code organization and
> `.architecture/API_INTEGRATION_GUIDELINES.md` for API work. `../../PRODUCT.md` owns users and
> product purpose. During the UI rebuild, the owner's explicit direction controls visual work and
> no permanent visual rule set is active. This file owns only operational run/build,
> generated-contract, and verification guidance. Active Spec Kit artifacts own feature intent and
> code owns implemented truth.

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

## Verification

`../../TESTING_CONSTITUTION.md` is the policy. The normal frontend verification chain is three
independent commands, in this order:

```bash
npm run check:no-unit-specs
npm run typecheck:app
npm run build:verify
```

Keep `check:no-unit-specs` independent; never fold it into `build:verify`. `npm run typecheck` is an
alias for `typecheck:app`. `npm run test:pre-pr` runs the permission catalogue, audit action type,
no-unit-specs, typecheck, and production-build checks in sequence.

New automated tests are frozen by default. The source tree must contain no Angular unit specs.
Playwright is the retained frontend test estate, and a new journey still requires owner approval
under the constitution. Commands are `npm run e2e` (headless), `npm run e2e:headed`, and
`npm run e2e:ui`; Chromium only. Playwright owns the Angular dev server, a backend in the `Testing`
environment, a local Management API stub, and a disposable clone of the local source database. The
source database supplies the clone and is never the E2E write target. The run needs mkcert
certificates and a prior `dotnet build Backend/QuranDashboard.sln`. Journey files live in `e2e/`
and must be named `*.e2e.ts`; see `e2e/README.md` for the six retained journeys and runtime
invariants.
