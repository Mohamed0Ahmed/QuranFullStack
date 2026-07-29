# Core (app-wide, cross-cutting)

**HOW rules:** `.architecture/API_INTEGRATION_GUIDELINES.md`, `.architecture/FRONTEND_STRUCTURE.md`
(project root). This file is the WHAT.

## What lives here (and only here)

App-wide concerns shared by all features. If it is cross-cutting, change it here — not
per-feature.

- `api/generated/` — payload DTO interfaces generated from the backend OpenAPI spec
  (`npm run generate:api`; see the project README). Generated output — never hand-edit;
  generation is pruned to models-only via `scripts/prune-generated-api.mjs` — no service/fn files are emitted.
- `data-access/` — the API client boundary:
  - `api-response.model.ts` — the `ApiResponse<T>` envelope every API returns (hand-written;
    intentionally not generated).
  - `paged-result.model.ts` — the shared `PagedResultDto<T>` generic (hand-written wrapper
    over generated payload models).
  - `secure-url.interceptor.ts` — forces/validates the API base URL; also lets the Logto
    IdP origin (`environment.logto.endpoint`) pass through un-blocked (the OIDC library uses
    `HttpClient` for discovery/token calls), while every other foreign origin stays blocked.
  - `dev-latency.interceptor.ts` + `dev-api-latency.ts` — dev-only injected latency.
  - `system.api.ts` / `system.models.ts` — health/system info (models re-export generated
    types with UI narrowing).
- `caching/api-response-cache.ts` — shared response cache (feature caches build on the
  same idea; keep the key strategy consistent).
- `auth/` — Logto authentication + roles (Feature 033):
  - `role.guard.ts` — `roleGuard(requiredRole)` factory (a functional `CanActivateFn`).
    Not authenticated → `authorize()` (Logto redirect) and block; authenticated → await
    `CurrentUserStore.ensureLoaded()`, then activate iff `status === 'active'` and
    `roleName === requiredRole`, else redirect to `/`. **Attached to nothing in Phase 2**
    (public browse, roles infrastructure only, decision record §G1/§I4) — the hook a
    future admin feature wires onto its admin routes. Supersedes the Phase-1 `authGuard`.
  - `access.api.ts` — `AccessApi.getMe()` → `GET /api/access/me`, returning the raw
    `ApiResponse<CurrentUserDto>` envelope (thin, like `system.api.ts`).
  - `current-user.model.ts` — `CurrentUser` (== `CurrentUserDto`; the backend `me`
    contract: `sub`, `email`, `displayName`, `status`, `roleId`, `roleName`). `roleName`
    is `null` until the account holds a role (the bootstrapped Owner is `active` /
    `roleName: 'Owner'`).
  - `current-user.store.ts` — `CurrentUserStore`: minimal signal store (`currentUser`,
    `errorMessage`); `load()` is fired post-callback (fresh each call) and never crashes
    the flow; `ensureLoaded()` is the awaitable, load-once path (single cached
    `GET /api/access/me`) the `roleGuard` uses.
- `layout/` — `app-shell`, `top-navbar`, `footer`, `shell-layout.model.ts`.
- `navigation/` — `route-paths.ts` (canonical route constants — incl. `DASHBOARD_ROUTE_PATH`
  and `CALLBACK_PATH` for the Feature-033 landing route — plus `navLabel(key)` for a
  nav item's Arabic label) + `nav-items.ts` + `app-title.strategy.ts` (the `TitleStrategy`
  registered in `app.config.ts`: browser-tab title = `<route title> — المنهج القرآني`, and
  the brand alone on the titleless `dashboard`/home route; each route supplies its own
  `title` from its nav label or explorer page-title constant) + `words-nav-items.ts`
  (`WORDS_MENU_ITEMS` — the Words-section sub-nav rendered as the top-navbar
  "الكلمات والجذور" dropdown; routes from `route-paths`, labels owned here in core).
- `navigation/detail-overlay/` — the app-wide floating detail-overlay navigation layer
  (Feature 029, Change B): `detail-overlay.models.ts` (versioned `v1~…` frame union — the
  URL contract, deliberately decoupled from Words models), `detail-overlay-url-codec.ts`
  (strict parse/serialize/canonicalize; repeated `qdDetail` values bottom→top plus
  `qdDetailOpen=1`; invalid first frame ⇒ no overlay, malformed later frame truncates,
  eight-frame cap), `detail-overlay-provenance.ts` (entry-bound history ownership; the
  live base and stack must match before provenance is trusted), and
  `detail-overlay-history.service.ts` (URL-authoritative state machine: push on entity
  append, replace on top-frame sub-state, close retains the stack in the URL, restore is
  a push; dialog Back uses browser Back only when entry provenance proves the parent,
  else a deterministic replace; fresh or markerless same-URL entries seed their prefix
  history, while reload keeps Angular-preserved entry provenance and does not duplicate
  it; a Restore-derived Mushaf base transition re-materializes a missing parent prefix
  before its final replace so browser and dialog Back return to the same historical
  parent), `detail-overlay-link.directive.ts` (real copyable hrefs;
  only unmodified primary clicks are intercepted), and
  `detail-overlay-ayah-link.directive.ts` (B7 ayah continuity: `a[qdAyahOverlayLink]`
  navigates the base route *underneath* the overlay via
  `navigateBaseWithOverlay` — open overlay ⇒ replace-nav that carries the whole stack to
  the new base; closed + a provided parent frame ⇒ push that promotes the source detail
  to a one-frame stack; neither ⇒ plain push with overlay keys stripped). Core owns
  navigation semantics only — entity rendering lives in
  `features/words/entity-detail-overlay/`.
- `theme/theme.service.ts` — light/dark theme.

## Gotchas / invariants

- **`ApiResponse<T>` is the contract** — all data-access maps through it; don't unwrap ad hoc
  in features.
- **Route strings live in `route-paths.ts`** — reference the constants, don't hardcode paths
  in components/routes.
- **Public-browse route tree (Feature 033, Phase 2)** — `/dashboard` is one **unguarded**
  parent with `''` (home), `mushaf`, and `words` children; the whole app is browsable
  anonymously (the Phase-1 blanket `authGuard` was removed, decision record §G1). URLs are
  unchanged. `/callback` (`CALLBACK_PATH`, the `features/auth/` landing page) is public and
  sits before the `**` wildcard in `app.routes.ts`. The placeholder nav routes (e.g.
  `/tafsirs`) stay top-level and unguarded. `/abwab` (Abwab doors & sections, Slice B) is a
  real top-level lazy feature route, same unguarded posture — see
  `../features/abwab/README.md`. Nothing is protected in this phase: the reusable
  `roleGuard` exists but is attached to no route — a future admin feature wires it onto its
  own admin routes.
- Interceptor order matters (`secureUrlInterceptor`, then `authInterceptor()`, then
  `devLatencyInterceptor`); keep registration order in `app.config.ts`. `authInterceptor()`
  (from `angular-auth-oidc-client`) attaches the Logto Bearer token only to requests under
  `apiBaseUrl` via the `secureRoutes` config, and must run after `secureUrlInterceptor`.

## Related

- Feature consumers: `../features/words/README.md`, `../features/mushaf/README.md`.
- Shared UI primitives: `../shared/`.
