# Feature 020 — Enriched Morphology FirstWordOrder Remediation Report

**Date (UTC):** 2026-07-04
**Branch:** `020-lexical-polish-and-project-hygiene`
**Scope:** Focused remediation of the Phase 2A `import-morphology --enriched` failure
(`duplicate key … "IX_quran_lemmas_first_word_order_in_mushaf"`).
**Predecessor:** `phase-2a-acceptance/phase-2a-acceptance-report.md` (FAIL at COPY `quran_lemmas` line ~35).

**Verdict:** **Assigned FirstWordOrder defect fixed and verified** (unit + real-Postgres integration + build).
The real local rerun advances **past** the FirstWordOrder constraint entirely and then stops on a
**separate, pre-existing** constraint (`UNIQUE(lemma_text)`) — reported here, not force-fixed.

---

## 1. Root-cause confirmation (the assigned defect)

`EnrichedDimensionBuilder.BuildState.Add` iterated **every STEM segment** of every word and called
`ResolveOrCreate{Root,Lemma,Stem}` on each. A word with two STEM segments carrying **distinct** lemma
Buckwalter values minted **two** lemma dimension rows in the same pass, both stamped
`FirstWordOrder = wordOrder` (the word's `quran_words.id`). All three morphology dimension tables carry a
**UNIQUE** index on `first_word_order_in_mushaf` (`QuranDashboardDbContextModelSnapshot.cs`: lemmas 1663–64,
roots 1706–07, stems 1741–42), so the `COPY` aborted with `23505`.

First colliding word (word order 52) = **`2:3:6`** (`مِمَّا` = `مِن` + `مَا`), lemma Buckwalter `min` / `maA`.
Confirmed against the staged artifact: **483** words carry >1 distinct STEM lemma; only word order 52 first
collides, matching the original `COPY quran_lemmas, line ~35`. The same latent risk existed for **roots and
stems** (both UNIQUE on `first_word_order_in_mushaf`).

## 2. Files changed

| File | Change |
| --- | --- |
| `Backend/infrastructure/.../MorphologyImporting/Enriched/EnrichedDimensionBuilder.cs` | Two-phase dimension resolution (see §3). Removed the per-STEM-segment minting; removed the now-dead `ResolveNonStemRoot`. |
| `Backend/tests/.../WordsMorphologyEnriched/EnrichedDimensionBuilderTests.cs` | +2 tests (collision guard + reuse path); corrected 1 stale assertion that encoded the stem-side bug. |
| `Backend/tests/.../WordsMorphologyEnriched/EnrichedMorphologyWriterIntegrationTests.cs` | +1 real-Postgres test + multi-STEM fixture reproducing the exact COPY failure shape. |

No other files touched. **No schema, migration, DTO, writer, legacy pathway, correction artifact, or JSON
artifact was modified.**

## 3. Exact logic change

Minting and resolution are now **two phases** (both still single stream-read of the 96 MB file; the word
projections were already fully buffered in memory, so phase 2 re-reads nothing):

- **Phase 1 — mint from the head STEM only.** For each word, only the head (lowest-`segmentNumber`) STEM
  segment mints root/lemma/stem dimensions. Each word therefore mints **at most one** new row per dimension,
  stamped with that word's unique order — so no two rows can ever share a `FirstWordOrder`. This honours the
  UNIQUE index by construction and is applied **consistently to roots, lemmas, and stems** (requirement 6).
- **Phase 2 — resolve every segment by value lookup (`ResolveSegmentDimensions`).** Once all heads have
  minted, each segment's `root_id`/`lemma_id`/`stem_id` is filled by looking its Buckwalter / stem-text up in
  the now-complete indices. A secondary STEM resolves to a dimension even when that dimension's head occurs
  **later** in word order (mirrors `MorphologyAssembler.ResolveLemmaId`, lookup-only). Nothing is minted in
  phase 2, so no `FirstWordOrder` is ever fabricated; a value that is never any word's head stays `null`
  (requirement 4). No fake `FirstWordOrder` is invented (requirement 5).

Two-phase (rather than single-pass lookup) is required so the real import also satisfies the post-COPY hard
checks `SEG-LEMMA-ID-MULTI-STEM-RESOLVES` / `SEG-ROOT-RESOLVES`: e.g. `2:3:6`'s secondary `مَا` resolves to
the standalone `مَا` head that appears later in word order.

## 4. Tests added / updated

- `Multi_stem_word_never_mints_two_dimensions_sharing_a_first_word_order` (builder, **RED before fix** —
  failed with `item 52 is not unique`; GREEN after).
- `Secondary_stem_dimension_reuses_an_already_minted_dimension` (builder, requirement 3 — lookup/reuse).
- `Multi_stem_word_with_distinct_lemmas_imports_without_violating_the_first_word_order_unique_index`
  (real-Postgres writer integration, **reproduces the exact Phase 2A COPY failure**; import succeeds after
  the fix; asserts 5 morphology rows, 6 segment rows, 5 lemmas — head-only, no 6th colliding lemma).
- Corrected the stale assertion in `Segment_dimension_ids_resolve_value_based_per_segment`
  (`secondaryStem.StemId` now expected `null`, not non-null — the old assertion encoded the stem-side
  duplicate-`FirstWordOrder` bug the DB rejects).

## 5. Test / build results

| Suite | Filter | Result |
| --- | --- | --- |
| `dotnet build QuranDashboard.sln` | — | **PASS** (0 Warning, 0 Error) |
| Enriched morphology | `FullyQualifiedName~WordsMorphologyEnriched` | **PASS 46/46** (was 43; +3) |
| Legacy regression | `~WordsMorphologyImport\|~MorphologyAssembler\|~SegmentDimension` | **PASS 35/35** |

## 6. Real `import-morphology --enriched` rerun — YES

DB target verified **local/dev** before acting: `Host=localhost;Port=5432;Database=quran_dashboard;User=postgres`
(from the shared user-secrets `9b57d4a2-…`). Pre-run state = **post-foundation, pre-morphology**
(`quran_words=83,668`; all morphology tables `0`). **No destructive reset run** (none needed).

1. `validate-enriched-morphology` → **PASS 16/16** (record 77,432 / segment 128,219 / 0 fallback; boundary
   ayahs `2:181`=14, `2:282`=128, `8:6`=12, `13:37`=20, `8:6:12`=2 segments; corrected lemmas `41:44:16`→`شِفَاء`,
   `11:29:17`→`مُّلَٰقُوا`, `2:102:41`→`مَرْء`, `2:144:20`→`شَطْر`).
2. `import-morphology --enriched --force` → see §7.

## 7. Rerun outcome — FirstWordOrder PASS; new distinct constraint FAIL

**The assigned FirstWordOrder collision is resolved.** The COPY now advances from the old failure point
(`quran_lemmas` line ~35) to `quran_lemmas` **line 258**, where it stops on a **different** constraint:

```
23505: duplicate key value violates unique constraint "IX_quran_lemmas_lemma_text"
  Where: COPY quran_lemmas, line 258
  at MorphologyBulkCopier.CopyLemmasAsync(...) line 76
```

### Root cause of the new (out-of-scope) failure

`quran_lemmas.lemma_text` carries a **UNIQUE** index (`QuranDashboardDbContextModelSnapshot.cs:1666–67`).
Lemma identity keys on `lemma_buckwalter` (plan §4), but the artifact distinguishes homograph lemmas with a
numeric-suffixed Buckwalter (`maE` / `maE2`) that render to the **same** vocalized `lemma_text`. Two distinct
lemma rows then share `lemma_text` → the second row violates `UNIQUE(lemma_text)`. This is exactly plan
**risk #2** ("bridge lemma vocalization artifacts … dedupe on `lemma_buckwalter`, not display text") colliding
with the schema's `UNIQUE(lemma_text)` — a **separate design tension**, not the FirstWordOrder defect.

Artifact scan (head lemmas, 77,432 records): **15** `lemma_text` collisions, first at **COPY row 258**:

| COPY row | lemma_text | Buckwalter (later) @ word order | collides with (earlier) |
| ---: | --- | --- | --- |
| 258 | `مَع` | `maE2` @ 682 (`2:41:6`) | `maE` @ 191 (`2:14:13`) |
| 368 | `عَصَا` | `EaSaA` @ 1028 (`2:61:57`) | `EaSaA2` @ 949 (`2:60:7`) |
| 645 | `حَيْث` | `Hayov2` @ 2753 (`2:144:15`) | `Hayov` @ 592 (`2:35:10`) |
| … | (12 more) | | |

**Roots are unaffected** (`root_text`: 1,642 Buckwalters → 1,642 distinct texts, **0** collisions).
**Stems are unaffected** (stem identity already keys on `stem_text`; homographs collapse to one row by design).

### Why this was NOT force-fixed here

Resolving it requires a decision outside a mechanical FirstWordOrder remediation, and each option is
explicitly out of the task's guardrails:
- keying lemma identity on `lemma_text` (merging the 15 Buckwalter-homograph pairs) overturns the
  **signed-off** "lemma identity = Buckwalter" decision (plan §4) — a product/data-modeling choice;
- editing the enriched artifact — **forbidden**;
- dropping/altering `UNIQUE(lemma_text)` — a **migration**, **forbidden**.

Per the failure rules, the run was stopped at first failure; nothing was forced and **no Quran validation was
weakened**.

### DB state after the rerun

Outer transaction **rolled back cleanly**. `quran_words=83,668` (foundation intact); every morphology-side
table (`quran_word_morphology`, `…_segments`, `quran_roots`, `quran_lemmas`, `quran_stems`, `quran_pos_tags`)
= **0**. No half-written state. Enriched final counts (words / segments / roots / lemmas / stems / POS tags)
**cannot be reported** — the import did not commit. No report-out file for the import step exists (the writer
threw mid-COPY before `MarkdownJsonMorphologyReportWriter` ran); the dry-validation report was written to
`phase-2a-acceptance/remediation-rerun/`.

### Recommended follow-up (separate task)

Decide lemma identity vs `UNIQUE(lemma_text)` for the 15 homograph pairs: either (a) collapse lemmas on
normalized `lemma_text` (storing one representative `lemma_buckwalter`, keeping per-segment Buckwalter
distinct) — mirroring the accepted stem-homograph rule; or (b) reconcile the artifact upstream so no two
persisted head lemmas share `lemma_text`. Both are product/data decisions and belong to a follow-up.

## 8. Boundaries respected

- **No schema change, no new column, no migration** created or applied. The `update-db`/reset step was not
  run (DB was already in the correct pre-morphology state).
- **No legacy pathway removal**; legacy regression 35/35 PASS.
- **No QUL / correction file deletion**; **no enriched JSON artifact edit** (digest verified at load).
- **No Quran data validation weakened.** The new failure was reported, not bypassed.
- **No commit.** Only source files under `Backend/…` were edited in the working tree; nothing staged or
  committed.
