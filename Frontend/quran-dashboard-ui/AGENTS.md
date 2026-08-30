# Frontend Structure

`Frontend/quran-dashboard-ui/` is an Angular 20 standalone application. `src/main.ts` bootstraps the
app, `src/app/app.config.ts` owns application providers, and `src/app/app.routes.ts` owns root routes.

## Project layout

- `src/app/core/` contains app-wide auth, navigation, layout, caching, environment-facing data
  access, and generated API models.
- `src/app/features/` is feature-first. Current feature areas include Dashboard, Mushaf, Words,
  Abwab, Linking, Access Admin, and Auth.
- Feature folders use the existing `pages/`, `components/`, `data-access/`, `state/`, `models/`,
  and `utils/` divisions when those responsibilities exist.
- `src/app/shared/` contains reusable UI, layout, navigation, Quran presentation, and URL helpers.
- Root routes lazy-load feature routes or standalone page components; larger features keep their
  route definitions in `<feature>.routes.ts`.
- For placement decisions, use `.architecture/FRONTEND_STRUCTURE.md`.

## API model boundary

- `openapi/swagger.json` is exported from the Backend by `Backend/scripts/export-swagger`.
- `src/app/core/api/generated/` is generated from that spec and pruned to models only by
  `npm run generate:api`.
- Feature-owned API clients remain hand-written under the feature's `data-access/` folder and use
  the generated wire models.
- `src/app/core/data-access/api-response.model.ts` is the hand-written response envelope model.
