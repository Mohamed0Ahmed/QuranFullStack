# Feature 008 — Quran Translations Foundation — Planning Report

> **Type:** Long-form pre-Spec-Kit planning report (planning/docs only).
> **Date:** 2026-06-15
> **Companions (same folder):**
> - `translation-source-curation-report.md` — verified source inventory (data facts).
> - `feature-008-decisions-addendum.md` — locked decisions D1–D14 and recomputed counts.
>
> **Hard constraints honored:** planning/docs only; no backend code, no migrations, no Spec Kit run,
> no Backend source/test/contract changes.

---

## 1. Executive Summary

Feature 008 is a **Backend data-foundation feature** that imports **ayah-level Quran translations**
(simple + with-footnotes) from a frozen local source package into two new tables, with strict
validation and audit reports. It mirrors the proven Feature 007 (Tafsir Foundation) pipeline, with a
**simpler 2-table model** because translations are strictly one text per ayah (no ranged blocks).

| Item | Value |
|---|---|
| Scope | Backend import + 2 tables + validation + reports. No UI/API/search/seed/permissions/WBW. |
| Included | Ayah-level `simple` + `with_footnotes` (keyed `surah:ayah`, value `{ "t": ... }`). |
| Excluded | All 11 word-by-word; 6 empty-text files; 2 unattributed near-dup copies. |
| Approved sources (final) | **167** (`simple` 129 / `with_footnotes` 38). |
| Languages covered | **83**. |
| Ayah mappings | **1,041,412** (167 × 6,236). |
| Tables | `quran_translation_sources`, `quran_translation_ayah_entries`. |
| Empty-text policy | **Hard exclude** (`TR-NO-EMPTY-TEXT` hard). |
| Markup policy | Preserve `[[…]]` + embedded HTML **exactly**; no footnote parsing. |
| Package | **Built & validated** at `App/resources/import-sources/quran-translations/` (852 checks passed). |
| **Readiness** | **`READY_FOR_SPEC_KIT`** (O1–O5 resolved; final package frozen). |
| Risk | **Low**. |

---

## 2. Source Package Assessment

### 2.1 Package shape (final, already built and validated)

The final package already exists at `App/resources/import-sources/quran-translations/`:

```
App/resources/import-sources/quran-translations/
├── README.md                         # final package description, scope, exclusions, provenance warning
├── manifest.json                     # frozen file/hash/source contract
├── source-display-metadata.json      # REQUIRED display metadata contract (status: final, 167 records)
├── source-display-metadata.review.json  # retained pre-final review overlay
├── package-report.md                 # counts, approved/excluded summaries, validation, warnings
└── sources/
    ├── <languageCode>-<translatorSlug>.json        # simple variant
    └── <languageCode>-<translatorSlug>.fn.json     # with_footnotes variant
```

**Two required contracts.** The importer must read **both** `manifest.json` (file/hash/source set)
and `source-display-metadata.json` (display labels), and **fail** if the display metadata is missing,
invalid, incomplete (≠167 records or any empty `displayNameEn`/`displayNameAr`), or not aligned with
the manifest `sourceKey` set.

**User-facing v1 selection model:** `languageNameAr` / `languageNameEn` + `translationType` +
`displayNameAr` / `displayNameEn` + translation text. `displayNameAr` / `displayNameEn` are **required**
(import-blocking). `translatorNameAr` / `translatorNameEn` are **optional, non-blocking, and not
user-facing**; `needsReview` is informational only. Display names never contain
`unattributed` / `unknown` / `غير منسوبة` / `غير معروف`. No license/provenance/source-attribution
columns are added to the v1 DB import.

Mirrors `resources/import-sources/quran-tafsirs/` (Feature 007).

### 2.2 Source identification & naming (D10)

- `source_key` = `<languageCode>-<translatorSlug>`, e.g. `en-yusufali`, `ur-junagarri`.
- **Language prefix is mandatory** because publisher slugs repeat across languages
  (`dar-al-salam-center` → 4 languages; `montada-islamic-foundation` → 2;
  `translation-pioneers-center` → 3; `rowad-translation-center` → 2).
- **Footnote variant marker:** `.fn` infix when a translator/language has both a simple and a
  footnotes edition (e.g. Hausa Abubakar Gumi): `ha-abubakar-gumi.json` + `ha-abubakar-gumi.fn.json`.
  The manifest `translation_type` is authoritative; the filename marker is a human convenience.
- `package_file` = `sources/<source_key>[.fn].json`; `source_file_original` records the raw path.

### 2.3 Approved vs excluded sources (recomputed under locked policy)

| Bucket | Count | Notes |
|---|---|---|
| Ayah-level inspected | 175 | 139 simple + 36 with-footnotes |
| **APPROVED_FOR_V1** | **167** | simple 129 / with_footnotes 38 (incl. 3 reclassified) — **final, package built** |
| EXCLUDE — word-by-word | 11 | word-level; future feature |
| EXCLUDE — empty text (hard, D4) | 6 | albanian-truncated(1955), kannada(66), en-maarif(5), ganda(2), dutch(1), urdu-sayyid(1) |
| EXCLUDE — unattributed near-dup (D11) | 2 | `ko-unknown`, `sq-unknown` |
| **Total excluded** | **19** | 167 + 19 = 186 ✓ |
| Kept both (O1/O2 resolved) | 2 pairs | hausa pair, malayalam pair |

Reclassified (kept, type corrected to `with_footnotes`, D7): `divehi/ml-shaikh-aboobakr`,
`russian/ru-abu-adel`, `russian/ru-ministry-of-awqaf`.

**The Final Package Curation pass has been run** and the package built at
`App/resources/import-sources/quran-translations/`. Exact count (167), per-type split (129/38),
language count (83), exclusions (19), and per-file `sha256`/size are now frozen in `manifest.json`
and verified byte-identical to the raw sources (852 validation checks passed).

### 2.4 License & provenance warning (D12)

License/provenance is **unknown** for all sources. Acceptable for internal import curation only —
**not** publish-ready. 10 files are explicitly unattributed (`-unknown-`); 8 of these are distinct and
kept **with a provenance warning**; the other 2 (`ko-unknown`, `sq-unknown`) are dropped as
unattributed duplicates. License/provenance remain manifest/report audit data only, not v1 DB
columns; a manifest-level and report-level warning is mandatory.

---

## 3. Proposed Database Model

### 3.1 Recommendation (D8): **two tables**

Translations are strictly **one text per (source, ayah)** — there is no ranged/leader-ayah grouping
like tafsir, so the tafsir-style middle `quran_translation_entries` table is **not created**.

```
quran_translation_sources (≈167)
  └── quran_translation_ayah_entries (one row per source + ayah; ≈1,041,412)
        ↳ ayah_id → quran_ayahs.id
```

### 3.2 `quran_translation_sources`

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | int PK | NO | |
| `source_key` | text | NO | unique, `<lang>-<translator>` |
| `language_code` | text | NO | e.g. `en`, `ur`, `dv` |
| `language_name_en` | text | NO | |
| `language_name_ar` | text | NO | |
| `native_name` | text | YES | |
| `direction` | text | NO | `ltr`/`rtl` — **per source** (e.g. Roman-Urdu `ltr` vs Urdu `rtl`) |
| `translation_type` | text | NO | `simple` / `with_footnotes` |
| `display_name_en` | text | NO | required app-facing selector |
| `display_name_ar` | text | NO | required app-facing selector |
| `translator_key` | text | YES | inferred from filename |
| `translator_name_en` | text | YES | |
| `translator_name_ar` | text | YES | |
| `contains_inline_footnotes` | bool | NO | `[[…]]` present |
| `contains_html_markup` | bool | NO | embedded HTML present |
| `content_coverage_count` | int | NO | 6,236 |

Indexes: unique `source_key`; index `language_code`; index `(language_code, translation_type)`.

Source file paths, package file paths, hashes, file sizes, license/provenance values, and other
manifest metadata remain part of `manifest.json`, `source-display-metadata.json`, and import reports
for validation/audit only; they are not persisted as v1 DB columns.

### 3.3 `quran_translation_ayah_entries`

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | bigint PK | NO | |
| `source_id` | int | NO | FK → `quran_translation_sources.id` |
| `ayah_id` | int | NO | FK → `quran_ayahs.id` (resolve by verse key) |
| `verse_key` | text | YES | optional convenience copy |
| `text` | text | NO | exact source `t`, incl. inline `[[…]]` / HTML (D5) |

Indexes: unique `(source_id, ayah_id)`; index `(ayah_id, source_id)`.
Expected rows ≈ 167 × 6,236 = **1,041,412**.

### 3.4 Explicit model non-goals (v1)

- **No** `quran_translation_entries` middle table (D8).
- **No** separate languages/contributors catalog table (D9 — denormalized).
- **No** separate footnotes table; footnotes stay inline in `text` (D5).
- **No** source/hash/license/provenance/package-integrity columns in v1 DB tables; manifest/report
  audit is the persistence boundary for those fields.
- **No** import-run history table; reports are the audit record.
- **No** API / read-model / search tables.
- Tables store translation text only — **never** copied Arabic ayah text (`TR-NO-QURAN-TEXT-COPY`).

---

## 4. Import Pipeline Design

### 4.1 CLI verb

A backend data-tool verb (mirroring tafsir), e.g. `import-translations`, that reads the frozen
package and writes the two tables. Not an API; not run at startup.

### 4.2 Flow

1. Resolve package root; assert shape (`README.md`, `manifest.json`, `package-report.md`, `sources/`).
2. Load manifest; assert it is the final import manifest.
3. Verify `sources/` file set exactly matches manifest; verify each `sha256` + size.
4. For each approved source: parse JSON; assert object root with exactly 6,236 ayah keys; assert each
   value `{ "t": <non-empty string> }`; resolve every verse key to `quran_ayahs`.
5. Build source rows (with denormalized app-facing metadata, `translation_type`, direction, display
   names, optional translator names, and content flags) and one ayah-entry row per (source, ayah),
   text byte-equal to source.
6. Bulk write in FK-safe order inside one transaction.
7. Run hard checks inside the transaction; re-verify package unchanged.
8. Write JSON + Markdown reports; accept the run **only if** all hard checks pass **and** reports are
   written; otherwise roll back.

### 4.3 Safe re-run / force behavior (`TR-RERUN-GUARD`)

- Re-run **refuses** if translation data already present, unless `--force`.
- `--force` re-validates the package (shape/hash/coverage/no-empty) **before** replacing, and replaces
  atomically (transactional) — never partial.

### 4.4 Transaction & rollback (`TR-ROLLBACK-ON-FAIL`)

All writes + hard checks run in a single transaction. Any hard-check failure (or missing reports)
rolls back the entire run; no partial import is ever committed.

---

## 5. Validation Rules

### 5.1 Hard checks (run inside the transaction)

| ID | Requirement |
|---|---|
| `TR-PACKAGE-SHAPE` | Package has `README.md`, `manifest.json`, `package-report.md`, `sources/`. |
| `TR-MANIFEST-FINAL` | Manifest flagged final import manifest. |
| `TR-SOURCE-COUNT` | Approved source count equals manifest (e.g. 167 — frozen by curation pass). |
| `TR-TYPE-COUNTS` | `simple` count and `with_footnotes` count equal manifest (e.g. 129 / 38). |
| `TR-EXCLUDED-COUNT` | Excluded count equals manifest (e.g. 19). |
| `TR-SOURCE-SET` | `sources/` files exactly match the manifest approved set. |
| `TR-SOURCE-HASH` | Every file `sha256` + size matches manifest. |
| `TR-NO-EXCLUDED-SOURCES` | Excluded / word-by-word sources are never importable or persisted. |
| `TR-JSON-SHAPE` | Each source root is an object; every value is `{ "t": string }`. |
| `TR-COVERAGE-COUNT` | Every approved source has the exact 6,236 verse-key set (no missing/extra). |
| `TR-NO-EMPTY-TEXT` | **(hard, D4)** No `t` is empty, null, missing, or non-string. |
| `TR-AYAH-KEYS-RESOLVE` | Every verse key resolves to `quran_ayahs`. |
| `TR-NO-DUPLICATE-AYAH-ENTRY` | No duplicate `(source, ayah)`. |
| `TR-TEXT-UNCHANGED` | Stored text byte-equal to source (markup preserved, D5). |
| `TR-NO-QURAN-TEXT-COPY` | Tables store translation text, not copied Arabic ayah text. |
| `TR-POSTCOPY-SOURCE-ROWS` | Persisted source rows = approved count. |
| `TR-POSTCOPY-AYAH-MAPPINGS` | Persisted mappings = approved × 6,236. |
| `TR-SOURCE-UNCHANGED` | Source files still match manifest at acceptance. |
| `TR-REPORT-WRITTEN` | Required JSON + Markdown reports written before acceptance. |
| `TR-ROLLBACK-ON-FAIL` | Any hard-check failure rolls back the whole run. |
| `TR-RERUN-GUARD` | Re-run refuses without `--force`; `--force` re-validates before replacing. |

### 5.2 Warnings

| ID | Requirement |
|---|---|
| `TR-PROVENANCE-WARNING` | License/provenance unknown for all sources (and 8 kept `-unknown-` files). Not publish-ready. |

### 5.3 Informational

| ID | Requirement |
|---|---|
| `TR-INLINE-MARKUP` | Inline `[[…]]` and embedded HTML preserved exactly; recorded via `contains_inline_footnotes` / `contains_html_markup`. |
| `TR-LANGUAGE-COVERAGE` | Source count by language / direction / type. |
| `TR-RECLASSIFIED` | Sources reclassified simple→with_footnotes by content (3). |

---

## 6. Report Design

### 6.1 JSON report

Per-source results (key, language, type, direction, coverage, empty-count=0, sha256, size, pass/fail
per gate), totals (approved, per-type split, excluded with reasons, languages), warnings, run outcome,
timestamps. Machine-readable audit record.

### 6.2 Markdown report

Verdict; scope; input package paths; counts (by language / type / approved-vs-excluded); approved
sources summary; excluded sources summary with reasons (WBW / empty-text / unattributed-dup);
reclassified list; validation checks; warnings (provenance, HTML markup); final confirmation. Lives
under `Backend/report/feature-008-quran-translations-foundation/` per workspace conventions.

---

## 7. Scope Boundaries

**In scope:** import-source package (curated separately), 2 tables, importer CLI verb, validation
gates, transactional load, JSON + Markdown reports.

**Out of scope (locked, D1/D3):** UI, API endpoints, search indexing, startup seeding,
permissions/access, word-by-word import, footnote parsing/sanitization, separate languages/footnotes
tables, import-run history table.

**Risks**

| Risk | Severity | Mitigation |
|---|---|---|
| Hard no-empty-text drops a language (Ganda) and any future single-source language with 1 empty ayah | Low | Accept for v1 (O3); document; revisit with better sources later. |
| O1/O2 unresolved → final count floats 165–167 | Low | Curation pass freezes the number; gates read from manifest, not hard-coded here. |
| Same translator across languages / both type variants → key collisions | Low | `<lang>-` prefix + `.fn` marker (D10) guarantee unique keys. |
| Embedded HTML in 18 footnote files (incl. anchor links) | Low | Preserve verbatim (D5); flag via `contains_html_markup`; sanitization is a later UI concern. |
| Unknown provenance | Medium (publishing only) | Internal-only; warning metadata + report; never publish-ready (D12). |
| Large payload (~279 MiB, ~1.04 M rows) | Low | Bulk insert in FK-safe order, single transaction (same pattern as tafsir's 523k mappings). |

---

## 8. Spec Kit Readiness

### 8.1 Recommended feature title

**Quran Translations Foundation** (Feature 008).

### 8.2 Proposed spec name

`008-quran-translations-foundation`.

### 8.3 User stories suitable for `/speckit.specify`

- As a data curator, I import the approved ayah-level translations so the dashboard DB holds complete,
  verified translations per source.
- As a maintainer, I rely on hard validation so no incomplete/empty/duplicate/word-by-word source is
  ever persisted.
- As a maintainer, I get JSON + Markdown reports so every import is auditable and reproducible.
- As a maintainer, I can safely re-run with `--force` so a corrected package replaces the old import
  atomically.

### 8.4 Final Package Curation step — DONE

The deterministic pass has been run: applied D2–D4/D7/D11; resolved O1/O2 (keep both); derived
`source_key`/`package_file`/direction/flags; computed `sha256` + size; wrote `README.md` +
`manifest.json` + `package-report.md` + 167 byte-identical files under `sources/` at
`App/resources/import-sources/quran-translations/`. This froze `TR-SOURCE-COUNT` (167),
`TR-TYPE-COUNTS` (129/38), and `TR-EXCLUDED-COUNT` (19). 852 post-build validation checks passed.

### 8.5 Open decisions — all resolved

O1 hausa pair → keep both; O2 malayalam pair → keep both; O3 Ganda loss → accepted; O4 codes →
`fil`/`tl` separate, kurdish `ku`, shared codes aligned with Feature 007; O5 marker → `.fn.json`.
See the decisions addendum §7.

### 8.6 Readiness verdict

**`READY_FOR_SPEC_KIT`.** All structural decisions are locked (D1–D14, 2-table model, hard
no-empty-text, preserve-markup, denormalized metadata), O1–O5 are resolved, and the final
import-source package is built and validated (167 sources, 129/38 split, 83 languages, 19 excluded,
all copies byte-identical to source). Nothing remains before `/speckit.specify`.
