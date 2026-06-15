# Quickstart — Quran Translations Foundation

How to implement, run, and verify the Feature 008 translation import. Commands are illustrative and should
be run from `Backend/` unless noted.

## 1. Confirm the local source package

The final staged package is local and Git-ignored:

```text
../resources/import-sources/quran-translations/
  README.md
  manifest.json
  source-display-metadata.json
  source-display-metadata.review.json
  package-report.md
  sources/
    167 copied <sourceKey>[.fn].json files
```

Expected manifest/display counts:

- Approved sources: 167
- Simple sources: 129
- With-footnotes sources: 38
- Excluded sources: 19
- Languages: 83
- Ayahs per source: 6,236
- Source-to-ayah mappings after import: 1,041,412

The importer must not read random upstream folders and must not edit this package.

## 2. Create schema during implementation

When implementation reaches schema work, generate a schema-only migration by EF tooling on explicit
request. Do not hand-write migration files.

Planned tables:

- `quran_translation_sources`
- `quran_translation_ayah_entries`

No seed data belongs in the migration. Source data is loaded only by the operator import verb.

## 3. Run the import

```bash
# default package and report paths
dotnet run --project tools/QuranDashboard.DataImporter -- import-translations

# explicit source and report path
dotnet run --project tools/QuranDashboard.DataImporter -- import-translations \
  --source ../resources/import-sources/quran-translations \
  --report-out report/feature-008-quran-translations-foundation

# rebuild after a prior run
dotnet run --project tools/QuranDashboard.DataImporter -- import-translations --force
```

Behavior:

- Refuses if the package shape, final manifest, final display metadata, file set, file size, or sha256 do
  not match.
- Treats the manifest as final only when `manifestType = "quran-translation-import-source-package"` and
  `isFinalImportManifest = true`.
- Treats display metadata as final only when `metadataType = "quran-translation-source-display-metadata"`,
  `status = "final"`, `sourceCount = 167`, and all required fields are non-empty.
- Refuses if any excluded source is importable.
- Refuses if any approved source lacks the exact 6,236 verse-key set.
- Refuses if any `t` value is empty, null, missing, or non-string.
- Refuses if `quran_ayahs` is missing or does not resolve all source verse keys.
- Refuses a normal run if translation tables already contain data.
- `--force` rebuilds only translation-owned tables.
- Stores translation text exactly as imported.
- Never copies Arabic Quran ayah text.
- Accepts a run only if both required reports are written.

Default report output:

```text
report/feature-008-quran-translations-foundation/
  translation-import-report.md
  translation-import-report.json
```

## 4. Verify the result

Read the report first. It should show `PASS`, `persisted = true`, all hard checks passing, and the
license/provenance warning.

Optional database spot checks:

```sql
SELECT count(*) FROM quran_translation_sources;       -- 167
SELECT count(*) FROM quran_translation_ayah_entries;  -- 1041412
SELECT count(DISTINCT ayah_id) FROM quran_translation_ayah_entries; -- 6236

SELECT translation_type, count(*)
FROM quran_translation_sources
GROUP BY translation_type
ORDER BY translation_type;
-- simple = 129, with_footnotes = 38

-- no excluded source keys persisted
SELECT count(*)
FROM quran_translation_sources
WHERE source_key IN ('ko-unknown', 'sq-unknown'); -- 0

-- all ayah links resolve by FK; this query should be 0
SELECT count(*)
FROM quran_translation_ayah_entries tae
LEFT JOIN quran_ayahs a ON a.id = tae.ayah_id
WHERE a.id IS NULL;

-- no copied Arabic Quran ayah text in translation rows; this query should be 0
SELECT count(*)
FROM quran_translation_ayah_entries tae
JOIN quran_ayahs a ON a.id = tae.ayah_id
WHERE tae.text = a.text_uthmani;

-- future read recipe: all translations for one ayah
SELECT s.language_code, s.translation_type, s.display_name_en, s.display_name_ar, tae.text
FROM quran_translation_ayah_entries tae
JOIN quran_translation_sources s ON s.id = tae.source_id
WHERE tae.ayah_id = :ayah_id
ORDER BY s.language_code, s.translation_type, s.source_key;
```

## 5. Run tests

```bash
dotnet test tests/QuranDashboard.Tests
```

Expected test areas:

- Manifest/package validation.
- Final display metadata validation.
- Source/display metadata alignment.
- Source reader object shape and complete verse-key set.
- Empty/null/missing/non-string `t` refusal.
- Ayah key resolution.
- Excluded source refusal/reporting.
- Duplicate source/ayah mapping failure.
- Exact text preservation, including inline `[[...]]` and embedded HTML.
- Force/refusal behavior.
- Transaction rollback.
- Report shape and required warning.
- Quran foundation data and source package unchanged.

Use synthetic source-safe translation text in tests unless a real fixture is explicitly copied from the
local source package with traceable provenance.

## Out of scope

No API, frontend, public reader, search indexing, word-by-word import, app-user permissions, startup
seeding, source package editing, excluded-source persistence, footnote parsing/sanitization, plain-text
derivative, license/provenance publication, or Quran foundation data mutation.
