# Quickstart: Run & Verify — Layout & Foundation

How to run both apps and verify the foundation against the spec's Success Criteria. No automated
tests exist; verification is build + observable checks.

## Prerequisites

- .NET 10 SDK, Node.js (Angular 20 compatible), and a local PostgreSQL reachable at
  `localhost:5432` with database `quran_dashboard`.
- The DB password is **not** in source control. Provide it locally one of two ways:

  ```bash
  # Option A — .NET user-secrets (Development)
  cd Backend/api/QuranDashboard.Api
  dotnet user-secrets set "ConnectionStrings:QuranDashboardDb" \
    "Host=localhost;Port=5432;Database=quran_dashboard;Username=postgres;Password=YOUR_PASSWORD"

  # Option B — environment variable
  export ConnectionStrings__QuranDashboardDb="Host=localhost;Port=5432;Database=quran_dashboard;Username=postgres;Password=YOUR_PASSWORD"
  ```

## Run the backend

```bash
cd Backend/api/QuranDashboard.Api
dotnet run            # http profile → http://localhost:5014
# Swagger UI:  http://localhost:5014/swagger   (title shows «المنهج القرآني API»)
```

Smoke-check the endpoints:

```bash
curl http://localhost:5014/api/health         # envelope with data.status + data.checks[database]
curl http://localhost:5014/api/dashboard/info  # envelope with appName/version/environment
```

## Run the frontend

```bash
cd Frontend/quran-dashboard-ui
npm install           # first run only
npm start             # → http://localhost:4200
```

## Build verification (gate for "done")

```bash
# Backend
cd Backend && dotnet build           # MUST succeed (SC-013)

# Frontend
cd Frontend/quran-dashboard-ui && npm run build   # MUST succeed (SC-013)
```

## Manual verification checklist (maps to Success Criteria)

- [ ] **SC-001** App loads right-to-left, Arabic UI, «المنهج القرآني» wordmark in navbar; no LTR
      flash; **0** console errors.
- [ ] **SC-002** Every section reachable from the navbar — primary in 1 click, «المزيد» in 2;
      every route loads a page (home or shared placeholder), never an error.
- [ ] **SC-003** Refresh and browser back/forward keep you on the correct section.
- [ ] **SC-004** The active section is visually marked for every section.
- [ ] **SC-005** Theme toggle switches light↔dark, persists across a full refresh; both themes
      read clearly (AA contrast).
- [ ] **SC-006** With backend up: home shows real appName/version/environment; footer shows live
      health (incl. database); no fabricated values.
- [ ] **SC-007** With backend stopped: home + footer show a calm error/unknown state with retry;
      no fabricated values, no false "healthy".
- [ ] **SC-008** Responses use the one envelope; a forced server error returns the failure
      envelope with no leaked internals.
- [ ] **SC-009** At 360px width: no horizontal scroll; nav collapses into an accessible menu.
- [ ] **SC-010** All controls keyboard-operable with a visible focus ring.
- [ ] **SC-011** Committed `appsettings*.json` contain no real password.
- [ ] **SC-012** No Quranic/religious content is fabricated anywhere.
- [ ] **SC-013** Backend build and frontend build both succeed.

## Notes

- Unknown routes (e.g. `/nope`) MUST redirect to `/dashboard` (Clarification Q1).
- Health/metadata are fetched **once on load** with a manual retry on failure — no background
  polling (Clarification Q2).
- Theme control is a **binary** light↔dark toggle (Clarification Q3).
