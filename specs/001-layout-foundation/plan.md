# Implementation Plan: Dashboard Layout & Foundation (Phase 0)

**Branch**: `001-layout-foundation` | **Date**: 2026-06-07 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-layout-foundation/spec.md`

> **Implementer note**: This plan is written for a lower-capability implementation model.
> Decisions are concrete and the exact files to touch are listed. Do not introduce new
> libraries, patterns, or files beyond what is described here without a stated reason.

## Summary

Deliver the Arabic-first (RTL) application foundation for «المنهج القرآني»: a calm top-navbar
app shell (navbar + content outlet + footer, **no global sidebar**), a centralized `qd-*` style
system with light/dark themes and self-hosted Arabic fonts, wired navbar routes with one shared
placeholder page and a real home page, and a proven end-to-end API integration (the frontend
reads real app metadata + health from the backend, fetched once on load with manual retry). On
the backend, apply light polish: align the `ApiResponse` envelope and the global exception
handler to `API_GUIDELINES.md`, add app metadata + a database health check, centralize a few
Arabic-default messages, and remove the committed DB password. No Quran feature data.

## Technical Context

**Language/Version**: Backend C# / .NET 10 (EF Core 10, Npgsql 10). Frontend TypeScript ~5.9,
Angular 20.3 (standalone components, zone-based change detection), SCSS, Tailwind CSS v3.4.
**Primary Dependencies**: Backend — ASP.NET Core, EF Core 10, `Npgsql.EntityFrameworkCore.PostgreSQL` 10,
Swashbuckle 10; **add** `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` (10.x).
Frontend — `@angular/router`, `@angular/common/http` (HttpClient), RxJS 7.8, Tailwind v3; **add**
self-hosted fonts **Amiri** (naskh) + **IBM Plex Sans Arabic** (UI).
**Storage**: PostgreSQL (already configured; connection name `QuranDashboardDb`). Schema stays
**empty** this phase — no entities, no migrations.
**Testing**: No automated test projects exist and none are required by the spec. Verification is
build-based (`dotnet build`, `npm run build`) plus the manual/observable checks in `quickstart.md`.
**Target Platform**: Modern evergreen browsers (frontend dev server at `http://localhost:4200`);
backend API at `http://localhost:5014` (https `5015`).
**Project Type**: Web application — Angular frontend + .NET Clean Architecture backend (two
repos tracked as submodules inside the workspace repo).
**Performance Goals**: Calm, responsive UI; no perceptible jank on theme switch / menu open; no
console errors on load or navigation; frontend build stays within Angular default budgets;
no left-to-right flash on first paint.
**Constraints**: Arabic-first RTL by default; fonts self-hosted (no external runtime font fetch);
WCAG 2.1 AA contrast in both themes; meaning never by color alone; respect reduced-motion;
no fabricated Quranic/religious data; no real DB credentials in source control; backend layering
per `CLEAN_ARCHITECTURE.md`; visual styling only via the `qd-*` system + tokens.
**Scale/Scope**: Frontend — 1 real page (home) + 1 shared placeholder serving 10 routes +
shell (navbar/footer) + theme service + nav config + style system (7 partials) + API layer
(3 small files). Backend — 2 endpoints polished, 1 envelope type, 1 exception handler, 1 message
constants holder, 1 health check, config/secrets change. Single locale (Arabic).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is an **unratified template** (placeholder content), so there
are no formal numbered articles to gate against. In its place, this plan treats the project's
canonical docs as the operative gates. All are satisfied by design:

| Gate (source) | Requirement | Status |
|---|---|---|
| `CLEAN_ARCHITECTURE.md` | Api references Infrastructure only for composition root; no business logic / EF in controllers | PASS — only Api-layer files touched; controllers stay thin, call `HealthCheckService`; no EF in controllers |
| `BACKEND_STRUCTURE.md` | Feature/domain grouping; no global dumping folders; file-size thresholds | PASS — small additions (`Common/ApiMessages.cs`) near the API feature; no new dumping folders |
| `API_GUIDELINES.md` | Consistent `ApiResponse` envelope; Arabic-default messages; centralized errors; safe health checks | PASS — envelope aligned; messages centralized; handler returns envelope; health hides details |
| `FRONTEND_STRUCTURE.md` | Feature-first; `core/` for app-wide singletons; routeable vs child components; separate `.html`/`.scss`; file-size thresholds | PASS — `core/layout`, `core/theme`, `core/navigation`, `core/data-access`; `features/dashboard`; `shared/ui/placeholder-page`; all small |
| `UI_STYLE_SYSTEM.md` | Centralized `qd-*` classes + CSS-variable tokens; `data-theme` theming; RTL logical properties; warm neutrals | PASS — `src/styles/` partials, `--qd-*` tokens, `[data-theme]`, logical CSS |
| `API_INTEGRATION_GUIDELINES.md` | Typed `ApiResponse<T>`; data-access vs state; explicit loading/error/empty | PASS — `system.api.ts` + typed models; explicit states on home + footer |
| `CODING_PRINCIPLES.md` | Clean Code, SOLID, DRY/KISS/YAGNI, strong typing, focused scope, error handling, Quranic data safety | PASS — no feature creep; strong types; no fabricated data |

**Result**: No violations. Complexity Tracking left empty.

## Project Structure

### Documentation (this feature)

```text
specs/001-layout-foundation/
├── plan.md              # This file
├── research.md          # Phase 0 output — decisions & best practices
├── data-model.md        # Phase 1 output — entities & token model
├── quickstart.md        # Phase 1 output — run & verify steps
├── contracts/           # Phase 1 output — API + UI contracts
│   ├── api-response-envelope.md
│   ├── api-health.md
│   ├── api-dashboard-info.md
│   ├── ui-navigation.md
│   └── ui-design-tokens.md
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

**Backend — only the Api project is touched** (`Backend/api/QuranDashboard.Api/`):

```text
Backend/api/QuranDashboard.Api/
├── Contracts/ApiResponse.cs                 # MODIFY: IsSuccess, Errors, Ok() + Fail()
├── Common/ApiMessages.cs                     # ADD: Arabic-default message constants
├── Controllers/HealthController.cs           # MODIFY: use HealthCheckService (overall + db)
├── Controllers/DashboardController.cs        # MODIFY: appName/version/environment + Arabic msg
├── Middleware/GlobalExceptionHandler.cs      # MODIFY: write ApiResponse failure (not ProblemDetails)
├── Extensions/ServiceCollectionExtensions.cs # MODIFY: AddHealthChecks().AddDbContextCheck; Swagger title
├── appsettings.json                          # MODIFY: remove committed password (placeholder)
├── appsettings.Development.json              # MODIFY: remove committed password (placeholder)
└── QuranDashboard.Api.csproj                 # MODIFY: add HealthChecks.EntityFrameworkCore pkg
```

(A short README note documents the user-secrets/env setup. `Domain`, `Application`,
`Infrastructure`, `Shared` are NOT changed; `QuranDashboardDbContext` stays empty; no migrations.)

**Frontend** (`Frontend/quran-dashboard-ui/`):

```text
Frontend/quran-dashboard-ui/
├── public/fonts/                             # ADD: amiri-*.woff2, ibm-plex-sans-arabic-*.woff2
├── src/
│   ├── index.html                            # MODIFY: lang="ar" dir="rtl", title «المنهج القرآني»
│   ├── styles.scss                           # MODIFY: @use partials + Tailwind layers
│   ├── styles/                               # ADD partials
│   │   ├── _tokens.scss                       #   base + light token values (--qd-*)
│   │   ├── _themes.scss                       #   [data-theme="light"|"dark"] token overrides
│   │   ├── _typography.scss                   #   @font-face (self-host) + qd text classes
│   │   ├── _layout.scss                       #   qd-shell, qd-navbar, qd-container, qd-footer
│   │   ├── _components.scss                   #   qd-card, qd-btn*, qd-badge, *-state
│   │   ├── _forms.scss                        #   qd-input (basic)
│   │   └── _utilities.scss                    #   small helpers
│   ├── environments/
│   │   ├── environment.ts                     # ADD: apiBaseUrl prod placeholder
│   │   └── environment.development.ts         # ADD: apiBaseUrl "http://localhost:5014"
│   └── app/
│       ├── app.ts                            # MODIFY: render <qd-app-shell>
│       ├── app.config.ts                     # MODIFY: provideHttpClient(withFetch())
│       ├── app.routes.ts                     # MODIFY: routes + wildcard redirect to /dashboard
│       ├── core/
│       │   ├── layout/
│       │   │   ├── app-shell/                 # navbar + <router-outlet> + footer
│       │   │   ├── top-navbar/                # brand, primary nav, «المزيد», theme toggle, actions, mobile menu
│       │   │   └── footer/                    # brand line, version/env (dev), live status
│       │   ├── theme/theme.service.ts         # toggle data-theme, persist localStorage, OS default
│       │   ├── navigation/nav-items.ts        # NavItem[] config (key/labelAr/labelEn/route/group)
│       │   └── data-access/
│       │       ├── api-response.model.ts      # ApiResponse<T> type
│       │       ├── system.models.ts           # AppInfo, HealthStatus types
│       │       └── system.api.ts              # getDashboardInfo(), getHealth()
│       ├── shared/ui/placeholder-page/        # single shared placeholder (reads route data)
│       └── features/dashboard/pages/dashboard-home/  # real home page (welcome + 5 cards + metadata)
```

**Structure Decision**: Web application reusing the **existing** layouts. Backend changes are
confined to the **Api** layer (composition root + thin controllers + contracts), honoring
Clean Architecture. Frontend follows `FRONTEND_STRUCTURE.md`: app-wide singletons (shell,
theme, navigation, data-access) live under `core/`; the one reusable placeholder lives under
`shared/ui/`; the home page lives under `features/dashboard/`. No empty folders are created in
advance; each folder above is created only with its real files.

## Complexity Tracking

> No constitution violations. Section intentionally empty.
