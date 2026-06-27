# Segment Dimension IDs — Phase 5 Verification Report

**Feature:** 017 Lexical Explorers Polish (prerequisite)
**Date:** 2026-06-27
**Scope:** Local clean reset/reseed + live morphology import verification only
**Branch context:** segment dimension IDs prerequisite (Phases 1–4 code + Phase 5 live verification)

---

## 1. Reset / migrate sequence (clean reset)

Executed in order:

| Step | Command | Result |
| --- | --- | --- |
| 1 | `./scripts/reset-db --yes` (drop only on first attempt; `update-db` blocked by stale sandbox NuGet paths) | Database `quran_dashboard` dropped |
| 2 | Remove stale `obj/` + `dotnet restore` with `NUGET_PACKAGES=/home/mohamed/.nuget/packages` | Sandbox path references cleared |
| 3 | `./scripts/update-db` | All migrations applied, including `20260627144247_AddSegmentDimensionIds` |
| 4 | `import-foundation` | surahs=114, ayahs=6236, words=83668 |
| 5 | `rebuild-words --force` | ordered=77432/77432, unique=21294/14783 |
| 6 | `import-morphology --force` | **PASS** after live-corpus resolver fixes (see §4) |
| 7 | `generate-i3rab --force` | 128,219 segments approved |

**Note:** Drop preceded migrate, per approved clean-reset order. Importer verbs used explicit `ConnectionStrings__QuranDashboardDb` from API user-secrets.

---

## 2. Live import totals

| Metric | Value |
| --- | ---: |
| morphology rows | 77,432 |
| segment rows | 128,219 |
| roots | 1,642 |
| lemmas | 4,793 |
| stems | 12,108 |
| pos tags | 49 |
| STEM segments with `lemma_id` | 72,990 |
| `stem_id` column on segments | absent (0) |

Canonical importer report:

- `resources/report/feature-017-segment-dimension-ids/morphology-import-report.json`
- `resources/report/feature-017-segment-dimension-ids/morphology-import-report.md`
- `resources/report/feature-017-segment-dimension-ids/simple-i3rab-generation-report.md`

---

## 3. Hard checks (all green)

All `SEG-*` checks passed on the live import, plus existing morphology checks and `MORPH-SOURCE-UNCHANGED`.

| Check | Observed |
| --- | --- |
| `SEG-LEMMA-ID-STEM-ONLY` | 0 violations |
| `SEG-LEMMA-ID-SINGLE-STEM-HEAD-CONSISTENT` | 0 violations |
| `SEG-LEMMA-ID-NO-FANOUT` | 0 violations |
| `SEG-LEMMA-ID-MULTI-STEM-RESOLVES` | 0 violations |
| `SEG-LEMMA-ID-REQUIRED-FOR-STEM` | 0 violations |
| `SEG-ROOT-ID-RESOLVES` | 0 violations |
| `SEG-ROOT-ID-CONSISTENT` | 0 violations |
| `SEG-DIM-NULL-SAFE` | 0 violations |
| `SEG-STEM-ID-ABSENT` | absent |
| `MORPH-SOURCE-UNCHANGED` | unchanged |

`generate-i3rab` completed successfully for all 128,219 segments after morphology commit.

---

## 4. Live-corpus fixes required during Phase 5

The first live `import-morphology --force` attempt failed. Resolver/check adjustments were required to match the approved DB verification policy on real data:

1. **Single-STEM head assignment before empty-buckwalter guard** — 86 words had QUL head `lemma_id` but empty segment `lemma_buckwalter`; segment must inherit head `lemma_id`.
2. **Expanded buckwalter alias index** — index single-STEM buckwalter variants so multi-STEM homograph suffix keys resolve.
3. **Arabic-form fallback** — when buckwalter lookup misses, match rendered segment form to `quran_lemmas.lemma_text`.
4. **Duplicate-buckwalter lowest-id tie-break** — per DB verification report for the 9 duplicate keys when form match is inconclusive.
5. **`SEG-DIM-NULL-SAFE` exception** — allow single-STEM head `lemma_id` when segment `lemma_buckwalter` is empty but head `lemma_id` is set.
6. **`SEG-LEMMA-ID-REQUIRED-FOR-STEM` scope** — require segment `lemma_id` only when word head `lemma_id` is non-null (~1,704 Buckwalter-only words legitimately remain null at both levels).

Files touched for these fixes:

- `MorphologyAssembler.cs`
- `MorphologySql.cs`
- `MorphologyValidationRunner.cs`
- `MorphologyAssemblerTests.cs`
- `MorphologyValidationFailureTests.cs`

---

## 5. Tests run after fixes

| Suite | Result |
| --- | --- |
| `dotnet build QuranDashboard.sln` | pass |
| `QuranDashboard.Tests.Quran.WordsMorphology` | **276** passed |
| `QuranDashboard.Tests.Quran.WordsSimpleI3rab` | **106** passed |

---

## 6. Out of scope (unchanged)

- No Lemma Details reader fix
- No frontend / Stems Explorer / POS / i'rab label changes
- No source corpus or staged resource mutation
- No commit in this step

---

## 7. Phase 5 verdict

**PASS** — local clean reset, migration, morphology reseed, and simple i'rab generation all succeeded with segment `lemma_id` / `root_id` populated and all `SEG-*` hard checks green on the live corpus.

**Next authorized step:** Phase 6 review/commit, then separate Lemma Details Option A reader work.
