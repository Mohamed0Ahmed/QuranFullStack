# Simplified Segment I‘rab Label Cleanup — Implementation Plan

**Feature:** 017 — Lexical Explorers Polish
**Task type:** IMPLEMENTATION PLAN ONLY (no code/seed/DB/migration/frontend changes; no commits)
**Branch:** `017-lexical-explorers-polish`
**Date:** 2026-06-27
**Reference audit:** `docs/feature-017-lexical-explorers-polish/simple-segment-i3rab-labels-review-report.md`

---

## 1. Scope

**Lower-line simplified segment i‘rab labels only** (segment-card bottom row = `segmentI3rabArabic`). **Not** the middle-line POS/type label (`segmentPosLabel`), not full ayah i‘rab.

### Will change (one file, two rows)
`I3rabRuleCatalogSeedData.cs`:

| Signature | Line | Field | From | To |
| --- | ---: | --- | --- | --- |
| `STEM:PRO` | 66 | i3rabArabic | ضمير منفصل | **لا الناهية** |
| `STEM:ACC` | 26 | i3rabArabic | حرف نصب (من أخوات إنّ/النواصب) | **حرف نصب** |

Plus: a generation step to propagate (Section 4) and focused tests (Section 5).

### Will NOT change (explicit)
- `STEM:SUB`, `STEM:EXL`, `PREFIX:CIRC`, `PREFIX:CAUS` — left as-is.
- All `PRON` labels (stem + suffix); noun/adjective/proper-noun case labels; verb tense/voice labels.
- POS labels (`quran_pos_tags` / `PosTagSeed.cs`) — separate, already cleaned.
- Frontend rendering — none.
- Full ayah i‘rab files — none.
- Generator / signature-builder logic — none (signatures already resolve `STEM:PRO` and `STEM:ACC` correctly; only the catalogue label strings are wrong).
- `rule_family`, `status` (`Approved`), `sort_order` on the two rows — unchanged; only the Arabic label string changes.

---

## 2. Files To Inspect

| File | Why | Expected outcome |
| --- | --- | --- |
| `Backend/infrastructure/.../SimpleI3rabGeneration/I3rabRuleCatalogSeedData.cs` | Edit target — `STEM:ACC` (L26), `STEM:PRO` (L66). | Two label-string edits. |
| `Backend/infrastructure/.../SimpleI3rabGeneration/I3rabRuleCatalogSeed.cs` | Confirms catalogue loaded into `signatureKey → row` dict; `TryGet`. | No edit; confirms new labels are served. |
| `Backend/infrastructure/.../SimpleI3rabGeneration/SegmentSignatureBuilder.cs` | Confirms `STEM:PRO` / `STEM:ACC` signatures are produced for those segments. | No edit (signatures correct). |
| `Backend/infrastructure/.../SimpleI3rabGeneration/I3rabAssembler.cs` | Confirms signature→label mapping (Approved). | No edit. |
| `Backend/infrastructure/.../SimpleI3rabGeneration/I3rabSeedLabelCorrections.cs` | Audit list of corrected signatures, checked by `I3rabValidationRunner` (`CountLabelCorrectionsPresent`). | **Optional**: add `STEM:PRO` / `STEM:ACC` so validation tracks them (Section 3). |
| `Backend/infrastructure/.../Persistence/.../SimpleI3rabGeneration/I3rabSql.cs` | `UpsertRule` (INSERT…ON CONFLICT) → `quran_i3rab_rules`; staging `UpdateSegmentsFromStaging` → segment `i3rab_arabic`. | No edit; confirms re-stamp path. |
| `Backend/infrastructure/.../Persistence/.../SimpleI3rabGeneration/EfI3rabGenerationWriter.cs` + `GenerateI3rabHandler` (`GenerateI3rabCommand(bool Force, …)`) | Generation orchestration + force flag. | No edit; run path for Section 4. |
| `Backend/infrastructure/.../Persistence/Reads/Quran/MushafReader/EfWordAnalysisReader.cs` (`MapSegments`) | Confirms `SegmentI3rabArabic` flows from segment column. | No edit. |
| `Backend/application/.../MushafReader/Responses/WordAnalysisResponse.cs` (`RenderedSegmentDto`) | DTO carrying `SegmentI3rabArabic`. | No edit. |
| `Frontend/.../mushaf/components/segment-data-rows/segment-data-rows.component.html` | Confirms lower line renders raw `segment.segmentI3rabArabic`. | **No edit** (confirmation only). |
| `Backend/tests/.../WordsSimpleI3rab/I3rabRuleCatalogSeedTests.cs`, `I3rabLabelCorrectnessTests.cs` | Catalogue/label assertions. **Confirmed: neither pins `STEM:PRO` or `STEM:ACC` label text today.** | Add new assertions (Section 5). |
| `Backend/tests/.../WordsSimpleI3rab/I3rabGenerationTests.cs`, `SegmentSignatureBuilderTests.cs`, `SuffixPronounSignatureTests.cs` | Generation + signature regression. | Keep green; extend if useful. |

---

## 3. Expected Code Changes (exact)

Single file: `I3rabRuleCatalogSeedData.cs`.

**L66 — `STEM:PRO`:**
```csharp
// from
new("STEM:PRO", "PRO", "ضمير منفصل", I3rabStatusMapping.Approved, null, 57),
// to
new("STEM:PRO", "PRO", "لا الناهية", I3rabStatusMapping.Approved, null, 57),
```

**L26 — `STEM:ACC`:**
```csharp
// from
new("STEM:ACC", "ACC", "حرف نصب (من أخوات إنّ/النواصب)", I3rabStatusMapping.Approved, null, 17),
// to
new("STEM:ACC", "ACC", "حرف نصب", I3rabStatusMapping.Approved, null, 17),
```

**Optional — `I3rabSeedLabelCorrections.cs`:** the `Signatures` list tracks catalogue rows whose labels were deliberately corrected (asserted present by `I3rabValidationRunner`). `STEM:ACC` is **not** currently in it and `STEM:PRO` is **not** in it. Adding both keeps the correction-tracking honest:
```csharp
// add to the Signatures array
"STEM:PRO",
"STEM:ACC",
```
Only do this if the team wants validation to track these as corrections; it is housekeeping, not required for the label to render.

**No other edits:** no reader, DTO, frontend, signature-builder, generator, or POS changes.

---

## 4. Database / Generation Strategy

The catalogue reaches data only via `generate-i3rab`:
1. **Catalogue → rules table:** `I3rabSql.UpsertRule` runs `INSERT … ON CONFLICT (signature_key) DO UPDATE`, so the edited `STEM:PRO` / `STEM:ACC` labels **overwrite** the existing `quran_i3rab_rules` rows in place.
2. **Rules → segments:** every affected segment is re-assembled and `UpdateSegmentsFromStaging` re-stamps `quran_word_morphology_segments.i3rab_arabic` (joined to rules by `signature_key`).

### Recommended approach
- Re-run **`generate-i3rab` with `Force = true`** (`GenerateI3rabCommand(Force: true)`). This regenerates labels for all segments, picking up the two new strings.
- **No `import-morphology`** — morphology segments are untouched; only the derived `i3rab_*` columns change.
- **No migration** — `i3rab_arabic` is a derived column updated by the generator's `UPDATE`; the idempotent `UpsertRule` handles the rule table. A manual data patch is possible but unnecessary.
- **No frontend change** — lower line renders the regenerated `segmentI3rabArabic`.
- **After generation:** restart the API / flush the word-analysis cache (`CachedWordAnalysisReader`) so users see the new labels immediately.

### Ordering caveat
Do not run a full `import-morphology --force` for this change — it truncates segments and **clears** the `i3rab_*` columns, forcing a full regeneration anyway. The smallest safe path is `generate-i3rab --force` alone.

---

## 5. Test Plan (after implementation)

| # | Test | Assertion |
| ---: | --- | --- |
| 1 | Catalogue — `STEM:PRO` | `I3rabRuleCatalogSeed.TryGet("STEM:PRO")` row `I3rabArabic == "لا الناهية"` and `!= "ضمير منفصل"`. |
| 2 | Catalogue — `STEM:ACC` | `STEM:ACC` row `I3rabArabic == "حرف نصب"` (no parenthetical). |
| 3 | Generated segment (`PRO`) | After `generate-i3rab`, the segment at `2:11:4` (لَا) has `i3rab_arabic == "لا الناهية"`. |
| 4 | Word-analysis API (`PRO`) | `GetWordAnalysis("2:11:4")` → the prohibition segment's `RenderedSegmentDto.SegmentI3rabArabic == "لا الناهية"`. |
| 5 | `PRON` unchanged | `STEM:PRON:*` still `ضمير + person`; `SUFFIX:PRON:*` still `ضمير متصل + person` (kind-driven, untouched). |
| 6 | `P` + GEN atomic | A `P`+`N:GEN` word keeps atomic segment labels (`حرف جر` + `اسم مجرور`); no `جار ومجرور` in any `i3rab_arabic`. |
| 7 | `STEM:ACC` segment | Generated segment for an إنّ location (e.g. `2:6:1`) returns `حرف نصب`. |
| 8 | Rule ↔ segment sync | `quran_i3rab_rules.i3rab_arabic` for `STEM:PRO`/`STEM:ACC` matches the stamped `quran_word_morphology_segments.i3rab_arabic` after generation. |
| 9 | Regression green | Existing `WordsSimpleI3rab/*` tests (generation, idempotency, signature builder, label correctness, schema) stay green — none currently pins the two edited strings, so none should break. |

> Tests 3,4,7,8 need seeded morphology + a `generate-i3rab` run in the fixture (the existing `I3rabGenerationTestFixture` already exercises generation). Tests 1,2,5,6 are pure catalogue/signature unit tests.

---

## 6. Verification Commands (do not run unless asked later)

```bash
# Backend build
dotnet build /projects/Dashboard/App/Backend

# Simple i'rab suite
dotnet test /projects/Dashboard/App/Backend --filter "FullyQualifiedName~WordsSimpleI3rab"

# Focused
dotnet test /projects/Dashboard/App/Backend --filter "FullyQualifiedName~I3rabRuleCatalogSeed"
dotnet test /projects/Dashboard/App/Backend --filter "FullyQualifiedName~I3rabLabelCorrectness"
dotnet test /projects/Dashboard/App/Backend --filter "FullyQualifiedName~WordAnalysis"

# Apply to existing DB (only when running the data path, not part of this plan):
#   run generate-i3rab with Force = true  (then restart API / flush word-analysis cache)

# Read-only confirmation after generation (privileged role):
#   SELECT signature_key, i3rab_arabic FROM quran_i3rab_rules WHERE signature_key IN ('STEM:PRO','STEM:ACC');
#   SELECT w.location, s.i3rab_arabic FROM quran_word_morphology_segments s
#     JOIN quran_i3rab_rules r ON r.id = s.i3rab_rule_id
#     JOIN quran_words w ON w.id = s.quran_word_id
#     WHERE r.signature_key = 'STEM:PRO' LIMIT 10;
```

Frontend: no build/test required (no frontend change).

---

## 7. Risks

- **Existing DB stale until regen:** the seed edit alone does nothing to `quran_i3rab_rules` / segment columns; labels appear only after `generate-i3rab --force`.
- **Cache/API restart:** `CachedWordAnalysisReader` may serve old labels until the cache is flushed / API restarted.
- **Mixed-label window:** if local DB still holds older POS or i‘rab generated data, the UI can briefly show mixed/contradictory labels (e.g. POS middle line `حرف نهي` vs old lower line `ضمير منفصل`) until regeneration completes. Regenerating resolves it.
- **Intentional non-changes:** `STEM:SUB`, `STEM:EXL`, `PREFIX:CIRC`, `PREFIX:CAUS` are deliberately left as-is; reviewers should not treat their unchanged wording as oversight.
- **Do not over-reach:** avoid `import-morphology --force` for this label change (would clear all `i3rab_*` and force a heavier rebuild).
- **Low test-break risk:** no current test pins the two edited strings (verified), so regression risk is minimal; still add the new assertions in Section 5.

---

## 8. Recommended Implementation Prompt (use after approval)

> Implement the approved simplified segment i‘rab label cleanup on branch `017-lexical-explorers-polish`, scope strictly limited to two rows in `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/SimpleI3rabGeneration/I3rabRuleCatalogSeedData.cs`:
> 1. `STEM:PRO` (L66): change i3rab label `ضمير منفصل` → `لا الناهية`.
> 2. `STEM:ACC` (L26): change i3rab label `حرف نصب (من أخوات إنّ/النواصب)` → `حرف نصب`.
>
> Optionally add `"STEM:PRO"` and `"STEM:ACC"` to `I3rabSeedLabelCorrections.cs` `Signatures` if we want validation to track them.
>
> Do not change POS labels, PRON/case/verb labels, `STEM:SUB`/`STEM:EXL`/`PREFIX:CIRC`/`PREFIX:CAUS`, reader/DTO/frontend, signature-builder, generator logic, or full ayah i‘rab. No migration, no `import-morphology`.
>
> Add the focused tests from the plan's Section 5 (catalogue label assertions for `STEM:PRO`/`STEM:ACC`; generated-segment + word-analysis assertions for `2:11:4` → `لا الناهية`; `PRON` unchanged; `P`+GEN atomic). Run `dotnet build` and `dotnet test --filter "FullyQualifiedName~WordsSimpleI3rab"`; keep the suite green. Then note that applying labels to an existing DB requires `generate-i3rab --force` + API cache flush (operational, not part of the code change). Do not commit unless asked.

---

## 9. Constraints Honored

Plan only — no files modified, no seed/DB/migration/frontend edits, no commands run, no commit. Smallest safe path: one catalogue file, two label strings, zero reader/DTO/frontend/generator changes; propagation deferred to an explicit `generate-i3rab --force`.
