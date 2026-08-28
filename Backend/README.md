# Quran Dashboard Backend

.NET 10 / ASP.NET Core / EF Core / PostgreSQL backend for the Quran Dashboard
(المنهج القرآني). Clean Architecture. The API is read-heavy over curated Quran data and
also exposes permission-protected Abwab writes plus Owner-only access administration;
bulk import and generation writes remain CLI-only.

> This is the operational build/run/deployment guide. Active Spec Kit artifacts own feature
> intent, code owns implemented truth, and `.architecture/` owns Backend structure and API rules.

## Build / run

```bash
cd App/Backend
dotnet build
dotnet run --project api/QuranDashboard.Api   # or: scripts/qd-api after scripts/qd-build
```

Connection string via user secrets — see `scripts/README.md` §Prerequisites. Swagger at
`https://localhost:5015/swagger`, health at `/api/health`.

## Deployment (Docker / Railway)

Before release, run the mandatory Backend pre-release gate:

```bash
Backend/scripts/test-backend pre-pr
```

Daily Backend verification is `Backend/scripts/test-backend smoke` plus
`Backend/scripts/test-backend tier-b`. Change-specific gates are `pipeline`, `canonical-data`,
`migration`, `access-db`, and `gate-contract`; run each on its own trigger. The complete lane
contract and flags live in `tests/QuranDashboard.Tests/README.md` and `scripts/README.md`.

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

### PhraseSearch one-shot build capacity

PhraseSearch permits one data generation only. A build is refused when an active, previous,
non-failed, or child-data-bearing generation exists; rebuilding therefore requires a full database
reset first. Metadata-only failed audits do not block a retry. There is no replacement build,
blue/green overlap, A/B generation, or in-database rollback generation.

Before the sole build row is created, the storage preflight conservatively reserves one current
database size as one-shot build working space, one current database size for WAL headroom, plus
`PhraseSearch:DiskSafetyBytes` (4 GiB by default). This keeps the prior byte-safety margin without
assuming that a second PhraseSearch generation exists. The executable formula is in
[`PhraseDatabaseStoragePreflight.cs`](infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/PhraseSearch/PhraseDatabaseStoragePreflight.cs),
and the empty-generation guard runs before source bootstrap or staging in
[`EfPhraseIndexBuilder.cs`](infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/PhraseSearch/EfPhraseIndexBuilder.cs).

Prove the database filesystem's freshly available bytes and its WAL retention/archiving behavior
immediately before the one-shot build. Every environment, including loopback, fails closed unless
the operator supplies `PhraseSearch:VerifiedDatabaseFreeBytes` and sets
`PhraseSearch:DatabaseStorageProofContract` to
`operator-verified-database-filesystem-v1`; there is no automatic filesystem measurement. That
proof does not inspect the provider's WAL policy for the operator. WAL LSN deltas are cumulative
bytes generated during a run, not resident WAL bytes on disk, so they cannot replace a
resident-filesystem measurement. Build, report, refusal, and cancellation operations are retained in
[`scripts/README.md`](scripts/README.md#phrasesearch-index-operations).

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

- EF migrations are generated with EF tooling only. Add a migration only when explicitly
  requested, and do not apply one with `dotnet ef database update` without explicit authority.
- Do not hand-write migration files or manually edit generated migration `.cs`, `.Designer.cs`,
  or `ModelSnapshot` files except for a clearly documented exceptional fix. For an exception,
  explain why, list every manually edited file, and report the verification run.
- After generating a migration, report its name, generated files, build status, applicable test
  status, and whether the database update was executed or skipped.
- `resources/` source packages are local/gitignored.
