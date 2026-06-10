# Feature 003 — Word Identity Links Restructure (Implementation Plan)

**Date:** 2026-06-10
**Status:** Plan only. No code, no migrations, no commands, no commits.
**Companion analysis:** `Backend/report/feature-003-word-identity-links/feature-003-word-identity-links-restructure-report.md`
**Scope:** Backend Feature 003 restructure only.

This plan is implementation-ready: every change names the concrete file/construct it touches.
All counts (14,783 / 21,294 / anchors) were measured directly from the canonical import sources
over the authoritative readable set (77,432 words). Quranic data safety is called out throughout
(§ "Quranic Data Safety").

---

## 1. Executive Summary

Restructure Feature 003 so that "without-tashkeel" identity is the clean imlaei key, and so each
readable `quran_words` occurrence can be colored by identity from a single-table read:

1. **`quran_words_unique_simple`** changes its identity/grouping from `text_uthmani_simple` →
   **`word_key_imlaei_simple`** (15,826 → **14,783** rows), keeping representative Uthmani display
   fields.
2. **`quran_words`** gains two **nullable** link columns: `unique_tashkeel_word_id`,
   `unique_simple_word_id` (Option A from the analysis report). No enforced FK for now.
3. **`rebuild-words --force`** is extended to null + repopulate those links inside its existing
   single transaction, after building the unique tables.
4. **Hard validation** is extended with dataset-agnostic structural link checks (in the
   transactional gate) plus dataset-specific absolute-count/anchor checks (in real-import
   integration tests and the rebuild report).
5. **Tests** prove counts, link completeness, marker nullness, link consistency, anchors, and that
   Uthmani/QPC import and the Feature 002 `word_key_imlaei_simple` binding are unchanged.
6. **Dev flow** is reset → migrate → import → rebuild, documented; restart-identity reset is
   accepted for now with a future production note (not blocking).

`unique_tashkeel` stays grouped by raw `text_uthmani` (**21,294**), accepting known Uthmani-mark
splitting.

---

## 2. Confirmed Decisions

| # | Decision |
| --- | --- |
| 1 | With-tashkeel stats stay on `text_uthmani`. |
| 2 | Without-tashkeel identity = `word_key_imlaei_simple`. |
| 3 | Display stays Uthmani/Mushaf: `text_uthmani`, `text_uthmani_simple`, `qpc_glyph`. |
| 4 | Mushaf rendering reads `quran_words` (layout/glyph fields live there). |
| 5 | Every readable `quran_words` row exposes `quran_word_id`, `unique_tashkeel_word_id`, `unique_simple_word_id`. |
| 6 | Ayah-marker rows keep both unique IDs **null**. |
| 7 | **Option A** — nullable link columns directly on `quran_words`. |
| 8 | `TRUNCATE … RESTART IDENTITY` / reset accepted for this dev phase (Quran core data, no live id-dependent links yet). |
| 9 | Future production note required (natural key / remap / stable-id upsert) **before** real user/gate data depends on these ids — **not** a blocker now. |

Decision D1 (id stability) from the analysis report is treated **only** as a future/out-of-scope
production note (§14, §15), not a blocker.

---

## 3. Current State

- **Rebuild:** `SqlDisplayWordsRebuilder.RebuildAsync` runs, in one Npgsql transaction:
  `TruncateDerivedTables` → `InsertOrderedTashkeel` → `InsertOrderedSimple` →
  `InsertUniqueTashkeel` → `InsertUniqueSimple` → gather totals → `RunHardChecksAsync` →
  commit if all hard checks pass, else rollback. Behind `rebuild-words --force`
  (`tools/QuranDashboard.DataImporter/Program.cs`).
- **SQL:** all grouping/stat SQL lives in `DisplayWordsSql` (one `ReadableBase` CTE feeding four
  `INSERT … SELECT` strings + check strings).
- **Simple grouping today:** `text_uthmani_simple` (unique-simple unique index on
  `text_uthmani_simple`). The `ReadableBase` CTE does **not** currently select
  `word_key_imlaei_simple` or `qpc_glyph`.
- **Entities/configs:** `UniqueSimpleWord` has `TextUthmaniSimple`, `TextImlaeiSimple`, counts,
  `first_*` — **no** `TextUthmani`, **no** `QpcGlyph`, **no** `WordKeyImlaeiSimple`.
  `QuranWord` has **no** link columns.
- **Checks:** `DisplayWordsSql.CheckUnqCountDistinctSimpleText` counts
  `DISTINCT text_uthmani_simple`; `CheckStatMatchViolations` and `CheckFirstOccViolations` join
  simple by `text_uthmani_simple`. Warning constants in `DisplayWordsInvariants`
  (`InformationalUniqueTashkeel = 21_210`, `InformationalUniqueSimple = 14_783`).
- **Measured truth (canonical source, readable set):** unique simple by clean imlaei = **14,783**;
  unique tashkeel by `text_uthmani` = **21,294**; ordered = **77,432** each; total **83,668**;
  markers **6,236**.

---

## 4. Target State

- `quran_words_unique_simple` keyed by `word_key_imlaei_simple`, **14,783** rows, representative
  Uthmani display (`text_uthmani`, `text_uthmani_simple`, `qpc_glyph`), deterministic `first_*`.
- `quran_words_unique_tashkeel` unchanged grouping (`text_uthmani`), **21,294** rows.
- `quran_words.unique_tashkeel_word_id` / `unique_simple_word_id`: nullable; null after import;
  populated after a successful rebuild; non-null for every readable row; null for every marker.
- `quran_words_ordered_simple` stats computed on `word_key_imlaei_simple`; carries
  `word_key_imlaei_simple` for internal consistency and check joins; still 1 row per readable
  occurrence (77,432).
- `quran_words_ordered_tashkeel` unchanged (`text_uthmani`).
- Rebuild populates links inside its single transaction; hard checks gate the commit.

---

## 5. Schema / Migration Plan

Generate with EF tooling only — `./scripts/add-mig <Name>` (wraps
`dotnet ef migrations add --project infrastructure --startup-project api --context
QuranDashboardDbContext`). **Do not** hand-write or hand-edit migration/`.Designer`/snapshot
files. **Do not** run `dotnet ef database update` as part of implementation (dev applies it
manually per §12; integration tests apply via `Database.MigrateAsync()`).

**Recommended: two migrations for reviewability** (one cohesive migration is acceptable if
preferred):

### Migration 1 — `AddUniqueSimpleImlaeiIdentity`

On `quran_words_unique_simple`:
- Add `word_key_imlaei_simple text NOT NULL DEFAULT ''` (identity key).
- Add `text_uthmani text NOT NULL DEFAULT ''` (representative display, with tashkeel).
- Add `qpc_glyph text NOT NULL DEFAULT ''` (representative glyph; optional but recommended).
- **Drop** the unique index on `text_uthmani_simple`.
- **Add** a unique index on `word_key_imlaei_simple`.
- Keep the unique index on `first_word_order_in_mushaf`.

On `quran_words_ordered_simple`:
- Add `word_key_imlaei_simple text NOT NULL DEFAULT ''` (per-occurrence key; supports stat join
  + checks).

> `DEFAULT ''` only satisfies the ALTER on any pre-existing rows; a normal `rebuild-words --force`
> truncates + repopulates, so no row keeps the empty default after a rebuild.

### Migration 2 — `AddQuranWordIdentityLinks`

On `quran_words`:
- Add `unique_tashkeel_word_id integer NULL`.
- Add `unique_simple_word_id integer NULL`.
- Add filtered b-tree index
  `WHERE is_ayah_marker = false AND unique_simple_word_id IS NOT NULL` on `unique_simple_word_id`.
- Add the analogous filtered index on `unique_tashkeel_word_id`.
- **No** FK constraint to the unique tables (Decision 8: those tables are `TRUNCATE … RESTART
  IDENTITY` rebuilt; an enforced FK would fight the truncate for no gain). Links are logical,
  validated by hard checks.

Report after generation: migration names, generated files, build status, and that
`database update` was **skipped**.

---

## 6. Entity / Configuration Plan

### 6.1 `QuranWord` (Domain) + `QuranWordConfiguration`
- Add `public int? UniqueTashkeelWordId { get; set; }` and `public int? UniqueSimpleWordId { get; set; }`.
- Config: map `unique_tashkeel_word_id` / `unique_simple_word_id` (nullable, no `.IsRequired()`),
  add the two filtered indexes from Migration 2. No `HasOne/HasForeignKey` to the unique tables.
- Leave all existing foundation properties and the Feature 002 `WordKeyImlaeiSimple` mapping
  untouched.

### 6.2 `UniqueSimpleWord` (Domain) + `UniqueSimpleWordConfiguration`
- Add `WordKeyImlaeiSimple` (string), `TextUthmani` (string, representative), `QpcGlyph` (string,
  representative; optional).
- Keep `TextUthmaniSimple` (now a **representative**, no longer unique), `TextImlaeiSimple`,
  counts, `first_*`.
- Config: map the new columns required; move the unique index from `TextUthmaniSimple` to
  `WordKeyImlaeiSimple`; keep unique index on `FirstWordOrderInMushaf`; keep the FK
  `first_quran_word_id → quran_words`.

### 6.3 `OrderedSimpleWord` (Domain) + `OrderedSimpleWordConfiguration`
- Add `WordKeyImlaeiSimple` (string); map `word_key_imlaei_simple` required. Existing
  per-occurrence display columns and the `quran_word_id` unique index/FK stay.

### 6.4 Unchanged
- `UniqueTashkeelWord` / `OrderedTashkeelWord` and their configs: **no change** (still
  `text_uthmani`-based).
- `DbContext` sets already expose all four derived tables; no new `DbSet` needed unless Option B
  were chosen (it is not).

---

## 7. Rebuild SQL Plan (`DisplayWordsSql` + `SqlDisplayWordsRebuilder`)

### 7.1 `ReadableBase` CTE
Add `w.word_key_imlaei_simple` and `w.qpc_glyph` to the `readable` CTE projection (so unique/
ordered simple inserts can group/display by them). Marker exclusion (`WHERE is_ayah_marker =
false`) is unchanged — markers never enter any derived table.

### 7.2 `InsertUniqueSimple`
- `stats_simple`: `GROUP BY word_key_imlaei_simple`.
- `first_occ`: `DISTINCT ON (word_key_imlaei_simple) … ORDER BY word_key_imlaei_simple,
  word_order_in_mushaf` (deterministic representative = first Mushaf occurrence).
- Insert columns add `word_key_imlaei_simple`, representative `text_uthmani`, `qpc_glyph`; keep
  representative `text_uthmani_simple`, `text_imlaei_simple`, counts, `first_*`.
- Join `stats_simple` on `word_key_imlaei_simple`.

### 7.3 `InsertOrderedSimple`
- `stats_simple`: `GROUP BY word_key_imlaei_simple`; join `ON s.word_key_imlaei_simple =
  r.word_key_imlaei_simple`.
- Insert column list adds `word_key_imlaei_simple` (per-occurrence value from `ranked`).
- Per-row display columns (`text_uthmani_simple`, `text_imlaei_simple`) stay as the occurrence's
  own values; the `occurrences/ayahs/surahs` counts now reflect the imlaei group.

### 7.4 New link SQL constants
- `NullQuranWordLinks`:
  `UPDATE quran_words SET unique_tashkeel_word_id = NULL, unique_simple_word_id = NULL`
  (run inside the transaction before/with truncate; on rollback it reverts atomically).
- `UpdateUniqueTashkeelLinks`:
  `UPDATE quran_words w SET unique_tashkeel_word_id = u.id
   FROM quran_words_unique_tashkeel u
   WHERE u.text_uthmani = w.text_uthmani AND w.is_ayah_marker = false`.
- `UpdateUniqueSimpleLinks`:
  `UPDATE quran_words w SET unique_simple_word_id = u.id
   FROM quran_words_unique_simple u
   WHERE u.word_key_imlaei_simple = w.word_key_imlaei_simple AND w.is_ayah_marker = false`.

### 7.5 `RebuildAsync` order (inside the existing single transaction)
1. `NullQuranWordLinks` (clear stale links).
2. `TruncateDerivedTables` (when `force`).
3. `InsertUniqueTashkeel`.
4. `InsertUniqueSimple` (now imlaei-keyed).
5. `InsertOrderedTashkeel`, `InsertOrderedSimple`.
   *(Ordered inserts may run before unique inserts as today; the only hard ordering requirement is
   that both unique tables exist before step 6–7.)*
6. `UpdateUniqueTashkeelLinks`.
7. `UpdateUniqueSimpleLinks`.
8. Existing hard checks (`RunHardChecksAsync`) + new structural link checks (§10).
9. Commit iff all hard checks pass; else rollback (unchanged gate logic).

`CommandTimeout = 600s` is retained; the two extra `UPDATE` passes over 77,432 rows are well
within budget.

---

## 8. Unique Simple Redesign Plan

- **Identity:** `word_key_imlaei_simple`, unique index, **14,783** rows expected.
- **Display (representative, from first Mushaf occurrence):** `text_uthmani` (reverent, with
  tashkeel), `text_uthmani_simple`, `qpc_glyph`.
- **Stats:** `occurrences_count`, `ayahs_count`, `surahs_count` computed over the imlaei group.
- **Provenance:** `first_*` columns unchanged, deterministic via
  `DISTINCT ON … ORDER BY word_key_imlaei_simple, word_order_in_mushaf`.
- **Identity-vs-display divergence (document for admin UI):** one imlaei identity may map to one
  representative Uthmani label while underlying Uthmani vocalizations vary — e.g. identity
  `الرحمان` (45 occurrences) displays representative Uthmani `الرحمن`. Surface
  `word_key_imlaei_simple` in **admin** DTOs (future API work) so curators see why two
  Uthmani-looking forms share one identity; the public Mushaf DTO does not need it.

`unique_tashkeel` is untouched: grouping `text_uthmani`, **21,294** rows, accepted Uthmani-mark
splitting (§14).

---

## 9. QuranWord Identity Link Plan

- Columns: `unique_tashkeel_word_id int?`, `unique_simple_word_id int?` (nullable).
- Lifecycle: NULL after `import-foundation`; populated by `rebuild-words`; readable → both
  non-null; markers → both null (markers never matched by the `is_ayah_marker = false` UPDATE
  predicates, and are nulled by `NullQuranWordLinks`).
- No enforced FK (Decision 8); logical reference validated by `LINK-RESOLVES` / `LINK-CONSISTENT`.
- Filtered indexes for the page-coloring read path (`WHERE is_ayah_marker = false AND
  unique_*_word_id IS NOT NULL`).
- `SRC-UNTOUCHED`: the existing check is **count-based** (`COUNT(*)` of words/ayahs/surahs), and an
  `UPDATE` does not change row counts, so it still passes unchanged. *(Optional hardening: also
  assert a checksum of the foundation text columns is unchanged; not required.)*

---

## 10. Hard Validation Checks

Split by where each check can be proven, because the transactional rebuild gate must pass on
**any** dataset (including the synthetic seed used by most integration tests), while absolute
counts/anchors are specific to the canonical Quran dataset.

### 10.1 Structural checks — in the transactional gate (`RunHardChecksAsync`), dataset-agnostic
Keep all existing checks (`ORD-COUNT`, `ORD-READABLE`, `ORD-NO-MARKERS`, `ORD-BIJECTION`,
`ORD-*-CONTIG`, `STAT-MATCH`, `FIRST-OCC`, `SRC-UNTOUCHED`, `UNQ-COUNT`), updating the simple-side
SQL to the imlaei key:
- `CheckUnqCountDistinctSimpleText` → `COUNT(DISTINCT word_key_imlaei_simple)`.
- `CheckStatMatchViolations` (simple portions) → group/join by `word_key_imlaei_simple`.
- `CheckFirstOccViolations` (simple portion) → join `ordered_simple` ↔ `unique_simple` on
  `word_key_imlaei_simple`.
- `UNQ-COUNT` stays relative: `unique_simple` rows == `COUNT(DISTINCT word_key_imlaei_simple)` and
  `unique_tashkeel` rows == `COUNT(DISTINCT text_uthmani)` — true for any dataset.

Add new structural link checks (hard):

| Id | Assertion (expected 0 violations) |
| --- | --- |
| `LINK-READABLE-COMPLETE` | readable rows with `unique_tashkeel_word_id IS NULL OR unique_simple_word_id IS NULL` = 0 |
| `LINK-MARKERS-NULL` | marker rows with either id non-null = 0 |
| `LINK-RESOLVES` | non-null `unique_*_word_id` with no matching unique row = 0 (both keys) |
| `LINK-CONSISTENT` | readable rows where linked `unique_simple.word_key_imlaei_simple <> w.word_key_imlaei_simple`, or linked `unique_tashkeel.text_uthmani <> w.text_uthmani` = 0 |

### 10.2 Warnings — keep informational
- `UNQ-EXPECT-SIMPLE` (14,783): now **matches** reality — warning passes.
- `UNQ-EXPECT-TASHKEEL` (21,210): still deviates from 21,294 — warning fires, **accepted** (§14).

### 10.3 Absolute-count + anchor checks — in real-import integration tests + the rebuild report
These are canonical-dataset truths and would (correctly) fail on synthetic seed, so they are
asserted against the real import (via `ImportTestFixture`), not in the generic gate:

| Assertion | Expected |
| --- | --- |
| total `quran_words` | 83,668 |
| readable / markers | 77,432 / 6,236 |
| ordered tashkeel / ordered simple | 77,432 / 77,432 |
| unique tashkeel | 21,294 |
| unique simple | 14,783 |
| `الله` unique-simple occurrences | 2,155 |
| `العظيم` unique-simple occurrences | 36 |
| `الرحمان` unique-simple occurrences | 45 (+ representative Uthmani display for الرحمن) |
| `ال ياسين` | remains 1 |
| `5:52:12` | remains `دايرة` |

The canonical absolute counts (14,783 / 21,294) should also be recorded as constants
(`DisplayWordsInvariants`) so the real-data tests and the report share one source of truth.

---

## 11. Test Plan

Follow `test-guard`: test behavior, mocks only at boundaries, data-driven `[Theory]` for variants,
real entities/DTOs, real Postgres where persistence is the subject, Quran-safe data.

### 11.1 Prerequisite — synthetic seed
Update `DisplayWordsSyntheticSeed` so every seeded readable `QuranWord` sets a deterministic
**synthetic** `WordKeyImlaeiSimple` (and `QpcGlyph`), since `unique_simple` now groups by it. Use
safe placeholder tokens (no real Quranic verse text), consistent with existing seed practice.

### 11.2 Integration — synthetic seed (Testcontainers, dataset-agnostic)
Extend the existing `WordsDisplay` test suite:
- Rebuild succeeds; ordered counts == readable; `unique_simple` rows == distinct synthetic key;
  `unique_tashkeel` rows == distinct `text_uthmani`.
- `LINK-READABLE-COMPLETE`: all readable rows have both ids non-null.
- `LINK-MARKERS-NULL`: all marker rows have both ids null.
- `LINK-RESOLVES` / `LINK-CONSISTENT`: no dangling, keys match.
- Idempotency: second `--force` rebuild yields identical counts/links; refusal without `--force`
  still holds (existing tests).

### 11.3 Integration — real import (Testcontainers + `ImportTestFixture`)
New test class (mirrors `ImlaeiCleanKeyImportTests` pattern: import-foundation then rebuild-words):
- Absolute counts: 83,668 / 77,432 / 6,236 / 77,432 / 77,432 / 21,294 / **14,783**.
- Anchors via `[Theory]`: `الله`=2,155, `العظيم`=36, `الرحمان`=45 (+ representative Uthmani),
  `ال ياسين`=1, `5:52:12`→`دايرة`.
- Every readable row has both links; markers null; links resolve + are consistent.

### 11.4 Regression
- Raw `text_uthmani` / `text_uthmani_simple` / `text_imlaei_simple` / `qpc_glyph` on `quran_words`
  unchanged by rebuild (extend the existing source-untouched test).
- Feature 002 `word_key_imlaei_simple` binding test (`ImlaeiCleanKeyImportTests`) still passes.
- `ValidatorRulesTests` / import validation suites unaffected (no Feature 002 import rule change).

### 11.5 Unit
- Optional: assert `DisplayWordsInvariants` constants (e.g. `ExpectedReadableWords`, the new
  canonical unique constants). Avoid testing framework guarantees or trivial getters (test-guard
  Rule 4/7). No mocking of entities/DTOs (Rule 8).

---

## 12. Dev Reset / Reseed Workflow

After implementation, recommended **local/dev** flow (documented; run by the developer, not by
this plan):

1. **Drop/reset** the dev database (e.g. `dotnet ef database drop -f --project infrastructure
   --startup-project api`).
2. **Apply migrations** (`dotnet ef database update --project infrastructure --startup-project
   api --context QuranDashboardDbContext`).
3. **Import foundation**
   (`dotnet run --project tools/QuranDashboard.DataImporter -- import-foundation --source
   ../resources/import-sources/quran-foundation --report-out ../resources/report`).
4. **Rebuild words**
   (`dotnet run --project tools/QuranDashboard.DataImporter -- rebuild-words --force --report-out
   ../resources/report`).
5. **Run audit/report checks** (review the generated rebuild report + the real-import integration
   tests).

**Why acceptable now:** Quran core data is fully reproducible from the canonical, reviewed import
sources; no live user/gate data depends on the unique ids yet; `RESTART IDENTITY` reset + reseed
from the same canonical data is safe in development (Decision 8).

**Future production note (NOT blocking, NOT in scope):** before any real user/gate data depends on
`unique_simple_word_id` / `unique_tashkeel_word_id`, adopt one of: (a) store a stable natural key
(`word_key_imlaei_simple`) alongside the id and link on it; (b) a remap step after each rebuild;
or (c) a stable-id upsert (build unique tables without `RESTART IDENTITY`, preserving ids by
natural key). Choose at that time; do not block the current implementation.

---

## 13. Reports and Verification

Produce `Backend/report/feature-003-word-identity-links-implementation-report.md` with:
- **Files changed** (entities, configs, `DisplayWordsSql`, `SqlDisplayWordsRebuilder`,
  `DisplayWordsInvariants`, synthetic seed, tests).
- **Migration names** (`AddUniqueSimpleImlaeiIdentity`, `AddQuranWordIdentityLinks`) + generated
  files; confirm `database update` skipped (applied only in disposable test containers).
- **Commands run** (`./scripts/add-mig …`, `dotnet build`, `dotnet test`) + results.
- **Counts** (83,668 / 77,432 / 6,236 / 77,432 / 77,432 / 21,294 / 14,783) from the real-import
  test.
- **Anchor results** (الله 2,155; العظيم 36; الرحمان 45 + representative Uthmani; ال ياسين 1;
  5:52:12 → دايرة).
- **Hard-check verdict** (structural gate all-pass; warnings noted: tashkeel 21,294 vs 21,210
  informational).
- **Final verdict:** PASS / PASS WITH NOTES / FAIL.

Before delivery, run the CLAUDE.md clean-code self-check and the test-code self-check, and the
formal `engineering-review` (with `test-guard` for the test portion).

---

## 14. Out of Scope

Explicitly **not** part of this work:
- Mushaf page API implementation and any API DTOs.
- Frontend coloring implementation.
- Gate/Topic schema or linking changes.
- Stable-id upsert strategy / production remap tooling.
- Changing Uthmani or with-tashkeel normalization (Uthmani-mark splitting in `unique_tashkeel`
  is accepted as-is).
- Any change to Feature 002 import behavior beyond the already-completed
  `word_key_imlaei_simple` binding.
- Decision D1 (id stability) beyond documenting the future production note.

---

## 15. Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| Absolute-count/anchor checks are dataset-specific and would break synthetic-seed rebuild tests if placed in the gate. | Keep the transactional gate **structural/relative**; assert absolutes/anchors in real-import integration tests + the report (§10.3). |
| Synthetic seed lacks `word_key_imlaei_simple` → `unique_simple` rebuild collapses to empty/garbage in tests. | Update `DisplayWordsSyntheticSeed` to set a synthetic key first (§11.1). |
| `text_uthmani_simple` is no longer unique within `unique_simple` (representative may repeat across imlaei groups). | Drop its unique index; move uniqueness to `word_key_imlaei_simple` (§5, §6.2). |
| Identity-vs-display divergence (`الرحمان` identity, `الرحمن` display) confuses curators. | Document; expose `word_key_imlaei_simple` in admin DTOs (future API) (§8). |
| Two extra `UPDATE` passes lengthen rebuild. | Set-based `UPDATE … FROM` on indexed keys, 600s timeout; ~77k rows — negligible. |
| Canonical constants (14,783 / 21,294) drift if source encoding changes. | Centralize in `DisplayWordsInvariants`; treat deviation as investigate; tests + report surface it. |
| Restart-identity reset invalidates ids across rebuilds. | Accepted for dev (Decision 8); future production note (§12) — not blocking. |
| Rebuild now writes `quran_words` link columns; could be read as "source touched." | `SRC-UNTOUCHED` is count-based and still passes; optional checksum hardening noted (§9). |
| Migration partial-apply / wrong order. | EF-tool-generated, additive, reviewed; dev applies via §12; tests apply via `MigrateAsync`. |

---

## 16. Phase-by-Phase Implementation Tasks

**Phase 1 — Unique-simple identity switch (no links yet)**
1. `ReadableBase` CTE: add `word_key_imlaei_simple`, `qpc_glyph`.
2. `InsertUniqueSimple`: regroup on `word_key_imlaei_simple`; representative `text_uthmani` /
   `text_uthmani_simple` / `qpc_glyph`; deterministic `first_*`.
3. `InsertOrderedSimple`: stats on `word_key_imlaei_simple`; add `word_key_imlaei_simple` column.
4. `UniqueSimpleWord` + config: add `WordKeyImlaeiSimple` (unique), `TextUthmani`, `QpcGlyph`;
   move unique index. `OrderedSimpleWord` + config: add `WordKeyImlaeiSimple`.
5. Update checks: `CheckUnqCountDistinctSimpleText`, `CheckStatMatchViolations`,
   `CheckFirstOccViolations` to the imlaei key.
6. Generate **Migration 1** (`./scripts/add-mig AddUniqueSimpleImlaeiIdentity`); do not apply.

**Phase 2 — Identity link schema**
7. `QuranWord` + config: add nullable `UniqueTashkeelWordId` / `UniqueSimpleWordId` + filtered
   indexes; no FK.
8. Generate **Migration 2** (`./scripts/add-mig AddQuranWordIdentityLinks`); do not apply.

**Phase 3 — Rebuilder link population**
9. Add `NullQuranWordLinks`, `UpdateUniqueTashkeelLinks`, `UpdateUniqueSimpleLinks` to
   `DisplayWordsSql`.
10. Wire them into `SqlDisplayWordsRebuilder.RebuildAsync` per §7.5 (inside the existing
    transaction).

**Phase 4 — Hard checks**
11. Add `LINK-READABLE-COMPLETE`, `LINK-MARKERS-NULL`, `LINK-RESOLVES`, `LINK-CONSISTENT` to
    `DisplayWordsSql` + `RunHardChecksAsync`. Keep warnings; add canonical absolute constants to
    `DisplayWordsInvariants`.

**Phase 5 — Tests**
12. Update `DisplayWordsSyntheticSeed` (synthetic `word_key_imlaei_simple` / `qpc_glyph`).
13. Extend synthetic-seed integration tests (links complete/null/resolve/consistent, counts).
14. Add real-import integration test (absolute counts + anchors + links).
15. Regression: source-untouched (incl. `qpc_glyph`), Feature 002 binding still green.

**Phase 6 — Build, verify, report**
16. `dotnet build` (0 warnings) and `dotnet test` (all green).
17. Run clean-code + test-code self-checks and `engineering-review`/`test-guard`.
18. Write `feature-003-word-identity-links-implementation-report.md` (§13).

**Phase 7 — Dev reset / reseed (developer-run, documented)**
19. Reset → migrate → import-foundation → rebuild-words --force → audit (§12).

---

## 17. Final Implementation Recommendation

Implement in the phase order of §16. Concretely:

1. Switch `unique_simple` to `word_key_imlaei_simple` (→ **14,783**), keeping deterministic
   representative Uthmani display and `first_*` provenance.
2. Add nullable `unique_tashkeel_word_id` / `unique_simple_word_id` to `quran_words` with filtered
   indexes and **no** enforced FK (Decision 8).
3. Populate links inside `rebuild-words --force`'s single transaction (null → truncate → build
   unique → build ordered → update links → checks → commit-on-all-pass).
4. Gate the commit with **structural** link checks; prove **absolute counts + anchors** in
   real-import integration tests and the report.
5. Keep `unique_tashkeel` on raw `text_uthmani` (**21,294**), accepting Uthmani-mark splitting.
6. Use the dev reset/reseed flow now; carry the production stable-id note forward (not blocking).

### Quranic Data Safety

Throughout: **display stays Uthmani/Mushaf** (`text_uthmani`, `text_uthmani_simple`, `qpc_glyph`);
**identity/statistics move to the clean imlaei key** (`word_key_imlaei_simple`); **occurrence
coloring uses ids** (`quran_word_id`, `unique_*_word_id`) — never raw word text. Anchor assertions
use **single word-identity forms only** (no verse passages); synthetic seed data uses safe
placeholders; no Quranic content is invented and no Uthmani/QPC text is normalized beyond the
already-reviewed clean-key derivation.
