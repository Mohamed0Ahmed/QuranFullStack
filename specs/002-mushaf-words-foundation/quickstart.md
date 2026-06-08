# Quickstart — Quran Mushaf Words & Layout Data Foundation

How to assemble the source set, create the schema, run the import, and verify it. Audience: the data operator (and the implementer validating their work).

## 0. Prerequisites

- .NET 10 SDK; a reachable, **empty** PostgreSQL database; connection string configured for the importer.
- The five projects build; the new `tools/QuranDashboard.DataImporter` and `tests/QuranDashboard.Tests` exist.

## 1. Assemble the source staging tree (one-time, manual)

Copy the verified source files from `resources/` into the curated tree and write the manifest:

```text
resources/import-sources/quran-foundation/
  mushaf/qpc-v4.json                       (from resources/mushaf/qpc-v4-tajweed/words/original/)
  mushaf/qpc-v4-pages-layout.json          (from resources/mushaf/qpc-v4-tajweed/layout/jsonData/)
  words/uthmani.json                       (from resources/words/with-tashkeel/original/)
  words/uthmani-simple.json                (from resources/words/without-tashkeel/original/)
  words/imlaei-simple.json                 (from resources/words/without-tashkeel/original/)
  metadata/quran-metadata-surah-name.json  (from resources/metadata/surah-names/original/)
  metadata/quran-metadata-ayah.json        (from resources/metadata/ayahs/original/)
  manifest.json                            (see contracts/import-manifest.schema.md)
  README.md
```

> This assembly is a prerequisite, **not** part of the running import. The importer only **reads** this tree.

## 2. Create the database schema (schema-only migration)

Generate and apply the migration **only when explicitly requested** (per `Backend/CLAUDE.md`):

```bash
# from Backend/
dotnet ef migrations add QuranFoundationSchema \
  -p infrastructure/QuranDashboard.Infrastructure \
  -s api/QuranDashboard.Api
dotnet ef database update -p infrastructure/QuranDashboard.Infrastructure -s api/QuranDashboard.Api
```

The migration MUST contain **schema only** (5 tables, constraints, indexes) — **no Quran data**, no `HasData`.

## 3. Run the import

```bash
# from Backend/
dotnet run --project tools/QuranDashboard.DataImporter -- \
  --source ../resources/import-sources/quran-foundation \
  --report-out ../resources/report
# add --force to wipe-and-reload non-empty tables
```

Expected behavior:
- Validates the manifest/source set first (fail-fast on any mismatch).
- Refuses if tables are non-empty and `--force` was not given.
- Assembles → validates → (only on pass) bulk-loads in one transaction → writes the report.
- Exit code `0` on `pass`/`pass-with-warnings`; non-zero on `fail`.

## 4. Verify the import

**a. Read the report** `resources/report/quran-foundation-import-report.md` — verdict should be `pass-with-warnings` (the only warning is `37:130`).

**b. Spot-check the database:**

```sql
SELECT count(*) FROM quran_surahs;        -- 114
SELECT count(*) FROM quran_ayahs;         -- 6236
SELECT count(*) FROM quran_mushaf_pages;  -- 604
SELECT count(*) FROM quran_mushaf_lines;  -- 9046
SELECT count(*) FROM quran_words;         -- 83668
SELECT count(*) FROM quran_words WHERE is_ayah_marker;       -- 6236
SELECT count(*) FROM quran_words WHERE NOT is_ayah_marker;   -- 77432

-- contiguity & uniqueness
SELECT min(id), max(id), count(DISTINCT id) FROM quran_words;          -- 1, 83668, 83668
SELECT count(*) FROM (SELECT location FROM quran_words GROUP BY location HAVING count(*)>1) d; -- 0

-- a sample word resolves fully
SELECT location, page_number, line_number, line_word_order, is_ayah_marker
FROM quran_words WHERE location = '2:25:3';

-- page 1 reconstructs (8 lines; line 1 = surah_name, lines 2..8 = ayah)
SELECT line_number, line_type, first_word_id, last_word_id
FROM quran_mushaf_lines WHERE page_number = 1 ORDER BY line_number;

-- bismillah vs basmallah counts both = 112
SELECT count(*) FROM quran_surahs WHERE bismillah_pre;                          -- 112
SELECT count(*) FROM quran_mushaf_lines WHERE line_type = 'basmallah';          -- 112
```

**c. Re-run safety:**
- Run step 3 again **without** `--force` → it refuses, changes nothing.
- Run with `--force` → counts identical to the first import.

## 5. Done criteria

- Report verdict `pass-with-warnings`; all hard checks pass; the only warning is `37:130`.
- All counts above match exactly.
- Page fonts are not imported or validated (out of scope); `qpc_glyph` is populated from `qpc-v4.json`.
- No API endpoint exists yet (that is follow-up **001b**).
