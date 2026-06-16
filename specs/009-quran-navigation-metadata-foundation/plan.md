# Implementation Plan: Quran Navigation Metadata Foundation

**Branch**: `009-quran-navigation-metadata-foundation` | **Date**: 2026-06-16 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/009-quran-navigation-metadata-foundation/spec.md`

> **Companion documents (source of truth):**
> `docs/feature-009-quran-navigation-metadata-foundation/feature-009-quran-navigation-metadata-foundation-planning-report.md`
> (long-form pre-Spec-Kit planning report, locked scope + decisions),
> `docs/feature-009-quran-navigation-metadata-foundation/quran-metadata-inventory-gap-analysis-report.md`
> (gap analysis: why only juz/hizb/rub/sajda are missing), and
> `resources/import-sources/quran-navigation-metadata/{README.md,manifest.json,package-report.md}`
> (final staged import-source package; every import count traces here).
> **Governance:** `AGENTS.md`, `CODING_PRINCIPLES.md`, `Backend/AGENTS.md`,
> `Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE,API_GUIDELINES}.md`.

## Summary

Build the **Quran Navigation Metadata Foundation** backend import: a local operator-only importer that reads
the final staged package at `resources/import-sources/quran-navigation-metadata/`, verifies the final
manifest (`packageType = "quran-navigation-metadata-import-source-package"`, `isFinalImportManifest = true`)
and per-file sha256/size/count, parses each division's `verse_mapping`, resolves every verse reference to
canonical `quran_ayahs`, and persists the four navigation/division datasets — **juz (30)**, **hizb (60)**,
**rub (240)**, **sajda (15)** — into four new navigation-owned tables, while tagging **all 6,236 ayahs**
with their `juz_number`, `hizb_number`, and `rub_number`. It emits Markdown + JSON audit reports.

The importer is a new verb on the existing backend data-import console host: `import-navigation-metadata`.
It is **not** an API, UI, search feature, startup-seeding path, permissions feature, reader, or a re-import
of surah/ayah data. It never edits the source package, never reads or copies Quran ayah text, and never
mutates `quran_ayahs.text_uthmani` or any surah/page/line/word/tafsir/translation/mutashabihat/morphology/
i3rab data — it only adds the three ayah navigation columns and the four new tables. A run is accepted only
if all hard checks pass, both required report files are written, and the source package still matches the
manifest before commit.

Two clarifications from `/speckit-clarify` are locked into this plan: (1) `--source` points at the **package
root** (the folder containing `manifest.json` + `sources/`); (2) each division's stored verse count is the
count **computed from its ranges** — the source `verses_count` is informational and only drives a
non-blocking warning when it diverges.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (`net10.0`).
**Primary Dependencies**: EF Core / PostgreSQL provider already used by Infrastructure; `Npgsql` (binary
`COPY` / parameterized bulk writes via the existing connection pattern); `Microsoft.Extensions.Hosting`
`10.0.0` for the existing `QuranDashboard.DataImporter` console host; `System.Text.Json` for manifest and
source parsing.
**Storage**: PostgreSQL. Four new navigation-owned tables are planned — `quran_juzs`, `quran_hizbs`,
`quran_rubs`, `quran_sajdas` — plus three additive **nullable** columns on the existing `quran_ayahs`:
`juz_number`, `hizb_number`, `rub_number`. `quran_ayahs` text/other columns are read-only; the navigation
columns are the only addition and are populated by an in-transaction `UPDATE`.
**Testing**: xUnit `2.9.3`, FluentAssertions `8.2.0`, Testcontainers.PostgreSql `4.4.0` in
`Backend/tests/QuranDashboard.Tests`, plus pure unit tests for the manifest reader, source readers, and the
`verse_mapping` assembler / validation logic.
**Target Platform**: Linux server / local backend operator environment running .NET 10.
**Project Type**: Existing Backend Clean Architecture solution. Reuse the existing
`tools/QuranDashboard.DataImporter` project; no new project and no frontend/API work.
**Performance Goals**: Tiny operator-run batch — 30 + 60 + 240 + 15 = **345** header rows plus a single
`UPDATE` over the **6,236** ayahs. Not a user-facing latency path; correctness and atomicity dominate.
**Constraints**: Staged package root only; final manifest only; exact file set / size / sha256 validation;
exactly juz=30 / hizb=60 / rub=240 / sajda=15; each division type's ranges cover all 6,236 ayahs exactly
once (no gaps/overlaps); hierarchy (hizb→juz, rub→hizb) derived by range containment and validated; every
verse reference resolves to `quran_ayahs`; sajda type ∈ {required, optional}; stored division verse count =
computed range count (source `verses_count` informational → warning on divergence); after success all 6,236
ayahs have non-null juz/hizb/rub; no Quran ayah text read or copied; normal run refuses if any navigation
target already populated; `--force` clears/reloads only navigation-owned tables + the three ayah columns;
transactional hard-gated import; no accepted run without both report formats; no HTTP/UI/search/seeding/
permissions; no ruku/manzil/audio.
**Scale/Scope**: 345 division rows + 6,236 ayah-column updates; 0 new Quran-text rows. `resources/` is local
and Git-ignored (the real-data run is gated on package presence; CI uses synthetic fixtures).

*No unresolved clarification items. All open choices are locked by the planning report and the two
`/speckit-clarify` answers: header-tables-plus-denormalized-ayah-columns (no child range table, no JSON
column), dedicated `quran_sajdas` table (no ayah sajda flags), `verse_key` linking (not numeric-id
alignment), `--source` = package root, and computed verse count is authoritative for storage.*

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution (`.specify/memory/constitution.md`) is still an unratified placeholder template.
For Feature 009, the explicit interim governance authority is the workspace/backend rule set: `AGENTS.md`,
`CODING_PRINCIPLES.md`, `Backend/AGENTS.md`, `Backend/.architecture/BACKEND_STRUCTURE.md`,
`Backend/.architecture/CLEAN_ARCHITECTURE.md`, and `Backend/.architecture/API_GUIDELINES.md`. Do not infer
additional MUST rules from the placeholder constitution; ratifying a real constitution remains a separate
`/speckit-constitution` concern.

| Gate | Status | Notes |
|---|---|---|
| Clean Architecture dependency direction | PASS | Domain owns pure division/sajda entities + `SajdahType` enum; Application orchestrates import via abstractions; Infrastructure implements manifest/source readers, `verse_mapping` assembler, persistence, validation, report writing; console host is composition/dispatch only. |
| Feature/domain foldering | PASS | New types live under `Quran/Navigation/` and `Quran/Navigation/ImportNavigationMetadata/`. No global `Enums`, `Models`, `DTOs`, `Helpers`, or technical dumping folders. The three new `quran_ayahs` columns extend the existing `Ayah` entity in place. |
| Import source safety | PASS | Reads only the staged `resources/import-sources/quran-navigation-metadata/` package root; source package is never modified; re-verified (sha256/size) before commit. |
| Quranic data safety | PASS | Ayahs referenced by `verse_key` only; no ayah text read or copied; `quran_ayahs.text_uthmani` and all prior foundation data are untouched except the additive nav columns. No invented Quranic data. |
| EF migration policy | PASS | Planning allows schema design only; the additive migration must be EF-tooling-generated later and only on explicit implementation/migration request. No hand-written migrations in this planning phase; no `database update` without explicit request. |
| API boundary | N/A | No API endpoints, controllers, or `ApiResponse` behavior in Feature 009. |
| Operator-only scope | PASS | Console import only; no app-user permissions, frontend, public API, search, or reader behavior. |
| Reporting/audit gate | PASS | Accepted import requires both JSON and Markdown reports; report-write failure means no accepted navigation changes are kept. |
| Additive / isolation gate | PASS | Existing `quran_ayahs` columns and all other domains are read-only; only the four nav tables + three nav columns are written; `--force` is scoped to navigation-owned data only. |

**Post-design re-check:** PASS. The generated data model and contracts preserve the same boundaries and
introduce no justified violations (see [research.md](./research.md) and [data-model.md](./data-model.md)).

## Project Structure

### Documentation (this feature)

```text
specs/009-quran-navigation-metadata-foundation/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── cli-verb.md
│   ├── navigation-abstractions.md
│   └── validation-report.schema.md
├── checklists/
│   └── requirements.md
└── tasks.md                 # created later by /speckit-tasks, not by /speckit-plan
```

### Source Code (repository root)

```text
Backend/
  domain/QuranDashboard.Domain/Quran/Navigation/
    Juz.cs
    Hizb.cs
    Rub.cs
    Sajda.cs
    SajdahType.cs
  domain/QuranDashboard.Domain/Quran/Ayahs/
    Ayah.cs                          # add nullable JuzNumber, HizbNumber, RubNumber

  application/QuranDashboard.Application.Abstractions/Quran/Navigation/
    INavigationMetadataImportSource.cs
    INavigationMetadataImportWriter.cs
    INavigationMetadataReportWriter.cs
    INavigationMetadataImportReportBuilder.cs
    NavigationMetadataSourceData.cs
    NavigationMetadataImportResult.cs
    NavigationMetadataImportReport.cs
    NavigationMetadataImportConstants.cs
    NavigationMetadataInvariants.cs
    NavigationMetadataSourceException.cs
    NavigationMetadataValidationException.cs

  application/QuranDashboard.Application/Quran/Navigation/ImportNavigationMetadata/
    ImportNavigationMetadataCommand.cs
    ImportNavigationMetadataHandler.cs
    ImportNavigationMetadataResult.cs
    NavigationMetadataImportReportEmitter.cs

  infrastructure/QuranDashboard.Infrastructure/
    Files/Quran/Navigation/
      NavigationManifestReader.cs
      JsonNavigationDatasetReader.cs       # reads juz/hizb/rub/sajda json
      NavigationMetadataAssembler.cs       # expands verse_mapping -> per-ayah assignments + hierarchy
      NavigationMetadataImportSource.cs
      NavigationValidationChecks.cs
    Persistence/Configurations/Quran/Navigation/
      JuzConfiguration.cs
      HizbConfiguration.cs
      RubConfiguration.cs
      SajdaConfiguration.cs
    Persistence/Configurations/Quran/
      AyahConfiguration.cs                 # add 3 nav columns (additive)
    Persistence/Repositories/Quran/Navigation/
      EfBulkNavigationMetadataImportWriter.cs
      NavigationMetadataBulkCopier.cs
      NavigationMetadataCommandExecutor.cs
      NavigationMetadataImportReportBuilder.cs
      NavigationMetadataSql.cs
      NavigationMetadataValidationRunner.cs
    Reports/Quran/Navigation/
      MarkdownJsonNavigationMetadataReportWriter.cs
    Persistence/QuranDashboardDbContext.cs # add Juz/Hizb/Rub/Sajda DbSets
    Migrations/                            # EF-generated additive migration (later, on request)
    DependencyInjection.cs                 # wire navigation import services

  tools/QuranDashboard.DataImporter/
    Program.cs                             # add import-navigation-metadata verb only

  tests/QuranDashboard.Tests/Quran/Navigation/
    NavigationSchemaShapeTests.cs
    NavigationManifestReaderTests.cs
    NavigationDatasetReaderTests.cs
    NavigationAssemblerTests.cs            # verse_mapping expansion, coverage, hierarchy
    NavigationImportTests.cs               # integration on synthetic quran_ayahs
    NavigationValidationFailureTests.cs    # gaps/overlaps/unresolved keys/invalid sajda type
    NavigationRefusalForceTests.cs         # rerun guard + force reload
    NavigationRollbackTests.cs             # forced-reload mid-failure rollback
    NavigationReportShapeTests.cs
    NavigationSourceSafetyTests.cs         # no Quran text read/persisted
    NavigationImportTestFixture.cs
```

**Structure Decision**: Reuse the existing backend solution and `QuranDashboard.DataImporter` console host.
The feature belongs under `Quran/Navigation` because juz/hizb/rub/sajda are Quran-core navigation/division
metadata beside `Surahs`, `Ayahs`, `MushafPages`, `Words`, `Mutashabihat`, `Tafsirs`, and `Translations`.
The three additive `quran_ayahs` columns extend the existing `Ayah` aggregate in place (no new ayah table).
No frontend/API folders are touched. Contracts document the operator CLI and the Application-boundary
abstractions; they define no public HTTP endpoints.

## Complexity Tracking

No constitution or architecture violations.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| _(none)_ | - | - |
