# Enriched Morphology Dot Render Import Continuation Report

- Feature: 020 — Lexical Polish and Project Hygiene
- Run date: 2026-07-04
- Scope: continue Dashboard-side enriched morphology acceptance after SourceAudit dot-render fix

## 1. Verdict

**FAIL_STOPPED**

The SourceAudit-fixed artifact was staged, manifest hash/size were updated, dry validation passed, and the real enriched import **completed and persisted** with all importer hard checks passing, including `MORPH-SEG-RENDER-TOTAL`.

Acceptance stopped during the required post-import count verification because `quran_stems` persisted **11,848** rows, while the expected value in this task was **11,843**.

No fixes were attempted after this mismatch.

## 2. Safety Confirmation

- Workspace branch confirmed: `020-lexical-polish-and-project-hygiene`.
- Backend submodule worktree was already dirty with Feature 020 implementation/report files; no unrelated changes were reverted.
- API user-secrets connection string confirmed local: `Host=localhost;Port=5432;Database=quran_dashboard;Username=postgres;...`.
- DataImporter user-secrets connection string confirmed local: `Host=localhost;Port=5432;Database=quran_dashboard;Username=postgres;...`.
- `DOTNET_ENVIRONMENT` was unset globally; every DataImporter command was run with `DOTNET_ENVIRONMENT=Development` explicitly.
- No `reset-db` run.
- No production/remote DB touched.
- No Dashboard schema/migration changes.
- No `PosTagSeed` changes.
- No `MORPH-SEG-RENDER-TOTAL` weakening.
- No SourceAudit changes in this task.
- No commit.

## 3. Staged Artifact Replacement

Source path:

`/home/mohamed/Desktop/projects/QuranMorphologySourceAudit/jsonData/corpus-based-enriched-morphology.dashboard-ready.json`

Destination path:

`/projects/Dashboard/App/resources/import-sources/quran-enriched-morphology/corpus-based-enriched-morphology.dashboard-ready.json`

Hash/size:

| Field | Before | After |
|---|---|---|
| sha256 | `3B2253035C5BA4C46AFC8E1989C44121547CA9F2D2F18F0AF5D657056BFB9E7A` | `658059DEFFEA85B57AF594C71FA0405B00FBBA072C2616EACC1B01947DD070DD` |
| sizeBytes | `96,123,182` | `96,125,952` |

Manifest changes:

- Updated only `files[0].sha256`.
- Updated only `files[0].sizeBytes`.
- Left `recordCount=77432` unchanged.
- Left `segmentCount=128219` unchanged.

Staged artifact verification:

| Check | Observed | Result |
|---|---:|---|
| records | 77,432 | PASS |
| segments | 128,219 | PASS |
| fallback / corpusPresent false | 0 | PASS |
| null POS | 0 | PASS |
| blank POS | 0 | PASS |
| non-empty formBuckwalter with empty/null formArabic | 0 | PASS |

Dot-render anchor:

| Field | Value |
|---|---|
| location | `12:101:14` |
| segmentNumber | `2` |
| formBuckwalter | `.` |
| formArabic | `ۦ` |
| kind | `SUFFIX` |
| pos | `PRON` |
| featuresRaw | `SUFFIX|PRON:1S` |

## 4. Commands Run

| # | Working dir | Command | Result |
|---|---|---|---|
| 1 | `/projects/Dashboard/App` | `git status --short --branch` | branch confirmed; Backend submodule dirty |
| 2 | `/projects/Dashboard/App/Backend` | `git status --short --branch` | existing Feature 020 dirty state confirmed |
| 3 | `Backend/api/QuranDashboard.Api` | `dotnet user-secrets list` | local `localhost:5432/quran_dashboard` confirmed |
| 4 | `Backend/tools/QuranDashboard.DataImporter` | `dotnet user-secrets list` | local `localhost:5432/quran_dashboard` confirmed |
| 5 | `/projects/Dashboard/App` | hash/size source and destination artifact | before/after hash and size captured |
| 6 | `/projects/Dashboard/App` | `rm <staged-artifact> && cp <SourceAudit-artifact> <staged-artifact>` | staged artifact replaced |
| 7 | `/projects/Dashboard/App` | staged artifact JSON verification probe | PASS |
| 8 | `/projects/Dashboard/App/resources/import-sources/quran-enriched-morphology/manifest.json` | manifest hash/size update | PASS |
| 9 | `/projects/Dashboard/App/Backend` | read-only prerequisite `psql` count query | PASS prerequisites |
| 10 | `Backend/tools/QuranDashboard.DataImporter` | `DOTNET_ENVIRONMENT=Development dotnet run --no-build -- validate-enriched-morphology` | PASS 16/16 |
| 11 | `Backend/tools/QuranDashboard.DataImporter` | `DOTNET_ENVIRONMENT=Development dotnet run --no-build -- import-morphology --enriched` | PASS, persisted true |
| 12 | `/projects/Dashboard/App/Backend` | read-only post-import `psql` count/check query | FAIL on `quran_stems=11848` vs expected `11843` |

No command exited with a tool-level failure after staging. The acceptance failure is a post-import expected-count mismatch, not a CLI crash.

## 5. Prerequisite DB State Before Import

Read-only prerequisite counts before running validation/import:

| Table/check | Expected | Observed | Result |
|---|---:|---:|---|
| quran_words total | 83,668 | 83,668 | PASS |
| readable words | 77,432 | 77,432 | PASS |
| ayah markers | 6,236 | 6,236 | PASS |
| quran_words_ordered_tashkeel | 77,432 | 77,432 | PASS |
| quran_words_ordered_simple | 77,432 | 77,432 | PASS |
| quran_words_unique_tashkeel | 21,294 | 21,294 | PASS |
| quran_words_unique_simple | 14,783 | 14,783 | PASS |
| morphology/dimension target tables | expected empty after rollback | all 0 | PASS |

## 6. Validation / Import Results

### Dry Validation

Report path:

`Backend/report/feature-020-lexical-polish-and-project-hygiene/enriched-morphology-validation/enriched-morphology-dry-validation-20260704123256.md`

Result:

- Overall verdict: PASS.
- 16/16 checks passed.
- POS resolves: 0 unknown.
- Boundary checks passed for `2:181`, `2:282`, `8:6`, `13:37`, and `8:6:12` two segments.
- Corrected lemma anchors passed:
  - `41:44:16` -> `شِفَاء`
  - `11:29:17` -> `مُّلَٰقُوا`
  - `2:102:41` -> `مَرْء`
  - `2:144:20` -> `شَطْر`

### Real Import

Report path:

`resources/report/words-morphology/morphology-import-report.md`

Result:

- Verdict: PASS.
- Persisted: True.
- Forced: False.
- `MORPH-SEG-RENDER-TOTAL`: PASS, `non_empty_null=0, empty_non_null=0`.
- `MORPH-POS-RESOLVES`: PASS, `0 unknown`.
- `MORPH-SEG-RENDER-PROVENANCE`: PASS.
- `MORPH-DIMENSION-RESOLVES`: PASS.
- All importer hard checks passed.

Importer persisted totals:

| Table | Expected by task | Observed | Result |
|---|---:|---:|---|
| quran_word_morphology | 77,432 | 77,432 | PASS |
| quran_word_morphology_segments | 128,219 | 128,219 | PASS |
| quran_roots | 1,642 | 1,642 | PASS |
| quran_lemmas | 4,817 | 4,817 | PASS |
| quran_lemma_analyses | 4,832 | 4,832 | PASS |
| quran_stems | 11,843 | 11,848 | FAIL |
| quran_pos_tags | 49 | 49 | PASS |

Because `quran_stems` differed from the task expectation, post-import acceptance stopped here. No fixes were attempted.

## 7. Read-Only Post-Import DB Checks Captured Before Stop

| Check | Observed | Result |
|---|---:|---|
| segment POS null/blank | 0 | PASS |
| unresolved segment POS FK | 0 | PASS |
| non-empty `form_buckwalter` with NULL `form_arabic_normalized` | 0 | PASS |
| empty `form_buckwalter` with NULL `form_arabic_normalized` | 208 | PASS |
| empty `form_buckwalter` with non-NULL `form_arabic_normalized` | 0 | PASS |
| duplicate `quran_lemmas.lemma_text` groups | 0 | PASS |
| `quran_lemma_analyses` rows | 4,832 | PASS |

Persisted dot anchor:

| Field | Value |
|---|---|
| segment_location | `12:101:14:2` |
| form_buckwalter | `.` |
| form_arabic_normalized | `ۦ` |
| pos | `PRON` |
| kind | `SUFFIX` |

## 8. Gate Results

| Gate | Result | Evidence |
|---|---|---|
| STAGED-ARTIFACT-COUNT | PASS | 77,432 records / 128,219 segments |
| STAGED-SEGMENT-POS-NON-BLANK | PASS | null POS=0, blank POS=0 |
| STAGED-FORM-ARABIC-NON-BLANK | PASS | non-empty BW with empty/null Arabic=0 |
| STAGED-DOT-RENDER-ANCHOR | PASS | `12:101:14:2` -> `.` / `ۦ` / `SUFFIX` / `PRON` |
| DB-FOUNDATION-PRESENT | PASS | quran_words total=83,668; readable=77,432; markers=6,236 |
| DB-WORDS-REBUILD-PRESENT | PASS | ordered/unique words-display counts match expected |
| ENRICHED-VALIDATION | PASS | dry validation 16/16 |
| ENRICHED-MORPHOLOGY-IMPORT | PASS | importer verdict PASS, persisted true |
| MORPH-SEG-RENDER-TOTAL | PASS | `non_empty_null=0, empty_non_null=0` |
| SEGMENT-POS-FK-RESOLVES | PASS | unresolved segment POS=0 |
| LEMMA-TEXT-COLLISION-NOT-REGRESSED | PASS | duplicate lemma_text groups=0 |
| QURAN-LEMMA-ANALYSES-PERSISTED | PASS | 4,832 rows |
| BOUNDARY-AYAHS-PRESERVED | PASS (dry validation) / NOT RUN (post-import deep DB check) | dry boundary gates passed; stopped before deeper DB boundary query after stem-count mismatch |
| CORRECTED-LEMMA-ANCHORS-PASS | PASS (dry validation) / NOT RUN (post-import deep DB check) | dry anchor gates passed; stopped before deeper DB anchor query after stem-count mismatch |
| POST-IMPORT-STEM-COUNT | FAIL | expected 11,843; observed 11,848 |

## 9. Failure Detail / Likely Layer

The original dot-render failure is resolved:

- staged artifact has `12:101:14:2 formArabic="ۦ"`.
- real import passed `MORPH-SEG-RENDER-TOTAL`.
- persisted DB row has `form_buckwalter='.'` and `form_arabic_normalized='ۦ'`.

The new stop condition is a post-import count expectation mismatch:

- expected `quran_stems=11843` per task.
- observed `quran_stems=11848` from importer report and read-only DB query.
- importer treats `MORPH-DIM-COUNTS` as warning/informational and still committed because all hard checks passed.

Likely layer: expected-count contract / enriched stem dimension count drift after the regenerated SourceAudit artifact, not a DB failure, render failure, POS failure, lemma_text collision, or quran_lemma_analyses persistence failure.

## 10. Final Status

- Did `import-morphology --enriched` complete? **Yes.** It committed with `Persisted=True`.
- Is the dot-render issue fixed in Dashboard? **Yes.** `MORPH-SEG-RENDER-TOTAL` passed and `12:101:14:2` persisted correctly.
- Is Feature 020 still blocked? **Yes, by acceptance criteria.** Post-import `quran_stems` count is `11,848`, not the expected `11,843`.
- Is a final clean reset acceptance still recommended? **Yes.** First reconcile whether `11,848` is the new correct stem count or diagnose the 5-row stem drift; then rerun a clean reset acceptance.
- Next action: investigate the 5 extra `quran_stems` rows/count drift against the regenerated SourceAudit artifact and the expected 11,843 baseline. Do not reset or mutate DB until that diagnostic decision is made.
