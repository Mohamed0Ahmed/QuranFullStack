---
description: "Task list for Dashboard Layout & Foundation (Phase 0)"
---

# Tasks: Dashboard Layout & Foundation (Phase 0)

**Input**: Design documents from `/specs/001-layout-foundation/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Per the spec Assumptions, **no automated test suite is required this phase**. No test
projects exist; verification is build-based (`dotnet build`, `npm run build`) plus the manual
checklist in `quickstart.md`. No test tasks are generated. Do not add a test framework.

**Organization**: Tasks are grouped by user story (US1–US5 from spec.md) so each story is an
independently testable increment.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1–US5 (omitted for Setup / Foundational / Polish)
- Every task lists an exact file path.

## Path Conventions

- Backend (Api project only): `Backend/api/QuranDashboard.Api/...`
- Frontend (Angular): `Frontend/quran-dashboard-ui/...`

> **Execution note on priority vs. dependency**: User stories are listed in spec priority order
> P1→P5, **except** US4 (P4, frontend live data) is placed *after* US5 (P5, backend boundary)
> because US4 consumes the US5 endpoints. By user value US4 outranks US5; by build dependency it
> follows it. The MVP is **US1 only**.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Install dependencies and wire configuration needed by later phases.

- [x] T001 Add NuGet package `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` (10.x) to `Backend/api/QuranDashboard.Api/QuranDashboard.Api.csproj`
- [x] T002 [P] Add self-hosted font files (regular + bold `woff2`) for **Amiri** and **IBM Plex Sans Arabic** under `Frontend/quran-dashboard-ui/public/fonts/`
- [x] T003 [P] Add `Frontend/quran-dashboard-ui/src/environments/environment.ts` (prod placeholder) and `environment.development.ts` (`apiBaseUrl: 'http://localhost:5014'`), and register the dev `fileReplacements` in `Frontend/quran-dashboard-ui/angular.json`
- [x] T004 [P] Add `provideHttpClient(withFetch())` to the providers in `Frontend/quran-dashboard-ui/src/app/app.config.ts`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: RTL bootstrap + the centralized `qd-*` style system. **Blocks all UI stories.**

**⚠️ CRITICAL**: No user-story UI work can begin until this phase is complete.

- [x] T005 [P] Set RTL/Arabic + branding in `Frontend/quran-dashboard-ui/src/index.html`: `<html lang="ar" dir="rtl">` and `<title>المنهج القرآني</title>`
- [x] T006 [P] Create `Frontend/quran-dashboard-ui/src/styles/_tokens.scss` defining all `--qd-*` tokens in `:root` with light "parchment" values (warm-tinted OKLCH; no pure `#fff`/`#000`) per `contracts/ui-design-tokens.md`
- [x] T007 [P] Create `Frontend/quran-dashboard-ui/src/styles/_typography.scss` with `@font-face` for self-hosted Amiri (content/headings) + IBM Plex Sans Arabic (UI), `font-display: swap`, and text classes `qd-page-title`, `qd-section-title`, `qd-card-title`, `qd-text`, `qd-text-muted`, `qd-text-meta`
- [x] T008 [P] Create `Frontend/quran-dashboard-ui/src/styles/_layout.scss` with layout primitives `qd-shell`, `qd-navbar`, `qd-container`, `qd-footer` (logical CSS properties, flat/hairline per DESIGN.md)
- [x] T009 [P] Create `Frontend/quran-dashboard-ui/src/styles/_components.scss` with `qd-card`, `qd-btn`/`qd-btn-primary`/`qd-btn-secondary`/`qd-btn-ghost`, `qd-badge`, `qd-empty-state`, `qd-loading-state`, `qd-error-state`
- [x] T010 [P] Create `Frontend/quran-dashboard-ui/src/styles/_forms.scss` (`qd-input`) and `Frontend/quran-dashboard-ui/src/styles/_utilities.scss` (small helpers)
- [x] T011 Create `Frontend/quran-dashboard-ui/src/styles/_themes.scss` with `[data-theme="dark"]` token overrides (warm "ink"; light is the `:root` default from T006) (depends on T006)
- [x] T012 Update `Frontend/quran-dashboard-ui/src/styles.scss` to keep the three `@tailwind` layers and `@use`/import all partials from `src/styles/` (depends on T006–T011)

**Checkpoint**: App renders RTL in the light theme; `qd-*` classes and tokens are available.

---

## Phase 3: User Story 1 - Calm Arabic-first app shell (Priority: P1) 🎯 MVP

**Goal**: A calm RTL shell — top navbar (brand «المنهج القرآني»), main content area, footer — with a real home page rendering in the content area. No global sidebar.

**Independent Test**: Launch the app; confirm RTL Arabic UI, the brand wordmark in the navbar, the three shell regions (navbar/content/footer), and the home page (welcome + description + 5 cards) — with no console errors and no horizontal scroll.

- [x] T013 [P] [US1] Create the footer component in `Frontend/quran-dashboard-ui/src/app/core/layout/footer/` (`.ts`/`.html`/`.scss`): brand line «المنهج القرآني © 2026» + short Arabic description; leave slots for version/environment + status (filled in US4), using `qd-footer`
- [x] T014 [P] [US1] Create the top-navbar component in `Frontend/quran-dashboard-ui/src/app/core/layout/top-navbar/`: brand wordmark «المنهج القرآني» (naskh), a user/actions area, and a mobile menu button, using `qd-navbar` (nav links added in US2, theme toggle in US3)
- [x] T015 [US1] Create the app-shell component in `Frontend/quran-dashboard-ui/src/app/core/layout/app-shell/` composing top-navbar + `<router-outlet>` + footer with `qd-shell` (depends on T013, T014)
- [x] T016 [US1] Update `Frontend/quran-dashboard-ui/src/app/app.ts` to render `<qd-app-shell>` (selector from T015) (depends on T015)
- [x] T017 [P] [US1] Create the home page in `Frontend/quran-dashboard-ui/src/app/features/dashboard/pages/dashboard-home/`: `qd-page` with welcome heading, short description, and exactly 5 `qd-card` overview cards linking to `/mushaf`, `/words`, `/tafsirs`, `/gates`, `/resources` (no fabricated stats)
- [x] T018 [US1] Update `Frontend/quran-dashboard-ui/src/app/app.routes.ts`: `''` → redirect to `dashboard` (`pathMatch: 'full'`) and `dashboard` → lazy-load the home page (depends on T017)

**Checkpoint**: App loads RTL; brand + footer + home page visible; no console errors. MVP reached.

---

## Phase 4: User Story 2 - Navigate sections from the top navbar (Priority: P2)

**Goal**: All sections reachable from the navbar (primary + «المزيد» menu, Settings in actions); not-yet-built sections open one shared placeholder; active state + URL state correct.

**Independent Test**: Click every navbar / «المزيد» / Settings item; each loads its route (home or shared placeholder), the active item is marked, the URL updates, and refresh + back/forward preserve location; an unknown URL redirects to `/dashboard`.

- [ ] T019 [P] [US2] Create `Frontend/quran-dashboard-ui/src/app/core/navigation/nav-items.ts` exporting the `NavItem[]` config (key/labelAr/labelEn/route/group) exactly per `contracts/ui-navigation.md`
- [ ] T020 [P] [US2] Create the shared placeholder page in `Frontend/quran-dashboard-ui/src/app/shared/ui/placeholder-page/`: reads route `data.titleAr` as the title and shows the fixed Arabic body «سيتم ربط هذا القسم ضمن خطة الميزات التالية.» using `qd-page`/`qd-empty-state` (calm, no "coming soon")
- [ ] T021 [P] [US2] Update `Frontend/quran-dashboard-ui/src/app/app.routes.ts`: add routes for `mushaf, words, tafsirs, gates, resources, i3rab, translations, audio, mutashabihat, settings` → the placeholder with `data: { titleAr }`, and add the `**` wildcard → redirect to `dashboard` (depends on T018, T019, T020)
- [ ] T022 [US2] Update the top-navbar (`core/layout/top-navbar/`) to render `primary` items as links, `more` items inside a «المزيد» dropdown, and `settings` in the actions area, driven by `nav-items.ts`, with `routerLinkActive` for the active state (depends on T014, T019)
- [ ] T023 [US2] Add the mobile collapse menu to the top-navbar (`core/layout/top-navbar/`) exposing all nav items via the mobile menu button (depends on T022)

**Checkpoint**: Every section reachable; active + URL state work; unknown routes go home.

---

## Phase 5: User Story 3 - Light / Dark theme (Priority: P3)

**Goal**: A binary light↔dark theme toggle in the navbar; choice persists across refresh; both themes readable.

**Independent Test**: Toggle the theme — the whole UI switches without reload; refresh keeps the chosen theme; with no stored choice the OS preference is used (default light).

- [ ] T024 [P] [US3] Create `Frontend/quran-dashboard-ui/src/app/core/theme/theme.service.ts`: `providedIn: 'root'`; reads/writes `localStorage` key `qd-theme`, sets `data-theme` on `<html>`, resolves initial theme (stored → `prefers-color-scheme` → light), exposes `toggle()` (binary light↔dark)
- [ ] T025 [US3] Add a binary theme toggle control to the top-navbar (`core/layout/top-navbar/`) wired to `ThemeService.toggle()` with an accessible label and icon (depends on T014, T024)
- [ ] T026 [P] [US3] Add a tiny inline no-flash theme-bootstrap script to `Frontend/quran-dashboard-ui/src/index.html` `<head>` that sets `data-theme` from `localStorage`/`matchMedia` before first paint (depends on T005)

**Checkpoint**: Theme toggles, persists across refresh, no flash; both themes pass contrast.

---

## Phase 6: User Story 5 - Consistent, safe backend boundary (Priority: P5)

**Goal**: One `ApiResponse` envelope, Arabic-default messages, central error handling, a DB health check, real app metadata, Swagger branding, and no committed DB secrets.

**Independent Test**: Call `/api/health` and `/api/dashboard/info` and confirm the envelope + Arabic messages + db check; force a server error and confirm the failure envelope with no leaked internals; inspect committed config and confirm no real password.

- [ ] T027 [US5] Update `Backend/api/QuranDashboard.Api/Contracts/ApiResponse.cs` to `{ IsSuccess, Message, Data, Errors }` with `Ok(data, message?)` and `Fail(message, errors?)` per `contracts/api-response-envelope.md`
- [ ] T028 [P] [US5] Create `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs` with Arabic-default `const` messages (e.g. `HealthOk`, `DashboardInfo`, `UnexpectedError`)
- [ ] T029 [US5] Update `Backend/api/QuranDashboard.Api/Middleware/GlobalExceptionHandler.cs` to write the `ApiResponse.Fail` envelope (HTTP 500, `application/json`, Arabic safe message), still logging the exception and leaking no internals (depends on T027, T028)
- [ ] T030 [P] [US5] Update `Backend/api/QuranDashboard.Api/Extensions/ServiceCollectionExtensions.cs`: register `AddHealthChecks().AddDbContextCheck<QuranDashboardDbContext>("database")` and set the Swagger title to «المنهج القرآني API» (depends on T001)
- [ ] T031 [US5] Update `Backend/api/QuranDashboard.Api/Controllers/HealthController.cs` to inject `HealthCheckService`, run the checks, and return the envelope `data` `{ status, checks:[{name,status}] }` per `contracts/api-health.md` (no connection details) (depends on T027, T028, T030)
- [ ] T032 [US5] Update `Backend/api/QuranDashboard.Api/Controllers/DashboardController.cs` to return `{ appName: "المنهج القرآني", version, environment }` (inject `IHostEnvironment`; read version from the entry assembly, fallback `"0.1.0"`) with an Arabic message, per `contracts/api-dashboard-info.md` (depends on T027, T028)
- [ ] T033 [P] [US5] Remove the password from `Backend/api/QuranDashboard.Api/appsettings.json` and `appsettings.Development.json` (leave a placeholder; keep Host/Port/Database/Username)
- [ ] T034 [P] [US5] Document the user-secrets/env setup for `ConnectionStrings:QuranDashboardDb` in a short note in `Backend/api/QuranDashboard.Api/README.md`

**Checkpoint**: Endpoints return the envelope with Arabic messages + db health; errors are safe; no secrets committed.

---

## Phase 7: User Story 4 - Trustworthy live status & app metadata (Priority: P4)

**Goal**: The home page shows real backend app metadata and the footer shows a live health status, fetched once on load with manual retry; never fabricated.

**Independent Test**: With the backend up, home shows real appName/version/environment and the footer shows the live health (incl. database); stop the backend and confirm a calm error/unknown state with retry and no fabricated values.

> Depends on Phase 6 (US5) endpoints for correct data.

- [ ] T035 [P] [US4] Create `Frontend/quran-dashboard-ui/src/app/core/data-access/api-response.model.ts` defining `ApiResponse<T>` per `contracts/api-response-envelope.md`
- [ ] T036 [P] [US4] Create `Frontend/quran-dashboard-ui/src/app/core/data-access/system.models.ts` defining `AppInfo`, `HealthStatus`, `HealthCheckItem` per `data-model.md`
- [ ] T037 [US4] Create `Frontend/quran-dashboard-ui/src/app/core/data-access/system.api.ts` (`providedIn: 'root'`) with `getDashboardInfo()` and `getHealth()` using `environment.apiBaseUrl`, unwrapping the envelope (depends on T035, T036, T003, T004)
- [ ] T038 [P] [US4] Update the home page (`features/dashboard/pages/dashboard-home/`) to fetch app metadata once on load with explicit loading/success/error states + manual retry; show no fabricated values on error (depends on T037, T017)
- [ ] T039 [P] [US4] Update the footer (`core/layout/footer/`) to fetch health once on load, show a live status indicator with loading/error states + manual retry, and never show a false "healthy" (depends on T037, T013)

**Checkpoint**: Real metadata + live status when up; calm honest state when down.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Accessibility, responsiveness, and final verification across all stories.

- [ ] T040 [P] Accessibility pass across navbar, «المزيد» dropdown, theme toggle, mobile menu, and cards: keyboard operability, visible `--qd-focus-ring`, ARIA labels, and WCAG 2.1 AA contrast in **both** themes (no color-only meaning)
- [ ] T041 [P] Responsive pass: verify no horizontal scroll at 360px, the navbar collapses into the mobile menu, and content has safe padding
- [ ] T042 [P] Respect `prefers-reduced-motion` for theme switch / menu transitions (motion conveys state only)
- [ ] T043 Run the `quickstart.md` verification checklist (SC-001…SC-013): `dotnet build` (Backend) and `npm run build` (Frontend) both succeed; all manual checks pass
- [ ] T044 Final safety review: confirm no real DB password is committed and no Quranic/religious content is fabricated anywhere

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (P1)**: no dependencies.
- **Foundational (P2)**: after Setup. Blocks all UI stories (US1–US4).
- **US1 (Phase 3)**: after Foundational. MVP.
- **US2 (Phase 4)**: after US1 (extends navbar + routes).
- **US3 (Phase 5)**: after Foundational (independent of US2; touches navbar + index.html).
- **US5 (Phase 6, backend)**: after Setup (only T001). Independent of the frontend phases; can be built in parallel with US1–US3 by a backend developer.
- **US4 (Phase 7, frontend live data)**: after Foundational for the UI, and after **US5** for correct data.
- **Polish (Phase 8)**: after all desired stories.

### Within stories

- Foundational: T006–T010 (partials) → T011 (themes) → T012 (styles.scss aggregator).
- US1: (T013, T014, T017) → T015 → T016; T017 → T018.
- US2: (T019, T020) → T021; T019 → T022 → T023.
- US3: T024 → T025; T026 independent (index.html).
- US5: T027 → (T029, T031, T032); T028 → (T029, T031, T032); T001 → T030 → T031.
- US4: (T035, T036) → T037 → (T038, T039).

### Parallel opportunities

- Setup: T002, T003, T004 in parallel (T001 is backend, also independent).
- Foundational: T005 + T006 + T007 + T008 + T009 + T010 in parallel (distinct files).
- US1: T013 + T014 + T017 in parallel.
- US2: T019 + T020 in parallel.
- US5: T028 + T030 + T033 + T034 in parallel (with T027 as the base for the controllers/handler).
- US4: T035 + T036 in parallel; later T038 + T039 in parallel.
- Cross-team: a backend dev can do US5 while frontend devs do Foundational + US1–US3.

---

## Parallel Example: Foundational style system

```bash
# After Setup, build the style partials together (distinct files):
Task T006: "_tokens.scss — --qd-* light tokens"
Task T007: "_typography.scss — @font-face + qd text classes"
Task T008: "_layout.scss — qd-shell/navbar/container/footer"
Task T009: "_components.scss — qd-card/btn*/badge/states"
Task T010: "_forms.scss + _utilities.scss"
# Then T011 (_themes.scss) → T012 (styles.scss aggregator)
```

## Parallel Example: User Story 1

```bash
# Build the shell pieces + home in parallel, then compose:
Task T013: "footer component in core/layout/footer/"
Task T014: "top-navbar component in core/layout/top-navbar/"
Task T017: "dashboard-home page in features/dashboard/pages/dashboard-home/"
# Then T015 (app-shell) → T016 (app.ts); T018 (routes)
```

---

## Implementation Strategy

### MVP first (US1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational → 3. Phase 3 US1 → **STOP & validate** the shell +
home render RTL with branding. This is a demoable MVP.

### Incremental delivery

1. Setup + Foundational → foundation ready.
2. US1 → calm RTL shell + home (MVP).
3. US2 → full navbar navigation + placeholder + URL state.
4. US3 → light/dark theme toggle with persistence.
5. US5 (backend) → consistent envelope + health + metadata + secrets out.
6. US4 → home metadata + footer live status wired to the backend.
7. Polish → a11y, responsive, reduced-motion, build + quickstart verification.

### Parallel team strategy

- Backend dev: US5 (Phase 6) right after T001 — independent of the frontend.
- Frontend dev(s): Foundational → US1 → US2 → US3, then US4 once US5 is ready.

---

## Notes

- `[P]` = different files, no dependency on an unfinished task.
- `[Story]` labels (US1–US5) map tasks to spec user stories for traceability.
- No automated tests this phase (per spec Assumptions); verify via `quickstart.md` + builds.
- Backend changes stay in the **Api** project; `QuranDashboardDbContext` stays empty; **no EF migrations**.
- Commit after each task or logical group (when you choose to commit).
- Honor file-size thresholds in `FRONTEND_STRUCTURE.md`; split components if a file approaches them.
- Total: **44 tasks** across 8 phases.
