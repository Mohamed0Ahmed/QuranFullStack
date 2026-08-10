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
| EF migration or schema work | Read `TESTING_CONSTITUTION.md`, `Backend/README.md` §Invariants, and `Backend/scripts/README.md`. |
| Select, run, or report Backend tests | Read `TESTING_CONSTITUTION.md`; use `Backend/tests/QuranDashboard.Tests/README.md` only for lanes and fixtures. |
| Write or review retained Backend tests | Read `TESTING_CONSTITUTION.md`, then `.claude/skills/test-guard/SKILL.md` and `references/dotnet.md`. |
| Deployment or runtime smoke | `Backend/README.md` §Deployment and `.claude/skills/deploy-smoke/SKILL.md`. |
| A changed Backend file reaches a documented size threshold | `Backend/.architecture/BACKEND_STRUCTURE.md` §File Size and Responsibility Guidelines at pre-delivery. |
