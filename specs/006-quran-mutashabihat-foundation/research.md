# Phase 0 Research — Quran Mutashabihat Foundation

All decisions below are settled; there are **no open NEEDS CLARIFICATION** items (the spec, its
2026-06-13 Clarifications, the planning report, and the capability report are exhaustive). Each entry:
**Decision → Rationale → Alternatives rejected.** Facts about the existing system were read from source
(the importer `Program.cs` verb dispatch, the Feature 002/004 import abstractions, the `COPY`-based
`EfBulkMorphologyWriter`, the JSON readers / `ManifestReader`, and the `Ayah` entity / `AyahConfiguration`
— `Id` is an `int` PK with `ValueGeneratedNever`; `verse_key` is `UNIQUE`; `words_count_real` exists).

---

## R1. Computation location: in-memory assembly + Npgsql binary `COPY` (source-driven, not DB-to-DB)

**Decision.** Read the two local JSON source files, **assemble** the relationship graph (group rows,
occurrence rows, directed link rows, with every `verse_key` resolved to an `ayah_id` and all counters
recomputed) in application memory, then **bulk-load with Npgsql binary `COPY`** and validate inside one
transaction. This mirrors the Feature 002/004 importer (`EfBulkMorphologyWriter` /
`EfBulkQuranImportWriter`), **not** a DB-to-DB `INSERT … SELECT`.

**Rationale.** The inputs are **files** (`phrases.json`, `matching-ayah.json`). The work — parsing nested
JSON, resolving each `verse_key` against `quran_ayahs`, recomputing group counters, collapsing the one
duplicate occurrence, flagging the representative occurrence — is inherently application-side and cannot
be expressed as set-based SQL over existing tables. `COPY` is the established, fastest bulk path in this
codebase. Only `quran_ayahs.{id, verse_key, words_count_real}` is read from the DB (to resolve references
and run the word-range upper-bound warning).

**Alternatives rejected.** *`INSERT … SELECT` (Feature 003 style)* — impossible; the source is not in the
DB. *EF `AddRange` change-tracking* — unnecessary overhead; `COPY` is the house pattern even at this small
scale and keeps the importer uniform with its siblings.

---

## R2. Source readers + manifest verification (read-only, local-only)

**Decision.** One reader per file (`JsonPhrasesReader`, `JsonSimilarAyahReader`) plus
`MutashabihatManifestReader`, orchestrated by `MutashabihatImportSource` (the `IMutashabihatImportSource`).
The default `--source` is the local staged package `App/resources/import-sources/mutashabihat/`; the
importer reads **only** that tree and never the original `resources/mutashabihat/` working folder. The
manifest is verified (exact file set, `expectedRecordCount`, `fileSizeBytes`, `sha256`) **before** the
run; `MUT-SOURCE-UNCHANGED` re-verifies size/`sha256` **after** assembly and before commit to prove the
files were never written.

**Rationale.** Mirrors the Feature 002/004 `ManifestReader` / `…ImportSource` split. Per-file readers keep
each parser cohesive and unit-testable. Local-only, manifest-gated reads make the load reproducible and
tamper-evident, and keep runtime independent of any upstream path. The staged `manifest.json` already
carries the two roles with `expectedRecordCount` 814 and 1162.

**Alternatives rejected.** *One monolithic reader* (two very different JSON shapes — less cohesive).
*Reading the original `resources/mutashabihat/` working folder at runtime* (provenance only, not the
canonicalized package). *Trusting files without a manifest* (no tamper/version evidence).

---

## R3. Canonical ayah target: resolve every `verse_key` to `quran_ayahs.id` (store `ayah_id`, not strings)

**Decision.** Build an in-memory `verse_key → ayah_id` map from `quran_ayahs` (read-only) and resolve
**every** reference through it: the group `source.key`, every occurrence `verse_key`, and both ends of
every similar link. Store the resulting integer `ayah_id` as a real FK into `quran_ayahs.id`. The new
tables store **no raw `verse_key` strings**. If **any** reference fails to resolve, the run fails the hard
gate (`MUT-AYAH-RESOLVE`) and rolls back.

**Rationale.** `quran_ayahs.verse_key` is `UNIQUE` and its `Id` is the stable integer identity already
used as the FK target across the schema; joining on the integer id is the canonical, index-friendly shape.
The capability report independently re-validated that **all 3,084** distinct referenced verse_keys resolve
(0 invalid, 0 missing), so a successful run resolves 100 %.

**Alternatives rejected.** *Storing raw `verse_key` strings* (denormalized, no referential integrity,
slower joins). *A new ayah-key lookup table* (redundant — `quran_ayahs` already is the canonical map).
*Best-effort resolution that skips unresolved refs* (would silently drop data — fail-closed is safer).

---

## R4. Two separate table sets, never a polymorphic merge

**Decision.** Model the two datasets as **two independent table sets**: a group + leaf-occurrence pair
(`quran_mutashabihat_groups` 1→many `quran_mutashabihat_occurrences`) for repeated phrases, and a single
directed-edge table (`quran_similar_ayah_links`) for scored similarity. **No** shared/polymorphic
`ayah_relations` table.

**Rationale.** The datasets have **different grain** (a group of N occurrences vs. a fixed source→target
pair) and **different attributes** (representative phrase + positional word ranges vs.
score/coverage/matched-word count). The capability report showed they are complementary, not redundant
(expansion to undirected pairs yields 17,862 phrase pairs vs. 2,336 similar pairs, only 813 shared).
Merging them would conflate two distinct notions of similarity and lose grain. `BACKEND_STRUCTURE.md`
mandates domain/feature grouping over a generic technical container.

**Alternatives rejected.** *A unified `ayah_relations(source, target, kind, payload jsonb)` table* (loses
grain and type safety, mixes two notions of similarity, forces a `jsonb` grab-bag). *One wide table with
nullable columns for both shapes* (sparse, ambiguous, hard to constrain).

---

## R5. Recompute group counters from the actual occurrences (source counters are stale)

**Decision.** Store `occurrence_count`, `distinct_ayah_count`, and `distinct_surah_count` as values
**recomputed** from the group's stored unique occurrence rows after dedupe. The source's pre-computed
`{surahs, ayahs, count}` are preserved verbatim in an audit-only `raw_source_counts` (`jsonb`, nullable)
column but are **never** used as the stored counts. `MUT-STALE-SOURCE-COUNTERS` reports how many groups
disagreed.

**Rationale.** The capability report found the source `count`/`ayahs`/`surahs` are stale for tens of
groups; trusting them would persist wrong totals. Recomputing makes the stored counts provably consistent
with the occurrence rows (`MUT-GROUP-MIN-SIZE` then guarantees `distinct_ayah_count ≥ 2`). Keeping the raw
blob preserves provenance/traceability for audit without letting it drive reads.

**Alternatives rejected.** *Trust the source counters* (persists known-stale data). *Drop the raw counters
entirely* (loses provenance for the diff/audit). *Promote raw counters to real columns* (would invite
their accidental use as truth).

---

## R6. Occurrence grain + uniqueness: one row per (group, ayah, word-range); collapse the duplicate

**Decision.** `quran_mutashabihat_occurrences` is one row per **unique** (`group_id`, `ayah_id`,
`word_from`, `word_to`), with a `UNIQUE` constraint on that tuple. The **3,558** raw source occurrence
entries collapse to **3,557** stored rows because the single known duplicate identical occurrence (group
75, ayah 16:28, range `[[17,19],[17,19]]`) is absorbed by the constraint and recorded as warning
`MUT-DUPLICATE-OCCURRENCE`. Word ranges map to two `smallint` columns (`word_from`, `word_to`), not
`jsonb`, since each occurrence is a single fixed `[from, to]` pair.

**Rationale.** The unique constraint is the natural data integrity rule and doubles as the duplicate
collapse, so the dedupe is enforced by the schema, not by ad-hoc code. Two `smallint` columns keep the
range queryable and cheap (vs. a `jsonb` pair). The capability report confirmed exactly one duplicate.

**Alternatives rejected.** *Store all 3,558 raw rows* (admits a true duplicate; breaks "stored unique
occurrences"). *`jsonb` for the single range* (unqueryable, heavier, no benefit for a fixed pair). *A
child table for word ranges* (over-engineered for a single `[from, to]`).

---

## R7. Representative occurrence: at most one per group; the `source.key`-absent anomaly kept

**Decision.** Flag `is_representative = true` on the **one** occurrence whose `(ayah, word-range)` equals
the group's `source` phrase (`source.key` + `source.from`/`source.to`). Normal groups (whose `source.key`
appears in their occurrence list) get exactly one representative occurrence. The single known anomalous
group (`source_group_id = 1782`, `source.key = 3:28`, whose source key is absent from its own occurrence
list) is kept with **zero** representative occurrence rows; its **group-level** `representative_ayah_id` /
`representative_word_from` / `representative_word_to` are still populated from the source `source`
metadata, and `MUT-SOURCE-KEY-ABSENT` records the anomaly as a non-blocking warning.

**Rationale.** The group-level representative fields always come from `source` (so the anchor is never
lost), while the occurrence-level flag marks the matching leaf row when one exists. Keeping the one
anomalous group (rather than dropping or erroring) preserves the source faithfully; surfacing it as a
warning makes the anomaly visible without failing the build.

**Alternatives rejected.** *Drop the anomalous group* (loses source data). *Hard-fail on it* (a known,
documented source quirk should not block a faithful import). *Synthesize a representative occurrence for
it* (would invent data not present in the source).

---

## R8. Similar links stored directed and faithful; no synthesized reverse rows; coverage stored raw

**Decision.** Store the **3,552** directed source→target links exactly as `matching-ayah.json` has them.
Do **not** synthesize or persist reverse/mirror rows — the ≈ 1,120 one-way links stay one-way
(`MUT-ONEWAY-LINKS` reports the count). `coverage` is stored **raw** (observed 5–200); the 4 rows > 100
are kept unchanged and reported via `MUT-COVERAGE-GT-100`. A `UNIQUE(source_ayah_id, target_ayah_id)`
constraint plus a `CHECK(source_ayah_id <> target_ayah_id)` (no self-links) guard integrity; a non-unique
index on `target_ayah_id` lets a future read layer synthesize the undirected/incoming view **at read
time** without stored reverse rows.

**Rationale.** Faithful storage is the foundation's job; clamping coverage or inventing reverse edges is
read-layer policy that a later feature owns. The `target_ayah_id` index makes the reverse/undirected view
a cheap read, so persisting mirror rows would only duplicate truth and risk drift. The capability report
confirmed 0 self-links, score ∈ 50–100, coverage ∈ 5–200 (4 rows > 100).

**Alternatives rejected.** *Synthesize reverse rows at import* (duplicates truth, doubles the table, risks
asymmetric drift). *Clamp/normalize coverage on import* (destroys the raw source value; normalization
belongs at read). *Store `match_words` as columns* (it is genuinely ragged/variable-length — see R9).

---

## R9. `match_words` as `jsonb`; word ranges as 1-based source indices stored unchanged

**Decision.** Persist each link's `match_words` (the list of matched word ranges on the source ayah) as
`jsonb`, preserving the source list exactly (each entry `[from, to]` or a single-word `[x]`). Treat all
word indices (occurrence `word_from`/`word_to`, `match_words` entries, `representative_word_*`) as
**1-based source indices** and store them unchanged, asserting `from ≥ 1` and `to ≥ from`
(`MUT-WORD-RANGE-SHAPE`, hard). If a range's upper index exceeds the referenced ayah's word count
(`quran_ayahs.words_count_real`), the row is still stored unchanged and reported via
`MUT-WORD-RANGE-UPPER-BOUND` (warning only).

**Rationale.** `match_words` is variable-length and ragged, so a child table is over-engineering for a
read-only foundation; `jsonb` keeps it lossless and faithful. The 1-based assumption matches the source's
own indexing; making the upper-bound check a **warning** means a possible source/corpus alignment
difference cannot block v1, consistent with the locked clarification.

**Alternatives rejected.** *A child table for `match_words`* (premature normalization). *Re-basing indices
to 0* (would silently rewrite source values). *Hard-failing on an over-range index* (a possible alignment
quirk should not block a faithful import — keep it a warning).

---

## R10. Exclude `phrase_verses.json`; serve the verse→groups lookup from an index

**Decision.** Do **not** import `phrase_verses.json` as a table. The "which groups contain this ayah"
lookup it represents is served at read time by the non-unique `ayah_id` index on
`quran_mutashabihat_occurrences`. `phrase_verses.json` is excluded from the staged package and from
storage; it MAY be used only as an optional derived-consistency cross-check
(`MUT-PHRASE-VERSES-CONSISTENCY`, informational, never stored).

**Rationale.** The capability report verified `phrase_verses.json` is a 100 %-consistent **reverse index**
of `phrases.json`, fully regenerable. Storing it would duplicate truth and risk drift, while the
occurrence `ayah_id` index answers the same question directly.

**Alternatives rejected.** *Import it as a real table* (duplicates derivable data, drift risk).
*Materialize a reverse view now* (YAGNI — the index covers it; the read layer is a later feature).

---

## R11. Trigger: a new `import-mutashabihat` verb on the existing console host

**Decision.** Add a verb to `tools/QuranDashboard.DataImporter/Program.cs`
(`import-foundation | rebuild-words | import-morphology | generate-i3rab | import-mutashabihat`).
`import-mutashabihat` accepts `--source <path>` (default = the local `mutashabihat/` staged package),
`[--report-out <path>]`, and `[--force]`. It depends on `import-foundation` having populated `quran_ayahs`
and is independent of the other verbs.

**Rationale.** An operator/CI batch op must stay **off any HTTP surface**; the host already wires DI + a
report writer; a new verb avoids a new project. A distinct verb keeps each verb single-purpose and matches
how Features 002/004 run their imports. The argument shape (`--source/--report-out/--force`) and the
repository-root walk reuse the exact `RunImportMorphologyAsync` / `ResolveRepositoryRoot` pattern already
in `Program.cs`.

**Alternatives rejected.** *Overloading an existing verb* (mixes unrelated source schemas). *A new console
project* (host/DI sprawl). *A guarded API endpoint* (violates "no API / no request-path").

---

## R12. Transactional load with a hard-gated validation suite before commit; refuse-unless-empty / `--force`

**Decision.** Within one transaction: (if `--force`) `TRUNCATE` the three mutashabihat tables
`RESTART IDENTITY CASCADE`; `COPY` groups → occurrences → links (FK-safe order); run the validation
queries; **commit only if every hard check passes, else roll back**. A non-forced run against any
non-empty target table **refuses** before writing (console only, no report). `quran_ayahs` / `quran_words`
are never in the truncate/write set. `MUT-SOURCE-UNCHANGED` re-verifies the manifest sha256 after assembly
and before commit, so a source that changed mid-run rolls back. Orchestration mirrors
`ImportMorphologyHandler` (refuse-unless-empty → act → write report → map exit code).

**Rationale.** Validating inside the same transaction as the writes is the only way to guarantee "nothing
is written on failure" atomically. FK order (groups before occurrences; ayah FKs already exist) satisfies
referential integrity during `COPY`. `ON DELETE CASCADE` on `occurrences.group_id` makes `--force`
replacement clean. The capability report's counts become the expected values the hard checks compare
against.

**Alternatives rejected.** *Validate-after-commit-then-delete* (non-atomic; bad-data window). *`DELETE`
instead of `TRUNCATE`* (slower; keeps identity high-water mark). *Auto-overwrite without `--force`* (unsafe
for a Quranic data foundation).

---

## R13. Validation home: DB-bound queries in Infrastructure; constants/records in Abstractions

**Decision.** Structural/relational checks (row counts, ayah resolution, no-self-link, score range,
occurrence uniqueness, group min size, source-unchanged, and the warning/info aggregates) run as queries
in `EfBulkMutashabihatWriter` (SQL text in `MutashabihatSql`). File-shape and manifest checks
(`MUT-MANIFEST-SET`, `MUT-MANIFEST-CHECKSUM`, `MUT-JSON-SHAPE`, raw occurrence count) are computed during
read/assembly. Known constants (`ExpectedGroups = 814`, `ExpectedRawOccurrences = 3_558`,
`ExpectedStoredOccurrences = 3_557`, `ExpectedSimilarSources = 1_162`, `ExpectedSimilarLinks = 3_552`,
`ExpectedDistinctAyahs = 3_084`) and the check-result/result records live in
`Application.Abstractions/Quran/Mutashabihat/`. Correctness is proven with **real-Postgres integration
tests** (Testcontainers) plus pure unit tests for the readers/assembler.

**Rationale.** Invariants are query-bound, so they execute where DB access lives (Infrastructure
references Abstractions + Domain, not Application). Keeping records/constants in Abstractions keeps the
contract strongly typed and avoids leaking EF entities. Matches the Feature 002/004 posture and test-guard
guidance (real infrastructure for query correctness).

**Alternatives rejected.** *Pure in-memory validator* (cannot express the relational invariants like
"every `ayah_id` resolves" against the DB). *Holding an open transaction across the Application boundary*
(leaky). *Hardcoding counts inline in the writer* (constants belong in the typed contract).

---

## R14. Report: feature-local records + a Markdown+JSON writer

**Decision.** Feature-local records (`MutashabihatImportResult`, `MutashabihatImportTotals`,
`MutashabihatCheckResult`) and `IMutashabihatReportWriter`, implemented by
`MarkdownJsonMutashabihatReportWriter`, mirroring the Feature 002/004 report pattern. The report records
the written row counts (groups / stored unique occurrences / links / distinct sources), the raw source
occurrence count (3,558), every hard-check result, every warning count (coverage>100 = 4,
duplicate-occurrence = 1, source-key-absent = 1, provenance/license unknown = 2 source files,
stale-counter group count), and every informational figure (one-way links ≈ 1,120, cross-dataset overlap
792 ayahs / 813 pairs, surah coverage 109/114, 3,084 distinct ayahs). Default output:
`resources/report/mutashabihat/`.

**Rationale.** A small cohesive feature-local set keeps domain grouping clean and parallels the familiar
`MorphologyCheckResult` shape (`Id, Severity, Expected, Observed, Passed`). Every started build emits the
report on both pass and fail; early refusals write none.

**Alternatives rejected.** *Reusing another feature's result type* (cross-feature coupling, unused
fields). *A shared generic result type now* (premature). *Report-only provenance with no per-check table*
(loses the auditable check list the maintainer relies on).

---

## R15. Column types and keys

**Decision.**
- Surrogate `id` PKs on all three tables: **`int`**, generated on add (`ValueGeneratedOnAdd`), mirroring
  the morphology dimension/segment tables; the writer streams ids during the binary `COPY`. (Contrast the
  `ValueGeneratedNever` style used only for rows keyed 1:1 to an existing parent — not the case here.)
- `source_group_id`, `representative_ayah_id`, `ayah_id`, `source_ayah_id`, `target_ayah_id`, FKs to
  `quran_ayahs.id`: **`int`**.
- `representative_word_from/_to`, `word_from/_to`, `occurrence_count`, `distinct_ayah_count`,
  `distinct_surah_count`, `score`, `coverage`, `matched_words_count`: **`smallint`** (all ≤ 32,767;
  observed maxima are far smaller).
- `is_representative`: **`bool`** (NOT NULL, default false).
- `raw_source_counts`, `match_words`: **`jsonb`** (the only genuinely variable-length values).
- FKs: groups.`representative_ayah_id` → `quran_ayahs.id`; occurrences.`group_id` →
  `quran_mutashabihat_groups.id` **ON DELETE CASCADE**; occurrences.`ayah_id` → `quran_ayahs.id`;
  links.`source_ayah_id` / `target_ayah_id` → `quran_ayahs.id`.

**Rationale.** Matches the Feature 002/004 typing convention (`smallint` where it fits, `int` otherwise).
Real FKs to the stable, never-truncated `quran_ayahs` add integrity at no cost. `jsonb` is used only where
a value is genuinely ragged (audit counters, matched-word ranges).

**Alternatives rejected.** *`smallint` ids* (fine for current counts but inconsistent with the schema's
`int`-id convention). *`ValueGeneratedNever` surrogate ids* (these tables own their identity; nothing keys
1:1 to a parent). *Promoting `match_words` / `raw_source_counts` to columns* (ragged/variable — keep as
`jsonb`).

---

## Resolved unknowns summary

| Topic | Resolution |
|---|---|
| How to compute | Read 2 JSON files → assemble in memory → Npgsql binary `COPY`, one transaction (R1) |
| Source reads | Per-file readers + manifest verify; local-only staged `mutashabihat/`; re-verify after (R2) |
| Ayah mapping | Resolve every `verse_key` → `quran_ayahs.id`; store `ayah_id` FK; 0 unresolved (hard) (R3) |
| Model shape | Two separate table sets (group+occurrence, directed links); no polymorphic merge (R4) |
| Group counters | Recompute from occurrences; keep raw counters in audit `jsonb`; report diffs (R5) |
| Occurrence grain | One row per (group, ayah, word-range); UNIQUE collapses the 1 duplicate → 3,557 (R6) |
| Representative | At most one rep occurrence/group; group `1782` allowed zero; warning records it (R7) |
| Similar links | Directed + faithful; no reverse rows; coverage stored raw (4 rows > 100 kept) (R8) |
| Word ranges / `match_words` | 1-based, stored unchanged; `match_words` `jsonb`; over-range = warning (R9) |
| `phrase_verses.json` | Excluded; verse→groups served by the occurrences `ayah_id` index (R10) |
| Trigger | New `import-mutashabihat` verb on the existing console host (R11) |
| Atomicity & gate | One transaction; validate before commit; rollback on failure; refuse-unless-empty/`--force` (R12) |
| Validation home | Queries in Infra; file/shape checks at assembly; constants/records in Abstractions (R13) |
| Report | Feature-local records + Markdown+JSON writer; `resources/report/mutashabihat/` (R14) |
| Types & keys | `int`/`smallint` per range; surrogate `int` ids; `jsonb` only for ragged values; FKs to `quran_ayahs` (R15) |
