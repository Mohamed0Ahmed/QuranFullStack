# Backend Instructions

## Architecture invariants

Organize changes by the owning feature across the Clean Architecture layers. Keep business rules in
Domain, use-case orchestration in Application, Application-consumed contracts in
Application.Abstractions, concrete external concerns in Infrastructure, and HTTP/composition concerns
in Api. Domain and Shared remain independent; Shared contains only genuinely cross-project primitives.

- **Placement or global-usings decisions:** read `.architecture/BACKEND_STRUCTURE.md`.
- **Layer responsibility, dependency direction, request flow, or data-access seams:** read
  `.architecture/CLEAN_ARCHITECTURE.md`.

Preserve source traceability for generated or imported Quran data; fabricated Quranic content is never
a fallback.

## Route by task

- **HTTP or cross-stack contract:** follow the feature from `api/` through Application and its
  abstractions. When an endpoint, payload, route, permission vocabulary, or authentication behavior may
  affect callers, read `../Frontend/quran-dashboard-ui/AGENTS.md`. Use `scripts/README.md` sections
  `export-swagger` and `check-api-contract` for the Backend-to-Frontend contract workflow.
- **Persistence or schema:** keep EF Core implementation and migrations under
  `infrastructure/QuranDashboard.Infrastructure/`. Use `scripts/README.md` for migration, pending-model,
  reset, and database-safety workflows.
- **Data import or generation:** read `tools/QuranDashboard.DataImporter/README.md` before changing or
  running importer verbs. Its reports and safety gates are part of the workflow.
- **Access administration:** read `tools/QuranDashboard.AccessAdmin/README.md` before changing or running
  identity, permission-catalogue, Owner, legacy-role, or authorization-preflight operations.
- **Build, run, or deploy:** use `README.md` for the supported operational path and deployment
  constraints; use `scripts/README.md` for repository shortcuts.

## Manifests and verification

- `QuranDashboard.sln` defines solution membership; each project's `.csproj` defines its packages and
  project references; `dotnet-tools.json` defines repository-local .NET tools.
- `tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj` defines the Backend test stack. Run the
  smallest relevant project or solution verification for the change.
- A cross-stack contract change is complete only when `scripts/check-api-contract` passes and the
  committed OpenAPI spec and generated Frontend models match the Backend.
