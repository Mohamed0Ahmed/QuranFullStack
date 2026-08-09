# Quran Dashboard Backend

.NET 10 / ASP.NET Core / EF Core / PostgreSQL backend for the Quran Dashboard
(المنهج القرآني). Clean Architecture. The API is read-heavy over curated Quran data and
also exposes permission-protected Abwab writes plus Owner-only access administration;
bulk import and generation writes remain CLI-only.

> HOW to work here (rules): `.architecture/BACKEND_STRUCTURE.md`,
> `.architecture/CLEAN_ARCHITECTURE.md`, `.architecture/API_GUIDELINES.md`,
> `.architecture/LOGGING_GUIDELINES.md`. This file is the WHAT (current truth + map).

## Current scope

- **Mushaf reader** — pages, ayah study, similar ayahs (متشابهات), word analysis, catalogs.
- **Words explorers** — Roots, Lemmas, Stems, WordTypes, Unique Words (read-only).
- **Import/generate pipelines** — foundation, morphology (+ enriched), simple/full إعراب,
  mutashabihat, tafsirs, translations, navigation metadata, display-word rebuild.
- **Access foundation** — normalized identity, the 19-code Abwab catalogue, direct-grant/audit
  persistence, the operator conversion/preflight CLI, the request-scoped database authorization core, exact permission
  metadata on all twenty-one Abwab writes, and fail-closed unsafe-endpoint startup validation. Active Owners
  can administer non-Owner users, direct grants, audit history, and verified Logto-subject relinks through
  transactional Backend APIs; Owner membership/configuration remains reconciliation-only. Public GETs,
  including all four Abwab reads and the tree/template conditional requests, remain anonymous; production
  activation is a separate deployment gate.

## Layer map

```text
api/QuranDashboard.Api                 Controllers, ApiResponse contract, middleware
application/QuranDashboard.Application  CQRS handlers: Quran/{DataPipelines, MushafReader, Words}
  .Application.Abstractions             interfaces (persistence, data sources, services, paging)
domain/QuranDashboard.Domain           entities, enums, value objects, events
infrastructure/QuranDashboard.Infrastructure
  Files/Quran/DataPipelines/**          source readers + assemblers (import inputs)
  Persistence/DataPipelines/Quran/**    EF bulk writers (import outputs)      → see its README
  Persistence/Reads/Quran/**            read-only EF readers                  → Words/README.md
  Persistence/{Configurations,Migrations,QuranDashboardDbContext.cs}
shared/QuranDashboard.Shared           Result/Error primitives
tools/QuranDashboard.DataImporter      import/generate CLI                    → see its README
tools/QuranDashboard.AccessAdmin       access conversion/preflight CLI         → see its README
tests/QuranDashboard.Tests
scripts/                               dev CLI shortcuts                      → see its README
```

## Sub-area READMEs (read the nearest before changing)

- `infrastructure/.../Files/Quran/DataPipelines/Foundation/README.md`
- `infrastructure/.../Files/Quran/DataPipelines/Words/MorphologyImporting/README.md`
- `infrastructure/.../Files/Quran/DataPipelines/Words/SimpleI3rabGeneration/README.md`
- `infrastructure/.../Persistence/DataPipelines/Quran/README.md`
- `infrastructure/.../Persistence/Reads/Quran/Words/README.md`
- `tools/QuranDashboard.DataImporter/README.md`
- `tools/QuranDashboard.AccessAdmin/README.md`
- `report/README.md` (report locations + filename conventions)

## Build / run

```bash
cd App/Backend
dotnet build
dotnet run --project api/QuranDashboard.Api   # or: scripts/qd-api after scripts/qd-build
```

Connection string via user secrets — see `api/QuranDashboard.Api/README.md`. Swagger at
`https://localhost:5015/swagger`, health at `/api/health`.

## Deployment (Docker / Railway)

The API is containerized for Railway (Hobby). Artifacts live at the backend root:
`Dockerfile` (multi-stage: `sdk:10.0` build → `aspnet:10.0` runtime, publishes only
`api/QuranDashboard.Api`), `.dockerignore`, and `railway.json`. Those three files plus the
variables below are the whole contract — this section is the record, not a summary of one.

Build the image locally (build context is this `Backend/` folder):

```bash
docker build -f Backend/Dockerfile -t quran-api Backend
```

**Production config comes ONLY from environment variables** — nothing secret is baked into
the image (`appsettings.Production.json` is gitignored and excluded from the build context).
Railway must set:

| Variable | Value / note |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://0.0.0.0:${PORT}` — Kestrel does not read Railway's `$PORT` on its own |
| `ConnectionStrings__QuranDashboardDb` | `Host=…;Port=5432;Database=…;Username=…;Password=…;SSL Mode=Prefer` |
| `Cors__AllowedOrigins__0` | `https://manhag-qurany-ui.vercel.app` (array → **indexed** keys; a joined string leaves it empty and the app throws) |
| `Cors__VercelPreviewHostPrefix` | `manhag-qurany` (enables `*.vercel.app` preview CORS) |

TLS is terminated at Railway's edge; the container serves plain HTTP on `$PORT`
(`app.UseHttpsRedirection()` no-ops with no HTTPS port configured). Railway healthcheck path:
`/api/health` (liveness — returns `200` with per-check status in the body).

**One instance — a real constraint that nothing in the contract above enforces.** Railway runs a
single instance of this container today, but that is a fact about the current deployment, not
something `railway.json`, the `Dockerfile`, or any variable pins. It is load-bearing because the
Abwab conditional-GET validators are per-process: `AbwabCacheGeneration` is registered
`AddSingleton` (`infrastructure/.../DependencyInjection/AbwabDependencyInjection.cs:14`) and keeps
its boot id and its two generation numbers in instance fields
(`infrastructure/.../Caching/Abwab/AbwabCacheGeneration.cs:7-10`), which every ETag it mints embeds
(`:16-21`); the tree body is cached in the same process's `IMemoryCache` stamped with that counter
(`infrastructure/.../Caching/Abwab/CachedAbwabTreeReader.cs:20-29`). A write handled by one process
bumps only that process's counter, so a second instance would go on serving its own cached body and
answering `304` to its own clients' `If-None-Match` — stale data behind a validator that looks
fresh. **Before a second instance is added**, the generation has to become shared across processes
or the validator has to be derived from the data rather than from a process counter. Neither
exists; do not scale this service horizontally until one does.

## Invariants

- Word identity keys on **clean imlaei-simple** (display stays Uthmani).
- EF migrations are generated with EF tooling only. Add a migration only when explicitly
  requested, and do not apply one with `dotnet ef database update` without explicit authority.
- Do not hand-write migration files or manually edit generated migration `.cs`, `.Designer.cs`,
  or `ModelSnapshot` files except for a clearly documented exceptional fix. For an exception,
  explain why, list every manually edited file, and report the verification run.
- After generating a migration, report its name, generated files, build status, applicable test
  status, and whether the database update was executed or skipped.
- `resources/` source packages are local/gitignored.
