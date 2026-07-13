# API controllers

HTTP entry points for `QuranDashboard.Api`. This folder owns route groups, HTTP status mapping,
and the `ApiResponse<T>` envelope; application handlers own use-case logic.

## Route families

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

## Related

- API root: `../README.md`
- Contract envelope: `../Contracts/ApiResponse.cs`
- Read-model counterparts: `../../../infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/`
