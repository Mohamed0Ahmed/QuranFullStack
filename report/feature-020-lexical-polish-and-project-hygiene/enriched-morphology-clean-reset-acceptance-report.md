# Enriched Morphology — Clean Local Reset & Import Acceptance Report

- Feature: 020 — Lexical Polish and Project Hygiene
- Branch: `020-lexical-polish-and-project-hygiene`
- Run date (local): 2026-07-04
- Scope: Destructive **local development database only**. Dashboard-side acceptance of the regenerated SourceAudit enriched morphology artifact.

---

## 1. Verdict

**FAIL_STOPPED**

The staged artifact replaced cleanly, foundation + words rebuild succeeded, and the enriched morphology **dry validation passed all 16 checks**. However, the **real `import-morphology --enriched` step failed one hard render gate** and was rolled back. Per the failure policy, the run was stopped immediately. No morphology rows were persisted. No fixes were attempted.

---

## 2. Safety confirmation

- **Local DB target confirmed.**
  - `dotnet user-secrets list` for both `QuranDashboard.Api` and `QuranDashboard.DataImporter` resolves to:
    `Host=localhost;Port=5432;Database=quran_dashboard;Username=postgres;Password=123456`
  - The reset/drop/update scripts force `DOTNET_ENVIRONMENT=Development` and use the API startup project, so the Development user-secrets connection string is the one used.
- **Destructive reset was local only.** The Production connection string in `appsettings.Production.json` points at a remote Neon host; it was **not** loaded because `DOTNET_ENVIRONMENT=Development` was set by the scripts.
- **No production/remote DB touched.**
- `qd-build` passed with 0 warnings, 0 errors before any DB work.

---

## 3. Staged artifact replacement

| Field | Before | After |
|---|---|---|
| Source path | `~/Desktop/projects/QuranMorphologySourceAudit/jsonData/corpus-based-enriched-morphology.dashboard-ready.json` | — |
| Destination | `resources/import-sources/quran-enriched-morphology/corpus-based-enriched-morphology.dashboard-ready.json` | — |
| sizeBytes | 96,051,070 | **96,123,182** |
| sha256 | `D8137D4B90B5E7C428B390029B29E6AE83F36E217003CD9EE9AD8CF067DF5C3D` | **`3B2253035C5BA4C46AFC8E1989C44121547CA9F2D2F18F0AF5D657056BFB9E7A`** |
| recordCount | 77,432 | 77,432 (unchanged) |
| segmentCount | 128,219 | 128,219 (unchanged) |

Pre-stage verification of the regenerated file (Python read of the artifact):

- records: **77,432** ✓
- segments: **128,219** ✓
- null POS: **0** ✓
- blank POS: **0** ✓
- fallback records: **0** ✓
- JSON readable ✓

**Manifest update required:** yes — `EnrichedMorphologyManifestReader` enforces `sha256` and `sizeBytes` (plus `recordCount`/`segmentCount`). Only the two enforced fields that changed were edited in `resources/import-sources/quran-enriched-morphology/manifest.json`:

- `files[0].sha256` → `3B2253035C5BA4C46AFC8E1989C44121547CA9F2D2F18F0AF5D657056BFB9E7A`
- `files[0].sizeBytes` → `96123182`

`recordCount` (77432) and `segmentCount` (128219) were already correct and were left unchanged. No other manifest fields were touched.

---

## 4. Commands run (in order)

All commands run from the Backend submodule root or the DataImporter project as noted.

| # | Working dir | Command | Result |
|---|---|---|---|
| 1 | `Backend/` | `git status --short --branch` / `git rev-parse --abbrev-ref HEAD` | branch `020-lexical-polish-and-project-hygiene` confirmed; only pre-existing `m Backend` submodule marker |
| 2 | `Backend/api/QuranDashboard.Api` | `dotnet user-secrets list` | `ConnectionStrings:QuranDashboardDb = Host=localhost;...` (local confirmed) |
| 3 | `Backend/tools/QuranDashboard.DataImporter` | `dotnet user-secrets list` | same localhost connection (local confirmed) |
| 4 | `Backend/` | `./scripts/qd-build` | **PASS** — Build succeeded. 0 Warning(s) 0 Error(s) |
| 5 | `Backend/` | `./scripts/reset-db --yes` | **PASS** — DB dropped; all 17 migrations applied incl. pending `20260704102858_AddQuranLemmaAnalyses` |
| 6 | `Backend/` | `dotnet ef migrations list ...` | **PASS** — 17 migrations shown as applied |
| 7 | `Backend/tools/QuranDashboard.DataImporter` | `dotnet run --no-build -- import-foundation --source <repo>/resources/import-sources/quran-foundation` | **PASS** — surahs=114, ayahs=6236, pages=604, lines=9046, words=83668 |
| 8 | `Backend/tools/QuranDashboard.DataImporter` | `dotnet run --no-build -- rebuild-words` | **PASS** — ordered_tashkeel=77432, ordered_simple=77432, unique_tashkeel=21294, unique_simple=14783 |
| 9 | `Backend/tools/QuranDashboard.DataImporter` | `dotnet run --no-build -- validate-enriched-morphology` | **PASS** — 16/16 checks PASS |
| 10 | `Backend/tools/QuranDashboard.DataImporter` | `dotnet run --no-build -- import-morphology --enriched` | **FAIL (exit 1)** — hard check `MORPH-SEG-RENDER-TOTAL` failed; rolled back |

---

## 5. Database / import counts

### Foundation (persisted)

- quran_surahs: 114
- quran_ayahs: 6,236
- quran_mushaf_pages: 604
- quran_mushaf_lines: 9,046
- quran_words total rows: **83,668** ✓ (expected total word rows)
  - readable words (source): **77,432** ✓
  - ayah markers: 6,236 ✓ (83,668 − 77,432)

### Words rebuild (persisted)

- quran_words_ordered_tashkeel: 77,432 (readable words) ✓
- quran_words_ordered_simple: 77,432
- quran_words_unique_tashkeel: 21,294
- quran_words_unique_simple: 14,783

### Enriched morphology dry validation (no DB write — all PASS, 16/16)

| Check | Expected | Observed |
|---|---|---|
| ENRICHED-RECORD-COUNT | 77432 | 77432 ✅ |
| ENRICHED-SEGMENT-COUNT | 128219 | 128219 ✅ |
| ENRICHED-NO-FALLBACK-WORDS | 0 | 0 ✅ |
| ENRICHED-NO-DUPLICATE-LOCATIONS | 0 | 0 ✅ |
| ENRICHED-NO-DUPLICATE-SEGMENT-KEYS | 0 | 0 ✅ |
| ENRICHED-BOUNDARY-2:181 | 14 | 14 ✅ |
| ENRICHED-BOUNDARY-2:282 | 128 | 128 ✅ |
| ENRICHED-BOUNDARY-8:6 | 12 | 12 ✅ |
| ENRICHED-BOUNDARY-13:37 | 20 | 20 ✅ |
| ENRICHED-BOUNDARY-8:6:12-SEGMENTS | 2 | 2 ✅ |
| ENRICHED-POS-RESOLVES | 0 unknown | 0 unknown ✅ |
| ENRICHED-NO-QUL-WORD-LEMMA-LINK | corpus-bridge-enriched + verified=true | matches ✅ |
| ENRICHED-CORRECTED-LEMMA-41:44:16 | شِفَاء (not ءَامَنَ) | شِفَاء ✅ |
| ENRICHED-CORRECTED-LEMMA-11:29:17 | مَّلَٰقُوا (not ءَامَنَ) | مَّلَٰقُوا ✅ |
| ENRICHED-CORRECTED-LEMMA-2:102:41 | مَرْء (not فَرَّقُ) | مَرْء ✅ |
| ENRICHED-CORRECTED-LEMMA-2:144:20 | شَطْر (not كَانَ) | شَطْر ✅ |

### Enriched morphology import — **attempted totals (NOT persisted; rolled back)**

| Table | Attempted rows |
|---|---:|
| quran_word_morphology | 77,432 |
| quran_word_morphology_segments | 128,219 |
| quran_roots | 1,642 |
| quran_lemmas | 4,817 |
| quran_lemma_analyses | 4,832 |
| quran_stems | 11,843 |
| quran_pos_tags | 49 |
| readable words (source) | 77,432 |

Render tiers (attempted): clean=128,219, quranic_marks=0, review=0, multiword=0; empty-form renders → NULL: 208.

**Lemma / lemma-analyses import**: was not reached as a separate step; it is part of the morphology import transaction, which was rolled back. No lemma rows persisted.

### Segment POS checks (from the failed import's pre-rollback validation)

- `MORPH-POS-RESOLVES`: 0 unknown — every head_pos and segment pos resolves to `quran_pos_tags.code` ✅
- No FK failure on `quran_word_morphology_segments.pos` reported (the failure is not a POS FK issue).

---

## 6. Gate results

| Gate | Result |
|---|---|
| STAGED-ARTIFACT-COUNT (77,432 / 128,219) | ✅ PASS |
| STAGED-SEGMENT-POS-NON-BLANK (0 null, 0 blank) | ✅ PASS |
| STAGED-FALLBACK-COUNT-0 | ✅ PASS |
| DB-FOUNDATION-COUNTS (words 83,668; readable 77,432; ayah markers 6,236) | ✅ PASS |
| DB-WORDS-REBUILD (ordered_tashkeel 77,432) | ✅ PASS |
| ENRICHED-VALIDATION (dry, 16/16) | ✅ PASS |
| **ENRICHED-MORPHOLOGY-IMPORT** | ❌ **FAIL** — `MORPH-SEG-RENDER-TOTAL`: `non_empty_null=1` |
| SEGMENT-POS-FK-RESOLVES (0 unknown POS) | ✅ PASS (pre-rollback) |
| LEMMA-TEXT-COLLISION-NOT-REGRESSED | ⚪ NOT REACHED — morphology import rolled back before lemma persistence |
| BOUNDARY-AYAHS-PRESERVED (2:181, 2:282, 8:6, 13:37, 8:6:12) | ✅ PASS (dry validation) |
| CORRECTED-LEMMA-ANCHORS-PASS (41:44:16, 11:29:17, 2:102:41, 2:144:20) | ✅ PASS (dry validation) |

---

## 7. Failure detail

**Failing command (step 10):**

```bash
dotnet run --no-build -- import-morphology --enriched
# working dir: Backend/tools/QuranDashboard.DataImporter
```

- **Exit code:** 1
- **Verdict from importer:** `FAIL`, `Persisted: False`, `Forced: False`
- **DB state:** Rolled back. No `quran_word_morphology`, `quran_word_morphology_segments`, `quran_roots`, `quran_lemmas`, `quran_lemma_analyses`, `quran_stems`, or `quran_pos_tags` rows were persisted by this run. Foundation + words-display tables remain populated from steps 7–8.
- **Staged artifact state:** Unchanged — the importer's own `MORPH-SOURCE-UNCHANGED` hard check PASSED (`unchanged`), confirming the staged file/manifest match before and after the run.
- **Single failing hard check:**
  - `MORPH-SEG-RENDER-TOTAL`
  - Expected: non-empty form → non-null render; empty form → NULL
  - Observed: `non_empty_null=1, empty_non_null=0`
  - Meaning: exactly **one** segment has a non-empty `form` but its computed render is NULL. (The 208 empty-form → NULL renders are the expected/documented behavior and are not the cause.)
- **All other 24 hard checks PASSED**, including: readable-complete, markers-excluded, location-match, segments-present, POS-present, verb-feature-consistency, seg-charset, seg-tier-valid, seg-render-provenance, dimension-resolves, every lemma_id/root_id/stem_id integrity check, and POS-resolves (0 unknown).
- **Likely failing layer:** `validation` (importer-side hard check `MORPH-SEG-RENDER-TOTAL`), specifically the **render computation** for one segment whose source `form` is non-empty. This is **not** a source-artifact structural problem (counts, POS, locations, dimensions all pass), **not** a DB constraint failure, **not** a foundation/words-rebuild problem, and **not** a code bug in the sense of a crash — it is a single-segment render edge case that the importer's render pipeline maps to NULL where the gate requires a non-null render. Root-cause investigation (which segment, why its render is NULL) was intentionally not performed, per the failure policy.

Import report written by the importer:
`resources/report/words-morphology/morphology-import-report.{md,json}`

Dry-validation report:
`Backend/report/feature-020-lexical-polish-and-project-hygiene/enriched-morphology-validation/enriched-morphology-dry-validation-*.md`

---

## 8. Final status

- **Is the regenerated staged artifact accepted by Dashboard validation?**
  Yes by the **dry validator** (16/16 checks, including boundary ayahs and corrected lemma anchors). **No** by the **real importer's render gate** (`MORPH-SEG-RENDER-TOTAL`).
- **Did enriched morphology import complete?** **No.** It failed one hard render check and was rolled back. No morphology/lemma/lemma-analysis rows persisted.
- **Is Feature 020 still blocked by anything?** **Yes.** Feature 020 is blocked on the single-segment render edge case (`non_empty_null=1` in `MORPH-SEG-RENDER-TOTAL`).

### Next required action

Investigate the one segment with a non-empty form but NULL render:

1. Identify the offending segment (location + segment number) — likely surfacing it requires either (a) extending the importer's render check to emit the offending location/segment, or (b) a focused read-only diff of the regenerated artifact against the previously-staged one around render-affecting fields (`formBuckwalter`/`formArabic`, `kind`, `pos`).
2. Determine whether the NULL render originates in the **source artifact** (a non-empty `form` that the render pipeline legitimately cannot map) or in the **Dashboard render computation** (a render-path gap that should be fixed on the Dashboard side).
3. If the root cause is on the **SourceAudit** side, regenerate/correct the artifact and re-run this acceptance flow. If it is on the **Dashboard** side, fix the render path and re-run `import-morphology --enriched` against the unchanged staged artifact.

Per instructions, **no fixes were attempted** and **nothing was committed**. The only files modified locally are the staged artifact (`resources/import-sources/quran-enriched-morphology/corpus-based-enriched-morphology.dashboard-ready.json`) and its manifest (`.../manifest.json`), both under the gitignored `resources/` tree.
