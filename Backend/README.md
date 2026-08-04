# Quran Dashboard Backend

.NET 10 / ASP.NET Core / EF Core / PostgreSQL backend for the Quran Dashboard
(المنهج القرآني). Clean Architecture. **Read-only** over curated Quran data at the API;
writes happen only through the import/generate CLI.

> HOW to work here (rules): `.architecture/BACKEND_STRUCTURE.md`,
> `.architecture/CLEAN_ARCHITECTURE.md`, `.architecture/API_GUIDELINES.md`,
> `.architecture/LOGGING_GUIDELINES.md`. This file is the WHAT (current truth + map).

## Current scope

- **Mushaf reader** — pages, ayah study, similar ayahs (متشابهات), word analysis, catalogs.
- **Words explorers** — Roots, Lemmas, Stems, WordTypes, Unique Words (read-only).
- **Import/generate pipelines** — foundation, morphology (+ enriched), simple/full إعراب,
  mutashabihat, tafsirs, translations, navigation metadata, display-word rebuild.

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

## Invariants

- Word identity keys on **clean imlaei-simple** (display stays Uthmani).
- Do not hand-write EF migrations or edit snapshots (see `AGENTS.md` → EF Core Migrations).
- `resources/` source packages are local/gitignored.
