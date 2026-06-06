# Backend Structure Guide

## Purpose

This document defines how Backend files and folders should be organized in the
Quran Dashboard backend.

Agents must read this before adding or moving backend files.

## Main Rule

Organize by domain, feature, and bounded context first.

Do not organize unrelated files by technical type.

## Avoid Technical Dumping Folders

Avoid global dumping folders such as:

- `Enums/`
- `Models/`
- `DTOs/`
- `Helpers/`
- `Utils/`
- `Services/`

unless the files inside are truly shared, cross-cutting, and very small in number.

Bad example:

```text
Domain/
  Enums/
  Models/
  Helpers/
```

Good principle: a type should live near the domain feature, entity, or use case
that owns it.

Examples:

- `ApprovalStatus` belongs with Approvals.
- `ActivityType` belongs with Audit.
- `LineType` belongs with Quran/MushafPages or Quran/Lines.
- `WordSortBy` belongs with Words or QuranWords.
- `GateRelationType` belongs with Gates.
- `GateProtectionMode` belongs with Gates, or the feature that owns gate
  protection.

## Domain Layer

Domain must stay independent.

Allowed:

- Entities
- Value objects
- Domain enums
- Domain exceptions
- Domain rules
- Domain events if needed

Not allowed:

- EF Core
- API contracts
- HTTP concepts
- File system access
- Database access
- External service integrations

Preferred organization:

```text
Domain/
  Common/
    Entity.cs
    ValueObject.cs
    DomainException.cs

  Quran/
    Surahs/
    Ayahs/
    MushafPages/
    Words/

  Gates/
  Approvals/
  Audit/
```

Important: `Common` is allowed only for true base primitives. Do not turn `Common`
or `Shared` into a dumping ground.

If a domain folder grows, split it internally by subdomain or feature.

Example:

```text
Quran/
  Surahs/
  Ayahs/
  MushafPages/
  Words/
  Audio/
  Tafsir/
  Translations/
```

## Application.Abstractions Layer

This layer contains contracts needed by Application.

Examples:

- Persistence abstractions
- File reader abstractions
- Report writer abstractions
- External integration abstractions

Preferred organization:

```text
Application.Abstractions/
  Persistence/
  Files/
  Reports/
  Quran/
  Gates/
```

Avoid one large unrelated `Interfaces` folder if many contracts exist.

## Application Layer

Application contains use cases and orchestration.

Prefer grouping by feature/use case.

Good example:

```text
Application/
  MushafPages/
    Queries/
      GetMushafPage/
        GetMushafPageQuery.cs
        GetMushafPageHandler.cs
        GetMushafPageResponse.cs

  QuranWords/
    Queries/

  Gates/
    Commands/
    Queries/
```

Rules:

- Commands, queries, handlers, validators, and responses should stay near the use
  case they belong to.
- Avoid global `DTOs`, `Services`, `Validators`, or `Handlers` folders unless truly
  shared and small.
- Application must not depend on Infrastructure.
- Application should depend on abstractions.

## Infrastructure Layer

Infrastructure contains implementations.

Allowed:

- EF Core DbContext
- EF configurations
- Migrations
- File readers
- Report writers
- External service clients
- Dependency injection wiring

Preferred organization:

```text
Infrastructure/
  Persistence/
    QuranDashboardDbContext.cs
    Configurations/
      Quran/
      Gates/
      Approvals/
    Migrations/

  Files/
  Reports/
  DependencyInjection.cs
```

Rules:

- EF configurations should be grouped by feature/domain when many entities exist.
- Avoid one huge flat `Configurations` folder if it becomes hard to navigate.
- Do not mix domain logic into infrastructure implementations.

## API Layer

API is the entry point only.

Allowed:

- Controllers or endpoints
- Request/response contracts
- Middleware
- API extensions
- Swagger/API configuration

Rules:

- Controllers must stay thin.
- No business logic in controllers.
- No data processing hidden in controllers.
- Endpoints/controllers should be grouped by feature.
- API contracts should live near endpoints or feature folders when possible.

## Shared Layer

Shared is only for truly cross-layer, cross-project primitives.

Allowed examples:

- `Result`
- `Error`
- Very small shared constants if truly global

Not allowed:

- Feature-specific enums
- Domain-specific models
- Random helpers
- Anything that belongs to one feature only

## File Placement Decision Rule

Before creating a file, ask:

1. Which domain or feature owns this?
2. Is it truly shared?
3. Is it part of a use case?
4. Is it infrastructure implementation?
5. Is it API-only?

Then place it accordingly.

## Examples

Bad:

```text
Domain/Enums/LineType.cs
```

Good:

```text
Domain/Quran/MushafPages/LineType.cs
```

Bad:

```text
Application/DTOs/GetMushafPageResponse.cs
```

Good:

```text
Application/MushafPages/Queries/GetMushafPage/GetMushafPageResponse.cs
```

Bad:

```text
Domain/Helpers/QuranLocationHelper.cs
```

Good:

```text
Domain/Quran/Words/WordLocation.cs
```

or `Application/QuranWords/...` depending on ownership.

## When Unsure

Do not create a global folder.

Choose the closest feature/domain folder. If the file is truly shared, document why
it belongs in `Common` or `Shared`.

## Required Behavior for Agents

When adding backend files:

- Keep changes focused.
- Do not create broad technical dumping folders.
- Do not move files across layers without reason.
- Do not introduce infrastructure dependencies into Domain or Application.
- Do not invent Quranic data.
- Preserve traceability for generated/imported data.
