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
- Do not scatter hardcoded repeated user-facing messages across
  controllers/handlers/services.
- Use message keys/resources/constants close to the owning feature when possible.
- Shared messages only go to a truly shared/common location.

See also section 10 (Localization and Messages) and the localization rules in
`AGENTS.md` / `CLAUDE.md`.

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

- Arabic is the default response language.
- English support is required later for visitor-facing responses.
- API property names and code identifiers stay **English**.
- Message values shown to users/admins must be localizable.
- Avoid repeated hardcoded Arabic or English strings; centralize via message
  keys/resources/constants close to the owning feature.
- Technical protocol strings such as `Authorization`, `Bearer`,
  `application/json`, `GET`, and `POST` are **not** user-facing messages.

## 11. Security and Safety

- Never return sensitive internal details.
- Never return local file system paths.
- Never trust route/query/body input.
- Be careful with Quranic data: do not silently invent or correct Quranic text in
  API responses.
- If data is missing, return a clear controlled response.
- Admin-only behavior must not be exposed as public endpoints later without
  authorization.

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

## 15. Definition of Done for API Changes

Any API change should report:

- endpoints added/changed
- request/response contracts added/changed
- status codes used
- localization/message impact
- Swagger impact
- build status
- test status (if tests exist or were added)
