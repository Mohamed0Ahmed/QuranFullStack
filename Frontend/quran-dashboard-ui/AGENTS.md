# Frontend Instructions

## Architecture invariants

Keep the Angular application feature-first. Routeable pages are stable URL destinations; meaningful
main-content tabs survive refresh through routes or query parameters. Components render and coordinate,
feature state orchestrates, and feature data-access clients own HTTP calls. Reserve `core/` for app-wide
concerns and `shared/` for genuinely cross-feature building blocks.

Read `.architecture/FRONTEND_STRUCTURE.md` before placing or moving components, routes, state,
data-access code, or shared UI. It is canonical for page/component boundaries and URL-state decisions.

Arabic-first RTL behavior is the default. Preserve Quranic source fidelity: represent missing data as a
controlled state instead of inventing Quranic text or labels.

## Route by task

- **Responsive layout:** treat `src/app/shared/layout/breakpoints.contract.json` as the breakpoint source
  of truth. Its consumers are `src/app/shared/layout/breakpoints.ts`, `tailwind.config.js`, and
  `src/styles/_breakpoints.scss`.
- **API client or wire model:** read `README.md` section `Generated API contract` and
  `ng-openapi-gen.json`. `openapi/swagger.json` and `src/app/core/api/generated/` are committed generated
  outputs; feature-owned clients and UI models remain hand-written outside that generated directory.
- **Backend contract impact:** when a UI change requires an endpoint, payload, route,
  authentication/authorization behavior, or permission vocabulary to change, read
  `../../Backend/AGENTS.md`. Regenerate and verify the contract through
  `../../Backend/scripts/check-api-contract` rather than editing generated files.
- **Build or local run:** use `README.md` for the supported operational path. Use `package.json` scripts
  as command truth and `angular.json` for build, serve, assets, and schematic configuration.
- **Browser behavior:** use `playwright.config.ts` and the `package.json` E2E scripts. Keep tests aligned
  with the repository's configured test strategy rather than adding a parallel test convention.

## Manifests and verification

`package.json` and `package-lock.json` define direct and resolved dependencies; `tsconfig*.json` files
define TypeScript compilation scopes. Run the smallest relevant checks from `package.json`; use its
aggregate pre-PR gate when the change spans multiple Frontend concerns.
