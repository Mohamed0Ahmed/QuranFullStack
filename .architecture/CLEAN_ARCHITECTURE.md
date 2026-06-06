# Backend Clean Architecture Guide

## Purpose

This document defines how Clean Architecture should be applied in the Quran
Dashboard backend.

It explains:

- layer responsibilities
- dependency direction
- request flow
- where business logic belongs
- where data access belongs
- how backend features should be added

For file/folder organization rules, also read:

- `.architecture/BACKEND_STRUCTURE.md`

## Canonical Ownership

To avoid drift between the backend architecture docs:

- **This file (`CLEAN_ARCHITECTURE.md`)** is canonical for layer responsibilities,
  dependency direction, and request/use-case flow.
- **`BACKEND_STRUCTURE.md`** is canonical for file/folder placement and
  file-size/responsibility thresholds.
- **`API_GUIDELINES.md`** is canonical for the API boundary and
  response/localization rules.

## Projects

The backend uses these projects:

- `api/QuranDashboard.Api`
- `domain/QuranDashboard.Domain`
- `application/QuranDashboard.Application`
- `application/QuranDashboard.Application.Abstractions`
- `infrastructure/QuranDashboard.Infrastructure`
- `shared/QuranDashboard.Shared`

## Dependency Direction

Allowed dependency direction:

- `QuranDashboard.Api` may reference:
  - `QuranDashboard.Application`
  - `QuranDashboard.Application.Abstractions`
  - `QuranDashboard.Infrastructure`
  - `QuranDashboard.Shared`

- `QuranDashboard.Application` may reference:
  - `QuranDashboard.Application.Abstractions`
  - `QuranDashboard.Domain`
  - `QuranDashboard.Shared`

- `QuranDashboard.Application.Abstractions` may reference:
  - `QuranDashboard.Domain`
  - `QuranDashboard.Shared`

- `QuranDashboard.Infrastructure` may reference:
  - `QuranDashboard.Application.Abstractions`
  - `QuranDashboard.Domain`
  - `QuranDashboard.Shared`

- `QuranDashboard.Domain` must not depend on other backend projects.

- `QuranDashboard.Shared` must stay independent and must not become a dumping
  ground.

Forbidden:

- Domain referencing Application, Infrastructure, or Api.
- Application referencing Infrastructure or Api.
- Application.Abstractions referencing Infrastructure or Api.
- Infrastructure leaking EF Core or external dependencies into Domain/Application
  contracts.
- Api containing business logic or data-processing logic.

## Layer Responsibilities

## Domain

Domain is the core business model.

Allowed in Domain:

- entities
- value objects
- domain enums
- domain exceptions
- domain rules
- domain events if needed

Forbidden in Domain:

- EF Core
- DbContext
- database access
- file system access
- HTTP concepts
- API contracts
- external service clients
- JSON/file parsing infrastructure

Examples:

- Quran page number validation can be a Domain value object/rule.
- A `LineType` enum belongs near the Quran/MushafPages domain that owns it.
- A `GateRelationType` belongs near Gates if Gates own that concept.

## Application

Application contains use cases and orchestration.

Allowed in Application:

- commands
- queries
- handlers
- use-case services
- application responses/results
- application validation
- mapping between domain/application responses when needed

Forbidden in Application:

- EF Core implementation details
- direct DbContext usage unless explicitly approved
- file system access
- HTTP response handling
- controller logic
- infrastructure-specific code

Application should depend on abstractions, not infrastructure implementations.

Preferred use-case structure:

```text
Application/
  MushafPages/
    Queries/
      GetMushafPage/
        GetMushafPageQuery.cs
        GetMushafPageHandler.cs
        GetMushafPageResponse.cs
```

## Application.Abstractions

Application.Abstractions contains contracts needed by Application and implemented
by Infrastructure.

Allowed:

- repository interfaces
- read service interfaces
- source file reader interfaces
- report writer interfaces
- clock/time provider interfaces
- external integration abstractions

Rules:

- Keep interfaces focused.
- Avoid one large interface that mixes unrelated features.
- Do not expose EF Core types in abstractions.
- Do not expose infrastructure-specific implementation details.

Example:

```csharp
public interface IMushafPageReadRepository
{
    Task<MushafPageDetails?> GetPageAsync(
        int pageNumber,
        CancellationToken cancellationToken);
}
```

## Infrastructure

Infrastructure contains implementations of external concerns.

Allowed:

- EF Core DbContext
- EF Core configurations
- migrations
- repository implementations
- file readers
- report writers
- external API clients
- dependency injection wiring

Forbidden:

- domain rules that should live in Domain
- use-case orchestration that should live in Application
- API/controller logic

Rules:

- Infrastructure implements Application.Abstractions contracts.
- Infrastructure may depend on EF Core, file system APIs, PostgreSQL libraries, and
  external clients.
- Infrastructure dependencies must not leak into Domain or Application contracts.

## Api

Api is the HTTP entry point.

Allowed:

- controllers/endpoints
- request/response HTTP contracts
- middleware
- API extensions
- Swagger/OpenAPI configuration
- dependency injection composition

Forbidden:

- business logic
- EF queries
- direct file parsing
- validation report generation
- domain/data processing logic

Controllers/endpoints should be thin:

1. receive request
2. call Application use case
3. map result to HTTP response

## Shared

Shared is for truly cross-layer primitives only.

Allowed examples:

- Result
- Error
- Maybe/Option if used consistently
- tiny cross-layer constants if truly global

Forbidden:

- feature-specific enums
- domain models
- random helpers
- DTO dumping
- anything that belongs to one layer or feature only

## Request Flow

Typical read-only request flow:

1. HTTP request reaches Api controller/endpoint.
2. Api creates/calls an Application query or use case.
3. Application handler validates/orchestrates the use case.
4. Application handler calls an abstraction from Application.Abstractions.
5. Infrastructure implementation retrieves data from DB/files/external source.
6. Application returns a response/result.
7. Api maps it to HTTP.

Example:

GET `/api/mushaf/pages/5`

Flow:

- `MushafPagesController`
- `GetMushafPageQuery`
- `GetMushafPageHandler`
- `IMushafPageReadRepository`
- `MushafPageReadRepository`
- response returned to API

## Repository and Data Access Policy

Default policy: use focused read repositories or read services behind
Application.Abstractions for feature data access.

Reason: the Quran Dashboard will likely read from multiple sources:

- PostgreSQL
- JSON
- SQLite
- derived files
- imported resources

Application should not be tied early to a specific storage mechanism.

Rules:

- Do not inject Infrastructure implementations directly into Application.
- Do not expose DbContext to Application unless explicitly approved.
- Prefer focused interfaces such as:
  - `IMushafPageReadRepository`
  - `IQuranWordReadRepository`
  - `IGateReadRepository`

Avoid broad generic repositories unless there is a clear reason.

## Where Logic Belongs

Domain:

- core business rules
- invariants
- value object validation

Application:

- use-case orchestration
- application-level validation
- combining data from multiple abstractions
- deciding response shape for a use case

Infrastructure:

- how data is loaded/saved
- EF/file/external system implementation details

Api:

- HTTP routing
- status codes
- request/response mapping

Frontend:

- not relevant here; do not put backend rules in frontend.

## Quranic Data Safety

Quranic data is source-sensitive.

Rules:

- Never invent Quran text, ayah text, tafsir, translations, morphology,
  gates/topics, or religious content.
- Never silently modify source data.
- Do not hide data problems.
- Preserve traceability from generated data back to source files.
- Data processors/importers/generators must produce clear reports with:
  - totals
  - missing records
  - duplicates
  - warnings
  - validation result

## Feature Implementation Pattern

Before adding a backend feature:

1. Identify the feature/bounded context.
2. Check `.architecture/BACKEND_STRUCTURE.md` for file placement.
3. Add Domain types only if there is real domain behavior or a clear domain
   concept.
4. Add Application use case under the feature folder.
5. Add abstractions needed by Application.
6. Add Infrastructure implementation for those abstractions.
7. Add Api endpoint/controller as a thin entry point.
8. Add tests/verification when relevant.
9. Report changed files and build/test status.

## Example: Mushaf Page Viewer Read-only

Possible structure:

Domain:

- `Domain/Quran/MushafPages/MushafPageNumber.cs`
- `Domain/Quran/MushafPages/LineType.cs`

Application.Abstractions:

- `Application.Abstractions/Quran/MushafPages/IMushafPageReadRepository.cs`

Application:

- `Application/MushafPages/Queries/GetMushafPage/GetMushafPageQuery.cs`
- `Application/MushafPages/Queries/GetMushafPage/GetMushafPageHandler.cs`
- `Application/MushafPages/Queries/GetMushafPage/GetMushafPageResponse.cs`

Infrastructure:

- `Infrastructure/Persistence/Repositories/Quran/MushafPageReadRepository.cs`
  or, if file-backed:
- `Infrastructure/Files/Quran/MushafPages/JsonMushafPageReadRepository.cs`

Api:

- `Api/Controllers/MushafPagesController.cs`
  or
- `Api/Endpoints/MushafPages/GetMushafPageEndpoint.cs`

Important: only add files that are needed by the current feature. Do not create
broad empty folder structures in advance.

## Global Usings

GlobalUsings rules are defined in:

- `.architecture/BACKEND_STRUCTURE.md`

Important summary:

- Each C# project may have its own `GlobalUsings.cs`.
- Keep it small and layer-safe.
- Do not use it to hide architectural dependencies.

## Definition of Done

For backend implementation work, the final summary should include:

- changed files
- build status
- test status if applicable
- validation/report path if data-related
- any skipped or uncertain items
