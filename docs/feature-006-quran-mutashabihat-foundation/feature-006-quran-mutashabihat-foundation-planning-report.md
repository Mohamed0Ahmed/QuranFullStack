# Feature 006 — Quran Mutashabihat Foundation — Planning Report

**Project:** المنهج القرآني — Quran Dashboard
**Branch:** `006-quran-mutashabihat-foundation`
**Type:** Backend data foundation only. Planning report — source material for a later `/speckit.specify`.
**Date:** 2026-06-13

**Inputs read:** the data-capability report
(`docs/feature-006-quran-mutashabihat-foundation/mutashabihat-data-capability-report.md`); the staged
package (`resources/import-sources/mutashabihat/` — `manifest.json`, `README.md`,
`mutashabihat-ul-quran/phrases.json`, `similar-ayahs/matching-ayah.json`); the backend architecture
docs (`Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE,API_GUIDELINES}.md`),
`CODING_PRINCIPLES.md`; and prior foundation features 002 / 004 / 005 (specs + planning docs +
the live importer code under `Backend/{application,infrastructure,tools,tests}`).

> **Companion source of truth:**
> `docs/feature-006-quran-mutashabihat-foundation/mutashabihat-data-capability-report.md` (folder
> inventory, per-file structure, validated counts). This report does not restate it; it turns it into
> an implementable plan that mirrors the existing Feature 002/004 importer.

---

## 1. Executive Verdict

**READY WITH NOTES — `/speckit.specify` can start.**

All modeling decisions are locked, both source files are staged and checksummed, and every referenced
ayah resolves to `quran_ayahs` (0 invalid / 0 missing, independently re-validated in the capability
report). The design is a faithful clone of the existing **source-driven, COPY-bulk, one-transaction,
hard-gated, report-emitting** importer used by Features 002 and 004 — no new architecture is invented.

The "with notes" qualifier covers **decisions, not data defects**, all already enumerated in §12:
provenance/license is still undocumented; the word-index base should be confirmed 1-based; coverage is
stored raw (clamping deferred to read); reverse similar-edges are not persisted; and the staged folder
is named `mutashabihat` rather than the `quran-`-prefixed convention of its siblings. None block
specification.

---

## 2. Feature Goal

**What Feature 006 does.** Add three read-only PostgreSQL tables and **one operator-run console verb**
(`import-mutashabihat`) that ingests two independent, pre-staged local JSON datasets, resolves every
ayah reference to the existing `quran_ayahs` foundation, validates under a hard gate, bulk-loads inside
a single transaction, and writes a Markdown + JSON import report — exactly the shape of
`import-foundation` / `import-morphology`.

**Product capability it enables later (out of scope now).** A durable, query-ready relationship
foundation for: a similar-ayah panel on the ayah study page, "where else does this phrase occur"
navigation, Quran-search enrichment, and an ayah-relationship graph. Feature 006 builds **only the
data layer** those features will read.

**Why it stays backend-data-foundation only now.** Same staged posture as 002/004/005: get the
canonical data modeled, validated, and persisted with full provenance and a repeatable import before
any read model, API, or UI is designed. Locking the schema and the ayah-FK mapping first prevents the
read layer from being built on unverified shapes. No API/UI/read-model/tafsir/translation/audio is in
scope (§4).

---

## 3. Dataset Summary

Two **independent** datasets, each with its own source of truth, persisted as **two separate modules**
(never a shared polymorphic table — a locked decision).

### A) Mutashabihat phrase groups — `mutashabihat-ul-quran/phrases.json`

Verbatim repeated-phrase groups (المتشابهات اللفظية). **814 groups**, keyed by an opaque phrase id
(sparse 50–16746). Each group is one recurring phrase with a representative `source` occurrence
(`{key, from, to}`) and an `ayah` map of `verse_key → [[word_from, word_to], …]` listing every
occurrence. Shape = **group → many occurrences**; an ayah may belong to 1–7 groups. 2,232 distinct
ayahs, 3,558 occurrences. No text — positional word indices only.

### B) Similar Ayahs — `similar-ayahs/matching-ayah.json`

Scored ayah-to-ayah similarity edges (آيات متشابهة). **1,162 source ayahs → 3,552 directed links.**
Each item: `{matched_ayah_key, score (50–100), coverage (5–200), matched_words_count, match_words}`.
Shape = **directed source→target links**. Conceptually undirected similarity, but the source stores it
directed and asymmetrically pruned (per-source top-N, score-threshold ≥ 50); 2,432 links have a stored
reverse, 1,120 are one-way. No text — references + word ranges only.

### Why they are separate

Different grain (group-of-N vs. fixed pair) and different attributes (representative phrase + word
ranges vs. score/coverage/matched-count). The capability report showed expansion to undirected pairs
yields 17,862 (phrases) vs. 2,336 (similar) with **only 813 shared** — the datasets are complementary,
not redundant. Merging them would conflate two distinct notions of similarity and lose grain.

### Why `phrase_verses.json` is excluded

It is a verified-100%-consistent **reverse index** (verse_key → group-ids) of `phrases.json`, fully
regenerable. Storing it would duplicate truth and risk drift. The same "ayah → its groups" lookup is
served at read time by an index on the occurrences table (§6). It is excluded from the staged package
and may only be used as an optional derived-consistency cross-check.

---

## 4. Recommended Scope

**In scope**

- **Staged source package** at `resources/import-sources/mutashabihat/` (already created): the two
  truth files in dataset subfolders + `manifest.json` (sha256, sizes, expected counts) + `README.md`.
- **Source-manifest validation**: exact file set, per-file sha256 + byte size, expected record count
  — read before *and* re-verified after the build (source-unchanged gate), mirroring
  `MorphologyManifestReader`.
- **Three tables**: `quran_mutashabihat_groups`, `quran_mutashabihat_occurrences`,
  `quran_similar_ayah_links` (§5).
- **Importer pipeline** (§7): reader → assembler (verse_key→ayah_id resolution, counter recompute) →
  validator (hard/warning/info) → COPY bulk writer in one transaction → report writer; new verb
  `import-mutashabihat` on the existing `DataImporter` host.
- **Validation + reporting** (§8): Markdown + JSON report at `resources/report/mutashabihat/`.
- **Safe rerun**: refuse-unless-empty without `--force`; `--force` replaces; source files never mutated.

**Explicitly out of scope** (locked exclusions)

- No UI, no API endpoints/controllers, no frontend, **no read model yet**.
- No tafsir, translations, audio.
- No writes to `quran_words` or `quran_ayahs` (read-only join to resolve ayah_id).
- **No Quran text copied** into the new tables (references + word indices only).
- `phrase_verses.json` **not** stored as a table.
- **No synthesized/persisted reverse similar-ayah edges** at import.
- **No** unified polymorphic `ayah_relations` table — keep the two modules separate.

---

## 5. Proposed Database Model

Three read-only tables, snake_case, `quran_` prefix, consistent with Features 002/004. All FKs target
`quran_ayahs(id)` via the verse_key→ayah_id map built at import. Surrogate `id` columns follow the
morphology dimension/segment convention (own generated `id`); the importer streams ids during the
binary `COPY`. PostgreSQL types: `int` ids, `smallint` for word indices / small counts / score /
coverage, `jsonb` only where a variable-length list genuinely needs it.

### 5.1 `quran_mutashabihat_groups` — one row per repeated phrase

Purpose: identity + summary for each verbatim-phrase group; anchors its occurrences.

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | int PK | no | surrogate, generated (mirrors morphology dimension tables) |
| `source_group_id` | int | no | opaque phrase id from `phrases.json` (50–16746); provenance + idempotency key |
| `representative_ayah_id` | int FK→`quran_ayahs.id` | no | from `source.key` |
| `representative_word_from` | smallint | no | from `source.from` (1-based — confirm, §12) |
| `representative_word_to` | smallint | no | from `source.to` |
| `occurrence_count` | smallint | no | **recomputed** total occurrences (raw `count` is stale) |
| `distinct_ayah_count` | smallint | no | **recomputed** distinct ayahs |
| `distinct_surah_count` | smallint | no | **recomputed** distinct surahs |
| `raw_source_counts` | jsonb | yes | optional audit: original `{surahs, ayahs, count}` for traceability |

- **Unique:** `source_group_id`.
- **Indexes:** `representative_ayah_id` (find groups anchored at an ayah).
- **jsonb?** Yes for `raw_source_counts` only (small, audit-only, never queried structurally). Keep it
  nullable; drop it if the team prefers report-only provenance.

### 5.2 `quran_mutashabihat_occurrences` — leaf grain: (group, ayah, word-range)

Purpose: every occurrence of a group's phrase, as a positional reference into an ayah.

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | int PK | no | surrogate, generated |
| `group_id` | int FK→`quran_mutashabihat_groups.id` | no | owning group |
| `ayah_id` | int FK→`quran_ayahs.id` | no | occurrence ayah |
| `word_from` | smallint | no | 1-based word index |
| `word_to` | smallint | no | `word_to ≥ word_from` |
| `is_representative` | bool | no | true when this row equals the group's `source` occurrence; default false |

- **Unique:** (`group_id`, `ayah_id`, `word_from`, `word_to`) — absorbs the 1 known duplicate occurrence.
- **Indexes:** `ayah_id` (the core "all mutashabihat of this ayah" lookup); `group_id` is the unique
  index's leading column, so "all occurrences of a group" is already covered.
- **FK behavior:** `group_id` → `ON DELETE CASCADE` (group replacement cascades cleanly under `--force`).
- **jsonb?** No — fixed `[from,to]` pair maps to two `smallint` columns; keeps it queryable.

### 5.3 `quran_similar_ayah_links` — one directed source→target similarity edge

Purpose: faithful, directed mirror of `matching-ayah.json`.

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | int PK | no | surrogate, generated |
| `source_ayah_id` | int FK→`quran_ayahs.id` | no | the map key ayah |
| `target_ayah_id` | int FK→`quran_ayahs.id` | no | `matched_ayah_key` |
| `score` | smallint | no | 50–100 |
| `coverage` | smallint | no | **raw**, may exceed 100 (4 known rows); clamping deferred to read |
| `matched_words_count` | smallint | no | 1–29 |
| `match_words` | jsonb | no | list of source-ayah ranges, `[[from,to], …]` or `[x]` |

- **Unique:** (`source_ayah_id`, `target_ayah_id`).
- **Check:** `source_ayah_id <> target_ayah_id` (no self-links; currently 0).
- **Indexes:** `target_ayah_id` (lets the future read layer synthesize the undirected/reverse view
  without persisting reverse rows); `source_ayah_id` is the unique index's leading column.
- **jsonb?** Yes for `match_words` — genuinely variable-length, ragged ranges; not worth a child table
  for a read-only foundation. Persisted raw and faithful.

### 5.4 Import-run / source-manifest table — **not needed in v1**

Recommendation: **report-only**, no `import_runs` table. Features 002/004 carry no such table; the
`manifest.json` (sha256 + expected counts) plus the emitted Markdown/JSON report already provide
provenance, the source-unchanged gate, and idempotency. Adding a run-history table now would be
YAGNI. Note it as a possible later cross-cutting feature if import auditing is ever needed for
multiple importers.

---

## 6. Relationship Model

- **`quran_mutashabihat_groups` 1 → many `quran_mutashabihat_occurrences`** via `occurrences.group_id`.
  One group owns 2–70 occurrences (min size 2; no singletons).
- **`quran_ayahs` 1 → many `quran_mutashabihat_occurrences`** via `occurrences.ayah_id`. One ayah may
  appear in several groups, so ayah↔group is many-to-many, realized through the occurrence rows.
- **`quran_ayahs` → `quran_similar_ayah_links`** twice: as `source_ayah_id` and as `target_ayah_id`
  (two FKs from the same parent). The relation is stored directed; any undirected/reverse reading is a
  future read-layer concern served by the `target_ayah_id` index.

**Read queries this model already supports (later, no schema change):**

- *All mutashabihat (repeated-phrase occurrences) of an ayah* →
  `occurrences WHERE ayah_id = :id` → join `groups` → join sibling `occurrences` for co-members.
  Backed by the `occurrences(ayah_id)` index.
- *All occurrences under a repeated-phrase group* →
  `occurrences WHERE group_id = :id` (covered by the unique index's leading column).
- *Similar ayahs of an ayah* → `links WHERE source_ayah_id = :id` (directed), plus
  `links WHERE target_ayah_id = :id` (incoming) when an undirected view is wanted later.

---

## 7. Import Pipeline Plan

A near-mechanical clone of the Feature 002/004 importer. **No new project** — a new verb on the
existing `tools/QuranDashboard.DataImporter` host; **operator/CI only, never HTTP**.

- **CLI verb:** `import-mutashabihat` (joins `import-foundation`, `rebuild-words`, `import-morphology`),
  signature `import-mutashabihat [--source <path>] [--report-out <path>] [--force]`.
- **Source package path (default):** `resources/import-sources/mutashabihat/` (resolved via the same
  repository-root walk as `import-morphology`). The importer reads **only** this staged, git-ignored
  path — never `resources/mutashabihat/` originals.
- **Reader** (`Infrastructure/Files/Quran/Mutashabihat/`): `MutashabihatManifestReader` (verify set +
  sha256 + size), `JsonPhrasesReader` (→ group/occurrence DTOs), `JsonSimilarAyahReader` (→ link DTOs).
  Pure `System.Text.Json`, no EF types crossing the boundary.
- **Assembler** (`MutashabihatAssembler`, behind `IMutashabihatImportSource`): build the in-memory
  `verse_key → ayah_id` map from `quran_ayahs` (read-only); resolve every reference; recompute group
  counters; mark `is_representative`; emit a `MutashabihatSourceData` graph. Clamp nothing; flag
  anomalies as warnings.
- **Validator** (`Application/Quran/Mutashabihat/…` + a `MutashabihatSql`/invariants helper): run the
  §8 checks, producing `MutashabihatCheckResult[]` + a `Hard/Warning/Info` verdict.
- **Writer** (`Infrastructure/Persistence/Repositories/Quran/Mutashabihat/EfBulkMutashabihatWriter`):
  Npgsql **binary `COPY`** of groups → occurrences → links, then run validation SQL, **all in one
  transaction**; commit only if no hard check fails, else **rollback**. Mirrors `EfBulkMorphologyWriter`.
- **Report writer** (`Infrastructure/Reports/Quran/Mutashabihat/MarkdownJsonMutashabihatReportWriter`):
  emit `mutashabihat-import-report.md` + `.json` to `resources/report/mutashabihat/` (default), with
  totals, per-check results, warnings, and recomputed-vs-raw counter diffs.
- **DI placement:** register the source, writer, and report writer in
  `Infrastructure/DependencyInjection.cs`; the console host stays composition-only.
- **Transaction boundary:** one DB transaction wraps COPY + validation + commit/rollback (atomic).
- **Bulk strategy:** binary `COPY` via the Npgsql connection (proven for ≤ ~7.4k rows here — trivially
  fast vs. morphology's ~128k).
- **Safe rerun / `--force`:** if any target table is non-empty and `--force` is absent → **refuse**
  (console only, no report, no writes). With `--force` → truncate/replace the three tables inside the
  transaction, then load. The **source-unchanged** check re-reads the manifest sha256 after assembly
  and refuses to commit on drift.
- **Report output path:** `resources/report/mutashabihat/mutashabihat-import-report.{md,json}`
  (overridable via `--report-out`).

---

## 8. Validation Strategy

Check ids are kebab-case with a `Hard / Warning / Info` severity map, mirroring
`ImportValidationCheckIds` / `ImportValidationSeverities`. A single failed **hard** check rolls the
transaction back; warnings and info are recorded in the report but never block.

### Hard checks (block; rollback on failure)

- `manifest-set` — staged file set is exactly `{mutashabihat-ul-quran/phrases.json,
  similar-ayahs/matching-ayah.json}` (+ `manifest.json`, `README.md`); no extras, none missing.
- `manifest-sha256` — each file's sha256 + byte size match `manifest.json`.
- `json-shape` — both roots are objects; group values carry `{source, ayah}`; link items carry the
  5 expected fields.
- `group-count` — observed groups == manifest `expectedRecordCount` (814).
- `similar-source-count` — observed source ayahs == manifest `expectedRecordCount` (1162).
- `similar-link-count` — observed directed links == 3552.
- `verse-key-format` — every reference matches `^\d+:\d+$`.
- `ayah-resolve` — every reference (group source, every occurrence ayah, every link source & target)
  resolves in `quran_ayahs`; **0 unresolved required**.
- `word-range-shape` — `word_from ≥ 1` and `word_to ≥ word_from` for all occurrences and `match_words`.
- `group-min-size` — every group has ≥ 2 distinct ayahs.
- `link-no-self` — no link has `target == source`.
- `score-range` — every link `score` ∈ `[50, 100]`.
- `source-unchanged` — manifest sha256 re-verified after assembly, before commit.

### Warning checks (record; never block)

- `coverage-gt-100` — links with `coverage > 100` (4 known) — stored raw, flagged.
- `word-range-upper-bound` — occurrence/match `word_to` exceeds `quran_ayahs.words_count_real` for the
  ayah (optional cross-check; potential index misalignment).
- `duplicate-occurrence` — identical `(group, ayah, from, to)` deduped by the unique constraint
  (1 known: group 75, ayah 16:28).
- `stale-source-counters` — raw `surahs/ayahs/count` disagree with recomputed values (46/55/56 groups);
  recomputed values win, diffs reported.
- `source-key-absent` — a group's `source.key` is absent from its own occurrence set (1 known: group
  1782, 3:28); group kept.

### Informational checks (report only)

- `oneway-similar-links` — count of links lacking a stored reverse (~1,120; expected from top-N pruning).
- `cross-dataset-overlap` — ayahs/pairs shared by both datasets (792 ayahs / 813 pairs).
- `surah-coverage` — distinct surahs touched (109 / 114) and total distinct ayahs referenced (3,084).
- `phrase-verses-consistency` *(optional)* — if `phrase_verses.json` is supplied as a derived
  cross-check, confirm it is a consistent reverse index of `phrases.json`; never stored.

---

## 9. Data Safety Rules

- **No Quran text copied** into any new table — references (`ayah_id`) and positional word indices only.
- **No `quran_words` writes** — not touched at all.
- **No `quran_ayahs` writes** — read-only join to resolve `verse_key → ayah_id`.
- **No source-file mutation** — staged files are read-only; the source-unchanged sha256 gate enforces it.
- **No generated reverse similar-edges persisted** — directed source rows stored faithfully; reverse is
  a future read concern.
- **Warning anomalies never block** unless they would violate a hard integrity rule (e.g. an
  unresolved ayah reference is hard, not a warning).
- **Provenance preserved** — `source_group_id`, optional `raw_source_counts`, the manifest, and the
  emitted report keep every persisted row traceable to the source.
- **Source-safe tests** — fixtures use tiny synthetic groups/links, never real verse passages.

---

## 10. Test Strategy

xUnit + FluentAssertions + Testcontainers Postgres in the existing `tests/QuranDashboard.Tests`
project (`Quran/Mutashabihat/`), mirroring the morphology suite.

- **Reader tests** — `phrases.json` / `matching-ayah.json` parse into the expected DTO shapes; opaque
  ids, word ranges, and `match_words` ragged ranges preserved.
- **Assembler tests** — verse_key→ayah_id resolution; counter recompute (stale raw values overridden);
  `is_representative` flagging; `source.key`-absent handled without dropping the group.
- **Validator tests** — each hard check fails on an injected violation (unresolved ref, self-link,
  bad word range, count mismatch, sha256 drift); warnings flag without failing (coverage>100, dup
  occurrence, stale counters, source-key-absent).
- **Rollback tests** — an injected hard violation leaves all three tables empty and writes a failure
  report (atomicity).
- **Real-source gated tests** — when the staged package is present, a full import yields exactly 814
  groups / 3,558 occurrences / 3,552 links / 0 unresolved refs; gated like the foundation canonical
  test so CI without resources still passes.
- **Report-shape tests** — Markdown + JSON report contain totals, per-check id/severity/verdict, and
  counter diffs.
- **Safe-rerun tests** — refuse on non-empty targets without `--force`; `--force` replaces; a second
  identical run is idempotent (same row counts).
- **Source-unchanged tests** — sha256 drift between read and commit refuses/rolls back and leaves
  source files untouched.

---

## 11. Implementation Phases Proposal

High-level only (Spec Kit will expand into tasks):

1. **Setup / schema** — 3 domain entities + EF configs + DbContext DbSets; one schema-only EF migration
   (generated on request, no `HasData`); DI stubs.
2. **Reader + assembler** — manifest + two JSON readers; assembler with ayah_id resolution and counter
   recompute; source DTOs + `MutashabihatSourceData`.
3. **Validator + report** — hard/warning/info checks, verdict, Markdown/JSON report writer.
4. **Writer + transaction** — `EfBulkMutashabihatWriter` (binary COPY of the three tables) + validation
   inside one transaction (commit/rollback).
5. **CLI + DI** — `import-mutashabihat` verb (`--source/--report-out/--force`), DI registration,
   refuse/force semantics, exit codes.
6. **Safe rerun** — non-empty refusal, `--force` replace, source-unchanged gate.
7. **Tests / polish** — the §10 suite; clean-code + test-guard self-checks; quickstart doc.

---

## 12. Open Questions for `/speckit.clarify`

Real decisions only (none block specification):

1. **Provenance / license** of both datasets is undocumented (manifest notes it `UNKNOWN — TODO`).
   Required before any future *publishing*, not before import. → decide owner/source/license text.
2. **Word-index base** — confirm `[from,to]` indices are **1-based** and aligned with `quran_words`
   ordering (drives the optional `word-range-upper-bound` warning and any later highlighting).
3. **Coverage policy** — confirmed store-raw (5–200); clamp only later at read. (Lock as stated.)
4. **Reverse-edge policy** — confirmed persist directed source rows only; synthesize undirected/reverse
   later at the read layer. (Lock as stated.)
5. **`phrase_verses.json`** — confirmed not stored; allowed only as an optional derived-consistency
   cross-check. (Lock as stated.)
6. **Staged folder naming** — package is `import-sources/mutashabihat/` while siblings are
   `quran-foundation` / `quran-morphology`. Keep `mutashabihat` (and point the verb's default `--source`
   at it) or rename to `quran-mutashabihat` for prefix consistency? (Minor; recommend deciding before
   the importer hardcodes the default path.)
7. **`raw_source_counts` audit column** — keep the optional jsonb audit column on `groups`, or rely on
   the report only? (Recommend keep; cheap, improves traceability.)

---

## 13. Spec Kit Prompt Notes

**What `/speckit.specify` should lock (carry these in as settled):**

- Backend-data-foundation only; the §4 exclusions verbatim (no UI/API/read-model/tafsir/translation/
  audio; no `quran_words`/`quran_ayahs` writes; no text copied; no `phrase_verses` table; no reverse
  edges; no polymorphic merge).
- The three tables of §5 as the data model; `quran_ayahs` as the canonical ayah target via
  `verse_key → ayah_id`; `phrases.json` and `matching-ayah.json` as the two sources of truth.
- The source-driven importer of §7 (new `import-mutashabihat` verb on the existing `DataImporter`
  host; COPY + one transaction + hard gate + report; refuse-unless-empty/`--force`; source-unchanged).
- The validation taxonomy of §8 (hard rolls back; warnings/info recorded) and the known-anomaly list as
  warnings, not blockers.
- The known data facts (814 groups; 1162 source ayahs / 3552 links; all refs resolve; recompute stale
  counters; store raw coverage; directed links faithful).

**What `/speckit.clarify` should ask:** the §12 items — primarily provenance/license, word-index base,
and the staged-folder naming; the coverage/reverse/`phrase_verses` items are pre-decided and only need
confirmation.

---

## 14. Final Recommendation

**Start `/speckit.specify` now.** The data is staged, checksummed, and fully validated; the modeling
decisions are locked; and the implementation is a faithful reuse of the proven Feature 002/004 importer
with no new architecture or project. The only prerequisites are **clarifications, not fixes** — fold
the §12 open questions (especially provenance/license, word-index base, and staged-folder naming) into
`/speckit.clarify`. No data must be repaired first.

---

### Assumptions flagged

- **A1 — Source path:** the importer's default `--source` is `resources/import-sources/mutashabihat/`
  (matching what is staged), pending the §12.6 naming decision.
- **A2 — PK strategy:** new tables use own generated surrogate `id`s loaded via COPY, mirroring the
  morphology dimension/segment tables (`ValueGeneratedOnAdd`), not the keyed-to-parent
  `ValueGeneratedNever` style of `quran_word_morphology`.
- **A3 — Host:** new verb on the existing `tools/QuranDashboard.DataImporter` host; no new project.
- **A4 — Report path:** `resources/report/mutashabihat/` by convention with `words-morphology`.
- **A5 — Counts as invariants:** 814 / 1162 / 3552 (and recomputed 3558 occurrences, 3084 distinct
  ayahs) become expected-count constants used by the §8 checks; manifest `expectedRecordCount` is the
  hard baseline, derived counts are reported.

No data beyond the staged source files was invented; all counts trace to the capability report and a
direct re-read of `phrases.json` / `matching-ayah.json`.
