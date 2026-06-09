# Tasks: Quran Words Display Tables Foundation

**Input**: Design documents from `/specs/003-words-display-tables/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`
**Branch**: `003-words-display-tables`

**Tests**: INCLUDED — the spec lists tests in scope. Integration tests use the existing
xUnit + Testcontainers PostgreSQL setup (`postgres:16-alpine`).

**Organization**: Tasks are grouped by user story. US1 builds the data, US2 adds the
hard-gated validation, US3 verifies the safe-rebuild guarantees.

---

## How to read these tasks (read this first)

This feature is a **.NET 10 / EF Core 10** backend in `Backend/`. There is **no new
project** — you add files to existing projects and add one verb to an existing console
host. Follow these rules on every task:

1. **Match existing patterns.** The Feature 002 word import is your template. Open and
   imitate:
   - Entity style → `Backend/domain/QuranDashboard.Domain/Quran/Words/QuranWord.cs`
   - EF config style → `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/QuranWordConfiguration.cs`
   - Bulk DB writer + transaction style → `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Import/EfBulkQuranImportWriter.cs`
   - Handler/result/refusal style → `Backend/application/QuranDashboard.Application/Quran/Import/ImportQuranFoundation/`
   - Report writer style → `MarkdownJsonImportReportWriter` + `IImportReportWriter`
   - Console host style → `Backend/tools/QuranDashboard.DataImporter/Program.cs`
   - Test fixture style → `Backend/tests/QuranDashboard.Tests/Quran/Import/ImportTestFixture.cs`
2. **Column names are `snake_case`; C# properties are `PascalCase`.** The exact column
   list for every table is in `data-model.md` §1–4 — copy it precisely.
3. **Never mutate `quran_words` / `quran_ayahs` / `quran_surahs`.** They are read-only
   inputs.
4. **Test data must be source-safe**: invent placeholder Arabic-like tokens (e.g.
   `"كلمة-١"`), never real Quran text.
5. **After each phase, run** `dotnet build Backend` and (from US1 onward)
   `dotnet test Backend/tests/QuranDashboard.Tests` and confirm green before moving on.
6. **Design note used throughout** (important): the "77,432" expectation is **injectable**
   so small test fixtures don't trip the production gate. The rebuild takes an
   `expectedReadableWords` value that **defaults to 77,432** (used by the CLI) but is set
   to the fixture's row count in tests. See T021/T023/T025.

Paths below are relative to the repository root `/projects/Dashboard/App/`.

---

## Phase 1: Setup

**Purpose**: Confirm a clean baseline before adding code.

- [x] T001 Confirm the baseline builds: run `dotnet build Backend` from the repo root and confirm it succeeds with no errors. Confirm the connection-string key `ConnectionStrings:QuranDashboardDb` is how the DB is configured (see `ImportTestFixture.cs`). Do not change anything yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Create the four tables (schema), the typed contracts, and the test fixture.
After this phase the solution compiles and the migration applies, but no rebuild logic
exists yet.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Domain entities (data carriers, no behavior) — folder `Backend/domain/QuranDashboard.Domain/Quran/Words/Display/`

Each entity is a `public sealed class` in namespace `QuranDashboard.Domain.Quran.Words.Display`, with `{ get; set; }` properties. Use the exact property↔column mapping from `data-model.md`. Types: `int` for `WordOrderInMushaf`, `QuranWordId`, `FirstQuranWordId`, `FirstWordOrderInMushaf`, `OccurrencesCount`, and unique-table `Id`; `short` for all `*Number`, `*Order*InAyah`, `*OrderInSurah`, `AyahsCount`, `SurahsCount`; `string` (init `= string.Empty;`) for all text/`location`/`verse_key` fields.

- [x] T002 [P] Create `OrderedTashkeelWord.cs` in `Backend/domain/QuranDashboard.Domain/Quran/Words/Display/` with properties for every column in `data-model.md` §1 (WordOrderInMushaf, QuranWordId, Location, VerseKey, SurahNumber, AyahNumber, PageNumber, LineNumber, WordOrderInAyah, WordOrderInSurah, TextUthmani, TextUthmaniSimple, TextImlaeiSimple, OccurrencesCount, AyahsCount, SurahsCount).
- [x] T003 [P] Create `OrderedSimpleWord.cs` in the same folder with properties for every column in `data-model.md` §2 (same as §1 but text fields are TextUthmaniSimple + TextImlaeiSimple only; no TextUthmani).
- [x] T004 [P] Create `UniqueTashkeelWord.cs` in the same folder with properties for every column in `data-model.md` §3 (Id, TextUthmani, TextUthmaniSimple, TextImlaeiSimple, OccurrencesCount, AyahsCount, SurahsCount, FirstQuranWordId, FirstLocation, FirstSurahNumber, FirstAyahNumber, FirstWordOrderInMushaf, FirstPageNumber, FirstLineNumber).
- [x] T005 [P] Create `UniqueSimpleWord.cs` in the same folder with properties for every column in `data-model.md` §4 (Id, TextUthmaniSimple, TextImlaeiSimple, OccurrencesCount, AyahsCount, SurahsCount, FirstQuranWordId, FirstLocation, FirstSurahNumber, FirstAyahNumber, FirstWordOrderInMushaf, FirstPageNumber, FirstLineNumber).

### Abstractions (records + interfaces) — folder `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Display/`

Namespace `QuranDashboard.Application.Abstractions.Quran.Words.Display`. Use `contracts/rebuild-abstractions.md` verbatim for shapes.

- [x] T006 [P] Create `DisplayWordsTotals.cs` — `public sealed record DisplayWordsTotals(int OrderedTashkeelRows, int OrderedSimpleRows, int UniqueTashkeelRows, int UniqueSimpleRows, int ReadableWords);`
- [x] T007 [P] Create `DisplayWordsCheckResult.cs` — `public sealed record DisplayWordsCheckResult(string Id, string Severity, string Expected, string Observed, bool Passed);`
- [x] T008 [P] Create `DisplayWordsRebuildResult.cs` — the `public sealed record DisplayWordsRebuildResult(...)` exactly as in `contracts/rebuild-abstractions.md` (RunAtUtc, Verdict, Persisted, Forced, Totals, Checks, Warnings, Errors, InfoNotes). Use `IReadOnlyList<>` for the lists.
- [x] T009 [P] Create `DisplayWordsInvariants.cs` — `public static class DisplayWordsInvariants` with `public const int ExpectedReadableWords = 77_432;`, `public const int InformationalUniqueTashkeel = 21_210;`, `public const int InformationalUniqueSimple = 14_783;`, and `public const string TargetsNotEmpty = "Display word tables are not empty. Re-run with --force to truncate and rebuild them.";`
- [x] T010 [P] Create `IDisplayWordsRebuilder.cs` — interface with `Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);` and `Task<DisplayWordsRebuildResult> RebuildAsync(bool force, int expectedReadableWords, CancellationToken ct);` (NOTE: the `expectedReadableWords` parameter refines the contract for testability — see the design note at the top).
- [x] T011 [P] Create `IDisplayWordsReportWriter.cs` — interface with `Task WriteAsync(DisplayWordsRebuildResult result, string outputDir, CancellationToken ct);`

### EF Core configurations — folder `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Words/Display/`

Each is a `public sealed class XxxConfiguration : IEntityTypeConfiguration<Xxx>` in namespace `QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Words.Display`. Copy the structure of `QuranWordConfiguration.cs`: `builder.ToTable(...)`, `HasColumnName(snake_case)` for every property, `HasColumnType("smallint")` for all `short` properties, keys/unique/FK/indexes per `data-model.md`. For ordered configs set `builder.Property(x => x.WordOrderInMushaf).ValueGeneratedNever()` (it is the PK and is assigned by the rebuild, not generated). For unique configs the `Id` PK is identity (`ValueGeneratedOnAdd` / default).

- [x] T012 [P] Create `OrderedTashkeelWordConfiguration.cs`: `ToTable("quran_words_ordered_tashkeel")`; PK = `word_order_in_mushaf`; UNIQUE index on `quran_word_id`; FK `quran_word_id` → `quran_words(id)`; indexes `(surah_number, word_order_in_surah)` and `(surah_number, ayah_number, word_order_in_ayah)`; all columns per `data-model.md` §1.
- [x] T013 [P] Create `OrderedSimpleWordConfiguration.cs`: `ToTable("quran_words_ordered_simple")`; same keys/indexes/FK as T012; columns per `data-model.md` §2.
- [x] T014 [P] Create `UniqueTashkeelWordConfiguration.cs`: `ToTable("quran_words_unique_tashkeel")`; PK = `id` (identity); UNIQUE index on `text_uthmani`; UNIQUE index on `first_word_order_in_mushaf`; FK `first_quran_word_id` → `quran_words(id)`; columns per `data-model.md` §3.
- [x] T015 [P] Create `UniqueSimpleWordConfiguration.cs`: `ToTable("quran_words_unique_simple")`; PK = `id` (identity); UNIQUE index on `text_uthmani_simple`; UNIQUE index on `first_word_order_in_mushaf`; FK `first_quran_word_id` → `quran_words(id)`; columns per `data-model.md` §4.

### Wire schema into the model + migrate

- [x] T016 Edit `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs`: add four `DbSet` properties — `public DbSet<OrderedTashkeelWord> QuranWordsOrderedTashkeel => Set<OrderedTashkeelWord>();`, `QuranWordsOrderedSimple`, `QuranWordsUniqueTashkeel`, `QuranWordsUniqueSimple` — and add the matching `using QuranDashboard.Domain.Quran.Words.Display;`. The configs are auto-discovered by the existing `ApplyConfigurationsFromAssembly`; do not change `OnModelCreating`. (Depends on T002–T005, T012–T015.)
- [x] T017 Generate the schema-only EF migration (depends on T016). From repo root run: `dotnet ef migrations add WordsDisplayTables --project Backend/infrastructure/QuranDashboard.Infrastructure --startup-project Backend/api/QuranDashboard.Api`. Then open the generated migration and CONFIRM it only `CreateTable`s the four `quran_words_*` tables with the right columns/keys/indexes and contains **no** `InsertData`/`HasData`. Do NOT run `database update` here (tests apply migrations themselves). Report the migration file name and that no data is seeded.

### Test fixture

- [x] T018 Create `Backend/tests/QuranDashboard.Tests/Quran/WordsDisplay/WordsDisplayTestFixture.cs` modeled on `ImportTestFixture.cs`: a `public sealed class WordsDisplayTestFixture : IAsyncLifetime` that starts a `PostgreSqlContainer` (`postgres:16-alpine`), exposes `CreateServiceProvider()` (using `AddApplication().AddInfrastructure(configuration)` with the container connection string), runs `dbContext.Database.MigrateAsync()` in `InitializeAsync`, and provides a helper `Task SeedReadableWordsAsync(IEnumerable<QuranWord> words, IEnumerable<Ayah> ayahs)` that inserts synthetic source-safe `quran_ayahs` + `quran_words` rows directly via the `QuranDashboardDbContext`. Also add a `Task TruncateAllAsync()` helper that truncates the four derived tables AND the source `quran_words`/`quran_ayahs` between tests. (This only references the interfaces/entities created above, so it compiles now.)

**Checkpoint**: `dotnet build Backend` succeeds; the migration applies in a throwaway container. No rebuild behavior yet.

---

## Phase 3: User Story 1 — Precomputed tables exist and are correct (Priority: P1) 🎯 MVP

**Goal**: Running the rebuild on a populated source DB creates and fills the four tables
with correct ordering, statistics, and first-occurrence values.

**Independent Test**: Seed a small synthetic readable-word set, run the rebuild, and assert
the four tables are populated with contiguous orders, correct per-group counts, and
correct first-occurrence rows.

### Implementation for User Story 1

- [x] T019 [US1] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Words/Display/DisplayWordsSql.cs` — a `internal static class DisplayWordsSql` holding the SQL text as `const string` fields. Implement the readable-base + ranked + stats CTEs and the four `INSERT … SELECT` statements exactly per `data-model.md` "Derivation". Provide: `InsertOrderedTashkeel`, `InsertOrderedSimple`, `InsertUniqueTashkeel`, `InsertUniqueSimple`. Use `ROW_NUMBER() OVER (ORDER BY id)` for `word_order_in_mushaf`, `ROW_NUMBER() OVER (PARTITION BY surah_number ORDER BY id)` for `word_order_in_surah`, `ROW_NUMBER() OVER (PARTITION BY ayah_id ORDER BY word_number)` for `word_order_in_ayah`; group stats with `COUNT(*)`, `COUNT(DISTINCT ayah_id)`, `COUNT(DISTINCT surah_number)`; unique tables via `DISTINCT ON (<text>) … ORDER BY <text>, word_order_in_mushaf`. Source filter is always `WHERE is_ayah_marker = false`.
- [x] T020 [US1] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Words/Display/SqlDisplayWordsRebuilder.cs` — `public sealed class SqlDisplayWordsRebuilder : IDisplayWordsRebuilder` (namespace `QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Words.Display`). Take `QuranDashboardDbContext` via constructor (copy the connection/transaction approach from `EfBulkQuranImportWriter`). Implement:
  - `AnyTargetTableHasDataAsync` → `await dbContext.QuranWordsOrderedTashkeel.AnyAsync(ct) || … (the other 3)`.
  - `RebuildAsync(bool force, int expectedReadableWords, CancellationToken ct)`: open the Npgsql connection, `BeginTransactionAsync`; if `force` run `TRUNCATE quran_words_ordered_tashkeel, quran_words_ordered_simple, quran_words_unique_tashkeel, quran_words_unique_simple RESTART IDENTITY;` (only those four — never the source tables); execute the four `INSERT … SELECT` from `DisplayWordsSql` via `dbContext.Database.ExecuteSqlRawAsync(...)`; gather totals (row counts of the four tables + readable count `SELECT count(*) FROM quran_words WHERE is_ayah_marker = false`). For THIS task commit at the end and return a `DisplayWordsRebuildResult` with `Verdict = "pass"`, `Persisted = true`, `Forced = force`, the totals, and **empty** Checks/Warnings/Errors lists (validation is added in US2). Wrap in try/rollback-on-exception like `EfBulkQuranImportWriter`.
- [x] T021 [US1] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/Words/MarkdownJsonDisplayWordsReportWriter.cs` — `public sealed class MarkdownJsonDisplayWordsReportWriter : IDisplayWordsReportWriter` modeled on `MarkdownJsonImportReportWriter`. Write `words-display-report.md` and `words-display-report.json` into `outputDir` (create the dir if missing). Render the totals table and a checks table (per `contracts/validation-report.schema.md`); the checks table may be empty for now. Always include an info note that unique counts are derived (not the informational figures).
- [x] T022 [US1] Create `Backend/application/QuranDashboard.Application/Quran/Words/RebuildDisplayWords/RebuildDisplayWordsCommand.cs` — `public sealed record RebuildDisplayWordsCommand(bool Force, string? ReportOutDir = null, int ExpectedReadableWords = DisplayWordsInvariants.ExpectedReadableWords);` (namespace `QuranDashboard.Application.Quran.Words.RebuildDisplayWords`; add `using QuranDashboard.Application.Abstractions.Quran.Words.Display;`).
- [x] T023 [US1] Create `RebuildDisplayWordsResult.cs` in the same folder modeled on `ImportQuranFoundationResult`: a `public sealed record` with `bool Succeeded`, `string Message`, `int ExitCode`, optional `DisplayWordsTotals? Totals`, and static factory methods `Success(DisplayWordsTotals totals)`, `Refused(string message)`, `Failure(string message)`. Use exit code `0` for success and a non-zero constant (e.g. `FailureExitCode = 1`) otherwise (copy the constant style from `ImportQuranFoundationResult`).
- [x] T024 [US1] Create `RebuildDisplayWordsHandler.cs` in the same folder modeled on `ImportQuranFoundationHandler`: constructor injects `IDisplayWordsRebuilder` and `IDisplayWordsReportWriter`. `HandleAsync(RebuildDisplayWordsCommand command, CancellationToken ct)`: (1) if `!command.Force && await rebuilder.AnyTargetTableHasDataAsync(ct)` → return `RebuildDisplayWordsResult.Refused(DisplayWordsInvariants.TargetsNotEmpty)` (write no report); (2) else call `result = await rebuilder.RebuildAsync(command.Force, command.ExpectedReadableWords, ct)`; (3) resolve a report directory: use `command.ReportOutDir` if set, otherwise default to `resources/report/words-display/` (relative to the repository root — mirror `ImportQuranFoundationHandler.ResolveReportOutDir`); create the directory if missing; (4) `await reportWriter.WriteAsync(result, reportDir, ct)`; (5) map `result.Verdict == "pass"` → `Success(result.Totals)` else `Failure(...)`.
- [x] T025 [US1] Register services. Edit `Backend/application/QuranDashboard.Application/DependencyInjection.cs`: add `services.AddScoped<RebuildDisplayWordsHandler>();` (+ using). Edit `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection.cs`: add `services.AddScoped<IDisplayWordsRebuilder, SqlDisplayWordsRebuilder>();` and `services.AddSingleton<IDisplayWordsReportWriter, MarkdownJsonDisplayWordsReportWriter>();` (+ usings).
- [x] T026 [US1] Edit `Backend/tools/QuranDashboard.DataImporter/Program.cs` to add verb dispatch. The FIRST argument is the verb: `import-foundation` (existing behavior — move the current arg parsing + handler call into this branch, still requiring `--source`) or `rebuild-words` (new). For `rebuild-words`: parse optional `--report-out <path>` and `--force`; build the host (same `AddApplication`/`AddInfrastructure` wiring); resolve `RebuildDisplayWordsHandler`; call `HandleAsync(new RebuildDisplayWordsCommand(force, reportOutDir))`; print success/refusal/failure like the import path; return `result.ExitCode`. Unknown/missing verb → print usage for BOTH verbs and return non-zero. See `contracts/cli-verb.md`.

### Tests for User Story 1

> Use `WordsDisplayTestFixture`. Seed a SMALL synthetic readable-word set (e.g. 8–15 words
> across 2 surahs / a few ayahs, with at least one token repeated within an ayah, across
> ayahs, and across surahs, plus two tashkeel variants that share one simple form). Pass
> `ExpectedReadableWords = <number of readable words you seeded>` so the rebuild succeeds.
> Assert against the KNOWN values of your fixture — do NOT assert 77,432 here.

- [x] T027 [P] [US1] Create `Backend/tests/QuranDashboard.Tests/Quran/WordsDisplay/DisplayWordsRebuildTests.cs`: seed the fixture, run the rebuild (force=false on empty tables), assert all four tables are populated; ordered tables row count == number of readable words seeded; no ordered row maps to an `is_ayah_marker = true` source row.
- [x] T028 [P] [US1] Create `DisplayWordsOrderingTests.cs`: assert `word_order_in_mushaf` is contiguous `1..N` (MIN=1, MAX=N, COUNT(DISTINCT)=N where N = readable count); `word_order_in_surah` contiguous `1..` per surah; `word_order_in_ayah` equals the source `word_number` order within each ayah.
- [x] T029 [P] [US1] Create `DisplayWordsStatisticsTests.cs`: for a known repeated token assert `occurrences_count`, `ayahs_count`, `surahs_count` equal the hand-computed values from the fixture; assert the unique-tashkeel table has MORE rows than the unique-simple table for the two-variant token (grouping divergence).
- [x] T030 [P] [US1] Create `DisplayWordsFirstOccurrenceTests.cs`: assert each unique row's `first_word_order_in_mushaf` is the minimum `word_order_in_mushaf` of its display-text group in the ordered table, and that `first_quran_word_id`/`first_location`/`first_surah_number`/`first_ayah_number`/`first_page_number`/`first_line_number` come from that same row; assert `SUM(occurrences_count)` over a unique table == readable count.

**Checkpoint**: US1 is independently functional — the rebuild builds correct tables on a
seeded DB and the tests prove ordering/statistics/first-occurrence.

---

## Phase 4: User Story 2 — Hard-gated validation with a report (Priority: P1)

**Goal**: The rebuild validates every hard invariant before committing and writes nothing
on failure; every run produces a report.

**Independent Test**: Force a hard-check failure and assert the four tables stay empty
(rolled back) and a failure report is written; run valid data and assert the report records
`verdict = pass` with all hard checks passed and the actual derived unique counts.

### Implementation for User Story 2

- [x] T031 [US2] Add validation query SQL to `DisplayWordsSql.cs` (extend T019). Add `const string` queries that return the observed values for each hard check in `contracts/validation-report.schema.md`: `ORD-COUNT`, `ORD-READABLE`, `ORD-NO-MARKERS`, `ORD-BIJECTION`, `ORD-MUSHAF-CONTIG`, `ORD-SURAH-CONTIG`, `ORD-AYAH-CONTIG`, `UNQ-COUNT`, `STAT-MATCH`, `FIRST-OCC`, `SRC-UNTOUCHED`. Each query should return scalars/booleans the rebuilder can compare. (Tip: express each as a query that returns the observed number, and compute pass/fail in C#.)
- [x] T032 [US2] Edit `SqlDisplayWordsRebuilder.cs` (extend T020): add a private `Task<List<DisplayWordsCheckResult>> RunHardChecksAsync(NpgsqlConnection conn, NpgsqlTransaction tx, int expectedReadableWords, CancellationToken ct)` that runs the T031 queries WITHIN the open transaction and builds one `DisplayWordsCheckResult` per check (Severity `"hard"`). Add the two warning checks `UNQ-EXPECT-TASHKEEL`/`UNQ-EXPECT-SIMPLE` (Severity `"warning"`) comparing the derived unique counts to `DisplayWordsInvariants.Informational*` (never affecting the verdict). Then MODIFY `RebuildAsync`: after the four inserts and before committing, call `RunHardChecksAsync`; if every hard check `Passed` → `CommitAsync`, `Verdict = "pass"`, `Persisted = true`; otherwise `RollbackAsync`, `Verdict = "fail"`, `Persisted = false`, and add the failed checks' messages to `Errors`. Return the full `DisplayWordsRebuildResult` with `Checks`, `Warnings`, `InfoNotes` populated either way.
- [x] T033 [US2] Verify `MarkdownJsonDisplayWordsReportWriter` (T021) now renders the populated checks table, the warnings/errors/notes, and `verdict`/`persisted` correctly for both pass and fail results. Adjust rendering if needed so a reviewer can read the outcome without the DB.

### Tests for User Story 2

- [x] T034 [P] [US2] Create `Backend/tests/QuranDashboard.Tests/Quran/WordsDisplay/DisplayWordsValidationFailureTests.cs`: seed valid data but call the handler/rebuild with `ExpectedReadableWords` set to a WRONG number (so `ORD-READABLE` fails). Assert `result.Verdict == "fail"`, `Persisted == false`, the four derived tables are EMPTY (rollback), and a failure report file was written containing the failed check.
- [x] T035 [P] [US2] Create `DisplayWordsValidationSuccessReportTests.cs`: seed valid data, run with the correct `ExpectedReadableWords`, assert `Verdict == "pass"`, every hard check `Passed == true`, `Totals` records the actual derived unique counts, and both report files (`.md` + `.json`) exist with those totals.

**Checkpoint**: US2 is independently verifiable — bad data never persists; good data always
produces a complete report.

---

## Phase 5: User Story 3 — Safe, repeatable rebuild that never touches source data (Priority: P2)

**Goal**: Re-runs are safe: refuse-unless-empty, `--force` replaces only the four derived
tables, source tables are never modified, and forced re-runs are idempotent.

**Independent Test**: Run twice without `--force` (second refused, no change); run with
`--force` (replaces derived tables, identical contents); confirm source row counts unchanged.

### Implementation for User Story 3

- [x] T036 [US3] Review/confirm `SqlDisplayWordsRebuilder` (T020/T032) and `RebuildDisplayWordsHandler` (T024): the `TRUNCATE` statement lists EXACTLY the four `quran_words_*` derived tables with `RESTART IDENTITY` and nothing else; the handler's refuse path returns `Refused(DisplayWordsInvariants.TargetsNotEmpty)` with a non-zero exit and writes no report when `!force` and targets are non-empty. Fix if either is wrong. (No new files expected.)

### Tests for User Story 3

- [x] T037 [P] [US3] Create `Backend/tests/QuranDashboard.Tests/Quran/WordsDisplay/DisplayWordsRefusalForceTests.cs`: seed + rebuild once (success). Run again with `force=false` → assert `Refused` (non-zero exit), and the existing derived rows are unchanged. Run again with `force=true` → assert success and tables rebuilt.
- [x] T038 [P] [US3] Create `DisplayWordsSourceUntouchedTests.cs`: capture `quran_words`/`quran_ayahs`/`quran_surahs` row counts before; run a forced rebuild; assert those counts are identical afterward.
- [x] T039 [P] [US3] Create `DisplayWordsIdempotencyTests.cs`: run a forced rebuild twice on unchanged source data; assert the four derived tables have identical row counts and that a representative ordered row and a representative unique row are byte-identical across the two runs.

**Checkpoint**: All three stories independently functional and verified.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [x] T040 [P] Run the clean-code self-check (`.claude/skills/engineering-review/references/clean-code-guard/`) and the test-code self-check on all new files; in particular confirm `SqlDisplayWordsRebuilder.cs` stays cohesive — if it approaches the repository soft threshold (~400 lines), keep all SQL text in `DisplayWordsSql.cs` and keep checks in the private `RunHardChecksAsync` (do not split into new public types unnecessarily).
- [x] T041 [P] Confirm the implemented default report path matches the specs: the handler (T024) must write to `resources/report/words-display/` when `--report-out` is omitted, exactly as documented in `quickstart.md` and `contracts/cli-verb.md`. Fix the code or the docs if they diverge.
- [x] T042 Run the full gate from repo root: `dotnet build Backend` then `dotnet test Backend/tests/QuranDashboard.Tests`; confirm all green. Record the derived unique counts the tests/report produced.
- [ ] T043 [P] (Optional, only if you ran it on the real imported DB) Capture the production rebuild report (verdict PASS, ordered = 77,432 each, actual unique counts) and note the unique counts vs the informational ~21,210 / ~14,783 in the report's notes.

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (T001)**: no dependencies.
- **Foundational (T002–T018)**: depends on Setup. **Blocks all user stories.**
- **US1 (T019–T030)**: depends on Foundational.
- **US2 (T031–T035)**: depends on US1 (edits `DisplayWordsSql.cs` and `SqlDisplayWordsRebuilder.cs` created in US1).
- **US3 (T036–T039)**: depends on US1 (verifies the force/refuse built in US1). US3's `T036` touches `SqlDisplayWordsRebuilder.cs`, so run US3 **after** US2 to avoid editing that file concurrently.
- **Polish (T040–T043)**: depends on all desired stories.

### Within Foundational

- T002–T015 are all `[P]` (distinct new files).
- T016 depends on T002–T005 and T012–T015 (it uses the entity types).
- T017 depends on T016 (migration reflects the model). T018 depends on the schema existing (T016) but is a new file, so it can be written alongside T017.

### Within each user story

- US1: T019 → T020 (rebuilder uses the SQL) → T024 (handler uses the rebuilder); T021/T022/T023 are independent files (`[P]`); T025 after T020/T021/T024; T026 after T024/T025. Tests T027–T030 after T026 (all `[P]`, distinct files).
- US2: T031 → T032 → T033; tests T034/T035 after T033 (`[P]`).
- US3: T036 first; tests T037–T039 after T036 (`[P]`).

### Parallel opportunities

- All of T002–T015 in parallel.
- US1 tests T027–T030 in parallel; US2 tests T034–T035 in parallel; US3 tests T037–T039 in parallel.

---

## Parallel Example: Foundational entities + abstractions

```bash
# These are all new, independent files — safe to create together:
T002 OrderedTashkeelWord.cs   T003 OrderedSimpleWord.cs
T004 UniqueTashkeelWord.cs     T005 UniqueSimpleWord.cs
T006 DisplayWordsTotals.cs     T007 DisplayWordsCheckResult.cs
T008 DisplayWordsRebuildResult.cs   T009 DisplayWordsInvariants.cs
T010 IDisplayWordsRebuilder.cs T011 IDisplayWordsReportWriter.cs
T012–T015 the four EF configurations
```

---

## Implementation Strategy

### MVP (User Story 1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational (schema migrates, compiles) → 3. Phase 3 US1
   (rebuild builds correct tables) → **STOP and validate** with T027–T030.

### Incremental delivery

1. Foundational → schema ready.
2. US1 → correct tables + correctness tests (MVP).
3. US2 → hard-gated validation + report + tests.
4. US3 → safe-rebuild guarantees + tests.

### Notes

- `[P]` = different files, no incomplete dependency.
- Keep every change inside the feature folders above; do not modify Feature 002 tables or
  unrelated files.
- Tests use synthetic source-safe tokens and assert against the fixture's own known
  numbers; the absolute 77,432 / informational unique counts are checked by the rebuild
  against the live DB (via `ExpectedReadableWords`) and reported, not asserted in
  small-fixture tests.
- Commit after each phase (or each logical group) so progress is reviewable.
