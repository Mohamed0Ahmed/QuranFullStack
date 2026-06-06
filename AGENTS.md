## Backend Architecture Guides

Before adding or moving Backend files, folders, entities, enums, value objects, DTOs, handlers, services, EF configurations, controllers, infrastructure implementations, or `GlobalUsings.cs` files, read and follow:

- `.architecture/BACKEND_STRUCTURE.md`
- `.architecture/CLEAN_ARCHITECTURE.md`

Before adding or changing API endpoints, controllers, request/response contracts, middleware, Swagger/OpenAPI setup, API error handling, response shapes, API configuration, or health checks, read and follow:

- `.architecture/API_GUIDELINES.md`

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
