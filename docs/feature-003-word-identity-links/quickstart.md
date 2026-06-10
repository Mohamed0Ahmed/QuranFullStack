# Quickstart — Word Identity Links (Dev Reset / Reseed)

How to reset a **local/dev** PostgreSQL database, apply the Feature 003 identity-link
schema, re-import the canonical Quran foundation, rebuild display tables with populated
`quran_words` identity links, and verify the result.

Audience: developers validating Feature 003 after Phases 1–6 (unique-simple imlaei
identity + nullable link columns + rebuilder population + hard checks + tests).

> Commands run from `Backend/` unless noted. Requires .NET 10, PostgreSQL, and the curated
> import tree at `resources/import-sources/quran-foundation/` (see Feature 002 quickstart for
> assembly).

### Connection string

- **EF scripts** (`./scripts/reset-db`, `./scripts/update-db`) use the API project and
  `dotnet user-secrets` (`api/QuranDashboard.Api`).
- **DataImporter** reads `tools/QuranDashboard.DataImporter/appsettings.json` and environment
  variables. If import/rebuild cannot connect, set
  `ConnectionStrings__QuranDashboardDb` to the same value as your API user secret, or update
  the importer `appsettings.json`.

## Why a full reset?

Feature 003 adds columns to `quran_words` and changes `unique_simple` grouping to
`word_key_imlaei_simple`. A clean reset guarantees:

- Migrations apply on an empty database (no partial legacy state).
- Foundation data is loaded fresh from the reviewed import sources.
- `rebuild-words --force` truncates derived tables with `RESTART IDENTITY`, nulls stale
  links, rebuilds unique/ordered tables, populates links, and gates commit on hard checks.

This is **acceptable in development** because Quran core data is fully reproducible from
canonical sources and no live user/gate data depends on unique ids yet. See the
implementation plan §12 for the future production note on stable ids.

## 1. Reset and migrate

Use the Backend helper script (drop + `database update`):

```bash
cd Backend
./scripts/reset-db --yes
```

Equivalent manual steps:

```bash
./scripts/drop-db --yes
./scripts/update-db
```

This applies all EF migrations, including `AddUniqueSimpleImlaeiIdentity` and
`AddQuranWordIdentityLinks`.

## 2. Import foundation

```bash
dotnet run --project tools/QuranDashboard.DataImporter -- \
  import-foundation \
  --source ../resources/import-sources/quran-foundation \
  --report-out ../resources/report
```

Expected after import:

- `quran_words`: **83,668** rows (77,432 readable + 6,236 ayah markers).
- `unique_tashkeel_word_id` and `unique_simple_word_id` on `quran_words`: **NULL** for all
  rows (links are populated only by rebuild).

Review `resources/report/quran-foundation-import-report.md` for verdict `pass` or
`pass-with-warnings`.

## 3. Rebuild display tables and identity links

```bash
dotnet run --project tools/QuranDashboard.DataImporter -- \
  rebuild-words --force \
  --report-out ../resources/report/words-display
```

When `--report-out` is omitted, reports default to `resources/report/words-display/`.

Behavior:

- `--force` truncates the four derived tables (`RESTART IDENTITY`), nulls link columns on
  `quran_words`, rebuilds unique/ordered tables, populates identity links, runs hard checks,
  and commits only if all hard checks pass.
- Writes `words-display-report.md` and `words-display-report.json`.

Expected totals after a successful rebuild:

| Table / metric | Expected |
| --- | --- |
| `quran_words` (total) | 83,668 |
| readable / markers | 77,432 / 6,236 |
| `quran_words_ordered_tashkeel` | 77,432 |
| `quran_words_ordered_simple` | 77,432 |
| `quran_words_unique_tashkeel` | 21,294 |
| `quran_words_unique_simple` | 14,783 |

## 4. Verify

**a. Read the rebuild report** — verdict `pass`; all hard checks ✅ including the four
`LINK-*` checks (`LINK-READABLE-COMPLETE`, `LINK-MARKERS-NULL`, `LINK-RESOLVES`,
`LINK-CONSISTENT`).

**b. Spot-check the database:**

```sql
-- foundation counts unchanged
SELECT count(*) FROM quran_words;                              -- 83668
SELECT count(*) FROM quran_words WHERE is_ayah_marker;         -- 6236
SELECT count(*) FROM quran_words WHERE NOT is_ayah_marker;     -- 77432

-- derived counts
SELECT count(*) FROM quran_words_unique_simple;                -- 14783
SELECT count(*) FROM quran_words_unique_tashkeel;              -- 21294

-- identity links: readable complete, markers null
SELECT count(*) FROM quran_words
WHERE NOT is_ayah_marker
  AND (unique_tashkeel_word_id IS NULL OR unique_simple_word_id IS NULL);  -- 0

SELECT count(*) FROM quran_words
WHERE is_ayah_marker
  AND (unique_tashkeel_word_id IS NOT NULL OR unique_simple_word_id IS NOT NULL);  -- 0

-- anchor spot-check (unique-simple occurrences by imlaei key)
SELECT word_key_imlaei_simple, occurrences_count
FROM quran_words_unique_simple
WHERE word_key_imlaei_simple IN ('الله', 'العظيم', 'الرحمان');
-- الله 2155, العظيم 36, الرحمان 45
```

**c. Run integration tests:**

```bash
dotnet test tests/QuranDashboard.Tests --filter "FullyQualifiedName~WordsDisplay"
dotnet test tests/QuranDashboard.Tests --filter "FullyQualifiedName~DisplayWordsRealImportIdentityLinks"
```

Real-import identity-link tests require the canonical import source tree (gated by
`CanonicalImportSourceTestGate`).

## What this workflow does NOT do

- No API or frontend changes.
- No production stable-id strategy (documented future note only).
- No modification of import sources under `resources/import-sources/`.
- No hand-written migrations — schema changes use `./scripts/add-mig` only.
