# Abwab — gates tree, relations, templates

Index only — defers to the linked code + README, which are the authority. See [docs/contracts/README.md](./README.md).

Covers the أبواب curation surface: the sections/doors tree snapshot, door relations,
door templates and their apply, and the frontend page's URL-state and caching. This
page does **not** restate routes, DTO fields, refusal statuses, or cache rules.

## Authoritative sources

- Frontend feature (routes, URL-state, overlays, cache) → [`features/abwab/README.md`](../../Frontend/quran-dashboard-ui/src/app/features/abwab/README.md)
- Write models (writers, transactions, exception translation) → [`Persistence/Writes/Abwab/README.md`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/README.md)
- Read models (tree snapshot, relations, templates, conditional GET) → [`Persistence/Reads/Abwab/README.md`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/README.md)
- HTTP endpoints and failure statuses → [`Controllers/Abwab/`](../../Backend/api/QuranDashboard.Api/Controllers/Abwab/), [`Controllers/README.md`](../../Backend/api/QuranDashboard.Api/Controllers/README.md) and [http-api.md](./http-api.md)

**Precedence:** backend code + the writes/reads READMEs win for the API contract; the
frontend README wins for URL-state and page behaviour.
