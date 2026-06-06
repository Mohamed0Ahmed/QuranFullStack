# Backend Structure Guide

## Purpose

This document defines how Backend files and folders should be organized in the
Quran Dashboard backend.

Agents must read this before adding or moving backend files.

## Canonical Ownership

To avoid drift between the backend architecture docs:

- **This file (`BACKEND_STRUCTURE.md`)** is canonical for backend file/folder
  placement, feature/domain organization, global usings placement, and
  file-size/responsibility thresholds.
- **`CLEAN_ARCHITECTURE.md`** is canonical for layer responsibilities, dependency
  direction, and request/use-case flow.
- **`API_GUIDELINES.md`** is canonical for the API boundary, HTTP behavior,
  response shape, and API localization/message rules.

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

## Global Usings

- Each C# project in the Backend may have its own `GlobalUsings.cs`.
- Do not use one shared GlobalUsings file across all Backend projects.
- Use project-local global usings only for namespaces that are common and
  repeated across many files in the same project.
- Keep each `GlobalUsings.cs` small, intentional, and layer-safe.
- Do not add feature-specific namespaces to `GlobalUsings.cs`.
- Do not use global usings to hide architectural dependencies.
- Global usings must respect Clean Architecture boundaries.

Layer-specific examples:

- API project may include:

  ```csharp
  global using Microsoft.AspNetCore.Mvc;
  ```

- Infrastructure project may include:

  ```csharp
  global using Microsoft.EntityFrameworkCore;
  ```

- Application project may include shared result or application abstractions only
  if they are used broadly.

Layer restrictions:

- Domain project must not include:
  - ASP.NET Core namespaces
  - EF Core namespaces
  - Infrastructure namespaces
  - Application namespaces
- Application project must not include:
  - Infrastructure namespaces
  - ASP.NET Core namespaces unless there is an explicit approved reason
- Infrastructure project must not leak infrastructure namespaces into Domain or
  Application.

Decision rule before adding a global using:

1. Is this namespace used repeatedly across many files in this same project?
2. Is it safe for this layer?
3. Does it hide a dependency that should remain explicit?
4. Is it feature-specific? If yes, keep it as a normal local using.

Preferred file placement:

```text
api/QuranDashboard.Api/GlobalUsings.cs
domain/QuranDashboard.Domain/GlobalUsings.cs
application/QuranDashboard.Application/GlobalUsings.cs
application/QuranDashboard.Application.Abstractions/GlobalUsings.cs
infrastructure/QuranDashboard.Infrastructure/GlobalUsings.cs
shared/QuranDashboard.Shared/GlobalUsings.cs
```

If a project has no repeated common usings yet, it does not need a
`GlobalUsings.cs` file.

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

## File Size and Responsibility Guidelines

File size limits here are **review thresholds, not blind automatic failures**. A
file that exceeds a threshold is a strong signal that responsibilities may be
mixed and should be reviewed and justified — not a number to satisfy mechanically.

Principles:

- File size limits are review thresholds, not blind automatic failures.
- A file exceeding the threshold is a strong signal that responsibilities may be
  mixed.
- The agent must not create very large files without explaining why.
- Never create files with thousands of lines.
- A 1000+ line service/component is almost always a design smell.
- A 3000+ line service/component is not acceptable and must be split before
  completion.
- Prefer cohesive small files organized by feature / domain / bounded context.
- Split by responsibility, not by technical dumping folders (see the dumping-folder
  rules earlier in this document).

### Backend thresholds

Thresholds are line counts per file. A **soft** threshold means "review and
justify"; a **hard** threshold means "stop and split, or split immediately".

#### 1. Controllers / API endpoints

- Ideal: 80–150 lines
- Soft review threshold: 200 lines
- Hard review threshold: 300 lines

Rules:

- Controllers / endpoints must stay thin.
- If a controller exceeds the threshold, move logic into Application use cases.
- Controllers must not contain business logic, EF queries, file parsing, or Quranic
  data processing.

#### 2. Application command / query handlers

- Ideal: 80–180 lines
- Soft review threshold: 250 lines
- Hard review threshold: 350 lines

Rules:

- Handlers should orchestrate one use case.
- If a handler grows too large, split private logic into focused domain/application
  services, validators, mappers, or helper classes near the feature.
- Do not hide multiple use cases inside one handler.

#### 3. Application services / Domain services

- Ideal: 150–250 lines
- Soft review threshold: 300 lines
- Hard review threshold: 450 lines

Rules:

- Services must have one clear reason to change.
- Avoid oversized services.
- If a service approaches the hard threshold, split by responsibility or workflow.
- A service over 1000 lines is not acceptable without explicit human approval and
  should normally be refactored before finishing.

#### 4. Repository implementations / read services

- Ideal: 150–300 lines
- Soft review threshold: 400 lines
- Hard review threshold: 600 lines

Rules:

- Repositories / read services may be larger because of queries, but must remain
  focused.
- Split large repositories by aggregate, feature, read model, or use case.
- Do not create a single repository that owns unrelated data access for many
  domains.

#### 5. Domain entities / aggregates

- Ideal: 100–250 lines
- Soft review threshold: 300 lines
- Hard review threshold: 500 lines

Rules:

- Entities may contain domain behavior, but should remain cohesive.
- If an entity becomes too large, check whether behavior belongs in value objects,
  domain services, or smaller aggregates.

#### 6. DTOs / contracts / models

- Ideal: small and focused
- Soft review threshold: 150 lines
- Hard review threshold: 250 lines

Rules:

- Do not combine many unrelated contracts in one file.
- Prefer one focused contract per file when it improves clarity.

### Backend review behavior

If a backend file is expected to exceed its **soft** threshold, the agent must:

- mention it in the plan or final response
- explain why the size is justified
- explain why splitting is not better

If a backend file is expected to exceed its **hard** threshold, the agent must:

- stop and propose a split before implementing, or
- split the file immediately into cohesive smaller files

If a backend file would exceed **1000 lines**:

- do not proceed without explicit human approval
- propose a concrete split plan
