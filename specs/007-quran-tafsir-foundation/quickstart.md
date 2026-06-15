# Quickstart — Quran Tafsir Foundation

How to implement, run, and verify the Feature 007 tafsir import. Commands are illustrative and should be
run from `Backend/` unless noted.

## 1. Confirm the local source package

The final staged package is local and Git-ignored:

```text
../resources/import-sources/quran-tafsirs/
  README.md
  manifest.json
  package-report.md
  sources/
    84 copied <sourceKey>.json files
```

Expected manifest counts:

- Approved sources: 84
- Excluded sources: 9
- Arabic approved sources: 35
- Non-Arabic approved sources: 49
- Languages: 33
- Source-to-ayah mappings after import: 523,824

The importer must not read random upstream folders and must not edit this package.

## 2. Create schema during implementation

When implementation reaches schema work, generate a schema-only migration by EF tooling on explicit
request. Do not hand-write migration files.

Planned tables:

- `quran_tafsir_sources`
- `quran_tafsir_entries`
- `quran_tafsir_ayah_entries`

No seed data belongs in the migration. Source data is loaded only by the operator import verb.

## 3. Run the import

```bash
# default package and report paths
dotnet run --project tools/QuranDashboard.DataImporter -- import-tafsirs

# explicit source and report path
dotnet run --project tools/QuranDashboard.DataImporter -- import-tafsirs \
  --source ../resources/import-sources/quran-tafsirs \
  --report-out ../resources/report/quran-tafsirs

# rebuild after a prior run
dotnet run --project tools/QuranDashboard.DataImporter -- import-tafsirs --force
```

Behavior:

- Refuses if the package shape, final manifest, file set, file size, or sha256 do not match.
- Treats the manifest as final only when `manifestType = "quran-tafsir-import-source-package"` and `isFinalImportManifest = true`.
- Refuses if any excluded source is importable.
- Refuses if `quran_ayahs` is missing or does not resolve all source ayah keys.
- Refuses a normal run if tafsir tables already contain data.
- `--force` rebuilds only tafsir-owned tables.
- Stores tafsir text exactly as imported.
- Never copies Quran ayah text.
- Accepts a run only if both required reports are written.

Default report output:

```text
../resources/report/quran-tafsirs/
  tafsir-import-report.md
  tafsir-import-report.json
```

## 4. Verify the result

Read the report first. It should show `PASS`, `persisted = true`, all hard checks passing, and the
license/provenance warning.

Optional database spot checks:

```sql
SELECT count(*) FROM quran_tafsir_sources;      -- 84
SELECT count(*) FROM quran_tafsir_ayah_entries; -- 523824
SELECT count(DISTINCT ayah_id) FROM quran_tafsir_ayah_entries; -- 6236

-- no excluded source keys persisted
SELECT count(*) FROM quran_tafsir_sources
WHERE source_key IN (
  'ar-wajiz',
  'ar-durr-al-manthur',
  'ar-ibn-al-qayyim',
  'ar-ibn-uthaymeen',
  'ar-baydawi',
  'ar-suddi',
  'ar-muyassar-fi-al-gharib',
  'id-saadi',
  'tr-ibn-kathir'
); -- 0

-- all ayah links resolve by FK; this query should be 0
SELECT count(*)
FROM quran_tafsir_ayah_entries tae
LEFT JOIN quran_ayahs a ON a.id = tae.ayah_id
WHERE a.id IS NULL;

-- future read recipe: all tafsir sources for one ayah
SELECT s.source_key, e.tafsir_text
FROM quran_tafsir_ayah_entries tae
JOIN quran_tafsir_sources s ON s.id = tae.source_id
JOIN quran_tafsir_entries e ON e.id = tae.tafsir_entry_id
WHERE tae.ayah_id = :ayah_id
ORDER BY s.language_code, s.source_key;
```

## 5. Run tests

```bash
dotnet test tests/QuranDashboard.Tests
```

Expected test areas:

- Manifest/package validation.
- Source reader object values and string pointers.
- Grouped tafsir block assembly.
- Ayah key resolution.
- Excluded source refusal/reporting.
- Duplicate source/ayah mapping failure.
- Force/refusal behavior.
- Transaction rollback.
- Report shape and required warning.
- Quran foundation data and source package unchanged.

Use synthetic source-safe tafsir text in tests unless a real fixture is explicitly copied from the local
source package with traceable provenance.

## Out of scope

No API, frontend, public reader, search indexing, translations feature, app-user permissions, startup
seeding, source package editing, excluded-source persistence, plain-text derivative, or Quran foundation
data mutation.
