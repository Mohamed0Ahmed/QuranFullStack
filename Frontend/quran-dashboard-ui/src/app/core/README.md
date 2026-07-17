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
  - `secure-url.interceptor.ts` — forces/validates the API base URL.
  - `dev-latency.interceptor.ts` + `dev-api-latency.ts` — dev-only injected latency.
  - `system.api.ts` / `system.models.ts` — health/system info (models re-export generated
    types with UI narrowing).
- `caching/api-response-cache.ts` — shared response cache (feature caches build on the
  same idea; keep the key strategy consistent).
- `layout/` — `app-shell`, `top-navbar`, `footer`, `shell-layout.model.ts`.
- `navigation/` — `route-paths.ts` (canonical route constants, plus `navLabel(key)` for a
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
- Interceptor order matters (secure-url before dev-latency); keep registration order in
  `app.config.ts`.

## Related

- Feature consumers: `../features/words/README.md`, `../features/mushaf/README.md`.
- Shared UI primitives: `../shared/`.
