# Feature 004 — Quran Word Morphology Foundation (Final Completion Report)

**Date:** 2026-06-11
**Status:** Complete — final verdict **PASS WITH NOTES**. No blocking issues.
**Scope:** Documentation/record only. No code, migrations, DB changes, importer runs, or commits
were made to produce this report.

This is the *what was built / what was verified* record for Feature 004. The forward-looking plan
lives under `docs/feature-004-word-morphology-foundation/`; the live generated import report lives
under `resources/report/words-morphology/` (git-ignored — see §9).

---

## 1. Verdict

**PASS WITH NOTES — complete.** The feature is fully implemented and committed, the build is clean,
all WordsMorphology tests pass, and the full real-source import produces the expected result with all
hard checks green. Outstanding items are non-blocking (see §9).

---

## 2. Scope Summary

Data foundation only. Explicitly **in scope**: per-occurrence morphology for every readable
`quran_words` row, ordered segments, root/lemma/stem dimensions, a POS controlled vocabulary, and a
derived Arabic reading aid for segments.

Explicitly **out of scope** (locked decisions, all honored):

- No UI.
- No API endpoints (importer is console/CI-only).
- No generated Arabic i3rab.
- No syntactic roles (فاعل / مفعول / مبتدأ / خبر).
- No `quran_words` writes (read-only join only).
- Ayah markers excluded (morphology is per readable occurrence).
- No physical `quran_verbs` table.
- Source files never modified.

---

## 3. Schema Summary

Six tables make up the foundation:

| Table | Purpose |
| --- | --- |
| `quran_word_morphology` | One row per readable word (head POS, segment count, verb tense/voice, case, dimension links). |
| `quran_word_morphology_segments` | Ordered segments per word (kind, pos, Buckwalter form, derived Arabic render + tier, features). |
| `quran_roots` | Deduped Arabic roots (words_count, distinct_lemmas_count, first_word_order_in_mushaf). |
| `quran_lemmas` | Deduped Arabic lemmas (optional root link). |
| `quran_stems` | Deduped Arabic stems. |
| `quran_pos_tags` | Curated POS controlled vocabulary (code, Arabic/English labels, category, sort order). |

FK-safe write order: `quran_pos_tags` → roots/lemmas/stems → `quran_word_morphology` → segments.

---

## 4. Migration Summary

- **Migration name:** `20260610155434_AddQuranWordMorphology`
- Creates **exactly the six tables** above and their indexes/foreign keys.
- **No `HasData` / `InsertData`** — the POS vocabulary is curated reference data seeded by the
  importer inside the import transaction, not a migration seed.
- Generated on explicit request (T053). `dotnet ef database update` was **not** run.

---

## 5. Import Summary (full real-source run)

| Metric | Value |
| --- | --- |
| Verdict | **PASS** |
| Persisted | **true** |
| Morphology rows | **77,432** |
| Segments | **128,219** |
| Roots | **1,642** |
| Lemmas | **4,793** |
| Stems | **12,108** |
| POS tags | **49** |
| Null renders (empty forms) | **208** |

Render tiers:

| Tier | Count |
| --- | --- |
| clean | 120,624 |
| quranic_marks | 6,890 |
| review | 496 |
| multiword | 1 |

- Whole-word agreement ≈ **79.83 %** — advisory warning only, never a hard gate.
- **All 13 hard checks passed**, including `MORPH-SOURCE-UNCHANGED` (local source files byte-identical
  before and after the run).

---

## 6. Arabic Rendering Decision

- `form_arabic_normalized` is **derived from Buckwalter transliteration** of `form_buckwalter` via the
  single `BuckwalterArabicMap`, with `arabic_render_source = "buckwalter-transliteration"`.
- It is a **reading aid only**.
- It is **not Mushaf text** and must not be displayed as authoritative Quran text.
- It is **not** guaranteed to be an exact `qpcUthmani` substring (hence the advisory whole-word
  agreement metric).
- Empty forms render `NULL` (208 rows); the raw `form_buckwalter` is always retained for traceability.

---

## 7. T056 Refactor Summary

`EfBulkMorphologyWriter` exceeded the repository hard file-size threshold and was split into focused,
single-responsibility classes. **Behavior was preserved** (verified by re-running the full import and
the test suite after the split).

| Class | Responsibility |
| --- | --- |
| `EfBulkMorphologyWriter` | Transaction orchestration: gate → COPY → validate → commit-iff-all-pass / rollback. |
| `MorphologyBulkCopier` | FK-safe binary `COPY` write path. |
| `MorphologyValidationRunner` | Hard-check gate (US1 structural + US3 rendering/dimension + POS-resolves). |
| `MorphologyImportReportBuilder` | Per-run totals and warnings/refusal results. |
| `MorphologyCommandExecutor` / `MorphologyImportConstants` | Shared Npgsql command helpers and verdict/severity constants. |

---

## 8. Verification

- `dotnet build Backend`: **0 warnings / 0 errors**.
- WordsMorphology tests: **83 / 83 pass** (Testcontainers `postgres:16-alpine`, source-safe synthetic
  tokens only).
- Full real-source import: **PASS** (see §5).

---

## 9. Non-Blocking Notes

- The live generated report (`morphology-import-report.md` / `.json`, plus the multi-STEM
  investigation report) lives under `resources/report/words-morphology/`, which is **git-ignored**;
  this tracked summary is the committed record.
- `pos_tags = 49` is correct — the seed covers **every** POS code emitted by the real corpus
  (the earlier "≈30" figure was a planning estimate).
- Whole-word agreement (≈79.83 %) and elapsed-time/performance observations are **advisory only** and
  never affect the verdict.
