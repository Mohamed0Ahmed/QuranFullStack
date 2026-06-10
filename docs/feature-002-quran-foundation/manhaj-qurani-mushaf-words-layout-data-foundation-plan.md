# Implementation Plan — Quran Mushaf Words & Layout Data Foundation

> **Type:** Planning only. No code, no migrations, no specs/tasks, no data import. This document is written to be converted into a Spec Kit `/specify` prompt (see §10).
>
> **Feature:** Quran Mushaf Words & Layout Data Foundation (the first real backend data feature).
>
> **Grounded in:**
> - Data report: `resources/report/quran-mushaf-words-data-foundation-report.md`
> - `Backend/.architecture/BACKEND_STRUCTURE.md` (file/folder placement, thresholds)
> - `Backend/.architecture/CLEAN_ARCHITECTURE.md` (layer responsibilities, dependency direction)
> - `Backend/.architecture/API_GUIDELINES.md` (API boundary, `ApiResponse`, localization)
> - Verified backend: `.NET 10`, EF Core + `Npgsql.EntityFrameworkCore.PostgreSQL`, projects `QuranDashboard.{Domain, Application.Abstractions, Application, Infrastructure, Api, Shared}` — **no Quran entities exist yet** (clean slate).
>
> **Locked decisions from interview:**
> 1. **Trigger:** a dedicated **console/CLI** project runs the import (no HTTP exposure).
> 2. **Source location:** importer reads a curated staging tree `resources/import-sources/quran-foundation/` guided by a `manifest.json`.
> 3. **Read endpoint:** **deferred** to a tiny follow-up feature (001b). This feature is import + validation only.
> 4. **Re-run safety:** importer **refuses to run if the target tables are non-empty**; an explicit `--force` flag performs an atomic truncate-and-reload.
> 5. **No `search_normalized_text`** column. The two no-tashkeel forms (`text_uthmani_simple`, `text_imlaei_simple`) are the searchable forms; search normalization is a later Search feature.

---

## 1. Objective

Build the backend **data foundation** for the Quran Dashboard: import and store the five immutable reference tables — **surahs (114), ayahs (6,236), mushaf pages (604), mushaf lines (9,046), and word occurrences (83,668)** — joining QPC v4 glyph codes and three Uthmani/Imlaei text forms by a stable key, with page/line/order resolved from the mushaf layout.

**Why this must come first** (before Mushaf Reader, Words Explorer, Search, Morphology, i3rab):

- Every later feature is a **read** over this exact data. The Mushaf Reader renders `quran_words` (glyph + page/line) per page; the Word Details panel reads a single `quran_words` row; the Words Explorer aggregates over them; Search indexes the text forms; Morphology/i3rab **attach to `quran_words.id` / `location`**. Without the canonical word rows and their stable keys, none of those can be built without re-deriving the same data ad hoc.
- The join correctness (glyph ⇄ text ⇄ ayah ⇄ surah ⇄ page ⇄ line) is **non-trivial and must be validated once, centrally**. Doing it here — with a validation report — means later features trust the data instead of re-checking it.
- It establishes the **import pipeline pattern** (manifest → read → validate → persist → report) that every future Quran data layer (morphology, tafsir, translations, audio) will reuse.

**Out of scope by design:** anything that is a *read view* or a *later data layer* (see §10 non-goals).

---

## 2. Inputs and Source Files

All inputs are verified in the data report. At import time they live under the curated staging tree (see §5.1), assembled from the existing `resources/{mushaf,words,metadata}` folders. Counts below are the verified expected values.

| Source | Staging path (under `quran-foundation/`) | Role in import | Join key | DB or asset |
|---|---|---|---|---|
| **QPC v4 glyph** | `mushaf/qpc-v4.json` (83,668) | Provides `qpc_glyph` per word occurrence | `location` (+ `id`) | → `quran_words.qpc_glyph` |
| **Uthmani (with tashkeel)** | `words/uthmani.json` (83,668) | Display text | `location` | → `quran_words.text_uthmani` |
| **Uthmani simple (no tashkeel)** | `words/uthmani-simple.json` (83,668) | Search/display form A | `location` | → `quran_words.text_uthmani_simple` |
| **Imlaei simple (no tashkeel)** | `words/imlaei-simple.json` (83,668) | Search/display form B | `location` | → `quran_words.text_imlaei_simple` |
| **Mushaf layout** | `mushaf/qpc-v4-pages-layout.json` (604 pages / 9,046 lines) | Pages, lines, and word→page/line/order via `firstWordId`/`lastWordId` ranges | word `id` | → `quran_mushaf_pages`, `quran_mushaf_lines`, and derived columns on `quran_words` |
| **Page fonts** | `fonts/p1.woff2 … p604.woff2` (604) | Render glyphs; **page-specific** | filename → page number | **Static asset** — DB stores only the file name; binaries never enter PostgreSQL |
| **Surah metadata** | `metadata/quran-metadata-surah-name.json` (114) | Surah rows | `id` (surah number) | → `quran_surahs` |
| **Ayah metadata** | `metadata/quran-metadata-ayah.json` (6,236) | Ayah rows + `words_count` (source) + ayah-level text | `verse_key` | → `quran_ayahs` |

**Notes carried from the report:**
- The four word-level files are **perfectly aligned** by `location` and `id` (0 mismatches across 83,668). `location = surah:ayah:word` is the canonical **text** join key; numeric `id` (1..83,668, contiguous) is the **layout** join key.
- The layout's ayah-line word-id ranges cover **1..83,668 contiguously** (no gaps/overlaps) — this is what lets the importer assign `page_number`/`line_number`/`line_word_order` to every word in one pass.
- `prior report bug:` `uthmani-simple.json` is **without** tashkeel (0 harakat verified); treat any "includes tashkeel: yes" label as wrong.
- The SQLite layout (`qpc-v4-tajweed-15-lines.db`) is the original; the **exported JSON is the import source** (faithful per the export report; `sqlite3` not required at import time).

---

## 3. Data Model Plan

Backend domain/data model at a practical level. All five tables are **immutable reference data** (read-only after import). snake_case in PostgreSQL; PascalCase entities in Domain.

### Value objects (Domain — used for logic, persisted as plain strings)
- **`VerseKey`** — `"surah:ayah"` (e.g. `2:25`). Validates format/range. Persisted as a `string` column (`verse_key`), **not** an EF owned type — keep mapping simple.
- **`WordLocation`** — `"surah:ayah:word"` (e.g. `2:25:3`). Validates format. Persisted as `string` (`location`).

> Rationale: value objects give us safe construction/validation in import and use-case code, but persisting them as strings avoids EF owned-type complexity on a 83,668-row table. Don't over-engineer.

### Enums (Domain — placed with the feature that owns them, per BACKEND_STRUCTURE)
- **`MushafLineType`** — `Ayah | SurahName | Basmallah`. Lives in `Domain/Quran/MushafPages/`.
- **`RevelationPlace`** — `Makkah | Madinah`. Lives in `Domain/Quran/Surahs/`.

### `quran_surahs` (Surah)
- **Purpose:** the 114 surahs (navigation + headers).
- **Key fields:** `surah_number` (PK, smallint, = source `id`), `name_arabic`, `name_simple` (ASCII translit), `name_transliteration` (= source `name`, with diacritics), `revelation_place` (`RevelationPlace`), `revelation_order` (smallint), `verses_count` (smallint), `bismillah_pre` (bool).
- **Relationships:** one-to-many → `quran_ayahs`.
- **Constraints:** PK `surah_number`; unique `name_arabic`.
- **Indexes:** PK suffices.
- **Not yet:** no English *meaning* translation (source has only transliteration) — a future localized field, not invented here.

### `quran_ayahs` (Ayah)
- **Purpose:** the 6,236 ayahs.
- **Key fields:** `id` (PK, int, = source id), `surah_number` (FK), `ayah_number` (smallint), `verse_key` (unique), `text_uthmani` (ayah-level convenience text), `words_count_source` (smallint, from metadata), `words_count_real` (smallint, computed = words in ayah minus its 1 marker), `page_from` / `page_to` (smallint, **computed from layout**).
- **Relationships:** many → `quran_surahs`; one-to-many → `quran_words`.
- **Constraints:** PK `id`; unique `verse_key`; FK `surah_number`.
- **Indexes:** `(surah_number, ayah_number)`.
- **Important:** store **both** word counts. They agree for 6,235 ayahs and differ only at **37:130** (source 4, real 3 — see §6/§9). `page_from`/`page_to` are derived, never trusted from outside.
- **Not yet:** no `juz`/`hizb`/`rub` columns (data exists but is a later navigation layer).

### `quran_mushaf_pages` (MushafPage)
- **Purpose:** the 604 pages.
- **Key fields:** `page_number` (PK, smallint), `first_surah_number`, `first_ayah_number`, `last_surah_number`, `last_ayah_number` (all derived from the page's first/last word), `font_file_name` (`p{n}.woff2`), `font_asset_path` (nullable logical path/URL template), `lines_count` (smallint; 15 except pages 1–2 = 8).
- **Relationships:** one-to-many → `quran_mushaf_lines`.
- **Constraints:** PK `page_number`; unique `font_file_name`.
- **Indexes:** PK suffices.
- **Not yet:** font binaries are **never** stored; only the reference (see §9).

### `quran_mushaf_lines` (MushafLine) — authoritative line structure
- **Purpose:** the 9,046 layout lines, including non-word lines.
- **Key fields:** `id` (PK, surrogate), `page_number` (FK), `line_number` (smallint), `line_type` (`MushafLineType`), `is_centered` (bool), `surah_number` (nullable; set on the 114 `SurahName` lines), `first_word_id` / `last_word_id` (nullable; set only on the 8,820 `Ayah` lines), `words_count` (smallint; `last-first+1` on ayah lines else 0).
- **Relationships:** many → `quran_mushaf_pages`; `first_word_id`/`last_word_id` → `quran_words(id)`.
- **Constraints:** PK `id`; unique `(page_number, line_number)`; FKs on word ids.
- **Indexes:** `(page_number, line_number)`.
- **Important:** `SurahName` (114) and `Basmallah` (112) lines carry **no words** — they are rendered as headers/basmala from the page font. Keep them; the reader needs them.

### `quran_words` (QuranWord) — one row per word occurrence
- **Purpose:** the 83,668 word occurrences = the heart of the foundation (markers included, flagged).
- **Key fields:**
  - `id` (PK, int, = source id 1..83,668, **`ValueGeneratedNever`**; also defines mushaf reading order so "next/prev word" is `id ± 1`).
  - `location` (unique), `surah_number`, `ayah_number`, `word_number`.
  - `page_number`, `line_number`, `line_word_order` — **denormalized from layout** (immutable data → no update-anomaly risk; makes page reads single-table).
  - `qpc_glyph`, `text_uthmani`, `text_uthmani_simple`, `text_imlaei_simple`.
  - `is_ayah_marker` (bool; `true` for the 6,236 markers).
- **Relationships:** many → `quran_surahs`, `quran_ayahs`; `(page_number, line_number)` → `quran_mushaf_lines`.
- **Constraints:** PK `id`; unique `location`; FKs `surah_number`, ayah ref.
- **Indexes:** unique `location`; `(surah_number, ayah_number, word_number)`; `(page_number, line_number, line_word_order)`; **partial index `WHERE is_ayah_marker = false`** for normal word/read queries.
- **Explicitly NOT in this schema:**
  - ❌ `search_normalized_text` (deferred to Search feature).
  - ❌ `unique_word_id` / unique-words table (deferred — derived aggregation; no source list exists).
  - ❌ any morphology/root/i3rab columns.

**Denormalization recommendation (confirmed):** keep `quran_mushaf_lines` as the authoritative line structure, **and** duplicate `page_number`/`line_number`/`line_word_order` onto `quran_words`. Validate the denormalized values against the lines during import (§6).

---

## 4. Clean Architecture Placement

Following `BACKEND_STRUCTURE.md` (organize by domain/feature; **no** `Enums/Models/DTOs/Helpers/Utils` dumping folders) and `CLEAN_ARCHITECTURE.md` (dependency direction). Only files needed by this feature — no broad empty scaffolding.

### `QuranDashboard.Domain`
```
Domain/Quran/
  Surahs/        Surah.cs, RevelationPlace.cs
  Ayahs/         Ayah.cs
  MushafPages/   MushafPage.cs, MushafLine.cs, MushafLineType.cs
  Words/         QuranWord.cs, WordLocation.cs, VerseKey.cs
```
Entities, value objects, enums, invariants only. **No** EF, file I/O, or HTTP.

### `QuranDashboard.Application.Abstractions`
```
Application.Abstractions/Quran/Import/
  IQuranImportSource.cs          // reads manifest + source files into import DTOs
  IQuranImportWriter.cs          // persists the assembled rows (bulk)
  IImportReportWriter.cs         // writes the validation report (md + json)
Application.Abstractions/Quran/MushafPages/
  IMushafPageReadRepository.cs   // declared now, IMPLEMENTED in 001b (endpoint)
```
Focused interfaces; **no EF types exposed**. Source-reader interfaces return plain import DTOs, not EF entities.

### `QuranDashboard.Application`
```
Application/Quran/Import/
  ImportQuranFoundation/
    ImportQuranFoundationCommand.cs     // input: source root path, force flag
    ImportQuranFoundationHandler.cs     // orchestration (see §5)
    ImportQuranFoundationResult.cs
  Validation/
    QuranImportValidator.cs             // runs the §6 checks
    QuranImportValidationResult.cs      // totals, mismatches, warnings, verdict
```
Use-case orchestration + application-level validation models. Depends on abstractions, **not** Infrastructure. Watch handler size (soft 250 / hard 350 lines) — split validation into `QuranImportValidator`.

### `QuranDashboard.Infrastructure`
```
Infrastructure/Files/Quran/Import/
  ManifestReader.cs
  JsonWordSourceReader.cs        // qpc-v4, uthmani, uthmani-simple, imlaei-simple
  JsonLayoutSourceReader.cs      // pages-layout.json
  JsonMetadataSourceReader.cs    // surah + ayah metadata
  QuranImportSource.cs           // IQuranImportSource impl (composes the readers)
Infrastructure/Reports/Quran/
  MarkdownJsonImportReportWriter.cs     // IImportReportWriter impl
Infrastructure/Persistence/
  QuranDashboardDbContext.cs
  Configurations/Quran/          // SurahConfiguration, AyahConfiguration,
                                 // MushafPageConfiguration, MushafLineConfiguration,
                                 // QuranWordConfiguration  (constraints + indexes)
  Repositories/Quran/Import/
    EfBulkQuranImportWriter.cs   // IQuranImportWriter impl (Npgsql COPY / bulk)
Infrastructure/DependencyInjection.cs
```
All EF, file parsing, bulk insert, and report writing live here, behind the abstractions.

### Console host (new project — the import trigger)
```
tools/QuranDashboard.DataImporter/
  Program.cs                 // parse args (--source, --force), build host, call use case, exit code
  GlobalUsings.cs
```
- This is a **driving adapter / host**, analogous to `Api` — not a new architectural layer. It references `Application`, `Application.Abstractions`, `Infrastructure` (for DI composition only), and `Shared`. It contains **no business logic** (same thinness rule as controllers): parse args → call `ImportQuranFoundationHandler` → map result to console output + exit code.
- Keeping the trigger out of `Api` means the heavy data op is never reachable over HTTP and there is **no public import endpoint** (a stated non-goal).

### `QuranDashboard.Api`
- **No changes in this feature.** (The page endpoint is feature 001b — see §7.)

### `QuranDashboard.Shared`
- Reuse existing `Result`/`Error` primitives if present; do **not** add feature-specific types here.

---

## 5. Import Pipeline Plan

A custom, repeatable importer. **Never** EF `HasData`. EF migrations create **schema only**; the console importer writes the data afterward.

### 5.1 Source staging tree + manifest (locked decision)
```
resources/import-sources/quran-foundation/
  mushaf/
    qpc-v4.json
    qpc-v4-pages-layout.json
  words/
    uthmani.json
    uthmani-simple.json
    imlaei-simple.json
  metadata/
    quran-metadata-surah-name.json
    quran-metadata-ayah.json
  fonts/
    p1.woff2 … p604.woff2
  manifest.json
  README.md
```
- This curated tree is **assembled from** the existing `resources/{mushaf,words,metadata}` folders (a planning note — **do not create it as part of this planning task**). It gives the importer one stable, self-describing root.
- **`manifest.json` contract** (the importer validates this before reading anything): `version`, `generatedAt`, and a `sources[]` array, each entry: `key` (e.g. `qpc-glyph`, `uthmani`, `layout`, `surah-meta`, `ayah-meta`, `fonts`), `relativePath`, `format` (`json` | `woff2-dir`), `expectedRecordCount` or `expectedFileCount`, optional `sha256`, `role`, `joinKey`. The importer fails fast on any missing file, count mismatch, or (if present) checksum mismatch — this is the **traceability** required by the Quranic-data-safety rule.
- The console takes `--source <path>` (defaults to the staging tree) and `--force`.

### 5.2 Safety / re-run behavior (locked decision)
- Default run **refuses** if any of the five target tables is non-empty (clear message, non-zero exit).
- `--force` performs an **atomic** truncate-of-all-five-then-reload inside a single transaction. No partial state is ever committed: validation must pass before commit; any hard failure → rollback.

### 5.3 Order (and why)
1. **Validate manifest + files** (counts/checksums) — fail fast before touching the DB.
2. **Pre-flight DB guard** — refuse if non-empty (unless `--force`); open transaction.
3. **Surahs** (114) — FK target for ayahs.
4. **Ayahs** (6,236) — FK target for words; needs surahs.
5. **Mushaf pages + lines** (604 / 9,046) — parse layout; structural containers must exist (and word-id ranges parsed) before words so page/line/order can be assigned in one pass.
6. **Word skeleton** — load `id`/`location`/`surah`/`ayah`/`word` (from any of the four aligned files) → 83,668 rows.
7. **Attach glyph + text forms** — join `qpc-v4`, `uthmani`, `uthmani-simple`, `imlaei-simple` by `location`.
8. **Assign `page_number`/`line_number`/`line_word_order`** — walk the contiguous layout ranges.
9. **Flag ayah markers** — `is_ayah_marker = true` for the last word of each ayah (cross-checked: digit-only imlaei text). Backfill `quran_ayahs.words_count_real`, `page_from/page_to`, and `quran_mushaf_pages` boundaries.
10. **Validate** the assembled set in memory (§6). On hard failure → rollback + report + non-zero exit.
11. **Persist** via Npgsql binary **`COPY`**/bulk insert (single transaction).
12. **Write validation report** (md + json) and commit.

> This order matches the data report. Bulk insert (`COPY`) is the *mechanism* used inside the pipeline, not a separate strategy.

### 5.4 Properties
- **Repeatable & deterministic:** same inputs → same rows (source `id` is the PK, assigned not generated).
- **Idempotent via `--force`:** truncate-reload yields an identical table state each run.
- **Validates before commit:** nothing persists unless the in-memory set passes §6.
- **Self-reporting:** every run emits a report with totals, mismatches, warnings, and a verdict.

---

## 6. Validation Plan

Concrete checks with expected values from the data report. ✅ = pre-verified in the report; the importer must re-assert all of them at run time. Any **hard** failure rolls back; documented exceptions are warnings.

| # | Check | Expected | Severity |
|---|---|---|---|
| 1 | Surahs count | **114** | hard |
| 2 | Ayahs count | **6,236** | hard |
| 3 | Pages count | **604** | hard |
| 4 | Lines count | **9,046** | hard |
| 5 | Lines per page | **15**, except pages **1 & 2 = 8** | hard |
| 6 | Total word rows (incl. markers) | **83,668** | hard |
| 7 | Ayah markers | **6,236** (one per ayah, always last word) | hard |
| 8 | Real words (markers excluded) | **77,432** | hard |
| 9 | Duplicate `location` | **0** | hard |
| 10 | Duplicate / non-contiguous `id` | **0** / contiguous `1..83,668` | hard |
| 11 | Word files align by `location` + `id` | **0** mismatches across all 4 | hard |
| 12 | Fonts present | **604** (`p1..p604`, 0 missing/dup) | hard |
| 13 | Layout word-id coverage | `1..83,668` contiguous, **0** gaps/overlaps | hard |
| 14 | Every word has `page_number` + `line_number` | yes | hard |
| 15 | Every word has surah/ayah ref | yes | hard |
| 16 | Every `Ayah` line has valid `first/last_word_id` | 8,820/8,820 | hard |
| 17 | `bismillah_pre=true` count == `Basmallah` line count | **112 == 112** | hard |
| 18 | Σ `verses_count` (surahs) | **6,236** | hard |
| 19 | Denormalized page/line on words agree with `quran_mushaf_lines` | exact | hard |
| 20 | Sample page reconstruction | pages **1, 2, 5, 604** rebuild from layout + words | hard |
| 21 | `37:130` word-count discrepancy | metadata 4 vs real 3 — **documented, NOT fatal** | warning |
| 22 | Ayah-level `text` vs word-level `uthmani` | different encoding — **do not** assert equality | info |

**Report output:** two files — `quran-foundation-import-report.md` (human) and `.json` (machine) — written to a configurable path (default: alongside the source root or `resources/report/`). Contents: per-check totals, any mismatches/duplicates/missing, the `37:130` note, and an overall verdict (`pass` / `pass-with-warnings` / `fail`).

---

## 7. API Plan

**Decision (locked): defer the endpoint.** This feature ships **no API**. Reason: the goal is import + validation correctness; the import is verified by the console run's validation report and by direct DB inspection, so an endpoint is not required to prove correctness. Adding it now widens the surface (controller, contract, `ApiResponse` mapping, localization, Swagger, tests) against the "keep it tight" goal.

**Follow-up feature 001b — "Mushaf Page Read Endpoint"** (separate `/specify`):
- `GET /api/mushaf/pages/{pageNumber}` (route per `API_GUIDELINES.md`).
- Purpose: return a page for the reader.
- Constraints: `1 ≤ pageNumber ≤ 604` → else `404`/`400` with localized message.
- Response: `ApiResponse<T>` envelope (`isSuccess`, Arabic `message`, `data`), `data` = page meta (`fontFileName`, `linesCount`) + ordered `lines[]`, each line carrying `lineType`, `isCentered` and its `words[]` (`location`, `qpcGlyph`, `isAyahMarker`, `lineWordOrder`); `SurahName`/`Basmallah` lines return with `words: []` and their type so the client renders headers/basmala from the page font.
- Errors: out-of-range page → `404`; unexpected → global handler (`500`).
- Pre-wired now: only the `IMushafPageReadRepository` **interface** is declared (Application.Abstractions); its implementation + controller land in 001b.

**Never in this or the immediate follow-up:** search/word-search APIs, unique-words APIs, morphology APIs, or any public import endpoint.

---

## 8. Migration and EF Plan

- **Schema only.** A single EF migration creates the five tables + constraints + indexes. **No Quran data** in migrations; **no `HasData`** for Quran data.
- **Do not hand-write migrations.** Generate via EF tooling, and **only when explicitly requested** (`Backend/CLAUDE.md`). Report the migration name, generated files, and build status after generating.
- **Entity configurations** (`Infrastructure/Persistence/Configurations/Quran/`) define keys, FKs, unique constraints, indexes, max lengths, and the partial index `WHERE is_ayah_marker = false`. `quran_words.id` and `quran_surahs.surah_number` use **`ValueGeneratedNever`** (assigned, not identity).
- **Order:** schema migration first (tables exist) → console importer writes data.
- **`dotnet ef database update` runs only when explicitly requested.** Never automatically.
- Confirm Postgres collation/`citext` choices for Arabic text columns during EF configuration (default `text`; no `citext` needed yet since search is deferred).

---

## 9. Risks / Decisions / Open Questions

1. **Font static-asset handling.** 604 `woff2` (~50 MB) stay as files (frontend/CDN); DB stores `font_file_name` (+ optional `font_asset_path`). *Minor open item:* whether to store a path/URL template in DB or let the frontend derive it from the page number — default: store the file name, leave `font_asset_path` nullable.
2. **Glyph ⇄ font coupling.** `qpc_glyph` only renders correctly with the **matching page font**; the same code differs per page. The reader (001b+) must load fonts per page. The glyph column is meaningless without its page font.
3. **Ayah-marker handling.** Markers are real rows (needed for faithful rendering) but excluded from word counts/search/explorer via `is_ayah_marker`. Detectable as last-word-of-ayah / digit-only text.
4. **`37:130` word-count discrepancy.** Metadata says 4, the word index says 3 (`ال ياسين` is one token, internal space, in all four word files). Store both counts; word index is canonical; **document, do not fail**.
5. **Two Uthmani encodings.** Ayah-level metadata `text` ≠ word-level `uthmani.json` byte-for-byte (different codepoints + NBSP). Do not cross-validate by equality; word-level forms are canonical for word display.
6. **Licensing / attribution.** QPC v4 (King Fahd Complex) glyphs/fonts and QUL/Tarteel data carry usage terms. Record source + license/attribution (the `manifest.json` is a good home) before any public exposure. Release-relevant, not an import blocker.
7. **Search normalization deferred** — intentionally no `search_normalized_text`; the two simple forms suffice until the Search feature defines normalization rules.
8. **Unique words deferred** — derived aggregation; no source list; later feature. No `unique_word_id` column now.
9. **Endpoint deferred** to 001b (decided).
10. **Re-run safety** — refuse-unless-empty + `--force` truncate-reload (decided). Truncate is destructive; the guard + transaction + report mitigate it.
11. **Manifest drift** — if the staging tree changes without updating `manifest.json`, the fail-fast checksum/count checks catch it. Keep manifest authoritative.
12. **New console project** adds a 7th project to the solution; justified as a host/driving-adapter (like `Api`), not a new layer — confirm this is acceptable in the solution layout when implementing.

---

## 10. Recommended Spec Scope (paste-ready for `/specify`)

**Feature title:** Quran Mushaf Words & Layout Data Foundation

**Summary:** Import the immutable Quran reference data (surahs, ayahs, mushaf pages, mushaf lines, and word occurrences with QPC glyphs + Uthmani/Imlaei text forms) into PostgreSQL via a custom, repeatable console importer that validates and reports, so later read features (Mushaf Reader, Word Details, Words Explorer, Search, Morphology/i3rab) build on trusted data.

**Included:**
- EF Code-First schema (schema-only migration) for `quran_surahs`, `quran_ayahs`, `quran_mushaf_pages`, `quran_mushaf_lines`, `quran_words`.
- Domain entities/value objects/enums under `Domain/Quran/...`.
- A console/CLI importer that: validates a `manifest.json`-described source tree, refuses to run on non-empty tables (unless `--force` → atomic truncate-reload), assigns page/line/order from layout, flags ayah markers, bulk-inserts via `COPY`, and writes a validation report (md + json).
- `quran_words` = one row per occurrence (83,668) incl. markers flagged `is_ayah_marker`, with fields: `id, location, surah_number, ayah_number, word_number, page_number, line_number, line_word_order, qpc_glyph, text_uthmani, text_uthmani_simple, text_imlaei_simple, is_ayah_marker`.
- Fonts referenced by file name only; binaries remain static assets.

**Excluded (non-goals):**
- `search_normalized_text`, full search, search ranking.
- Unique words, roots, morphology, i3rab, tafsir, translations, audio, mutashabihat, word meanings.
- Any API endpoint (page read endpoint is follow-up **001b**), any public import endpoint, storing fonts in the DB.
- Frontend / Words Explorer / Mushaf Reader UI.

**Acceptance criteria:**
- Migration creates exactly the five tables with the documented keys/constraints/indexes; no data in migration; `HasData` not used for Quran data.
- Running the importer against the staging tree on empty tables imports: 114 surahs, 6,236 ayahs, 604 pages, 9,046 lines, 83,668 words (6,236 markers / 77,432 real).
- All §6 **hard** checks pass; `37:130` appears as a documented **warning**, not a failure.
- Re-running without `--force` on populated tables refuses with a clear message and changes nothing; with `--force` it reproduces an identical table state.
- A validation report (md + json) is produced with totals, any mismatches, the `37:130` note, and a verdict.
- Pages 1, 2, 5, 604 reconstruct from `quran_mushaf_lines` + `quran_words`.

**Success metrics:**
- Import completes within a single transaction with a `pass` / `pass-with-warnings` verdict.
- Zero duplicate `location`/`id`; layout covers `1..83,668` contiguously; denormalized page/line equals the line table.

---

## 11. Suggested Implementation Phases

Each phase is independently reviewable and ends green (build + relevant tests). EF migration and `database update` are generated/run **only when explicitly requested**.

**Phase 1 — Domain model + EF schema**
- *Intent:* the five entities, value objects (`VerseKey`, `WordLocation`), enums (`MushafLineType`, `RevelationPlace`), EF configurations (keys/constraints/indexes, `ValueGeneratedNever`, partial index), `DbContext`.
- *Reviewable:* schema shape, constraints, indexes, domain-feature foldering.
- *Not in this phase:* generating/running migrations (only when requested); no readers, no import logic, no data.

**Phase 2 — Source readers + import DTOs**
- *Intent:* `IQuranImportSource` + manifest reader + JSON readers (words, layout, metadata) returning plain import DTOs.
- *Reviewable:* manifest validation, fail-fast behavior, DTO shapes, file-grounded counts.
- *Not in this phase:* persistence, orchestration, EF writes.

**Phase 3 — Import orchestration + validation**
- *Intent:* `ImportQuranFoundationHandler` (assemble skeleton → attach glyph/text → assign page/line/order → flag markers → backfill counts/boundaries) + `QuranImportValidator` running §6.
- *Reviewable:* join correctness, validation completeness, handler size within thresholds.
- *Not in this phase:* actual DB writes (use abstractions/in-memory), no report file yet.

**Phase 4 — Persistence / import writer**
- *Intent:* `EfBulkQuranImportWriter` (Npgsql `COPY`/bulk), transactional truncate-reload + refuse-unless-empty guard.
- *Reviewable:* transaction boundaries, `--force` semantics, bulk-insert correctness.
- *Not in this phase:* console UX, report formatting.

**Phase 5 — Validation report output**
- *Intent:* `MarkdownJsonImportReportWriter` (md + json) with totals/mismatches/warnings/verdict + traceability to sources.
- *Reviewable:* report completeness, the `37:130` warning surfaced, machine-readable shape.
- *Not in this phase:* any API.

**Phase 6 — Console host (trigger)**
- *Intent:* `tools/QuranDashboard.DataImporter` — parse `--source`/`--force`, build host, call the use case, map result to console output + exit code.
- *Reviewable:* thinness (no business logic), exit codes, DI composition.
- *Not in this phase:* any HTTP endpoint.

**Phase 7 — Build / test / review cleanup**
- *Intent:* full build, importer run on a scratch DB, confirm all §6 checks, run `engineering-review`/`backend-structure-review`, tidy.
- *Reviewable:* green build, passing validation report, architecture conformance.
- *Not in this phase:* feature 001b (the page endpoint) — separate spec.

> **Note:** feature **001b — Mushaf Page Read Endpoint** (`GET /api/mushaf/pages/{pageNumber}`, `ApiResponse<T>`, Arabic default message, implements `IMushafPageReadRepository`) is the recommended immediate follow-up and is intentionally **out of scope here**.
