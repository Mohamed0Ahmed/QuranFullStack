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
  boundaries, checks file-size/responsibility thresholds, and checks Quranic data
  safety, then returns a structured verdict. It relies on the canonical backend
  architecture docs rather than restating them. This is a review skill only: do
  not implement fixes unless the user explicitly asks.
---

# Backend Structure Review Skill

Use this skill to review backend file organization, Clean Architecture
boundaries, domain/feature foldering, and file-size/responsibility for the Quran
Dashboard backend. Its core job: keep the backend organized by domain/feature
first, prevent technical-type dumping (global `Enums/`, `Models/`, `Helpers/`,
`Utils/`), verify layering, and flag oversized/overloaded files.

This skill is review only. Do not implement fixes unless explicitly asked.

## Required Context / Reading Rules

Rely on the workspace/tool context already loaded by your agent for entrypoint
files. Read the **canonical** docs for the full rules instead of restating them
here:

- **Always:** `CODING_PRINCIPLES.md`, `Backend/.architecture/BACKEND_STRUCTURE.md`
  (canonical for file/folder placement, feature/domain organization, global usings
  placement, and file-size/responsibility thresholds).
- `Backend/.architecture/CLEAN_ARCHITECTURE.md` — when layer responsibilities,
  dependency direction, or use-case/request flow are involved (canonical for
  those).
- `Backend/.architecture/API_GUIDELINES.md` — when the API boundary, HTTP
  behavior, response shape, or API localization/messages are involved (canonical
  for those).

If a referenced document is missing or unavailable, state that clearly in the
output. Do not copy large sections from these docs into the review; cite them.

## Backend Context

- **Backend path:** `Backend/`
- **Stack:** .NET 10, ASP.NET Core Web API, EF Core, PostgreSQL, Code First,
  Clean Architecture style.
- **Projects:** `api/QuranDashboard.Api`, `domain/QuranDashboard.Domain`,
  `application/QuranDashboard.Application.Abstractions`,
  `application/QuranDashboard.Application`,
  `infrastructure/QuranDashboard.Infrastructure`, `shared/QuranDashboard.Shared`.

## What to Check

### 1. File/folder placement

- Files grouped by domain/feature/bounded context.
- No global dumping folders (`Enums/`, `Models/`, `DTOs/`, `Helpers/`, `Utils/`,
  `Services/`) unless truly shared, cross-cutting, and small.
- Each enum/value object/type lives next to the feature/domain/use case that owns
  it. (Canonical: `BACKEND_STRUCTURE.md`.)

### 2. Layer boundaries and dependency direction

- Domain independent (no Application/Infrastructure/Api/EF dependencies).
- Application does not depend on Infrastructure.
- Application.Abstractions does not depend on Infrastructure/Api.
- Infrastructure implements abstractions; does not leak into Domain/Application.
- Api is the entry point only; uses Infrastructure for composition/DI wiring, not
  controller logic. (Canonical: `CLEAN_ARCHITECTURE.md`.)

### 3. File-size and responsibility thresholds

Check changed files against the thresholds in `BACKEND_STRUCTURE.md` for:
controllers/endpoints, handlers, services, repositories/read services,
entities/aggregates, DTOs/contracts/models.

- **Soft threshold exceeded:** ask whether the size is justified and whether
  splitting would improve clarity.
- **Hard threshold exceeded:** mark as a finding and recommend a split.
- **1000+ line files:** serious design smell unless explicit human approval exists.
- Thousands-of-lines files are not acceptable.

Terminology: use **overloaded service** / **oversized service** / **oversized
store**. Do not use "God service".

### 4. Thinness and responsibility split

- Controllers/endpoints stay thin (no business logic, EF queries, file parsing, or
  Quranic data processing).
- Handlers orchestrate one use case.
- Services have one reason to change and are not overloaded/oversized.
- Repositories/read services stay focused and do not own unrelated data access for
  many domains.

### 5. API structure (when API files changed)

- Controllers/endpoints are feature-grouped; contracts live near endpoints/feature.
- For boundary/response/localization rules, defer to `API_GUIDELINES.md`.

### 6. Quranic data safety / traceability (when relevant)

- No Quranic/source-sensitive data invented or silently modified.
- Importers/generators preserve traceability and produce reports (totals, missing,
  duplicates, warnings, validation result).

## Anti-Pattern Reminder (external example — not necessarily present here)

A global `Enums/` folder (or `Models/`, `Helpers/`, `Utils/`) that collects
unrelated types is a dumping ground: it separates a type from the domain concept
that owns it. Do **not** assume such folders exist in this repo or search for them
as current code — use this only to recognize the smell. Preferred rule: organize
by domain/feature/bounded context first; place each type next to the feature it
belongs to.

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
List issues that must be fixed before merge. If none, write: None.

## Structure Notes
Folder organization and domain/feature boundaries.

## Layering Check
Clean Architecture dependency/layering rules.

## File Size Check
List files near/over soft thresholds, over hard thresholds, or 1000+ lines. If not
applicable, say so.

## Anti-Pattern Check
Whether global dumping folders (Enums/Models/Helpers/Utils) were introduced or
expanded.

## Quranic Data Safety Check
Any source-sensitive data risk. Say PASS / CONCERN / NOT APPLICABLE.

## Recommendations
Practical improvements. Do not request broad refactors unless necessary.

## Changed Files Reviewed
List changed files if known.

## Guardrails

- Be direct and practical.
- Do not implement fixes unless explicitly asked.
- Do not invent facts; if the file tree/status is unavailable, request it.
- Rely on the canonical docs above; do not restate their full rules here.
- Domain/feature grouping is the default; technical-type grouping is allowed only
  when truly cross-cutting and not a dumping ground.
- Separate blocking issues from optional recommendations; do not over-engineer.
- Use "overloaded service" / "oversized service"; do not use "God service".
- Keep this skill focused on backend structure, layering, placement, and file
  size — do not duplicate the broader engineering-review skill.
