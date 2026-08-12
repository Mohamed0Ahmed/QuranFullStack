# Abwab — gates tree, relations, templates

Index only — defers to the linked code. See [docs/contracts/README.md](./README.md).

Covers the أبواب curation surface: the sections/doors tree snapshot, door relations,
door templates and their apply, and the frontend page's URL-state and caching. This
page does **not** restate routes, DTO fields, refusal statuses, or cache rules.

## Authoritative sources

- Frontend feature (routes, URL-state, overlays, cache) → [`features/abwab/`](../../Frontend/quran-dashboard-ui/src/app/features/abwab/)
- Write models (writers, transactions, exception translation) → [`Persistence/Writes/Abwab/`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/)
- Read models (tree snapshot, relations, templates, conditional GET) → [`Persistence/Reads/Abwab/`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/)
- HTTP endpoints and failure statuses → [`Controllers/Abwab/`](../../Backend/api/QuranDashboard.Api/Controllers/Abwab/) and [http-api.md](./http-api.md)
- Write-access classification → [security-access.md](./security-access.md)

**Precedence:** backend code wins for the API contract; frontend code wins for URL-state and
page behaviour.
