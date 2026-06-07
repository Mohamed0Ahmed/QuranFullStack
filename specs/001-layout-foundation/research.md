# Phase 0 Research: Layout & Foundation

All spec ambiguities were resolved in `spec.md` (Locked Decisions + Clarifications session
2026-06-07), so there are **no open `NEEDS CLARIFICATION` items**. This document records the
chosen approaches, why, and the alternatives rejected — plus best-practice notes the
implementer needs.

---

## R1. Backend `ApiResponse` envelope alignment

- **Decision**: Change `Contracts/ApiResponse.cs` to `{ IsSuccess, Message, Data, Errors }` with
  factories `Ok(data, message?)` and `Fail(message, errors?)`. Property names English; serialized
  camelCase → `isSuccess/message/data/errors`.
- **Rationale**: Matches the canonical `API_GUIDELINES.md` §5 shape; the frontend builds one
  `ApiResponse<T>` type against it. Resolves the existing code/doc contradiction (current code is
  `{Success,Data,Message}`, no errors).
- **Alternatives rejected**: (a) Update the doc to match the old code — rejected: the doc is
  canonical and the failure shape with `errors` is needed. (b) Leave as-is — rejected: frontend
  would be built against an inconsistent contract.

## R2. Global exception handler → envelope (not ProblemDetails)

- **Decision**: `GlobalExceptionHandler` returns the `ApiResponse` **failure** envelope
  (`isSuccess=false`, Arabic safe message, `errors: []`) with HTTP 500 and
  `Content-Type: application/json`. Keep logging the full exception server-side.
- **Rationale**: One consistent contract for clients (`API_GUIDELINES.md` §5–§6); never leak
  stack/SQL/paths.
- **Alternatives rejected**: Keep RFC7807 `ProblemDetails` — rejected: two response shapes
  complicate the frontend; the doc prefers the envelope. `AddProblemDetails()` may remain
  registered but is no longer the shape our handler emits.

## R3. Database health check

- **Decision**: Add package `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`;
  register `services.AddHealthChecks().AddDbContextCheck<QuranDashboardDbContext>("database")`.
  `HealthController` injects `HealthCheckService`, runs `CheckHealthAsync()`, and maps the result
  into the `ApiResponse` data: `{ status: "healthy|unhealthy", checks: [{ name, status }] }`.
- **Rationale**: `AddDbContextCheck` verifies the DB is reachable via the existing context with no
  custom SQL. Surfacing through the controller keeps the single envelope and lets us hide details.
- **Alternatives rejected**: (a) `AddNpgSql(connectionString)` health check — rejected: would
  re-handle the connection string; `AddDbContextCheck` reuses the registered context. (b) Map the
  built-in `/health` endpoint via `MapHealthChecks` — rejected: that returns a non-envelope body;
  we keep `/api/health` returning the envelope. Do **not** expose connection details in output.

## R4. App metadata (name / version / environment)

- **Decision**: `DashboardController.GetInfo` returns `{ appName: "المنهج القرآني", version,
  environment }` inside the envelope with an Arabic success message. `version` = the assembly
  informational version (fallback constant `"0.1.0"` if unavailable); `environment` =
  `IHostEnvironment.EnvironmentName`. Inject `IHostEnvironment` (and read version via reflection
  on the entry assembly).
- **Rationale**: Real, non-fabricated values sourced from the running app (FR-026, FR-030).
- **Alternatives rejected**: Hardcode all three — rejected: environment/version must reflect
  reality. A config-only version is acceptable but assembly version is the simplest single source.

## R5. Arabic-default messages — minimal centralization

- **Decision**: Add `Common/ApiMessages.cs` (static class with `const string` Arabic messages,
  e.g. `HealthOk`, `DashboardInfo`, `UnexpectedError`). Controllers/handler reference these.
- **Rationale**: Satisfies "don't scatter hardcoded user-facing strings" (`API_GUIDELINES.md`
  §10) without building a full localization/resource system (explicitly deferred — YAGNI).
- **Alternatives rejected**: Full `IStringLocalizer`/resx localization — rejected: out of scope
  this phase; English-later support is preserved by centralizing now.

## R6. DB credentials out of source control

- **Decision**: In `appsettings.json` / `appsettings.Development.json`, replace the password with
  an empty/placeholder (keep `Host/Port/Database/Username`). Real password supplied via
  `dotnet user-secrets` (Development) or environment variable
  `ConnectionStrings__QuranDashboardDb`. Document in a short README note.
- **Rationale**: FR-031 / plan rule "no secrets in source control"; .NET config providers layer
  user-secrets/env over appsettings automatically — no code change beyond the existing
  `GetConnectionString("QuranDashboardDb")`.
- **Alternatives rejected**: Leave `Password=postgres` committed — rejected by product owner.

## R7. Angular bootstrap & HttpClient

- **Decision**: Add `provideHttpClient(withFetch())` to `app.config.ts` providers. Keep existing
  `provideRouter`, zone change detection.
- **Rationale**: Standalone Angular 20 idiom; `withFetch` is the modern transport. Needed for
  `system.api.ts` (FR-029).
- **Alternatives rejected**: `HttpClientModule` import — rejected: deprecated for standalone apps.

## R8. RTL / Arabic-first setup

- **Decision**: `index.html` → `<html lang="ar" dir="rtl">`, `<title>المنهج القرآني</title>`.
  Styles use logical CSS properties (`margin-inline-*`, `padding-inline`, `inset-inline-*`,
  `border-inline-*`); avoid hardcoded `left/right`.
- **Rationale**: FR-003 (no LTR flash → set on the root element, not at runtime). `DESIGN.md`
  Genuinely-RTL rule; `UI_STYLE_SYSTEM.md` §8.
- **Alternatives rejected**: Setting `dir` at runtime in JS — rejected: causes an LTR flash.

## R9. Self-hosting fonts (Amiri + IBM Plex Sans Arabic)

- **Decision**: Ship `woff2` files under `public/fonts/` and declare `@font-face` in
  `_typography.scss` with `font-display: swap` and `unicode-range` for Arabic where helpful.
  Amiri → content/headings (naskh); IBM Plex Sans Arabic → UI chrome. Both are SIL OFL 1.1
  licensed (redistribution allowed). Include at least regular + bold weights per family.
- **Rationale**: FR-022 (no external runtime fetch; offline-capable); `public/` is served at the
  web root by the Angular builder.
- **Alternatives rejected**: Google Fonts `<link>` — rejected: external runtime dependency,
  conflicts with calm/offline intent. `@fontsource` npm packages are acceptable but self-hosting
  `woff2` in `public/fonts/` is the simplest, dependency-free path.

## R10. Theme system (light/dark via `data-theme`)

- **Decision**: Tokens are CSS variables in `:root`/`_tokens.scss`; `_themes.scss` overrides them
  under `[data-theme="light"]` and `[data-theme="dark"]`. `ThemeService` (in `core/theme/`) sets
  `document.documentElement.dataset.theme`, persists the choice to `localStorage` (key
  `qd-theme`), and on init applies: stored value → else `prefers-color-scheme: dark` → else
  `light`. To avoid a flash, apply the initial theme as early as possible (a tiny inline script in
  `index.html` `<head>` that reads `localStorage`/`matchMedia` and sets `data-theme` before first
  paint is acceptable and recommended). The navbar toggle is **binary** (light ↔ dark).
- **Rationale**: FR-014–FR-018; `UI_STYLE_SYSTEM.md` §5 (single root attribute; components never
  branch on theme). Binary toggle per Clarification Q3.
- **Alternatives rejected**: Per-component dark styles — rejected (§5). Tri-state toggle —
  rejected by Clarification Q3.

## R11. `qd-*` style system organization & Tailwind coexistence

- **Decision**: `styles.scss` keeps the three `@tailwind` layers and imports the 7 partials.
  Reusable visual patterns are `qd-*` classes built **on tokens**; Tailwind utilities are allowed
  for simple one-off layout/spacing only, never to bypass tokens for color/repeated patterns.
- **Rationale**: `UI_STYLE_SYSTEM.md` §1–§3, §6. Keeps a single source of truth.
- **Alternatives rejected**: Tailwind-only (no `qd-*`) — rejected (§6: Tailwind supports, not
  replaces, the system).

## R12. Palette (parchment & ink) — values finalized in implementation

- **Decision**: Use the `DESIGN.md` "parchment & ink + one muted accent" direction. Define the
  full token set now with **warm-tinted** OKLCH values (no pure `#fff`/`#000`); pick concrete
  values during implementation and review them running. Light = warm parchment surfaces + deep
  ink text; dark = warm deep ink surfaces + soft parchment text; one low-chroma accent used
  sparingly (One Voice Rule, ≤10% of a screen).
- **Rationale**: Locked decision (palette direction now, values in implementation); `DESIGN.md`
  §2, Warm Neutral + One Voice rules.
- **Alternatives rejected**: Pin exact hex in the spec — rejected by product owner; better judged
  in the running UI.

## R13. Navigation config, routeable placeholder, wildcard redirect

- **Decision**: A single `NavItem[]` array in `core/navigation/nav-items.ts`
  (`{ key, labelAr, labelEn, route, group }`, `group ∈ primary|more|actions`) drives the navbar.
  Routes in `app.routes.ts`: `''` redirects to `dashboard`; `dashboard` → home page; all other
  section routes → the shared `PlaceholderPageComponent` with route `data` (titleAr); `**`
  (wildcard) redirects to `dashboard` (Clarification Q1). Active state via `routerLinkActive`.
- **Rationale**: `FRONTEND_STRUCTURE.md` nav-item shape, stable route keys, routeable vs child,
  URL state on refresh/back-forward (FR-008–FR-012, FR-036).
- **Alternatives rejected**: Hardcoding nav inside the navbar component — rejected (§Route
  Structure Guidance). A not-found page — rejected by Clarification Q1.

## R14. API integration pattern

- **Decision**: `api-response.model.ts` defines `ApiResponse<T>`; `system.models.ts` defines
  `AppInfo` and `HealthStatus`; `system.api.ts` (injectable, `providedIn: 'root'`) exposes
  `getDashboardInfo(): Observable<AppInfo>` and `getHealth(): Observable<HealthStatus>`, each
  unwrapping the envelope and using `environment.apiBaseUrl`. Home + footer call them **once on
  load**, render explicit loading → success/error states, with a manual retry on error
  (Clarification Q2). No fabricated values on error (FR-030).
- **Rationale**: `API_INTEGRATION_GUIDELINES.md` (data-access vs state; explicit states; typed
  envelope). This phase has no feature state, so a small data-access service is sufficient (no
  facade/store yet — YAGNI).
- **Alternatives rejected**: Background polling — rejected by Clarification Q2. A full
  facade/store — rejected: no shared page state this phase.

## R15. Environments for API base URL

- **Decision**: Add `src/environments/environment.ts` (placeholder/prod) and
  `environment.development.ts` (`apiBaseUrl: 'http://localhost:5014'`), wired via the Angular
  build `fileReplacements` for the development configuration.
- **Rationale**: Keeps the API origin out of code; CORS already allows `:4200` over http/https.
  Using `http://localhost:5014` (the `http` launch profile) avoids dev TLS friction.
- **Alternatives rejected**: Hardcode the URL in the service — rejected: not configurable per
  environment.
