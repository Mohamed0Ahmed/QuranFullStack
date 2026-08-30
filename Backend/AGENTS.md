# Backend Structure

`Backend/QuranDashboard.sln` contains the .NET 10 API, application, domain, infrastructure, shared,
tooling, and test projects.

## Project boundaries

- `domain/QuranDashboard.Domain` is independent.
- `shared/QuranDashboard.Shared` is independent and contains only cross-project primitives.
- `application/QuranDashboard.Application.Abstractions` depends on Domain and Shared.
- `application/QuranDashboard.Application` depends on Abstractions, Domain, and Shared.
- `infrastructure/QuranDashboard.Infrastructure` implements application abstractions and owns EF
  Core persistence, reads, writes, configurations, migrations, files, caching, and integrations.
- `api/QuranDashboard.Api` is the HTTP/composition entry point and references Application,
  Abstractions, Infrastructure, and Shared.
- `tools/QuranDashboard.DataImporter` and `tools/QuranDashboard.AccessAdmin` are separate CLI hosts.
- `tests/QuranDashboard.Tests` is the Backend test project.

## Placement

- Organize code by the owning bounded context or feature across layers. Existing top-level contexts
  include Quran, Abwab, Linking, and Access.
- API controllers live under `api/QuranDashboard.Api/Controllers/<feature>/`.
- Application commands and queries live beside their handlers under the owning feature.
- Contracts consumed by Application live under `Application.Abstractions` near the same feature.
- Infrastructure implementations follow the same feature under `Persistence/Reads`,
  `Persistence/Writes`, `Persistence/Configurations`, `Files`, or another concrete concern.
- For a placement or dependency decision, use `.architecture/BACKEND_STRUCTURE.md` and
  `.architecture/CLEAN_ARCHITECTURE.md`.
