# Backend Global Usings Cleanup

Use the project backend global usings cleanup skill as the procedure:

.claude/skills/backend-global-usings-cleanup/SKILL.md

Consolidate commonly-repeated, layer-safe namespaces into each Backend project's
`GlobalUsings.cs` and remove the now-redundant per-file `using` directives. This is an
action skill: it edits `using` directives and `GlobalUsings.cs` only (import-only,
behavior-preserving). Respect Clean Architecture layer boundaries — never promote
EF Core / ASP.NET Core / Infrastructure namespaces into Domain or Application — keep
feature-specific namespaces local, and verify with `dotnet build` (0 warnings, 0
errors). Follow the workflow and report format defined in the skill.
