# Feature 008 — Quran Translations Foundation — Translation Source Curation Report

> **Status:** Planning / source-analysis only. No backend code, no migrations, no Spec Kit,
> no source copy into `App/resources/import-sources`, no source mutation.
> **Source root inspected:** `/projects/Dashboard/resources/translations`
> **Date:** 2026-06-15
> **Method:** Read-only filesystem inspection plus independent Python validation over every file.
> Pre-existing audit reports under `resources/translations/report/` were read **and
> independently re-verified** against the current on-disk state (several were stale; see notes).

---

## 0. Executive Summary

| Metric | Value |
|---|---|
| Languages with at least one resource | 85 of 87 folders (2 empty: `chechen`, `georgian`) |
| Languages with **ayah-level** resources (v1 candidates) | **84** |
| Total resource files | **186** JSON |
| — Simple ayah-level | 139 (≈ 210.6 MiB) |
| — With-footnotes ayah-level | 36 (≈ 80.6 MiB) |
| — Word-by-word (word-level) | 11 (≈ 29.2 MiB) |
| Total bytes | 336,015,945 (≈ 320 MiB) |
| Ayah-level subtotal (simple + footnotes) | **175** files (≈ 291 MiB) |
| Invalid JSON | 0 |
| Ayah-level files with full 6,236 coverage | 175 of 175 (key set complete) |
| Ayah-level files with **empty-string text** entries | 6 (one severe: albanian truncated, 1,955 empty) |
| Exact duplicate files | 0 |
| Near-duplicate / same-translation pairs | 4 (korean, albanian, hausa, malayalam) |
| Weak-provenance files (`-unknown-`) | 10 |

**Readiness verdict: `READY_WITH_DECISIONS`.** Risk level: **Low–Medium**. The data is clean,
single-schema per type, and well-covered; remaining work is a short list of curation policy
decisions (Section 9), not data repair.

---

## 1. Folder and Source Inventory

### 1.1 Top-level structure of `resources/translations`

```
translations/
├── README.md                  # organization + alias decisions (auxiliary)
├── languages/                 # 87 language folders — THE import candidates live here
│   └── <language>/
│       ├── notes.md                       # generic per-language template (auxiliary)
│       ├── simple/original/*.json         # ayah-level, no inline notes
│       ├── with-footnotes/original/*.json # ayah-level, inline footnotes
│       └── word-by-word/original/*.json   # word-level
├── client-showcase/           # translations-resources.html (auxiliary, client demo)
├── report/                    # ~20 prior audit .md/.json/.tsv files (auxiliary)
├── samples/                   # translations-inventory-sample.json (auxiliary)
└── scripts/                   # audit-translation-json-structures.mjs (auxiliary)
```

All resource JSON lives under `languages/<lang>/<type>/original/`. Every JSON file is correctly
placed under an `original/` folder (placement check: 0 stray files).

### 1.2 Counts

- **Languages:** 87 folders. **85** contain files; **2 are empty** (`chechen`, `georgian` — their
  only resources were incomplete and were deleted during a prior cleanup; see Section 3.4).
- **Total translation resources:** **186** JSON files.

| Type | Files | Bytes | Level | v1? |
|---|---|---|---|---|
| `simple/` (ayah, no notes) | 139 | 220,873,408 | ayah | candidate |
| `with-footnotes/` (ayah, inline notes) | 36 | 84,514,131 | ayah | candidate |
| `word-by-word/` | 11 | 30,628,406 | word | **defer** |
| **Total** | **186** | **336,015,945** | | |

- **"Other/unknown" type:** none. Every file falls cleanly into one of the three folders, and each
  type has exactly one schema signature (verified, not just per the prior audit).

### 1.3 File formats

- **JSON only** for all 186 resources. No SQLite, no NDJSON, no chunked/sharded files, no embedded
  binary. No `.zip` remain (an older alias report referenced `.json.zip` for divehi/uyghur/sinhala;
  those have since been extracted — current on-disk state is plain `.json`, independently confirmed).
- **Auxiliary (non-resource) files:** `notes.md` ×87 (generic template, no per-resource metadata),
  `report/` audit artifacts (`.md`, `.json`, `.tsv`), `client-showcase/*.html`, `samples/*.json`,
  `scripts/*.mjs`, top-level `README.md`.

### 1.4 Import candidates vs auxiliary

| Category | Items | Disposition |
|---|---|---|
| **v1 import candidates** | 175 ayah-level JSON (139 simple + 36 footnotes) | curate → package |
| **Deferred (later feature)** | 11 word-by-word JSON | exclude from v1 |
| **Auxiliary / non-v1** | `notes.md`, `report/`, `client-showcase/`, `samples/`, `scripts/`, `README.md` | do not import |

---

## 2. Data Shape Analysis

### 2.1 Simple ayah-level (`simple/original/*.json`) — 139 files

- **Root shape:** JSON **object** (map). One schema across all 139 files (`root=object | record has key "t"`).
- **Keying:** by **verse key** `"<surah>:<ayah>"`, e.g. `"1:1"`, `"4:106"`. Not by id, not by array index.
- **Value:** object `{ "t": "<translation text>" }`. The text field name is **uniformly `t`**.
- **Coverage:** all 139 hold **exactly 6,236 keys**; key **set** is complete (no missing, no extra,
  no malformed keys) — independently verified against a canonical 6,236 verse-key set.
- **Text:** plain text for the great majority. **Exceptions:** 3 "simple" files actually contain inline
  `[[...]]` footnote markup (misclassified — see 3.2). 0 simple files contain HTML.
- **Direction:** mostly LTR; RTL for Arabic-script languages. **Direction is per-resource, not
  per-language** (see 2.4).
- **Metadata:** none inside the file. Translator/source must be **inferred from folder + filename**
  (`notes.md` is a generic template; it does not name the translator).

Example (`english/simple/original/quran-en-yusufali-simple.json`):
```json
{ "1:1": { "t": "In the name of Allah, Most Gracious, Most Merciful." }, "1:2": { "t": "..." } }
```

### 2.2 With-footnotes ayah-level (`with-footnotes/original/*.json`) — 36 files

- **Root shape / keying / value / coverage:** **identical** to simple — object keyed by `"S:A"`,
  value `{ "t": ... }`, all 36 files have exactly 6,236 complete keys.
- **Footnotes are inline inside `t`**, delimited by **`[[ ... ]]`**. There is **no separate footnotes
  field / array** in any file. All 36 contain `[[...]]` markers.
- **Markup inside footnotes:** **18 of 36** files embed HTML inside the footnote text:
  `<p>…</p>`, `<br />`, `<strong class=h>…</strong>`, and cross-reference anchors
  (`urdu/ur-al-maududi` uses `<a href='/10/31-40' class='no-ref' target='_blank'>`).
- **Structured footnotes:** none — footnotes are text-embedded only.

Example (`english/with-footnotes/original/en-sahih-international-inline-footnotes.json`):
```json
{ "1:1": { "t": "In the name of Allāh,[[Allāh is a proper name…]] the Entirely Merciful…[[Ar-Raḥmān…]]" } }
```

### 2.3 Word-by-word (`word-by-word/original/*.json`) — 11 files

- **Root shape:** JSON **object**. One schema (`root=object | value=string`).
- **Keying:** by **word location** `"<surah>:<ayah>:<word>"`, e.g. `"1:1:1"`. All location keys
  well-formed (0 bad keys).
- **Value:** a **plain string** (the word gloss), e.g. `"In (the) name"`. No `t` wrapper.
- **Coverage:** **word-level, not 6,236**. Record counts vary widely by source:

  | Source | Words | Source | Words |
  |---|---|---|---|
  | tamil | 61,458 | indonesian | 83,664 |
  | ingush | 66,159 | persian | 83,664 |
  | hindi | 70,522 | bengali | 83,664 |
  | turkish | 70,539 | english (wbw) | 83,665 |
  | english (colored-wbw) | 77,429 | urdu | 83,665 (280 empty) |
  | french | 77,429 | | |

  The counts do **not** converge on a single Quran word count; segmentation differs per source
  (even the two English files differ: 77,429 vs 83,665). This is the core reason to defer WBW.
- **Direction:** the gloss language's direction (per resource).

### 2.4 Key cross-cutting shape findings (verified independently)

- **Direction must be stored per resource, not per language.** Counter-examples within one language
  folder: `urdu/maududi-roman-urdu` is **LTR** (Roman/Latin transliteration) while all other Urdu
  files are **RTL**. `kurdish/*` are all Arabic-script **RTL** (Kurmanji here is written in Arabic
  script, not Latin). `divehi` is Thaana **RTL** with embedded Arabic.
- **Ordering is not guaranteed.** Keys are complete as a **set**, but JSON insertion order is **not**
  reliably Mushaf order (e.g. a complete file's last keys land in sūrah 99, not 114). The importer
  must resolve by **verse key**, never by position.
- **No source metadata in the data.** Translator, publisher, language code, direction, license — all
  absent from the files and from `notes.md`. Must be inferred/curated into the manifest.

---

## 3. Coverage and Quality

### 3.1 Languages and resource counts (per language)

Per-language matrix `[simple, with-footnotes, word-by-word]` for the 85 non-empty languages
(empty: `chechen`, `georgian`). Languages with the most resources:

| Language | simple | footnotes | wbw | total |
|---|---|---|---|---|
| english | 14 | 6 | 2 | 22 |
| urdu | 5 | 3 | 1 | 9 |
| russian | 6 | 0 | 0 | 6 |
| turkish | 5 | 0 | 1 | 6 |
| bengali | 5 | 0 | 1 | 6 |
| albanian | 3 | 1 | 0 | 4 |
| chinese | 4 | 0 | 0 | 4 |
| french | 0 | 3 | 1 | 4 |
| indonesian | 0 | 3 | 1 | 4 |
| spanish | 1 | 3 | 0 | 4 |
| tamil | 3 | 0 | 1 | 4 |
| uzbek | 1 | 3 | 0 | 4 |

The remaining 73 languages have 1–3 resources each. **63 languages are single-resource** (mostly one
`simple` or one `with-footnotes`), so excluding a single file in those languages removes the language
from v1 entirely — relevant to the empty-text and dedup decisions below.

Full counts are reproducible from `languages/<lang>/<type>/original/`.

### 3.2 Complete 6,236-coverage (ayah-level)

**All 175 ayah-level files have the complete 6,236 verse-key set** — no missing keys, no extra keys,
no malformed keys, no `null`, no non-string `t`. (Independently verified against a canonical
verse-key set; this is stronger than "record count = 6236".)

### 3.3 Incomplete / suspicious resources — **empty-string text** (NOT caught by prior count-only audits)

Six ayah-level files pass the 6,236 **key** check but contain **empty-string `t`** values:

| File | Type | Empty ayahs | Severity |
|---|---|---|---|
| `albanian/simple/translation-pioneers-center-simple.json` | simple | **1,955** | **Severe — effectively truncated** |
| `kannada/with-footnotes/kannada-quran-inline-footnotes.json` | footnotes | 66 | Moderate |
| `english/simple/en-maarif-ul-quran-simple.json` | simple | 5 | Minor |
| `ganda/simple/african-development-foundation-simple.json` | simple | 2 | Minor (only Ganda resource) |
| `dutch/simple/nl-abdalsalaam-simple.json` | simple | 1 | Minor |
| `urdu/simple/urdu-sayyid-qatab-simple.json` | simple | 1 | Minor |

- **Albanian `translation-pioneers-center`** is the critical case: **72 sūrahs are 100% empty**
  (sūrah 43 onward) plus sūrah 42 partially empty from 42:10. It is a **truncated** translation that
  *looks* complete by key count. Albanian still has 2 other complete simple files (`quran-al-ahmeti`,
  `sq-unknown`) and 1 footnotes file, so the language is not lost if this file is excluded.
- The other 5 carry only 1–66 placeholder-empty ayahs (publisher left certain ayahs untranslated).
  These are **warning-level**, not structural.

### 3.4 Resources removed earlier (provenance of the 2 empty folders)

A prior cleanup (`report/incomplete-translation-resources-cleanup-report.md`) deleted 6
below-6,236 files. Two of those were the **only** resource for their language, leaving empty folders:

| Language | Deleted file | Records | Effect |
|---|---|---|---|
| chechen | `chechen-translation-inline-footnotes.json` | 6,030 | folder now empty |
| georgian | `georgian-translation-simple.json` | 899 | folder now empty |
| english | `dr-waleed-bleyhesh-omary-simple.json` | 2,250 | english still rich |
| korean | `korean-translation-rowwad-...-simple.json` | 3,691 | korean still has 2 |
| german | `german-translation-rowwad-...-simple.json` | 4,473 | german still has 2 |
| kurdish | `ku-burhan-muhammad-simple.json` | 6,235 | kurdish still has 3 |

No word-by-word files were deleted; no complete 6,236 files were deleted; contents were not modified.

### 3.5 Duplicate / near-duplicate resources

- **Exact duplicates: none.** No two ayah-level files share identical text (incl. markup).
- **No "footnotes-stripped == simple" collisions:** stripping `[[...]]` from each footnotes file
  never reproduces an existing simple file — i.e. simple and footnotes editions are genuinely
  distinct translations, so there is no simple/footnotes overlap to dedupe.
- **Near-duplicate / same-translation pairs** (fraction of identical non-empty ayahs):

  | Pair | Identical-ayah fraction | Interpretation |
  |---|---|---|
  | `korean/hamed-choi-simple` ≈ `korean/ko-unknown-simple` | **0.99** | Same translation; `ko-unknown` is the unattributed copy |
  | `albanian/quran-al-ahmeti-simple` ≈ `albanian/sq-unknown-simple` | **0.95** | Same translation; `sq-unknown` is the unattributed copy |
  | `hausa/quran-ha-abubakar-simple` ≈ `hausa/abubakar-mahmood-jummi-inline-footnotes` | **0.89** | Same translator, two **type variants** (simple vs footnotes) |
  | `malayalam/abdul-hamid-haidar-kanhi-muhammad-simple` ≈ `malayalam/quran-ml-abdul-hameed-simple` | **0.60** | Possibly related editions — review |

  (Pairs in the 0.30–0.45 band — bosnian, chinese, spanish, thai, indonesian — are **distinct**
  translations that merely share short formulaic ayahs; not duplicates.)

### 3.6 Misclassified resources — inline footnotes inside `simple/`

Three files live in `simple/` but contain inline `[[...]]` footnote markup, so they are really
*with-footnotes* content:

- `divehi/simple/ml-shaikh-aboobakr-ibrahim-ali-simple.json`
- `russian/simple/ru-abu-adel-simple.json`
- `russian/simple/ru-ministry-of-awqaf-simple.json`

Decision needed: relabel these as `translation_type = with_footnotes`, or accept folder-based typing.

### 3.7 Malformed / empty / non-string

- Invalid JSON: **0**. Root-not-object: **0**. `null` text: **0**. Non-string text: **0**.
- Empty-string text: only the 6 files in 3.3 (ayah-level) and `urdu` WBW (280 empty, WBW deferred).
- Unusual keys / ordering: key **sets** are correct everywhere; key **order** is not Mushaf order
  (handle by resolving on verse key — see 2.4).

### 3.8 Weak provenance

10 ayah-level files are explicitly unattributed (`-unknown-` / `-unknow-` in filename):
`albanian/sq-unknown`, `azeri/az-unknown`, `bosnian/bs-unknown`, `czech/cs-unknown`,
`divehi/dv-unknow`, `finnish/fi-unknown`, `korean/ko-unknown`, `maranao/mrn-unknown`,
`norwegian/no-unknown`, `tatar/tt-unknow`. Two of these (`sq-unknown`, `ko-unknown`) are also the
near-duplicate copies in 3.5.

---

## 4. Language Normalization

### 4.1 Aliases already resolved (per source `README.md`, confirmed on disk)

| Canonical folder | Aliases folded in | Recommended code |
|---|---|---|
| `divehi` | dhivehi, maldivian | `dv` |
| `uyghur` | uighur | `ug` |
| `sinhala` | sinhalese | `si` |
| `chewa` | chichewa, nyanja | `ny` |

The `chichewa`/`nyanja` alias folders no longer exist (removed during cleanup); only `chewa` remains.
No alias folders currently contain files.

### 4.2 Additional alias / code questions found during inspection (need a decision)

| Folders | Issue | Recommendation |
|---|---|---|
| `filipino` **and** `tagalog` | Both present; Filipino is standardized Tagalog | Keep separate but assign distinct codes `fil` vs `tl`, OR merge — **decision needed** |
| `dari` vs `persian` | Dari = Afghan Persian | Keep separate: `prs` (dari) vs `fa` (persian), as QUL does |
| `kurdish` (mixed) | Folder mixes Kurmanji + Sorani, all Arabic-script here | Either one code `ku`, or split Sorani as `ckb` (Feature 007 tafsir used `ckb`) — **decision needed** |
| `central-khmer` | = Khmer | code `km` |
| `asante` | Asante Twi (Akan) | code `ak` (or `tw`) |
| `bisayan` | Cebuano/Bisaya | code `ceb` |
| `azeri` | Azerbaijani | code `az` |
| `amazigh` | Berber | code `ber` (or `kab`) |

**Recommended canonical scheme:** ISO 639-1 where available, else 639-3; store both `language_code`
and human names (`nameEn`, `nameAr`, `nativeName`) plus `direction` per the Feature 007 manifest
shape. Reuse the codes already chosen in `resources/import-sources/quran-tafsirs/manifest.json` for
languages shared with tafsir (ar, sq, as, az, bn, bs, km, ckb, en, es, fa, ff, fr, hi, id, it, ja,
ky, ml, ps, ru, si, sr, ta, te, th, tl, tr, ug, ur, uz, vi …) so the two features stay consistent.

---

## 5. v1 Scope Recommendation

**Recommended direction (matches the requested preference, and the data supports it):**

- **Include ayah-level only.** Simple **and** with-footnotes are both ayah-level, single-schema,
  fully covered → both in v1.
- **Exclude word-by-word from v1.** WBW is word-level, counts vary per source (61k–84k), keys are
  `S:A:W`, and it will need separate alignment with `quran_words`. Reserve for a later feature.

### 5.1 Group classification

| Group | Files | Class | Reason |
|---|---|---|---|
| Simple ayah-level (clean, complete, distinct) | ~133 | **APPROVED_FOR_V1** | One schema, 6,236 complete, plain text, no dup |
| With-footnotes ayah-level (clean, complete) | ~35 | **APPROVED_FOR_V1** | One schema, 6,236 complete, inline `[[…]]` preserved |
| `albanian/simple/translation-pioneers-center` | 1 | **EXCLUDE_FROM_V1** | Truncated: 1,955 empty ayahs / 72 empty sūrahs |
| `korean/ko-unknown`, `albanian/sq-unknown` | 2 | **NEEDS_REVIEW** | ≥0.95 duplicate of an attributed edition + no provenance |
| `hausa` simple ↔ footnotes pair | 2 | **NEEDS_REVIEW** | Same translation in two type variants — keep which? |
| `malayalam` near-pair (0.60) | 2 | **NEEDS_REVIEW** | Possibly related editions |
| `kannada/with-footnotes/kannada-quran` (66 empty) | 1 | **NEEDS_REVIEW** | Above warning threshold |
| 3 misclassified `simple` w/ inline footnotes (divehi, ru-abu-adel, ru-ministry) | 3 | **NEEDS_REVIEW** | Reclassify as `with_footnotes`? |
| 8 other `-unknown-` files (az, bs, cs, dv, fi, mrn, no, tt) | 8 | **APPROVED_FOR_V1 + provenance warning** | Complete & distinct; provenance unknown |
| Minor-empty files (en-maarif 5, ganda 2, dutch 1, urdu-sayyid 1) | 4 | **APPROVED_FOR_V1 + warning** | 1–5 placeholder empties; acceptable with warning |
| All 11 word-by-word | 11 | **EXCLUDE_FROM_V1** | Word-level; defer to later feature |
| `notes.md`, `report/`, `client-showcase/`, `samples/`, `scripts/`, `README.md` | — | **AUXILIARY_ONLY** | Not resource data |

### 5.2 Count estimate (depends on Section 9 decisions)

- **APPROVED_FOR_V1 (ayah-level):** ≈ **168–172** (175 ayah-level − 1 truncated − the few held for review).
- **NEEDS_REVIEW:** ≈ **8** named resources.
- **EXCLUDE_FROM_V1:** **11** WBW **+ 1** truncated (+ up to 2 dedup losers) = **12–14**.
- **AUXILIARY_ONLY:** all non-resource files (87 `notes.md`, ~20 report files, etc.).

---

## 6. Proposed Import-Source Package (do not create yet)

Mirror the Feature 007 tafsir package shape for consistency.

### 6.1 Proposed layout

```
App/resources/import-sources/quran-translations/
├── README.md
├── manifest.json
├── package-report.md
└── sources/
    ├── en-yusufali.json
    ├── en-sahih-international.fn.json
    ├── ur-junagarri.fn.json
    └── … (one file per approved source)
```

### 6.2 Source file naming convention

- `sources/<sourceKey>.json`, where `sourceKey = <languageCode>-<translatorSlug>`.
- **Language prefix is required for uniqueness:** publisher slugs repeat across languages
  (`dar-al-salam-center` → 4 languages; `montada-islamic-foundation` → 2; `translation-pioneers-center`
  → 3; `rowad-translation-center` → 2). The `<lang>-` prefix disambiguates.
- **Encode type when a translator has both variants in the same language** (e.g. Hausa Abubakar Gumi
  exists as simple *and* footnotes). Recommend a `.fn` infix or `-fn` suffix for footnotes
  (`ha-abubakar-gumi.json` vs `ha-abubakar-gumi.fn.json`), with `translationType` also in the manifest.

### 6.3 `manifest.json` structure (per-source fields)

```jsonc
{
  "manifestType": "quran-translation-import-source-package",
  "isFinalImportManifest": true,
  "createdAtUtc": "…",
  "sourceRoot": "sources",
  "sourceCount": 0,
  "licenseWarning": "License/provenance unknown for all sources; internal curation only.",
  "summary": {
    "rawInspectedSources": 186,
    "ayahLevelInspected": 175,
    "approvedSimple": 0, "approvedWithFootnotes": 0,
    "excludedWordByWord": 11, "excludedTruncated": 1,
    "needsReview": 0, "languageCount": 0
  },
  "selectionRules": { "contentCoverageCount": 6236, "resourceKind": "translation", "levels": ["ayah"] },
  "languages": [ { "code": "en", "nameEn": "English", "nameAr": "الإنجليزية", "nativeName": "English", "direction": "ltr" } ],
  "sources": [
    {
      "sourceKey": "en-sahih-international",
      "languageCode": "en", "languageNameEn": "English", "languageNameAr": "الإنجليزية",
      "direction": "ltr",
      "translationType": "with_footnotes",      // simple | with_footnotes
      "displayNameEn": "Saheeh International", "displayNameAr": "صحيح إنترناشونال",
      "translatorKey": "saheeh-international", "translatorNameEn": "Saheeh International",
      "containsInlineFootnotes": true,           // "[[ ]]" present
      "containsHtmlMarkup": true,                // <p>,<br/>,<a>,<strong> inside footnotes
      "contentCoverageCount": 6236,
      "emptyAyahCount": 0,
      "sourceFileOriginal": "languages/english/with-footnotes/original/en-sahih-international-inline-footnotes.json",
      "packageFile": "sources/en-sahih-international.fn.json",
      "sha256": "…", "fileSizeBytes": 0,
      "license": "unknown", "provenance": "unknown"
    }
  ],
  "excludedSourceSummary": [ /* wbw + truncated + dedup losers, with reasons */ ]
}
```

### 6.4 Expected counts (to lock as gates)

- Approved ayah-level source count (final number set after Section 9 decisions; estimate 168–172).
- Split: approved simple count + approved with-footnotes count.
- Language count among approved sources (≈ 84 minus any language fully held for review).
- Excluded: 11 WBW + 1 truncated (+ any dedup/review excludes).

### 6.5 Excluded source list (to enumerate in manifest)

- All 11 word-by-word files (deferred).
- `albanian/simple/translation-pioneers-center` (truncated).
- Any dedup loser / review reject decided in Section 9.

### 6.6 `README.md` contents (package)

Purpose; "final approved import-source package for Feature 008"; exact approved count; that only
ayah-level simple + with-footnotes are included; that WBW is deliberately excluded for a future
feature; that text/footnote markup is preserved **exactly** (no normalization); license/provenance
unknown warning; "packaging artifact only — no backend/migration/importer code".

### 6.7 `package-report.md` contents

Mirror tafsir: (1) Verdict, (2) Scope, (3) Input files, (4) Output package paths, (5) Counts
(by language, by type, approved vs excluded), (6) Approved sources summary, (7) Excluded sources
summary (with reasons), (8) Validation checks, (9) Warnings (provenance, empty-ayah, HTML markup),
(10) Final confirmation.

---

## 7. Proposed Backend Data Model (planning only)

### 7.1 Recommendation: **two tables**, not three

Tafsir needs three tables because tafsir blocks can span ranges of ayahs (a `leader_ayah` + an
`entries` table). **Translations are strictly one text per ayah** — every file maps each verse key to
its own text, with no grouping/ranging. So the middle `quran_translation_entries` pass-through table
adds nothing. Recommend:

- **`quran_translation_sources`** — one row per approved source.
- **`quran_translation_ayah_entries`** — one row per (source, ayah); the read-lookup table.

`quran_translation_entries` (the tafsir-style middle table) is **not needed** for v1. (Only revisit
if footnotes are later split into their own structured table — see 7.3.)

### 7.2 `quran_translation_sources` (denormalized, like tafsir)

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | int PK | NO | |
| `source_key` | text | NO | unique, e.g. `en-sahih-international` |
| `language_code` | text | NO | e.g. `en`, `ur`, `dv` |
| `language_name_en` / `language_name_ar` / `native_name` | text | NO/YES | denormalized |
| `direction` | text | NO | `ltr` / `rtl` — **per source**, not per language |
| `translation_type` | text | NO | `simple` / `with_footnotes` |
| `display_name_en` / `display_name_ar` | text | YES | |
| `translator_key` / `translator_name_*` | text | YES | inferred from filename |
| `contains_inline_footnotes` | bool | NO | |
| `contains_html_markup` | bool | NO | |
| `content_coverage_count` | int | NO | 6236 |
| `empty_ayah_count` | int | NO | curation visibility |
| `source_file_original` | text | NO | provenance path |
| `sha256` / `file_size_bytes` | text/bigint | NO | integrity |
| `license` / `provenance` | text | NO | default `unknown` |
| `manifest_metadata` | jsonb | YES | snapshot of unmodeled manifest fields |

Indexes: unique `source_key`; index `language_code`; index `(language_code, translation_type)`.

### 7.3 `quran_translation_ayah_entries`

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | bigint PK | NO | |
| `source_id` | int | NO | FK → `quran_translation_sources.id` |
| `ayah_id` | int | NO | FK → `quran_ayahs.id` (resolve by verse key) |
| `text` | text | NO | exact source text incl. inline `[[…]]` footnotes/HTML |
| `verse_key` | text | YES | optional convenience copy |

Indexes: unique `(source_id, ayah_id)`; index `(ayah_id, source_id)`.
Expected row count ≈ approved_sources × 6,236 (e.g. 170 × 6,236 ≈ 1,060,120).

### 7.4 Specific design questions (answers)

- **Separate languages table in v1?** No — denormalize into the source row + manifest, matching
  Feature 007. (With 84 languages a catalog table is more defensible than for tafsir's 33; still
  recommend denormalize for v1 consistency and add a catalog later if the UI needs it.)
- **Separate footnote table in v1?** **No.** The source data has **no structured footnotes** — only
  inline `[[…]]` (sometimes with HTML). Preserve text **exactly**; do not parse footnotes out in v1.
  A structured footnote table would require lossy parsing of inline markup → defer.
- **`translation_type`?** **Yes** — `simple` / `with_footnotes`, set per source.
- **Direction / language / translator metadata?** Yes — all on the source row; **direction per source**.
- **License/provenance fields?** Yes — default `unknown` with a manifest-level warning.
- **Checksums / source manifest?** Yes — `sha256` + `file_size_bytes` per source; final manifest is
  the contract.

---

## 8. Validation Gates to Include Later in Spec Kit

Mirror the `TAFSIR-*` invariants as `TRANSLATION-*`:

| ID | Severity | Requirement |
|---|---|---|
| `TR-PACKAGE-SHAPE` | hard | Package has `README.md`, `manifest.json`, `package-report.md`, `sources/`. |
| `TR-MANIFEST-FINAL` | hard | Manifest flagged final import manifest. |
| `TR-SOURCE-COUNT` | hard | Approved source count equals manifest. |
| `TR-TYPE-COUNTS` | hard | Approved simple count & with-footnotes count equal manifest. |
| `TR-EXCLUDED-COUNT` | hard | Excluded count (WBW + truncated + rejects) equals manifest. |
| `TR-SOURCE-SET` | hard | `sources/` files exactly match manifest approved set. |
| `TR-SOURCE-HASH` | hard | Every file size + sha256 matches manifest. |
| `TR-NO-EXCLUDED-SOURCES` | hard | Excluded/WBW sources never persisted. |
| `TR-JSON-SHAPE` | hard | Each source root is an object with 6,236 ayah keys, value has `t` (string). |
| `TR-COVERAGE-COUNT` | hard | Every approved source has the complete 6,236 key set (no missing/extra). |
| `TR-AYAH-KEYS-RESOLVE` | hard | Every verse key resolves to `quran_ayahs`. |
| `TR-NO-DUPLICATE-AYAH-ENTRY` | hard | No duplicate `(source, ayah)`. |
| `TR-TEXT-UNCHANGED` | hard | Stored text byte-equal to source (markup preserved). |
| `TR-NO-QURAN-TEXT-COPY` | hard | Tables store translation text, not copied Arabic ayah text. |
| `TR-POSTCOPY-SOURCE-ROWS` | hard | Persisted source rows = approved count. |
| `TR-POSTCOPY-AYAH-MAPPINGS` | hard | Persisted mappings = approved × 6,236. |
| `TR-SOURCE-UNCHANGED` | hard | Source files still match manifest at acceptance. |
| `TR-REPORT-WRITTEN` | hard | Required MD + JSON reports written before run acceptance. |
| `TR-ROLLBACK-ON-FAIL` | hard | Any hard-check failure rolls back the whole run (transactional). |
| `TR-EMPTY-TEXT` | **decision** | Empty-string `t`: hard-fail, OR warning + count (6 known files). See §9. |
| `TR-RERUN-GUARD` | hard | Re-run refuses unless `--force`; `--force` re-validates before replacing. |
| `TR-PROVENANCE-WARNING` | warning | License/provenance unknown for all sources. |
| `TR-INLINE-MARKUP` | info | Inline `[[…]]`/HTML preserved exactly. |
| `TR-LANGUAGE-COVERAGE` | info | Source count by language/direction/type. |

Note: tafsir's `TAFSIR-NO-EMPTY-TEXT` is **hard**. If reused verbatim, the 6 empty-text files
(incl. albanian truncated) **fail** — so either pre-exclude them or downgrade to a warning. This is
the single most important gate decision (§9).

---

## 9. Open Decisions Before `/speckit.specify`

1. **With-footnotes in v1?** Recommended **yes** (ayah-level, single schema). Confirm.
2. **Empty-text policy (the key one).** Choose: (a) hard-fail any empty `t` → must exclude/repair the
   6 files; (b) warning + record `empty_ayah_count`; or (c) hybrid: exclude the truncated albanian
   file, warn on the ≤66-empty rest. **Recommend (c).**
3. **Markup policy.** Preserve inline `[[…]]` and embedded HTML **exactly** (recommended), or
   normalize/strip at import. (18 footnote files contain HTML incl. anchor links.) Recommend preserve.
4. **Word-by-word.** Confirm WBW is a **separate future feature** (recommended — word-level, varying
   counts, needs `quran_words` alignment).
5. **Misclassified simple-with-footnotes (3 files).** Relabel as `with_footnotes`, or keep folder-based
   typing? Recommend relabel via `translation_type` + `contains_inline_footnotes`.
6. **Near-duplicates / dedup policy.** korean `ko-unknown` (0.99) and albanian `sq-unknown` (0.95):
   drop the unattributed copy or keep both? hausa simple↔footnotes (same translation): keep one or
   both type variants? malayalam pair (0.60): keep both? **Recommend** drop `ko-unknown`/`sq-unknown`,
   keep the hausa footnotes variant, keep malayalam pending a closer look.
7. **Unknown-provenance policy.** Allow the 10 `-unknown-` files with a provenance warning
   (recommended), or hold for review?
8. **Language alias / code policy.** Confirm ISO code scheme; decide `filipino`↔`tagalog`
   (separate `fil`/`tl` vs merge) and `kurdish` (`ku` vs split `ckb`). Align codes with the tafsir
   manifest.
9. **Source naming / key policy.** Confirm `sourceKey = <lang>-<translator>` with a `-fn`/`.fn`
   marker for the footnotes variant when a translator has both.
10. **Languages table.** Denormalize (recommended, matches tafsir) or add a catalog table now.

---

## 10. Final Recommendation

- **Recommended v1 scope:** Ayah-level translations only — **simple + with-footnotes**, full 6,236
  coverage, inline markup preserved exactly. **Exclude word-by-word** (future feature).
- **Approved source count estimate:** **≈ 168–172** ayah-level sources (firm number after §9).
- **Excluded source count estimate:** **12–14** (11 WBW + 1 truncated + up to ~2 dedup/review rejects).
- **Needs-review count:** **≈ 8** named resources (§5.1).
- **Risk level:** **Low–Medium.** Data is single-schema per type, fully key-covered, no invalid JSON,
  no exact duplicates. Residual risk is curation policy (empty-text, dedup, aliases) — not data repair.
- **Readiness verdict:** **`READY_WITH_DECISIONS`.** Resolve the §9 decisions (especially #2 empty-text,
  #6 dedup, #8 aliases), lock the approved count, then proceed to `/speckit.specify` and build the
  `quran-translations` import-source package.

---

### Appendix A — Independent verification method

All numbers above were produced by read-only inspection of `/projects/Dashboard/resources/translations`
(filesystem listing + Python JSON parsing of every file), not by trusting the pre-existing
`report/` artifacts. Where the prior reports were stale (e.g. the `.json.zip` alias note) the current
on-disk state was used. No source file was modified, moved, or copied.
