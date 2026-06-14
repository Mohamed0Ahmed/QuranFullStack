# Phase 1 Data Model — Quran Mutashabihat Foundation

Three PostgreSQL **read-only, source-built** tables, loaded from the local `mutashabihat/` staged package
and keyed to the existing `quran_ayahs`. DB columns `snake_case`; EF entities `PascalCase` under
`Domain/Quran/Mutashabihat/`. Types follow the Feature 002/004 convention: `smallint` where values ≤
32,767, `int` otherwise; `jsonb` only where a value is genuinely variable-length (research R15).

> **Authoritative Quran text is never touched.** The new tables store **references (`ayah_id`) and
> positional word indices only — never any copied ayah text** (research R3). `quran_ayahs` (with its
> `UNIQUE verse_key` and stable `int id`) is read-only — used solely to resolve `verse_key → ayah_id`. The
> two datasets are stored as **two separate table sets**, never a polymorphic merge (research R4).

---

## Sources & relationships

```text
local mutashabihat/ (manifest-verified, read-only)
  mutashabihat-ul-quran/phrases.json        → 814 groups; each group = a representative `source`
                                               occurrence {key, from, to} + an `ayah` map
                                               verse_key → [[word_from, word_to], …]
                                               (3,558 raw occurrence entries; 2,232 distinct ayahs)
  similar-ayahs/matching-ayah.json          → 1,162 source ayahs → 3,552 directed links; each item
                                               {matched_ayah_key, score, coverage,
                                                matched_words_count, match_words}
                                               (1,644 distinct ayahs)

quran_ayahs (read-only)  ── verse_key (UNIQUE) → id (int) ──┐  resolve EVERY reference (3,084 distinct)
   │                                                        │
   ▼                                                        ▼
quran_mutashabihat_groups (814)  ──▶ quran_mutashabihat_occurrences (3,557 stored unique)
   │  representative_ayah_id → quran_ayahs.id   group_id → groups.id (CASCADE); ayah_id → quran_ayahs.id
   │
quran_similar_ayah_links (3,552)  source_ayah_id → quran_ayahs.id ; target_ayah_id → quran_ayahs.id
```

- **Read-only inputs (never mutated):** the two local source files (read once, manifest-verified) and
  `quran_ayahs` (`id`, `verse_key`, and `words_count_real` for the word-range warning only).
- **Canonical mapping:** the group `source.key`, every occurrence `verse_key`, and both ends of every
  similar link are resolved to `quran_ayahs.id` via the `UNIQUE verse_key` and stored as `ayah_id` FKs —
  **no raw `verse_key` strings are stored**.
- **Fixed-count invariants** (validated source — re-derived in the capability report): **814** groups /
  **3,558** raw source occurrence entries / **1** duplicate identical occurrence / **3,557** stored unique
  occurrences / **1,162** distinct similar-ayah sources / **3,552** directed links / **3,084** distinct
  referenced ayahs.

---

## 1. `quran_mutashabihat_groups` — 814 rows (one per repeated phrase)

Identity + recomputed summary for each verbatim-phrase group; anchors its occurrences.

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, generated identity (surrogate; mirrors morphology dimension tables) |
| `source_group_id` | `int` | NO | opaque phrase id from `phrases.json` (range 50–16746); **UNIQUE** — provenance + idempotency key |
| `representative_ayah_id` | `int` | NO | **FK** → `quran_ayahs.id`; resolved from the source `source.key` |
| `representative_word_from` | `smallint` | NO | from source `source.from` (1-based, stored unchanged) |
| `representative_word_to` | `smallint` | NO | from source `source.to`; `≥ representative_word_from` |
| `occurrence_count` | `smallint` | NO | **recomputed** count of this group's stored unique occurrence rows |
| `distinct_ayah_count` | `smallint` | NO | **recomputed** distinct ayahs; **≥ 2** (no single-ayah groups) |
| `distinct_surah_count` | `smallint` | NO | **recomputed** distinct surahs across the group's occurrences |
| `raw_source_counts` | `jsonb` | YES | audit only: the original `{surahs, ayahs, count}` from the source (never used as a stored count) |

**Indexes:** PK(`id`); **UNIQUE**(`source_group_id`); `representative_ayah_id` (find groups anchored at an
ayah).

**Derivation.** `representative_*` come from the source `source` block; the three count columns are
**recomputed** from the group's stored unique occurrence rows after dedupe (the source's `surahs`/`ayahs`/
`count` go to `raw_source_counts` only — research R5). `MUT-STALE-SOURCE-COUNTERS` reports how many groups
disagreed.

## 2. `quran_mutashabihat_occurrences` — 3,557 stored unique rows (leaf grain: group × ayah × word-range)

Every occurrence of a group's phrase, as a positional reference into an ayah. Derived from 3,558 raw
source occurrence entries after collapsing 1 duplicate identical occurrence (research R6).

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, generated identity |
| `group_id` | `int` | NO | **FK** → `quran_mutashabihat_groups.id`, **ON DELETE CASCADE** |
| `ayah_id` | `int` | NO | **FK** → `quran_ayahs.id`; the occurrence ayah |
| `word_from` | `smallint` | NO | 1-based word index; `≥ 1` |
| `word_to` | `smallint` | NO | `≥ word_from` |
| `is_representative` | `bool` | NO | default `false`; `true` on the one occurrence equal to the group's `source` phrase |

**Indexes:** PK(`id`); **UNIQUE**(`group_id`, `ayah_id`, `word_from`, `word_to`) — this constraint
collapses the 1 known duplicate occurrence; `ayah_id` (the core "all mutashabihat of this ayah" lookup).
`group_id` is the unique index's leading column, so "all occurrences of a group" is already covered.

**Representative rule (research R7):** at most **one** occurrence per group may be `is_representative =
true`. Normal groups whose `source.key` is present in their occurrence list have exactly one. The known
anomalous group `source_group_id = 1782` (`source.key = 3:28`) has **zero** representative occurrence rows
— its group-level `representative_*` fields are still populated from source metadata, and
`MUT-SOURCE-KEY-ABSENT` records the anomaly (warning).

## 3. `quran_similar_ayah_links` — 3,552 rows (one directed source→target similarity edge)

Faithful, directed mirror of `matching-ayah.json` over 1,162 distinct source ayahs (research R8).

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, generated identity |
| `source_ayah_id` | `int` | NO | **FK** → `quran_ayahs.id`; the map key ayah |
| `target_ayah_id` | `int` | NO | **FK** → `quran_ayahs.id`; resolved from `matched_ayah_key` |
| `score` | `smallint` | NO | stored exactly as source; observed range 50–100 |
| `coverage` | `smallint` | NO | **raw**, stored exactly as source (observed 5–200); **never clamped** (4 rows > 100 kept) |
| `matched_words_count` | `smallint` | NO | stored as source; observed range 1–29 |
| `match_words` | `jsonb` | NO | the source list of word ranges on the source ayah, each `[from, to]` or single-word `[x]`; preserved exactly |

**Indexes / constraints:** PK(`id`); **UNIQUE**(`source_ayah_id`, `target_ayah_id`); **CHECK**
(`source_ayah_id <> target_ayah_id`) — no self-links (currently 0); `target_ayah_id` (lets a future read
layer synthesize the undirected/incoming view **without** stored reverse rows). `source_ayah_id` is the
unique index's leading column.

**No reverse rows.** The ≈ 1,120 one-way links stay one-way; `MUT-ONEWAY-LINKS` reports the count. Any
undirected/reverse reading is future read-layer work served by the `target_ayah_id` index.

---

## Excluded by design (not stored)

- **`phrase_verses.json` → no table.** It is a 100 %-consistent derivable reverse index of `phrases.json`;
  the verse→groups lookup is served at read time by the `occurrences(ayah_id)` index (research R10). It is
  excluded from the staged package; `MUT-PHRASE-VERSES-CONSISTENCY` is an optional informational
  cross-check only.
- **No reverse/undirected similar-ayah rows** (read-layer concern; `target_ayah_id` index covers it).
- **No merged/polymorphic `ayah_relations` table** (the two datasets stay in separate table sets).
- **No `import_runs` / history table in v1** (the manifest + emitted report provide provenance,
  source-unchanged proof, and idempotency; a run-history table would be YAGNI).
- **No Quran ayah text** in any new table (references + word positions only).

---

## Derivation (assemble in memory, then COPY — research R1)

1. **Read + verify** the manifest (exact file set, `expectedRecordCount`, `fileSizeBytes`, `sha256`); read
   `quran_ayahs.{id, verse_key, words_count_real}` and build the `verse_key → ayah_id` map.
2. **Assemble groups + occurrences** from `phrases.json`: for each group, resolve `source.key` →
   `representative_ayah_id`; expand the `ayah` map into occurrence rows (resolve each `verse_key` →
   `ayah_id`); collapse the 1 duplicate identical occurrence; flag the representative occurrence; recompute
   `occurrence_count` / `distinct_ayah_count` / `distinct_surah_count`; capture `{surahs, ayahs, count}`
   into `raw_source_counts`.
3. **Assemble links** from `matching-ayah.json`: for each source ayah, resolve `source_ayah_id`; for each
   item resolve `matched_ayah_key` → `target_ayah_id`; carry `score` / raw `coverage` /
   `matched_words_count` / `match_words` unchanged.
4. **Validate (assembly-time):** manifest set + checksum, JSON shape, raw occurrence count (3,558),
   verse-key format, charset/word-range shape, group/source/link counts.
5. **In one transaction:** (if `--force`) `TRUNCATE … RESTART IDENTITY CASCADE`; `COPY` groups →
   occurrences → links (FK-safe order); run validation SQL; re-verify source sha256
   (`MUT-SOURCE-UNCHANGED`); commit iff all hard checks pass, else roll back (research R12).

## Domain types

Three plain entities in `Domain/Quran/Mutashabihat/`: `MutashabihatGroup`, `MutashabihatOccurrence`,
`SimilarAyahLink`. No new enums — there is no controlled vocabulary to model (POS-style tables are a
morphology concern, not here). `is_representative` is a `bool`; `score`/`coverage`/`matched_words_count`
are plain `smallint`; `raw_source_counts`/`match_words` are `jsonb` (mapped to a string/`JsonDocument` per
the existing jsonb convention).

## Validation invariants (enforced before commit — see contracts/validation-report.schema.md)

| Id | Severity | Invariant |
|---|---|---|
| `MUT-MANIFEST-SET` | hard | staged file set is exactly `{mutashabihat-ul-quran/phrases.json, similar-ayahs/matching-ayah.json}` (+ `manifest.json`, `README.md`); no extras/missing |
| `MUT-MANIFEST-CHECKSUM` | hard | each source file's `sha256` + byte size match `manifest.json` |
| `MUT-JSON-SHAPE` | hard | both roots are objects; group values carry `{source, ayah}`; each similar item carries `{matched_ayah_key, score, coverage, matched_words_count, match_words}` |
| `MUT-GROUP-COUNT` | hard | group count = manifest expected **814** |
| `MUT-RAW-OCCURRENCE-COUNT` | hard | raw occurrence entries in `phrases.json` = **3,558** |
| `MUT-STORED-OCCURRENCE-COUNT` | hard | stored unique occurrence rows after dedupe = **3,557** |
| `MUT-SIMILAR-SOURCE-COUNT` | hard | distinct source-ayah count = manifest expected **1,162** |
| `MUT-SIMILAR-LINK-COUNT` | hard | directed link count = **3,552** |
| `MUT-VERSEKEY-FORMAT` | hard | every reference matches `^\d+:\d+$` |
| `MUT-AYAH-RESOLVE` | hard | every referenced verse_key resolves to a `quran_ayahs` row (**0** unresolved) |
| `MUT-WORD-RANGE-SHAPE` | hard | every occurrence range and every `match_words` range has `from ≥ 1`, `to ≥ from` |
| `MUT-GROUP-MIN-SIZE` | hard | every group has `distinct_ayah_count ≥ 2` |
| `MUT-LINK-NO-SELF` | hard | no link has `target_ayah_id = source_ayah_id` |
| `MUT-SCORE-RANGE` | hard | every link `score` ∈ [50, 100] |
| `MUT-OCCURRENCE-UNIQUE` | hard | after dedupe, occurrences unique on (`group_id`, `ayah_id`, `word_from`, `word_to`) |
| `MUT-SOURCE-UNCHANGED` | hard | source `sha256` re-verified after assembly, before commit |
| `MUT-COVERAGE-GT-100` | warning | count of links with `coverage > 100` (expected **4**); stored raw |
| `MUT-DUPLICATE-OCCURRENCE` | warning | identical occurrence ranges collapsed by the unique constraint (expected **1**: group 75, ayah 16:28) |
| `MUT-SOURCE-KEY-ABSENT` | warning | groups whose `source.key` is absent from their own occurrences (expected **1**: group 1782, 3:28) |
| `MUT-STALE-SOURCE-COUNTERS` | warning | groups whose source `surahs`/`ayahs`/`count` disagreed with recomputed values (recomputed values win) |
| `MUT-WORD-RANGE-UPPER-BOUND` | warning | word ranges whose upper index exceeds the ayah's `quran_ayahs.words_count_real` (possible alignment mismatch; stored unchanged) |
| `MUT-PROVENANCE-LICENSE-UNKNOWN` | warning | source provenance/license unknown in the manifest (expected **2** source files); never gates v1, blocks future publishing |
| `MUT-ONEWAY-LINKS` | info | directed links with no stored reverse (≈ **1,120**) |
| `MUT-CROSS-DATASET-OVERLAP` | info | ayahs / undirected pairs shared by both datasets (≈ **792** ayahs / **813** pairs) |
| `MUT-SURAH-COVERAGE` | info | distinct surahs referenced (**109 / 114**); total distinct ayahs referenced (**3,084**) |
| `MUT-PHRASE-VERSES-CONSISTENCY` | info (optional) | if `phrase_verses.json` is supplied, confirm it is a consistent reverse index of `phrases.json`; never stored |

Any **hard** check failing ⇒ rollback (write nothing) + failure report + non-zero exit (FR-021, FR-029).
Warning and informational checks are recorded in the report but never block the commit (FR-030, FR-031).
