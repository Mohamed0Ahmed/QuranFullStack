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
  It promotes only common, layer-safe, non-feature-specific namespaces per project,
  removes the redundant local usings, strictly respects Clean Architecture layer
  boundaries (never promotes EF Core / ASP.NET Core / Infrastructure namespaces into
  Domain or Application), keeps each `GlobalUsings.cs` small and intentional, and
  verifies with `dotnet build`. Not for adding a single using, for frontend/TypeScript
  imports, or for C# `using` resource/disposal statements.
---

# Backend Global Usings Cleanup Skill

Use this skill to tidy and consolidate `global using` directives across the Quran
Dashboard .NET backend. Its job: find namespaces that are imported over and over
across a project's files, promote the genuinely ubiquitous and **layer-safe** ones
into that project's `GlobalUsings.cs`, and delete the now-redundant per-file `using`
lines — so each project keeps a small, intentional set of global usings and the
individual files stay clean.

This is an **action skill**: it edits code. But the change must be **import-only and
behavior-preserving** — you touch `using` directives and `GlobalUsings.cs` files,
nothing else. The `dotnet build` at the end is the safety net that proves you neither
broke compilation nor left redundant-using warnings behind.

The rules for *what belongs* in a global using are not invented here — they are
canonical in `Backend/.architecture/BACKEND_STRUCTURE.md` (`## Global Usings`). Read
that section and apply it; this skill is the *procedure* for carrying it out safely.

## Required Context / Reading Rules

Read the canonical docs for the rules instead of trusting memory:

- **Always:** `Backend/.architecture/BACKEND_STRUCTURE.md` — the `## Global Usings`
  section is canonical for which namespaces may be promoted, the per-layer
  restrictions, the preferred file placement, and the "decision rule before adding a
  global using". `CODING_PRINCIPLES.md` — §7 Focused Changes (don't touch unrelated
  files) and §6 Strong Typing apply.
- **When layer direction is in question:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`
  — canonical for the dependency direction that gates which namespaces are safe in
  which project.

If a referenced doc is missing, say so in the report rather than guessing the rules.

## Backend Projects (default scope: all of them)

Invoked without a target, sweep every backend C# project, each with its own
`GlobalUsings.cs`:

| Project | Path | Layer note |
|---|---|---|
| Domain | `domain/QuranDashboard.Domain` | Most restricted — no framework/infra namespaces |
| Application.Abstractions | `application/QuranDashboard.Application.Abstractions` | Contracts only; no Infrastructure/Api |
| Application | `application/QuranDashboard.Application` | No Infrastructure; no ASP.NET unless explicitly approved |
| Infrastructure | `infrastructure/QuranDashboard.Infrastructure` | May globally use EF Core etc.; must not leak into Domain/Application |
| Api | `api/QuranDashboard.Api` | May globally use ASP.NET Core (e.g. MVC) |
| Shared | `shared/QuranDashboard.Shared` | Keep genuinely cross-cutting only |
| Tests | `tests/QuranDashboard.Tests` | Test-only framework usings (e.g. xUnit) |

If the user names a specific project or only the current git diff is relevant, scope
to that instead and say so in the report.

## The Gate: what may be promoted to a global using

A namespace qualifies for `GlobalUsings.cs` only when **all** of these hold (this is
`BACKEND_STRUCTURE.md`'s decision rule, applied per project):

1. **Common in this same project** — imported across many of the project's files. Rule
   of thumb: a clear majority of files, or many files where the namespace is plainly
   framework/cross-cutting (e.g. `Microsoft.EntityFrameworkCore` in Infrastructure).
   When in doubt, leave it local: a global using trades the locality signal ("this file
   touches EF") for brevity, and only pays off when the namespace is genuinely
   everywhere.
2. **Layer-safe for this project** — promoting it must not violate the dependency
   direction (see the table below). This is a hard gate, never overridden by frequency.
3. **Not feature-specific** — namespaces like `QuranDashboard.Application.Quran.Import.Validation`
   stay local even if repeated within their feature folder; they signal which feature a
   file belongs to. Promote framework/cross-cutting namespaces, not feature ones.
4. **Doesn't hide a dependency that should stay explicit** — if only a few files use it,
   or its presence is a meaningful architectural signal, keep it local.

### Layer restriction table (hard gate)

Never add these to a project's `GlobalUsings.cs`, regardless of how often they repeat:

| Project | Must NOT globally use |
|---|---|
| Domain | ASP.NET Core, EF Core, Infrastructure, Application namespaces |
| Application.Abstractions | Infrastructure, Api namespaces |
| Application | Infrastructure namespaces; ASP.NET Core (unless explicitly approved) |
| Infrastructure | (none extra) — but its namespaces must not be promoted into Domain/Application |

When a promotion would cross a boundary, that is a finding to report, not an edit to
make. If the repetition itself reveals a layering smell (e.g. Domain files repeatedly
importing an Infrastructure namespace), surface it — do not paper over it with a global
using.

## Workflow

1. **Read the canonical rules** (above). Confirm the project list and scope.
2. **Inventory per project.** For each project, list the plain `using` directives and how
   often each appears. A reliable starting point (adjust globs as needed):

   ```bash
   rg -No '^using [^;]+;' <projectDir> \
     -g '!**/bin/**' -g '!**/obj/**' -g '!**/GlobalUsings.cs' \
     | sort | uniq -c | sort -rn
   ```

   Occurrence count ≈ file count (a file rarely repeats the same directive). Treat the
   numbers as a guide, then apply judgment — the gate decides, not the raw count.
3. **Select candidates** per project using the gate. Exclude `using static …` and
   `using Alias = …` from automatic promotion (handle only if genuinely pervasive and
   obviously safe; otherwise leave local).
4. **Apply the layer gate.** Drop any candidate the restriction table forbids; note it
   as a finding if the repetition hints at a real boundary problem.
5. **Update `GlobalUsings.cs`** for the project (one file per project — never a single
   shared cross-project file). Create it if absent; if the project has no qualifying
   repeated usings, it doesn't need one. Keep entries sorted and grouped (framework
   namespaces, then project namespaces), small and intentional.
6. **Remove the redundant local usings.** For every namespace you promoted, delete its
   per-file `using` line across that project. This step is mandatory: a local using that
   duplicates a global using is a redundant-using warning, and the clean build depends on
   removing them.
7. **Verify** (below). If anything fails, fix or revert — never leave the backend in a
   non-building state.

## Leave alone

- **Feature-specific namespaces** — keep local; they carry meaning.
- **`using` statements / resource blocks** (`using var x = …`, `using (…) { }`) — these
  are not import directives; never touch them.
- **Aliases and `using static`** — only promote if pervasive and obviously safe.
- **Conditional `#if` usings**, generated files (EF `Migrations/`), and `bin/`/`obj/`.
- **`Program.cs` top-level statements** and any non-import code.

## Verification

- Run `dotnet build` from `Backend/` — it must finish **0 warnings, 0 errors**. Zero
  warnings matters here: redundant-using and unused-using warnings are exactly how an
  incomplete cleanup shows up.
- If the test project was touched (or you want extra confidence), run `dotnet test`.
- Report build/test status honestly. If the build breaks, the most common cause is a
  removed local using whose namespace was *not* actually added globally — re-check the
  promotion before reverting.

## Output / Report

After the work, report (matching `CODING_PRINCIPLES.md` §12 Definition of Done):

```
# Backend Global Usings Cleanup

## Summary
Scope (which projects), one-line outcome.

## Per-Project Changes
| Project | Promoted to GlobalUsings.cs | Local usings removed | Files touched |
|---|---|---:|---:|
...

## Layer-Safety Decisions
Candidates deliberately NOT promoted because they would cross a boundary, and any
layering smell the repetition revealed.

## Verification
Build status (warnings/errors) and test status if run.

## Skipped / Uncertain
Anything left local on judgment, aliases/static usings deferred, or projects with
nothing to consolidate.
```

## Guardrails

- **Import-only, behavior-preserving.** Touch only `using` directives and
  `GlobalUsings.cs`. Do not reorder/rename/refactor anything else, and do not mix in
  unrelated edits (`CODING_PRINCIPLES.md` §7).
- **Conservative by default.** Fewer, genuinely ubiquitous global usings beat a long
  swept-up list. When unsure, leave it local.
- **Layer boundaries are absolute.** Frequency never justifies crossing them.
- **One `GlobalUsings.cs` per project.** Never introduce a shared cross-project usings
  file.
- **Cite, don't restate.** `BACKEND_STRUCTURE.md` is canonical for the rules; defer to
  it when the situation is ambiguous.
- **Never leave the backend non-building.** The clean `dotnet build` is the contract.
