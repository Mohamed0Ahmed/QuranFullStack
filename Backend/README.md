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

## Invariants

- Word identity keys on **clean imlaei-simple** (display stays Uthmani).
- Do not hand-write EF migrations or edit snapshots (see `AGENTS.md` → EF Core Migrations).
- `resources/` source packages are local/gitignored.
