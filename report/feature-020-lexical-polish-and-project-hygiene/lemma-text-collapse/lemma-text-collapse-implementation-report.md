# Feature 020 — Lemma-Text Collapse + `quran_lemma_analyses` Variant Layer

**Date (UTC):** 2026-07-04
**Branch:** `020-lexical-polish-and-project-hygiene`
**Scope:** Resolve the Phase 2A `import-morphology --enriched` failure
`duplicate key value violates unique constraint "IX_quran_lemmas_lemma_text"` without losing Corpus
lemma distinctions — the follow-up to the FirstWordOrder remediation.
**Predecessor:** `phase-2a-acceptance/enriched-first-word-order-remediation-report.md` (stopped at
`COPY quran_lemmas` line 258 on `UNIQUE(lemma_text)`).

**Verdict:** **Lemma-text collision resolved and verified** (unit + real-Postgres integration + build +
migration + real local rerun). The full-artifact rerun advances **past** `quran_lemmas` **and**
`quran_lemma_analyses` with zero `lemma_text` violations, then stops on a **separate, pre-existing**
blocker (affix segments with null POS) — reported here, not force-fixed.

---

## 1. Exact schema decision implemented

`quran_lemmas` stays the **Arabic display-lemma dimension** and keeps its `UNIQUE(lemma_text)` and
`UNIQUE(first_word_order_in_mushaf)` indexes unchanged. A new sibling table preserves the Corpus variants:

- **`quran_lemmas`** — **one row per distinct Arabic `lemma_text`.** Numeric-suffix buckwalter homographs
  that render to the same text (e.g. `EaSaA2` / `EaSaA` → `عَصَا`) collapse into this one row, so no second
  `COPY` row can violate `UNIQUE(lemma_text)`. `lemma_buckwalter` / `root_id` store the **representative**
  (earliest-occurrence) variant and are explicitly **not** the sole analytical truth when variants exist.
- **`quran_lemma_analyses`** (new) — **one row per distinct Corpus `lemma_buckwalter`.** Columns:
  `id`, `lemma_id`→`quran_lemmas` (NOT NULL, FK), `lemma_buckwalter` (UNIQUE), `root_id`→`quran_roots`
  (nullable FK), `head_pos` (nullable, first-occurrence head-STEM POS), `words_count`,
  `first_word_order_in_mushaf`, `first_location`. Indexes: unique `lemma_buckwalter`, plus `lemma_id`,
  `root_id`, `first_word_order_in_mushaf`.

Identity/linking rules:

- Word- and segment-level `lemma_id` (`quran_word_morphology`, `..._segments`) point to the **collapsed
  display lemma**, resolved via the segment's own buckwalter → its analysis → the display lemma. A
  buckwalter that is never any word's head has no analysis and resolves to null (existing null-safe rule).
- `root_id` / `head_pos` on an analysis reflect that variant's **first occurrence**; multi-POS variants are
  **not** flattened to a misleading single POS — occurrence-level POS stays authoritative on the segments.
- Occurrence-level truth (per-segment `pos`, `root_buckwalter`, `lemma_buckwalter`, `root_id`) is unchanged;
  the analysis table is a lemma-level breakdown, so no segment→analysis FK was added (kept intentionally
  simple per the task's requirement 7 fallback).

This satisfies the product constraints: `lemma_text` stays unique, no two display rows share Arabic text,
and no Corpus distinction is collapsed away.

## 2. Files changed

| File | Change |
| --- | --- |
| `domain/.../Words/Morphology/QuranLemmaAnalysis.cs` | **new** entity |
| `infrastructure/.../Configurations/Quran/Words/Morphology/QuranLemmaAnalysisConfiguration.cs` | **new** EF config (table, unique `lemma_buckwalter`, FKs, indexes) |
| `infrastructure/.../Persistence/QuranDashboardDbContext.cs` | `DbSet<QuranLemmaAnalysis> QuranLemmaAnalyses` |
| `infrastructure/.../Migrations/20260704102858_AddQuranLemmaAnalyses.cs` (+ `.Designer.cs`, snapshot) | **generated** migration (create table only) |
| `application/.../MorphologyImporting/MorphologySourceData.cs` | `ResolvedLemmaAnalysisDto`; optional `LemmaAnalyses` (nullable, default null → legacy path untouched) |
| `application/.../MorphologyImporting/MorphologyImportResult.cs` | `MorphologyImportTotals.LemmaAnalysisRows` |
| `infrastructure/.../MorphologyImporting/Enriched/EnrichedDimensionBuilder.cs` | rekey display lemma on `lemma_text`; per-buckwalter analysis index; representative buckwalter; `BuildResolvedLemmaAnalyses`; segment `lemma_id` resolves via analysis |
| `infrastructure/.../Enriched/EnrichedMorphologyImportSource.cs` | pass `LemmaAnalyses: build.ResolvedLemmaAnalyses` |
| `infrastructure/.../Persistence/.../MorphologyBulkCopier.cs` | `CopyLemmaAnalysesAsync` (after lemmas+roots) |
| `infrastructure/.../Persistence/.../EfBulkMorphologyWriter.cs` | call `CopyLemmaAnalysesAsync`; add analyses to the non-empty-targets guard |
| `infrastructure/.../Persistence/.../MorphologySql.cs` | `CountLemmaAnalysisRows`; add `quran_lemma_analyses` to `TruncateMorphologyTables` |
| `infrastructure/.../Persistence/.../MorphologyImportReportBuilder.cs` | gather + surface analyses count |
| `infrastructure/.../Reports/.../MarkdownJsonMorphologyReportWriter.cs` | totals row for `quran_lemma_analyses` |
| `tests/.../WordsMorphology/MorphologyImportTestFixture.cs` | snapshot `LemmaAnalysisRows`; `QueryLemmaAnalysesByTextAsync`; truncate lists |
| `tests/.../WordsMorphologyEnriched/EnrichedDimensionBuilderTests.cs` | +3 builder tests |
| `tests/.../WordsMorphologyEnriched/EnrichedMorphologyWriterIntegrationTests.cs` | +1 real-Postgres collision test + fixture |

No legacy pathway removed, no correction/QUL artifact deleted, no enriched JSON artifact edited, no
validation weakened, no commit.

## 3. Migration

- **Name:** `20260704102858_AddQuranLemmaAnalyses`
- **Generated files:** `20260704102858_AddQuranLemmaAnalyses.cs`, `.Designer.cs`,
  `QuranDashboardDbContextModelSnapshot.cs` (updated). Generated via `dotnet ef migrations add` (not
  hand-written); `Up` creates only `quran_lemma_analyses` + its FKs/indexes.
- **Applied:** **yes**, to the confirmed local target only (`Host=localhost` / `Database=quran_dashboard`,
  verified before applying; password never printed). `__EFMigrationsHistory` now includes it.

## 4. How the 15 known collisions are represented after the fix

From the collision inventory (`lemma-text-collisions/`), the artifact has **4,832** distinct head
`lemma_buckwalter` values but **4,817** distinct head `lemma_text` values — a difference of **15**.

- **Collapsed-to-one display lemma rows:** the **15** colliding `lemma_text` values each become **one**
  `quran_lemmas` row (down from the 2 rows that previously collided at COPY).
- **Variant/analysis rows created:** every distinct head buckwalter mints one `quran_lemma_analyses` row →
  **4,832** analyses total; each of the 15 collisions contributes **2** analysis rows under its single
  display lemma (30 analysis rows for the collided set).
- Per collision: exactly **2** analysis rows each (all 15 are 2-variant), e.g. `عَصَا` → analyses `EaSaA2`
  (root ع ص و, head POS N) and `EaSaA` (root ع ص ي, head POS V) — different roots **and** POS preserved,
  never merged. Full per-collision buckwalter/root/POS detail is in the collision inventory report.

## 5. Tests / build / migration results

| Suite | Filter | Result |
| --- | --- | --- |
| `dotnet build QuranDashboard.sln` | — | **PASS** (0 Warning, 0 Error) |
| Enriched morphology | `~WordsMorphologyEnriched` | **PASS 50/50** (was 46; +3 builder, +1 integration) |
| Legacy regression | `~WordsMorphologyImport\|~MorphologyAssembler\|~SegmentDimension` | **PASS 35/35** |

New tests:

- `Colliding_lemma_text_variants_collapse_to_one_display_lemma_but_keep_distinct_analyses` (builder) —
  one display lemma, two analyses, first_word_order unique after collapse.
- `Different_root_collision_variants_are_not_analytically_merged` (builder) — عَصَا's two roots stay
  distinct across analyses.
- `Different_pos_collision_variants_preserve_head_pos_per_variant` (builder) — مَٰلِك N vs PN preserved.
- `Colliding_lemma_text_variants_collapse_to_one_lemma_and_preserve_distinct_analyses` (real-Postgres) —
  two head buckwalters sharing `lemma_text` عَصَا import successfully → 1 lemma row + 2 analysis rows with
  distinct `root_id` and `head_pos`; `LemmaRows`/`LemmaAnalysisRows` asserted.

## 6. Real local rerun — lemma_text PASS; new distinct blocker

DB target verified **local** first. Sequence:

1. `validate-enriched-morphology` → **PASS 16/16** (record 77,432 / segment 128,219 / 0 fallback; boundary
   ayahs 2:181=14, 2:282=128, 8:6=12, 13:37=20, 8:6:12=2; corrected lemmas 41:44:16→شِفَاء, 11:29:17→مُّلَٰقُوا,
   2:102:41→مَرْء, 2:144:20→شَطْر).
2. `import-morphology --enriched` → **advances past the lemma_text collision.** COPY completes
   `quran_pos_tags`, `quran_roots`, `quran_lemmas`, **`quran_lemma_analyses`**, `quran_stems`,
   `quran_word_morphology`, then fails inside `CopySegmentsAsync` on a **different** constraint:

```
23503: insert or update on table "quran_word_morphology_segments"
       violates foreign key constraint "FK_quran_word_morphology_segments_quran_pos_tags_pos"
```

### Root cause of the new (out-of-scope) blocker

**50,195** affix segments (28,610 `PREFIX` + 21,585 `SUFFIX`; e.g. `بِ`, `ٱل`, `نَا`, `وَ`) carry
`pos: null` in the enriched artifact. `EnrichedDimensionBuilder.ProjectSegments` maps null POS to `""`,
and the segment `pos` FK to `quran_pos_tags.code` has no `""` row → COPY aborts. It surfaces only now
because `CopySegmentsAsync` is the **last** COPY step; the FirstWordOrder and lemma_text failures always
aborted earlier. `MORPH-POS-RESOLVES` / `ENRICHED-POS-RESOLVES` pass because both only flag **non-empty**
unknown codes, so blank affix POS slips through the pre-COPY gate. The schema/validation contract
(`CheckPosPresentNullSegmentPos` expects 0 null/blank segment POS) means this is a genuine
artifact/pathway gap, not a lemma-collapse issue.

### Why NOT force-fixed here

Resolving it is a separate product/data decision (supply affix POS in the artifact/importer, or change the
`pos` FK / null-POS validation) — outside this lemma-collapse task, exactly mirroring how the FirstWordOrder
remediation stopped at the lemma_text failure and reported it. No validation was weakened, no artifact
edited, no schema relaxed.

### DB state after the rerun

Outer transaction **rolled back cleanly.** Verified: `quran_words=83,668` (foundation intact); every
morphology-side table (`quran_word_morphology`, `..._segments`, `quran_roots`, `quran_lemmas`,
**`quran_lemma_analyses`**, `quran_stems`, `quran_pos_tags`) = **0**. No half-written state. Final import
counts cannot be reported — the import did not commit (the writer threw mid-COPY before the report writer
ran); the dry-validation report is under `phase-2a-acceptance/lemma-collapse-rerun/`.

## 7. Follow-up work

- **Separate blocker (next task):** decide how affix segments (null POS) are handled for the enriched
  pathway — supply the corpus affix POS in the artifact/importer, or adjust the segment `pos` FK /
  null-POS validation. Until then the full enriched import cannot complete past `CopySegmentsAsync`.
- **Lemmas Explorer UI/API (follow-up):** the new `quran_lemma_analyses` table is populated by the importer
  but **no read API/DTO surfaces it yet.** `LemmasController` / `EfLemmasReader` still read only
  `quran_lemmas`. Exposing the per-variant breakdown (buckwalter/root/POS/first-location under a display
  lemma) for the Lemmas Explorer detail view is additive follow-up work; it was intentionally not built in
  this schema/import task.

## 8. Boundaries respected

- Kept `quran_lemmas.lemma_text` UNIQUE; never created two display rows with the same Arabic text; did not
  blindly collapse away Corpus distinctions.
- Migration generated via EF tooling (not hand-written); applied to the confirmed local dev DB only.
- No legacy pathway removed; legacy regression 35/35 PASS. No QUL/correction file deleted, no enriched
  artifact edited, no validation weakened, **no commit.**
