# Implementation Plan — Quran Words Display Tables Foundation

**Status:** Planning only (no code, no spec, no tasks). Precursor to `/specify`.
**Workspace:** المنهج القرآني — Quran Dashboard FullStack.
**Builds on:** Feature 002 (`002-mushaf-words-foundation`), which imported the core
tables into PostgreSQL.
**Governance:** `Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE,API_GUIDELINES}.md`,
`CODING_PRINCIPLES.md`, `Backend/CLAUDE.md` (EF migration policy).

---

## 0. Grounding facts (verified from the current codebase)

These are read from source, not assumed:

- The `QuranWord` entity (`Backend/domain/QuranDashboard.Domain/Quran/Words/QuranWord.cs`)
  and table `quran_words` already carry every field the new tables need:
  `Id` (int), `Location` (`"s:a:w"`), `AyahId`, `SurahNumber`, `AyahNumber`,
  `WordNumber`, `PageNumber`, `LineNumber`, `LineWordOrder`, `QpcGlyph`,
  `TextUthmani`, `TextUthmaniSimple`, `TextImlaeiSimple`, `IsAyahMarker`.
- **`quran_words.id` is the global mushaf reading order `1..83,668`** across *all*
  occurrences, including the 6,236 ayah markers (data-model.md §5). It is therefore
  **not** the same as a contiguous `word_order_in_mushaf` over readable words only —
  that must be re-ranked after excluding markers (see §5).
- **`word_number` is the position within the ayah, with the marker as the last
  word** of each ayah (data-model.md §5). So for readable words, `word_number`
  already equals the in-ayah order `1..n`.
- A **partial index already exists** for the readable read path:
  `IX_quran_words_readable_surah_ayah_word ... WHERE is_ayah_marker = false`
  (`QuranWordConfiguration.cs:83`). The rebuild query benefits from it.
- `quran_ayahs` owns `verse_key` and `words_count_real`; `quran_surahs` owns the
  Arabic names. Join via `quran_words.ayah_id → quran_ayahs.id`.
- The backend already ships a **console host** `tools/QuranDashboard.DataImporter`
  driving an Application use case (`ImportQuranFoundationHandler`) with the pattern:
  *refuse-unless-empty + `--force` (atomic truncate-and-reload) → assemble →
  hard-gated validation → write inside one transaction → emit a Markdown+JSON
  report* (`Program.cs`, `ImportQuranFoundationHandler.cs`, `EfBulkQuranImportWriter.cs`).
  This feature reuses that pattern.

---

## 1. Feature scope and explicit out-of-scope

### In scope

Create **exactly four** precomputed, derived, read-only tables for displaying Quran
words, built **only** from the already-imported database tables, plus the tooling to
(re)build and validate them.

1. **Ordered words with tashkeel** — one row per readable word, display text
   `text_uthmani`.
2. **Ordered words without tashkeel** — one row per readable word, display/grouping
   text `text_uthmani_simple`.
3. **Unique words with tashkeel** — one row per distinct `text_uthmani`.
4. **Unique words without tashkeel** — one row per distinct `text_uthmani_simple`.

Plus:

- EF Core entities, configurations, `DbSet`s, and **one schema-only migration**.
- A **rebuild trigger** (recommendation in §6).
- A **hard-gated validation suite** + a traceable rebuild report.
- Tests (synthetic, source-safe fixtures + a full-data validation path).

### Explicit out of scope (do not introduce in this feature)

- API endpoints / controllers / `ApiResponse` contracts (read API is a later feature).
- Any frontend / UI.
- Search, normalized search text, `citext`, fuzzy/diacritic-insensitive matching.
- Runtime/on-request aggregation (the whole point is to avoid it — see §2).
- Morphology, corpus, roots, lemma, stem, POS, i3rab.
- Tafsir, translations, audio, mutashabihat.
- **New external source files** — nothing is read from disk; the source of truth is
  the existing DB.
- Changes to the five Feature-002 tables (read them, never mutate them).

---

## 2. Why fixed derived tables instead of runtime calculation

- **Cost is paid once.** The statistics (`occurrences_count`, `ayahs_count`,
  `surahs_count`) and the three order ranks require grouping/`DISTINCT`/window
  functions over 77,432 rows. Computing them per page view is wasteful and slow;
  computing them once at build time makes the eventual words page a trivial indexed
  read.
- **The inputs are immutable.** The Feature-002 tables are read-only reference data;
  the derived tables only change when the source is re-imported. There is no
  freshness problem — the data does not drift between rebuilds.
- **Deterministic, reviewable correctness.** A fixed table can be validated once
  against hard invariants (§7) and signed off, instead of trusting an ad-hoc query
  on every request.
- **Stable ordering for display.** `word_order_in_mushaf` / `_surah` / `_ayah` and
  the unique tables' "first occurrence" fields give a single, stable sort the UI can
  rely on without re-deriving ranks.
- **Separation of concerns.** Heavy aggregation belongs in a controlled, operator-run
  batch step (like the existing importer), not in a request path — consistent with
  the existing architecture.

The trade-off (storage + a rebuild step + denormalized counts repeated across
occurrence rows) is acceptable and intended for read-optimized reference data.

---

## 3. Proposed database table names

Recommended (keeps the existing `quran_words` family prefix, `snake_case`):

| # | Purpose | Table name |
|---|---|---|
| 1 | Ordered, with tashkeel | `quran_words_ordered_tashkeel` |
| 2 | Ordered, without tashkeel | `quran_words_ordered_simple` |
| 3 | Unique, with tashkeel | `quran_words_unique_tashkeel` |
| 4 | Unique, without tashkeel | `quran_words_unique_simple` |

Conventions: `_tashkeel` = display/group on `text_uthmani` (diacritized);
`_simple` = display/group on `text_uthmani_simple` (the no-tashkeel Uthmani-spelling
form). "Simple" here always means *that specific text form*, never "search".

> Alternative naming (open decision §11): `quran_display_words_*` or
> `quran_word_*_view`. Recommendation: use the table above; it is unambiguous and
> consistent with `quran_words`, `quran_ayahs`, etc.

EF entity placement (PascalCase, `BACKEND_STRUCTURE.md` feature-foldering):
`Domain/Quran/Words/Display/` →
`OrderedTashkeelWord`, `OrderedSimpleWord`, `UniqueTashkeelWord`, `UniqueSimpleWord`.

---

## 4. Proposed columns for each table

> Type notes: `smallint` where values ≤ 32,767; `int` for `word_order_in_mushaf`
> (max 77,432) and for `quran_word_id`/`first_quran_word_id` (FK to
> `quran_words.id`, max 83,668). `occurrences_count` uses `int` for headroom (the
> most frequent token is well under 32,767, but `int` avoids any sizing surprise).
> All Arabic text is `text` with default collation (no normalization, no `citext`).

### 4.1 `quran_words_ordered_tashkeel` — 77,432 rows

| Column | Type | Null | Notes |
|---|---|---|---|
| `word_order_in_mushaf` | `int` | NO | **PK**, contiguous `1..77,432` over readable words only |
| `quran_word_id` | `int` | NO | **FK** → `quran_words.id`, **UNIQUE** |
| `location` | `text` | NO | `"s:a:w"` from `quran_words.location` |
| `verse_key` | `text` | NO | from `quran_ayahs.verse_key` |
| `surah_number` | `smallint` | NO | |
| `ayah_number` | `smallint` | NO | |
| `page_number` | `smallint` | NO | |
| `line_number` | `smallint` | NO | |
| `word_order_in_ayah` | `smallint` | NO | order within ayah (`= word_number`, validated) |
| `word_order_in_surah` | `smallint` | NO | contiguous `1..n` within surah |
| `text_uthmani` | `text` | NO | **display** (with tashkeel) + grouping key |
| `text_uthmani_simple` | `text` | NO | optional paired form (no tashkeel) |
| `occurrences_count` | `int` | NO | occurrences of this `text_uthmani` group |
| `ayahs_count` | `smallint` | NO | distinct ayahs containing this group |
| `surahs_count` | `smallint` | NO | distinct surahs containing this group |

### 4.2 `quran_words_ordered_simple` — 77,432 rows

Identical shape; the **display/grouping key is `text_uthmani_simple`** and the
statistics are computed by grouping on `text_uthmani_simple`.

| Column | Type | Null | Notes |
|---|---|---|---|
| `word_order_in_mushaf` | `int` | NO | **PK**, `1..77,432` |
| `quran_word_id` | `int` | NO | **FK** → `quran_words.id`, **UNIQUE** |
| `location` | `text` | NO | |
| `verse_key` | `text` | NO | |
| `surah_number` | `smallint` | NO | |
| `ayah_number` | `smallint` | NO | |
| `page_number` | `smallint` | NO | |
| `line_number` | `smallint` | NO | |
| `word_order_in_ayah` | `smallint` | NO | |
| `word_order_in_surah` | `smallint` | NO | |
| `text_uthmani_simple` | `text` | NO | **display** (no tashkeel) + grouping key |
| `text_imlaei_simple` | `text` | NO | optional paired reference form (stored, **not** searched) |
| `occurrences_count` | `int` | NO | occurrences of this `text_uthmani_simple` group |
| `ayahs_count` | `smallint` | NO | distinct ayahs |
| `surahs_count` | `smallint` | NO | distinct surahs |

> Note: the ordering columns (`word_order_in_mushaf`, `_surah`, `_ayah`,
> `quran_word_id`, page/line, verse_key) are **identical** between the two ordered
> tables. Only the display text and the grouped statistics differ (different grouping
> key). This redundancy is intentional given the "four fixed tables" requirement; see
> the consolidation note in §11.

### 4.3 `quran_words_unique_tashkeel` — derive count from DB (~21,210 expected)

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, generated identity (surrogate) |
| `text_uthmani` | `text` | NO | **display**, **UNIQUE** |
| `occurrences_count` | `int` | NO | total occurrences of this word |
| `ayahs_count` | `smallint` | NO | distinct ayahs |
| `surahs_count` | `smallint` | NO | distinct surahs |
| `first_quran_word_id` | `int` | NO | **FK** → `quran_words.id`; earliest occurrence |
| `first_location` | `text` | NO | from the first occurrence |
| `first_surah_number` | `smallint` | NO | |
| `first_ayah_number` | `smallint` | NO | |
| `first_word_order_in_mushaf` | `int` | NO | **UNIQUE**; stable display sort key |
| `first_page_number` | `smallint` | NO | |
| `first_line_number` | `smallint` | NO | |

Stable display order: `ORDER BY first_word_order_in_mushaf`.

### 4.4 `quran_words_unique_simple` — derive count from DB (~14,783 expected)

Identical shape; display/grouping key is `text_uthmani_simple`.

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, identity |
| `text_uthmani_simple` | `text` | NO | **display**, **UNIQUE** |
| `text_imlaei_simple` | `text` | YES | optional reference, taken from first occurrence |
| `occurrences_count` | `int` | NO | |
| `ayahs_count` | `smallint` | NO | |
| `surahs_count` | `smallint` | NO | |
| `first_quran_word_id` | `int` | NO | **FK** → `quran_words.id` |
| `first_location` | `text` | NO | |
| `first_surah_number` | `smallint` | NO | |
| `first_ayah_number` | `smallint` | NO | |
| `first_word_order_in_mushaf` | `int` | NO | **UNIQUE** |
| `first_page_number` | `smallint` | NO | |
| `first_line_number` | `smallint` | NO | |

> The optional paired forms (`text_uthmani_simple` in 4.1, `text_imlaei_simple` in
> 4.2/4.4) are cheap and explicitly permitted ("may be stored as reference"). Keeping
> them is the recommendation; dropping them to keep scope minimal is a valid
> alternative (§11). They are stored only — **never** used for search in this feature.

---

## 5. How to compute each table from existing DB tables

All four are computed from a single **readable-words base** (markers excluded), so
the four tables are guaranteed mutually consistent. Computation is **set-based SQL in
the database** (`INSERT … SELECT` with window functions), not row-by-row in C# — the
data already lives in PostgreSQL and the aggregates are exactly what SQL does best.

**Base CTE (readable words, joined for verse_key):**

```text
readable =
  SELECT w.id, w.location, w.ayah_id, w.surah_number, w.ayah_number,
         w.word_number, w.page_number, w.line_number,
         w.text_uthmani, w.text_uthmani_simple, w.text_imlaei_simple,
         a.verse_key
  FROM quran_words w
  JOIN quran_ayahs a ON a.id = w.ayah_id
  WHERE w.is_ayah_marker = false        -- uses the existing partial index
```

**Ordering ranks (computed once on `readable`):**

- `word_order_in_mushaf = ROW_NUMBER() OVER (ORDER BY id)` → contiguous `1..77,432`
  (ordering readable words by the global mushaf id yields a gap-free rank).
- `word_order_in_surah = ROW_NUMBER() OVER (PARTITION BY surah_number ORDER BY id)`.
- `word_order_in_ayah = ROW_NUMBER() OVER (PARTITION BY ayah_id ORDER BY word_number)`
  — re-ranked for robustness, then **validated** to equal `word_number` (markers are
  always last, so they match; the validation guards against any future data quirk).

**Group statistics** (cannot use `COUNT(DISTINCT …)` as a window function in
PostgreSQL, so compute via a grouped CTE and join back):

```text
stats_tashkeel =
  SELECT text_uthmani,
         COUNT(*)                  AS occurrences_count,
         COUNT(DISTINCT ayah_id)   AS ayahs_count,
         COUNT(DISTINCT surah_number) AS surahs_count
  FROM readable GROUP BY text_uthmani
```

(and the analogous `stats_simple` grouped on `text_uthmani_simple`).

### 5.1 Ordered tables

`INSERT … SELECT` joining `readable` (with its ranks) to `stats_tashkeel` /
`stats_simple` on the relevant text column. Each occurrence row therefore carries the
group-level counts for its own display text.

### 5.2 Unique tables

Derive from the **same ranked `readable` base** (so `first_word_order_in_mushaf`
matches the ordered tables by construction). Per display-text group:

- counts = the `stats_*` aggregates above;
- first occurrence = the group row with `MIN(word_order_in_mushaf)` (equivalently
  `MIN(id)`); use `DISTINCT ON (text)` ordered by `word_order_in_mushaf`, or a join to
  the per-group `MIN`, to pull `first_quran_word_id`, `first_location`,
  `first_surah_number`, `first_ayah_number`, `first_page_number`, `first_line_number`,
  and the optional `text_imlaei_simple`.

**Grouping semantics:** group on the **exact stored string** (default collation, no
trimming/normalization), per the requirement to use `text_uthmani` /
`text_uthmani_simple` directly. The unique row counts are **derived from the DB** at
build time and compared to the prior-project expectations (~21,210 / ~14,783) as a
*soft* sanity check — **never hardcoded** unless confirmed equal against this DB
(§7, §11).

---

## 6. Rebuild strategy

**Recommendation: extend the existing `tools/QuranDashboard.DataImporter` console
host with a second verb — do not add a new project, do not put data in migrations.**

Rationale:

- The rebuild reads only the DB (no source files), so it does not need the
  source-file import pipeline — but it *does* need the exact same host, DI,
  Infrastructure wiring, transaction discipline, refuse-unless-empty/`--force`
  semantics, and report writer the importer already has. Reuse beats duplication.
- Adding an 8th project would repeat the Feature-002 "7th project" complexity
  justification for no benefit (the host already exists).
- Migration `HasData`/seed is explicitly rejected: migrations stay **schema-only**
  (`Backend/CLAUDE.md`), and 77k+ derived rows must never live in a migration.

**Proposed CLI shape** (verb-based; keeps the existing import behavior intact):

```text
QuranDashboard.DataImporter import-foundation --source <path> [--report-out <path>] [--force]
QuranDashboard.DataImporter rebuild-words [--report-out <path>] [--force]
```

`Program.cs` gains a small verb dispatcher; the existing argument parser becomes the
`import-foundation` branch. (Back-compat note in §11.)

**Use-case flow** (mirrors `ImportQuranFoundationHandler`):

1. Refuse if any of the four target tables is non-empty **unless** `--force`.
2. Open one transaction.
3. If `--force`: `TRUNCATE quran_words_ordered_tashkeel, quran_words_ordered_simple,
   quran_words_unique_tashkeel, quran_words_unique_simple RESTART IDENTITY;`
   (parent `quran_words` is **not** touched).
4. `INSERT … SELECT` the two ordered tables, then the two unique tables (§5).
5. Run the validation suite (§7) **inside** the transaction.
6. Commit only if all hard checks pass; otherwise roll back (write nothing).
7. Emit the Markdown+JSON rebuild report (totals, derived unique counts, check
   results), regardless of outcome.

**Layering** (`CLEAN_ARCHITECTURE.md`):

- **Domain** `Quran/Words/Display/`: the four entities (data only, no behavior beyond
  invariants if any).
- **Application.Abstractions** `Quran/Words/Display/`: `IDisplayWordsRebuilder`
  (executes the rebuild + returns row totals), and a report-writer abstraction (reuse
  or generalize the existing `IImportReportWriter` — §11).
- **Application** `Quran/Words/RebuildDisplayTables/`: `RebuildDisplayWordsCommand`,
  `RebuildDisplayWordsHandler`, `RebuildDisplayWordsResult`, and a
  `DisplayWordsValidator` (+ result type) under `…/Validation/`.
- **Infrastructure** `Persistence/Repositories/Quran/Words/Display/`:
  `SqlDisplayWordsRebuilder` (the raw `INSERT … SELECT` + truncate, transaction-owned)
  and the four `…/Configurations/Quran/Words/Display/*Configuration.cs`. Raw SQL is
  allowed here (Infrastructure is the only layer touching the DB).
- **Tools** `DataImporter`: the new verb wiring only (composition).

---

## 7. Validation strategy (hard-gated, all run before commit)

| # | Invariant | Check |
|---|---|---|
| V1 | Ordered tables row count | each ordered table has exactly **77,432** rows |
| V2 | No markers | `quran_word_id` set ∩ `is_ayah_marker = true` = ∅ (equivalently rows = readable-word count) |
| V3 | One row per readable word | ordered `quran_word_id` is bijective with readable words (count match **and** `COUNT(DISTINCT quran_word_id)` = 77,432) |
| V4 | `word_order_in_mushaf` contiguous | `MIN = 1`, `MAX = 77,432`, `COUNT(DISTINCT) = 77,432` |
| V5 | `word_order_in_surah` contiguous per surah | per surah: `MIN = 1`, `MAX = COUNT(*)`, no gaps |
| V6 | `word_order_in_ayah` correct per ayah | per ayah: `MIN = 1`, contiguous, `MAX = words_count_real`; and equals `word_number` ordering |
| V7 | Unique table row counts | rows = `COUNT(DISTINCT text)` over readable words (per form), derived live from DB |
| V8 | Counts match grouping | `Σ occurrences_count` over a unique table = 77,432; per-group counts match the ordered tables' grouped values |
| V9 | First-occurrence fields | each unique row's `first_word_order_in_mushaf` = `MIN(word_order_in_mushaf)` of its group; `first_quran_word_id` = the matching word; all `first_*` fields come from that one row |
| V10 | Cross-table consistency | the two ordered tables agree on all ordering columns for the same `quran_word_id`; unique `first_*` fields reference rows present in the ordered tables |
| V11 | Soft sanity (warning, not failure) | unique-tashkeel count ≈ 21,210 and unique-simple count ≈ 14,783 — **report the actual DB-derived numbers**; deviation is a warning to investigate, not a hard fail |

Any V1–V10 failure ⇒ roll back, persist nothing, write the report with the failure,
non-zero exit code (mirrors the importer's hard-gate behavior).

---

## 8. Migration strategy

- **EF Core Code-First, schema-only, one migration.** Add the four entities, four
  `IEntityTypeConfiguration<>` classes (`ToTable`, `snake_case` columns, PK, the
  UNIQUE/FK/index set from §4 and below), and four `DbSet`s on
  `QuranDashboardDbContext` (auto-discovered by the existing
  `ApplyConfigurationsFromAssembly`).
- **Generated by EF tooling only**, on explicit request, per `Backend/CLAUDE.md`: do
  not hand-write migration `.cs`/`.Designer.cs`/snapshot; do not run
  `dotnet ef database update` unless explicitly requested; after generating, report
  the migration name, files, build status, and whether the DB update was run.
- **No data in the migration** (no `HasData`) — data is populated by the rebuild verb
  (§6).
- **Indexes to include:**
  - Ordered tables: PK(`word_order_in_mushaf`); UNIQUE(`quran_word_id`);
    index(`surah_number`, `word_order_in_surah`); index(`ayah_id`-equivalent via
    `surah_number,ayah_number,word_order_in_ayah`); optional index on the display-text
    column for future exact lookups (kept minimal — no search).
  - Unique tables: PK(`id`); UNIQUE(display text); UNIQUE(`first_word_order_in_mushaf`).
- **FKs:** `quran_word_id` / `first_quran_word_id` → `quran_words.id`. The parent is
  stable and never truncated by this feature, so FKs are safe and add integrity. (An
  alternative — denormalized validated values with no FK, mirroring the
  `line_number` precedent — is noted in §11.)

---

## 9. Testing strategy

Follow `test-guard` + the existing `tests/QuranDashboard.Tests/Quran/Import` pattern
(xUnit + Testcontainers PostgreSQL). New folder:
`tests/QuranDashboard.Tests/Quran/WordsDisplay/`.

1. **Synthetic, source-safe logic tests (primary).** Seed a *small, fabricated*
   readable-words fixture using **placeholder tokens** (never real Quranic text —
   Quranic data safety). Construct rows that deterministically exercise:
   - re-ranking after markers (gaps in `id` collapse to contiguous `word_order_in_mushaf`);
   - per-surah and per-ayah contiguity;
   - grouping/`DISTINCT` for `occurrences/ayahs/surahs_count`, including a token that
     repeats across ayahs and across surahs;
   - tashkeel vs simple grouping divergence (two diacritized forms collapsing to one
     simple form) so unique-tashkeel count > unique-simple count;
   - first-occurrence selection (`MIN(word_order_in_mushaf)`).
   Assert the V1–V10 invariants on this fixture.
2. **End-to-end rebuild test.** Import the small fixture, run the rebuild use case,
   assert the four tables are populated, validated, committed atomically, and that a
   second non-`--force` run is refused while `--force` truncates-and-reloads
   idempotently (re-running yields identical rows).
3. **Validation-failure test.** Inject a fixture that violates an invariant (e.g. a
   forced gap) and assert the rebuild rolls back and reports failure.
4. **Full-data validation (opt-in / integration).** Against a DB that already has the
   real Feature-002 import, run the rebuild and assert V1–V6 and that the unique
   counts are recorded; treat the ~21,210 / ~14,783 figures as **reported**, not
   asserted-equal, unless confirmed (§11). Gate this so it skips when the full data is
   absent.

Test-code self-check: behavior not implementation; real entities/DTOs constructed
(not mocked); real PostgreSQL for persistence/query correctness; data-driven cases
for the count variants; no tests of EF/framework guarantees; all Quran test data
source-safe.

---

## 10. Suggested Spec Kit breakdown

### User stories

- **US1 (P1) — Precomputed display tables exist.** As a data engineer, I can build
  the four derived tables from the existing DB so the future words page reads
  precomputed rows instead of aggregating at runtime.
- **US2 (P1) — Trustworthy, hard-gated rebuild.** As a reviewer, the rebuild validates
  every structural invariant and writes nothing unless all hard checks pass, so the
  tables are never left partially or incorrectly populated.
- **US3 (P2) — Safe, repeatable, traceable rebuild.** As an operator, I can re-run the
  rebuild safely (refuse-unless-empty + `--force` atomic truncate-and-reload) and get
  a Markdown+JSON report with totals, derived unique counts, and check results.

### Acceptance criteria (samples)

- **US1:** the four tables exist with the §4 columns/keys; ordered tables have exactly
  77,432 rows each; no marker rows; unique tables have one row per distinct display
  text; `occurrences/ayahs/surahs_count` match grouping over readable words.
- **US2:** `word_order_in_mushaf` is `1..77,432` contiguous; `_surah`/`_ayah`
  contiguous per partition; unique `first_*` fields match the earliest occurrence; any
  invariant failure ⇒ rollback + failure report + non-zero exit.
- **US3:** a second run without `--force` is refused; `--force` reproduces identical
  rows; every run emits a report; the source tables are never modified.

### Phases / tasks outline

- **Phase 0 — Research/decisions:** lock table names, computation location
  (server-side SQL), rebuild verb, FK-vs-denormalized, optional paired columns,
  report-result reuse-vs-new; query the DB once to record the actual unique counts.
- **Phase 1 — Schema:** four Domain entities; four EF configurations; four `DbSet`s;
  generate the schema-only migration (on request); build.
- **Phase 2 — Rebuild use case:** abstraction `IDisplayWordsRebuilder`;
  `SqlDisplayWordsRebuilder` (`INSERT … SELECT` + truncate, transaction);
  `RebuildDisplayWords` command/handler/result; refuse-unless-empty + `--force`.
- **Phase 3 — Validation + report:** `DisplayWordsValidator` (V1–V11) + result type;
  report writer (reuse/generalize `IImportReportWriter`); hard-gate the commit.
- **Phase 4 — Console verb:** verb dispatch in `Program.cs`; wire `rebuild-words`.
- **Phase 5 — Tests:** synthetic logic tests, e2e rebuild test, failure test, opt-in
  full-data validation.
- **Phase 6 — Docs/polish:** quickstart (how to run the rebuild + verify), record the
  DB-derived unique counts, engineering-review + test-guard self-checks.

---

## 11. Risks and open decisions

1. **Unique counts must come from the DB, not the prior project.** The ~21,210 /
   ~14,783 figures are from a previous project; this DB's exact text encoding may
   differ. **Decision:** derive live (V7/V11), report the actual numbers, treat
   deviation as a warning. *Do not hardcode.*
2. **Grouping semantics / collation.** Grouping is exact-string on
   `text_uthmani` / `text_uthmani_simple` with default collation — no trimming or
   normalization (that would be "search", which is out of scope). **Risk:** if the
   simple text retains invisible marks or inconsistent spacing, "unique" counts could
   be higher than expected. **Decision:** group on the raw stored value; surface the
   derived count for review. Normalization stays out of scope.
3. **Redundancy between the two ordered tables.** Their ordering columns are
   identical; only display text + grouped stats differ. **Decision:** keep four tables
   per the explicit requirement. *Open:* a future consolidation (one ordered table
   carrying both text forms and both stat sets) is noted but **not** pursued here.
4. **Type sizing.** `word_order_in_mushaf` and the FK ids must be `int` (>32,767);
   `occurrences_count` uses `int` for headroom. **Decision:** confirm the max
   `occurrences_count` from the DB during Phase 0 (expected ≪ 32,767).
5. **FK vs denormalized values.** Recommend real FKs to `quran_words.id` (parent
   stable, integrity benefit). *Open:* mirror the `line_number` denormalization
   precedent (no FK) if independent truncate ordering is ever desired.
6. **Optional paired text columns.** Recommend keeping `text_uthmani_simple` in the
   tashkeel-ordered table and `text_imlaei_simple` in the simple tables (cheap,
   explicitly allowed, useful). *Open:* drop them to minimize scope.
7. **Rebuild trigger.** Recommend extending the `DataImporter` console with a
   `rebuild-words` verb. *Open:* a dedicated tool/project (rejected: duplication),
   migration seed (rejected: schema-only policy).
8. **CLI back-compat.** Introducing verbs changes the importer's current
   `--source`-first invocation. **Decision:** add `import-foundation` as the explicit
   verb and update quickstart/docs; confirm no automation depends on the old
   no-verb form.
9. **Report result type.** Reuse `IImportReportWriter` (typed to the foundation
   import result) vs introduce a feature-local validation/report result. *Open:*
   lean toward a small feature-local result + a generalized writer to avoid coupling
   this feature to the import schema.
10. **Computation location.** Server-side `INSERT … SELECT` (recommended) vs load 77k
    rows into C# and COPY back (rejected: wasteful for DB-to-DB). Confirm
    `INSERT … SELECT` runs comfortably within one transaction.

---

## 12. Recommended final scope for `/specify`

A single, tightly-scoped feature — suggested branch `003-quran-words-display-tables`:

> **Build four precomputed, read-only Quran word display tables**
> (`quran_words_ordered_tashkeel`, `quran_words_ordered_simple`,
> `quran_words_unique_tashkeel`, `quran_words_unique_simple`) **entirely from the
> existing imported database tables** (`quran_words` + `quran_ayahs` for `verse_key`),
> excluding ayah markers (`is_ayah_marker = false`). Deliver: the EF entities +
> configurations + one schema-only migration; a `rebuild-words` verb on the existing
> `DataImporter` console host that rebuilds via server-side `INSERT … SELECT` inside
> one transaction with refuse-unless-empty + `--force`; a hard-gated validation suite
> (77,432 ordered rows each, no markers, contiguous mushaf/surah/ayah ordering,
> grouped counts, first-occurrence consistency, DB-derived unique counts) that rolls
> back on any failure; a Markdown+JSON rebuild report; and source-safe tests.**

**Explicitly excluded from the spec:** API/endpoints, frontend/UI, search/normalized
text, runtime aggregation, morphology/corpus/roots/lemma/stem/POS/i3rab, tafsir,
translations, audio, mutashabihat, and any new external source files. No changes to
the Feature-002 tables beyond reading them.

Run `/speckit.specify` with the statement above; then `/speckit.plan` to formalize the
table DDL and the rebuild/validation flow; then `/speckit.tasks` for the Phase 0–6
breakdown in §10.
