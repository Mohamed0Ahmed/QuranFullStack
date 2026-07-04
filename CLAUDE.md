## Backend Architecture Guides

Before adding or moving Backend files, folders, entities, enums, value objects, DTOs, handlers, services, EF configurations, controllers, infrastructure implementations, or `GlobalUsings.cs` files, read and follow:

- `.architecture/BACKEND_STRUCTURE.md`
- `.architecture/CLEAN_ARCHITECTURE.md`

Before adding or changing API endpoints, controllers, request/response contracts, middleware, Swagger/OpenAPI setup, API error handling, response shapes, API configuration, or health checks, read and follow:

- `.architecture/API_GUIDELINES.md`

Before adding or changing logging, exception handling, diagnostics, DataPipelines/importer run summaries, CLI console output, or other observability-related behavior, read and follow:

- `.architecture/LOGGING_GUIDELINES.md`

## Backend Local READMEs

- Before touching a backend area, read the nearest `README.md` (e.g.
  `infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/README.md`,
  `infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/README.md`,
  `infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md`,
  `tools/QuranDashboard.DataImporter/README.md`) before the `.architecture/*` HOW docs.
- If you change pipeline behavior, import commands/verbs, read-model derivation,
  identity/ordering invariants, source-safety handling, or an API contract that a
  README documents, update that README in the same change.
- Reports (`Backend/report/…`) stay evidence-only; do not spawn a new feature report
  for routine work. Migrations, EF snapshots, and source packages are not documented
  by README and must not be hand-edited.

## Backend Reports and Import Sources

Backend report outputs belong under:

- `report/feature-XXX-feature-name/` from the Backend repo perspective
- `/projects/Dashboard/App/Backend/report/feature-XXX-feature-name/` as an absolute path

Use this location for backend implementation reports, import reports, engineering review outputs, real-run reports, validation reports, and backend feature completion reports.

For backend report filename conventions, follow `report/README.md`: from Feature 006 onward, human-authored reports use three-digit chronological prefixes, while generated importer/tool outputs keep stable canonical names. Do not rename old reports unless explicitly requested.

Importer/source-data rules:

- Canonical local source packages live at `/projects/Dashboard/App/resources/import-sources/<feature-or-source-name>/`.
- `resources/` is local and gitignored; do not assume these files are committed or available in CI/production.
- Import features should read from staged/canonicalized source packages, not random upstream folders, when a staged package is required.
- Treat upstream source folders as provenance/read-only inputs unless the task explicitly asks to stage or canonicalize a package.
- Do not silently modify source data. Preserve traceability from imported/generated data back to the staged source package.

Planning and Spec Kit separation:

- Workspace planning reports and pre-Spec Kit documents belong under `/projects/Dashboard/App/docs/feature-XXX-feature-name/`.
- Spec Kit artifacts belong under `/projects/Dashboard/App/specs/`.
- Backend post-work and validation reports belong under `/projects/Dashboard/App/Backend/report/`, not under workspace `docs/`.

## EF Core Migrations

- Do not hand-write EF Core migrations.
- Do not manually create migration `.cs`, `.Designer.cs`, or `ModelSnapshot` files.
- Use EF Core tooling to generate migrations.
- Only add migrations when explicitly requested.
- Do not run `dotnet ef database update` unless explicitly requested.
- Do not manually edit generated migration designer files or model snapshots except for a clearly documented exceptional fix.
- If an exceptional manual migration fix is needed, explain why, list exactly which files were edited manually, and report the verification that was run.
- After generating a migration, report the migration name, generated files, build status, test status if applicable, and whether database update was executed or skipped.

## Backend Response Messages and Localization

For backend API response messages, localization, the `ApiResponse` shape, and
user-facing message rules, read (canonical):

- `.architecture/API_GUIDELINES.md`

Essential reminders:

- Arabic is the default user-facing response language.
- API identifiers/property names remain English.
- Do not scatter hardcoded user-facing messages; centralize them close to the
  owning feature.
- Do not invent Quranic/religious content while writing messages.


<!-- lean-ctx -->

## lean-ctx

lean-ctx is active — the MCP tools replace native equivalents.
Full rules: LEAN-CTX.md (open on demand — do not auto-load).

Use LeanCTX by default for reads/searches and shell-output compression. If LeanCTX blocks a specific command, run only that command outside LeanCTX with normal shell/Bash when available, then continue using LeanCTX.

<!-- /lean-ctx -->