# HTTP API — route families

Index only — defers to the linked code. See [docs/contracts/README.md](./README.md).

Current HTTP route families, status mapping, and the `ApiResponse<T>` envelope are
defined by the controller and contract code. This page does **not** restate routes,
parameters, or payloads.

## Authoritative sources

- Controllers (actual routes) → [`Controllers/`](../../Backend/api/QuranDashboard.Api/Controllers/) — `Abwab/`, `Words/`, `Quran/MushafReader/`, [`Quran/PhraseSearch/`](../../Backend/api/QuranDashboard.Api/Controllers/Quran/PhraseSearch/), `Dashboard/`, `System/`, `Access/`
- Authentication and authorization boundary → [`Authentication/`](../../Backend/api/QuranDashboard.Api/Authentication/)
- Response envelope → [response-envelope.md](./response-envelope.md)
- Machine contract (generated) → [`Frontend/quran-dashboard-ui/openapi/swagger.json`](../../Frontend/quran-dashboard-ui/openapi/swagger.json), exported by `Backend/scripts/export-swagger`
- Human-browsable reference → generate it on demand with `npm run docs:api` from `Frontend/quran-dashboard-ui/`. It is **not** committed: a generated bundle that nobody regenerates is stale data pretending to be a contract. `Backend/scripts/check-api-contract` verifies the spec and the generated client instead, which is the part that can actually break a caller

**Precedence:** the controller and contract code win over any other description.
The generated spec and reference are machine expressions of that code, not new authorities;
`Backend/scripts/check-api-contract` keeps them in sync.
