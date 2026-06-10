# Implementation Plan: Quran Word Morphology Foundation

**Branch**: `004-word-morphology-foundation` | **Date**: 2026-06-10 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/004-word-morphology-foundation/spec.md`

> **Companion documents (long-form rationale, source of truth):**
> `docs/feature-004-word-morphology-foundation/feature-004-word-morphology-foundation-planning-report.md`,
> `docs/feature-004-word-morphology-foundation/feature-004-decisions-addendum.md`,
> `docs/feature-004-word-morphology-foundation/segment-arabic-rendering-capability-report.md`.
> **Governance:** `Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE,API_GUIDELINES}.md`,
> `CODING_PRINCIPLES.md`, `Backend/CLAUDE.md` (EF migration policy).

## Summary

Build the **word-morphology data foundation**: for every **readable** Quran word occurrence (77,432;
ayah markers excluded), a per-occurrence morphology record plus its ordered segments, sourced from the
**QAC aligned corpus** (classification/structure) and the **QUL** files (Arabic root/lemma/stem display).
Six new read-only tables: `quran_word_morphology`, `quran_word_morphology_segments`, `quran_roots`,
`quran_lemmas`, `quran_stems`, `quran_pos_tags`. Each non-empty segment also gets a flagged, derived
**normalized Arabic rendering** (Option B: `form_arabic_normalized` + `arabic_render_tier` +
`arabic_render_source`) that is **never** Mushaf text and **never** claimed an exact `qpcUthmani`
substring; the raw `form_buckwalter` is always retained.

**Technical approach:** unlike Feature 003 (DB-to-DB), Feature 004 is **source-driven** like the
Feature 002 importer — it reads local JSON files, assembles the morphology graph in memory (including
the deterministic Buckwalter→Arabic transliteration), then **bulk-loads via Npgsql binary `COPY`** and
validates inside **one transaction**, committing only if a hard-check gate passes (else rollback). It is
exposed as a **third console verb, `import-morphology`**, on the existing
`tools/QuranDashboard.DataImporter` host (operator/CI only — never HTTP). The importer reads **only** the
local, Git-ignored in-repo path `App/resources/import-sources/quran-morphology/`; it never reads the
external research workspace and never mutates `quran_words`. POS labels come from a curated **in-code
dictionary** seeded idempotently. **No API, UI, generated i3rab, or syntactic roles** are in scope.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (`net10.0`)
**Primary Dependencies**: EF Core `10.0.8`, `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.0` (binary
`COPY` bulk load via the Npgsql connection — mirrors `EfBulkQuranImportWriter`),
`Microsoft.Extensions.Hosting` `10.0.0` (console host + DI), `System.Text.Json` (source readers, as in
the Feature 002 JSON readers)
**Storage**: PostgreSQL — 6 new read-only tables built from **local JSON source files** + a read-only
join to the existing `quran_words` (`id`, `location`, `is_ayah_marker`); `quran_words` is never mutated
**Testing**: xUnit `2.9.3` + FluentAssertions `8.2.0` + Testcontainers.PostgreSql `4.4.0`
(`postgres:16-alpine`) in the existing `tests/QuranDashboard.Tests` project; plus pure unit tests for the
transliteration map (no DB)
**Target Platform**: Linux server, .NET 10 runtime
**Project Type**: Existing Backend Clean Architecture solution (7 projects incl. the `DataImporter`
console host) — **no new project**; one new verb (`import-morphology`) + new feature folders
**Performance Advisory**: parse the ~58 MB aligned corpus + three QUL files, assemble ~77,432 morphology
records / ~128,219 segments / ~14k–50k dimension rows, and bulk-`COPY` + validate in **one transaction**;
the operator-run timing target/observation is seconds to low minutes, not a user-facing path and not a
hard gate (`CommandTimeout` ~600 s)
**Constraints**: schema-only migration (no `HasData`); **source-driven** from the local Git-ignored
`quran-morphology/` path only (never the external Desktop path; runtime has no dependency on it);
`quran_words` and all Feature 002/003 tables never mutated; **per readable occurrence** grain keyed to
`quran_word_id`; ayah markers excluded; Option B segment rendering (**never** Uthmani/Mushaf, empties →
`NULL`, fragile rows flagged not invented); dimensions dedup on **Arabic** text — a corpus Buckwalter-only
value with no QUL Arabic ⇒ **null** `root_id`/`lemma_id` (clarification Q1); verb voice = `passive` iff
PASS else `active` by convention, **no inferred flag** (clarification Q2); POS vocabulary from an in-code
dictionary; **no** physical `quran_verbs` table; atomic + hard-gated validation + Markdown/JSON report;
Quranic data safety (raw `form_buckwalter` retained; source files read-only; source-safe test fixtures);
**no API / UI / i3rab / syntactic roles / `qpcUthmani` offsets**
**Scale/Scope**: 6 tables; ≈ 77,432 morphology + ≈ 128,219 segments + distinct roots/lemmas/stems
(dimension counts derived & reported, not hardcoded) + ≈ 30 POS tags; repeatable operator-run import

*No unresolved NEEDS CLARIFICATION items — the spec, its 2026-06-10 Clarifications, and the three
planning docs are exhaustive; all design decisions are settled in research.md.*

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution (`.specify/memory/constitution.md`) is an **unratified template**
(placeholders only). In its absence the governance gates are the workspace architecture docs (same
posture as Features 002/003). Status:

| Gate (source) | Status | Notes |
|---|---|---|
| Clean Architecture dependency direction (`CLEAN_ARCHITECTURE.md`) | ✅ Pass | Domain holds 6 pure entities + 4 enums; Application orchestrates via Abstractions; Infrastructure implements JSON readers, the transliteration renderer, the bulk writer, validation, and the report writer; the console host is composition only. Infra references Application.Abstractions + Domain (not Application). |
| Domain/feature foldering, no dumping folders (`BACKEND_STRUCTURE.md`) | ✅ Pass | All new types under `Quran/Words/Morphology/` (Domain, Abstractions, Infrastructure) and `Quran/Words/ImportMorphology/` (Application). No `Enums/Models/Helpers/DTOs` dumping folders. |
| EF migration policy (`Backend/CLAUDE.md`) | ✅ Pass | One **schema-only** migration (6 tables) generated by EF tooling on explicit request; **no `HasData`** (POS vocabulary is seeded by the importer, not the migration); no DB update unless requested. |
| API boundary / `ApiResponse` (`API_GUIDELINES.md`) | ✅ N/A | No API in this feature. |
| Quranic data safety (`CLEAN_ARCHITECTURE.md`, root `CLAUDE.md`) | ✅ Pass | `quran_words` never mutated; raw `form_buckwalter` always retained; empty forms → `NULL`; fragile rows flagged not invented; `form_arabic_normalized` never written from Uthmani/QPC and never used as Mushaf text; local source files read-only (`MORPH-SOURCE-UNCHANGED`); every started import build attempt emits a Markdown + JSON report; early refusals such as source/manifest mismatch, missing/empty `quran_words`, or non-empty targets without `--force` write no report artifact and report only to the console; tests use single-word, source-safe synthetic tokens (no verse passages). |
| Strong typing / no leaking EF types across boundaries | ✅ Pass | Abstractions expose source DTOs + result/report records, never EF entities. |
| No runtime / request-path work | ✅ Pass | Operator-run console verb only; no HTTP, no on-request work. |
| File-size/responsibility thresholds (`BACKEND_STRUCTURE.md`) | ⚠️ Watch | The assembler + bulk writer + validation could approach the soft thresholds. Mitigated by separating: per-file JSON readers, `BuckwalterArabicMap` (data), `SegmentArabicRenderer` (Option B logic), `MorphologyAssembler` (corpus+QUL → domain graph), `EfBulkMorphologyWriter` (COPY + transaction), `MorphologySql` (validation SQL text), and the report writer — each cohesive and single-purpose. |
| Transliteration correctness (Quranic data safety, new) | ✅ Pass (gated) | `BuckwalterArabicMap` is a correctness-critical asset with its own unit tests and the **fail-closed** `MORPH-SEG-CHARSET` gate (an unmapped character refuses the import rather than emitting `�`). |

**Complexity note:** this feature adds **no new project** — it reuses the existing `DataImporter`
console host (new verb) and the existing `tests/QuranDashboard.Tests` project. No Complexity Tracking
entries are required.

## Project Structure

### Documentation (this feature)

```text
specs/004-word-morphology-foundation/
├── plan.md              # This file
├── research.md          # Phase 0 — decisions & rationale
├── data-model.md        # Phase 1 — the six tables: columns, keys, indexes, derivation
├── quickstart.md        # Phase 1 — stage files, run the import & verify
├── contracts/           # Phase 1 — import abstractions, CLI verb, validation/report schema
│   ├── cli-verb.md
│   ├── morphology-abstractions.md
│   └── validation-report.schema.md
├── checklists/
│   └── requirements.md  # Spec quality checklist (from /speckit-specify)
└── tasks.md             # Phase 2 — created by /speckit-tasks (NOT here)
```

### Source Code (repository root)

```text
Backend/
  domain/QuranDashboard.Domain/Quran/Words/Morphology/
    WordMorphology.cs              # entity: quran_word_morphology (PK/FK quran_word_id)
    WordMorphologySegment.cs       # entity: quran_word_morphology_segments
    QuranRoot.cs                   # entity: quran_roots
    QuranLemma.cs                  # entity: quran_lemmas
    QuranStem.cs                   # entity: quran_stems
    PosTag.cs                      # entity: quran_pos_tags (code PK)
    SegmentKind.cs                 # enum: Prefix / Stem / Suffix
    VerbTense.cs                   # enum: Past / Present / Imperative
    VerbVoice.cs                   # enum: Active / Passive
    MorphologicalCase.cs           # enum: Nominative / Accusative / Genitive

  application/QuranDashboard.Application.Abstractions/Quran/Words/Morphology/
    IMorphologyImportSource.cs     # read local staged files → assembled MorphologySourceData
    IMorphologyImportWriter.cs     # one-transaction COPY + validation → commit/rollback
    IMorphologyReportWriter.cs     # Markdown+JSON report writer contract
    MorphologySourceData.cs        # assembled in-memory graph (words + segments + dimension maps)
    MorphologyImportResult.cs      # totals + checks + warnings + verdict + persisted + forced
    MorphologyImportTotals.cs      # per-table counts + readable count + tier distribution
    MorphologyCheckResult.cs       # one validation check (id, severity, expected, observed, passed)
    MorphologyInvariants.cs        # constants (ExpectedReadableWords=77_432, ExpectedEmptyForms=208…) + messages
    AlignedWordDto.cs / AlignedSegmentDto.cs / QulValueDto.cs   # source DTOs (no EF types)

  application/QuranDashboard.Application/Quran/Words/ImportMorphology/
    ImportMorphologyCommand.cs
    ImportMorphologyHandler.cs     # refuse/force → source → write+validate → report → exit code
    ImportMorphologyResult.cs      # CLI-facing success/failure/refused + exit code

  infrastructure/QuranDashboard.Infrastructure/
    Files/Quran/Morphology/
      MorphologyManifestReader.cs  # read + verify manifest (size/sha256) before & after
      JsonAlignedCorpusReader.cs   # parse quranic-corpus-morphology-qpc-aligned.json
      JsonQulRootReader.cs / JsonQulLemmaReader.cs / JsonQulStemReader.cs
      BuckwalterArabicMap.cs       # the QAC Buckwalter→Arabic table (single source of truth)
      SegmentArabicRenderer.cs     # form → form_arabic_normalized + tier (Option B)
      MorphologyAssembler.cs       # corpus+QUL → per-word morphology + segments + dimensions
      MorphologyImportSource.cs    # IMorphologyImportSource: orchestrates readers → MorphologySourceData
    Persistence/Configurations/Quran/Words/Morphology/
      WordMorphologyConfiguration.cs … PosTagConfiguration.cs   # 6 EF configs
    Persistence/Repositories/Quran/Morphology/
      EfBulkMorphologyWriter.cs    # COPY dims+pos+morphology+segments + validation, one transaction
      MorphologySql.cs             # validation SQL text (kept out of the writer)
    Reports/Quran/
      MarkdownJsonMorphologyReportWriter.cs
    Persistence/QuranDashboardDbContext.cs   # +6 DbSets (auto-discovered configs)
    Migrations/                              # +1 schema-only migration (generated on request)
    DependencyInjection.cs                   # register source, writer, report writer, renderer, map

  tools/QuranDashboard.DataImporter/
    Program.cs                     # verb dispatch: import-foundation | rebuild-words | import-morphology

  tests/QuranDashboard.Tests/Quran/WordsMorphology/
    MorphologyImportTests.cs          # e2e: stage small fixture → import → assert tables/validation/atomicity
    BuckwalterArabicMapTests.cs       # charset coverage + deterministic mapping (pure unit, no DB)
    MorphologySegmentRenderingTests.cs# tiers, empties→NULL, never-Uthmani guard, raw form retained
    MorphologyPosResolutionTests.cs   # head_pos/segment pos resolve; POS vocabulary seeded
    MorphologyVerbFeatureTests.cs     # tense/voice consistency; active-by-default (no flag)
    MorphologyDimensionTests.cs       # dedup on Arabic; Buckwalter-only → null link; no dangling
    MorphologyRefusalForceTests.cs    # refuse-unless-empty, --force, source tables + files untouched
    MorphologyValidationFailureTests.cs # injected violation → rollback + failure report
    MorphologyImportTestFixture.cs    # Testcontainers Postgres + synthetic source-safe staged files
```

**Structure Decision**: Reuse the existing solution. All new types live under feature folders
(`Quran/Words/Morphology/`, `Quran/Words/ImportMorphology/`) per `BACKEND_STRUCTURE.md`. The load is a
**source-driven** operator action mirroring the Feature 002 importer (JSON readers → in-memory assembly →
Npgsql binary `COPY` → validate → commit), exposed as a new verb on the existing console host — **not** a
new project and **not** an HTTP endpoint. See research.md for the rationale and rejected alternatives.

## Complexity Tracking

*No constitution violations. No new projects (the `DataImporter` host and `QuranDashboard.Tests` project
already exist). The only watch item is assembler/writer file size (⚠️ above), mitigated by the
single-purpose split listed under Project Structure.*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| _(none)_ | — | — |
