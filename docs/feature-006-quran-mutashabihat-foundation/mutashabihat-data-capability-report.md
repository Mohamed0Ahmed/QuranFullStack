# Mutashabihat Data Capability & Modeling Report — Feature 006

**Project:** المنهج القرآني — Quran Dashboard
**Feature:** 006 — Quran Mutashabihat Foundation (backend data foundation only)
**Scope of this document:** Report-only. No code, no migrations, no source edits, no Spec Kit artifacts.
**Inspected path:** `/projects/Dashboard/resources/mutashabihat`
**Date:** 2026-06-13

All numbers below were re-derived directly from the raw source files in this session (not copied
from the pre-existing inspection report), and every referenced ayah was independently validated
against the canonical Hafs ayah-count table.

---

## 1. Verdict

**READY WITH NOTES.**

The two datasets are valid, internally consistent JSON, every ayah reference resolves to a real
Quran ayah, and the relationship models are well understood. The data can be modeled and imported
into a clean backend foundation.

The "with notes" qualifier reflects items to decide/record before importing — none are data-quality
blockers, but they must be captured as Spec Kit clarifications:

- No **license / provenance** metadata ships with the files (see §10).
- A handful of **minor anomalies** must be handled as non-blocking warnings (coverage > 100, one
  duplicate occurrence, one group whose source key is absent from its own occurrence set, stale
  pre-computed counts).
- The raw files are **not yet staged** under `resources/import-sources/` (per project constraint).
- `phrase_verses.json` is a **derived reverse index**, not an independent source — decide explicitly
  not to store it.

We can proceed to `/speckit.specify` once these notes are acknowledged (see §12).

---

## 2. Folder Inventory

### 2.1 Tree summary (812 KB total)

```
resources/mutashabihat/
├── README.md                                   1.5 KB   doc
├── mutashabihat-ul-quran/
│   ├── original/
│   │   ├── phrases.json                      133.9 KB   SOURCE (truth)
│   │   └── phrase_verses.json                 41.3 KB   SOURCE (derived reverse index)
│   ├── jsonData/                              (empty)    placeholder
│   ├── report/                                (empty)    placeholder
│   └── samples/                               (empty)    placeholder
├── similar-ayahs/
│   ├── original/
│   │   └── matching-ayah.json                373.4 KB   SOURCE (truth)
│   ├── jsonData/                              (empty)    placeholder
│   ├── report/                                (empty)    placeholder
│   └── samples/                               (empty)    placeholder
├── report/
│   ├── mutashabihat-resources-inspection-report.md    5.6 KB   DERIVED report
│   └── mutashabihat-resources-inspection-report.json 127 KB    DERIVED report
├── samples/
│   ├── mutashabihat-resources-samples.md     12.1 KB   DERIVED sample
│   └── mutashabihat-resources-samples.json   49.5 KB   DERIVED sample
├── derived/                                   (empty)    placeholder
├── scripts/                                   (empty)    placeholder
└── client-showcase/                           (empty)    placeholder
```

### 2.2 File-type / role classification

| File | Type | Role | Source of truth? |
|---|---|---|---|
| `mutashabihat-ul-quran/original/phrases.json` | JSON object map | **Original source** | ✅ Yes — Mutashabihat ul Quran |
| `mutashabihat-ul-quran/original/phrase_verses.json` | JSON object map | Original-folder file, but a **derived reverse index** of `phrases.json` | ⚠️ No — fully regenerable |
| `similar-ayahs/original/matching-ayah.json` | JSON object map | **Original source** | ✅ Yes — Similar Ayahs |
| `report/*.md`, `report/*.json` | Markdown / JSON | **Derived** inspection reports | No |
| `samples/*.md`, `samples/*.json` | Markdown / JSON | **Derived** sample extracts (also inject a synthetic `ayahKeys`/`text` field not present in source) | No |
| `derived/`, `scripts/`, `client-showcase/`, per-resource `jsonData/`, `report/`, `samples/` | empty dirs | Placeholders | n/a |

**Conclusion:** Only **three** raw JSON files matter, and only **two** are independent sources of
truth: `phrases.json` and `matching-ayah.json`. Everything else is derived, empty, or documentation.

---

## 3. Dataset Separation

### a) `mutashabihat-ul-quran` — verbatim phrase groups (المتشابهات اللفظية)

A catalogue of **repeated Quranic phrases**. Each record is one phrase that recurs across the
Mushaf; the record lists every ayah where that phrase occurs, with the exact **word-index range**
of the occurrence inside each ayah. This is a **group → many occurrences** model: one phrase, N
locations. It answers *"where else does this exact wording appear?"*

### b) `similar-ayahs` — scored similarity links (آيات متشابهة)

A graph of **ayah-to-ayah similarity edges** produced by partial word matching. Each record is one
**source ayah → target ayah** edge carrying a `score`, a `coverage` percentage, a matched-word
count, and the matched word ranges. This is a **pairwise link** model. It answers *"which ayahs are
the most similar to this one, and how strongly?"*

### Are they semantically different? — **Yes, materially.**

Evidence (re-derived this session):

- Expanding phrase groups into undirected co-membership pairs yields **17,862** pairs; the
  similar-ayahs file yields **2,336** undirected pairs; **only 813** pairs are shared by both.
- **792** ayahs appear in both datasets, but the *relationships* they assert mostly do not coincide.
- Different grain: group-of-N (variable, 2–70) vs. fixed pairs; different attributes (word ranges
  with a representative "source phrase" vs. score/coverage/matched-count).

**Recommendation:** store them in **separate tables / modules**. They are complementary, not
redundant; merging them would lose grain and conflate two distinct notions of "similarity."

---

## 4. Source File Analysis

### 4.1 `mutashabihat-ul-quran/original/phrases.json` — *SOURCE OF TRUTH (groups)*

- **Format:** JSON object; root is a map keyed by **numeric phrase id** (string), e.g. `"50"`.
  Ids are **sparse and non-sequential**, ranging **50 → 16746** (only 814 used). The id is *not* a
  row index — treat it as an opaque external id.
- **Record count:** **814 groups.**
- **Value schema (uniform across all 814 records):**
  ```json
  "50": {
    "surahs": 32,
    "ayahs": 70,
    "count": 71,
    "source": { "key": "2:23", "from": 15, "to": 17 },
    "ayah": {
      "19:48": [[4, 6]],
      "2:23":  [[15, 17]],
      "16:28": [[17, 19], [17, 19]],
      "...":   "..."
    }
  }
  ```
- **Key fields:**
  - `source` — the **representative occurrence** that defines the phrase: `key` (verse_key) plus the
    `from`/`to` word range. `source.key` is one of the ayahs in `ayah` for **813 / 814** groups
    (1 exception, see risks).
  - `ayah` — map of **verse_key → list of `[word_from, word_to]` ranges**. Each entry is one ayah;
    each range is one occurrence of the phrase inside that ayah (an ayah may hold >1 occurrence).
  - `surahs`, `ayahs`, `count` — **pre-computed summary counters** (distinct surahs, distinct
    ayahs, total occurrences). They are **stale/approximate**: they disagree with the actual `ayah`
    map in 46, 55, and 56 groups respectively. **Recompute at import; keep raw values only as
    source metadata.**
- **Ayah reference fields:** `source.key` and every key of the `ayah` map — all `S:A` verse_keys.
- **Text fields:** **None.** No Quran text. Word references are **positional indices only**
  (`[from,to]` over the ayah's words). Word-index upper bound observed: **128** (plausible; longest
  ayah is 2:282). The `samples/*.md` file *adds* an `ayahKeys`/`text` field — that is synthetic and
  **not** in this source.
- **Metadata fields:** `surahs`/`ayahs`/`count` (stale, informational).
- **Distribution:** group size (distinct ayahs) **min 2 / median 2 / max 70**, mean ≈ 4.37 —
  right-skewed (many small groups, a few very large). **No singleton groups.** 2,232 distinct
  verse_keys referenced; 3,558 total `[from,to]` occurrences.
- **Risks:**
  - Pre-computed counters are stale → must recompute.
  - 1 duplicate identical range: group `75`, ayah `16:28` → `[[17,19],[17,19]]` (dedupe).
  - 1 group (`1782`, `source.key = 3:28`) where the source key is **absent** from its own `ayah`
    map (warn; do not drop the group).
  - Sparse opaque ids must be preserved for idempotent re-runs, not used as PKs blindly.

### 4.2 `mutashabihat-ul-quran/original/phrase_verses.json` — *DERIVED reverse index*

- **Format:** JSON object; map keyed by **verse_key**, value = **array of phrase-group ids**.
  Example: `"2:23": [50, 16379]`.
- **Record count:** **2,232 verse_keys**; group ids per ayah range **1 → 7**.
- **Verified relationship to `phrases.json`:** it is an **exact, fully-consistent reverse index** —
  all 814 group ids referenced, all exist, every (group → ayah) pair round-trips, 0 inconsistencies
  in either direction, and the verse_key sets match exactly.
- **Risk / decision:** it is **100% regenerable** from `phrases.json`. **Do not** import it as a
  table; if anything, validate it as a consistency cross-check, then derive the same lookup from an
  index on the occurrences table. Storing it would duplicate truth and risk drift.

### 4.3 `similar-ayahs/original/matching-ayah.json` — *SOURCE OF TRUTH (links)*

- **Format:** JSON object; map keyed by **source verse_key**, value = **array of match items**.
  ```json
  "1:1": [
    { "matched_ayah_key": "27:30", "matched_words_count": 4, "coverage": 50,  "score": 80, "match_words": [[5, 8]] },
    { "matched_ayah_key": "1:3",   "matched_words_count": 2, "coverage": 100, "score": 50, "match_words": [[1, 2]] }
  ]
  ```
- **Record count:** **1,162 source ayahs**, **3,552 directed link items** (items per source 1 → 31).
- **Key fields (uniform schema across all 3,552 items):**
  - `matched_ayah_key` — target verse_key (the source key is the map key).
  - `score` — similarity score, integer **50 → 100** (continuous). The floor of 50 indicates an
    **implicit cut-off threshold**: only matches ≥ 50 were kept.
  - `coverage` — % of source-ayah words covered by the match, **5 → 200** (see risks).
  - `matched_words_count` — **1 → 29**.
  - `match_words` — list of word ranges **on the source ayah**; entries are `[from,to]` (3,877) or
    single-word `[x]` (74); 339 items carry multiple disjoint ranges. Max word index 76.
- **Ayah reference fields:** map key (source) + `matched_ayah_key` (target). 1,644 distinct
  verse_keys; 1,299 distinct targets, of which **482 never appear as a source** (they only receive
  edges).
- **Text fields:** **None.** References + positional word ranges only.
- **Metadata fields:** `score`, `coverage`, `matched_words_count` are per-edge metrics.
- **Directionality:** stored **directed**; 2,432 items have their reverse present, **1,120 are
  one-way only** (a consequence of per-source top-N pruning, not asymmetry of meaning). `coverage`
  legitimately differs by direction (it is relative to the source ayah length); `score` is
  symmetric in sampled mirror pairs.
- **Risks:**
  - `coverage > 100` in **4 items** (e.g. `56:27 → 56:38` = 200) — clamp or store raw + flag.
  - Word ranges are not yet validated against `quran_ayahs.words_count_real` (warning check).
  - Asymmetric pruning means a full undirected view requires synthesizing reverse edges at read
    time (decision, §10).

---

## 5. Relationship Model Analysis

| Question | `mutashabihat-ul-quran` (phrases) | `similar-ayahs` (matching) |
|---|---|---|
| Group-based? | **Yes** — one group per repeated phrase | No |
| Phrase-based? | **Yes** — phrase = group identity, with a representative `source` occurrence | Partial — `match_words` marks the matched span, but no shared phrase identity |
| Occurrence-based? | **Yes** at the leaf grain — (group, ayah, word-range) | Edge carries matched word ranges, but grain is the pair, not an occurrence |
| Pair-based? | Only implicitly (co-membership) | **Yes** — explicit source→target pairs |
| Directional? | **Undirected** (a phrase group has no direction; `source` is just the representative) | **Stored directed**, conceptually **undirected** similarity, asymmetrically pruned |
| Shape | **one-to-many** (group → occurrences); ayah↔group is **many-to-many** (an ayah is in 1–7 groups) | **many-to-many** ayah↔ayah |

**Summary:**
- Phrases = a **group → occurrence** hierarchy (1 group : N occurrences), with ayah↔group
  many-to-many because the same ayah recurs across phrases.
- Similar-ayahs = a **directed weighted edge list** over ayahs (a similarity graph), best treated as
  an undirected relation at the read layer while persisting the source's directed rows faithfully.

---

## 6. Ayah Mapping Analysis

**Canonical target:** existing `quran_ayahs` table (Feature 002 foundation). Confirmed schema:
`id` (externally-assigned int PK), `surah_number`, `ayah_number`, **`verse_key` (UNIQUE, format
`S:A`)**, `text_uthmani`, `words_count_source`, `words_count_real`, `page_from`, `page_to`.

- **Expected join key:** **`verse_key`** — the mutashabihat files use the identical `S:A` string,
  and `quran_ayahs.verse_key` has a unique index. `(surah_number, ayah_number)` is an equally valid
  unique alternate key. Resolve each verse_key to the stable integer **`ayah_id`** and store FKs as
  `ayah_id`, not raw strings.
- **Invalid / missing references:** **none.** Independently validating all **3,084** distinct
  verse_keys (union of all three files) against the canonical Hafs ayah-count table: **0 malformed,
  0 out-of-range, 0 outside `1:1…114:6`.** Coverage: **109 of 114 surahs**.
- **Is enrichment from `quran_ayahs` required?** Only two ways:
  1. **Required:** verse_key → `ayah_id` resolution at import (the FK target).
  2. **Optional (warning-level):** `words_count_real` to validate that word-range `to` indices do
     not exceed an ayah's word count.
  No ayah **text** needs to be copied — the datasets store references and positional indices only,
  which aligns perfectly with the constraint that *ayah identity relies on the existing Quran
  foundation, not re-created text identity.* `quran_words` is **not** touched.

---

## 7. Recommended Data Model

Two source-aligned modules. Names use the project's `quran_` prefix and snake_case table
convention.

### Module A — Mutashabihat phrase groups

**`quran_mutashabihat_groups`** — one row per repeated phrase.

| Column | Type | Notes |
|---|---|---|
| `id` | int PK (surrogate) | own surrogate key |
| `source_group_id` | int, UNIQUE | original phrase id (e.g. 50); provenance + idempotent re-run key |
| `representative_ayah_id` | int FK → `quran_ayahs.id` | from `source.key` |
| `representative_word_from` | smallint | from `source.from` |
| `representative_word_to` | smallint | from `source.to` |
| `occurrence_count` | smallint | **recomputed** total occurrences |
| `distinct_ayah_count` | smallint | **recomputed** |
| `distinct_surah_count` | smallint | **recomputed** |
| `raw_counts` | jsonb (nullable) | optional: original stale `{surahs,ayahs,count}` for audit |

**`quran_mutashabihat_occurrences`** — leaf grain: one row per (group, ayah, word-range).

| Column | Type | Notes |
|---|---|---|
| `id` | int PK | |
| `group_id` | int FK → `quran_mutashabihat_groups.id` | |
| `ayah_id` | int FK → `quran_ayahs.id` | |
| `word_from` | smallint | 1-based word index (verify base, §10) |
| `word_to` | smallint | |
| `is_representative` | bool | true when this row equals the group's `source` occurrence |
| | | **UNIQUE (`group_id`, `ayah_id`, `word_from`, `word_to`)** → absorbs the 1 duplicate |

> `phrase_verses.json` is intentionally **not** a table — its "ayah → groups" lookup is served by an
> index on `quran_mutashabihat_occurrences(ayah_id)`.

### Module B — Similar-ayah links

**`quran_similar_ayah_links`** — one row per directed source→target edge (faithful mirror of source).

| Column | Type | Notes |
|---|---|---|
| `id` | int PK | |
| `source_ayah_id` | int FK → `quran_ayahs.id` | map key |
| `target_ayah_id` | int FK → `quran_ayahs.id` | `matched_ayah_key` |
| `score` | smallint | 50–100 |
| `coverage` | smallint | raw 5–200 (clamp policy = §10) |
| `matched_words_count` | smallint | 1–29 |
| `match_words` | jsonb | list of `[from,to]`/`[x]` ranges on the source ayah |
| | | **UNIQUE (`source_ayah_id`, `target_ayah_id`)**; **CHECK source ≠ target** |

### Alternatives considered

- **A-alt: flatten occurrences into a `jsonb ayahs` column on the group row.** Rejected — loses the
  queryable per-ayah/per-word grain and the ayah↔group reverse lookup; harder to validate.
- **B-alt: one polymorphic `quran_ayah_relations` table for both datasets with a `relation_type`
  column.** Rejected — the two datasets differ in grain and attributes, and the source carries **no
  relation-type taxonomy** (similar-ayahs is purely lexical-overlap scored). A type column would be
  a constant. Keep two purpose-built tables.
- **B-alt2: store an undirected edge once (min(id),max(id)) and synthesize reverse at read.**
  Rejected for *storage* because `coverage` is direction-specific; mirroring would fabricate a
  coverage value. Keep directed rows; build the undirected view at read time.

**Recommended:** the four tables above (A: groups + occurrences; B: links), each behind its own
feature module/folder per Clean Architecture.

---

## 8. Recommended Import Pipeline

Mirror the existing Quran-foundation importer shape (reader → assembler → validator → writer →
report writer), one pipeline per dataset, sharing a common verse_key→ayah_id resolver.

1. **Source package (staged, not the live resources folder):**
   ```
   resources/import-sources/mutashabihat/<version>/
   ├── manifest.json          # file list + sha256 + record counts + retrieved-date + license note
   ├── phrases.json           # mutashabihat groups (truth)
   └── matching-ayah.json     # similar-ayah links (truth)
   ```
   `phrase_verses.json` is omitted (derived). The importer reads only from this staged package.
2. **Reader:** parse JSON, assert root is an object; no schema coercion.
3. **Assembler:**
   - Build an in-memory `verse_key → ayah_id` map from `quran_ayahs`.
   - Groups: emit group rows (recomputing counters) + occurrence rows; mark `is_representative`.
   - Links: emit directed link rows; clamp/flag `coverage`.
4. **Validator:** run §9 hard checks (fail fast) and collect §9 warnings.
5. **Writer:** transactional bulk insert. Idempotent: upsert/replace keyed on
   `source_group_id` (groups) and `(source_ayah_id, target_ayah_id)` (links). Delete-then-insert
   per dataset inside the transaction is acceptable.
6. **Report writer:** emit an import report (input counts, written counts, unresolved refs = 0
   expected, warning list, recomputed-vs-raw counter diffs).
7. **Safe re-run / force:** a re-run guard compares the manifest sha256 against the last successful
   import; **skip** if unchanged, **re-import** under `--force`. Never partially commit (transaction).

---

## 9. Validation Checks

### Hard checks (block the import)

- **Source manifest exact set:** exactly `{phrases.json, matching-ayah.json}` present, each matching
  its expected **sha256** and a non-zero record count.
- **Valid JSON, root = object** for both files.
- **Every verse_key** (group `source.key`, every occurrence ayah, every link source & target)
  matches `^\d+:\d+$` **and resolves** to an existing `quran_ayahs` row. Unresolved count must be
  **0** (currently 0).
- **Word ranges well-formed:** `word_from ≥ 1`, `word_to ≥ word_from`.
- **Groups have ≥ 2 distinct ayahs** (a mutashabih needs at least two occurrences; currently min 2).
- **No self-links** in similar-ayahs (`target ≠ source`; currently 0).
- **Score range** within `[50,100]` (currently true).
- **Uniqueness:** `source_group_id` unique; occurrence rows unique on
  (group, ayah, word_from, word_to); link rows unique on (source, target).

### Warning checks (record, do not block)

- **`coverage > 100`** (4 rows) → clamp to 100 (or store raw) and warn.
- **Word-range `to` exceeds `quran_ayahs.words_count_real`** for the ayah → potential index
  misalignment; warn per occurrence.
- **Stale pre-computed counters** (`surahs`/`ayahs`/`count`) disagree with recomputed values
  (46/55/56 groups) → use recomputed, warn with diffs.
- **Duplicate identical occurrence** within a group (1 case) → deduped by the unique constraint;
  warn.
- **`source.key` absent from its own group's occurrences** (group 1782) → warn, keep group.
- **`phrase_verses.json` present but inconsistent** with `phrases.json` (only if it is shipped) →
  warn.

### Informational notes (no action)

- **1,120 one-way** similar links lack a stored reverse (expected — per-source top-N pruning).
- **792 ayahs** appear in both datasets; **813** undirected pairs overlap → datasets are
  complementary.
- **109 / 114 surahs** covered; 3,084 distinct ayahs referenced overall.

---

## 10. Open Questions (real decisions before Spec Kit)

1. **Provenance / license.** Neither file carries license or attribution metadata. Record the
   origin and licensing of "Mutashabihat ul Quran" and "Similar Ayahs" in the `manifest.json` before
   any future publishing. (Storage as internal foundation data is fine; *publishing* needs this.)
2. **Word-index base.** Confirm the `[from,to]` indices are **1-based** and align with `quran_words`
   ordering, so word-range validation (warning check) and any future highlighting are correct.
3. **Coverage clamp policy.** Store raw `coverage` (5–200) or clamp to `[0,100]`? Recommendation:
   store raw, expose clamped at read.
4. **Reverse similar-edges.** Synthesize missing reverse edges at **read** (recommended) vs. at
   import. Keep persistence a faithful directed mirror either way.
5. **Confirm** `phrase_verses.json` is **not** stored (derive instead) — recommended.

None of these block modeling; they are clarifications, not data defects.

---

## 11. Recommended Feature Scope

**Feature 006 — Quran Mutashabihat Foundation** *should include:*

- A staged source package under `resources/import-sources/mutashabihat/<version>/` with a manifest
  (sha256, counts, provenance/license note).
- Two backend modules with the four tables in §7 (groups + occurrences; similar-ayah links).
- One idempotent, transactional import pipeline per dataset (reader → assembler → validator →
  writer → report) with a re-run guard and `--force`.
- The §9 hard/warning/info validation suite and an import report artifact.
- FK integrity to existing `quran_ayahs` via `ayah_id` (verse_key resolution).

**Feature 006 should explicitly EXCLUDE:**

- Any UI, API endpoints, controllers, or read models.
- Tafsir, translations, audio.
- Any modification to `quran_words`, `quran_ayahs`, or Quran text.
- Storing copied Quran text in the new tables (references + word indices only).
- Storing `phrase_verses.json` as a table (derive it).
- Synthesizing/persisting reverse similar-edges (defer to a later read feature).
- Cross-linking/merging the two datasets into a unified relation table.

---

## 12. Spec Kit Readiness

**Yes — we can start `/speckit.specify` after this report.**

The data is validated, the source-of-truth files are identified, the relationship models and the
canonical ayah mapping are confirmed, and a concrete table design and import pipeline are proposed.
The only prerequisites are **decisions, not fixes** — capture the §10 open questions (especially
provenance/license, word-index base, and coverage policy) as clarifications during
`/speckit.specify` → `/speckit.clarify`, and stage the two source files into
`resources/import-sources/` before `/speckit.implement`.

No data must be repaired first.

---

## Final Console Summary

- **Verdict:** READY WITH NOTES.
- **Main datasets found (2 sources of truth):**
  - `mutashabihat-ul-quran/original/phrases.json` — 814 repeated-phrase **groups** (group →
    occurrence model; word-range indices; no text).
  - `similar-ayahs/original/matching-ayah.json` — 3,552 directed **similarity links** over 1,162
    source ayahs (score 50–100, coverage, matched words; no text).
  - (`phrase_verses.json` = derived reverse index of `phrases.json`; do not store.)
- **Recommended tables:** `quran_mutashabihat_groups`, `quran_mutashabihat_occurrences`,
  `quran_similar_ayah_links` (join everything to existing `quran_ayahs` via `verse_key → ayah_id`).
- **Blocking issues:** none. All 3,084 referenced ayahs resolve (0 invalid). Notes to record before
  implementing: provenance/license missing; minor warnings (coverage > 100 ×4, 1 duplicate
  occurrence, 1 group source-key gap, stale pre-computed counters); stage files into
  `resources/import-sources/`.
