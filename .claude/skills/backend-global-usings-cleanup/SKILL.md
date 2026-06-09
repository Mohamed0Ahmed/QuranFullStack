---
name: backend-global-usings-cleanup
description: >-
  Action skill (it edits code) that cleans up and consolidates C# global usings
  across the Quran Dashboard .NET backend (App/Backend: Api, Domain, Application,
  Application.Abstractions, Infrastructure, Shared, and the test project). Use this
  skill whenever the user wants to clean up, consolidate, tidy, dedupe, or reduce
  repeated `using` directives in the backend, promote commonly-repeated namespaces
  into a project's `GlobalUsings.cs`, remove now-redundant per-file usings, or fix a
  sprawling/missing/shared `GlobalUsings.cs` — even if they don't say the exact words
  "global usings" (e.g. "the same imports are repeated in every Infrastructure file",
  "tidy the usings in the .NET projects", "our using statements are everywhere").
  It promotes layer-safe namespaces that repeat in more than five files in the same
  project (including feature/handler namespaces at that threshold), removes redundant
  local usings, respects Clean Architecture layer boundaries from
  BACKEND_STRUCTURE.md, and verifies with `dotnet build`. Do not edit
  BACKEND_STRUCTURE.md as part of this skill. Not for adding a single using,
  frontend/TypeScript imports, or C# `using` resource/disposal statements.
---

# Backend Global Usings Cleanup Skill

Use this skill to tidy and consolidate `global using` directives across the Quran
Dashboard .NET backend. Its job: find namespaces imported over and over in the same
project, promote the **layer-safe** ones into that project's `GlobalUsings.cs`, and
delete the now-redundant per-file `using` lines.

This is an **action skill**: it edits code. The change must be **import-only and
behavior-preserving** — you touch `using` directives and `GlobalUsings.cs` files,
nothing else. The `dotnet build` at the end is the safety net.

## Relationship to `BACKEND_STRUCTURE.md`

**Do not edit** `Backend/.architecture/BACKEND_STRUCTURE.md` as part of this skill.
That doc stays as written.

| Topic | Source of truth |
|---|---|
| Layer restrictions (what each project may never global-use) | `BACKEND_STRUCTURE.md` § Global Usings |
| File placement (`GlobalUsings.cs` per project) | `BACKEND_STRUCTURE.md` |
| Clean Architecture dependency direction | `CLEAN_ARCHITECTURE.md` |
| **When repetition qualifies for promotion** | **This skill** — see threshold below |
| **Feature/handler namespaces at high repetition** | **This skill** — promote at >5; do not revert because BACKEND_STRUCTURE prefers local feature usings in general |

How the two fit together:

- `BACKEND_STRUCTURE.md` says global usings are for namespaces **common and repeated
  across many files**, and that feature-specific namespaces should normally stay local.
- This skill defines **many** operationally: **more than five files in the same
  project**. At that count, repetition is high enough that promotion reduces noise
  without hiding layer boundaries.
- Feature-specific namespaces (handlers, validation folders, etc.) **are promoted at
  >5** like any other namespace. Folder structure and type names still show feature
  ownership; the team accepts global usings for high repetition.
- If count is ≤5, keep local — that aligns with BACKEND_STRUCTURE's default for
  feature-specific and infrequent usings.

## Required Context / Reading Rules

- **Always read:** `Backend/.architecture/BACKEND_STRUCTURE.md` — § Global Usings for
  layer restrictions and placement. `CODING_PRINCIPLES.md` — §7 Focused Changes.
- **When layer direction is in question:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`.
- **Do not** change `BACKEND_STRUCTURE.md` to match cleanup output.

## Backend Projects (default scope: all of them)

| Project | Path | Layer note |
|---|---|---|
| Domain | `domain/QuranDashboard.Domain` | Most restricted — no framework/infra namespaces |
| Application.Abstractions | `application/QuranDashboard.Application.Abstractions` | Contracts only; no Infrastructure/Api |
| Application | `application/QuranDashboard.Application` | No Infrastructure; no ASP.NET unless explicitly approved |
| Infrastructure | `infrastructure/QuranDashboard.Infrastructure` | May globally use EF Core etc. |
| Api | `api/QuranDashboard.Api` | May globally use ASP.NET Core (e.g. MVC) |
| Shared | `shared/QuranDashboard.Shared` | Keep genuinely cross-cutting only |
| Tests | `tests/QuranDashboard.Tests` | Test frameworks (xUnit, FluentAssertions) + shared test infrastructure |

If the user names a specific project or only the current git diff is relevant, scope
to that and say so in the report.

## The Gate: what may be promoted

A namespace qualifies for `GlobalUsings.cs` when **all** of these hold:

1. **>5 files in this same project** — counted as local `using` lines (excluding
   `GlobalUsings.cs`, EF `Migrations/`, and `bin/`/`obj/`). **Automatic promotion
   threshold**: above five → promote unless a hard gate blocks it.
2. **Layer-safe** — must not violate the restriction table below. **Never overridden
   by frequency.**
3. **No cross-layer smell** — if repetition reveals a boundary problem (e.g. Domain
   importing Infrastructure), **report it**; do not paper over with a global using.

**At ≤5 files:** keep local.

**Exception (still ≤5):** a plainly framework/cross-cutting namespace used in a clear
**majority** of the project's files (e.g. `Microsoft.EntityFrameworkCore` across
most Infrastructure persistence code) may be promoted even below six files.

**Feature namespaces:** not exempt. Handler, validation, and feature-folder namespaces
follow the same >5 rule (e.g. test handler usings repeated across a feature's test
files).

### Layer restriction table (hard gate — from `BACKEND_STRUCTURE.md`)

Never add these to a project's `GlobalUsings.cs`, regardless of count:

| Project | Must NOT globally use |
|---|---|
| Domain | ASP.NET Core, EF Core, Infrastructure, Application namespaces |
| Application.Abstractions | Infrastructure, Api namespaces |
| Application | Infrastructure namespaces; ASP.NET Core (unless explicitly approved) |
| Infrastructure | — (its namespaces must not be promoted into Domain/Application) |

## Workflow

1. **Read** `BACKEND_STRUCTURE.md` § Global Usings (layer rules only). Confirm scope.
2. **Inventory per project** — count each `using` namespace:

   ```bash
   rg -No '^using [^;]+;' <projectDir> \
     -g '!**/bin/**' -g '!**/obj/**' -g '!**/GlobalUsings.cs' \
     -g '!**/Migrations/**' \
     | sort | uniq -c | sort -rn
   ```

   Promote every namespace with **count > 5** unless the layer gate blocks it.
3. **Exclude** from automatic promotion: `using static …`, `using Alias = …` (unless
   pervasive and obviously safe).
4. **Apply the layer gate.** Blocked candidates → report as findings, do not promote.
5. **Update `GlobalUsings.cs`** — one file per project; create if absent. Group:
   framework namespaces first, then project namespaces; keep sorted within each group.
6. **Remove redundant local usings** for every promoted namespace. Mandatory — else
   redundant-using warnings fail the build.
7. **Verify** (below).

## Leave alone

- Namespaces at **≤5 files** (unless majority/framework exception above).
- `using var` / `using (...)` resource blocks — not import directives.
- Aliases and `using static` — unless pervasive and safe.
- Generated migrations, `bin/`/`obj/`, conditional `#if` usings.
- **`BACKEND_STRUCTURE.md`** — never edit as part of this cleanup.

## Verification

- `dotnet build` from `Backend/` — **0 warnings, 0 errors**.
- Run `dotnet test` when the test project was touched.
- If build fails, check a removed local using was actually added globally before reverting.

## Output / Report

```
# Backend Global Usings Cleanup

## Summary
Scope, one-line outcome.

## Per-Project Changes
| Project | Promoted to GlobalUsings.cs | Local usings removed | Files touched |
|---|---|---:|---:|

## Layer-Safety Decisions
Blocked candidates and any layering smells (not papered over).

## Verification
Build and test status.

## Skipped / Uncertain
≤5 counts left local, aliases/static deferred, projects with nothing to consolidate.
```

## Guardrails

- **Import-only.** Only `using` directives and `GlobalUsings.cs`.
- **>5 threshold is consistent.** Do not skip a qualifying namespace because it is
  feature-specific — that is the team rule for this skill.
- **Layer boundaries are absolute.** From `BACKEND_STRUCTURE.md`; frequency never
  crosses them.
- **One `GlobalUsings.cs` per project.**
- **Do not edit `BACKEND_STRUCTURE.md`.** This skill operationalizes repetition
  threshold; the architecture doc stays unchanged.
- **Never leave the backend non-building.**
