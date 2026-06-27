# POS / Segment-Type Label Cleanup — Implementation Plan

**Feature:** 017 — Lexical Explorers Polish
**Task type:** IMPLEMENTATION PLAN ONLY (no code/seed/DB/migration/frontend changes; no commits)
**Branch:** `017-lexical-explorers-polish`
**Date:** 2026-06-27
**Companion audit:** `docs/feature-017-lexical-explorers-polish/pos-tag-arabic-labels-review-report.md`

---

## 1. Scope

### Will change (one file, four rows)
`PosTagSeed.cs` — Arabic/English label and (for `PRO`) category on four POS codes:

| Code | Field | From | To |
| --- | --- | --- | --- |
| `PRO` | ArabicLabel | ضمير منفصل | **حرف نهي** |
| `PRO` | EnglishLabel | Independent Pronoun | **Prohibition Particle** |
| `PRO` | Category | noun | **particle** |
| `CAUS` | ArabicLabel | فاء السببية | **حرف سببية** |
| `CIRC` | ArabicLabel | واو الحال | **حرف حال** |
| `EXP` | ArabicLabel | حرف استثناء | **أداة استثناء** |

Plus: a database propagation step (Section 4) and focused tests (Section 5).

### Will NOT change
- No edits to `MorphologyBulkCopier`, readers, broad-label logic, or DTOs (the corrected `PRO` category flows through the **existing** `particle → حرف` mapping automatically).
- No frontend changes (confirmed: no hardcoded POS labels — Section 2).
- No simplified i‘rab labels. No `quran_i3rab_rules`. No `I3rabRuleCatalogSeedData.cs` (the stale `STEM:PRO → "ضمير منفصل"` stays for a later dedicated i‘rab cleanup — Section 7).
- No lemma/stem filtering logic.
- No other 45 POS codes.
- No `SortOrder` changes (PRO stays `SortOrder = 20`; re-ordering is out of scope and would be churn).
- No new migration unless the team picks the data-patch option in Section 4 (justified there).

---

## 2. Files To Inspect

| File | Why inspect | Expected outcome |
| --- | --- | --- |
| `Backend/infrastructure/.../Files/Quran/DataPipelines/Words/MorphologyImporting/PosTagSeed.cs` | The only edit target. Rows at **L28 `PRO`**, **L33 `CAUS`**, **L48 `CIRC`**, **L51 `EXP`**. | Apply the four-row change. |
| `Backend/infrastructure/.../Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologyBulkCopier.cs` (`CopyPosTagsAsync`) | Confirms seed → `COPY quran_pos_tags(... category ...)`. | No edit; confirms reseed carries the new category. |
| `Backend/infrastructure/.../MorphologyImporting/EfBulkMorphologyWriter.cs` + `MorphologySql.cs` (`TruncateMorphologyTables`) | Confirms `--force` reseed path and the `TRUNCATE … RESTART IDENTITY CASCADE` cascade. | No edit; informs reseed-vs-patch decision. |
| `Backend/infrastructure/.../Persistence/Reads/Quran/MushafReader/EfWordAnalysisReader.cs` | Confirms head + segment labels are read from `quran_pos_tags` (fallback = raw code). | No edit; corrected labels flow through. |
| `Backend/infrastructure/.../Persistence/Reads/Quran/Words/EfUniqueWordsReader.cs` (`ResolvePrimaryWordTypeBroadLabel`) | Confirms broad label = `category` map (`noun→اسم`, `verb→فعل`, `particle→حرف`, `INL→حروف مقطّعة`). | No edit; `PRO` category `noun→particle` auto-flips broad label `اسم→حرف`. |
| `Backend/application/.../Quran/MushafReader/Responses/WordAnalysisResponse.cs` | DTO carrying `HeadPosLabel` / `SegmentPosLabel`. | No edit; shape unchanged. |
| `Frontend/.../features/mushaf/components/segment-data-rows/segment-data-rows.component.html` | Renders the **segment middle-line** label `segment.segmentPosLabel.ar`. | No edit; renders API value. |
| `Frontend/.../features/mushaf/components/word-morphology-summary/word-morphology-summary.component.{ts,html}` | Renders `headPosLabel.ar` (نوع الكلمة). | No edit; renders API value. |
| `Frontend/.../features/mushaf/utils/morphology-display.labels.ts` | Sanity-check it stays a dash helper only (no Arabic POS strings). | No edit. |
| `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsListReadTests.cs` | Asserts broad labels (`PN→اسم`, `V→فعل`, `P→حرف`, `INL→حروف مقطّعة`). No `PRO` assertion today. | Add `PRO→حرف` assertion (Section 5). |
| `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/WordAnalysis*Tests.cs` | Existing word-analysis tests (incomplete-data, marker rejection, cache). | Add a `PRO` label assertion test alongside. |

**Frontend confirmation:** segment middle-line labels come solely from `segmentPosLabel.ar` (API). No hardcoded Arabic POS map exists in the frontend → **no frontend edit needed**.

---

## 3. Expected Code Changes (exact)

Single file: `PosTagSeed.cs`.

**L28 — `PRO` (label + english + category):**
```csharp
// from
new PosTag { Code = "PRO", ArabicLabel = "ضمير منفصل", EnglishLabel = "Independent Pronoun", Category = "noun", SortOrder = 20 },
// to
new PosTag { Code = "PRO", ArabicLabel = "حرف نهي", EnglishLabel = "Prohibition Particle", Category = "particle", SortOrder = 20 },
```
> Optional (recommended) for parity with siblings: add `Description = "Prohibition particle (لا الناهية); jussive-inducing, not a pronoun"`. Not required for correctness.

**L33 — `CAUS` (ArabicLabel only):**
```csharp
ArabicLabel = "فاء السببية"  →  ArabicLabel = "حرف سببية"
```
(Keep `EnglishLabel = "Causative"`, `Category = "particle"`, `Description` unchanged — or update the parenthetical in Description to match; optional.)

**L48 — `CIRC` (ArabicLabel only):**
```csharp
ArabicLabel = "واو الحال"  →  ArabicLabel = "حرف حال"
```

**L51 — `EXP` (ArabicLabel only):**
```csharp
ArabicLabel = "حرف استثناء"  →  ArabicLabel = "أداة استثناء"
```

**Confirmations baked into the change:**
- `PRO.Category` becomes `particle` → fixes both the detailed Mushaf label and the Unique Words broad label (`اسم → حرف`) with **no reader change**.
- No frontend change (Section 2).
- No DTO/contract change (label strings only; `LocalizedLabel` shape unchanged).

---

## 4. Database / Reseed Strategy

The seed reaches `quran_pos_tags` only via the morphology import `COPY`. Two options:

### Option A — `--force` morphology reseed (recommended for LOCAL dev)
- Run `import-morphology --force`. `EfBulkMorphologyWriter` executes `TRUNCATE quran_word_morphology_segments, quran_word_morphology, quran_lemmas, quran_roots, quran_stems, quran_pos_tags RESTART IDENTITY CASCADE`, then re-`COPY`s all six tables from the current seed → corrected labels land.
- **Cascade caveat:** this truncates `quran_word_morphology_segments`, on which i‘rab assignments (Feature 005) depend. Established order is `import-morphology → generate-i3rab`. So after a force reseed you must also re-run the downstream pipeline(s) that populate segment i‘rab (`generate-i3rab --force`, and any further dependents). Locally this is acceptable and keeps **DB == seed** (no drift), which is the project's canonical state.
- **Pros:** single source of truth; matches how the table is meant to be populated; no schema artifact. **Cons:** heavier; rebuilds morphology + forces downstream regeneration.

### Option B — targeted data-patch migration (justified for SHARED / production-like DBs)
- A migration running an idempotent `UPDATE` on four rows:
  ```sql
  UPDATE quran_pos_tags SET arabic_label='حرف نهي', english_label='Prohibition Particle', category='particle' WHERE code='PRO';
  UPDATE quran_pos_tags SET arabic_label='حرف سببية' WHERE code='CAUS';
  UPDATE quran_pos_tags SET arabic_label='حرف حال'   WHERE code='CIRC';
  UPDATE quran_pos_tags SET arabic_label='أداة استثناء' WHERE code='EXP';
  ```
- **Why a data patch can be safer than reseed here:** it touches only the four label rows — **no `TRUNCATE … CASCADE`**, so morphology segments and the downstream i‘rab/mutashabihat/full-i‘rab data are untouched and need no regeneration. On an environment where rebuilding morphology + all dependents is expensive or risky, the 4-row `UPDATE` is the smaller, lower-blast-radius change.
- **Cons:** introduces a one-off data migration that must stay consistent with `PosTagSeed.cs` (otherwise a future fresh import and the patched DB could diverge if the seed weren't also edited). Mitigation: **always edit `PosTagSeed.cs` too** (Section 3) so fresh imports and patched DBs agree.

### Recommendation
- **Local development:** Option A (`import-morphology --force` then re-run downstream `generate-i3rab --force`). Simplest, keeps DB aligned to seed.
- **Shared / pre-existing DB where a full morphology rebuild is undesirable:** Option B data patch, **in addition** to the `PosTagSeed.cs` edit.
- In all cases the `PosTagSeed.cs` edit is mandatory so future imports are correct.

---

## 5. Test Plan (after implementation)

| # | Test | Assertion |
| ---: | --- | --- |
| 1 | PosTagSeed contains corrected `PRO` | `PosTagSeed.GetAll()` `PRO` row: `ArabicLabel == "حرف نهي"`, `EnglishLabel == "Prohibition Particle"`, `Category == "particle"`. |
| 2 | PosTagSeed wording polish | `CAUS.ArabicLabel == "حرف سببية"`, `CIRC.ArabicLabel == "حرف حال"`, `EXP.ArabicLabel == "أداة استثناء"`. |
| 3 | `PRO` not shown as pronoun | Word analysis for a `PRO` location (e.g. `2:11:4` لَا) → `morphology.headPosLabel.ar != "ضمير منفصل"` and `== "حرف نهي"`; the prohibition segment's `segmentPosLabel.ar == "حرف نهي"`. |
| 4 | `PRO` broad label | Unique Words item whose winner `head_pos == "PRO"` → `primaryWordTypeBroadArabicLabel == "حرف"` (not `اسم`). Extend `UniqueWordsListReadTests.cs`. |
| 5 | CAUS/CIRC/EXP labels | Segment word analysis for a `CAUS`/`CIRC`/`EXP` segment returns the updated `segmentPosLabel.ar`. |
| 6 | Mushaf Reader exposure | `GetWordAnalysisHandler` / `EfWordAnalysisReader` surfaces the corrected `headPosLabel`/`segmentPosLabel` end-to-end. |
| 7 | Regression green | Existing morphology import + `UniqueWordsListReadTests` + `WordAnalysis*Tests` remain green (no test currently asserts `PRO` under `noun`, so none should break — verify category-count style assertions, if any, were not added). |

> Note: tests 3–6 require seeded morphology test data (Testcontainers/real import). If the test harness reseeds from `PosTagSeed`, they validate the new labels directly; otherwise gate them behind the same import fixture the existing word-analysis tests use.

---

## 6. Verification Commands (do not run unless asked later)

```bash
# Backend build
dotnet build /projects/Dashboard/App/Backend

# Backend tests (whole suite)
dotnet test /projects/Dashboard/App/Backend

# Focused test runs (after adding the tests above)
dotnet test /projects/Dashboard/App/Backend --filter "FullyQualifiedName~PosTagSeed"
dotnet test /projects/Dashboard/App/Backend --filter "FullyQualifiedName~UniqueWordsListRead"
dotnet test /projects/Dashboard/App/Backend --filter "FullyQualifiedName~WordAnalysis"

# Local reseed (Option A) — only if running the data path, not part of this plan
#   import-morphology --force   (then) generate-i3rab --force

# Read-only DB confirmation after propagation (privileged role)
#   SELECT code, arabic_label, english_label, category FROM quran_pos_tags
#   WHERE code IN ('PRO','CAUS','CIRC','EXP');
```

Frontend: no build/test needed (no frontend change). If a smoke check is wanted regardless, the existing capped `npm test` for the mushaf segment components covers rendering of `segmentPosLabel.ar`.

---

## 7. Risks

- **DB not auto-updated:** editing `PosTagSeed.cs` alone does **not** change existing `quran_pos_tags` rows. The corrected labels appear only after Option A reseed or Option B data patch (Section 4).
- **Cascade on force reseed:** `import-morphology --force` truncates morphology segments `CASCADE`; downstream i‘rab (and any dependent pipelines) must be regenerated afterward. Plan the run order (`import-morphology → generate-i3rab → …`).
- **Runtime cache:** word-analysis reads are cached (`CachedWordAnalysisReader`). After propagation, restart the API / flush cache so users see corrected labels immediately.
- **Stale i‘rab label remains:** `quran_i3rab_rules` still carries `STEM:PRO → "ضمير منفصل"` (from `I3rabRuleCatalogSeedData.cs`). It is intentionally **out of scope** here and will surface in the segment i‘rab line until the dedicated i‘rab cleanup. Flag to reviewers so the lingering pronoun wording isn't mistaken for an incomplete fix.
- **Category-count assumptions:** moving `PRO` from `noun` to `particle` shifts category tallies (noun 11→10, particle 34→35). No current test asserts these counts, but confirm none is added that would break.
- **Seed/patch divergence (Option B):** if a data patch is applied without the matching `PosTagSeed.cs` edit, a future fresh import would reintroduce the old labels. Mitigation: always ship the seed edit (mandatory in this plan).

---

## 8. Constraints Honored

Plan only — no files modified, no seed/DB/migration/frontend edits, no commands run, no commit. Smallest safe path: one seed file, four rows, zero reader/DTO/frontend changes; database propagation deferred to an explicit reseed or an optionally-justified data patch.
