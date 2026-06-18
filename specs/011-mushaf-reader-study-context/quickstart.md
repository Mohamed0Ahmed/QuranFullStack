# Quickstart: Mushaf Reader Study Context (local, HTTPS)

Goal: run the backend API and the Angular dashboard **over HTTPS** locally and verify the Mushaf Reader at `https://localhost:4200/dashboard/mushaf`, with all data calls hitting `https://localhost:5015` only.

> Prerequisites: .NET 10 SDK, Node + Angular CLI, a local PostgreSQL with the seeded `quran_dashboard` database (from Features 002–010). This feature is read-only — it does not import or migrate anything.

## 1. Trust local HTTPS certificates

```bash
# .NET dev cert (backend)
dotnet dev-certs https --trust

# Frontend dev cert (choose one):
#   a) mkcert (recommended)
mkcert -install
mkcert localhost            # produces localhost.pem + localhost-key.pem
#   b) or export the .NET dev cert to PEM and reuse it for ng serve
```

Point `angular.json` `serve.options.sslCert`/`sslKey` (and the `start:https` script) at the generated `localhost.pem` / `localhost-key.pem`.

## 2. Configure the backend

```bash
# Database connection (do NOT commit secrets):
cd Backend/api/QuranDashboard.Api
dotnet user-secrets set "ConnectionStrings:QuranDashboardDb" "Host=localhost;Port=5432;Database=quran_dashboard;Username=postgres;Password=<your-password>"
```

Confirm in `appsettings.Development.json`:
- `Cors:AllowedOrigins` = `["https://localhost:4200"]` (HTTPS origin only).
- `MushafReader` defaults = `ar-muyassar` / `en-sahih-international` / `muyassar`.

## 3. Run the backend over HTTPS

```bash
cd Backend/api/QuranDashboard.Api
dotnet run --launch-profile https
# → https://localhost:5015  (http://localhost:5014 redirects to https)
# Swagger (dev): https://localhost:5015/swagger
# Health:        https://localhost:5015/api/health
```

## 4. Run the frontend over HTTPS

```bash
cd Frontend/quran-dashboard-ui
npm install
npm run start:https        # ng serve --ssl ...
# → https://localhost:4200
```

Confirm `src/environments/environment.development.ts` has `apiBaseUrl: 'https://localhost:5015'`.

## 5. Open and smoke-test

Open `https://localhost:4200/dashboard/mushaf`.

| Check | Expected |
|---|---|
| Page renders | A real Mushaf page (lines/words RTL), header shows surah(s)/juz/hizb/rub/page |
| Navigate | Prev/next page and jump-by-surah work; cannot go below 1 or above 604 |
| Markers | juz/hizb/rub/sajda markers beside the right ayah (first line on the page) |
| Select ayah | Bottom-left study shows core ayah + tafsir + translation + full i3rab together |
| Switch source | Each source reloads; "source used" label updates |
| Select word | Top-left analysis shows morphology + glued color-linked segments |
| Marker not selectable | Selecting an ayah-end marker does not produce word analysis |
| URL state | Copy URL, reopen in a new tab → same page/ayah/word/segment/tabs/sources restored |
| HTTPS only | DevTools Network: every `/api/mushaf/*` call targets `https://localhost:5015`; zero HTTP/mixed-content |

## 6. Verify HTTPS-only programmatically (US2)

- DevTools → Network → filter `mushaf`: all requests `https://localhost:5015/...`.
- Temporarily set `apiBaseUrl` to an `http://` URL → the secure-URL interceptor blocks the call with a controlled error (do not ship this change).
- Stop the backend → the reader shows a calm Arabic error state, with no fallback to HTTP.

## 7. Tests

```bash
# Backend
cd Backend && dotnet test
# Frontend (requires the unit-test runner configured in task T013 — the project ships without one)
cd Frontend/quran-dashboard-ui && npm test
```

- **Frontend tests**: the Angular project has **no test runner by default**. Configure it first (tasks.md **T013**: add a `test` target + `test` script, e.g. `@angular/build:unit-test` Vitest or Karma+Jasmine); `npm test` only works after that.
- **Backend integration tests**: use a `Testcontainers.PostgreSql` fixture seeded with a **representative content slice** (not the full DB and not your local DB) covering the pages/ayah/word/sources the tests assert on — loaded from a committed seed script so runs are deterministic and offline. See `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/` (fixture: tasks.md **T009**).

## Example deep link

```
https://localhost:4200/dashboard/mushaf?page=5&ayah=2:25&word=2:25:3&segment=2:25:3:1&panel=word&wordTab=segments&ayahTab=tafsir&tafsirSource=ar-muyassar&translationSource=en-sahih-international&fullI3rabSource=muyassar
```
