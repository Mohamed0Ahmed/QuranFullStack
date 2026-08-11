---
name: backend-global-usings-cleanup
description: Use when asked to consolidate repeated C# using directives or GlobalUsings.cs files in the Quran Dashboard backend.
---

# Backend Global Usings Cleanup

## Responsibility

Consolidate `using` directives in the requested Quran Dashboard backend project(s):
promote qualifying namespaces into that project's `GlobalUsings.cs` (one per project),
remove the now-redundant local `using` lines, and verify the affected projects still
compile. The change is import-only and behavior-preserving — `using` directives and
`GlobalUsings.cs` files, nothing else.

**Not this skill's job:** tests or test selection, code review, refactoring beyond
import lines, documentation edits, or any Git action. Never edit
`Backend/.architecture/BACKEND_STRUCTURE.md` to match cleanup output.

## Promotion gate

A namespace is promoted only when all of these hold:

1. **More than five files in the same project** carry it as a local `using` (excluding
   `GlobalUsings.cs`, EF `Migrations/`, `bin/`, `obj/`). One exception: a plainly
   framework/cross-cutting namespace used in a clear majority of the project's files may
   be promoted below that count.
2. **Layer-safe** for that project. Frequency never overrides a layer restriction.
3. **Not feature-specific.** Feature, handler, validation, and bounded-context
   namespaces stay local at any count — repetition alone hides feature ownership.

Leave alone: `using static`, aliases, `using var`/resource blocks, generated migrations,
and conditional `#if` usings. If repetition reveals a cross-layer problem, report it as a
finding — never paper over it with a global using.

## Workflow

1. Confirm the requested scope (a named project, the current diff, or all backend
   projects) and state it in the report.
2. Inventory the locally repeated `using` namespaces per project in scope.
3. Apply the promotion gate. Update `GlobalUsings.cs` (create if absent): framework
   namespaces first, then project namespaces, sorted within each group.
4. Remove the redundant local usings for every promoted namespace.
5. Verify with a focused compile of the affected projects (`dotnet build`) — zero
   warnings, zero errors. This focused compilation is the only verification this skill
   runs; it never adds test lanes, reviews, or broader builds.

## Conditional context

- `Backend/.architecture/BACKEND_STRUCTURE.md` §Global Usings — only when a candidate's
  layer safety or the `GlobalUsings.cs` placement for the requested scope is ambiguous.
- `Backend/.architecture/CLEAN_ARCHITECTURE.md` — only for a disputed
  dependency-direction boundary.

## Output

Report: scope, per-project promotions and removed local usings, blocked candidates with
the layer reason, the compile result, and anything left local or uncertain.
