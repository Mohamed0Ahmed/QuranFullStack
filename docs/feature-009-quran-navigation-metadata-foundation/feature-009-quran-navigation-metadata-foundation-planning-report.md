# Feature 009 — Quran Navigation Metadata Foundation — Planning Report

**Status:** Planning / documentation only. No Spec Kit run, code, migration, DB update, Backend edit, resource edit, or commit was performed.
**Purpose:** Source of truth for `/speckit.specify`.
**Date:** 2026-06-16
**Companion analysis:** `docs/feature-009-quran-navigation-metadata-foundation/quran-metadata-inventory-gap-analysis-report.md`
**Staged source package:** `App/resources/import-sources/quran-navigation-metadata/`

---

## 1. Verdict

✅ **Ready to specify.** Scope, data-model decisions, source package, and validation strategy are all locked. Feature 009 adds the four Quran navigation/division datasets (`juz`, `hizb`, `rub`, `sajda`) that Feature 002 deliberately deferred, via four new tables plus three additive denormalized columns on `quran_ayahs`, fed by a new dedicated importer that follows the established tafsir/translations/mutashabihat pattern. No existing data is re-imported and no Quran text is touched.

---

## 2. Background / Why this feature exists

Feature 002 (Quran Foundation) imported and validated the Quran core: **114 surahs, 6236 ayahs, 604 pages, 9046 lines, 83668 words**. Its plan explicitly deferred division/navigation metadata:

> *"Not yet: no `juz`/`hizb`/`rub` columns (data exists but is a later navigation layer)."*

The companion gap-analysis (Verdict C) confirmed that `surah-names` and `ayahs` metadata are already fully represented in `quran_surahs` / `quran_ayahs` (and must **not** be re-imported), while four datasets are present in resources but absent from the database:

| Dataset | Records | Concept |
|---|---|---|
| juz | 30 | الجزء — 30 major divisions |
| hizb | 60 | الحزب — half-juz divisions |
| rub | 240 | الربع — quarter-hizb divisions |
| sajda | 15 | سجدة — prostration locations + type |

Feature 009 closes exactly this gap and unlocks ayah→division navigation (which juz/hizb/rub an ayah belongs to) for future reader/details surfaces — without building those surfaces now.

---

## 3. Source package summary

The importer reads **only** this staged, gitignored package. Values below are the authoritative `manifest.json` contract.

- **packageType:** `quran-navigation-metadata-import-source-package`
- **isFinalImportManifest:** `true`
- **createdAtUtc:** `2026-06-16T08:31:05Z`
- **sourceRoot (provenance, read-only, do NOT read at import):** `/projects/Dashboard/resources/metadata`

| File | datasetKey | recordCount | sizeBytes | sha256 |
|---|---|---|---|---|
| `sources/quran-metadata-juz.json` | juz | 30 | 4933 | `0e34f5abbd786c828d388b1f3c732db8b42b1bf36a4f619603e4c632f9e86628` |
| `sources/quran-metadata-hizb.json` | hizb | 60 | 8567 | `6a99e08337fc3629b3c0ae04897cbdd33c818fea94ff01d1be8aa33a80e2cb64` |
| `sources/quran-metadata-rub.json` | rub | 240 | 30480 | `9a8b17d69a3c173bff92aae5d53f6f83829cc8f513514b82266c4e4a3f81fd38` |
| `sources/quran-metadata-sajda.json` | sajda | 15 | 1049 | `5b7a7859b57b03387b9b58fd46dfdcb3792b61cad5e982f0295ac29603958988` |

**Record shapes (keyed JSON objects):**

```jsonc
// juz / hizb / rub  (identical shape; only the *_number key differs)
{ "juz_number":1, "verses_count":148, "first_verse_key":"1:1", "last_verse_key":"2:141",
  "verse_mapping": { "1":"1-7", "2":"1-141" } }   // { surah: "from-to" }

// sajda
{ "sajdah_number":1, "verse_key":"7:206", "sajdah_type":"optional" }  // type ∈ {required×4, optional×11}
```

`verse_mapping` is the key to ayah-level population: for each division it lists, per surah, the inclusive ayah range belonging to that division. It is parsed at import time and **never** stored verbatim in the DB.

---

## 4. Locked scope (in scope)

- Import `juz`, `hizb`, `rub`, `sajda` from the staged package.
- Create four navigation tables: `quran_juzs`, `quran_hizbs`, `quran_rubs`, `quran_sajdas`.
- Add three additive denormalized columns to `quran_ayahs`: `juz_number`, `hizb_number`, `rub_number`.
- Populate those three `quran_ayahs` columns from `verse_mapping` during the navigation import.
- Reference ayahs by `verse_key` (resolved to `quran_ayahs`), never by copied Quran text.
- A **separate, dedicated** navigation-metadata importer (new CLI verb).
- A separate validation + JSON/Markdown reporting flow, mirroring tafsir/translations/mutashabihat importers.

---

## 5. Out of scope

- No UI, no API endpoints, no frontend.
- No search feature.
- No startup/runtime seeding.
- No re-import of surah metadata; no re-import of ayah metadata.
- No reading or copying of ayah text; no mutation of `quran_ayahs.text_uthmani`.
- No changes to `quran_words`.
- No `ruku`, no `manzil`, no audio metadata.
- No extension or rerun of the Feature 002 Quran foundation importer.
- No `quran_division_ayah_ranges` child table; no `verse_mapping` JSON persisted (v1).
- No `is_sajda` / `sajda_type` columns on `quran_ayahs` (v1).

---

## 6. Locked decisions

1. **juz/hizb/rub representation** — header tables (`quran_juzs`, `quran_hizbs`, `quran_rubs`) **plus** denormalized columns (`juz_number`, `hizb_number`, `rub_number`) on `quran_ayahs`. No child range table; no JSON column. `verse_mapping` is parsed at import to populate the ayah columns.
2. **sajda representation** — dedicated `quran_sajdas` table. No `is_sajda` / `sajda_type` columns on `quran_ayahs` in v1.
3. **Link strategy** — `verse_key` is the source join key; resolved to `quran_ayahs` during validation/import. Do **not** rely on metadata numeric `id` aligning with `quran_ayahs.id` as the contract.
4. **Source package** — importer reads only `App/resources/import-sources/quran-navigation-metadata/`; never `/projects/Dashboard/resources/metadata`.
5. **Source integrity** — `manifest.json` is final and authoritative. Validate `packageType`, `isFinalImportManifest`, exact source file set, `sha256`, `sizeBytes`, `recordCount`, required fields, and allowed `sajdah_type` values.

---

## 7. Proposed data model

### New tables

**`quran_juzs`**

| Column | Type | Notes |
|---|---|---|
| `juz_number` | smallint | PK (1..30) |
| `verses_count` | smallint | not null |
| `first_ayah_id` | int | not null, FK → `quran_ayahs.id` |
| `last_ayah_id` | int | not null, FK → `quran_ayahs.id` |
| `first_verse_key` | text | not null |
| `last_verse_key` | text | not null |

**`quran_hizbs`**

| Column | Type | Notes |
|---|---|---|
| `hizb_number` | smallint | PK (1..60) |
| `juz_number` | smallint | not null, FK → `quran_juzs.juz_number` |
| `verses_count` | smallint | not null |
| `first_ayah_id` | int | not null, FK → `quran_ayahs.id` |
| `last_ayah_id` | int | not null, FK → `quran_ayahs.id` |
| `first_verse_key` | text | not null |
| `last_verse_key` | text | not null |

**`quran_rubs`**

| Column | Type | Notes |
|---|---|---|
| `rub_number` | smallint | PK (1..240) |
| `hizb_number` | smallint | not null, FK → `quran_hizbs.hizb_number` |
| `verses_count` | smallint | not null |
| `first_ayah_id` | int | not null, FK → `quran_ayahs.id` |
| `last_ayah_id` | int | not null, FK → `quran_ayahs.id` |
| `first_verse_key` | text | not null |
| `last_verse_key` | text | not null |

**`quran_sajdas`**

| Column | Type | Notes |
|---|---|---|
| `sajdah_number` | smallint | PK (1..15) |
| `ayah_id` | int | not null, FK → `quran_ayahs.id` |
| `verse_key` | text | not null |
| `sajdah_type` | text / enum-backed | allowed: `required`, `optional` (store lowercase; enum `Required`/`Optional` in domain) |

### Additive columns on `quran_ayahs`

| Column | Type | Notes |
|---|---|---|
| `juz_number` | smallint | nullable at schema level; populated by importer |
| `hizb_number` | smallint | nullable at schema level; populated by importer |
| `rub_number` | smallint | nullable at schema level; populated by importer |

**Migration is additive.** Columns are nullable for migration safety, but the importer/validator must guarantee **all 6236 ayahs** carry non-null `juz_number`/`hizb_number`/`rub_number` after a successful run. Follow Backend EF rules: generate the migration with EF tooling (no hand-written migrations), do not run `database update` unless explicitly requested.

### Domain / persistence placement (convention)

New entities under `domain/.../Quran/Navigation/` (`Juz`, `Hizb`, `Rub`, `Sajda`, `SajdahType` enum); EF configs under `infrastructure/.../Persistence/Configurations/Quran/Navigation/`. The three new `quran_ayahs` columns extend the existing `Ayah` entity + `AyahConfiguration` additively.

---

## 8. Importer design

Mirror the established import pipeline (as used by `import-translations`, `import-tafsirs`, `import-mutashabihat`).

- **New CLI verb:** `import-navigation-metadata` (sits alongside `import-foundation`, `import-morphology`, `import-mutashabihat`, `import-tafsirs`, `import-translations`).
- **Pipeline:** source reader → assembler (verse_mapping expansion) → validator → EF bulk writer / single transaction → report writer. Suggested file set mirroring `Translations/`:
  - `NavigationMetadataReader` / source DTOs (Application.Abstractions contract + Infrastructure impl)
  - `NavigationMetadataAssembler` (expands `verse_mapping` → per-ayah juz/hizb/rub assignments; builds header rows)
  - `NavigationMetadataValidationRunner`
  - `EfBulkNavigationMetadataImportWriter` + `NavigationMetadataBulkCopier` + `NavigationMetadataSql`
  - `NavigationMetadataCommandExecutor`
  - `NavigationMetadataImportReportBuilder`
  - `NavigationInvariants` (counts 30/60/240/15, expected total 6236, allowed sajdah types)
- **Re-run guard:** refuse to run if any target table (`quran_juzs/hizbs/rubs/sajdas`) is non-empty **or** any `quran_ayahs` navigation column is already populated, unless `--force` is supplied.
- **`--force`:** atomically clear/reload **only** navigation tables and the three `quran_ayahs` navigation columns (set back to NULL, then repopulate) inside one transaction. Never truncates/edits foundation, words, tafsir, translations, mutashabihat, morphology, or i3rab data.
- **Isolation guarantee:** the importer never reads ayah `text` from the metadata sources and never writes `quran_ayahs.text_uthmani` or any word/text column.
- **Atomicity:** all writes (4 header tables + ayah-column update) commit in a single transaction; failure rolls back fully.

---

## 9. Validation and reporting strategy

### Hard checks (fail the import)

| ID | Check |
|---|---|
| NAV-PACKAGE-SHAPE | Required files exist; only the expected source set is present. |
| NAV-MANIFEST-FINAL | `packageType` correct and `isFinalImportManifest == true`. |
| NAV-SOURCE-COUNT | Counts exactly juz=30, hizb=60, rub=240, sajda=15. |
| NAV-SOURCE-HASH | `sha256` and `sizeBytes` match `manifest.json` for every file. |
| NAV-JSON-SHAPE | Required fields present per dataset (juz/hizb/rub: `*_number, verses_count, first_verse_key, last_verse_key, verse_mapping`; sajda: `sajdah_number, verse_key, sajdah_type`). |
| NAV-VERSE-KEYS-RESOLVE | Every `first_verse_key`, `last_verse_key`, and sajda `verse_key` resolves to a `quran_ayahs` row. |
| NAV-RANGE-COVERAGE-JUZ | Juz `verse_mapping` covers all 6236 ayahs exactly once. |
| NAV-RANGE-COVERAGE-HIZB | Hizb `verse_mapping` covers all 6236 ayahs exactly once. |
| NAV-RANGE-COVERAGE-RUB | Rub `verse_mapping` covers all 6236 ayahs exactly once. |
| NAV-NO-RANGE-GAPS-OVERLAPS | No gaps or overlaps within each division type. |
| NAV-HIERARCHY | Each hizb belongs to exactly one juz; each rub to exactly one hizb. |
| NAV-SAJDA-TYPE | `sajdah_type` ∈ {`required`, `optional`} only. |
| NAV-AYAH-COLUMNS-COMPLETE | After import, all 6236 `quran_ayahs` have non-null `juz_number`/`hizb_number`/`rub_number`. |
| NAV-NO-QURAN-TEXT-COPY | Importer never reads or persists Quran ayah text from metadata sources. |
| NAV-SOURCE-UNCHANGED | Staged source files unchanged between load and commit. |
| NAV-RERUN-GUARD | Refuse non-empty target without `--force`. |

### Warning checks (report, do not fail)

| ID | Check |
|---|---|
| NAV-VERSE-COUNT-MATCH | Source `verses_count` matches the computed range count per division. |
| NAV-SAJDA-DISTRIBUTION | Expected distribution `optional=11`, `required=4`. |

### Reports (JSON + Markdown)

Each report must include: **verdict**, **persisted** (bool), **forced** (bool), **source path**, **totals**, **check results** (per-ID pass/fail), **warnings/errors**, **counts** (per dataset), **ayah coverage summaries** (juz/hizb/rub completeness over 6236), and an explicit **"no Quran ayah text"** assertion. Output location follows Backend convention: `Backend/report/feature-009-quran-navigation-metadata-foundation/`.

---

## 10. Testing strategy

- **Reader tests:** manifest/package validation (shape, final flag, file set, sha256/size, counts).
- **Assembler tests:** `verse_mapping` expansion to per-ayah juz/hizb/rub (incl. surah-spanning ranges).
- **Validation tests:** detect gaps, overlaps, unresolved verse keys, invalid `sajdah_type`, hierarchy violations.
- **Integration tests:** against synthetic `quran_ayahs` (small fixture) → header rows + populated ayah columns.
- **Re-run refusal tests:** non-empty target without `--force` is rejected.
- **`--force` tests:** truncate/reload navigation tables + reset/repopulate ayah columns atomically.
- **Report shape tests + no-Quran-text safety tests:** report fields present; assert no ayah text read/persisted.
- **Real-package gated test:** runs only if `App/resources/import-sources/quran-navigation-metadata/` exists; validates the actual 30/60/240/15 package end-to-end.

Test-code rules: construct real DTOs/entities/value objects (e.g. `VerseKey`), use real infrastructure for persistence/coverage correctness, data-drive the dataset variants, and keep Quranic test data source-safe (use `verse_key`s, not ayah text).

---

## 11. Implementation phases recommendation

| Phase | Content | Gate |
|---|---|---|
| **P1 — Domain + schema** | Entities (`Juz`, `Hizb`, `Rub`, `Sajda`, `SajdahType`), EF configs, additive `quran_ayahs` columns, EF-tooling migration (generate only; do not apply). | Build green; migration generated and reviewed. |
| **P2 — Reader + manifest validation** | Source reader, DTOs, manifest/package integrity checks (NAV-PACKAGE-SHAPE, -MANIFEST-FINAL, -SOURCE-COUNT, -SOURCE-HASH, -JSON-SHAPE). | Reader tests green. |
| **P3 — Assembler** | `verse_mapping` expansion, header-row build, hierarchy derivation. | Assembler/coverage tests green. |
| **P4 — Validator** | All hard + warning checks, incl. coverage/gaps/overlaps/hierarchy/sajda type. | Validation tests green. |
| **P5 — Writer + executor** | EF bulk write in one transaction, ayah-column population, re-run guard, `--force` reload. | Integration + rerun/force tests green. |
| **P6 — Reporting** | JSON + Markdown report builder; report-shape + no-text safety tests. | Report tests green. |
| **P7 — Real run (gated, explicit)** | Run `import-navigation-metadata` against the real package; produce validation/real-run/completion reports. | Only on explicit request; DB update explicitly authorized. |

---

## 12. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Touching the Feature 002 `quran_ayahs` table (cross-feature edit) | Additive nullable columns + post-import `UPDATE`; never alter existing columns or text. |
| Partial/failed import leaving ayah columns half-populated | Single transaction; NAV-AYAH-COLUMNS-COMPLETE blocks a "successful" verdict unless all 6236 populated. |
| Accidental Quran text leakage | NAV-NO-QURAN-TEXT-COPY check + reader that ignores any `text` field; tests assert no text read/persisted. |
| Reading from upstream `/projects/Dashboard/resources/metadata` instead of staged package | Hard-code the staged package path; NAV-SOURCE-HASH ties data to the manifest. |
| Verse-key vs numeric-id mismatch | `verse_key` is the contract; resolve to `quran_ayahs` via unique `verse_key` index, store resolved `*_ayah_id`. |
| Re-run clobbering data | NAV-RERUN-GUARD refuses non-empty targets; `--force` is explicit and scoped to navigation only. |
| `resources/` is gitignored / unavailable in CI | Real-package test is gated on package presence; unit/integration tests use synthetic fixtures. |
| Hierarchy denormalization (`juz_number` on hizb, `hizb_number` on rub) drift | Derived at import from coverage; NAV-HIERARCHY enforces exact containment. |
| Migration discipline | EF tooling only; no hand-written migration/snapshot edits; no `database update` without explicit request. |

---

## 13. Spec Kit input summary

- **Feature:** 009 — Quran Navigation Metadata Foundation.
- **One-line:** Import juz/hizb/rub/sajda from the staged package into four new tables + three additive `quran_ayahs` columns, via a dedicated, fully-validated importer; no UI/API, no text touched.
- **Data deltas:** +4 tables (`quran_juzs`, `quran_hizbs`, `quran_rubs`, `quran_sajdas`); +3 columns on `quran_ayahs` (`juz_number`, `hizb_number`, `rub_number`).
- **Importer:** new CLI verb `import-navigation-metadata`, re-run guarded, `--force` reload, single transaction, JSON+MD report.
- **Source contract:** `App/resources/import-sources/quran-navigation-metadata/` + `manifest.json` (final), counts 30/60/240/15.
- **Acceptance:** all NAV-* hard checks pass; all 6236 ayahs get non-null juz/hizb/rub; no Quran text read or persisted.
- **Explicitly excluded:** UI/API/frontend/search/seeding, surah/ayah re-import, text/word mutation, ruku/manzil/audio, foundation-importer reuse.

---

## 14. Suggested `/speckit.specify` prompt seed

> Create the spec for **Feature 009 — Quran Navigation Metadata Foundation**.
>
> Import the Quran navigation/division datasets **juz (30), hizb (60), rub (240), sajda (15)** from the staged source package at `App/resources/import-sources/quran-navigation-metadata/` (authoritative `manifest.json`, `packageType: quran-navigation-metadata-import-source-package`, `isFinalImportManifest: true`). These were intentionally deferred by Feature 002; surah and ayah metadata are already in `quran_surahs`/`quran_ayahs` and must **not** be re-imported.
>
> Add four tables — `quran_juzs`, `quran_hizbs`, `quran_rubs`, `quran_sajdas` — and three additive denormalized columns on `quran_ayahs`: `juz_number`, `hizb_number`, `rub_number`, populated from each division's `verse_mapping` during import. Use header-tables-plus-denormalized-columns (no child range table, no JSON column); a dedicated `quran_sajdas` table (no sajda columns on `quran_ayahs`). Link by `verse_key` resolved to `quran_ayahs`, never numeric-id alignment, never copied Quran text.
>
> Build a **separate** importer (CLI verb `import-navigation-metadata`) following the tafsir/translations/mutashabihat pipeline (reader → assembler → validator → EF bulk writer/transaction → JSON+MD report). It must be re-run guarded (refuse non-empty targets without `--force`; `--force` atomically reloads only navigation tables and the three ayah columns) and must never touch foundation text, words, tafsir, translations, mutashabihat, morphology, or i3rab.
>
> Enforce the hard checks NAV-PACKAGE-SHAPE, NAV-MANIFEST-FINAL, NAV-SOURCE-COUNT, NAV-SOURCE-HASH, NAV-JSON-SHAPE, NAV-VERSE-KEYS-RESOLVE, NAV-RANGE-COVERAGE-{JUZ,HIZB,RUB}, NAV-NO-RANGE-GAPS-OVERLAPS, NAV-HIERARCHY, NAV-SAJDA-TYPE, NAV-AYAH-COLUMNS-COMPLETE, NAV-NO-QURAN-TEXT-COPY, NAV-SOURCE-UNCHANGED, NAV-RERUN-GUARD; plus warnings NAV-VERSE-COUNT-MATCH and NAV-SAJDA-DISTRIBUTION (optional=11, required=4). Migration is additive and EF-tooling-generated; do not apply it or run the real import without explicit authorization.
>
> **Out of scope:** UI, API, frontend, search, startup seeding, surah/ayah re-import, ayah-text reading/mutation, `quran_words` changes, ruku, manzil, audio, and any reuse/rerun of the Feature 002 foundation importer.

---

*Planning artifact only — no code, migration, DB change, or commit was made. Companion: `quran-metadata-inventory-gap-analysis-report.md` (same folder).*
