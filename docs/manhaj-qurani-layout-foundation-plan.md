# Implementation Plan — المنهج القرآني · Layout & Foundation (Phase 0)

- **Project:** المنهج القرآني (code identifiers/namespaces stay `QuranDashboard.*`)
- **Phase:** Foundation before the first real Quran data feature (not Words/Ayahs yet)
- **Date:** 2026-06-07
- **Status:** Approved design — ready to implement
- **Source:** Revised from a ChatGPT draft after reviewing the actual codebase and the
  canonical docs (`PRODUCT.md`, `DESIGN.md`, `CODING_PRINCIPLES.md`, backend
  `.architecture/*`, frontend `.architecture/*`).

---

## 1. Summary

Finish the **frontend foundation** and apply **light backend polish** so future features
can be built consistently. The backend already has a working Clean Architecture skeleton
(PostgreSQL configured, health + dashboard controllers, global exception handler, clean
`Program.cs`); the frontend is essentially greenfield (empty Angular 20 app with Tailwind
configured). This phase delivers a calm, Arabic-first (RTL) app shell with a top navbar,
a centralized `qd-*` style system with light/dark themes, self-hosted Arabic typography,
wired routes with a shared placeholder page, and a proven end-to-end API integration —
without any Quran feature data.

---

## 2. Decisions (locked)

1. **Scope:** Frontend foundation + light backend polish. Defer heavier backend refactors
   (full localization system) until a feature needs them (YAGNI).
2. **API response:** Align the backend `ApiResponse` code to the canonical
   `API_GUIDELINES.md` shape (`isSuccess / message / data / errors`).
3. **Fonts:** Self-hosted **Amiri** (content/naskh) + **IBM Plex Sans Arabic** (UI chrome).
4. **Navigation model:** **Top navbar** is the primary navigation — **no global sidebar**.
   Shell = top navbar + router outlet + footer. Sidebars are page-specific contextual
   panels only, added per feature later.
5. **Palette:** Lock the parchment & ink direction now (per `DESIGN.md`); finalize concrete
   OKLCH token values during implementation and review them in the running app.
6. **Brand:** Typographic wordmark «المنهج القرآني» only (no icon/logo this phase).
7. **DB secrets:** Remove the committed dev password; use `dotnet user-secrets`/env locally,
   leave a placeholder in `appsettings`, document setup.
8. **API wiring:** Wire `HttpClient` and prove the contract by calling `/api/health` and
   `/api/dashboard/info` (footer status indicator + home metadata). No feature data.
9. **Navbar grouping:**
   - **Primary:** لوحة التحكم · المصحف والآيات · الكلمات والجذور · التفاسير · الأبواب · المصادر
   - **«المزيد» menu:** الإعراب · الترجمات · الصوتيات · المتشابهات
   - **Settings (الإعدادات):** in the user/actions area (not main nav)

---

## 3. Current state (review findings)

### Already built (no work needed)
- PostgreSQL configured via Npgsql in `Infrastructure/DependencyInjection.cs`; connection
  name is **`QuranDashboardDb`** (not `DefaultConnection`).
- EF Core layering correct; `QuranDashboardDbContext` is empty (kept empty this phase).
- Full Clean Architecture solution (Api / Application / Application.Abstractions / Domain /
  Infrastructure / Shared); clean `Program.cs` with extension methods.
- `GlobalExceptionHandler`, `/api/health`, `/api/dashboard/info`, env-aware CORS, Swagger.
- Frontend: Angular 20.3 standalone + Tailwind v3 configured.
- Stack: .NET 10 / EF Core 10 / Npgsql 10; API at `http://localhost:5014` (https `5015`);
  **no test projects exist**.

### Discrepancies to fix
- `ApiResponse.cs` is `{Success, Data, Message}` (no `errors`, no failure factory) and the
  exception handler returns RFC7807 `ProblemDetails` — both contradict `API_GUIDELINES.md`.
- Everything is branded "Quran Dashboard" instead of «المنهج القرآني»; `dashboard/info`
  lacks `version`/`environment`; Swagger title is English.
- `DESIGN.md` palette and both fonts are still placeholders.

### Gaps
- No DB health check (health is static). No localization/message foundation.
- Frontend: `index.html` is `lang="en"` with no `dir="rtl"`; routes empty; no HttpClient
  provider; no layout, `qd-*` classes, tokens, theme, or fonts.
- Dev DB password committed in `appsettings.json`.

---

## 4. Scope

### In scope
- Backend: ApiResponse alignment, exception-handler shape, branding + metadata, DB health
  check, minimal Arabic-default messages, secrets handling.
- Frontend: RTL bootstrap, centralized style system + tokens + light/dark themes, Arabic
  fonts, app shell (navbar + outlet + footer), navbar navigation + routes + shared
  placeholder, dashboard home, proven API integration, responsive + accessibility.

### Out of scope
Quran/feature data (words, ayahs, tafsir, translations, i3rab, morphology), mushaf reader,
CRUD, auth/authorization, admin/user management, resource import into the database, full
i18n/translation system, production deployment, advanced theme customization, real logo
design.

---

## 5. Backend design (light polish)

1. **ApiResponse alignment** (`Contracts/ApiResponse.cs`): rename `Success` → `IsSuccess`;
   add `Errors`; keep `Data` / `Message`; add a `Fail(message, errors)` factory beside `Ok`.
   Property names stay English (camelCase on the wire → `isSuccess/message/data/errors`).
2. **GlobalExceptionHandler:** return the `ApiResponse` failure shape (Arabic-default safe
   message, `errors: []`, status 500) instead of `ProblemDetails`. Still logs the full
   exception; never leaks stack traces/paths/SQL.
3. **Branding + metadata:** `/api/dashboard/info` returns `appName = "المنهج القرآني"`,
   `version`, `environment`; `/api/health` and info carry Arabic-default messages; Swagger
   title → «المنهج القرآني API».
4. **DB health check:** add `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`,
   register `AddDbContextCheck<QuranDashboardDbContext>()`, and have `HealthController`
   report overall + `db` status via `HealthCheckService` (no connection details leaked).
5. **Minimal messages:** a small Arabic-default message-constants holder near the API
   feature — not a full localization system (deferred).
6. **Secrets:** remove `Password=postgres` from `appsettings*.json`; use user-secrets/env
   locally; leave a placeholder; document setup. Connection name stays `QuranDashboardDb`.

> EF migrations: none required (DbContext stays empty). Do not hand-write migrations; do not
> run `database update`.

---

## 6. Frontend design

### 6.1 Bootstrap / RTL
- `index.html`: `lang="ar"`, `dir="rtl"`, title «المنهج القرآني».
- `app.config.ts`: add `provideHttpClient`.
- `app.ts`: host the app shell.

### 6.2 Style system (per `UI_STYLE_SYSTEM.md`)
- `src/styles/` partials: `_tokens.scss`, `_themes.scss`, `_typography.scss`, `_layout.scss`,
  `_components.scss`, `_forms.scss`, `_utilities.scss`, imported by `styles.scss` (with the
  Tailwind layers).
- `--qd-*` CSS-variable tokens (bg, surface, surface-elevated, text, text-muted, border,
  accent, danger, warning, success, focus-ring, radius-{sm,md,lg}, spacing scale,
  shadow/hairline). Warm-tinted neutrals; no pure `#fff`/`#000`. Concrete OKLCH values
  chosen during implementation.
- Themes via `[data-theme="light"|"dark"]`; **light parchment default**; components
  reference tokens only (no per-component theme branching).
- Typography: self-host **Amiri** (content/headings) + **IBM Plex Sans Arabic** (UI) as
  `woff2` in `public/fonts` with `@font-face`. Shared text classes: `qd-page-title`,
  `qd-section-title`, `qd-card-title`, `qd-text`, `qd-text-muted`, `qd-text-meta`.
- Build only the `qd-*` primitives the foundation actually uses: `qd-shell`, `qd-navbar`,
  `qd-container`, `qd-page`, `qd-page-header`, `qd-card`, `qd-btn` (+ `-primary`,
  `-secondary`, `-ghost`), `qd-badge`, `qd-empty-state`, `qd-loading-state`,
  `qd-error-state`, `qd-footer`.

### 6.3 Shell + navigation (`src/app/core/layout/`)
- `app-shell`: composes **top navbar + router outlet + footer** (layout only, no feature
  logic). No global sidebar.
- `top-navbar`: wordmark «المنهج القرآني», primary nav links, «المزيد» dropdown, theme
  toggle, user/actions area (holds Settings), mobile menu button.
- `footer`: brand line «المنهج القرآني © 2026», short Arabic description, version +
  environment (dev only), small **live status indicator** from `/api/health`.
- `core` **ThemeService**: toggles `data-theme` on the root, persists to `localStorage`,
  honors `prefers-color-scheme` for the initial value.
- Navigation config in `core` as `{ key, labelAr, labelEn, route }` items pointing to route
  paths — not hardcoded inside the navbar component.

### 6.4 Routes + placeholder
- `app.routes.ts` wires the nav sections. Not-yet-built sections share **one** calm
  `PlaceholderPageComponent` that reads route `data` (label/title) and shows neutral copy:
  «سيتم ربط هذا القسم ضمن خطة الميزات التالية.» (no "coming soon").
- Home (`features/dashboard/pages/dashboard-home`) is a real, simple overview: welcome
  heading, short description, cards linking to main areas (الكلمات والجذور · الآيات والمصحف ·
  التفاسير والترجمات · الإعراب والتحليل · المصادر), plus app metadata from
  `/api/dashboard/info`. **No fabricated statistics or Quran counts.**

### 6.5 API integration (`src/app/core/data-access/`, per `API_INTEGRATION_GUIDELINES.md`)
- Typed `ApiResponse<T>` model matching the backend (`isSuccess/message/data/errors`).
- Small `system.api.ts` with `getHealth()` and `getDashboardInfo()`.
- Calm loading / error / empty states for the home metadata and footer status.
- Angular environment `apiBaseUrl = "http://localhost:5014"`.

### 6.6 Responsive + accessibility
- Navbar collapses to a menu/drawer on mobile; safe content padding; no horizontal overflow.
- Semantic `nav`, visible focus states, keyboard-operable toggle/menu/dropdown, ARIA labels.
- WCAG 2.1 AA contrast in both themes; never color-only meaning; respect reduced-motion.

---

## 7. Implementation order

1. **Review/confirm foundation** — builds pass; re-read backend + frontend `.architecture`
   docs and `PRODUCT.md`/`DESIGN.md` for the touched areas.
2. **Backend polish** — ApiResponse alignment → exception-handler shape → branding/metadata
   → DB health check → minimal Arabic messages → secrets out of source control.
3. **Frontend style foundation** — style partials, `--qd-*` tokens, light/dark themes,
   self-hosted fonts, base `qd-*` primitives + text classes.
4. **Frontend shell** — `app-shell`, `top-navbar` (primary + «المزيد» + theme toggle +
   user/actions), `footer` (+ live status), `ThemeService`, RTL bootstrap.
5. **Routes + pages** — nav config, routes, shared `PlaceholderPageComponent`, dashboard
   home with API metadata, active-link state, route titles.
6. **API integration** — `provideHttpClient`, `ApiResponse<T>` model, `system.api.ts`,
   loading/error/empty states, environment base URL.
7. **Verification** — see below.

---

## 8. Verification

**Backend:** `dotnet build` (no test projects exist, so no `dotnet test`). Manual:
`/api/health` returns overall + `db` status; `/api/dashboard/info` returns
`appName/version/environment`; Swagger title is «المنهج القرآني API»; no secrets committed.

**Frontend:** `npm run build`. Manual: app opens RTL; navbar (primary + «المزيد») works;
theme toggle persists across refresh; footer shows live status; routes + placeholder work;
home shows real API metadata; mobile menu works; no console errors; no fabricated data.

---

## 9. Acceptance criteria

- Backend builds; `ApiResponse` matches `API_GUIDELINES.md`; errors use the failure shape;
  app name/version/environment exposed; DB health check present; no committed DB secrets;
  DbContext remains empty; no hand-written migrations.
- Frontend builds; RTL-first shell with top navbar + footer (no global sidebar); «المنهج
  القرآني» wordmark appears consistently; `--qd-*` tokens + light/dark themes + Arabic fonts
  in place; nav routes + shared placeholder; dashboard home wired to the API; responsive
  mobile menu; AA contrast; no fabricated Quran data; no feature logic in the shell.

---

## 10. Deferred decisions / next phase

**Deferred:** final logo, auth/roles, real Quran data import, first-feature schema,
Words/Ayahs API contracts, full localization/i18n system, production deployment, advanced
theming.

**Next phase (own spec):** *Quran Words & Ayahs Foundation* — schema/read models for
ayahs/words, import/seed strategy from `resources/`, endpoints (ayah details, ayah words,
word details, root/lemma/stem, simple i3rab), and pages (words explorer, ayah details,
selected-word panel, morphology/i3rab preview) using page-specific contextual sidebars.
