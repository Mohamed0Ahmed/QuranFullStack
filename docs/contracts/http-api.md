# HTTP API — route families

Index only — defers to the linked code + README, which are the authority. See [docs/contracts/README.md](./README.md).

Current HTTP route families, status mapping, and the `ApiResponse<T>` envelope are
defined by the controllers and the API README. This page does **not** restate routes,
parameters, or payloads.

## Authoritative sources

- Route families overview → [`Controllers/README.md`](../../Backend/api/QuranDashboard.Api/Controllers/README.md) ("Route families" section)
- Controllers (actual routes) → [`Controllers/`](../../Backend/api/QuranDashboard.Api/Controllers/) — `Words/`, `MushafReader/`, `Dashboard/`, `System/`
- API boundary rules (verbs, status codes, response shape) → [`API_GUIDELINES.md`](../../Backend/.architecture/API_GUIDELINES.md)
- API project overview → [`api/QuranDashboard.Api/README.md`](../../Backend/api/QuranDashboard.Api/README.md)
- Response envelope → [response-envelope.md](./response-envelope.md)
- Machine contract (generated) → [`Frontend/quran-dashboard-ui/openapi/swagger.json`](../../Frontend/quran-dashboard-ui/openapi/swagger.json), exported by `Backend/scripts/export-swagger`
- Human-browsable reference (generated) → [`docs/api-reference/index.html`](../api-reference/index.html), built by `npm run docs:api`

**Precedence:** the controller code + `Controllers/README.md` win over any other description.
The generated spec and reference are machine expressions of that code, not new authorities;
`Backend/scripts/check-api-contract` keeps them in sync.
