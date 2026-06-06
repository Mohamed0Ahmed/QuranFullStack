---
name: backend-structure-review
description: >-
  Review-only backend structure and Clean Architecture review for the Quran
  Dashboard .NET backend (App/Backend: Domain, Application,
  Application.Abstractions, Infrastructure, Api). Use this skill whenever the user
  asks to review backend file/folder organization, project structure,
  domain/feature foldering, Clean Architecture layering, or where an enum / value
  object / DTO / handler should live, or when new backend folders or files are
  added, even if they don't say the word "structure". It checks domain/feature
  (bounded-context) grouping over technical-type grouping, flags global dumping
  folders like Enums/Models/DTOs/Helpers/Utils, verifies layer dependency
  boundaries, and checks Quranic data safety, then returns a structured verdict.
  This is a review skill only: do not implement fixes unless the user explicitly
  asks.
---

# Backend Structure Review Skill

Use this skill to review backend file organization, Clean Architecture
boundaries, and domain/feature-based foldering for the Quran Dashboard backend.
Its main job is to keep the backend organized by domain/feature first and to
prevent technical-type dumping, such as global `Enums/`, `Models/`, `Helpers/`,
or `Utils/` folders that collect unrelated concepts.

This skill is review only. Do not implement fixes unless explicitly asked.

## Required Context

Before using this skill, rely on the relevant workspace/tool context already loaded by your agent (Claude, OpenCode, and Codex each load their own instruction files). Do not separately require reading those tool entrypoint files.

Read the stable reference documents this review needs:

- `CODING_PRINCIPLES.md`
- `Backend/.architecture/BACKEND_STRUCTURE.md`

If a referenced document is missing or unavailable, state that clearly in the output.

## Backend Context

- **Backend path:** `Backend/`
- **Stack:** .NET 10, ASP.NET Core Web API, EF Core, PostgreSQL, Code First,
  Clean Architecture style.
- **Current backend projects:**
  - `api/QuranDashboard.Api`
  - `domain/QuranDashboard.Domain`
  - `application/QuranDashboard.Application.Abstractions`
  - `application/QuranDashboard.Application`
  - `infrastructure/QuranDashboard.Infrastructure`
  - `shared/QuranDashboard.Shared`

## Important: the anti-pattern example below is external

The structure example in this section is an **external conceptual anti-pattern**
from another project. It is **not** necessarily present in this repository.

- Do **not** assume these folders or files exist in the current Backend.
- Do **not** search for them as current code.
- Use the example only to understand the kind of backend organization to avoid.

External conceptual anti-pattern example:

```text
Entities/
  Approvals/
  Audit/
  Content/
  Hierarchy/
  Identity/
  Navigation/
  Quran/
  Requests/

Enums/
  ActivityType.cs
  ApprovalStatus.cs
  AttributionType.cs
  CategoryProtectionMode.cs
  CategoryRelationshipType.cs
  CopyContentMode.cs
  HighlightTargetKind.cs
  LineType.cs
  NoteStatus.cs
  WordCustomGroupItemType.cs
  WordSortBy.cs
```

**Why this is a problem:** a global `Enums` folder becomes a dumping ground.
Related enum/type files are separated from the domain concept that owns them.

**Preferred principle:** organize by domain / feature / bounded context first, not
by technical type.

**Preferred rule:** place each enum, value object, domain type, and supporting
type next to the domain feature / entity / use case that owns it.

Examples of correct ownership:

- `ApprovalStatus` belongs with Approvals.
- `ActivityType` belongs with Audit.
- `CategoryProtectionMode` and `CategoryRelationshipType` belong with
  Categories / Gates / Hierarchy, depending on the actual domain naming.
- `LineType` belongs with Quran/MushafPages or Quran/Lines.
- `WordCustomGroupItemType` and `WordSortBy` belong with QuranWords/WordGroups or
  Words.
- `HighlightTargetKind` belongs with Highlighting, or the feature that owns
  highlights.
- `CopyContentMode` belongs with the copy/import/content operation that owns it.

Avoid global dumping folders like `Enums/`, `Models/`, `DTOs/`, `Helpers/`,
`Utils/` unless the files inside are truly shared, cross-cutting, and very small
in number.

## Backend Structure Rules

### 1. Domain organization

Inside Domain, group by bounded context / domain feature.

Good example:

```text
Domain/
  Common/
    Entity.cs
    ValueObject.cs
    DomainException.cs

  Quran/
    Surahs/
      Surah.cs
      RevelationPlace.cs
    Ayahs/
      Ayah.cs
      AyahText.cs
    MushafPages/
      MushafPage.cs
      MushafLine.cs
      LineType.cs
    Words/
      QuranWord.cs
      WordLocation.cs

  Gates/
    Gate.cs
    GateAyahLink.cs
    GateRelation.cs
    GateRelationType.cs
    GateProtectionMode.cs

  Approvals/
    ApprovalRequest.cs
    ApprovalStatus.cs

  Audit/
    ActivityLog.cs
    ActivityType.cs
```

Bad example:

```text
Domain/
  Entities/
  Enums/
  Models/
  Helpers/
  Utils/
```

Important nuance:

- A small `Common` folder is allowed for true base primitives such as `Entity`,
  `ValueObject`, `DomainEvent`, `DomainException`.
- `Shared` should not become a dumping ground; it is only for truly cross-project,
  cross-layer primitives.
- If a type belongs to one domain feature, it must stay inside that feature folder.
- If a domain folder grows, split it internally by subdomain/feature instead of
  moving unrelated files into global technical folders.

Example of splitting a large Quran domain:

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

### 2. Application layer organization

Prefer grouping use cases by feature:

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

Avoid global dumping folders like:

```text
Application/
  DTOs/
  Services/
  Validators/
  Handlers/
```

unless they are truly shared and small in number. Commands, queries, handlers,
validators, and responses should stay near the use case they belong to.

### 3. Application.Abstractions organization

Group abstractions by purpose and feature where useful.

Good example:

```text
Application.Abstractions/
  Persistence/
  Files/
  Reports/
  Quran/
  Gates/
```

Avoid one large unrelated interfaces folder if many contracts exist.

### 4. Infrastructure organization

Infrastructure may group by technical responsibility because it is an
implementation layer, but it should still preserve feature clarity.

Good example:

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

EF configurations:

- Prefer feature/domain grouping under `Persistence/Configurations`.
- Avoid one huge flat `Configurations` folder if many entities exist.

### 5. API organization

- Controllers/endpoints should be grouped by feature.
- Controllers must stay thin.
- No business logic in controllers.
- No data processing hidden in controllers.
- Request/response contracts may live near endpoints or in feature folders, not in
  a huge unrelated global dumping folder.

## Review Checklist

1. Folder organization
- Are files grouped by domain/feature/bounded context?
- Are unrelated files grouped by technical type?
- Are global folders like Enums/Models/Helpers/Utils being used as dumping grounds?
- Are enums/value objects placed near the feature that owns them?

2. Layer boundaries
- Domain does not depend on Application/Infrastructure/Api.
- Application does not depend on Infrastructure.
- Infrastructure implements abstractions.
- Api is only the entry point.

3. Domain clarity
- Entity, enum, value object, and domain rule names reflect Quran Dashboard
  concepts clearly.
- Avoid vague names like DataItem, Obj, Temp, Info2.
- Domain folders are not too broad; if a folder grows, suggest subfolder splits.

4. Application clarity
- Use cases are feature-scoped.
- Commands/queries/handlers/responses stay near each other.
- No global DTO/Service dumping unless truly shared.

5. Infrastructure clarity
- EF configurations are organized.
- File readers/importers/report writers are not mixed with domain logic.
- Migrations are not manually changed without reason.

6. API clarity
- Controllers/endpoints are feature grouped.
- Controllers are thin.
- No business rules or data processing hidden in controllers.

7. Quranic data safety
- No Quranic/source-sensitive data is invented or silently modified.
- Data processors/importers/generators preserve traceability and produce reports.

## Output Format

Return the review in this structure:

# Backend Structure Review

## Verdict
Use one of:
- PASS
- PASS WITH NOTES
- NEEDS CHANGES
- BLOCKED

## Summary
Briefly describe what was reviewed.

## Blocking Issues
List issues that must be fixed before merge.
If none, write:
None.

## Structure Notes
Discuss folder organization and domain/feature boundaries.

## Layering Check
Discuss Clean Architecture dependency/layering rules.

## Anti-Pattern Check
Mention whether global dumping folders such as Enums/Models/Helpers/Utils were introduced or expanded.

## Quranic Data Safety Check
Mention any source-sensitive data risk.

## Recommendations
List practical improvements.
Do not request broad refactors unless necessary.

## Changed Files Reviewed
List changed files if known.

## Guardrails

- Be direct and practical.
- Do not implement fixes unless explicitly asked.
- Do not invent facts.
- If the file tree is not available, ask for or request the relevant tree/status.
- Separate blocking issues from optional recommendations.
- Prefer small, focused restructuring suggestions.
- Do not force over-engineering.
- Domain/feature grouping is the default.
- Technical-type grouping is allowed only when it is truly cross-cutting and not a
  dumping ground.
- Remember that the anti-pattern example above is external and not necessarily
  present in this repository.
