# Phase 0 Research — Quran Words Display Tables Foundation

All decisions below are settled; there are **no open NEEDS CLARIFICATION** items. Each
entry: **Decision → Rationale → Alternatives rejected.** Facts about the existing system
were read from source (entities, EF configs, the importer, DI, tests).

---

## R1. Computation location: server-side `INSERT … SELECT`

**Decision.** Derive and populate all four tables with **server-side PostgreSQL
`INSERT … SELECT`** using window functions, executed as raw SQL on the EF/Npgsql
connection inside one transaction. No rows are pulled into application memory.

**Rationale.** The inputs already live in PostgreSQL; the work is grouping, `DISTINCT`,
and ranking — exactly what SQL is built for. Set-based SQL avoids materializing 77,432+
rows in C#, runs in seconds, and keeps the rebuild a thin orchestration around a few
deterministic statements. The existing readable partial index
(`IX_quran_words_readable_surah_ayah_word WHERE is_ayah_marker = false`) supports the
base scan.

**Alternatives rejected.**
- *Load into C#, compute, bulk `COPY` back* (mirrors the Feature 002 importer): wasteful
  for a DB-to-DB transform; adds a large in-memory model and serialization for no gain.
  (The Feature 002 importer uses `COPY` because its source is JSON files, not the DB.)
- *EF LINQ materialization + `AddRange`*: change-tracking 191k entities is slow and
  memory-heavy; not appropriate for a bulk derivation.

---

## R2. `word_order_in_mushaf` must be re-ranked, not reused from `quran_words.id`

**Decision.** Compute `word_order_in_mushaf = ROW_NUMBER() OVER (ORDER BY quran_words.id)`
over **readable words only**, yielding a gap-free `1..77,432`.

**Rationale.** `quran_words.id` is the global mushaf order across **all 83,668** rows
*including* the 6,236 ayah markers (Feature 002 data-model §5). Excluding markers leaves
gaps in `id`, so the contiguous display rank must be recomputed. `id` ascending is the
correct ordering key because it already encodes mushaf reading order.

**Alternatives rejected.** Using `id` directly (fails contiguity 1..77,432, FR-020);
ordering by `(page, line, line_word_order)` (equivalent to `id` here but redundant and
more fragile than the canonical `id`).

---

## R3. `word_order_in_surah` and `word_order_in_ayah`

**Decision.**
`word_order_in_surah = ROW_NUMBER() OVER (PARTITION BY surah_number ORDER BY id)`;
`word_order_in_ayah = ROW_NUMBER() OVER (PARTITION BY ayah_id ORDER BY word_number)`.
Validation additionally asserts `word_order_in_ayah` equals the source `word_number` for
readable words.

**Rationale.** Both are contiguous per-partition ranks (FR-021, FR-022). For readable
words, `word_number` is already the in-ayah order (markers are the last `word_number` of
each ayah — Feature 002 data-model §5), so re-ranking by `word_number` reproduces it
exactly while guaranteeing contiguity even if a future data quirk appeared. Asserting
equality with `word_number` is a cheap correctness guard.

**Alternatives rejected.** Trusting `word_number` blindly without re-rank/validation
(less robust); deriving order from layout columns (unnecessary).

---

## R4. Statistics: grouped CTE joined back (no `COUNT(DISTINCT)` window)

**Decision.** Compute per-display-key aggregates in a grouped CTE —
`occurrences_count = COUNT(*)`, `ayahs_count = COUNT(DISTINCT ayah_id)`,
`surahs_count = COUNT(DISTINCT surah_number)` — then join back onto each occurrence (for
the ordered tables) or use directly (for the unique tables).

**Rationale.** PostgreSQL does not allow `COUNT(DISTINCT …)` as a window function, so a
`GROUP BY` CTE is the correct, index-friendly form. Joining the aggregates back onto the
ordered rows gives each occurrence its group's denormalized counts (FR-010, FR-016–018).

**Alternatives rejected.** Per-row correlated subqueries (O(n²)); emulating distinct
counts with window tricks (opaque, slower).

---

## R5. Grouping key uses the exact stored text (no normalization)

**Decision.** Group strictly on the exact stored string — `text_uthmani` for the
tashkeel tables, `text_uthmani_simple` for the simple tables — with the database default
collation. No trimming, Unicode normalization, diacritic folding, or whitespace changes.

**Rationale.** FR-019 and the spec's scope guards: any normalization is a **search**
concern and search is explicitly out of scope. Grouping on the raw value keeps the
derivation faithful to the imported data; the **actual** unique counts are reported
(R7) so any encoding surprises surface for review rather than being silently "fixed".

**Alternatives rejected.** Normalizing before grouping (out of scope, risks altering
Quran text semantics); `citext`/case-insensitive collation (search behavior — excluded).

---

## R6. Unique tables derived from the same ranked base as the ordered tables

**Decision.** Compute the ranked readable base **once**; build the ordered tables from
it, and build the unique tables from the same base by picking, per display-key group, the
row with the minimum `word_order_in_mushaf` for the `first_*` fields.

**Rationale.** Guarantees by construction that `first_word_order_in_mushaf`,
`first_quran_word_id`, `first_location`, etc. are consistent with the ordered tables
(FR-023) and that "first occurrence = earliest mushaf order" holds. PostgreSQL
`DISTINCT ON (text) … ORDER BY text, word_order_in_mushaf` (or a join to the per-group
`MIN`) selects the first occurrence cleanly.

**Alternatives rejected.** Computing unique tables independently from `quran_words`
(risks drift from the ordered tables if ranking logic diverges).

---

## R7. Unique counts are derived and reported, never hardcoded

**Decision.** The validation suite reports the **actual** row counts of both unique
tables. The prior-project figures (~21,210 with tashkeel, ~14,783 without) are recorded
in the report as *informational expectations only* — a soft sanity note, never a hard
pass/fail threshold.

**Rationale.** FR-015. This DB's exact text encoding may differ from the prior project;
hardcoding would create false failures. The hard checks are structural (counts equal
`COUNT(DISTINCT text)` over readable words); the magnitude is reported for human review.

**Alternatives rejected.** Asserting `== 21,210 / == 14,783` (brittle, encoding-dependent).

---

## R8. Rebuild trigger: a new verb on the existing console host

**Decision.** Add a **verb dispatcher** to `tools/QuranDashboard.DataImporter/Program.cs`
with two verbs: `import-foundation` (the existing behavior, now explicit) and
`rebuild-words` (this feature). `rebuild-words` accepts `[--report-out <path>] [--force]`
and needs no `--source` (it reads the DB).

**Rationale.** The rebuild is an operator/CI batch op that must stay **off any HTTP
surface** (same reasoning that put the importer in a console host). The host already wires
DI (`AddApplication` + `AddInfrastructure`) and a report writer; reusing it avoids an
8th project and a second composition root. FR-024, FR-025.

**Alternatives rejected.**
- *New console project*: duplicates host/DI/wiring for no benefit; adds project sprawl.
- *Guarded API endpoint*: puts a destructive bulk op behind HTTP — violates "no API /
  no request-path work" (FR-035).
- *Migration `HasData`/seeder*: migrations must be schema-only (`Backend/CLAUDE.md`);
  191k derived rows must never live in a migration.

> **Back-compat note.** Introducing verbs changes the importer's current
> `--source`-first invocation. The quickstart and any automation must move to
> `import-foundation --source …`. Captured as a task and documented in quickstart.md.

---

## R9. Transactional rebuild with hard-gated validation before commit

**Decision.** Within one transaction: (if `--force`) `TRUNCATE` the four derived tables
`RESTART IDENTITY`; run the four `INSERT … SELECT`; run the validation queries; **commit
only if every hard check passes, otherwise roll back**. The source tables are never in
the truncate/write set. The orchestration mirrors `ImportQuranFoundationHandler`
(refuse-unless-empty → act → write report → map exit code).

**Rationale.** FR-026, FR-027, FR-028, FR-029, FR-031, FR-032. Validating inside the same
transaction as the writes is the only way to guarantee "nothing is written on failure"
without a separate cleanup path. `TRUNCATE … RESTART IDENTITY` is the cleanest reset for
identity-keyed derived tables and is transactional in PostgreSQL.

**Alternatives rejected.** Validate-after-commit-then-delete (leaves a window of bad
data; not atomic); `DELETE` instead of `TRUNCATE` (slower, keeps identity high-water mark).

---

## R10. Validation home: query execution in Infrastructure, constants in Abstractions

**Decision.** The validation **queries** run in the Infrastructure rebuilder (they are
inherently DB-bound — contiguity, grouping equality, first-occurrence joins). Known
**constants** (e.g. `ExpectedReadableWords = 77_432`) and the **check-result/result
records** live in `Application.Abstractions/Quran/Words/Display/`. The Infrastructure
rebuilder returns a populated `DisplayWordsRebuildResult` (totals + check results +
verdict + `persisted`); the Application handler interprets the verdict, writes the report,
and maps the CLI result.

**Rationale.** The invariants are query-bound, so they must execute where DB access lives
(Infrastructure references Application.Abstractions + Domain, not Application — confirmed
from the csproj graph). Keeping result/constants types in Abstractions keeps the contract
strongly typed and avoids leaking EF entities across boundaries. Correctness is proven
with **real-Postgres integration tests** (Testcontainers), which is the right tool for
query correctness per test-guard.

**Alternatives rejected.** A pure in-memory validator (cannot express the per-surah/
per-ayah/grouping invariants without the DB); holding an open transaction across the
Application boundary (awkward, leaky).

---

## R11. Report: feature-local result + a parallel Markdown+JSON writer

**Decision.** Introduce feature-local records (`DisplayWordsRebuildResult`,
`DisplayWordsTotals`, `DisplayWordsCheckResult`) and `IDisplayWordsReportWriter`,
implemented by `MarkdownJsonDisplayWordsReportWriter`, mirroring the Feature 002 report
pattern (`QuranImportValidationResult` + `MarkdownJsonImportReportWriter`).

**Rationale.** The existing `QuranImportValidationResult` is import-specific (carries
`ManifestVersion` and `ImportTotals` of surahs/ayahs/pages/lines/words) and lives under
`Quran/Import`. Reusing it would couple this feature to the import schema and drag in
irrelevant fields. A small, cohesive feature-local set keeps domain/feature grouping clean
(FR-033, BACKEND_STRUCTURE.md). The check-result shape intentionally parallels
`ImportCheckResult` (`Id, Severity, Expected, Observed, Passed`) for familiarity.

**Alternatives rejected.** Reusing `QuranImportValidationResult` (cross-feature coupling,
unused fields); promoting a shared generic result type now (premature; only two features).

---

## R12. Column types and keys

**Decision.**
- `word_order_in_mushaf`, `first_word_order_in_mushaf`, `quran_word_id`,
  `first_quran_word_id`, `occurrences_count`: **`int`** (mushaf order max 77,432 and FK
  ids max 83,668 exceed `smallint`; `occurrences_count` uses `int` for headroom though the
  most frequent token is well under 32,767).
- `surah_number`, `ayah_number`, `page_number`, `line_number`, `word_order_in_ayah`,
  `word_order_in_surah`, `ayahs_count`, `surahs_count`: **`smallint`** (all ≤ 32,767;
  per-surah word counts and `ayahs_count ≤ 6,236`, `surahs_count ≤ 114`).
- Ordered PK: `word_order_in_mushaf` (natural, contiguous). `quran_word_id` **UNIQUE** +
  **FK** → `quran_words.id`.
- Unique PK: surrogate `id` identity; display-text column **UNIQUE**;
  `first_word_order_in_mushaf` **UNIQUE**; `first_quran_word_id` **FK** → `quran_words.id`.

**Rationale.** Matches the Feature 002 typing convention (`smallint` where it fits, `int`
otherwise). Real FKs to the stable, never-truncated `quran_words` add integrity at no
cost (the parent is never in this feature's write set).

**Alternatives rejected.** Denormalized validated ids with no FK (the `line_number`
precedent exists, but here there is no circular-FK reason to avoid the FK).

---

## Resolved unknowns summary

| Topic | Resolution |
|---|---|
| How to compute | Server-side `INSERT … SELECT` + window functions, one transaction (R1) |
| Mushaf order | Re-rank readable words by `id` → 1..77,432 (R2) |
| In-surah / in-ayah order | `ROW_NUMBER` per partition; ayah order validated vs `word_number` (R3) |
| Statistics | Grouped CTE joined back; distinct ayahs/surahs (R4) |
| Grouping key | Exact stored text, no normalization (R5) |
| Unique ↔ ordered consistency | Unique derived from same ranked base, `MIN(word_order_in_mushaf)` (R6) |
| Unique counts | Derived + reported; ~21,210/~14,783 informational only (R7) |
| Trigger | `rebuild-words` verb on existing console host (R8) |
| Atomicity & gate | One transaction; validate before commit; rollback on failure (R9) |
| Validation home | Queries in Infra; constants/records in Abstractions (R10) |
| Report | Feature-local records + Markdown+JSON writer (R11) |
| Types & keys | `int`/`smallint` per range; FKs to `quran_words` (R12) |
