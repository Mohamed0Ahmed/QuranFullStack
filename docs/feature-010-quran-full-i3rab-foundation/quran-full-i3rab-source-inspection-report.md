# Feature 010 — Quran Full I‘rab Foundation: Source Inspection Report

**Scope:** Read-only inspection of the staged i‘rab source folder. No source files were
modified, no migrations created, no backend code written, no imports run, no database
changes, and no Spec Kit artifacts produced.

**Source folder inspected:** `/home/mohamed/Desktop/projects/Dashboard/resources/i3rab-quran`

**Date:** 2026-06-16

**Verdict:** `NEEDS_SMALL_IMPLEMENTATION_PLAN`

---

## 1. Executive Summary

The folder contains **four independent classical i‘rab books**, each delivered as a single
minified JSON file. Every file is structurally clean, complete, and internally consistent:

- **Grain:** All four are **ayah-level**, keyed by `verse_key` (`surah:ayah`).
- **Content type:** **Free-text scholarly i‘rab commentary** stored as **HTML** strings.
  There is **no** word-level, segment-level, or structured grammatical-metadata grain.
- **Grouping:** Three of the four files group consecutive ayahs into one i‘rab block. The
  block's text is stored once under a "leader" ayah; member ayahs hold a **string pointer**
  to the leader's `verse_key`. Block membership is also listed explicitly in an `ayah_keys`
  array on the leader record.
- **Completeness:** Each file's keys are **exactly the canonical 6,236 ayahs** (surahs
  1–114). Blocks **partition** all 6,236 ayahs with **zero gaps and zero overlaps**.
- **Integrity:** **Zero** broken pointers, **zero** `ayah_keys` mismatches, **zero** empty
  texts, **zero** malformed keys.
- **Join:** Joins safely to `quran_ayahs` by `verse_key`. No word-level join is possible or
  needed.

Critically, this exact data shape was **already imported successfully** for the **Tafsir
feature** (`quran_tafsir_sources` / `quran_tafsir_entries` / `quran_tafsir_ayah_entries`).
The Tafsir domain entities (`TafsirEntry`, `TafsirAyahEntry`) already model leader/member,
`CoveredAyahKeys`, `SourceShape`, `SourceValueKind`, `IsGroupLeader`, and `TextHash` — i.e.
the precise structure these i‘rab files exhibit. **This feature is effectively a clone of
the Tafsir pipeline with a different content payload.**

The data is **import-grade**. It is **not** flagged `READY_FOR_DIRECT_IMPLEMENTATION` only
because two non-data items must be resolved first: (a) the source is **not yet staged** as a
canonical import package with provenance/licensing (workspace convention requires this), and
(b) there are a few well-bounded design decisions (multi-book source dimension, HTML policy,
naming vs. the existing simple-i‘rab `quran_i3rab_rules` table). Those are exactly what a
**small implementation plan** covers; full Spec Kit ceremony is not warranted because there
is a single, well-understood grain and a proven in-repo precedent.

---

## 2. File Names, Formats, and Sizes

All four files live in the `original/` subfolder. The sibling `report/` and `samples/`
folders exist but are **empty**.

| File | Book (transliteration) | Size | Encoding | Format |
|------|------------------------|------|----------|--------|
| `al-i-rab-al-muyassar.json` | الإعراب الميسّر (Al-I‘rāb al-Muyassar) | 9,830,274 B (≈9.4 MB) | UTF-8 | minified JSON, single line |
| `al-jadwal-fi-i-rab-al-quran.json` | الجدول في إعراب القرآن (Al-Jadwal fī I‘rāb al-Qur’ān) | 11,991,115 B (≈11.4 MB) | UTF-8 | minified JSON, single line |
| `alrab-al-quran-li-da-as.json` | إعراب القرآن للدعّاس (I‘rāb al-Qur’ān li-al-Da‘‘ās) | 8,953,219 B (≈8.5 MB) | UTF-8 | minified JSON, single line |
| `i-rab-al-quran-li-al-darwish.json` | إعراب القرآن للدرويش (I‘rāb al-Qur’ān li-al-Darwīsh) | 11,786,168 B (≈11.2 MB) | UTF-8 | minified JSON |

Total payload ≈ 40.6 MB across four files. All valid UTF-8, parse cleanly with a strict
JSON parser.

> **Note on "4 files":** the task brief referenced "the 4 files." They are the four JSON
> books above, all under `original/`. There are no other data files in the tree.

---

## 3. Top-Level JSON Structure

Every file is a **single JSON object (dictionary)** whose keys are `verse_key` strings and
whose values describe the i‘rab for that ayah (or a pointer to its block leader).

```
{
  "1:1": { "text": "<div class=ar lang=ar>…i‘rab HTML…</div>", "ayah_keys": ["1:1","1:2", …] },
  "1:2": "1:1",                  // string pointer → leader "1:1" (grouped files only)
  …
  "114:6": { "text": "…" }
}
```

- **Top-level type:** object/dict (all four files).
- **Top-level key count:** **6,236** in every file (one entry per canonical ayah).
- **First keys:** `1:1, 1:2, 1:3, 1:4, 1:5, 1:6, 1:7, 2:1, …` (Fātiḥa then Baqara).

### Value shapes

A value is one of:

1. **Leader dict with grouping** — `{"text": <html>, "ayah_keys": [verse_key, …]}`.
   The i‘rab text covers the listed block of ayahs; `ayah_keys` always includes the leader
   itself first.
2. **Standalone dict** — `{"text": <html>}`. A single-ayah i‘rab with no grouping.
3. **String pointer** — `"<leader_verse_key>"`. A member ayah of a block; its actual text
   lives under the named leader.

Value-shape distribution per file (`dict` = entries 1 or 2; `str` = pointers):

| File | dict | of which: `text` only | of which: `text`+`ayah_keys` (leaders) | str (pointers) |
|------|-----:|----------------------:|---------------------------------------:|---------------:|
| al-i-rab-al-muyassar | 6,236 | 6,236 | 0 | 0 |
| al-jadwal-fi-i-rab-al-quran | 3,257 | 2,039 | 1,218 | 2,979 |
| alrab-al-quran-li-da-as | 3,633 | 2,455 | 1,178 | 2,603 |
| i-rab-al-quran-li-al-darwish | 1,387 | 227 | 1,160 | 4,849 |

Observations:

- **`al-i-rab-al-muyassar` has no grouping at all** — every ayah is its own entry
  (cleanest, 1:1 mapping).
- The other three are **heavily grouped**, especially **Darwīsh** (only 1,387 distinct
  i‘rab blocks cover all 6,236 ayahs; 4,849 ayahs are pointers).
- The only object keys ever present are `text` and `ayah_keys`. **There is no structured
  grammatical metadata** (no POS, case, root, governing-word, or segment fields).

---

## 4. Keying / Join Analysis

**Keyed by:** `verse_key` in `surah:ayah` form (e.g. `2:255`). This is the same identity used
by `quran_ayahs` and by the existing Tafsir / Translation features.

- **Not** keyed by word location, word order, or segment index.
- Block grouping is expressed in two redundant, mutually consistent ways: the `ayah_keys`
  array on the leader, and the string pointers on members. Both were validated to agree.

**Can it join safely to existing Quran foundation tables?** **Yes — by `verse_key` to
`quran_ayahs`.** No word-level join applies (the data has no word grain).

Validation performed against the canonical 6,236-ayah set (per-surah counts):

| Check | muyassar | jadwal | da‘as | darwish |
|-------|:--:|:--:|:--:|:--:|
| Surah range | 1–114 | 1–114 | 1–114 | 1–114 |
| Distinct surahs | 114 | 114 | 114 | 114 |
| Malformed keys | 0 | 0 | 0 | 0 |
| Missing vs canonical 6,236 | 0 | 0 | 0 | 0 |
| Extra/unknown keys | 0 | 0 | 0 | 0 |
| String pointers → missing key | 0 | 0 | 0 | 0 |
| String pointers → non-dict target | 0 | 0 | 0 | 0 |
| `ayah_keys` member → leader mismatch | 0 | 0 | 0 | 0 |
| Ayahs covered by blocks (should be 6,236) | 6,236 | 6,236 | 6,236 | 6,236 |
| Block coverage overlaps | 0 | 0 | 0 | 0 |
| Uncovered ayahs | 0 | 0 | 0 | 0 |
| Pointer keys missing from any block | 0 | 0 | 0 | 0 |

**Every source independently partitions the full Quran with perfect referential integrity.**

---

## 5. Grain Determination (the brief's five questions)

| Grain | Present? | Notes |
|-------|:--:|-------|
| **1. Ayah level** | **YES (primary)** | All entries are keyed and anchored at the ayah. |
| **2. Word level** | **NO** | No word location, no word order, no per-word records. |
| **3. Segment level** | **NO** | No morphological segments; not comparable to the simple-i‘rab segment labels. |
| **4. Mixed level** | **PARTIAL — only multi-ayah block grouping** | 3 of 4 files group consecutive ayahs into one i‘rab block. This is *coarser-than-ayah* grouping, still ayah-anchored — **not** a word/segment mix. |
| **5. Free-text commentary level** | **YES** | The payload is HTML scholarly prose; this is the actual content type. |

**Conclusion: ayah-level free-text i‘rab commentary, with optional multi-ayah block
grouping. Not word-level, not segment-level.**

---

## 6. Coverage

- **All 6,236 ayahs:** **Yes**, in every file (verified: 0 missing, 0 extra, blocks
  partition the corpus exactly).
- **All 77,432 readable words:** **Not applicable** — the data is not word-level, so there
  is nothing to check at word grain. (The brief asks "if word-level"; it is not.)

---

## 7. Quran Text Duplication

The i‘rab prose **embeds inline Quran quotations** — this is intrinsic to the i‘rab genre
(the scholar quotes the word/phrase, then parses it). It is **not** a separate redundant
ayah-text column; it is woven into the commentary and, in two of the four books, explicitly
marked with CSS classes.

| File | Quran quotation markup | dict entries containing a quotation |
|------|------------------------|-------------------------------------|
| al-i-rab-al-muyassar | `<span class="qpc-hafs">﴿…﴾</span>` inside `<span class="hlt">` | 6,236 / 6,236 |
| alrab-al-quran-li-da-as | `qpc-hafs` + `hlt` spans, `﴿…﴾` brackets | 3,514 / 3,633 |
| al-jadwal-fi-i-rab-al-quran | `hlt` highlights + `<b>`; **no** `qpc-hafs`, no `﴿﴾` | 0 (no qpc-hafs markup) |
| i-rab-al-quran-li-al-darwish | `<h3>` headings + `<p>`; **no** spans, **no** `qpc-hafs` | 0 (no qpc-hafs markup) |

- `qpc-hafs` = the QPC (King Fahd Complex) Ḥafṣ Uthmani glyph set; `hlt` = highlight; `ar` =
  Arabic/RTL wrapper. `﴿ ﴾` are ornate ayah brackets.
- **Recommendation:** Do **not** add a separate Quran-text column. Keep quotations inside the
  i‘rab text (they are part of the scholarly artifact) and record per-source whether quotation
  markup exists. This matches the project rule "do not duplicate Quran ayah text unless the
  source forces it and you clearly mark it as source text" — here the source *intrinsically*
  contains it, already class-marked in two books.

---

## 8. I‘rab Text vs. Grammatical Metadata

**I‘rab text only.** The payload is free-form HTML scholarly prose. There are **no**
structured grammatical fields (no POS tags, case markers, governing words, segment indices,
or root/lemma references). Any structured morphology/segment data already lives in the
existing morphology and simple-i‘rab tables; this feature contributes **full narrative
i‘rab**, not structured metadata.

HTML markup profile (tags actually used):

| File | Tags | Classes |
|------|------|---------|
| muyassar | `span` (107,035), `p` (20,373), `div` (6,236) | `hlt`, `qpc-hafs`, `ar` |
| jadwal | `p` (52,566), `span` (29,774), `b` (7,083), `div` (3,257), `h3` (11) | `hlt`, `ar` |
| da‘as | `span` (118,598), `p` (5,201), `div` (3,633) | `hlt`, `qpc-hafs`, `ar` |
| darwish | `p` (19,263), `h3` (3,587), `div` (1,387) | `ar` |

The markup vocabulary is small and bounded: `div, p, span, b, h3` with classes
`ar, hlt, qpc-hafs`. It is **heterogeneous across books**, so HTML handling must be
source-aware (see §13).

Text length (per dict entry, after stripping tags):

| File | plaintext min / median / max (chars) |
|------|--------------------------------------|
| muyassar | 22 / 435 / 5,990 |
| jadwal | 38 / 1,452 / 15,445 |
| da‘as | 10 / 613 / 4,727 |
| darwish | 470 / 3,734 / 25,219 |

Spot-checked shortest entries are genuine i‘rab (e.g. `صفة جنتان.`,
`حرفان مقطّعان لا محلّ لهما من الإعراب.`) — no placeholders or junk found.

---

## 9. Suitability for Direct Backend Import

**Structurally: yes.** The data is clean, complete, UTF-8, referentially intact, and uses
the project's standard `verse_key` identity. The leader/pointer/`ayah_keys` shape is already
handled end-to-end by the **existing Tafsir importer and schema**:

- `TafsirEntry`: `LeaderAyahId`, `TafsirText`, `CoveredAyahCount`, `CoveredAyahKeys`,
  `SourceShape`, `TextHash`.
- `TafsirAyahEntry` (junction): `AyahId`, `VerseKey`, `SourceValueKind` (dict vs.
  string-pointer), `SourceLeaderVerseKey`, `IsGroupLeader`, `SortOrder`.

These fields map 1:1 onto what these i‘rab files contain. So a direct import is low-risk and
the modeling is *de-risked by precedent*.

**Blockers to "direct" (hence the small-plan verdict):**

1. **Not staged as a canonical package.** The folder is `resources/i3rab-quran/`, not
   `resources/import-sources/<feature>/`, and carries **no `manifest.json`, `README.md`, or
   `package-report.md`**. Workspace conventions require importers to consume a staged package
   with provenance. This must be created before import (see §11, §12).
2. **Four books, no source dimension yet** — a `*_sources` table + four seed rows are needed.
3. **Naming collision risk** — a `quran_i3rab_rules` table already exists from Feature 005
   (simple, word/segment-level i‘rab). The new ayah-level book i‘rab must use a clearly
   distinct name (see §10).

---

## 10. Recommended v1 Database Model

Mirror the proven **Tafsir** three-table model exactly, renamed for full i‘rab and
**deliberately distinct** from the existing `quran_i3rab_rules` (Feature 005). Suggested
names use a `full_i3rab` qualifier to avoid any confusion with simple i‘rab:

### `quran_full_i3rab_sources` (dimension — 4 rows)
| Column | Notes |
|--------|-------|
| `id` (PK) | small int |
| `slug` | stable key, e.g. `muyassar`, `jadwal`, `daas`, `darwish` |
| `title_ar` | الإعراب الميسّر, الجدول في إعراب القرآن, إعراب القرآن للدعّاس, إعراب القرآن للدرويش |
| `author_ar` | author/compiler (to be sourced — see provenance gap) |
| `markup_format` | enum/string, e.g. `html` |
| `has_quran_quotation_markup` | bool (true for muyassar/da‘as, false for jadwal/darwish) |
| `quotation_markup_kind` | e.g. `qpc-hafs` / `none` |
| `license` / `source_url` / `attribution` | **currently unknown — must be filled** |
| `sort_order` | display ordering |

### `quran_full_i3rab_entries` (block leader text — one row per i‘rab block)
| Column | Notes |
|--------|-------|
| `id` (PK) | bigint |
| `source_id` (FK → sources) | |
| `leader_ayah_id` / `leader_verse_key` (FK → `quran_ayahs`) | the leader ayah |
| `text` | raw i‘rab HTML |
| `covered_ayah_count` | from `ayah_keys` length (1 if standalone) |
| `covered_ayah_keys` | denormalized list (audit/debug), as Tafsir does |
| `source_shape` | `standalone` / `grouped-leader` (preserve original shape) |
| `text_hash` | for idempotency + cross-book dedup detection |
| Unique: `(source_id, leader_verse_key)` | |

### `quran_full_i3rab_ayah_entries` (junction — one row per (source, ayah))
| Column | Notes |
|--------|-------|
| `id` (PK) | bigint |
| `source_id` (FK) | |
| `ayah_id` / `verse_key` (FK → `quran_ayahs`) | the covered ayah |
| `entry_id` (FK → entries) | resolves to the block text |
| `source_value_kind` | `dict` vs `string-pointer` (preserve original) |
| `source_leader_verse_key` | leader the original pointer/`ayah_keys` referenced |
| `is_group_leader` | bool |
| `sort_order` | order within block |
| Unique: `(source_id, verse_key)` | guarantees exactly one i‘rab per ayah per book |

**Why leader + junction (not one row per ayah with duplicated text):** Darwīsh would
duplicate one block's text across up to dozens of ayahs (4,849 pointer ayahs over 1,387
blocks). The junction model stores each text once and still answers "give me the i‘rab for
verse_key X in book Y" with a single join. This is exactly the Tafsir design.

**No separate Quran-text table/column** (quotations stay inside `text`; flag via
`has_quran_quotation_markup`).

---

## 11. Recommended Import Approach

1. **Stage a canonical package first** (prerequisite, currently missing):
   `resources/import-sources/quran-full-i3rab/` containing the four JSON files plus
   `manifest.json`, `README.md`, and `package-report.md` recording per-file counts, hashes,
   shapes, **and provenance/license** (§12). Importers must read from here, not from
   `resources/i3rab-quran/original/`.
2. **Seed the source dimension** (4 rows) with slug/title/markup flags.
3. **Reuse / clone the Tafsir importer.** Per file:
   - For each **dict** value → insert one `entries` row (leader text); record
     `source_shape`, `covered_ayah_count`, `covered_ayah_keys`, `text_hash`.
   - For each ayah in the block (`ayah_keys`, or the key itself if standalone) → insert one
     `ayah_entries` row pointing to that entry; set `is_group_leader`, `source_value_kind`,
     `source_leader_verse_key`, `sort_order`.
   - For each **string-pointer** value → insert one `ayah_entries` row pointing to the
     leader's entry; `source_value_kind = string-pointer`.
4. **Idempotent upsert** keyed by `(source_id, leader_verse_key)` for entries and
   `(source_id, verse_key)` for ayah_entries; safe to re-run.
5. **Store HTML as-is at import**; sanitize/allowlist on render (§13). Optionally compute a
   derived plaintext column for search (not required for v1).
6. **Import each book independently** so partial/failed runs don't block other books.

---

## 12. Provenance / Licensing Concerns

**This is the most significant gap.**

- **No license, README, manifest, or attribution file exists anywhere in the tree** — only
  the four raw JSON files. The `report/` and `samples/` folders are empty.
- These are **named classical i‘rab works** with known authors/compilers (e.g. al-Jadwal by
  Maḥmūd Ṣāfī; the Da‘‘ās and Darwīsh i‘rāb works). The JSON carries **no** author, edition,
  publisher, source URL, or license metadata.
- The QPC-Ḥafṣ quotation glyphs imply the Quran text uses the King Fahd Complex encoding;
  its usage terms should be acknowledged.

**Required before import:** capture author, edition, upstream source, and licensing/usage
terms for each of the four books into the staged package's `manifest.json` / `README.md`.
Do not import until provenance is recorded, per workspace data-safety conventions.

---

## 13. Recommended Validation Checks

**Pre-import (source-level) — all already passing in this inspection:**

- Each file parses as JSON; top-level is an object with exactly 6,236 keys.
- Every key matches `^\d+:\d+$` and is a canonical `verse_key` (surahs 1–114, correct
  per-surah ayah counts); no missing/extra.
- Every value is a dict (`text`, optional `ayah_keys`) **or** a string pointer.
- Every string pointer resolves to an existing **dict** key.
- Every `ayah_keys` member resolves back to its leader; leader is first in its `ayah_keys`.
- Blocks **partition** all 6,236 ayahs (no gaps, no overlaps); every pointer ayah belongs to
  exactly one block.
- No empty/blank `text`.
- HTML tag/class vocabulary stays within the observed allowlist (`div, p, span, b, h3`;
  classes `ar, hlt, qpc-hafs`); flag any out-of-allowlist markup.

**Post-import (DB-level):**

- `COUNT(*)` of `ayah_entries` per source = 6,236; `COUNT(DISTINCT verse_key)` per source =
  6,236 (exactly one i‘rab per ayah per book).
- Every `ayah_entries.verse_key` / `entry`'s `leader_verse_key` FK resolves in `quran_ayahs`.
- `SUM(covered_ayah_count)` over entries per source = 6,236.
- Each `ayah_entries` row references an `entry` of the **same** `source_id`.
- `text_hash` non-null; optionally report intra/cross-book duplicate texts.
- Re-running the importer changes nothing (idempotency).

---

## 14. Data Quality Concerns

- **Heterogeneous markup across books** (qpc-hafs vs. hlt+b vs. h3-only). Rendering and any
  sanitization must be **source-aware**; a single naive strip would lose quotation marking in
  muyassar/da‘as and headings in darwish/jadwal. *(Design item, not a defect.)*
- **HTML must be sanitized on render** to prevent injection and to enforce the allowlist;
  store raw, sanitize at the boundary.
- **Embedded Quran text** must be preserved and treated as source-marked quotation, not
  duplicated into a separate column (§7). Keep Quranic content source-safe in any tests
  (construct minimal real fixtures; do not dump large ayah text into test files).
- **Provenance/license absent** (§12) — the only true blocker; everything else is clean.
- No empty texts, no broken pointers, no key anomalies were found.

---

## 15. Spec Kit vs. Direct Implementation

**This feature does not require full Spec Kit.** Justification:

- **Single, well-understood grain** (ayah-level free-text), validated end-to-end.
- **Proven precedent in-repo:** the Tafsir feature already implements the identical
  leader/member/`ayah_keys` shape with `quran_tafsir_sources/entries/ayah_entries` and a
  working importer. This is a structural clone with a different payload.
- **Clean, complete, referentially-intact source** with trivial `verse_key` join.

It is **more than a one-shot direct import**, though, because of: the four-book source
dimension, source-aware HTML policy, the naming separation from existing
`quran_i3rab_rules`, and the **provenance/staging gap** that must be closed first.

→ **`NEEDS_SMALL_IMPLEMENTATION_PLAN`** is the right altitude: a short, focused plan
(stage package + provenance → 3-table schema mirroring Tafsir → clone importer → validation),
not full Spec Kit specification/clarification/contracts.

---

## Implementation Recommendation

**Verdict: `NEEDS_SMALL_IMPLEMENTATION_PLAN`.**

Proceed with a lightweight implementation plan that reuses the Tafsir pipeline. Concretely:

1. **Close the provenance gap (blocking).** Gather author/edition/source/license for all four
   books and the QPC-Ḥafṣ glyph usage terms.
2. **Stage a canonical package** at `resources/import-sources/quran-full-i3rab/` with the four
   JSON files + `manifest.json` + `README.md` + `package-report.md` (counts, hashes, shapes,
   provenance). Importers consume only this package.
3. **Add a 3-table schema** mirroring Tafsir, named to avoid collision with the existing
   simple-i‘rab `quran_i3rab_rules`: `quran_full_i3rab_sources`,
   `quran_full_i3rab_entries`, `quran_full_i3rab_ayah_entries` (model in §10).
4. **Seed 4 source rows**; **clone the Tafsir importer** (§11), one independent run per book,
   idempotent, HTML stored raw.
5. **Apply the validation suite** in §13 (pre- and post-import).
6. **Sanitize HTML on render** (source-aware), keep embedded Quran quotations as source-marked
   text, and keep test fixtures source-safe.

Do **not** add a separate Quran-text column, do **not** attempt word- or segment-level
modeling (the data has no such grain), and do **not** start implementation until the staged
package with provenance exists.
