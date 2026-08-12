# Sol/Codex Backend Router

For a Backend change, load only the sources selected by the matching trigger below. Active Spec
Kit artifacts own feature intent; code owns implemented truth. `Backend/README.md` and the other
retained READMEs are operational references, not code-area architecture. The root universal
kernel continues to control.

| Trigger | Load |
| --- | --- |
| Any `Backend/` path | The code in scope and, for phase-bound work, the active Spec Kit artifacts; `docs/contracts/README.md` is only a pointer index to code and architecture authorities. |
| Adding or moving Backend files, folders, entities, enums, value objects, DTOs, handlers, services, EF configurations, controllers, infrastructure implementations, or `GlobalUsings.cs` | `Backend/.architecture/BACKEND_STRUCTURE.md` and `Backend/.architecture/CLEAN_ARCHITECTURE.md`. |
| API endpoints, controllers, request/response contracts, middleware, Swagger/OpenAPI, error handling, response shapes, API configuration, or health checks | `Backend/.architecture/API_GUIDELINES.md`; include the security route below for auth/access work. |
| Logging, exception diagnostics, importer/pipeline summaries, CLI console output, or report output | `Backend/.architecture/LOGGING_GUIDELINES.md`. |
| Authentication, access, Owner, permissions, or identity | `docs/contracts/security-access.md`; route/security changes also require `Backend/.architecture/API_GUIDELINES.md` §11. |
| Quran import, generation, or source handling | `CODING_PRINCIPLES.md` §10 plus the implicated source code/manifests; use `Backend/tools/QuranDashboard.DataImporter/README.md` only for CLI operation and source-package safety. |
| EF migration or schema changes | Read `TESTING_CONSTITUTION.md`, `Backend/README.md` §Invariants, and `Backend/scripts/README.md`. |
| Backend test selection, execution, or reporting | Read `TESTING_CONSTITUTION.md`; use `Backend/tests/QuranDashboard.Tests/README.md` only for lanes and fixtures. |
| Writing or reviewing retained Backend tests | Read `TESTING_CONSTITUTION.md`, then `.agents/skills/test-guard/SKILL.md` with `references/dotnet.md`. |
| Deployment or runtime smoke | `Backend/README.md` §Deployment and `.agents/skills/deploy-smoke/SKILL.md`. |
| A changed Backend file meets a documented size threshold | `Backend/.architecture/BACKEND_STRUCTURE.md` §File Size and Responsibility Guidelines during pre-delivery. |
