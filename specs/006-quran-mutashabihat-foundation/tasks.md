# Tasks: Quran Mutashabihat Foundation

**Input**: Design documents from `specs/006-quran-mutashabihat-foundation/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/` (all present)

**Tests**: INCLUDED. This is a data-integrity feature; the "trustworthy, hard-gated import" value (US3/US4)
is only provable with tests. Integration tests use Testcontainers PostgreSQL with **synthetic, source-safe**
tiny groups/links (never real verse passages); readers/assembler have pure unit tests.

**Organization**: Tasks are grouped by user story. This feature is **one importer pipeline**
(`import-mutashabihat`) that builds **three** tables in one transaction, so the stories are *facets* of
that pipeline. Each facet is independently **testable** against the produced database, and the phases build
the pipeline incrementally in priority order. **MVP = US1 + US2** (both P1).

---

## Conventions for the implementer (READ FIRST)

- **Repo root** = `/projects/Dashboard/App`. All paths below are repo-relative. The Backend solution root
  is `Backend/`.
- **DB columns** are `snake_case`; **C# entities/properties** are `PascalCase`. Types: `smallint` for
  values ≤ 32,767, else `int`; `jsonb` only for `raw_source_counts` and `match_words` (see `data-model.md`
  / research R15).
- **Authoritative sources (do not invent values):** column lists & keys → `data-model.md`; interface
  signatures, records, DTOs & constants → `contracts/mutashabihat-abstractions.md`; verb behavior →
  `contracts/cli-verb.md`; check IDs & report shape → `contracts/validation-report.schema.md`; rationale →
  `research.md`. Fixed counts: **814** groups / **3,558** raw occurrences / **1** duplicate / **3,557**
  stored occurrences / **1,162** sources / **3,552** links / **3,084** distinct ayahs.
- **Mirror existing code.** This feature is a near-clone of **Feature 004 (morphology)**. For every new
  file, copy the structure/style of the named **precedent file** under `Backend/.../Quran/.../Morphology/`.
- **Canonical ayah target:** resolve every `verse_key` (group `source.key`, every occurrence `verse_key`,
  both ends of every link) against `quran_ayahs.verse_key` (UNIQUE) → store the integer `ayah_id` FK.
  **Never store raw `verse_key` strings.** Precedent for the FK target: `AyahConfiguration.cs`
  (`Id` PK `ValueGeneratedNever`, `verse_key` UNIQUE).
- **Quranic data safety (non-negotiable):** never modify `quran_ayahs` / `quran_words` / the Quran text;
  never write the source files; **no ayah text copied** (refs + word positions only); store `coverage`
  **raw** (no clamp); store directed links **faithfully** (no reverse rows); anomalies are recorded as
  **warnings**, never corrected; tests use fabricated tiny tokens only.
- **Do not** create the EF migration or run `dotnet ef database update` except at the explicitly-flagged
  task, and only on explicit request (`Backend/CLAUDE.md`). No `HasData`.
- **`[P]`** = may run in parallel (different files, no dependency on an incomplete task).

---

## Phase 1: Setup (orientation)

**Purpose**: Establish the baseline and the patterns to mirror.

- [x] T001 Verify the baseline builds and read the precedent files to mirror. Run `dotnet build Backend`
  for a green baseline. Then open and skim these precedents (do **not** edit) — every new file in this
  feature mirrors one of them:
  - ayah FK target: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/AyahConfiguration.cs`
  - entity (surrogate `id` + plain props): `Backend/domain/QuranDashboard.Domain/Quran/Words/Morphology/QuranRoot.cs`; entity with a `jsonb` prop: `.../Morphology/WordMorphologySegment.cs`
  - EF config (identity PK + UNIQUE + index): `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Words/Morphology/QuranRootConfiguration.cs`; config with FK + composite UNIQUE + `jsonb` column: `.../Morphology/WordMorphologySegmentConfiguration.cs`
  - manifest + JSON readers + source + assembler: `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Morphology/{MorphologyManifestReader,JsonAlignedCorpusReader,MorphologyImportSource,MorphologyAssembler}.cs`
  - bulk COPY writer + validation SQL: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Morphology/{EfBulkMorphologyWriter,MorphologyBulkCopier,MorphologySql,MorphologyValidationRunner}.cs`
  - report writer: `Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/Morphology/MarkdownJsonMorphologyReportWriter.cs`
  - command/handler/result: `Backend/application/QuranDashboard.Application/Quran/Words/ImportMorphology/{ImportMorphologyCommand,ImportMorphologyHandler,ImportMorphologyResult}.cs`
  - abstractions: `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Morphology/*.cs`
  - DI: `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection.cs`
  - verb dispatch: `Backend/tools/QuranDashboard.DataImporter/Program.cs` (`RunImportMorphologyAsync`, `ResolveDefaultMorphologySourcePath`)
  - test fixture + test DI: `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/{MorphologyImportTestFixture,MorphologyTestServiceCollectionExtensions}.cs`

---

## Phase 2: Foundational (BLOCKING — must finish before US1–US5)

**Purpose**: Compile-safe schema/domain/abstractions/reader scaffolding + the schema migration + the test
fixture. This phase does **not** wire production DI or expose the CLI verb (those land in US3 after the
concrete handler/source/writer/report-writer exist). **⚠️ No user-story work begins until this phase is done.**

### Domain — entities (`Backend/domain/QuranDashboard.Domain/Quran/Mutashabihat/`; plain data carriers, no behavior; mirror `QuranRoot.cs`)

- [x] T002 [P] Create `MutashabihatGroup.cs` — props for `quran_mutashabihat_groups` (data-model §1):
  `Id` (int), `SourceGroupId` (int), `RepresentativeAyahId` (int), `RepresentativeWordFrom` (short),
  `RepresentativeWordTo` (short), `OccurrenceCount` (short), `DistinctAyahCount` (short),
  `DistinctSurahCount` (short), `RawSourceCounts` (string?, holds jsonb).
- [x] T003 [P] Create `MutashabihatOccurrence.cs` — props for `quran_mutashabihat_occurrences`
  (data-model §2): `Id` (int), `GroupId` (int), `AyahId` (int), `WordFrom` (short), `WordTo` (short),
  `IsRepresentative` (bool).
- [x] T004 [P] Create `SimilarAyahLink.cs` — props for `quran_similar_ayah_links` (data-model §3):
  `Id` (int), `SourceAyahId` (int), `TargetAyahId` (int), `Score` (short), `Coverage` (short),
  `MatchedWordsCount` (short), `MatchWords` (string, holds jsonb).

### Infrastructure — EF configurations (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Mutashabihat/`; mirror `QuranRootConfiguration.cs` / `WordMorphologySegmentConfiguration.cs`)

- [x] T005 [P] Create `MutashabihatGroupConfiguration.cs` — table `quran_mutashabihat_groups`; identity PK
  `id` (`ValueGeneratedOnAdd`); **UNIQUE(`source_group_id`)**; FK `representative_ayah_id` →
  `quran_ayahs.id`; index on `representative_ayah_id`; `raw_source_counts` as `jsonb` (nullable). Columns
  per data-model §1.
- [x] T006 [P] Create `MutashabihatOccurrenceConfiguration.cs` — table `quran_mutashabihat_occurrences`;
  identity PK `id`; FK `group_id` → `quran_mutashabihat_groups.id` **ON DELETE CASCADE**; FK `ayah_id` →
  `quran_ayahs.id`; **UNIQUE(`group_id`, `ayah_id`, `word_from`, `word_to`)**; index on `ayah_id`;
  `is_representative` NOT NULL default `false`. Per data-model §2.
- [x] T007 [P] Create `SimilarAyahLinkConfiguration.cs` — table `quran_similar_ayah_links`; identity PK
  `id`; FKs `source_ayah_id` / `target_ayah_id` → `quran_ayahs.id`; **UNIQUE(`source_ayah_id`,
  `target_ayah_id`)**; **CHECK(`source_ayah_id <> target_ayah_id`)**; index on `target_ayah_id`;
  `match_words` as `jsonb` (NOT NULL). Per data-model §3.

### Infrastructure — DbContext

- [x] T008 Add three `DbSet<>`s (`MutashabihatGroup`, `MutashabihatOccurrence`, `SimilarAyahLink`) to
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs`.
  Configurations are auto-discovered (same as Feature 004) — do not register them manually.

### Application.Abstractions (`Backend/application/QuranDashboard.Application.Abstractions/Quran/Mutashabihat/`; mirror `Quran/Words/Morphology/` records)

- [x] T009 [P] Create `MutashabihatInvariants.cs` + `MutashabihatExpectedCounts.cs` — exact constants,
  `Production` instance, score range, and messages (`TargetsNotEmpty`, `SourceMismatch`, `AyahsMissing`)
  from `contracts/mutashabihat-abstractions.md` → "MutashabihatInvariants".
- [x] T010 [P] Create result records `MutashabihatImportResult.cs`, `MutashabihatImportTotals.cs`,
  `MutashabihatCheckResult.cs` — exact shapes in `contracts/mutashabihat-abstractions.md` → "Records".
- [x] T011 [P] Create source DTOs `MutashabihatSourceData.cs`, `PhraseGroupDto.cs`, `OccurrenceDto.cs`,
  `SimilarLinkDto.cs` — exact shapes in `contracts/mutashabihat-abstractions.md` → "Source DTOs".
- [x] T012 [P] Create interfaces `IMutashabihatImportSource.cs`, `IMutashabihatImportWriter.cs`,
  `IMutashabihatReportWriter.cs` — exact signatures in `contracts/mutashabihat-abstractions.md`. Expose
  records/DTOs only, never EF entities.

### Infrastructure — source reading (`Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Mutashabihat/`; mirror `Files/Quran/Morphology/`)

- [x] T013 [P] Create `MutashabihatManifestReader.cs` — read `manifest.json`; verify the source folder
  contains **exactly** `manifest.json`, `README.md`, `mutashabihat-ul-quran/phrases.json`,
  `similar-ayahs/matching-ayah.json` (reject missing files, extra/derived files like `phrase_verses.json`,
  wrong `expectedRecordCount`, wrong `fileSizeBytes`, wrong `sha256`); expose a recompute of size/sha256
  for `MUT-SOURCE-UNCHANGED`. Mirror `MorphologyManifestReader.cs`. (Computes `MUT-MANIFEST-SET`,
  `MUT-MANIFEST-CHECKSUM`.)
- [x] T014 [P] Create `JsonPhrasesReader.cs` — parse `mutashabihat-ul-quran/phrases.json` with
  `System.Text.Json`; yield per group: `source_group_id` (the object key, opaque int), `source` =
  `{key, from, to}`, and the `ayah` map `verse_key → [[word_from, word_to], …]`. Validate `{source, ayah}`
  shape (`MUT-JSON-SHAPE`); count raw occurrence entries (`MUT-RAW-OCCURRENCE-COUNT` = 3,558). Do NOT
  resolve `ayah_id` here (the assembler does that). Mirror `JsonAlignedCorpusReader.cs`.
- [x] T015 [P] Create `JsonSimilarAyahReader.cs` — parse `similar-ayahs/matching-ayah.json`; yield per
  source ayah (the object key = source `verse_key`) a list of items
  `{matched_ayah_key, score, coverage, matched_words_count, match_words}`. Validate the 5-field item shape
  (`MUT-JSON-SHAPE`). Preserve `match_words` ranges verbatim. Mirror `JsonAlignedCorpusReader.cs`.

### Infrastructure — schema migration (single migration for the feature)

- [x] T016 Generate the single schema-only EF migration `AddQuranMutashabihat`, once T002–T008 exist and
  `dotnet build Backend` is green:
  `dotnet ef migrations add AddQuranMutashabihat --project Backend/infrastructure/QuranDashboard.Infrastructure --startup-project Backend/api/QuranDashboard.Api`.
  **Generating this migration is pre-approved as part of implementing Feature 006** — it is the explicitly
  flagged migration task this feature's `Backend/CLAUDE.md` "on explicit request" rule points to, and it is
  required so the Testcontainers integration tests (T017+) can `MigrateAsync`; the implementer does not need
  to pause for separate approval to run `dotnet ef migrations add` here. Review the generated migration: it
  must create exactly the **3** tables with their UNIQUE constraints, the
  `source_ayah_id <> target_ayah_id` CHECK, the `occurrences.group_id` ON DELETE CASCADE, and the read
  indexes (`occurrences(ayah_id)`, `links(target_ayah_id)`), with **no `HasData`**. **Do NOT run
  `dotnet ef database update` unless explicitly requested.** Report the migration name, generated files, and
  build status.

### Tests — shared fixture (`Backend/tests/QuranDashboard.Tests/Quran/Mutashabihat/`; mirror `WordsMorphology/`)

- [x] T017 [P] Create `MutashabihatImportTestFixture.cs` + `MutashabihatTestServiceCollectionExtensions.cs`
  — Testcontainers `postgres:16-alpine` with `MigrateAsync`; helpers to (a) seed a small set of
  **synthetic, source-safe** `quran_ayahs` rows (fabricated `verse_key`s, e.g. `900:1`, `900:2`); (b) write
  a temporary staged source folder (manifest + tiny `phrases.json` + tiny `matching-ayah.json` + README)
  using fabricated keys; (c) wire the concrete source/writer/report-writer/handler for tests; (d) inject
  source-file-set violations, missing/empty `quran_ayahs`, forced reruns, hard-check failures; (e) take
  stable ordered table snapshots/hashes. Mirror `MorphologyImportTestFixture.cs` +
  `MorphologyTestServiceCollectionExtensions.cs`. Reuse in all later test tasks.

**Checkpoint**: Solution compiles; 3 entities/configs/DbSets exist; the migration is generated; manifest +
two JSON readers + abstractions + test fixture are in place. Production DI and the CLI verb are deferred to
US3 (T034–T035).

---

## Phase 3: User Story 1 — Repeated-phrase groups become queryable data (Priority: P1) 🎯 MVP

**Goal**: Produce **814** `quran_mutashabihat_groups` rows and **3,557** `quran_mutashabihat_occurrences`
rows (from 3,558 raw entries, 1 duplicate collapsed), every occurrence pointing at a real `quran_ayahs`
row, counters recomputed, the representative occurrence flagged.

**Independent Test**: After import, exactly 814 groups and 3,557 stored occurrences exist; every occurrence
`ayah_id` resolves; every group has `distinct_ayah_count ≥ 2`; at most one `is_representative` per group;
group `1782` (anchor `3:28`) has zero representative occurrences but keeps its group-level representative
fields; counts equal the recomputed values.

### Tests for User Story 1 (write FIRST; they must FAIL before T020–T024)

- [ ] T018 [P] [US1] Create `MutashabihatReaderTests.cs` — pure unit (no DB): `JsonPhrasesReader` parses a
  tiny `phrases.json` into the expected group/occurrence shapes; opaque `source_group_id`, `source`
  `{key, from, to}`, and ragged `ayah` ranges preserved; raw occurrence entries counted correctly.
- [ ] T019 [P] [US1] Create `MutashabihatAssemblerTests.cs` (groups part) — assert: `verse_key → ayah_id`
  resolution; the 1 duplicate identical occurrence collapses (3 raw → 2 stored in a fixture mirroring group
  75); counters recomputed (a fixture with stale source counters → stored counts come from the actual
  occurrences, raw kept in `RawSourceCounts`); representative occurrence flagged; the `source.key`-absent
  group kept with zero representative occurrences and group-level representative fields still set.

### Implementation for User Story 1

- [ ] T020 [US1] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Mutashabihat/MutashabihatAssembler.cs`
  (groups + occurrences part): given the parsed phrases + the `verse_key → ayah_id` map, for each group
  resolve `source.key` → `RepresentativeAyahId` and copy `source.from/to`; expand the `ayah` map into
  `OccurrenceDto`s (resolve each `verse_key` → `AyahId`); **collapse duplicates** on
  (`ayah_id`, `word_from`, `word_to`); flag the one occurrence equal to `source` as
  `IsRepresentative = true` (group `1782`: none); **recompute** `OccurrenceCount` /
  `DistinctAyahCount` / `DistinctSurahCount`; set `RawSourceCounts` from the source `{surahs, ayahs, count}`.
  Logic per `research.md` R5–R7 and `data-model.md` §1–§2. Mirror `MorphologyAssembler.cs`.
- [ ] T021 [US1] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Mutashabihat/MutashabihatImportSource.cs`
  implementing `IMutashabihatImportSource.LoadAsync` + `SourceUnchangedAsync` — verify the manifest (T013),
  parse both files (T014/T015), read `quran_ayahs.{id, verse_key, words_count_real}` (read-only) and build
  the `verse_key → ayah_id` map, run the assembler (T020) into `MutashabihatSourceData`. If `quran_ayahs`
  is missing/empty, surface a clean early refusal (`AyahsMissing`); if any reference fails to resolve,
  surface it for the `MUT-AYAH-RESOLVE` hard check. Capture pre-run file digests. Mirror
  `MorphologyImportSource.cs`. (Links assembly is added in US2 — T027.)
- [ ] T022 [US1] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Mutashabihat/EfBulkMutashabihatWriter.cs`
  (groups + occurrences COPY part): implement `AnyTargetTableHasDataAsync` and the first `ImportAsync`
  path — inside a transaction, Npgsql binary `COPY` `quran_mutashabihat_groups` then
  `quran_mutashabihat_occurrences` (FK-safe order; occurrences reference groups), streaming surrogate ids.
  Mirror `EfBulkMorphologyWriter.cs` + `MorphologyBulkCopier.cs`. (Full validate-before-commit gate is
  completed in US3 — T032; for now COPY inside a transaction and commit.)
- [ ] T023 [US1] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Mutashabihat/MutashabihatSql.cs`
  and add the US1 hard-check queries: `MUT-GROUP-COUNT` (814), `MUT-STORED-OCCURRENCE-COUNT` (3,557),
  `MUT-OCCURRENCE-UNIQUE`, `MUT-GROUP-MIN-SIZE` (≥2), `MUT-WORD-RANGE-SHAPE` (occurrence ranges),
  `MUT-VERSEKEY-FORMAT` + `MUT-AYAH-RESOLVE` (group source.key + occurrence refs). Plus the assembly-time
  `MUT-RAW-OCCURRENCE-COUNT` (3,558). Exact assertions in `data-model.md` "Validation invariants" +
  `contracts/validation-report.schema.md`. Mirror `MorphologySql.cs`.
- [ ] T024 [US1] Create the Application command/handler
  `Backend/application/QuranDashboard.Application/Quran/Mutashabihat/ImportMutashabihat/{ImportMutashabihatCommand,ImportMutashabihatHandler,ImportMutashabihatResult}.cs`
  — orchestrate early refusals first (source/manifest mismatch → refuse; `quran_ayahs` missing/empty →
  refuse; `!force` && `AnyTargetTableHasDataAsync` → refuse), else `ImportAsync` → map verdict to
  `ExitCode`. Early refusals write nothing and no report. Mirror `ImportMorphologyHandler.cs`. Wire the
  report writer later (T039). Then create `MutashabihatImportTests.cs` (US1 e2e via the test service
  collection): seed fixture → run import → assert 814 groups / 3,557 occurrences / counters / representative
  flag / group `1782` anomaly.

**Checkpoint**: Running the handler path against a fixture produces correct groups + occurrences; US1 tests
pass. The CLI verb is wired in US3 (T035).

---

## Phase 4: User Story 2 — Similar-ayah links become queryable data (Priority: P1)

**Goal**: Produce **3,552** `quran_similar_ayah_links` rows across **1,162** source ayahs, both ends
resolving, `score` 50–100, `coverage` stored **raw** (4 rows > 100 kept), 0 self-links, `match_words`
preserved, **no** reverse rows.

**Independent Test**: After import, exactly 3,552 links over 1,162 distinct `source_ayah_id`; both ends
resolve; 0 self-links; coverage raw (the 4 rows > 100 retained); `match_words` equals the source ranges;
stored link count is exactly 3,552 (no synthesized reverse).

### Tests for User Story 2 (write FIRST; they must FAIL before T027–T029)

- [ ] T025 [P] [US2] Create `MutashabihatSimilarReaderTests.cs` — pure unit: `JsonSimilarAyahReader` parses
  a tiny `matching-ayah.json` into directed link items; `score`/`coverage`/`matched_words_count`/ragged
  `match_words` preserved; a coverage > 100 value is read unchanged.
- [ ] T026 [P] [US2] Extend `MutashabihatAssemblerTests.cs` (links part) — assert: both ends resolve to
  `ayah_id`; a fixture with a one-way link produces no reverse row; a coverage-200 row stays 200; a
  self-link fixture is detectable for the `MUT-LINK-NO-SELF` hard check.

### Implementation for User Story 2

- [ ] T027 [US2] Extend `MutashabihatAssembler.cs` (T020) with the links part: for each source ayah resolve
  `SourceAyahId`; for each item resolve `matched_ayah_key` → `TargetAyahId`; carry `Score`, raw `Coverage`,
  `MatchedWordsCount`, and `MatchWords` (verbatim json) unchanged; **synthesize no reverse rows**. Append to
  `MutashabihatSourceData.Links`. Per `research.md` R8/R9.
- [ ] T028 [US2] Extend `EfBulkMutashabihatWriter.cs` (T022) to `COPY` `quran_similar_ayah_links` after
  groups/occurrences (all ayah FKs already exist in `quran_ayahs`).
- [ ] T029 [US2] Add the US2 hard checks to `MutashabihatSql.cs` (T023): `MUT-SIMILAR-SOURCE-COUNT` (1,162),
  `MUT-SIMILAR-LINK-COUNT` (3,552), `MUT-LINK-NO-SELF`, `MUT-SCORE-RANGE` (50–100), `MUT-WORD-RANGE-SHAPE`
  (match_words), and extend `MUT-VERSEKEY-FORMAT` + `MUT-AYAH-RESOLVE` to cover both link ends. Per
  `contracts/validation-report.schema.md`.

**Checkpoint**: Both datasets load correctly; US1 + US2 tests pass. **This is the data core of the MVP.**

---

## Phase 5: User Story 3 — Safe, repeatable import that never harms source or existing data (Priority: P2)

**Goal**: Make the import atomic and safe: one transaction, validate-before-commit, rollback on any hard
failure, refuse-unless-empty + `--force`, source-unchanged re-verify, proof that `quran_ayahs` /
`quran_words` / the source files are untouched — and expose the runnable `import-mutashabihat` CLI verb.

**Independent Test**: A passing run commits all 3 tables; an injected hard violation rolls back (all 3
tables empty/unchanged) + non-zero exit; a re-run without `--force` refuses and writes nothing; `--force`
replaces and yields identical stored data/counts; after any run `quran_ayahs` and the source files are
byte-identical.

### Tests for User Story 3 (write FIRST)

- [ ] T030 [P] [US3] Create `MutashabihatRefusalForceTests.cs` — assert: second run without `--force`
  refuses and writes nothing/no report; missing/empty `quran_ayahs` refuses cleanly with no report;
  `--force` truncates/rebuilds only the 3 mutashabihat tables; forced rerun on unchanged source is
  idempotent (compare stable ordered snapshots/hashes, **counts-only is not enough**); **both
  `quran_ayahs` and `quran_words` are unchanged** — snapshot each table's row count and content (stable
  ordered snapshot/hash) before and after every run and assert they are byte/row identical (proving the
  import reads `quran_ayahs` read-only and never touches `quran_words` at all); source files' size/sha256
  unchanged after the run.
- [ ] T031 [P] [US3] Create `MutashabihatValidationFailureTests.cs` — assert: (a) inject a hard violation
  (e.g. an unresolved `ayah_id` or a self-link) into an empty target → rollback leaves all 3 tables empty,
  `verdict = "fail"`, non-zero exit, failure report written; (b) successful import, snapshot all tables,
  then `--force` with an injected hard failure after the build starts → previous contents unchanged,
  non-zero exit, failure report written; (c) over populated tables, `--force` with an injected
  source/manifest failure → early refusal leaves snapshots unchanged, non-zero exit, **no** report.

### Implementation for User Story 3

- [ ] T032 [US3] Complete the transaction/gate in `EfBulkMutashabihatWriter.ImportAsync` (T022/T028): wrap
  truncate-if-force (`TRUNCATE quran_mutashabihat_groups, quran_mutashabihat_occurrences, quran_similar_ayah_links RESTART IDENTITY CASCADE`)
  + all COPYs + all hard-check queries (T023/T029) + the injected `sourceUnchangedCheck` in **ONE**
  transaction; **commit only if every hard check passes, else roll back**. `Persisted = true` iff committed.
  `quran_ayahs` / `quran_words` are never in the write set. Mirror `EfBulkMorphologyWriter.cs` +
  `MorphologyValidationRunner.cs`. Research R12.
- [ ] T033 [US3] Wire the remaining gate checks: `MUT-SOURCE-UNCHANGED` (re-verify source sha256 after
  assembly, before commit, via `IMutashabihatImportSource.SourceUnchangedAsync` injected by the handler),
  and ensure the assembly-time `MUT-MANIFEST-SET`, `MUT-MANIFEST-CHECKSUM`, `MUT-JSON-SHAPE`,
  `MUT-RAW-OCCURRENCE-COUNT` are recorded in the result `Checks`. Add the manifest/foundation early
  refusals to `ImportMutashabihatHandler` (T024). Per `contracts/validation-report.schema.md`.
- [ ] T034 [US3] Register the new services in
  `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection.cs` — register **only** the
  concrete types that exist by this phase: `IMutashabihatImportSource`→`MutashabihatImportSource`,
  `IMutashabihatImportWriter`→`EfBulkMutashabihatWriter`. Do **not** register
  `IMutashabihatReportWriter` here — its concrete `MarkdownJsonMutashabihatReportWriter` does not exist
  yet (it is created and registered in T038/T039). Keep the build green.
- [ ] T035 [US3] Add the `import-mutashabihat` verb to `Backend/tools/QuranDashboard.DataImporter/Program.cs`
  — extend the `verb switch` with `"import-mutashabihat" => await RunImportMutashabihatAsync(verbArgs)`, and
  add `RunImportMutashabihatAsync` + `ResolveDefaultMutashabihatSourcePath` mirroring
  `RunImportMorphologyAsync` / `ResolveDefaultMorphologySourcePath`: parse `[--source <path>]`
  (default `App/resources/import-sources/mutashabihat/`), `[--report-out <path>]`
  (default `resources/report/mutashabihat/`), `[--force]`; reject unknown args with usage; print
  `groups=…, occurrences=…, links=…, sources=…` + report path on success. Behavior per
  `contracts/cli-verb.md`. Update `PrintUsage` with the new verb line.

**Checkpoint**: The import is atomic, gated, reversible, and runnable as a CLI verb; US1–US3 tests pass.
**Foundational + US1 + US2 + US3 = the deployable MVP: a validated, safe mutashabihat load.**

---

## Phase 6: User Story 4 — Every run is validated and produces a trustworthy report (Priority: P2)

**Goal**: Emit a single Markdown + JSON report on every started build, listing the written counts, the raw
occurrence count, every hard-check result, every warning count, and every informational figure.

**Independent Test**: Run the import and open the report: it lists each hard check pass/fail, the exact
counts (814 / 3,557 / 3,552 / 1,162), the raw occurrence count (3,558), the warning counts (coverage>100=4,
duplicate-occurrence=1, source-key-absent=1, provenance/license unknown=2, stale-counter groups), and the
info figures (one-way links ≈1,120, cross-dataset overlap 792/813, surah coverage 109/114, 3,084 distinct
ayahs).

### Tests for User Story 4 (write FIRST)

- [ ] T036 [P] [US4] Create `MutashabihatReportShapeTests.cs` — assert the Markdown + JSON report contain:
  the four written counts + the raw occurrence count, per-check `id`/`severity`/`passed`, and the warning
  counts (coverage>100, duplicate-occurrence, source-key-absent, provenance/license-unknown, stale-counters).
- [ ] T037 [P] [US4] Create `MutashabihatWarningTests.cs` — assert each warning/info check is **recorded
  but never blocks**: a coverage-200 fixture commits with `MUT-COVERAGE-GT-100 = 1`; the duplicate-occurrence
  fixture commits with `MUT-DUPLICATE-OCCURRENCE = 1`; the source-key-absent fixture commits with
  `MUT-SOURCE-KEY-ABSENT = 1`; a stale-counter fixture commits with recomputed values and the diff reported.

### Implementation for User Story 4

- [ ] T038 [US4] Create
  `Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/Mutashabihat/MarkdownJsonMutashabihatReportWriter.cs`
  implementing `IMutashabihatReportWriter` — write Markdown + JSON per
  `contracts/validation-report.schema.md` for every **started build** (pass or fail), default dir
  `resources/report/mutashabihat/`. Mirror `MarkdownJsonMorphologyReportWriter.cs`.
- [ ] T039 [US4] Register `IMutashabihatReportWriter`→`MarkdownJsonMutashabihatReportWriter` in
  `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection.cs` (now that the concrete type
  from T038 exists — this completes the registrations deferred from T034), then wire the report writer into
  `ImportMutashabihatHandler` (T024): call it after every started build (commit or rollback); early
  refusals (manifest mismatch, missing/empty `quran_ayahs`, non-empty targets without `--force`) print to
  console and write **no** report artifact.
- [ ] T040 [US4] Add the warning checks to `MutashabihatSql.cs` / assembler and the result `Checks`:
  `MUT-COVERAGE-GT-100` (4), `MUT-DUPLICATE-OCCURRENCE` (1), `MUT-SOURCE-KEY-ABSENT` (1),
  `MUT-STALE-SOURCE-COUNTERS` (count), `MUT-WORD-RANGE-UPPER-BOUND` (vs `quran_ayahs.words_count_real`),
  `MUT-PROVENANCE-LICENSE-UNKNOWN` (2 source files). All severity `warning` — recorded, never gate.
- [ ] T041 [US4] Add the informational checks: `MUT-ONEWAY-LINKS` (≈1,120), `MUT-CROSS-DATASET-OVERLAP`
  (≈792 ayahs / 813 pairs), `MUT-SURAH-COVERAGE` (109/114; 3,084 distinct ayahs), and the optional
  `MUT-PHRASE-VERSES-CONSISTENCY` (only if `phrase_verses.json` is passed for cross-check; never stored).
  All severity `info`.

**Checkpoint**: Every run emits a trustworthy report with all hard/warning/info results; US1–US4 tests pass.

---

## Phase 7: User Story 5 — Read-time queries enabled without extra stored tables (Priority: P3)

**Goal**: Prove the three tables answer "all groups of an ayah", "all occurrences of a group", and "similar
ayahs of an ayah (outgoing + incoming)" with **no** extra stored structures (no `phrase_verses` table, no
stored reverse links).

**Independent Test**: For a sample ayah, query occurrences by `ayah_id` → its groups; query occurrences by
`group_id` → that group's occurrences; query links by `source_ayah_id` → outgoing, by `target_ayah_id` →
incoming — all using only the three tables and their indexes.

### Tests for User Story 5 (write FIRST)

- [ ] T042 [P] [US5] Create `MutashabihatReadQueryTests.cs` — after a fixture import, assert: querying
  `quran_mutashabihat_occurrences` by `ayah_id` returns all of an ayah's groups (the answer
  `phrase_verses.json` would give) with **no** `phrase_verses` table present; querying by `group_id`
  returns the group's occurrences; querying `quran_similar_ayah_links` by `target_ayah_id` returns incoming
  links (undirected view) with **no** stored reverse rows.

### Implementation for User Story 5

- [ ] T043 [US5] Confirm the read indexes exist and are used: `occurrences(ayah_id)` (T006),
  `links(target_ayah_id)` (T007) — they are already in the configs/migration; this task verifies them with
  the T042 read-query tests and confirms the DB has **exactly three** mutashabihat tables (no
  `phrase_verses`, no reverse-link table). No new stored structures. Document the read recipes in
  `quickstart.md` §4 if any drift is found (otherwise leave as-is).

**Checkpoint**: All five stories are independently testable; the read recipes work off the three tables.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [ ] T044 Run the full import against the real staged source
  (`App/resources/import-sources/mutashabihat/`) via the CLI verb and confirm the report: verdict PASS,
  groups = 814, stored occurrences = 3,557 (raw 3,558), links = 3,552, sources = 1,162, distinct ayahs =
  3,084; warnings coverage>100=4, duplicate-occurrence=1, source-key-absent=1, provenance/license-unknown=2;
  all hard checks ✅. Save the report under `resources/report/mutashabihat/`.
- [ ] T045 Run `dotnet test Backend/tests/QuranDashboard.Tests` and confirm all Mutashabihat tests pass;
  run `dotnet build Backend` clean.
- [ ] T046 [P] Clean-code + test-guard self-check (per root `CLAUDE.md`): naming/functions/SOLID/DRY/KISS;
  if `EfBulkMutashabihatWriter` or `MutashabihatAssembler` approaches the service soft threshold (300/450
  lines), split by responsibility (e.g. a `MutashabihatBulkCopier` / `MutashabihatValidationRunner` as
  morphology did); confirm tests assert behavior on real infrastructure (Testcontainers) and use only
  source-safe fabricated tokens (no real verse passages).
- [ ] T047 [P] Update the long-form companion doc
  `docs/feature-006-quran-mutashabihat-foundation/feature-006-quran-mutashabihat-foundation-planning-report.md`
  only if implementation revealed a deviation from the plan (otherwise leave as-is).

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1)**: none — start immediately.
- **Foundational (Phase 2)**: depends on Setup. **BLOCKS all user stories.** Includes the single migration
  (T016) so integration tests can run.
- **US1 (Phase 3)**: depends on Foundational. The MVP core (groups + occurrences).
- **US2 (Phase 4)**: depends on Foundational; extends the US1 assembler/writer (links). US1+US2 = data core.
- **US3 (Phase 5)**: depends on US1+US2 (completes the transaction/gate, adds DI + CLI verb). US1+US2+US3 = MVP.
- **US4 (Phase 6)**: depends on US3 (adds the report + warning/info checks).
- **US5 (Phase 7)**: depends on US1+US2 (read-query verification; indexes already created).
- **Polish (Phase 8)**: after all desired stories.

### Critical ordering notes (for correctness)

- Entities (T002–T004) before EF configs (T005–T007) before DbSets (T008) before the migration (T016).
- COPY order is **groups → occurrences → links** (FK-safe; occurrences reference groups). All ayah FKs
  already exist in `quran_ayahs`.
- The occurrence **UNIQUE(group_id, ayah_id, word_from, word_to)** constraint is what collapses the 1
  duplicate occurrence (3,558 raw → 3,557 stored) — it must exist before the COPY.
- Tests in each story are written before that story's implementation and must FAIL first.
- Production DI is split to match when each concrete type exists: the source + writer are registered in US3
  (T034) and the CLI verb is wired in US3 (T035); the report-writer registration is deferred to US4 (T039,
  after T038 creates `MarkdownJsonMutashabihatReportWriter`). US1/US2 e2e tests run via the test service
  collection (T017).

### Parallel opportunities

- All Foundational `[P]` files in the same group (entities T002–T004; configs T005–T007; abstractions
  T009–T012; readers T013–T015) — different files, parallelizable.
- Within each story, the `[P]` test files can be written together.
- US4 and US5 can be worked in parallel once US3 is done (different files), but both touch
  `MutashabihatSql.cs` — coordinate edits there.

---

## Parallel Example: Foundational entities + configs

```bash
# Launch the three entity files together (all [P], different files):
Task: "Create MutashabihatGroup.cs"        # T002
Task: "Create MutashabihatOccurrence.cs"   # T003
Task: "Create SimilarAyahLink.cs"          # T004

# Then the three EF configs together:
Task: "Create MutashabihatGroupConfiguration.cs"       # T005
Task: "Create MutashabihatOccurrenceConfiguration.cs"  # T006
Task: "Create SimilarAyahLinkConfiguration.cs"         # T007
```

---

## Implementation Strategy

### MVP first (Foundational + US1 + US2 + US3)

1. Phase 1 Setup → Phase 2 Foundational (schema + migration + readers + abstractions + fixture).
2. Phase 3 US1 → correct groups + occurrences (recomputed counters, representative flag).
3. Phase 4 US2 → directed similar links (raw coverage, no reverse rows).
4. Phase 5 US3 → atomic, gated, reversible import + source-unchanged + the runnable CLI verb.
5. **STOP and VALIDATE**: a trustworthy mutashabihat load is a usable MVP.

### Incremental delivery

6. Phase 6 US4 → report + warning/info checks. Re-run; validate the report.
7. Phase 7 US5 → confirm the read recipes off the three tables (no extra structures).
8. Phase 8 → full-source run, tests, self-checks, doc.

---

## Notes

- `[P]` = different files, no incomplete dependency. `[USx]` maps a task to its story for traceability.
- This feature is one importer; the stories are facets sharing one transaction — each is independently
  **testable** against the produced DB. MVP = US1+US2+US3.
- Verify each story's tests fail before implementing it.
- Commit after each task or logical group (only when the user asks).
- Never modify `quran_ayahs`, `quran_words`, the Quran text, or the source files; keep all Quranic test
  data source-safe; store `coverage` raw and links directed (no reverse rows).
