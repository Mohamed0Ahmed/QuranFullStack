# Phase 1 Data Model — Quran Word Morphology Foundation

Six PostgreSQL **read-only, source-built** tables, loaded from the local `quran-morphology/` source
files and keyed to the existing `quran_words`. DB columns `snake_case`; EF entities `PascalCase` under
`Domain/Quran/Words/Morphology/`. Types follow the Feature 002/003 convention: `smallint` where values
≤ 32,767, `int` otherwise (research R13). All Arabic text is `text` with default collation; feature bags
are `jsonb`; verbatim source strings are `text`.

> **Authoritative Quran text is never touched.** `quran_words` (Uthmani/QPC) stays the source of truth
> for display. `form_arabic_normalized` is a flagged **derived** reading aid — never Mushaf text, never
> an exact `qpcUthmani` substring (research R4). The raw `form_buckwalter` is always retained.

---

## Sources & relationships

```text
local quran-morphology/ (manifest-verified, read-only)
  corpus/quranic-corpus-morphology-qpc-aligned.json   → classification + structure (POS, segments,
                                                          features, verb tense/voice, case, Buckwalter
                                                          root/lemma cross-ref, segment form)
  corpus/corpus-qpc-location-alignment-map.json        → audit/provenance only (not seeded)
  qul/word-root.json / word-lemma.json / word-stem-corrected-arabic.json
                                                       → Arabic display values (root/lemma/stem)

quran_words (83,668)  WHERE is_ayah_marker = false → 77,432 readable words = the keying/gating input
   │ (join on location = qpcLocation; FK on id)
   ▼
quran_word_morphology (77,432)  ──▶ quran_word_morphology_segments (≈128,219)
   │  root_id / lemma_id / stem_id (nullable)        head_pos ──▶ quran_pos_tags.code
   ▼
quran_roots (derived count)   quran_lemmas (derived count)   quran_stems (derived count)
quran_pos_tags (≈30, importer-seeded)
```

- **Read-only inputs (never mutated):** the local source files (read once, manifest-verified) and
  `quran_words` (`id`, `location`, `is_ayah_marker` only).
- **FK from morphology → source:** `quran_word_id` → `quran_words.id`; segment `quran_word_id` →
  `quran_words.id`. Dimension and POS FKs are within the morphology table set.
- **Dimension counts** (roots/lemmas/stems) are **derived from the data and reported**, never hardcoded.

---

## 1. `quran_word_morphology` — 77,432 rows (one per readable word)

Word-level head morphology + dimension links + derived verb/case fields. Primary read surface.

| Column | Type | Null | Notes |
|---|---|---|---|
| `quran_word_id` | `int` | NO | **PK + FK** → `quran_words.id`; **UNIQUE**; 1:1 with readable words |
| `location` | `text` | NO | `"s:a:w"` (= `quran_words.location` = `qpcLocation`); provenance/join audit |
| `head_pos` | `text` | NO | POS of the first `kind = 'STEM'` segment by `segment_number`; operational morphology summary, **FK** → `quran_pos_tags.code` |
| `segment_count` | `smallint` | NO | ≥ 1; equals the number of segment rows |
| `root_id` | `int` | YES | **FK** → `quran_roots.id`; null where no QUL Arabic root (incl. Buckwalter-only) |
| `lemma_id` | `int` | YES | **FK** → `quran_lemmas.id`; null where no QUL Arabic lemma (incl. Buckwalter-only) |
| `stem_id` | `int` | YES | **FK** → `quran_stems.id`; null allowed |
| `is_verb` | `bool` | NO | `head_pos = 'V'` (derived) |
| `verb_tense` | `text` | YES | `past`/`present`/`imperative` (PERF/IMPF/IMPV); null for non-verbs |
| `verb_voice` | `text` | YES | `active`/`passive`; `passive` iff PASS, else `active` by convention; null for non-verbs |
| `case_feature` | `text` | YES | `nominative`/`accusative`/`genitive` (NOM/ACC/GEN); null where unmarked |
| `head_features_json` | `jsonb` | YES | parsed feature tokens of the head (STEM) segment |

**Indexes:** PK/UNIQUE(`quran_word_id`); `head_pos`; partial `(verb_tense)` / `(verb_voice)` `WHERE
is_verb`; `case_feature`; `root_id`; `lemma_id`; `stem_id`.

## 2. `quran_word_morphology_segments` — ≈ 128,219 rows (one per segment)

Full segment fidelity (prefix/stem/suffix), each with its own POS and rendering.

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, generated identity |
| `quran_word_id` | `int` | NO | **FK** → `quran_words.id` |
| `segment_location` | `text` | NO | `"s:a:w:seg"` |
| `segment_number` | `smallint` | NO | 1-based within the word |
| `kind` | `text` | NO | `PREFIX` / `STEM` / `SUFFIX` |
| `pos` | `text` | NO | segment POS code; **FK** → `quran_pos_tags.code` |
| `form_buckwalter` | `text` | NO | corpus `form` (segment surface, Buckwalter) — **always retained, lossless** |
| `form_arabic_normalized` | `text` | **YES** | Arabic rendering from Buckwalter; **`NULL`** for empty forms (expected 208); provenance-flagged and not authoritative Mushaf text |
| `arabic_render_tier` | `text` | YES | `clean` / `quranic_marks` / `review` / `multiword` |
| `arabic_render_source` | `text` | NO | constant `buckwalter-transliteration` (provenance flag) |
| `root_buckwalter` | `text` | YES | corpus `root` (Buckwalter cross-reference) |
| `lemma_buckwalter` | `text` | YES | corpus `lemma` (Buckwalter cross-reference) |
| `features_raw` | `text` | NO | **verbatim** corpus FEATURES string (lossless; supports re-parse / voice recompute) |
| `features_json` | `jsonb` | YES | parsed tokens (query convenience) |

**Indexes:** PK(`id`); `(quran_word_id, segment_number)` UNIQUE; `pos`; partial `(quran_word_id) WHERE
kind = 'STEM'`; `arabic_render_tier` (route `review`/`multiword` rows to curators).

## 3. `quran_roots` — dimension (distinct Arabic roots; count derived & reported)

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, generated identity |
| `root_text` | `text` | NO | **Arabic display** (QUL form, e.g. `"س م و"`); **UNIQUE** |
| `root_buckwalter` | `text` | YES | corpus form (e.g. `smw`) for cross-reference; null if QUL-only |
| `words_count` | `int` | NO | readable-word occurrences under this root |
| `distinct_lemmas_count` | `smallint` | NO | distinct lemmas under this root |
| `first_word_order_in_mushaf` | `int` | NO | stable display sort key; **UNIQUE** |

**Indexes:** PK(`id`); UNIQUE(`root_text`); UNIQUE(`first_word_order_in_mushaf`); `words_count`.

## 4. `quran_lemmas` — dimension (distinct Arabic lemmas; count derived & reported)

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, generated identity |
| `lemma_text` | `text` | NO | **Arabic display**; **UNIQUE** |
| `lemma_buckwalter` | `text` | YES | corpus form; cross-reference |
| `root_id` | `int` | YES | **FK** → `quran_roots.id` (dominant/first root; null when no root) |
| `words_count` | `int` | NO | occurrences |
| `first_word_order_in_mushaf` | `int` | NO | stable sort; **UNIQUE** |

**Indexes:** PK(`id`); UNIQUE(`lemma_text`); `root_id`; UNIQUE(`first_word_order_in_mushaf`).

## 5. `quran_stems` — dimension (distinct Arabic stems; count derived & reported)

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, generated identity |
| `stem_text` | `text` | NO | **Arabic display** (QUL corrected); **UNIQUE** |
| `words_count` | `int` | NO | occurrences |
| `first_word_order_in_mushaf` | `int` | NO | stable sort; **UNIQUE** |

**Indexes:** PK(`id`); UNIQUE(`stem_text`); UNIQUE(`first_word_order_in_mushaf`).

## 6. `quran_pos_tags` — POS controlled vocabulary (≈ 30 rows; importer-seeded, not migration `HasData`)

| Column | Type | Null | Notes |
|---|---|---|---|
| `code` | `text` | NO | **PK** (`N`, `V`, `PN`, `ADJ`, `PRON`, `P`, `CONJ`, `NEG`, `REL`, `DEM`, `VOC`, `INL`, …) |
| `arabic_label` | `text` | NO | Arabic display label (curated) |
| `english_label` | `text` | NO | English label (curated) |
| `category` | `text` | NO | broad group: `noun` / `verb` / `particle` / `other` |
| `sort_order` | `smallint` | NO | display order |
| `description` | `text` | YES | optional note |

**Indexes:** PK(`code`); `category`; `sort_order`. Seeded idempotently from the in-code dictionary.

---

## Derivation (assemble in memory, then COPY — research R1)

1. **Read + verify** the manifest and source files (size/`sha256`); read `quran_words.{id, location,
   is_ayah_marker}`.
2. **Assemble per readable word** (join aligned corpus by `location`, `is_ayah_marker = false`):
   map segments (kind/pos/form/features), transliterate each non-empty `form` →
   `form_arabic_normalized` + tier (`BuckwalterArabicMap` / `SegmentArabicRenderer`), pick the first
   STEM segment's POS as `head_pos`, preserve any additional STEM segments, derive
   `is_verb`/`verb_tense`/`verb_voice`/`case_feature` from the head STEM (research R7/R8).
3. **Resolve dimensions** from QUL Arabic values (dedup on Arabic text); set `root_id`/`lemma_id`/
   `stem_id` (NULL when QUL has no Arabic value, even if a Buckwalter value exists — research R5 / Q1);
   compute `words_count`, `distinct_lemmas_count`, and `first_word_order_in_mushaf`.
4. **Seed `quran_pos_tags`** from the curated in-code dictionary (idempotent).
5. **In one transaction:** (if `--force`) `TRUNCATE … RESTART IDENTITY CASCADE`; `COPY` pos →
   dimensions → morphology → segments (FK-safe order); run validation; commit iff all hard checks pass,
   else roll back (research R10).

## Domain types

Six plain entities in `Domain/Quran/Words/Morphology/`: `WordMorphology`, `WordMorphologySegment`,
`QuranRoot`, `QuranLemma`, `QuranStem`, `PosTag`. Four small enums/value objects (research R7, planning
§3.9): `SegmentKind` (Prefix/Stem/Suffix), `VerbTense` (Past/Present/Imperative), `VerbVoice`
(Active/Passive), `MorphologicalCase` (Nominative/Accusative/Genitive). `head_pos` and segment `pos` are
**not** enums — they reference `quran_pos_tags.code` (open, curated vocabulary).

## Validation invariants (enforced before commit — see contracts/validation-report.schema.md)

| Id | Severity | Invariant |
|---|---|---|
| `MORPH-READABLE-COMPLETE` | hard | one morphology row per readable word; count = `ExpectedReadableWords` (77,432) |
| `MORPH-MARKERS-EXCLUDED` | hard | 0 morphology/segment rows map to `is_ayah_marker = true` |
| `MORPH-LOCATION-MATCH` | hard | every morphology `location` matches a `quran_words.location`; 0 unmatched |
| `MORPH-SEGMENTS-PRESENT` | hard | every word has ≥ 1 segment; `segment_count` = segment-row count |
| `MORPH-POS-PRESENT` | hard | every segment has a `pos`; every word has at least one STEM; `head_pos` equals the first STEM POS by `segment_number` |
| `MORPH-POS-RESOLVES` | hard | every `head_pos` and segment `pos` resolves to a `quran_pos_tags.code` (0 unknown) |
| `MORPH-VERB-FEATURE-CONSISTENCY` | hard | head verbs have exactly one tense + valid voice; non-verbs null word-level verb fields |
| `MORPH-DIMENSION-RESOLVES` | hard | every non-null `root_id`/`lemma_id`/`stem_id` resolves (no dangling) |
| `MORPH-SEG-CHARSET` | hard | every `form` character is in the QAC map; **0 unmapped** (else refuse); space is allowed only for `multiword` tier |
| `MORPH-SEG-RENDER-TOTAL` | hard | every non-empty form → non-empty render; every empty form → `NULL` (expected 208) |
| `MORPH-SEG-TIER-VALID` | hard | every rendered row has a valid tier; `arabic_render_source = 'buckwalter-transliteration'` |
| `MORPH-SEG-RENDER-PROVENANCE` | hard (guard) | every rendered value is reproducible from `form_buckwalter` via the approved renderer; render source is `buckwalter-transliteration`; equality with Uthmani/QPC is informational |
| `MORPH-SOURCE-UNCHANGED` | hard | local source files match `manifest.json` size/`sha256` before & after the run |
| `MORPH-SEG-WORD-AGREEMENT` | warning | per-word translit vs `qpcUthmani` exact match ≈ 79.83 % (encoding-drift canary) |
| `MORPH-SEG-TIER-DIST` | warning | tier distribution ≈ 94.2 % / 5.4 % / 0.4 % / 1 |
| `MORPH-SEG-REVIEW-LIST` | warning | emit full `review` + `multiword` + empty (208) lists for manual sign-off |
| `MORPH-DIM-COUNTS` | warning | report actual distinct root/lemma/stem counts (derived, not hardcoded) |

Any hard check failing ⇒ rollback (write nothing) + failure report + non-zero exit (FR-028, FR-031).
