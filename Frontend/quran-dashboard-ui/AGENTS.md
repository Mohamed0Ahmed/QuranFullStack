# Frontend Agent Guide

## UI Style System

Before creating or changing global styles, theme tokens, reusable UI classes,
layout shell styles, component visual styles, dark/light theme behavior, or shared
UI patterns, read and follow:

- `.architecture/UI_STYLE_SYSTEM.md`

## Frontend Structure

Before adding or changing Angular components, routeable smart/page components,
child/presentational components, services, routes, tabs with URL state,
state/facade/store files, data-access files, or frontend feature organization,
read and follow:

- `.architecture/FRONTEND_STRUCTURE.md`

## Frontend Test Selection

Read `../../TESTING_STRATEGY.md` and `testing/README.md`, inspect the changed scope, then
use the `npm run test:*` commands. Start with one spec or the narrowest fast, feature, or
authorization lane. The full Frontend suite and production build run once at
engineering-review/pre-PR boundaries when Frontend files changed; Backend-only work with
no generated/frontend contract diff requires no Frontend test.

Preserve the two-fork Vitest cap and configured timeouts. Keep output visible, never pipe
to `tail`, and report the exact lane, command, reason, result, and skips. The formal
reviewer owns the final full Frontend gate; there is no CI fallback. Deleting a test requires documented
obsolete/redundant proof and named replacement coverage.

A browser E2E layer exists: Playwright (chromium only) at `playwright.config.ts` + `e2e/`,
run with `npm run e2e`. It is opt-in and is NOT a required gate — never cite it in place of
the Vitest lanes or a build, and never let an E2E run substitute for pre-PR evidence. Specs
are named `*.e2e.ts`. That is not cosmetic: the Vitest gate globs with `cwd` at `src/` and cannot
see `e2e/` at all, and `playwright.config.ts` matches only `/.*\.e2e\.ts$/` — so a `*.spec.ts`
placed there is run by nothing while looking like coverage.

## Frontend Local READMEs

- Before touching a frontend feature, read the nearest `README.md`
  (`src/app/features/words/README.md`, `src/app/features/mushaf/README.md`,
  `src/app/core/README.md`) before the `.architecture/*` HOW docs; use `docs/contracts/frontend-shell.md` / `words-explorers.md` / `mushaf-reader.md` to find the authoritative README/code.
- If you change routes, URL-state contracts, facade/cache patterns, render/font
  invariants, or the test-command rules a README documents, update that README in the
  same change.
- Do not create standalone frontend feature reports by default; reserve reports for
  audits, UX contracts, diagnostics, and acceptance evidence.

## Frontend Comment Policy

The canonical rule is *Comments are forbidden by default* in the root `CLAUDE.md`; read it
first. Only the Angular-specific detail lives here.

- **Scope:** `.ts`, `.html` and `.scss` under `src/`. Templates and stylesheets are production
  code and are included. Not `*.spec.ts`, not `e2e/`, not build config.
- **No JSDoc (`/** */`) narrating a component, service, facade, store or directive.** A block
  comment above a class is the default failure mode here and is forbidden outright.
- **No step-narrating `//` in `.ts`, `<!-- -->` in templates, or `//` section banners in SCSS.**
  A stylesheet that needs banners to be navigable needs splitting, not labelling.
- `// eslint-disable-*`, `// @ts-ignore`, `// prettier-ignore` and `/*! … */` in SCSS are
  directives, not comments. Never remove them.
- Feature behaviour, URL contracts and component boundaries belong in the feature's
  `README.md`, and the token/component vocabulary in `.architecture/UI_STYLE_SYSTEM.md`.

## API Integration

Before adding or changing frontend API services, data-access files, facade/store
services that call APIs, `ApiResponse<T>` handling, API-backed loading/error/empty
states, DTO/view model/state model mapping, or pagination/filter/search API
integration, read and follow:

- `.architecture/API_INTEGRATION_GUIDELINES.md`

For product and visual context, also read:

- `../../PRODUCT.md`
- `../../DESIGN.md`
