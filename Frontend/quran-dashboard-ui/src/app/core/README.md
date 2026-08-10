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
- `auth/` — Logto authentication and access foundation:
  - `owner.guard.ts` — `ownerGuard` is the sole route guard. It is attached only to
    `/settings/access`: anonymous visitors enter Logto with that internal URL saved; an
    authenticated visitor loads `/api/access/me` and must be both active and `isOwner`.
    Every public route remains unguarded and never waits for `/api/access/me`.
  - `auth-return-location.store.ts` — stores one safe internal return URL across the Logto
    redirect; the callback consumes it only after successful authentication, retains it through
    an error/retry, and logout clears it.
  - `write-auth-failure.coordinator.ts` — an opt-in foundation for future administrative
    writes only: a `401` starts one login flow without retrying the write, and a `403` forces
    an access snapshot refresh so the caller can re-evaluate its capability. It is not an
    HTTP interceptor, so public reads retain ordinary error handling.
  - `access.api.ts` — `AccessApi.getMe()` → `GET /api/access/me`, returning the raw
    `ApiResponse<CurrentUserResponse>` envelope (thin, like `system.api.ts`). For this interactive
    provisioning call only, it adds the raw signed Logto ID token as
    `X-Interactive-Identity-Evidence`; it never parses identity claims in the browser.
  - `permission-codes.generated.ts` — the named code shape, written by
    `npm run generate:permission-codes` from `AbwabPermissionCatalogue.cs` and committed. It is the
    single source of the codes; nothing here is typed by hand.
  - `permission-code.ts` — derives `PermissionCode`, the ordered `PERMISSION_CODES` and
    `isPermissionCode` from that generated shape, so the union and the runtime set cannot disagree
    with each other or with the server. `npm run check:permission-catalogue` re-renders the
    generated file from the backend catalogue and fails unless the committed copy matches, which is
    what makes adding or retiring a permission one visible change rather than a silent drift; no
    authorization decision uses a role name.
  - `current-user.model.ts` — normalizes the generated `/me` wire DTO to the bounded UI
    snapshot: `sub`, `email`, `displayName`, `status`, `isOwner`, ordered direct
    `permissions`. No role ID or role name crosses this contract.
  - `current-user.store.ts` — obtains the raw ID token from the OIDC client before `/api/access/me`
    while the normal auth interceptor continues to attach the API resource access token as
    `Authorization: Bearer`. It owns access snapshot signals (`currentUser`, `permissions`,
    `loadState`, `errorMessage`, `isAuthenticated`, `authStateKnown`, `isActive`, `isOwner`) and `can`/`canAny`.
    A Logto session observation refreshes the snapshot asynchronously, never blocking public render. Concurrent
    `ensureLoaded()` calls share one request; `refresh()` supersedes stale results; `clear()`
    invalidates pending work, snapshot, and permissions for logout. Unknown, loading, and
    error state fail closed.
- `layout/` — the F01 app chrome: `app-shell`, `top-navbar`, `app-navigation`, `footer`,
  `shell-layout.model.ts`, and `nav-progress/`.
  - `app-shell` owns the skip link (`.qd-skip-link` → the sole `<main id="qd-main-content"
    tabindex="-1">`), the shell order (nav progress → navbar → main → footer), and the
    **background/sibling arrangement**: while the navigation sheet is open the shell inerts
    `main` and the footer, and the sheet — a `qd-modal-shell` rendered outside both — is never
    inside a subtree it inerts. It reads that state from `TopNavbarComponent.sheetOpen` through a
    `viewChild`; the navbar keeps ownership of open/close/focus-return.
  - `top-navbar` owns navigation *state*: which dropdown is open, whether the sheet is open, the
    Wide/`matchMedia` band, theme toggle, and sign in/out. Desktop navigation is rendered **only**
    at Wide (`>= 1080`, `QD_BP_WIDE_QUERY`) — structurally, not with `display: none` — because a
    Medium-width navbar cannot hold the full link row and used to push the document to 865px at
    768. Below Wide the navbar shows one 44px sheet toggle. The navbar's own background subtree
    (`.qd-navbar__background`, `display: contents`) goes inert whenever anything holds the scroll
    lock; the toggle stays reachable while the sheet it opened is the lock holder, which is what
    lets focus return to it on close.
  - `app-navigation` is app-specific presentation, not shared UI: one component renders the same
    `NAV_MENU` tree in `desktop` mode (anchored dropdowns, `#<key>-menu`, `nav-<key>-trigger`) or
    `sheet` mode (inline sublists, group labels for auth-gated parents). It emits pointer/click and
    activation intent and holds no state, no authorization and no route logic. `ownerGuard` remains
    the boundary; the nav only hides what an Owner-only route would refuse anyway.
    **The Wide dropdown deliberately does not use `qdFloatingLayer` (F15).** It stays an anchored
    `position: absolute` `.qd-nav__menu` (`_components.scss`) for two behavioural reasons, both
    pinned by `e2e/shell-nav.e2e.ts`: the directive's floating surface sits a 4px anchor gap away
    from its trigger, which breaks the hover-open/hover-into-menu path the nav relies on, and the
    directive's `action-menu` variant would put `role="menuitem"` on links the same e2e resolves
    through `getByRole('link')`. It is a recorded deviation from F15, not an oversight; revisit it
    only together with those e2e expectations.
  - `nav-progress` — the router navigation progress bar (`qd-nav-progress`): a flat 2px track with
    a solid green segment, shown while a lazy route's chunk downloads (200ms show-delay; the settle
    rule is an inversion over the known in-flight router events so unknown/future event classes
    clear it, never stick it). Contract: `.architecture/UI_STYLE_SYSTEM.md` §17.
  - `footer` keeps its natural height and the navy chrome, owns its measure with
    `.qd-footer__inner` (no second gutter), and reports the health read politely: loading and
    error are both `role="status"`, because a failed health *read* is not a write failure and must
    not claim `role="alert"`.
- `navigation/` — `route-paths.ts` (canonical route constants — incl. `DASHBOARD_ROUTE_PATH`
  and `CALLBACK_PATH` for the Feature-033 landing route — plus `navLabel(key)` for a
  nav item's Arabic label) + `app-title.strategy.ts` (the `TitleStrategy`
  registered in `app.config.ts`: browser-tab title = `<route title> — المنهج القرآني`, and
  the brand alone on the titleless `dashboard`/home route; each route supplies its own
  `title` from its nav label or explorer page-title constant). The nav menu model is three
  files: `nav-items.ts` (the flat `NAV_ITEMS` registry — routes, titles, placeholder
  derivation; `NavItem` also carries optional `children`/`queryParams`, navbar-presentation
  fields that never enter this registry); `words-nav-items.ts` (`WORDS_MENU_ITEMS` as
  `NavItem[]`, labels owned here in core, routes from `route-paths`); and `nav-menu.ts` (the
  navbar's presentation tree — `NAV_MENU`, `NAV_ITEMS` with children attached from a
  `childrenByParentKey` table). Children attach **outside** `NAV_ITEMS` because
  `route-paths.ts` imports `NAV_ITEMS` and derives every route constant from it at module
  init — nesting children into `nav-items.ts` would create an import cycle that hits a TDZ
  `ReferenceError`; recorded here so nobody "simplifies" the children back in. The
  `childrenByParentKey` table carries `words`, `abwab`, and `settings` («إدارة الوصول» →
  `/settings/access`). The dropdowns ("الكلمات والجذور", "الأبواب", "الإعدادات") are
  `@if (item.children)` inside `layout/app-navigation`, data-driven, not per-key template
  branches; «الأرشيف» (`/abwab` + `{archive:'1'}`) is the app's first query-param nav entry, and
  «الإعدادات» is the app's first auth-gated one: a parent that navigates nowhere itself (like
  «المزيد») — a dropdown trigger in the desktop actions cluster, a non-navigable group label above
  its child in the navigation sheet — that the navbar renders in both modes only for an Active
  Owner and only once
  `CurrentUserStore.authStateKnown()` is true, so the entry cannot
  flash while `/api/access/me` resolves. That visibility is convenience; `ownerGuard` on
  `/settings/access` stays the authorization boundary. Also here: `idle-preload.strategy.ts` — the `withPreloading`
  strategy registered in `app.config.ts`; preloads every lazy route chunk, each after
  an idle callback (timeout-bounded, `setTimeout` fallback), so first clicks find
  chunks cached without preloading ever competing with bootstrap or the landing
  page's own work.
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
- **Public-browse route tree** — `/dashboard` is one **unguarded**
  parent with `''` (home), `mushaf`, and `words` children; the whole app is browsable
  anonymously (the Phase-1 blanket `authGuard` was removed, decision record §G1). URLs are
  unchanged. `/callback` (`CALLBACK_PATH`, the `features/auth/` landing page) is public and
  sits before the `**` wildcard in `app.routes.ts`. The placeholder nav routes (e.g.
  `/tafsirs`) stay top-level and unguarded. `/abwab` (Abwab doors & sections, Slice B) is a
  real top-level lazy feature route, same unguarded posture — see
  `../features/abwab/README.md`. `/settings/access` is a lazy Owner security-administration
  feature guarded only by `ownerGuard`; the navbar's «الإعدادات» dropdown exposes it to Active
  Owners (a visibility convenience — see `navigation/` above — never a second authorization
  rule). No other route has a guard.
- Interceptor order matters (`secureUrlInterceptor`, then `authInterceptor()`, then
  `devLatencyInterceptor`); keep registration order in `app.config.ts`. `authInterceptor()`
  (from `angular-auth-oidc-client`) attaches the Logto Bearer token only to requests under
  `apiBaseUrl` via the `secureRoutes` config, and must run after `secureUrlInterceptor`.
- Both Development and production Logto configuration request the `email` OIDC scope. The production
  environment guard rejects a build that omits it, because verified email is supplied only through
  the signed ID-token evidence path.

## Related

- Feature consumers: `../features/words/README.md`, `../features/mushaf/README.md`.
- Shared UI primitives: `../shared/`.
