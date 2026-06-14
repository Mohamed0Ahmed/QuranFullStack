# Implementation Plan: Quran Mutashabihat Foundation

**Branch**: `006-quran-mutashabihat-foundation` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/006-quran-mutashabihat-foundation/spec.md`

> **Companion documents (long-form rationale, source of truth):**
> `docs/feature-006-quran-mutashabihat-foundation/feature-006-quran-mutashabihat-foundation-planning-report.md`
> (the locked v1 plan — tables, pipeline, validation taxonomy),
> `docs/feature-006-quran-mutashabihat-foundation/mutashabihat-data-capability-report.md`
> (the validated source inventory; every count below was re-derived from the raw files there).
> **Governance:** `Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE,API_GUIDELINES}.md`,
> `CODING_PRINCIPLES.md`, `Backend/CLAUDE.md` (EF migration policy).

## Summary

Build the **Quran mutashabihat (repeated-phrase / similar-ayah) data foundation**: import **two
independent, pre-staged local JSON datasets** into **three new read-only PostgreSQL tables**, keyed to the
existing `quran_ayahs` foundation.

1. **Mutashabihat ul Quran — repeated-phrase groups** (`mutashabihat-ul-quran/phrases.json`): **814**
   groups → `quran_mutashabihat_groups`, with **3,557** stored unique occurrences (from **3,558** raw
   source occurrence entries after collapsing **1** duplicate identical occurrence) →
   `quran_mutashabihat_occurrences`.
2. **Similar Ayahs — directed scored links** (`similar-ayahs/matching-ayah.json`): **3,552** directed
   source→target links across **1,162** source ayahs → `quran_similar_ayah_links`.

Every ayah reference (group `source.key`, every occurrence `verse_key`, both ends of every link) is
resolved against `quran_ayahs.verse_key` (unique) to an integer `ayah_id`; **all 3,084** distinct
referenced verse_keys resolve (0 invalid / 0 missing). Counters are **recomputed** from the actual
occurrence data (the source's pre-computed counters are stale for tens of groups). `coverage` is stored
**raw** (range 5–200; the 4 rows > 100 are kept, not clamped). Directed links are stored **exactly** as
the source (no synthesized reverse rows). The two datasets stay in **separate tables** (no merged /
polymorphic relations table); `phrase_verses.json` is **not** stored (it is a derivable reverse index).
The new tables store **references and word positions only — never any copied Quran text**.

**Technical approach.** This is a **source-driven** import, identical in shape to the Feature 002/004
importer: read the two local JSON files + the manifest, assemble the relationship graph in memory
(resolving `verse_key → ayah_id`, recomputing counters, flagging the representative occurrence), validate
behind a hard gate, then **bulk-load via Npgsql binary `COPY`** and validate inside **one transaction**,
committing only if every hard check passes (else rollback). It is exposed as a **new console verb,
`import-mutashabihat`**, on the existing `tools/QuranDashboard.DataImporter` host (operator/CI only —
never HTTP). The importer reads **only** the local, Git-ignored staged package
`App/resources/import-sources/mutashabihat/`; it never mutates `quran_ayahs`, `quran_words`, the Quran
text, or the source files. **No API, UI, read model, reverse links, `phrase_verses` table, or polymorphic
merge** is in scope.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (`net10.0`)
**Primary Dependencies**: EF Core `10.0.8`, `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.0` (binary
`COPY` bulk load via the Npgsql connection — mirrors `EfBulkMorphologyWriter` / `EfBulkQuranImportWriter`),
`Microsoft.Extensions.Hosting` `10.0.0` (console host + DI), `System.Text.Json` (source readers, as in
the Feature 002/004 JSON readers)
**Storage**: PostgreSQL — 3 new read-only tables built from **two local JSON source files** + a read-only
join to the existing `quran_ayahs` (`id`, `verse_key`, and `words_count_real` for the optional word-range
warning); `quran_ayahs` and `quran_words` are never mutated
**Testing**: xUnit `2.9.3` + FluentAssertions `8.2.0` + Testcontainers.PostgreSql `4.4.0`
(`postgres:16-alpine`) in the existing `tests/QuranDashboard.Tests` project; plus pure unit tests for the
readers/assembler (no DB)
**Target Platform**: Linux server, .NET 10 runtime
**Project Type**: Existing Backend Clean Architecture solution (the `DataImporter` console host already has
`import-foundation | rebuild-words | import-morphology | generate-i3rab`) — **no new project**; one new
verb (`import-mutashabihat`) + new feature folders under `Quran/Mutashabihat/`
**Performance Advisory**: parse two small JSON files (`phrases.json` ≈ 134 KB / 814 groups,
`matching-ayah.json` ≈ 365 KB / 1,162 sources), assemble ≈ 814 groups + 3,557 occurrences + 3,552 links
(≈ 7,923 rows total), and bulk-`COPY` + validate in **one transaction**; the operator-run timing is
sub-second to low seconds (trivially small vs. morphology's ~205k rows); not a user-facing path and not a
hard gate (`CommandTimeout` reuse from the existing writers)
**Constraints**: schema-only migration (no `HasData`); **source-driven** from the local Git-ignored
`mutashabihat/` staged package only; `quran_ayahs`, `quran_words`, the Quran text, and all prior features'
tables never mutated; every ayah reference resolved via `verse_key → ayah_id` and stored as an `ayah_id`
FK (**never** raw verse_key strings); counters **recomputed** (source counters stale); `coverage` stored
**raw** (no clamp; 4 rows > 100 kept); directed links stored **faithful** (no synthesized reverse rows);
**no** `phrase_verses` table; **no** merged/polymorphic relations table; **no** Quran text copied (refs +
word positions only); atomic + hard-gated validation + Markdown/JSON report; refuse-unless-empty /
`--force`; source-unchanged re-verify before commit; Quranic data safety (anomalies recorded as warnings,
not corrected; source files read-only; source-safe synthetic test fixtures)
**Scale/Scope**: 3 tables; ≈ 814 groups + 3,557 occurrences + 3,552 links (≈ 7,923 rows) over 3,084
distinct referenced ayahs; repeatable operator-run import

*No unresolved NEEDS CLARIFICATION items — the spec (with its 2026-06-13 Clarifications), the planning
report, and the capability report are exhaustive; every design decision is settled in research.md.*

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution (`.specify/memory/constitution.md`) is an **unratified template** (placeholders
only). In its absence the governance gates are the workspace architecture docs (same posture as Features
002/004/005). Status:

| Gate (source) | Status | Notes |
|---|---|---|
| Clean Architecture dependency direction (`CLEAN_ARCHITECTURE.md`) | ✅ Pass | Domain holds 3 pure entities; Application orchestrates via Abstractions; Infrastructure implements the two JSON readers, the assembler (verse_key→ayah_id resolution + counter recompute), the bulk writer, validation SQL, and the report writer; the console host is composition-only. Infrastructure references Application.Abstractions + Domain (not Application). |
| Domain/feature foldering, no dumping folders (`BACKEND_STRUCTURE.md`) | ✅ Pass | All new types under `Quran/Mutashabihat/` (Domain, Abstractions, Infrastructure) and `Quran/Mutashabihat/ImportMutashabihat/` (Application). No `Enums/Models/Helpers/DTOs` dumping folders. The two datasets are modeled as two separate table sets — **no** polymorphic merge. |
| EF migration policy (`Backend/CLAUDE.md`) | ✅ Pass | One **schema-only** migration (3 tables) generated by EF tooling on explicit request; **no `HasData`** (there is no controlled vocabulary to seed); no DB update unless requested. |
| API boundary / `ApiResponse` (`API_GUIDELINES.md`) | ✅ N/A | No API in this feature. |
| Quranic data safety (`CLEAN_ARCHITECTURE.md`, root `CLAUDE.md`) | ✅ Pass | `quran_ayahs` / `quran_words` / Quran text never mutated; **no ayah text copied** (refs + word positions only); `coverage` stored raw (anomalies flagged, never corrected); directed links faithful (no invented reverse rows); local source files read-only (`MUT-SOURCE-UNCHANGED`); every started build emits a Markdown + JSON report; early refusals (missing file, checksum/size mismatch, non-empty targets without `--force`, missing/empty `quran_ayahs`) write no report artifact and report only to the console; tests use tiny synthetic groups/links (no real verse passages). |
| Strong typing / no leaking EF types across boundaries | ✅ Pass | Abstractions expose source DTOs + result/report records, never EF entities. |
| No runtime / request-path work | ✅ Pass | Operator-run console verb only; no HTTP, no on-request work. |
| File-size/responsibility thresholds (`BACKEND_STRUCTURE.md`) | ✅ Pass | Small feature. The assembler is the only file with real logic and stays well under the service soft threshold; it is split from the per-file readers, the bulk writer (`EfBulkMutashabihatWriter`), the validation SQL (`MutashabihatSql`), and the report writer — each cohesive and single-purpose. |

**Complexity note:** this feature adds **no new project** — it reuses the existing `DataImporter` console
host (new verb) and the existing `tests/QuranDashboard.Tests` project. No Complexity Tracking entries are
required.

## Project Structure

### Documentation (this feature)

```text
specs/006-quran-mutashabihat-foundation/
├── plan.md              # This file
├── research.md          # Phase 0 — decisions & rationale
├── data-model.md        # Phase 1 — the three tables: columns, keys, indexes, derivation, invariants
├── quickstart.md        # Phase 1 — stage files, create schema, run the import & verify
├── contracts/           # Phase 1 — import abstractions, CLI verb, validation/report schema
│   ├── cli-verb.md
│   ├── mutashabihat-abstractions.md
│   └── validation-report.schema.md
├── checklists/
│   └── requirements.md  # Spec quality checklist (from /speckit-specify)
└── tasks.md             # Phase 2 — created by /speckit-tasks (NOT here)
```

### Source Code (repository root)

```text
Backend/
  domain/QuranDashboard.Domain/Quran/Mutashabihat/
    MutashabihatGroup.cs            # entity: quran_mutashabihat_groups (surrogate id PK)
    MutashabihatOccurrence.cs       # entity: quran_mutashabihat_occurrences (surrogate id PK)
    SimilarAyahLink.cs              # entity: quran_similar_ayah_links (surrogate id PK)

  application/QuranDashboard.Application.Abstractions/Quran/Mutashabihat/
    IMutashabihatImportSource.cs    # read+verify staged package → assembled MutashabihatSourceData
    IMutashabihatImportWriter.cs    # one-transaction COPY + validation → commit/rollback
    IMutashabihatReportWriter.cs    # Markdown+JSON report writer contract
    MutashabihatSourceData.cs       # assembled in-memory graph (groups + occurrences + links)
    MutashabihatImportResult.cs     # totals + checks + warnings + info + verdict + persisted + forced
    MutashabihatImportTotals.cs     # per-table counts + raw occurrence count + distinct sources
    MutashabihatCheckResult.cs      # one validation check (id, severity, expected, observed, passed)
    MutashabihatInvariants.cs       # expected-count constants + messages (ExpectedGroups=814, …)
    PhraseGroupDto.cs / OccurrenceDto.cs / SimilarLinkDto.cs   # source DTOs (no EF types)

  application/QuranDashboard.Application/Quran/Mutashabihat/ImportMutashabihat/
    ImportMutashabihatCommand.cs
    ImportMutashabihatHandler.cs    # refuse/force → source → write+validate → report → exit code
    ImportMutashabihatResult.cs     # CLI-facing success/failure/refused + exit code

  infrastructure/QuranDashboard.Infrastructure/
    Files/Quran/Mutashabihat/
      MutashabihatManifestReader.cs # read + verify manifest (file set / size / sha256) before & after
      JsonPhrasesReader.cs          # parse phrases.json → group + occurrence DTOs
      JsonSimilarAyahReader.cs      # parse matching-ayah.json → directed link DTOs
      MutashabihatAssembler.cs      # verse_key→ayah_id resolution, counter recompute, representative flag
      MutashabihatImportSource.cs   # IMutashabihatImportSource: orchestrates readers → SourceData
    Persistence/Configurations/Quran/Mutashabihat/
      MutashabihatGroupConfiguration.cs
      MutashabihatOccurrenceConfiguration.cs
      SimilarAyahLinkConfiguration.cs
    Persistence/Repositories/Quran/Mutashabihat/
      EfBulkMutashabihatWriter.cs   # COPY groups → occurrences → links + validation, one transaction
      MutashabihatSql.cs            # validation SQL text (kept out of the writer)
    Reports/Quran/Mutashabihat/
      MarkdownJsonMutashabihatReportWriter.cs
    Persistence/QuranDashboardDbContext.cs   # +3 DbSets (auto-discovered configs)
    Migrations/                              # +1 schema-only migration (generated on request)
    DependencyInjection.cs                   # register source, writer, report writer

  tools/QuranDashboard.DataImporter/
    Program.cs                      # verb dispatch: + import-mutashabihat

  tests/QuranDashboard.Tests/Quran/Mutashabihat/
    MutashabihatImportTests.cs           # e2e: stage small fixture → import → assert tables/validation/atomicity
    MutashabihatReaderTests.cs           # phrases/matching-ayah parse into expected DTO shapes (pure, no DB)
    MutashabihatAssemblerTests.cs        # ayah_id resolution; counter recompute; representative flag; source-key-absent
    MutashabihatValidationFailureTests.cs# injected hard violation → rollback + failure report
    MutashabihatWarningTests.cs          # coverage>100, duplicate occurrence, stale counters, source-key-absent flagged not blocking
    MutashabihatRefusalForceTests.cs     # refuse-unless-empty, --force replace, quran_ayahs + source files untouched
    MutashabihatReportShapeTests.cs      # Markdown + JSON report: totals, per-check id/severity/verdict, anomaly counts
    MutashabihatImportTestFixture.cs     # Testcontainers Postgres + synthetic source-safe staged files
```

**Structure Decision**: Reuse the existing solution. All new types live under feature folders
(`Quran/Mutashabihat/`) per `BACKEND_STRUCTURE.md` — this is an **ayah-level relationship** foundation, so
it sits beside `Quran/Ayahs/`, not under `Quran/Words/`. The load is a **source-driven** operator action
mirroring the Feature 002/004 importer (JSON readers → in-memory assembly → Npgsql binary `COPY` →
validate → commit), exposed as a new verb on the existing console host — **not** a new project and **not**
an HTTP endpoint. The two datasets are deliberately stored as **two separate table sets** (a group/leaf
pair + a directed-link table), never a shared polymorphic table. See research.md for the rationale and
rejected alternatives.

## Complexity Tracking

*No constitution violations. No new projects (the `DataImporter` host and `QuranDashboard.Tests` project
already exist). No watch items — this is the smallest of the foundation importers (≈ 7,923 rows, 3
tables), with the only real logic isolated in a single cohesive assembler.*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| _(none)_ | — | — |
