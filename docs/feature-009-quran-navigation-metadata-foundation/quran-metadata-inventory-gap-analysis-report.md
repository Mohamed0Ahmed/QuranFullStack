# Quran Metadata Inventory & Gap Analysis Report

**Scope:** Inspection and planning only. No code, migrations, source edits, Spec Kit runs, DB updates, or resource moves were performed. This report is the sole artifact.

**Date:** 2026-06-16
**Metadata source folder:** `/projects/Dashboard/resources/metadata` (QUL / Tarteel origin; local + gitignored, *outside* `App/`)
**Existing project root:** `/projects/Dashboard/App`

---

## 1. Verdict

**C — A new dedicated Quran metadata foundation feature is warranted, but narrowly scoped.**

Of the six metadata datasets present, **two are already fully imported and represented** in the database (`surah-names` → `quran_surahs`, `ayahs` → `quran_ayahs`) and must **not** be re-imported. The remaining **four — `juz` (30), `hizb` (60), `rub` (240), `sajda` (15)** — are present in resources, are **not** represented anywhere in the Backend (no entity, no table, no migration), and were **explicitly deferred** by Feature 002 as "a later navigation layer."

Recommended next feature: **"Quran Navigation Metadata Foundation"** covering only `juz`, `hizb`, `rub`, `sajda` (~345 records total). Everything else is already covered.

---

## 2. Executive Summary

| Question | Answer |
|---|---|
| Is the metadata folder valid and complete? | Yes — 6 datasets, all valid JSON, all record counts match expected (per its own audit reports, independently confirmed by sampling). |
| Is anything already in the DB? | Yes — `surah-names` and `ayahs` metadata are 100% represented in `quran_surahs` / `quran_ayahs`, with extra fields the source lacks. |
| What is genuinely missing? | `juz`, `hizb`, `rub`, `sajda` — no entities, tables, or migrations exist. |
| Was the gap intentional? | Yes — Feature 002 plan: *"no `juz`/`hizb`/`rub` columns (data exists but is a later navigation layer)."* |
| Is navigation (page↔ayah) already covered? | Yes — `quran_ayahs.page_from/page_to`, `quran_mushaf_pages` (first/last surah+ayah), `quran_mushaf_lines`. |
| Data-safety flags? | `ayahs/.../quran-metadata-ayah.json` carries the full Uthmani `text` — this duplicates `quran_ayahs.text_uthmani` and must be ignored on import. Reference ayahs by `verse_key` only. |
| Migration needed if we proceed? | Yes — new tables (and optionally nullable division columns on `quran_ayahs`). Use EF tooling; do not hand-write. |
| Re-run the foundation importer? | No — write a **separate** navigation-metadata importer; the Feature 002 foundation importer is complete and locked. |

---

## 3. Metadata Folder Inventory

Folder layout: each dataset has `original/` (the real data), plus empty `report/`, `samples/` (and a top-level `client-showcase/`) placeholder folders. A top-level `report/` holds audit/extraction summaries; `scripts/` holds the audit tool. **Only the six `original/*.json` files are data.**

### 3.1 Data files (raw source)

| Relative path | Type | Records | Key fields | Purpose | Class | Domain category |
|---|---|---|---|---|---|---|
| `surah-names/original/quran-metadata-surah-name.json` | JSON object (keyed `"1".."114"`) | 114 | `id, name, name_simple, name_arabic, revelation_order, revelation_place, verses_count, bismillah_pre` | Surah identity/metadata | Raw source | Surah metadata |
| `ayahs/original/quran-metadata-ayah.json` | JSON object (keyed `"1".."6236"`) | 6236 | `id, surah_number, ayah_number, verse_key, words_count, text` | Ayah-level metadata (**incl. full Uthmani text**) | Raw source (text field is duplicate of DB) | Ayah metadata / Quran-core text |
| `juz/original/quran-metadata-juz.json` | JSON object (keyed `"1".."30"`) | 30 | `juz_number, verses_count, first_verse_key, last_verse_key, verse_mapping` | Juz (الجزء) boundaries + per-surah verse ranges | Raw source | Juz/Hizb/Rub navigation |
| `hizb/original/quran-metadata-hizb.json` | JSON object (keyed `"1".."60"`) | 60 | `hizb_number, verses_count, first_verse_key, last_verse_key, verse_mapping` | Hizb (الحزب) boundaries + verse ranges | Raw source | Juz/Hizb/Rub navigation |
| `rub/original/quran-metadata-rub.json` | JSON object (keyed `"1".."240"`) | 240 | `rub_number, verses_count, first_verse_key, last_verse_key, verse_mapping` | Rub / quarter-hizb (الربع) boundaries + verse ranges | Raw source | Juz/Hizb/Rub navigation |
| `sajda/original/quran-metadata-sajda.json` | JSON object (keyed `"1".."15"`) | 15 | `sajdah_number, verse_key, sajdah_type` | Sajda (سجدة) locations + type | Raw source | Sajda metadata |

**Sampled shapes (verbatim from `original/`):**

```jsonc
// surah-names "1"
{ "id":1, "name":"Al-Fātiĥah", "name_simple":"Al-Fatihah", "name_arabic":"الفاتحة",
  "revelation_order":5, "revelation_place":"makkah", "verses_count":7, "bismillah_pre":false }

// ayahs "1"  — NOTE: `text` duplicates quran_ayahs.text_uthmani
{ "id":1, "surah_number":1, "ayah_number":1, "verse_key":"1:1", "words_count":4,
  "text":"بِسۡمِ ٱللَّهِ ٱلرَّحۡمَٰنِ ٱلرَّحِيمِ ١" }

// juz "1"  (hizb/rub are identical shape with *_number)
{ "juz_number":1, "verses_count":148, "first_verse_key":"1:1", "last_verse_key":"2:141",
  "verse_mapping": { "1":"1-7", "2":"1-141" } }

// sajda "1"  — sajdah_type ∈ {optional×11, required×4}
{ "sajdah_number":1, "verse_key":"7:206", "sajdah_type":"optional" }
```

### 3.2 Derived / report / tooling artifacts (not import data)

| Path | Type | Purpose | Class |
|---|---|---|---|
| `README.md` | Markdown | Folder intent; states Ruku/Manzil intentionally excluded for now | Doc |
| `report/metadata-json-structure-audit-report.md` + `.json` | Report | Schema/field/count audit; "seeding readiness: yes" | Report |
| `report/metadata-resources-summary.md`, `report/metadata-client-summary.md` | Report | Per-dataset counts (114/6236/30/60/240/15) | Report |
| `report/metadata-zip-extraction-report.md` + `.json` | Report | Provenance: 6 zips extracted, all valid | Report |
| `scripts/audit-metadata-json-structures.mjs` | Node script | The auditor that produced the above | Tooling |
| `*/report/`, `*/samples/`, `client-showcase/` | Empty dirs | Reserved placeholders (no files) | — |

---

## 4. Already Covered by Existing Features

Feature 002 created `quran_surahs`, `quran_ayahs`, `quran_mushaf_pages`, `quran_mushaf_lines`, `quran_words` (foundation migration `20260608095952_QuranFoundationSchema`). Verified imported counts: **114 surahs / 6236 ayahs / 604 pages / 9046 lines / 83668 words.**

### 4.1 `surah-names` → `quran_surahs` — **fully covered**

| Source field | DB column (`quran_surahs`) | Note |
|---|---|---|
| `id` | `surah_number` (PK) | identical |
| `name` (e.g. "Al-Fātiĥah") | `name_transliteration` | covered |
| `name_simple` | `name_simple` | covered |
| `name_arabic` | `name_arabic` (unique index) | covered |
| `revelation_order` | `revelation_order` | covered |
| `revelation_place` (`makkah`/`madinah`) | `revelation_place` (enum `Makkah`/`Madinah`) | covered |
| `verses_count` | `verses_count` | covered |
| `bismillah_pre` | `bismillah_pre` | covered |

Every source field maps to an existing column. **Nothing to import.**

### 4.2 `ayahs` (metadata) → `quran_ayahs` — **fully covered + superset**

| Source field | DB column (`quran_ayahs`) | Note |
|---|---|---|
| `id` | `id` (PK) | identical (1..6236) |
| `surah_number` | `surah_number` | covered |
| `ayah_number` | `ayah_number` | covered |
| `verse_key` | `verse_key` (unique index) | covered |
| `words_count` | `words_count_source` | covered; DB also derives `words_count_real` |
| `text` | `text_uthmani` | **already imported — duplicate; ignore on any future import** |
| — | `page_from`, `page_to` | DB **adds** ayah→page navigation the source lacks |

Cross-check: the source's `words_count` is the same provenance Feature 002 used — the lone 37:130 discrepancy (source 4 / real 3) is already reconciled in the foundation import report. **Nothing to import; the `text` field must never overwrite `text_uthmani`.**

### 4.3 Navigation already provided by Feature 002

| Capability | Where | Status |
|---|---|---|
| page → ayah | `quran_mushaf_pages` (`first/last_surah_number`, `first/last_ayah_number`) | Covered |
| ayah → page | `quran_ayahs.page_from / page_to` | Covered |
| line placement | `quran_mushaf_lines` (page, line, type, first/last word, words_count) | Covered |
| surah names (ar/translit/simple) | `quran_surahs` | Covered |
| ayah counts per surah | `quran_surahs.verses_count` + `quran_ayahs` | Covered |
| revelation place/order | `quran_surahs` | Covered |

### 4.4 Other Quran features (not metadata-related, for completeness)

Words display (003), morphology/i3rab (004/005), mutashabihat (006), tafsir (007), translations (008) all exist as their own tables and are **out of scope** for this metadata question — none of them carry juz/hizb/rub/sajda.

---

## 5. Gap Analysis Table

| Dataset | Records (source) | DB representation | Classification |
|---|---|---|---|
| `surah-names` | 114 | `quran_surahs` (all 8 fields) | **Already imported & represented** |
| `ayahs` (metadata fields) | 6236 | `quran_ayahs` (all fields + page nav) | **Already imported & represented** |
| `ayahs.text` (Uthmani string) | 6236 | `quran_ayahs.text_uthmani` | **Duplicate / derived — do not re-import** |
| `juz` | 30 | none | **Available in resources, not imported** |
| `hizb` | 60 | none | **Available in resources, not imported** |
| `rub` | 240 | none | **Available in resources, not imported** |
| `sajda` | 15 | none | **Available in resources, not imported** |
| `report/*`, `samples/`, `client-showcase/`, `scripts/` | — | n/a | **Report/tooling — not needed for import** |

### Navigation/reading checklist (Task 4)

| Concept | Status | Evidence |
|---|---|---|
| juz | **Missing — needed** | no entity/table; deferred by Feature 002 |
| hizb | **Missing — needed** | no entity/table; deferred by Feature 002 |
| rub | **Missing — needed** | no entity/table; deferred by Feature 002 |
| sajda | **Missing — needed** | no entity/table; source has 15 records |
| ruku | Not available / not needed v1 | excluded by source README ("intentionally not included now") |
| manzil | Not available / not needed v1 | excluded by source README |
| page ranges | Covered | `quran_mushaf_pages`, `quran_ayahs.page_from/page_to` |
| surah names ar/en | Covered | `quran_surahs` |
| ayah counts | Covered | `quran_surahs.verses_count` |
| revelation place/order | Covered | `quran_surahs` |
| page↔ayah navigation | Covered | pages first/last + ayah page_from/page_to |
| line placement | Covered | `quran_mushaf_lines` |

---

## 6. Recommended Next Feature Scope

**Option C: a new dedicated feature — "Quran Navigation Metadata Foundation."**

Why C (not B "small enhancement to Feature 002", not A "nothing needed"):
- The missing data is four cohesive **new bounded concepts** (juz/hizb/rub/sajda), naturally **new tables**, not a couple of columns bolted onto existing entities — so it exceeds "small enhancement."
- Feature 002 explicitly scoped these out as a deliberate "later navigation layer," signalling a follow-up feature is the intended path.
- `surah-names` and `ayahs` are already covered, so a broad "metadata foundation" would mostly re-import existing data — to be avoided.

**In scope:** import `juz` (30), `hizb` (60), `rub` (240), `sajda` (15) and expose ayah→division navigation.
**Explicitly excluded from import:** `surah-names`, `ayahs` (both already represented), and the `ayahs.text` field.

Import strategy: a **new, separate importer** under the existing Quran import infrastructure pattern (mirroring how tafsir/translations/mutashabihat each got their own `Ef*ImportWriter` + `*Sql` + `*ValidationRunner`). Do **not** extend or re-run the Feature 002 foundation importer. Reference ayahs by `verse_key` (unique) — never copy ayah text. Requires a migration (new tables; see open question on optional ayah columns).

---

## 7. Proposed v1 Data Model (if a feature proceeds)

Naming follows existing convention (`quran_*`, snake_case columns, smallint keys).

### Header tables (one per division type)

```
quran_juzs
  juz_number       smallint  PK (1..30)
  verses_count     smallint
  first_verse_key  text  FK-by-value → quran_ayahs.verse_key
  last_verse_key   text  FK-by-value → quran_ayahs.verse_key

quran_hizbs
  hizb_number      smallint  PK (1..60)
  juz_number       smallint  (optional FK → quran_juzs)   -- derivable
  verses_count     smallint
  first_verse_key  text
  last_verse_key   text

quran_rubs
  rub_number       smallint  PK (1..240)
  hizb_number      smallint  (optional FK → quran_hizbs)  -- derivable
  verses_count     smallint
  first_verse_key  text
  last_verse_key   text

quran_sajdas
  sajdah_number    smallint  PK (1..15)
  verse_key        text  FK-by-value → quran_ayahs.verse_key (unique)
  sajdah_type      enum { Required, Optional }  -- stored as "required"/"optional"
```

### Ayah → division navigation (decision point — see §10)

`verse_mapping` (`{ "surah": "from-to" }`) must be turned into something the reader can query as "which juz/hizb/rub is ayah X in?" Two viable approaches:

- **Recommended (simplest for the reader): denormalized nullable columns on `quran_ayahs`** — add `juz_number`, `hizb_number`, `rub_number`. Populated by an `UPDATE … FROM` join derived from `verse_mapping` during the metadata import (no ayah re-import, no text touched). Gives O(1) ayah→division lookup, the exact need for Mushaf Reader / Ayah Details.
- **Alternative (more normalized): a mapping child table** `quran_division_ayah_ranges(division_type, division_number, surah_number, ayah_from, ayah_to)`. Keeps `quran_ayahs` untouched; ayah→division becomes a small range query.

Both are defensible; pick one in planning. `verse_mapping` should be parsed at import and **not** stored verbatim as JSON in v1 (it is derivable and not query-friendly).

---

## 8. Proposed Validation Checks

Hard checks (fail the import):
- Record counts exact: juz **30**, hizb **60**, rub **240**, sajda **15**.
- No duplicate `*_number`; numbers form a contiguous 1..N sequence.
- Every `first_verse_key` / `last_verse_key` / sajda `verse_key` **exists** in `quran_ayahs.verse_key` and matches `^\d+:\d+$`.
- Per division type, the union of all `verse_mapping` ranges covers **all 6236 ayahs exactly once** (no gaps, no overlaps); `Σ verses_count = 6236` for juz, for hizb, and for rub independently.
- Containment: each hizb falls within exactly one juz; each rub within exactly one hizb (if hierarchy columns are stored).
- `sajdah_type ∈ {required, optional}` (4 required / 11 optional in source).
- If ayah division columns are added: every ayah gets non-null `juz_number`/`hizb_number`/`rub_number`; each value is in valid range.

Warning checks (report, don't fail):
- `verses_count` consistency vs the range arithmetic in `verse_mapping`.

Safety checks:
- The `ayahs.text` field is **never read** by this importer.
- No write touches `quran_ayahs.text_uthmani`, `quran_surahs`, or any word table.

---

## 9. Out of Scope

- Re-importing `surah-names` or `ayahs` (already represented) — and never the `ayahs.text` field.
- **Ruku** and **Manzil** (absent from source; README marks them "intentionally not included now").
- Audio / recitation / timing metadata (none present in this folder).
- Any rewrite/normalization of Quran text, word segmentation, or page recomputation.
- Frontend Mushaf Reader / Surah Details / Ayah Details UI work (this report is data-layer planning only).
- Conflating the **rub-el-hizb word marks (199, ۞)** seen in Feature 004 morphology notes with **rub division boundaries (240)** — different concepts; only the 240 boundaries are in scope.

---

## 10. Risks / Open Questions

1. **Source provenance & staging.** These files live at `/projects/Dashboard/resources/metadata` — *outside* `App/` and gitignored. Workspace convention requires importers to read a **staged** package under `App/resources/import-sources/<name>/`. Before any import feature, the juz/hizb/rub/sajda JSON should be staged/canonicalized there. (Inspection-only now — not done.)
2. **`verse_mapping` representation** (denormalized ayah columns vs mapping child table vs JSON) — must be decided in planning. Recommendation: denormalized columns on `quran_ayahs`.
3. **Sajda as table vs ayah flag.** A `quran_sajdas` table preserves `sajdah_number` ordering + type faithfully; an `is_sajda`/`sajda_type` column on `quran_ayahs` is simpler for the reader. Recommendation: dedicated table for source fidelity.
4. **Touching `quran_ayahs`.** Adding division columns modifies a Feature-002 table; harmless via additive migration + post-import `UPDATE`, but it is a cross-feature edit to flag.
5. **Join key stability.** Use `verse_key` (unique, validated by the `VerseKey` value object) as the canonical link; do not assume the metadata `id` and `quran_ayahs.id` stay aligned (they currently both run 1..6236, but `verse_key` is the safe contract).
6. **Migration discipline.** New tables (and optional columns) require an EF-tooling-generated migration; per Backend rules, do not hand-write migrations and do not run `database update` without explicit request.
7. **Hierarchy denormalization.** Storing `juz_number` on hizb and `hizb_number` on rub is convenient but redundant (derivable). Decide whether to store or compute.

---

## 11. Suggested Next Step

If the team wants to proceed: open a brainstorming/spec pass for a **"Quran Navigation Metadata Foundation"** feature scoped to `juz` + `hizb` + `rub` + `sajda` only, with the §10 open questions (esp. #2 verse_mapping representation and #3 sajda shape) resolved first. Pre-work: stage the four `original/*.json` files into `App/resources/import-sources/quran-navigation-metadata/`. No code, migration, or DB change should happen until that scope and the data-model decisions are locked.

*(This report is informational. No commit was made and nothing outside this file was modified.)*
