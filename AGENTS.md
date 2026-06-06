## Backend Architecture Guides

Before adding or moving Backend files, folders, entities, enums, value objects, DTOs, handlers, services, EF configurations, controllers, infrastructure implementations, or `GlobalUsings.cs` files, read and follow:

- `.architecture/BACKEND_STRUCTURE.md`
- `.architecture/CLEAN_ARCHITECTURE.md`

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

- Backend user-facing messages must be localizable.
- Arabic is the default language.
- English should be supported for visitor-facing responses when language switching is needed.
- API property names, DTO property names, code identifiers, class names, method names, and database column names should remain English.
- Do not return repeated hardcoded user-facing success, error, validation, warning, or notification messages directly from controllers, handlers, services, validators, or middleware.
- Centralize reusable user-facing messages using localization resources, message keys, or feature-owned message constants.
- Prefer message keys such as:
  - `Common.NotFound`
  - `Common.ValidationFailed`
  - `MushafPages.InvalidPageNumber`
  - `Gates.CreatedSuccessfully`
- If a message belongs to one feature, keep its key/constants/resource close to that feature.
- If a message is truly shared, place it in a shared/common message location.
- Do not create broad dumping folders for unrelated messages.
- Technical protocol strings such as `Authorization`, `Bearer`, `application/json`, `GET`, or `POST` are not considered user-facing messages.
- If a language is missing or unsupported, fallback to Arabic.
- Do not invent Quranic/religious content while writing messages.
