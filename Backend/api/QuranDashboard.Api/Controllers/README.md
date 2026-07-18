# API controllers

HTTP entry points for `QuranDashboard.Api`. This folder owns route groups, HTTP status mapping,
and the `ApiResponse<T>` envelope; application handlers own use-case logic.

## Route families

- `Access/` — `api/access/me`; the authenticated caller's provisioned user. Carries `[Authorize]`
  (authenticated-only) and get-or-create provisions the local user on first login (email verified
  server-side via the Logto Management API). The response includes `roleName` (null when no role);
  the configured owner email is bootstrapped to `Owner`/`Active`. This is the only endpoint that
  requires authentication — role-based named policies are registered but applied to nothing, so every
  other route stays publicly browsable. See `../README.md` (Authentication / Roles).
- `Dashboard/` — `api/dashboard/info` for app/version/environment metadata.
- `MushafReader/Ayahs/` — `api/mushaf/ayahs/{verseKey}/study`, `/similar-ayahs`, and `/mutashabihat`.
- `MushafReader/Catalogs/` — `api/mushaf/surahs` and `api/mushaf/study-sources` catalogs.
- `MushafReader/Pages/` — `api/mushaf/pages/{pageNumber}` page-reader endpoint.
- `MushafReader/Words/` — `api/mushaf/words/{wordLocation}/analysis`.
- `System/` — `api/health` health-check endpoint.
- `Words/` — `api/words/unique`, `api/words/roots`, `api/words/lemmas`,
  `api/words/stems`, and `api/words/word-types` explorer endpoints. Word-types grouped detail reads
  (`api/words/word-types/table/{kind}/{dimensionId}[/words|/ayahs|/surahs]`, Feature 023) live in the
  separate `WordTypeGroupedDetailsController`, which shares the `…/word-types/table` route base without
  growing `WordTypesController`. Route `{kind}` is the plural key `roots|stems|lemmas`; an unknown value
  is a `400`. All four actions carry the identical five-field scope (`type`, `childCode`, `case`, `tense`,
  `voice`); `words` and `ayahs` are paged (`page`/`pageSize`), while summary and `surahs` are single-shot
  and expose no paging parameter. Invalid kind/id/filter/paging → `400`, an absent scoped group → `404`,
  and an out-of-range page → `200` with an empty page.

## Splitting an oversized controller

Controllers have a 300-line hard limit (`../../../.architecture/BACKEND_STRUCTURE.md`). Two shapes
are in use, and they are not interchangeable:

- **A new route family → a new controller class.** `WordTypeGroupedDetailsController` is the
  precedent: it shares the `…/word-types/table` route base without growing `WordTypesController`.
- **An existing endpoint group → a `partial` part of the same class.** `RootsController` (list) +
  `RootsController.Details.cs` (per-root detail/drilldown) and `WordTypesController`
  (tree/words/table/scope-counts) + `WordTypesController.Details.cs` (per-word detail) follow this.
  Swashbuckle derives each operation's OpenAPI `tags` from the controller **class name**, so moving
  *existing* actions to a *new class* would retag them and change the exported spec. Keep the class
  name and the split is invisible to `swagger.json`. The part carrying the primary constructor owns
  the shared handlers, the `[ApiController]`/`[Route]` attributes, and the paging defaults; the other
  parts declare only `public sealed partial class <Name>` and their actions.

## Boundary

- Controllers delegate to Application handlers under `../../../application/`; they do not
  query EF Core, read files, or own business rules.
- API envelope contract lives in `../Contracts/ApiResponse.cs`; middleware and controllers
  should keep returning that shape consistently.
- API-local contracts live in `../Contracts/`; feature response DTOs returned today are also
  shaped by `../../../application/QuranDashboard.Application.Abstractions/**/Responses/`.
- Per-action work here is HTTP-only: route binding, query parsing, status-code selection,
  and mapping handler outcomes to `ApiResponse<T>`.

## Invariants

- Route bases here are public API surface; renaming a path segment is a contract change.
- Validation failures map to `400`, missing resources to `404`, and successful reads to `200`.
- Unhandled exceptions should stay outside controllers and flow through the global exception
  handler so the API still returns the shared envelope.
- Rate-limited requests are rejected by middleware **before** reaching a controller and return
  `429` with the same `ApiResponse` failure envelope plus a `Retry-After` header. The limiter is
  per-client-IP with separate general and health profiles; see `../README.md` (Rate Limiting) and
  `../../../.architecture/API_GUIDELINES.md` §14.

## Generated contract artifacts

- The OpenAPI spec for this API is exported offline to
  `Frontend/quran-dashboard-ui/openapi/swagger.json` by `Backend/scripts/export-swagger`
  (Swashbuckle CLI; no running server). Controller (endpoint) XML docs are the source of the endpoint descriptions in that spec; response DTO schemas are intentionally undocumented (bare typed schemas). Keep the controller docs accurate.
- Frontend payload types are generated from that spec into
  `Frontend/quran-dashboard-ui/src/app/core/api/generated/` (models-only consumption), and a
  static human-browsable reference is generated at `docs/api-reference/index.html`.
  `Backend/scripts/check-api-contract` detects stale generated output.
- Typed non-200 response schemas (`[ProducesResponseType]` for 400/404/500) are a recorded
  follow-up; today error codes are documented via XML `<response>` tags only, and all error
  bodies still use the shared `ApiResponse<T>` envelope.

## Related

- API root: `../README.md`
- Contract envelope: `../Contracts/ApiResponse.cs`
- Read-model counterparts: `../../../infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/`
