# Tasks: Quran Word Morphology Foundation

**Input**: Design documents from `specs/004-word-morphology-foundation/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/` (all present)

**Tests**: INCLUDED. This is a data-integrity feature; the "trustworthy, hard-gated import" value (US2) is
only provable with tests, and `plan.md` enumerates the test classes. Integration tests use Testcontainers
PostgreSQL with **synthetic, source-safe single-word fixtures** (never real verse passages); the
transliteration map has pure unit tests.

**Organization**: Tasks are grouped by user story. This feature is **one importer pipeline** (`import-morphology`)
that builds six tables in one transaction, so the stories are *facets* of that pipeline. Each facet is
independently **testable** against the produced database, and the phases build the pipeline incrementally
in priority order.

## Conventions for the implementer (READ FIRST)

- **Repo root** = `/projects/Dashboard/App`. All paths below are repo-relative. The Backend solution root
  is `Backend/`.
- **DB columns** are `snake_case`; **C# entities/properties** are `PascalCase`. Types: `smallint` for
  values ≤ 32,767, else `int` (see `data-model.md` / research R13).
- **Authoritative sources** (do not invent values): column lists & keys → `data-model.md`; interface
  signatures & records → `contracts/morphology-abstractions.md`; verb behavior → `contracts/cli-verb.md`;
  check IDs & report shape → `contracts/validation-report.schema.md`; rationale → `research.md`.
- **Mirror existing code.** For every new file, copy the structure/style of the named **precedent file**
  from Feature 002 (`Quran/Import/…`) or Feature 003 (`Quran/Words/Display/…`).
- **Quranic data safety (non-negotiable):** never modify `quran_words`; never write the source files;
  `form_buckwalter` always retained; empty forms → `NULL`; `form_arabic_normalized` is never written from
  `qpc_glyph`/`text_uthmani` and never used as Mushaf text; tests use fabricated single-word tokens only.
- **Do not** create the EF migration or run `dotnet ef database update` until the explicitly-flagged task,
  and only on explicit request (`Backend/CLAUDE.md`). No `HasData`.
- **`[P]`** = may run in parallel (different files, no dependency on an incomplete task).

---

## Phase 1: Setup (orientation)

**Purpose**: Establish the baseline and the patterns to mirror.

- [x] T001 Verify the baseline builds and read the precedent files to mirror. Run `dotnet build Backend`
  to confirm a green baseline. Then open and skim these precedents (do not edit): entity
  `Backend/domain/QuranDashboard.Domain/Quran/Words/Display/OrderedTashkeelWord.cs`; EF config
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/QuranWordConfiguration.cs`;
  bulk COPY writer
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Import/EfBulkQuranImportWriter.cs`;
  transactional validate-then-commit
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Words/Display/SqlDisplayWordsRebuilder.cs`;
  manifest + JSON readers `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Import/{ManifestReader,JsonWordSourceReader,QuranImportSource}.cs`;
  report writer `Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/MarkdownJsonImportReportWriter.cs`;
  handler `Backend/application/QuranDashboard.Application/Quran/Words/RebuildDisplayWords/RebuildDisplayWordsHandler.cs`;
  verb dispatch `Backend/tools/QuranDashboard.DataImporter/Program.cs`;
  DI `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection.cs`;
  test fixture `Backend/tests/QuranDashboard.Tests/Quran/WordsDisplay/WordsDisplayTestFixture.cs`.

---

## Phase 2: Foundational (BLOCKING — must finish before US1–US4)

**Purpose**: Compile-safe schema/domain/configuration/source-reader/POS-seed scaffolding only. This phase
does **not** wire DI, expose the `import-morphology` CLI verb, or make the importer runnable end-to-end;
DI and CLI verb wiring happen later in T041-T042 after the concrete handler/source/writer/report-writer
types exist. **⚠️ No user-story work begins until this phase is done.**

### Domain — enums/value objects (`Backend/domain/QuranDashboard.Domain/Quran/Words/Morphology/`)

- [x] T002 [P] Create `SegmentKind.cs` enum — values `Prefix, Stem, Suffix` (maps to corpus `PREFIX/STEM/SUFFIX`). See `data-model.md` "Domain types".
- [x] T003 [P] Create `VerbTense.cs` enum — values `Past, Present, Imperative` (maps to `PERF/IMPF/IMPV`).
- [x] T004 [P] Create `VerbVoice.cs` enum — values `Active, Passive`.
- [x] T005 [P] Create `MorphologicalCase.cs` enum — values `Nominative, Accusative, Genitive` (maps to `NOM/ACC/GEN`).

### Domain — entities (same folder; plain data carriers, no behavior; mirror `OrderedTashkeelWord.cs`)

- [x] T006 [P] Create `PosTag.cs` — properties for `quran_pos_tags` columns: `Code` (string), `ArabicLabel`, `EnglishLabel`, `Category` (string), `SortOrder` (short), `Description` (string?). Columns/keys per `data-model.md` §6.
- [x] T007 [P] Create `QuranRoot.cs` — `Id` (int), `RootText`, `RootBuckwalter` (string?), `WordsCount` (int), `DistinctLemmasCount` (short), `FirstWordOrderInMushaf` (int). Per `data-model.md` §3.
- [x] T008 [P] Create `QuranLemma.cs` — `Id`, `LemmaText`, `LemmaBuckwalter` (string?), `RootId` (int?), `WordsCount`, `FirstWordOrderInMushaf`. Per `data-model.md` §4.
- [x] T009 [P] Create `QuranStem.cs` — `Id`, `StemText`, `WordsCount`, `FirstWordOrderInMushaf`. Per `data-model.md` §5.
- [x] T010 [P] Create `WordMorphologySegment.cs` — `Id` (int), `QuranWordId` (int), `SegmentLocation`, `SegmentNumber` (short), `Kind`, `Pos`, `FormBuckwalter`, `FormArabicNormalized` (string?), `ArabicRenderTier` (string?), `ArabicRenderSource`, `RootBuckwalter` (string?), `LemmaBuckwalter` (string?), `FeaturesRaw`, `FeaturesJson` (string?). Per `data-model.md` §2.
- [x] T011 [P] Create `WordMorphology.cs` — `QuranWordId` (int, PK/FK), `Location`, `HeadPos`, `SegmentCount` (short), `RootId` (int?), `LemmaId` (int?), `StemId` (int?), `IsVerb` (bool), `VerbTense` (string?), `VerbVoice` (string?), `CaseFeature` (string?), `HeadFeaturesJson` (string?). Per `data-model.md` §1.

### Infrastructure — EF configurations (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Words/Morphology/`; mirror `QuranWordConfiguration.cs`)

- [x] T012 [P] Create `PosTagConfiguration.cs` — table `quran_pos_tags`; PK `code`; columns + indexes (`category`, `sort_order`) per `data-model.md` §6.
- [x] T013 [P] Create `QuranRootConfiguration.cs` — table `quran_roots`; identity PK `id`; `UNIQUE(root_text)`, `UNIQUE(first_word_order_in_mushaf)`, index `words_count`. Per `data-model.md` §3.
- [x] T014 [P] Create `QuranLemmaConfiguration.cs` — table `quran_lemmas`; identity PK; `UNIQUE(lemma_text)`, `UNIQUE(first_word_order_in_mushaf)`, FK `root_id`→`quran_roots.id` (nullable), index `root_id`. Per `data-model.md` §4.
- [x] T015 [P] Create `QuranStemConfiguration.cs` — table `quran_stems`; identity PK; `UNIQUE(stem_text)`, `UNIQUE(first_word_order_in_mushaf)`. Per `data-model.md` §5.
- [x] T016 [P] Create `WordMorphologySegmentConfiguration.cs` — table `quran_word_morphology_segments`; identity PK `id`; FK `quran_word_id`→`quran_words.id`; `UNIQUE(quran_word_id, segment_number)`; indexes `pos`, partial `(quran_word_id) WHERE kind='STEM'`, `arabic_render_tier`; `features_json`/(none) as `jsonb`. Per `data-model.md` §2.
- [x] T017 [P] Create `WordMorphologyConfiguration.cs` — table `quran_word_morphology`; PK/FK/UNIQUE `quran_word_id`→`quran_words.id`; FK `head_pos`→`quran_pos_tags.code`; FKs `root_id`/`lemma_id`/`stem_id` (nullable); `head_features_json` as `jsonb`; indexes `head_pos`, partial `(verb_tense)`/`(verb_voice) WHERE is_verb`, `case_feature`, `root_id`, `lemma_id`, `stem_id`. Per `data-model.md` §1.

### Infrastructure — DbContext

- [x] T018 Add six `DbSet<>`s (`WordMorphology`, `WordMorphologySegment`, `QuranRoot`, `QuranLemma`, `QuranStem`, `PosTag`) to `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs`. Configurations are auto-discovered (same as Feature 003) — do not register them manually unless the precedent does.

### Application.Abstractions (`Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Morphology/`; mirror `Quran/Words/Display/` records)

- [x] T019 [P] Create `MorphologyInvariants.cs` — constants `ExpectedReadableWords = 77_432`, `ExpectedEmptyForms = 208`, `RenderSource = "buckwalter-transliteration"`, `InformationalWholeWordAgreement = 0.7983`, and messages `TargetsNotEmpty`, `SourceMismatch`. Exact contents in `contracts/morphology-abstractions.md` → "MorphologyInvariants".
- [x] T020 [P] Create result records `MorphologyImportResult.cs`, `MorphologyImportTotals.cs`, `MorphologyCheckResult.cs` — exact shapes in `contracts/morphology-abstractions.md` → "Records".
- [x] T021 [P] Create source DTOs `MorphologySourceData.cs`, `AlignedWordDto.cs`, `AlignedSegmentDto.cs` — exact shapes in `contracts/morphology-abstractions.md` → "Source DTOs".
- [x] T022 [P] Create interfaces `IMorphologyImportSource.cs`, `IMorphologyImportWriter.cs`, `IMorphologyReportWriter.cs` — exact signatures in `contracts/morphology-abstractions.md`. These expose records/DTOs only, never EF entities.

### Infrastructure — source reading (`Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Morphology/`; mirror `Files/Quran/Import/`)

- [x] T023 [P] Create `MorphologyManifestReader.cs` — read `manifest.json`; verify the source folder contains **exactly** this required file set and no extras: `manifest.json`, `README.md`, `corpus/quranic-corpus-morphology-qpc-aligned.json`, `corpus/corpus-qpc-location-alignment-map.json`, `qul/word-root.json`, `qul/word-lemma.json`, `qul/word-stem-corrected-arabic.json`. Reject missing files, unexpected research-only artifacts/extra files (`.db`, raw `.txt`, samples, reports, derived dumps), wrong `expectedRecordCount`, wrong `fileSizeBytes`, or wrong `sha256`; expose a way to recompute size/sha256 for `MORPH-SOURCE-UNCHANGED`. Mirror `ManifestReader.cs`. Manifest fields per `quickstart.md` §1 / planning report §5.1.
- [x] T024 [P] Create `JsonAlignedCorpusReader.cs` — parse `corpus/quranic-corpus-morphology-qpc-aligned.json` with `System.Text.Json`; yield per word: `qpcLocation`, `qpcUthmani`, and `segments[]` (`segmentNumber`, `kind`, `pos`, `form`, `features`, `root`, `lemma`). Mirror `JsonWordSourceReader.cs`. Do NOT join `quran_words` here (the assembler does that).
- [x] T025 [P] Create `JsonQulRootReader.cs`, `JsonQulLemmaReader.cs`, `JsonQulStemReader.cs` — each parses its QUL file into a `location → Arabic string` map. Files: `qul/word-root.json` (50,298), `qul/word-lemma.json` (72,507), `qul/word-stem-corrected-arabic.json` (77,432).

### Application — POS controlled vocabulary (curated dictionary; blocking because `head_pos` FK references it)

- [x] T026 Create the curated POS dictionary `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Morphology/PosTagSeed.cs` — a static, immutable list of ≈30 `PosTag` rows: `code`, `arabic_label`, `english_label`, `category` ∈ {`noun`,`verb`,`particle`,`other`}, `sort_order`, optional `description`. Cover every POS code the corpus emits (`N, V, PN, ADJ, PRON, P, CONJ, NEG, REL, DEM, VOC, INL, …`); categories per planning report §3.7. This is curated reference data, NOT a migration `HasData` seed (research R6). Do not invent religious content; labels are grammatical terms.

### Infrastructure — DI + verb wiring (deferred until concrete types exist)

Do **not** wire `DependencyInjection.cs` or `Program.cs` in Phase 2. The concrete source, writer, report
writer, and handler types do not exist yet, so wiring here would break the compile-safe phase checkpoint.
DI and CLI wiring are intentionally moved to US2 tasks T041–T042 after those types have been created.

### Tests — shared fixture

- [x] T027 [P] Create `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyImportTestFixture.cs` — Testcontainers `postgres:16-alpine`; helpers to seed a small set of **synthetic, source-safe** `quran_words` (readable + a few markers) and to write a temporary local source folder (manifest + tiny aligned corpus JSON + alignment map + tiny QUL files + README) using fabricated single-word tokens. Include helpers for exact-source-file-set violations, missing/empty foundation data, forced reruns, injected validation/source failures, and stable ordered table snapshots/hashes. Mirror `WordsDisplayTestFixture.cs`. Reuse this fixture in all later test tasks.

**Checkpoint**: Solution compiles; schema entities/configs/DbSets exist; source readers + POS seed are in
place. DI and CLI verb wiring are intentionally deferred until T041–T042, after concrete implementation
types exist.

---

## Phase 3: User Story 1 — Per-occurrence morphology for every readable word (Priority: P1) 🎯 MVP

**Goal**: Produce one correct `quran_word_morphology` row per readable word plus its ordered segments
(head POS, segment kind/pos/form, verb tense/voice, case), with ayah markers excluded. (Dimension links
and Arabic rendering are added in US3; here `root_id`/`lemma_id`/`stem_id` and `form_arabic_normalized`
may be `NULL`.)

**Independent Test**: After import, every readable `quran_words` row has exactly one morphology row
(count = 77,432 in prod / fixture count in tests); zero rows map to a marker; each word has ≥1 segment and
at least one STEM whose first segment-number POS is the word's `head_pos`; additional STEMs are preserved
and reported; verb fields are consistent with the head STEM.

### Tests for User Story 1 (write FIRST; they must FAIL before T030–T034)

- [x] T028 [P] [US1] Create `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyImportTests.cs` — end-to-end: seed fixture → run import through the handler/writer path → assert morphology row count = readable count, segment rows present, `segment_count` matches, and `head_pos` = the STEM segment's POS. Uses the T027 fixture.
- [x] T029 [P] [US1] Create `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyVerbFeatureTests.cs` — assert: a verb has exactly one of past/present/imperative and a non-null voice; `passive` only when the fixture marks PASS, else `active` (no inferred flag — research R7/clarification Q2); non-verbs have null verb fields; case set only when NOM/ACC/GEN present.

### Implementation for User Story 1

- [x] T030 [US1] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Morphology/MorphologyAssembler.cs` (morphology+segments part): for each readable word (join aligned corpus `qpcLocation` to `quran_words.location`, `is_ayah_marker = false`), build an `AlignedWordDto` with its `AlignedSegmentDto[]` — set `kind`, `pos`, `form_buckwalter`, `features_raw` (verbatim) + `features_json` (parsed); pick the first `STEM` segment by `segment_number` as the operational head for `head_pos`; set `is_verb = (head_pos == "V")`; map word-level `verb_tense` (PERF/IMPF/IMPV→past/present/imperative) and `verb_voice` (PASS→passive else active for verbs; null for non-verbs) from the head STEM only; set `case_feature` (NOM/ACC/GEN→…, else null); set `head_features_json` from the head STEM. Leave `form_arabic_normalized`/`arabic_render_tier` and dimension ids unset for now (US3). Logic per `research.md` R7/R8 and `data-model.md` §1–§2.
- [x] T031 [US1] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Morphology/MorphologyImportSource.cs` implementing `IMorphologyImportSource.LoadAsync` — orchestrate T023–T025 readers + T030 assembler into a `MorphologySourceData` (dimension maps may be empty until US3). Read `quran_words.{id, location, is_ayah_marker}` from the DB context (read-only). If `quran_words` is missing or empty, surface a clean early refusal result for the handler; do not start a build report. Mirror `QuranImportSource.cs`.
- [x] T032 [US1] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Morphology/EfBulkMorphologyWriter.cs` (POS + morphology+segments COPY part): implement `AnyTargetTableHasDataAsync` and the first `ImportAsync` path. Inside the transaction, seed/COPY `quran_pos_tags` from `PosTagSeed` **before** any `quran_word_morphology` or `quran_word_morphology_segments` COPY because `head_pos` and segment `pos` have FKs to `quran_pos_tags.code`; then COPY morphology and segments via the Npgsql binary importer. Mirror `EfBulkQuranImportWriter.cs`. (Full validate-before-commit wiring is completed in US2; for now COPY inside a transaction and commit.)
- [x] T033 [US1] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Morphology/MorphologySql.cs` and add the US1 hard-check queries: `MORPH-READABLE-COMPLETE`, `MORPH-MARKERS-EXCLUDED`, `MORPH-LOCATION-MATCH`, `MORPH-SEGMENTS-PRESENT`, `MORPH-POS-PRESENT`, `MORPH-VERB-FEATURE-CONSISTENCY`. Exact assertions in `data-model.md` "Validation invariants" + `contracts/validation-report.schema.md`.
- [x] T034 [US1] Create the Application command/handler `Backend/application/QuranDashboard.Application/Quran/Words/ImportMorphology/{ImportMorphologyCommand,ImportMorphologyHandler,ImportMorphologyResult}.cs` — orchestrate early refusals first: load/verify source (source/manifest mismatch refuses), verify foundation data exists (`quran_words` present and non-empty; otherwise refuse), if `!force` and `AnyTargetTableHasDataAsync` then refuse, else `ImportAsync` → map verdict to `ExitCode`. Early refusals write no target data and no report artifact; started build attempts write reports after T037. Mirror `RebuildDisplayWordsHandler.cs`.

**Checkpoint**: Invoking the handler/writer path against a fixture produces a correct, marker-free,
one-row-per-readable-word morphology table with valid segments and verb features; US1 tests pass. The
external `import-morphology` CLI verb is wired later in T042 after the report writer exists.

**Phase 3 test-enabler note (T053 exception)**: `AddQuranWordMorphology` (`20260610155434`) was generated
during Phase 3 so Testcontainers integration tests can `MigrateAsync` the six morphology tables. This is
an explicit Phase 3 exception to T053 sequencing — schema-only, no `HasData`, no unrelated schema changes.
Do not run `database update` unless explicitly requested.

---

## Phase 4: User Story 2 — Trustworthy, hard-gated import that never touches source data (Priority: P1)

**Goal**: Make the import atomic and safe: one transaction, validate-before-commit, rollback on any hard
failure, refuse-unless-empty + `--force`, a Markdown+JSON report on every started build attempt, and proof that
`quran_words` and the source files are unchanged (`MORPH-SOURCE-UNCHANGED`).

**Independent Test**: A passing run commits all tables + writes a report; an injected invariant violation
rolls back (all six tables empty/unchanged) + writes a failure report + non-zero exit; a re-run without
`--force` refuses and writes nothing; after any run `quran_words` and the source files are byte-identical.

### Tests for User Story 2 (write FIRST)

- [x] T035 [P] [US2] Create `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyRefusalForceTests.cs` — assert: second run without `--force` refuses and writes nothing/no report artifact; missing or empty `quran_words` foundation refuses cleanly, writes nothing, and writes no report artifact; `--force` truncates/rebuilds only the six morphology tables; forced rerun idempotence compares stable ordered snapshots or table hashes before and after `--force` on unchanged source (**counts-only is not enough**); `quran_words` row count/content unchanged; the local source files' size/sha256 unchanged after the run.
- [x] T036 [P] [US2] Create `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyValidationFailureTests.cs` — assert failure cases: (a) inject a known validation violation into an empty target (e.g. a word missing its STEM, or a duplicated `quran_word_id`) → rollback leaves all six tables empty, `verdict = "fail"`, non-zero exit, and a failure report is written; (b) first run a successful import, capture stable ordered snapshots/hashes of all six morphology tables and non-morphology tables, then run `--force` with an injected validation failure after the build attempt starts → verify previous morphology contents remain unchanged, non-morphology tables remain unchanged, non-zero exit, and a failure report is written; (c) over already-populated tables, run `--force` with an injected source/manifest failure → early refusal leaves previous morphology and non-morphology snapshots unchanged, returns non-zero, and writes no report artifact.

### Implementation for User Story 2

- [x] T037 [US2] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/MarkdownJsonMorphologyReportWriter.cs` implementing `IMorphologyReportWriter` — write Markdown + JSON per `contracts/validation-report.schema.md` for every **started build attempt** (pass or fail), default dir `resources/report/words-morphology/`. The report must include per-table totals, tier distribution, empty-form rows/list/count, review/fragile tier list, multiword tier list, hard/warning checks, warnings, and outcome. Mirror `MarkdownJsonImportReportWriter.cs`. Wire it into the handler (T034) after this file exists; early refusals (source/manifest mismatch, missing/empty foundation data, non-empty targets without `--force`) report to console and write no report artifact.
- [x] T038 [US2] Complete the transaction/gate in `EfBulkMorphologyWriter.ImportAsync` (T032): wrap truncate-if-force (`TRUNCATE quran_word_morphology, quran_word_morphology_segments, quran_roots, quran_lemmas, quran_stems, quran_pos_tags RESTART IDENTITY CASCADE`) + POS seed + all COPYs + all validation queries in ONE transaction; **commit only if every hard check passes, else roll back**. On a failed forced run over populated tables, rollback must preserve the previous committed morphology contents. `Persisted = true` iff committed. `quran_words` is never in the write set (FR-034). Mirror `SqlDisplayWordsRebuilder.cs` transaction handling. Research R10.
- [x] T039 [US2] Add `MORPH-SOURCE-UNCHANGED` to the flow: capture source file size/sha256 before the run (via T023) and re-verify after (`IMorphologyImportSource.SourceUnchangedAsync`); record the check in the result. Add `MorphologyManifestReader`-backed early refusal (`SourceMismatch`) and missing/empty foundation-data refusal in the handler. These early refusals write no target data and no report artifact. Per `contracts/validation-report.schema.md`.
- [x] T040 [US2] Harden the FK-safe COPY order in `EfBulkMorphologyWriter` (T032/T038): POS rows from `PosTagSeed` (T026) are written before morphology/segments in US1, and when dimensions are added the order remains `quran_pos_tags` → dimensions → `quran_word_morphology` → segments (research R10). Add the `MORPH-DIM-COUNTS` warning (report actual dimension counts).
- [x] T041 [US2] Register the new services in `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection.cs` only after concrete types exist: `IMorphologyImportSource`→`MorphologyImportSource`, `IMorphologyImportWriter`→`EfBulkMorphologyWriter`, `IMorphologyReportWriter`→`MarkdownJsonMorphologyReportWriter`, plus `BuckwalterArabicMap` and `SegmentArabicRenderer` for DI-based rendering/map access. Keep the build green; do not register missing/stub-only types.
- [x] T042 [US2] Add the `import-morphology` verb to `Backend/tools/QuranDashboard.DataImporter/Program.cs` only after the concrete handler/reporting path exists: extend the `verb switch` with `"import-morphology" => await RunImportMorphologyAsync(verbArgs)`, and add `RunImportMorphologyAsync` mirroring `RunRebuildWordsAsync` — parse `[--source <path>]` (default `App/resources/import-sources/quran-morphology/`), `[--report-out <path>]` (default `resources/report/words-morphology/`), `[--force]`; reject unknown args with usage text. Behavior per `contracts/cli-verb.md`; early refusals print to console and write no report artifact.

**Checkpoint**: The import is atomic, gated, reversible, and reported; US1 + US2 tests pass. **This
(Foundational + US1 + US2) is the deployable MVP: a validated, safe morphology load.**

---

## Phase 5: User Story 3 — Arabic display values & normalized segment rendering (Priority: P2)

**Goal**: Fill the Arabic layer: root/lemma/stem dimensions (deduped on Arabic text, with `words_count`,
`distinct_lemmas_count`, `first_word_order_in_mushaf`), the per-word `root_id`/`lemma_id`/`stem_id` links
(NULL when QUL has no Arabic value, even if the corpus has a Buckwalter value — clarification Q1), and the
Option B segment rendering (`form_arabic_normalized` + `arabic_render_tier` + `arabic_render_source`).

**Independent Test**: Each non-empty segment form has a non-empty `form_arabic_normalized` with a valid
tier; the 208 empty forms render `NULL`; no rendering equals/derives from Uthmani/QPC text; dimensions are
deduped on Arabic text; a word with only a Buckwalter lemma (no QUL Arabic) has `lemma_id = NULL` with the
Buckwalter kept at segment level; no dangling dimension references.

### Tests for User Story 3 (write FIRST)

- [x] T043 [P] [US3] Create `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/BuckwalterArabicMapTests.cs` — pure unit (no DB): assert the map covers the full QAC character set with 0 unmapped, and is deterministic (same input → same Arabic). Use the inventory in `segment-arabic-rendering-capability-report.md` §11.
- [x] T044 [P] [US3] Create `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologySegmentRenderingTests.cs` — assert tiers assigned correctly (clean/quranic_marks/review/multiword), empty form → `NULL` render, raw `form_buckwalter` retained for rendered rows, and the render-provenance guard (render recomputes from `form_buckwalter` with `arabic_render_source = buckwalter-transliteration`; legitimate equality with `qpc_glyph`/`text_uthmani` is allowed when deterministic). Also assert the generated report payload includes tier distribution, empty-form rows/list/count, review/fragile tier list, and multiword tier list — not just summary counts.
- [x] T045 [P] [US3] Create `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyDimensionTests.cs` — assert: dimensions deduped on Arabic text; a Buckwalter-only word → null `root_id`/`lemma_id` with Buckwalter retained on the segment; `words_count`/`first_word_order_in_mushaf` correct; no dangling `root_id`/`lemma_id`/`stem_id`.

### Implementation for User Story 3

- [x] T046 [P] [US3] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Morphology/BuckwalterArabicMap.cs` — the full QAC extended Buckwalter→Unicode table (61 characters) from `segment-arabic-rendering-capability-report.md` §11, as the single source of truth. Expose a deterministic `TryMap(char) → string?` and a way to detect unmapped characters.
- [x] T047 [US3] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Morphology/SegmentArabicRenderer.cs` — transliterate a non-empty `form` to `form_arabic_normalized` using T046; classify the tier (`clean` = letters+harakat+wasla+dagger/maddah; `quranic_marks` = contains ṣila/iqlab/pausal marks; `review` = tatweel/kashida-hamza/leading combining; `multiword` = contains a space); set `arabic_render_source = MorphologyInvariants.RenderSource`; empty form → `(null, null)`. Collect any out-of-map characters for `MORPH-SEG-CHARSET`. Logic per `research.md` R3/R4 and the capability report §3/§9.
- [x] T048 [US3] Extend `MorphologyAssembler` (T030) to (a) call `SegmentArabicRenderer` for every segment, setting `form_arabic_normalized`/`arabic_render_tier`, and copy the corpus `root`/`lemma` Buckwalter onto the segment (`root_buckwalter`/`lemma_buckwalter`); (b) build the dimension maps from the QUL readers, dedup on Arabic text, and resolve per-word `root_id`/`lemma_id`/`stem_id` — **NULL when QUL has no Arabic value** (research R5/Q1); compute `words_count`, `distinct_lemmas_count`, and `first_word_order_in_mushaf` (stable order by the word's mushaf position). Populate `MorphologySourceData.CharsetWarnings`.
- [x] T049 [US3] Extend `EfBulkMorphologyWriter` to `COPY` `quran_roots`, `quran_lemmas`, `quran_stems` (before morphology, per FK-safe order in T040). Add the US3 hard checks to `MorphologySql` (T033): `MORPH-DIMENSION-RESOLVES`, `MORPH-SEG-CHARSET` (0 unmapped — refuse; spaces allowed only for `multiword` tier), `MORPH-SEG-RENDER-TOTAL` (non-empty→non-null; empty→null, expected 208), `MORPH-SEG-TIER-VALID`, `MORPH-SEG-RENDER-PROVENANCE`. Add the warnings/report payloads `MORPH-SEG-WORD-AGREEMENT` (≈79.83%), `MORPH-SEG-TIER-DIST`, `MORPH-SEG-REVIEW-LIST` (review/fragile tier rows), `MORPH-SEG-MULTIWORD-LIST`, `MORPH-SEG-EMPTY-LIST` (empty-form rows/list/count), and `MORPH-MULTI-STEM-LIST`. Assertions per `contracts/validation-report.schema.md` and FR-030/SC-009.

**Checkpoint**: Segments carry flagged Arabic renderings; dimensions are populated and linked (Arabic-only,
Buckwalter-as-cross-ref); US1–US3 tests pass.

---

## Phase 6: User Story 4 — POS controlled-vocabulary foundation for word-type filtering (Priority: P3)

**Goal**: Lock the POS vocabulary as a usable filtering foundation: every `head_pos`/segment `pos`
resolves to a known code (fail-closed), each code has Arabic+English labels + category + sort order, and
the stored fields support future filtering (category, tense, voice, case) — data only, no UI/API.

**Independent Test**: `quran_pos_tags` is populated (~30 rows, all fields set); 100% of `head_pos` and
segment `pos` resolve to a code; a direct query can group readable words by POS category, verb tense,
verb voice, and grammatical case.

### Tests for User Story 4 (write FIRST)

- [x] T050 [P] [US4] Create `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyPosResolutionTests.cs` — assert: `quran_pos_tags` rows exist with non-null `arabic_label`/`english_label`/`category` (∈ noun/verb/particle/other)/`sort_order`; every `head_pos` and every segment `pos` resolves to a code; and grouping queries by `category`, `verb_tense`, `verb_voice`, `case_feature` return rows (no `quran_verbs` table needed).

### Implementation for User Story 4

- [x] T051 [US4] Add the `MORPH-POS-RESOLVES` hard check to `MorphologySql` (T033) and the import flow — assert every `head_pos` and segment `pos` is present in `quran_pos_tags` (0 unknown codes; a new code refuses the import — research R6). Include it in the report checks. POS rows are already physically written before morphology/segment COPY by T032; this task is the explicit validation gate and report check.
- [x] T052 [US4] Review/complete `PosTagSeed` (T026) so it covers **every** POS code observed in the corpus (run the import on the full source once and confirm `MORPH-POS-RESOLVES` passes; if any code is missing, add its curated Arabic/English label + category + sort order). Confirm categories map correctly per planning report §3.7: `PN/ADJ`→noun-family; nominal function words `PRON/PRO/REL (اسم موصول)/DEM (اسم إشارة)/SUB (اسم مبهم)`→**`noun`** (classical-grammar الأسماء المبنية, matching their Arabic labels); `P/CONJ/NEG/VOC/INL/DET`→`particle`. (Decided in Phase 6: relative pronouns and demonstratives are nouns, not particles — see §3.7 `category` mapping note.)

**Checkpoint**: All four stories are independently testable; the full hard-gate (all `MORPH-*` checks)
passes on the real source.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T053 Generate the schema-only EF migration **on explicit request only** (`Backend/CLAUDE.md`):
  `dotnet ef migrations add AddQuranWordMorphology --project Backend/infrastructure/QuranDashboard.Infrastructure --startup-project Backend/api/QuranDashboard.Api`. Review the generated migration: it must create exactly the six tables, no `HasData`. Do NOT run `database update` unless explicitly requested. Report the migration name, generated files, and build status.
- [ ] T054 Run the full import against the real local source and confirm the report: verdict PASS, morphology = 77,432, segments ≈ 128,219, 208 null renders, tier distribution ≈ 94.2/5.4/0.4/1, empty-form rows/list/count present, review/fragile tier list present, multiword tier list present, whole-word agreement ≈ 79.83% (warning), all hard checks ✅. Treat elapsed-time/performance observations as advisory and non-blocking unless a separate explicit performance gate is added. Save the report under `resources/report/words-morphology/`.
- [ ] T055 Run `dotnet test Backend/tests/QuranDashboard.Tests` and confirm all WordsMorphology tests pass; run `dotnet build Backend` clean.
- [ ] T056 [P] Clean-code + test-guard self-check (per root `CLAUDE.md`): naming/functions/SOLID/DRY/KISS; split any file approaching the size threshold (assembler/writer/SQL); confirm tests assert behavior on real infrastructure and use only source-safe fabricated tokens.
- [ ] T057 [P] Update the long-form companion doc `docs/feature-004-word-morphology-foundation/feature-004-word-morphology-foundation-planning-report.md` only if implementation revealed a deviation from the plan (otherwise leave as-is).

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1)**: none — start immediately.
- **Foundational (Phase 2)**: depends on Setup. **BLOCKS all user stories.**
- **US1 (Phase 3)**: depends on Foundational. The MVP core.
- **US2 (Phase 4)**: depends on Foundational; builds on the US1 writer/handler (completes the transaction + report). US1+US2 = MVP.
- **US3 (Phase 5)**: depends on Foundational + US1 (extends the assembler/writer). FK-safe COPY order means dimensions COPY before morphology — wire it via T040/T049.
- **US4 (Phase 6)**: depends on Foundational (POS seed) + US1 (morphology rows to resolve). Mostly a validation+coverage facet.
- **Polish (Phase 7)**: after all desired stories.

### Critical ordering notes (for correctness)

- Entities (T006–T011) before EF configs (T012–T017) before DbSets (T018) before the migration (T053).
- POS seed (T026/T032/T040) and dimensions (T049) must be written **before** morphology/segments in the COPY
  order so FKs resolve (one transaction).
- Tests in each story are written before that story's implementation tasks and must FAIL first.

### Parallel opportunities

- All Foundational `[P]` files in the same group (enums T002–T005; entities T006–T011; configs T012–T017;
  abstractions T019–T022; readers T023–T025) — different files, parallelizable.
- Within each story, the `[P]` test files can be written together.
- US3 and US4 can be worked in parallel by different people once US1 is done (different files), but both
  modify the import flow/checks — coordinate `MorphologySql.cs` and `EfBulkMorphologyWriter.cs` edits.

---

## Parallel Example: Foundational entities

```bash
# Launch the six entity files together (all [P], different files):
Task: "Create PosTag.cs"            # T006
Task: "Create QuranRoot.cs"         # T007
Task: "Create QuranLemma.cs"        # T008
Task: "Create QuranStem.cs"         # T009
Task: "Create WordMorphologySegment.cs"  # T010
Task: "Create WordMorphology.cs"    # T011
```

---

## Implementation Strategy

### MVP first (Foundational + US1 + US2)

1. Phase 1 Setup → Phase 2 Foundational (compile-safe schema/domain/configuration scaffolding, source readers, and POS seed only; DI + CLI verb wiring happens later in T041-T042).
2. Phase 3 US1 → correct per-word morphology + segments (dims/rendering NULL for now).
3. Phase 4 US2 → atomic, gated, reported, reversible import + source-unchanged.
4. **STOP and VALIDATE**: a trustworthy morphology load (without the Arabic layer) is a usable MVP.

### Incremental delivery

5. Phase 5 US3 → add dimensions + Option B segment rendering. Re-run with `--force`; validate.
6. Phase 6 US4 → lock POS resolution + filtering foundation; validate.
7. Phase 7 → migration (on request), full-source run, tests, self-checks.

---

## Notes

- `[P]` = different files, no incomplete dependency. `[USx]` maps a task to its story for traceability.
- This feature is one importer; the stories are facets sharing one transaction — each is independently
  **testable** against the produced DB, and the MVP is US1+US2 together (both P1).
- Verify each story's tests fail before implementing it.
- Commit after each task or logical group (only when the user asks).
- Never modify `quran_words` or the source files; keep all Quranic test data source-safe.
