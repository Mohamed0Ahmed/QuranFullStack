# Claude Backend Router

For any Backend change, read the nearest relevant README first, using `Backend/README.md` only
when no closer README owns the area. Then load only the sources selected below. This file is a
router; the root universal kernel remains in force.

| Trigger | Load |
| --- | --- |
| Any `Backend/` path | The nearest relevant README before any specialist document; use `docs/contracts/README.md` only to locate a contract owner. |
| Add or move Backend files, folders, entities, enums, value objects, DTOs, handlers, services, EF configurations, controllers, infrastructure implementations, or `GlobalUsings.cs` | `Backend/.architecture/BACKEND_STRUCTURE.md` and `Backend/.architecture/CLEAN_ARCHITECTURE.md`. |
| API endpoint, controller, request/response contract, middleware, Swagger/OpenAPI, API error handling, response shape, configuration, or health-check work | `Backend/.architecture/API_GUIDELINES.md`; add the auth/access route below when security is involved. |
| Logging, exception diagnostics, importer/pipeline run summary, CLI output, or report output | `Backend/.architecture/LOGGING_GUIDELINES.md`. |
| Auth, access, Owner, permission, or identity work | `docs/contracts/security-access.md`, then only its directly implicated README; API route/security changes also load `Backend/.architecture/API_GUIDELINES.md` §11. |
| Quran import, generation, or source work | `CODING_PRINCIPLES.md` §10 and the nearest pipeline or DataImporter README. |
| EF migration or schema work | `Backend/README.md` §Invariants, `Backend/scripts/README.md`, and the migration sections of `TESTING_STRATEGY.md`. |
| Select, run, or report Backend tests | `TESTING_STRATEGY.md` §§1–2 and §5 plus the relevant §3 lane; also §3.2 for output/hang safety, §§3.3–3.4 for database/canonical work, §6 for route/auth/binding, and §§7–10 for build/no-CI/failure/workflow ownership. Read `Backend/tests/QuranDashboard.Tests/README.md`. |
| Write or review Backend tests | `.claude/skills/test-guard/SKILL.md` and `references/dotnet.md`. |
| Deployment or runtime smoke | `Backend/README.md` §Deployment and `.claude/skills/deploy-smoke/SKILL.md`. |
| A changed Backend file reaches a documented size threshold | `Backend/.architecture/BACKEND_STRUCTURE.md` §File Size and Responsibility Guidelines at pre-delivery. |
