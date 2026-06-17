# Contract: Local HTTPS Environment & Frontend Integration

This contract realizes the locked requirement: **both apps run over HTTPS locally, and all normal frontend data calls target the HTTPS backend URL only.**

## Canonical local URLs

| App | URL |
|---|---|
| Backend API (HTTPS) | `https://localhost:5015` |
| Backend API (HTTP) | `http://localhost:5014` → **redirects** to HTTPS (`UseHttpsRedirection`, already present) |
| Frontend dev server (HTTPS) | `https://localhost:4200` |
| Mushaf Reader route | `https://localhost:4200/dashboard/mushaf` |

## Backend changes

1. **Launch profile**: use the existing `https` profile (`applicationUrl: "https://localhost:5015;http://localhost:5014"`). Make it the default profile used by `dotnet run`.
2. **CORS** (`appsettings.json` + `appsettings.Development.json`): restrict `Cors:AllowedOrigins` to the HTTPS origin only:
   ```json
   "Cors": { "AllowedOrigins": ["https://localhost:4200"] }
   ```
   (Remove `http://localhost:4200`.) The existing `AngularDev` policy already reads this list.
3. **MushafReader defaults** (config section):
   ```json
   "MushafReader": {
     "DefaultTafsirSourceKey": "ar-muyassar",
     "DefaultTranslationSourceKey": "en-sahih-international",
     "DefaultFullI3rabSourceKey": "muyassar"
   }
   ```
4. **Keep** `UseHttpsRedirection()`. No HSTS needed for local dev.
5. **Dev cert**: `dotnet dev-certs https --trust` (documented in quickstart).

## Frontend changes

1. **`environment.development.ts`**:
   ```ts
   export const environment = { production: false, apiBaseUrl: 'https://localhost:5015' };
   ```
   (Production `environment.ts` keeps `apiBaseUrl: ''` — same-origin, out of scope.)
2. **HTTPS dev server** (`angular.json` `serve` options) + `package.json` script:
   ```jsonc
   // angular.json → architect.serve.options
   "ssl": true,
   "sslCert": "<path-to-localhost.pem>",
   "sslKey": "<path-to-localhost-key.pem>"
   ```
   ```jsonc
   // package.json scripts
   "start:https": "ng serve --ssl --ssl-cert <cert> --ssl-key <key>"
   ```
   (Generate a local cert via `mkcert localhost` or reuse the .NET dev cert exported to PEM; documented in quickstart.)
3. **Secure-URL guard** (`core/data-access/secure-url.interceptor.ts`), registered via `provideHttpClient(withInterceptors([...]))` in `app.config.ts`:
   - Allow only requests whose absolute URL starts with `environment.apiBaseUrl` (which is `https://...`).
   - Reject (controlled error, no rewrite to HTTP) any request that is not HTTPS or not under `apiBaseUrl`.
   - This makes FR-003/FR-004 testable: a unit test asserts a non-HTTPS URL is blocked.
4. **Unreachable backend**: the facade/store surfaces a calm Arabic error state (`qd-error-state`); no silent HTTP fallback (the interceptor forbids it).

## Frontend API integration (per API_INTEGRATION_GUIDELINES)

Flow: `MushafReaderPageComponent` → `mushaf-reader.facade.ts` → `*.api.ts` (data-access) → backend.

- API services (`mushaf-pages.api.ts`, `mushaf-ayah-study.api.ts`, `mushaf-word-analysis.api.ts`) build URLs from `environment.apiBaseUrl`, type query params, and return `Observable<ApiResponse<T>>`. They do **not** own state.
- Facade checks `isSuccess`, reads `data`/`message`/`errors`, maps to page-ready view models, owns loading/empty/error, URL↔state sync, request dedupe, bounded cache, and optional prev/next prefetch.
- Components receive view models only; child components never call APIs directly.
- HTML (tafsir/full-i3rab) is rendered via the built-in sanitizer (`safe-html` pipe / `[innerHTML]`, **no** `bypassSecurityTrustHtml`).
- Never fabricate Quranic data; missing data → controlled empty state.

## Acceptance (maps to spec US2)

- Backend reachable only at `https://localhost:5015` (HTTP redirects).
- Frontend served at `https://localhost:4200`.
- Every page/ayah/word request targets `https://localhost:5015`; zero HTTP/mixed-content data requests.
- A request to a non-HTTPS URL is blocked by the interceptor with a controlled error.
