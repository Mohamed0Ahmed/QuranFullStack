# Sol/Codex Backend Router

For a Backend change, start with the closest relevant README, falling back to
`Backend/README.md` when no nearer file owns the area. Load only the triggered sources in this
table. The root universal kernel continues to control.

| Trigger | Load |
| --- | --- |
| Any `Backend/` path | The nearest relevant README before specialist guidance; `docs/contracts/README.md` is only an index to the owning contract README/code. |
| Adding or moving Backend files, folders, entities, enums, value objects, DTOs, handlers, services, EF configurations, controllers, infrastructure implementations, or `GlobalUsings.cs` | `Backend/.architecture/BACKEND_STRUCTURE.md` and `Backend/.architecture/CLEAN_ARCHITECTURE.md`. |
| API endpoints, controllers, request/response contracts, middleware, Swagger/OpenAPI, error handling, response shapes, API configuration, or health checks | `Backend/.architecture/API_GUIDELINES.md`; include the security route below for auth/access work. |
| Logging, exception diagnostics, importer/pipeline summaries, CLI console output, or report output | `Backend/.architecture/LOGGING_GUIDELINES.md`. |
| Authentication, access, Owner, permissions, or identity | `docs/contracts/security-access.md`, followed only by the directly implicated README; route/security changes also require `Backend/.architecture/API_GUIDELINES.md` §11. |
| Quran import, generation, or source handling | `CODING_PRINCIPLES.md` §10 and the nearest pipeline/DataImporter README. |
| EF migration or schema changes | `Backend/README.md` §Invariants, `Backend/scripts/README.md`, and `TESTING_STRATEGY.md` migration sections. |
| Backend test selection, execution, or reporting | Read `TESTING_CONSTITUTION.md`, then `Backend/tests/QuranDashboard.Tests/README.md`. |
| Writing or reviewing Backend tests | Apply that testing constitution, then read `.agents/skills/test-guard/SKILL.md` with `references/dotnet.md`. |
| Deployment or runtime smoke | `Backend/README.md` §Deployment and `.agents/skills/deploy-smoke/SKILL.md`. |
| A changed Backend file meets a documented size threshold | `Backend/.architecture/BACKEND_STRUCTURE.md` §File Size and Responsibility Guidelines during pre-delivery. |
