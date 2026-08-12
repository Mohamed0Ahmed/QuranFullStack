# Backend API Guidelines

## Purpose

This document defines lightweight but clear API boundary rules for the Quran
Dashboard backend.

It applies to the **API boundary only**:

- endpoints
- controllers
- API request/response contracts
- API error handling
- middleware
- Swagger/OpenAPI
- API configuration such as CORS, rate limiting, auth, versioning, and health
  checks

Read this file before adding or changing any of the above.

This file does **not** need to be read for pure Domain, Infrastructure, EF
configuration, repository implementation, or internal Application handler work,
**unless** that work changes API behavior (for example, changing a use-case
response shape that is exposed through an endpoint, or changing an error result
that maps to an HTTP status code).

For broader rules, also read:

- `.architecture/BACKEND_STRUCTURE.md`
- `.architecture/CLEAN_ARCHITECTURE.md`

## 1. API Layer Responsibility

The API layer is a **thin boundary**.

It may:

- receive HTTP requests
- bind route/query/body values
- call Application use cases
- map Application results to HTTP responses
- configure Swagger, middleware, DI, and the composition root

It must **not** contain:

- business logic
- EF Core queries
- file parsing
- Quranic data processing
- validation report generation
- direct Infrastructure usage inside controllers/endpoints

The Api project may reference Infrastructure **only** for the composition root /
DI wiring (registering implementations at startup). Controller and endpoint logic
must not use Infrastructure directly.

## 2. Endpoint Naming and Routes

- Use clear, resource-oriented routes.
- Prefer plural nouns for resource collections.
- Keep routes stable and predictable.
- Do not expose internal file names, database table names, or implementation
  details in routes.
- Use kebab-case **only if** the existing project route style already chooses it;
  otherwise stay consistent with the current project style. Do not mix styles.

Examples:

```text
GET /api/health
GET /api/dashboard/info
GET /api/mushaf/pages/{pageNumber}
```

## 3. HTTP Verbs

- `GET` for read-only operations.
- `POST` for commands, creation, and actions.
- `PUT` for full replacement when needed.
- `PATCH` for partial updates when needed.
- `DELETE` for delete/archive operations when needed.

Do not use `GET` for operations that mutate state.

## 4. Status Codes

Lightweight rules:

- `200 OK` — successful reads/commands that return a body.
- `201 Created` — successful creation when a resource is created.
- `204 No Content` — only when intentionally returning no body.
- `304 Not Modified` — conditional GETs only, in response to an `If-None-Match` that
  matches the current validator. No body, ever; `ETag` and `Cache-Control` headers are
  required on it. The second sanctioned bodiless status after `204`, and the first that
  is not the success of a write — see section 5.
- `400 Bad Request` — invalid input.
- `401 Unauthorized` — missing/invalid authentication (later).
- `403 Forbidden` — authenticated but not allowed (later).
- `404 Not Found` — missing resources.
- `409 Conflict` — business conflicts.
- `422 Unprocessable Entity` — may be used for validation **if** the project
  chooses it; keep the choice consistent across the API.
- `500` — produced by global exception handling, **not** thrown manually from
  normal business flow.

## 5. Response Shape

Document the current preferred approach without over-engineering. We already have
an `ApiResponse` concept; keep user-facing responses consistent with it.

Preferred success shape:

```json
{
  "isSuccess": true,
  "message": "تمت العملية بنجاح",
  "data": {}
}
```

Preferred failure shape:

```json
{
  "isSuccess": false,
  "message": "حدث خطأ",
  "errors": []
}
```

Rules:

- Property names stay **English** (`isSuccess`, `message`, `data`, `errors`).
- User-facing `message` values are **localized** (Arabic by default).
- Arabic is the default language; English must be supportable later.
- Error statuses reuse this same failure envelope. A rate-limited request returns
  **`429 Too Many Requests`** with the failure shape (`isSuccess:false`, Arabic
  `message`, `errors:[]`) plus a `Retry-After` header — see section 14.
- Do not scatter hardcoded repeated user-facing messages across
  controllers/handlers/services.
- Use message keys/resources/constants close to the owning feature when possible.
- Shared messages only go to a truly shared/common location.

### Conditional GETs

A read may support revalidation. When it does, these rules bind — the envelope itself does
not change, the rule is scoped to the conditional read:

- Every `200` from a conditional read carries `ETag` plus `Cache-Control: no-store`. The
  `no-store` is load-bearing: without it an `ETag`-bearing response is heuristically
  revalidatable by the browser's own cache, which becomes a second, invisible validator layer
  racing the client's explicit one.
- A matching `If-None-Match` returns `304` with **no body** and the same two headers. The
  `304` path must not run the query — a revalidation that still reads the database has bought
  nothing. One scoped exception: a **per-resource** validator embeds no existence — an
  `abwab-template-{id}-…` value is derivable for an id that never existed — so the template
  detail read answers existence first and takes the `304` branch only on a found row
  (`AbwabTemplatesController.Get`); its warm-path revalidation is served by the template
  cache. List-shaped validators (the tree, the templates list) keep the pre-query
  short-circuit: a list always exists.
- **Validators are opaque server-side generations, never derived from row data.** A validator
  built out of a payload hash or a data-derived version field turns a diagnostics field into a
  concurrency-adjacent one; keep the two apart.
- Comparison is **exact ordinal match against a member** of the request's list, and
  **fail-open**: an absent, malformed, or `*` header earns a full `200`. `*` is deliberately
  not given its RFC 9110 meaning — for a single first-party client, a mis-sent header should
  cost a body, never a stale representation.
- A `404` carries no validator headers: an absence has no representation to validate.
- The abwab reads are the implementation today. The operational **single-instance constraint and
  its migration path** live in `Backend/README.md` §Deployment; read that section before adding a
  second instance or a second conditional read.

## 6. Error Handling

- Do not catch exceptions in every controller.
- Prefer centralized/global exception handling.
- Do not leak stack traces, file paths, SQL errors, or internal details to API
  clients.
- Represent Application/Domain expected failures as controlled results (not
  unhandled exceptions) where practical.
- Log unexpected exceptions and convert them to a safe response.

## 7. Validation

- The API layer can perform basic binding and shape validation.
- The Application layer owns use-case validation.
- The Domain owns invariants.
- Controllers/endpoints must not contain business validation logic.
- Validation responses must be consistent and localizable.

## 8. Request/Response Contracts

- API contracts belong near the API feature/endpoint, **not** in Domain.
- Do not expose EF entities or Domain entities directly as API responses.
- Use DTOs/contracts for the external API shape.
- Keep API contracts stable and intentional.
- Do not expose raw Quranic internal processing fields unless the endpoint is
  explicitly for internal review/debug and is clearly named as such.

## 9. Swagger / OpenAPI

- Swagger descriptions should be clear.
- Do not document fake features.
- Do not include placeholder endpoints.
- Do not expose sensitive/internal-only endpoints accidentally.
- Keep examples realistic and safe.
- If an endpoint becomes admin/internal-only later, document that clearly.

## 10. Localization and Messages

This section is the canonical home for backend API localization and user-facing
message rules. `Backend/AGENTS.md` and `Backend/CLAUDE.md` point here.

- Arabic is the default response language.
- English support is required later for visitor-facing responses.
- If a language is missing or unsupported, fall back to Arabic.
- API property names and code identifiers stay **English**.
- Message values shown to users/admins must be localizable.
- Do not return repeated hardcoded user-facing success, error, validation,
  warning, or notification messages from controllers, handlers, services,
  validators, or middleware.
- Centralize reusable messages via message keys/resources/constants close to the
  owning feature; truly shared messages go to a shared/common location. Prefer
  keys such as `Common.NotFound`, `Common.ValidationFailed`,
  `MushafPages.InvalidPageNumber`, `Gates.CreatedSuccessfully`.
- Do not create broad dumping folders for unrelated messages.
- Technical protocol strings such as `Authorization`, `Bearer`,
  `application/json`, `GET`, and `POST` are **not** user-facing messages.
- Do not invent Quranic/religious content while writing messages.

## 11. Security and Safety

- Never return sensitive internal details.
- Never return local file system paths.
- Never trust route/query/body input.
- Be careful with Quranic data: do not silently invent or correct Quranic text in
  API responses.
- If data is missing, return a clear controlled response.
- Admin-only behavior must not be exposed as public endpoints later without
  authorization.

### Authorization classification

- Every `POST`, `PUT`, `PATCH`, and `DELETE` controller action must carry exactly one known
  `[RequirePermission(...)]` or `[RequireOwner]` requirement. Bare `[Authorize]`, an unknown
  permission code, conflicting authorization metadata, and an anonymous unsafe action are invalid.
  Startup metadata validation and route-smoke parity tests enforce that classification.
- Public content `GET`s remain the exception. `GET /api/access/me` is authenticated-only, and every
  access-administration endpoint, including security `GET`s, is Owner-only.
- One centralized authorization rejection writer owns the shared `ApiResponse` failure envelope:
  challenge is `401`, an active caller lacking the required access is `403`, and authorization-state
  infrastructure failure is `503`.

## 12. Health Checks and Diagnostics

- Health endpoints should be minimal.
- Diagnostics/debug endpoints must not expose sensitive data.
- Development-only diagnostics must not be enabled in production by accident.

## 13. API Versioning

Do not implement versioning now.

- If/when API versioning is added, configure it centrally.
- Do not mix versioning styles randomly.
- Existing routes should stay stable unless a versioning plan is agreed.

## 14. Middleware and Configuration

- Middleware/configuration files are allowed in the Api layer for
  composition/setup.
- They must not contain business logic.
- CORS, auth, rate limiting, Swagger, exception handling, and health checks should
  be configured **centrally and consistently**.
- SignalR hubs (later) should be thin and delegate real logic to Application.

### Rate limiting

Configured centrally in `RateLimiting/` (options, IP resolver, request classifier,
rejection writer, registration) and wired via `AddRateLimiting` + `UseRateLimiter`
(after CORS, in the reserved pre-auth slot). Two per-client-IP profiles, selected by a
single global partitioner with **namespaced partition keys** so the two profiles for the
same IP never share a cached limiter:

- **General** — token bucket over **all non-exempt requests except `/api/health*`**.
  Default `TokenLimit=30`, `TokensPerPeriod=30`, `ReplenishmentPeriodSeconds=15`,
  `QueueLimit=0` → sustained **120 req/min/IP**, burst 30. Key `general:{ip}`.
- **Health** — fixed window over `/api/health*` only (it runs a DB health check, so it is
  bounded per-IP rather than fully exempt). Default `HealthPermitLimit=300` /
  `HealthWindowSeconds=60`. Key `health:{ip}`.

Rules and invariants:

- **Client IP** comes from the configurable single-valued `ClientIpHeaderName` (default
  Railway `X-Real-IP`) → `RemoteIpAddress` → `"unknown"`. No `X-Forwarded-For` list
  parsing; no `ForwardedHeaders` middleware.
- **Exemptions (no limiter):** any `OPTIONS`, and `/swagger*` in Development only.
- **Rejections** return `429` with the shared `ApiResponse` failure envelope and a
  `Retry-After` header (section 5).
- **Secure by default:** `RateLimiting:Enabled` ships **`false`** in base, Development,
  and Production `appsettings`; it is enabled via environment override only after the
  deploy-time `X-Real-IP` trust and health-probe verification gates. Invalid config
  **fails fast** at startup.
- **Per-instance:** the limiter is in-memory per process; with N Railway instances the
  effective limits are N× the configured values (acceptable at single-instance; a
  distributed store is future work).

## 15. Definition of Done for API Changes

Any API change should report:

- endpoints added/changed
- request/response contracts added/changed
- status codes used
- localization/message impact
- Swagger impact
- build status
- test status (if tests exist or were added)
