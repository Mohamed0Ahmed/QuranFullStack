# Morphology Segment Render Offender Diagnostic Report

- Feature: 020 — Lexical Polish and Project Hygiene
- Scope: diagnostic only
- Date: 2026-07-04

## Verdict

**OFFENDER_IDENTIFIED**

Exactly one segment matches the failed import gate predicate: non-empty `form_buckwalter` with NULL computed/persisted `form_arabic_normalized`.

## Offending Segment

| Field | Value |
|---|---|
| word location | `12:101:14` |
| segment location | `12:101:14:2` |
| surah:ayah:word | `12:101:14` |
| segment number | `2` |
| quranWordId | `33346` |
| quranWordIdVerifiedAgainstDashboard | `true` |
| word textUthmani | `وَلِىِّۦ` |
| word textImlaei | `وليي` |
| word textUthmaniSimple | `ولى` |
| corpusPresent | `true` |
| kind | `SUFFIX` |
| pos | `PRON` |
| form / formBuckwalter | `.` |
| formArabic | empty string |
| featuresRaw | `SUFFIX|PRON:1S` |
| features | `PRON:1S` |
| lemmaBuckwalter / lemmaArabic | null / null |
| rootBuckwalter / rootArabic | null / null |
| stemBuckwalter / stemArabic | null / null |
| lemma/root/stem mapping statuses | `NOT_APPLICABLE` |
| projected form_buckwalter | `.` |
| projected form_arabic_normalized | `NULL` |
| projected arabic_render_tier | `clean` |
| projected arabic_render_source | `corpus_enriched_bridge` |
| form empty? | no; projected `formBuckwalter != ""` |

Nearby raw JSON summary:

- Word record `12:101:14` has text `وَلِىِّۦ` and two segments.
- Segment 1 is STEM with `formBuckwalter="waliY~i"`.
- Segment 2 is SUFFIX/PRON with `formBuckwalter="."`, `formArabic=""`, `featuresRaw="SUFFIX|PRON:1S"`, and no lemma/root/stem values.

## Existing Report Evidence

`resources/report/words-morphology/morphology-import-report.md` and `.json` contain only aggregate failure data, not offender details:

- `MORPH-SEG-RENDER-TOTAL`: expected non-empty form -> non-null render; empty form -> NULL.
- observed `non_empty_null=1, empty_non_null=0`.
- expected empty-form NULL cases are reported separately as `208` and are not this offender.
- all other hard checks passed, including charset, tier validity, enriched render provenance, POS resolution, and dimension resolution.

`Backend/report/feature-020-lexical-polish-and-project-hygiene/enriched-morphology-clean-reset-acceptance-report.md` confirms the same stopped failure and rollback, but intentionally does not identify the segment.

## Diagnostic Probe Evidence

Read-only probe scanned:

`resources/import-sources/quran-enriched-morphology/corpus-based-enriched-morphology.dashboard-ready.json`

Probe applied the exact enriched projection predicate:

- `formBuckwalter` projects as `segment.FormBuckwalter ?? ""`.
- `formArabicNormalized` projects as `NULL` when `formArabic` is null or empty string.
- offender predicate: projected `formBuckwalter != ""` and projected `formArabicNormalized == NULL`.

Probe result:

| Metric | Count |
|---|---:|
| records | 77,432 |
| non-empty projected forms | 128,011 |
| empty-form NULL renders | 208 |
| empty-form non-NULL renders | 0 |
| non-empty form with NULL render | 1 |

The one probe offender is `12:101:14:2`, matching the importer report count exactly.

## Dashboard Render Path Involved

Enriched import path:

- `EnrichedMorphologyReader.EnrichedMorphologySegment` reads raw `FormBuckwalter` and `FormArabic` from the staged artifact.
- `EnrichedDimensionBuilder.ProjectSegments` projects `formBuckwalter = segment.FormBuckwalter ?? string.Empty` and `FormArabicNormalized = string.IsNullOrEmpty(segment.FormArabic) ? null : segment.FormArabic`.
- `EnrichedDimensionBuilder.ProjectSegments` hardcodes `RenderTier = "clean"` and `RenderSource = "corpus_enriched_bridge"` for enriched segments.
- `MorphologyBulkCopier.CopySegmentsAsync` writes projected `FormBuckwalter`, `FormArabicNormalized`, `RenderTier`, and `RenderSource` to `quran_word_morphology_segments`.
- `MorphologySql.CheckSegRenderTotalNonEmpty` counts rows where `form_buckwalter <> '' AND form_arabic_normalized IS NULL`.
- `MorphologyValidationRunner.AddUs3ChecksAsync` reports that count as `MORPH-SEG-RENDER-TOTAL` / `non_empty_null`.

Important renderer comparison:

- Legacy `SegmentArabicRenderer.Render` would not return NULL for `.` because `BuckwalterArabicMap` maps `.` to U+06E6 (`ۦ`).
- Legacy tier classification would be `quranic_marks` for `.`.
- The enriched path intentionally does not call `SegmentArabicRenderer`; it trusts `formArabic` from the enriched artifact.

## Corpus Comparison

Staged aligned Corpus source:

`resources/import-sources/quran-morphology/corpus/quranic-corpus-morphology-qpc-aligned.json`

Matching record:

- `qpcLocation`: `12:101:14`
- `originalCorpusLocation`: `12:101:14`
- `alignmentType`: `direct`
- `qpcUthmani`: `وَلِىِّۦ`
- segment `12:101:14:2`: `form="."`, `posColumn="PRON"`, `kind="SUFFIX"`, `features="SUFFIX|PRON:1S"`, no root/lemma.

Conclusion from comparison:

- SourceAudit did not alter the Buckwalter form relative to the staged aligned Corpus source.
- The Buckwalter symbol `.` is present in Corpus and is a known Dashboard-mapped symbol.
- The enriched artifact carries the same non-empty form but leaves `formArabic` empty.

## Likely Root Cause Layer

**Source artifact issue.**

The staged enriched artifact violates the enriched import contract for one valid Corpus segment: `formBuckwalter="."` is non-empty and known/mappable, but `formArabic` is empty. Dashboard enriched projection therefore computes `FormArabicNormalized = NULL` exactly as coded.

Not likely Dashboard Buckwalter mapping issue:

- `BuckwalterArabicMap` contains `.` -> `ۦ`.
- The legacy renderer can render this form.

Not validation/reporting issue:

- The SQL predicate matches the projection result.
- Diagnostic count matches importer report: exactly `1` offender, `0` empty-form non-null rows.

## Recommended Next Fix Direction

Fix the enriched artifact generation/source bridge so segment `12:101:14:2` emits a non-empty `formArabic` for `formBuckwalter="."` (expected Dashboard map render: `ۦ`), or explicitly define this as an intentional empty-form segment by changing the source form semantics upstream.

If choosing a Dashboard-side fallback instead, update the enriched projection deliberately to handle blank `formArabic` with non-empty valid Buckwalter forms, then re-check tier/source/provenance semantics. Do not fix this by weakening `MORPH-SEG-RENDER-TOTAL`.

## Safety Confirmation

- no `reset-db` run
- no DB import/write run
- no source artifact regeneration
- no Dashboard schema/migration changes
- no `PosTagSeed` changes
- no lemma_text / quran_lemma_analyses decision changes
- no commit
- no production code changes
