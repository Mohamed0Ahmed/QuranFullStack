# Frontend Project Instructions

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

## Frontend Local READMEs

- Before touching a frontend feature, read the nearest `README.md`
  (`src/app/features/words/README.md`, `src/app/features/mushaf/README.md`,
  `src/app/core/README.md`) before the `.architecture/*` HOW docs.
- If you change routes, URL-state contracts, facade/cache patterns, render/font
  invariants, or the test-command rules a README documents, update that README in the
  same change.
- Do not create standalone frontend feature reports by default; reserve reports for
  audits, UX contracts, diagnostics, and acceptance evidence.

## API Integration

Before adding or changing frontend API services, data-access files, facade/store
services that call APIs, `ApiResponse<T>` handling, API-backed loading/error/empty
states, DTO/view model/state model mapping, or pagination/filter/search API
integration, read and follow:

- `.architecture/API_INTEGRATION_GUIDELINES.md`

For product and visual context, also read:

- `../../PRODUCT.md`
- `../../DESIGN.md`
