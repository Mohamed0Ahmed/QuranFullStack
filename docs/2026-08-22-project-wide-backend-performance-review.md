# Backend Performance Review

- Review date: 2026-08-22
- Review mode: initial project-wide static performance review with limited read-only cardinality evidence
- Current-state identity: `dev@1bcf56b1586153a65ee0cf74f0e944d3c04d3ed8`

## 1. Verdict

**CHANGES REQUESTED** — the Backend has evidence-backed MAJOR exposure in unbounded caller-controlled
cache key spaces, external provider work performed while database locks are held, a global Linking
revision lock spanning expensive preparation, repeated whole-corpus import scans, redundant multi-GB
package passes, and post-COPY validation that rematerializes complete persisted text corpora.

## 2. Scope reviewed

The review covered the .NET/ASP.NET Core/EF Core/PostgreSQL production paths across:

- API GET/write endpoints and their Application handlers.
- All `Infrastructure/Persistence/Reads/**` and `Persistence/Writes/**` paths.
- Caching, cache keys/options, invalidation, and Dependency Injection registration.
- Access/identity mutation, background jobs, leases, cleanup, transactions, and revision locks.
- EF configurations implicated by query/index findings.
- Quran DataPipelines, file/package readers, assemblers, Npgsql binary COPY writers, validation,
  reports, and `QuranDashboard.DataImporter` production code.

Three parallel passes inspected 269 read/query files, 203 write/background files, and 183 pipeline/
import files, with direct callers/callees and configurations followed where necessary. Overlap was
de-duplicated in this report.

Evidence and limitations:

- No code/data was mutated; no build, test, import, benchmark, or runtime service was started.
- PostgreSQL was reachable. At `2026-08-22 04:49–04:50 +03`, `pg_stat_user_tables` estimated a
  1,437 MB database with about 1.043M translation ayah rows, 523.8K tafsir ayah mappings, 382.5K
  tafsir entries, 128.2K morphology segments, 83.7K Quran words, 77.4K word-morphology rows,
  11.8K stems, 4.8K lemmas, and 16 Abwab doors.
- Exact table counts and `EXPLAIN (ANALYZE, BUFFERS)` were unavailable because the connected role
  could read statistics but had no `SELECT` permission on application tables. Cardinalities below are
  explicitly estimates.
- Read-only source-package measurements were about 292.6 MB translations, 1.026 GB tafsir,
  42.6 MB full I'rab, and 96.1 MB enriched morphology.
- Tests/specs/contracts, migrations/generated files, dependency audit, and measured test runtime were
  excluded.

## 3. Findings

### MAJOR

#### PBR-1 — Shared memory cache accepts unbounded caller-controlled key spaces

- Unique Words caches caller-controlled pages/filters, including non-null out-of-range results:
  `CachedUniqueWordsReader.cs:18-31`, `UniqueWordsCacheKeys.cs:11-28`, and
  `GetUniqueWordsPageHandler.cs:55-57`.
- Word Types caches rows/tables/scope counts under keys containing normalized free-form search:
  `CachedWordTypesReader.cs:24-61` and `WordTypesCacheKeys.cs:63-78`.
- Ayah Study keys include three raw source strings; unknown nonblank combinations still return and are
  cached: `MushafReaderCacheKeys.cs:7-25`, `GetAyahStudyHandler.cs:19-40`, and
  `CachedAyahStudyReader.cs:21-35`.
- Entries live 15–30 minutes, the shared `IMemoryCache` has no size limit, and rate limiting is
  disabled in production: `WordTypesCacheEntryOptions.cs:9-13`,
  `MushafReaderCacheEntryOptions.cs:5-9`, `MushafReaderDependencyInjection.cs:14`, and
  `appsettings.Production.json:22-30`.
- Impact: continuously varied page/filter/search/source values allocate distinct entries and execute
  miss work, creating GC pressure and a memory-exhaustion path.
- Suggested direction: use a dedicated bounded read-result cache with entry sizes/eviction; do not
  cache empty/out-of-range pages; bypass or tightly bound free-form search caching; cache only catalog
  source keys while preserving current unknown-source response behavior. Rate limiting is
  complementary, not a substitute.

#### PBR-2 — Identity-provider calls occur while database transactions and locks remain held

- `AccessUserMutationTransaction.cs:18-55` begins/locks, then awaits a mutation callback before
  commit. Subject relinking performs remote evidence/profile validation inside that callback at
  `EfLogtoSubjectRelinkService.cs:68,108,118`.
- Owner reconciliation acquires a transactional lease/locked snapshot at
  `OwnerReconciliationService.cs:53-56`, then performs sequential provider calls in
  `BuildPlanAsync` before commit at `:69-70`.
- Legacy conversion similarly holds its lease while resolving owner status.
- Impact: provider latency, throttling, or timeout directly lengthens row/advisory locks and exposes
  unrelated access mutations to contention/deadlock/timeout.
- Suggested direction: fetch remote evidence before locking, then acquire locks and re-read/revalidate
  every mutation-critical database fact before commit. Do not weaken uniqueness, authorization,
  transaction, or reconciliation guarantees.

#### PBR-3 — Global Linking revision lock spans source resolution and result persistence

- `EfLinkingDataRevisionReadScope.cs:43-52` starts repeatable-read, takes a shared lock on the single
  `linking_data_state` row, and holds it for the entire callback.
- `ProcessLinkingPreparedPreflightHandler.cs:117-209` performs source input building, confirmed-state
  loading, classification, hashing, and persistence under that scope.
- `LinkingPreparedPreflightInputBuilder.cs:24-57` resolves sources sequentially and accumulates every
  batch; `EfLinkingPreparedPreflightStore.Results.cs:25-138` drains and persists results under the same
  transaction.
- The synchronous workspace writer has the same long shared-lock topology, and prepared-preflight
  creation has no local source-count maximum.
- Impact: every mutation needing the exclusive revision lock waits for full source resolution and
  persistence rather than a short revision check.
- Suggested direction: first instrument lock-hold/wait time. Only if snapshot equivalence can be
  proven, prepare immutable input outside the lock, acquire a short locked phase, revalidate revision,
  load/classify/persist, and retry on revision change. Keep the current model if correctness cannot be
  preserved.

#### PBR-4 — Translation/Tafsir bulk writers repeatedly rescan flattened corpora

- `TranslationBulkCopier.cs:13-24` filters the full ayah-entry collection for every source, then opens
  source COPY, source-ID lookup, and ayah COPY per source.
- At the measured estimates, this is roughly `167 × 1.043M = 174M` SourceKey predicates and about
  501 logical COPY/SELECT operations.
- `TafsirBulkCopier.cs:14-30,43-63` rescans both full entry collections for each source; estimates imply
  about `84 × (382.5K + 523.8K) = 76M` predicates and about 420 operations.
- Suggested direction: bucket once by `SourceKey` or retain per-source buckets from assembly; COPY
  sources once, read IDs once, COPY entries once, resolve entry IDs once where required, and COPY ayah
  mappings once. Preserve association, ordering, validation, and atomicity.

#### PBR-5 — Large source packages are hashed/parsed repeatedly, including inside write transactions

- Tafsir pass ownership is visible in `TafsirImportSource.cs:34-35,93-100` and
  `TafsirManifestReader.cs:60-78,129-156`.
- Tafsir `ReadAsync` hashes approved files, digest capture reads/hashes them again, and unchanged
  verification repeats capture. Current topology is about five hashing passes over the ~1.026 GB
  package (~5.13 GB read), with two passes inside the acceptance transaction.
- `ManifestChecksum.cs:5-6` uses `File.ReadAllBytes`, producing a whole-file allocation; the largest
  current tafsir source is about 66.6 MB.
- Enriched morphology traverses its 96.1 MB artifact six times: four hashes and two JSON parses
  (`EnrichedMorphologyImportSource.cs:27-38,92-94` and
  `EnrichedMorphologyManifestReader.cs:72,86-146`).
- Suggested direction: return the validated initial digest snapshot from manifest reading, use the
  streaming hash path, and combine enriched structural counting/building passes where practical.
  Retain one independent post-write source-unchanged rehash before commit.

#### PBR-6 — Post-COPY validation rematerializes complete persisted text corpora

- `TranslationValidationRunner.cs:60-84` loads all persisted source/verse/text values into a
  dictionary beside the already-materialized ~1.043M source rows.
- `TafsirValidationRunner.cs:57-83` does the same for every persisted tafsir text/hash beside the
  ~1.026 GB source package.
- `FullI3rabValidationRunner.cs:136-162` repeats the shape at lower current cardinality.
- Impact: database-returned strings, dictionary nodes/keys, and existing source DTOs plausibly create
  multi-GB peak memory while the import transaction remains open.
- Suggested direction: stream the ordered persisted result and compare exact text incrementally
  against a source-keyed expected structure. A digest may be used only as a prefilter with exact
  comparison on every potentially accepted path; preserve exact text equality, counts, mismatch
  reporting, source checks, and rollback.

### MINOR

#### PBR-7 — Expensive finite cold loads are not consistently single-flighted

- Whole Lemma/Stem/Root summaries and Mushaf page loads use miss -> expensive load -> set without the
  existing `CacheLoadGate` (`CachedLemmasReader.cs:103-112`, `CachedStemsReader.cs:116-125`,
  `CachedRootsReader.cs:174-183`, `CachedMushafPageReader.cs:8-23`).
- The summaries run whole-corpus aggregates; a Mushaf page miss performs several reads.
- Impact: concurrent startup/prefetch misses duplicate database CPU and latency.
- Suggested direction: single-flight only finite keys such as the three summaries and 604 Mushaf
  pages. Do not use the lifetime-retained gate for unbounded caller-controlled keys.

#### PBR-8 — Ayah Study caches only whole combinations and repeats invariant reads

- `CachedAyahStudyReader.cs:15-35` caches `(verse, tafsir, translation, full-i3rab)` as one unit.
- Each changed selector repeats verse/surah, sajda, similarity, and unchanged source reads; a default
  cold response can issue up to six sequential commands (`EfAyahStudyReader.cs:23-48`).
- Suggested direction: after PBR-1 bounds source keys, cache invariant verse core by verse and each
  source block by `(verse, source key)`, then compose. Preserve exact identity/provenance/missing
  behavior and do not parallelize commands on one EF `DbContext`.

#### PBR-9 — Bulk door move has quadratic lookups and per-door alias queries

- No local batch maximum exists. `EfAbwabDoorsWriter.cs:252-255,283-284,307-310` repeatedly calls
  `loaded.Single(...)` inside batch work.
- `ToDtosAsync` serially calls `ToDtoAsync`; each call queries aliases separately at `:707-728`.
- Current `pg_stat` estimated only 16 doors, so the present heat is proportionally low; the shape is
  still unbounded and scales as O(B²) plus up to B sequential alias queries.
- Suggested direction: build `loadedById` once and batch-load aliases for all returned IDs. A request
  cap changes behavior/contract and should be considered separately.

#### PBR-10 — Failed/no-op Abwab writes invalidate cache generations

- `InvalidatingAbwabDoorsWriter.cs:23-30,33-49,114-123` and
  `InvalidatingAbwabTemplatesWriter.cs:18-27,30-39` increment generations in `finally`, so
  validation/not-found/concurrency failures and confirmed no-ops make the next tree/template read
  cold. The `DeleteAsync` path can return `false` and still invalidates; the same pattern repeats
  across the section/relation methods.
- Linking already demonstrates selective successful non-no-op invalidation.
- Suggested direction: invalidate after a confirmed committed mutation, retaining conservative
  invalidation for ambiguous commit outcomes. Confirm whether attempted-write ETag churn is intended.

#### PBR-11 — Confirmation cleanup updates a row immediately before deleting it

- `EfLinkingConfirmationJobStore.Maintenance.cs:11-41` locks one terminal row, updates cleanup/lease
  fields and saves, then immediately deletes and saves again in the same transaction.
- The intermediate update cannot become independently visible and adds a round trip plus WAL work.
- Suggested direction: delete with one save/commit unless an unobserved trigger/audit dependency
  requires the update; none was found in inspected configuration.

### NOTE — measure before changing

#### PBR-12 — Morphology/display validation issues many sequential scalar scans

- `MorphologyImportReportBuilder.cs:17-30` issues 13 sequential scalar calls;
  `MorphologyValidationRunner.cs:36-43,57-111,137-315` dispatches another 27 scalar calls. They run
  from the acceptance transaction at `EfBulkMorphologyWriter.cs:117-126`.
- `SqlDisplayWordsRebuilder.cs:251-333,468-482` hard-checks counts and then queries several ordered/
  simple/unique/readable totals again.
- Direction after phase timing/query stats: combine independent counts in one row or multi-result
  command, use filtered aggregates, and reuse totals. Keep every hard check on the same snapshot.

#### PBR-13 — Display rebuild expands the same ranked 77,432-word base four times

- `DisplayWordsSql.cs:5-149` repeats a windowed joined base in four statements executed sequentially.
- Measure each with `EXPLAIN (ANALYZE, BUFFERS)`; if material, stage the ranked base once inside the
  transaction while preserving deterministic IDs/order and post-build checks.

#### PBR-14 — Non-force target checks follow expensive package loading

- Translation loads before its non-force guard at `ImportTranslationsHandler.cs:34-37,69-72`;
  equivalent ordering appears in `ImportTafsirsHandler.cs:33-36,63-66`,
  `ImportMorphologyHandler.cs:30-35`, `ImportFullI3rabHandler.cs:33-36,63-66`, and
  `ImportMutashabihatHandler.cs:28-31,50-52`.
- A normal non-force refusal can hash/parse the full package before returning.
- Moving a cheap guard earlier changes error precedence; only do so with explicit behavior approval
  and keep a writer-side recheck.

#### PBR-15 — Cleanup claims at most one terminal item per interval

- `EfLinkingConfirmationJobStore.Maintenance.cs:11-24` and
  `EfLinkingPreparedPreflightStore.Maintenance.cs:113-135` each use `LIMIT 1`.
- Their hosted services call maintenance once per `CleanupInterval` at
  `LinkingConfirmationJobCleanupService.cs:14-35` and
  `LinkingPreparedPreflightCleanupService.cs:14-35`; the default is five minutes at
  `LinkingScalabilityOptions.cs:25`. That is nominally 288 terminal items/day per store/instance.
  Current backlog/ingress is unknown.
- Measure eligible backlog, oldest age, ingress, and duration before considering a bounded drain loop
  or batch with `SKIP LOCKED`.

#### PBR-16 — Prepared-preflight “streaming” retains complete source representations

- `LinkingPreparedPreflightInputBuilder.cs:19-160` appends all batches, then builds hashes, requested/
  included lists, units, dictionaries, and intent units over the retained source.
- Measure allocations, LOH, peak working set, GC pauses, source cardinality, and concurrent workers
  before redesign. Preserve classification and provenance fidelity.

#### PBR-17 — Latest reconciliation JSON lookup lacks a matching expression index

- `EfAccessAuditReader.cs:78-89` filters JSON provenance operation; available indexes do not match
  that expression.
- This is a low-frequency administrative path. Add a partial/expression index only if a realistic
  `EXPLAIN` demonstrates material work.

#### PBR-18 — Door-link snapshot/ordering scale with complete door size

- `EfDoorLinkRecordsReader.Snapshot.cs:22-119` materializes every live unit, ayah, selected word,
  description, and fully hydrated ayah for a door.
- `EfDoorLinkRecordsReader.cs:24-69` orders and summarizes records through correlated subqueries.
  Supporting join/order indexes exist.
- Measure largest-door payload, allocations, command time, and query plans. Pagination/contract
  splitting would be behavior-changing.

### Rejected false positives

- EF read paths consistently use `AsNoTracking`; no per-row awaited DB work was found in read loops.
- Unique/Word Types normalized text search has GIN trigram indexes.
- Access audit paging is keyset-based and indexed by descending time/id.
- Linking source/ayah caches are already dedicated, size-limited, and coordinated.
- Normal Word Types pages carry `COUNT(*) OVER()`; a separate count is only the empty-page fallback.
- Prepared Linking relational apply already uses bulk SQL/temp tables.
- Background channels are bounded and workers await signals/delays; no busy polling was found.
- Confirmation transaction splitting was rejected where atomic idempotency/revision/preflight/door
  state must commit together.
- Mandatory checksum, post-write unchanged, exact-text, report, and rollback work was never treated as
  removable; only redundant passes/materialization were retained.

## 4. Quran data safety

**PASS for the recommendations.** None removes or weakens manifest checksums/sizes, post-write
source-unchanged verification, exact persisted-text comparison, hard validation, deterministic Quran
mapping/order, transactions, rollback, reports, or provenance. “Slower but correct” remains the
constraint; revision-lock and import-memory optimizations must prove equivalent snapshot and data
integrity before adoption.

## 5. Next step

Fix PBR-1 first because it is an externally variable memory-pressure path. In parallel, add phase/
lock/cache telemetry for PBR-2 through PBR-6, then implement the smallest behavior-preserving
optimizations: two-phase revalidated identity work, revision-validated preparation, one-time source
grouping, streaming hashes, and incremental exact validation. Re-run a controlled read-only profile/
import benchmark before trusting closure.
