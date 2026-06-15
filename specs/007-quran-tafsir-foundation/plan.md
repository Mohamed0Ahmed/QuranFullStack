# Implementation Plan: Quran Tafsir Foundation

**Branch**: `007-quran-tafsir-foundation` | **Date**: 2026-06-14 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `specs/007-quran-tafsir-foundation/spec.md`

> **Companion documents (source of truth):**
> `docs/feature-007-quran-tafsir-foundation/feature-007-quran-tafsir-foundation-planning-report.md`
> (pre-Spec Kit planning report),
> `resources/import-sources/quran-tafsirs/{README.md,manifest.json,package-report.md}`
> (final local import-source package; every count below traces to this package).
> **Governance:** `CODING_PRINCIPLES.md`, `Backend/AGENTS.md`,
> `Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE}.md`.

## Summary

Build the **Quran Tafsir Foundation** backend import: a local operator-only importer that reads the final
staged package at `resources/import-sources/quran-tafsirs/`, verifies the final manifest and copied source
files, resolves all tafsir ayah keys to canonical `quran_ayahs`, stores approved tafsir source metadata and
tafsir text, links each source/ayah to the tafsir text block that covers it, and emits Markdown + JSON
audit reports. The feature imports **84 approved tafsir sources** (**35 Arabic**, **49 non-Arabic**, **33
languages**) and keeps the **9 excluded sources** report-only.

The importer is a new verb on the existing backend data-import console host: `import-tafsirs`. It is not an
API, UI, public reader, search feature, translation feature, or startup seeding path. The feature never
edits the source package, never imports excluded sources, never copies Quran ayah text, and never mutates
Quran foundation tables. Tafsir text is stored **exactly as imported**, including inline markup. A run is
accepted only if all hard checks pass, both required report files are written, and the source package still
matches manifest size/hash before commit.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (`net10.0`)  
**Primary Dependencies**: EF Core / PostgreSQL provider already used by Infrastructure; `Npgsql` binary
`COPY` via the existing PostgreSQL connection pattern; `Microsoft.Extensions.Hosting` `10.0.0` for the
existing `QuranDashboard.DataImporter` console host; `System.Text.Json` for manifest/source parsing.  
**Storage**: PostgreSQL. Three new tafsir-owned tables are planned: `quran_tafsir_sources`,
`quran_tafsir_entries`, `quran_tafsir_ayah_entries`. Existing `quran_ayahs` is read-only and used only to
resolve `verse_key -> ayah_id`.  
**Testing**: xUnit `2.9.3`, FluentAssertions `8.2.0`, Testcontainers.PostgreSql `4.4.0` in
`Backend/tests/QuranDashboard.Tests`, plus pure unit tests for readers/assembler.  
**Target Platform**: Linux server / local backend operator environment running .NET 10.  
**Project Type**: Existing Backend Clean Architecture solution. Reuse the existing
`tools/QuranDashboard.DataImporter` project; no new project and no frontend/API project work.  
**Performance Goals**: Import 84 complete tafsir source files and produce **523,824** source-to-ayah links
(`84 * 6,236`) in an operator-run batch. Use streaming/file-by-file parsing and bulk writes so the import
does not require holding all raw source text for all files in memory at once. This is not a user-facing
latency path.  
**Constraints**: Local staged package only; final manifest only; exact file set/size/sha256 validation;
all 84 approved sources must have 6,236 content-covered ayahs; 9 excluded sources report-only; tafsir text
stored exactly as imported; Quran ayah text not copied; every ayah reference resolved to `quran_ayahs`;
normal run refuses if tafsir tables already contain data; `--force` rebuilds only tafsir-owned tables;
transactional hard-gated import; no accepted run without both report formats; no app-user permissions,
HTTP, UI, search, translations, public publishing, or startup seeding.  
**Scale/Scope**: 84 source rows; at least 523,824 ayah-link rows; one text-block row per source text block
(fewer than or equal to 523,824 depending on grouped blocks); 33 languages; 42 contributor identities in the
manifest. `resources/` is local and Git-ignored.

*No unresolved NEEDS CLARIFICATION items. All open choices from the planning report were locked in
`spec.md` Clarifications on 2026-06-14.*

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution (`.specify/memory/constitution.md`) is still an unratified placeholder template.
For Feature 007, the explicit interim governance authority is the workspace/backend rule set:
`AGENTS.md`, `CODING_PRINCIPLES.md`, `Backend/AGENTS.md`, `Backend/.architecture/BACKEND_STRUCTURE.md`,
and `Backend/.architecture/CLEAN_ARCHITECTURE.md`. Do not infer additional constitution MUST rules from the
placeholder constitution. Ratifying a real constitution remains a separate `/speckit-constitution` concern.

| Gate | Status | Notes |
|---|---|---|
| Clean Architecture dependency direction | PASS | Domain owns pure tafsir entities; Application orchestrates import via abstractions; Infrastructure implements file readers, assembler, persistence, validation, and report writing; console host is composition/dispatch only. |
| Feature/domain foldering | PASS | New types live under `Quran/Tafsirs/` and `Quran/Tafsirs/ImportTafsirs/` feature folders. No global `Models`, `DTOs`, `Helpers`, or technical dumping folders. |
| Import source safety | PASS | Reads only `resources/import-sources/quran-tafsirs/`; source package is never modified; source package is re-verified before commit. |
| Quranic data safety | PASS | Tafsir text is imported as source content; Quran ayah text is never copied; `quran_ayahs` and prior foundation data are read-only. |
| EF migration policy | PASS | Planning allows schema changes only; migration must be generated by EF tooling later and only on explicit implementation/migration request. No hand-written migrations in this planning phase. |
| API boundary | N/A | No API endpoints or `ApiResponse` behavior in Feature 007. |
| Operator-only scope | PASS | Console import only; no app-user permissions, frontend, public API, search, or public reader behavior. |
| Reporting/audit gate | PASS | Accepted import requires both JSON and Markdown reports. Report write failure means no accepted tafsir changes are kept. |

**Post-design re-check:** PASS. The generated data model and contracts preserve the same boundaries and
introduce no justified violations.

## Project Structure

### Documentation (this feature)

```text
specs/007-quran-tafsir-foundation/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── cli-verb.md
│   ├── tafsir-abstractions.md
│   └── validation-report.schema.md
├── checklists/
│   └── requirements.md
└── tasks.md                 # created later by /speckit-tasks, not by /speckit-plan
```

### Source Code (repository root)

```text
Backend/
  domain/QuranDashboard.Domain/Quran/Tafsirs/
    TafsirSource.cs
    TafsirEntry.cs
    TafsirAyahEntry.cs

  application/QuranDashboard.Application.Abstractions/Quran/Tafsirs/
    ITafsirImportSource.cs
    ITafsirImportWriter.cs
    ITafsirReportWriter.cs
    TafsirSourceData.cs
    TafsirImportResult.cs
    TafsirImportTotals.cs
    TafsirCheckResult.cs
    TafsirInvariants.cs

  application/QuranDashboard.Application/Quran/Tafsirs/ImportTafsirs/
    ImportTafsirsCommand.cs
    ImportTafsirsHandler.cs
    ImportTafsirsResult.cs

  infrastructure/QuranDashboard.Infrastructure/
    Files/Quran/Tafsirs/
      TafsirManifestReader.cs
      JsonTafsirSourceReader.cs
      TafsirAssembler.cs
      TafsirImportSource.cs
    Persistence/Configurations/Quran/Tafsirs/
      TafsirSourceConfiguration.cs
      TafsirEntryConfiguration.cs
      TafsirAyahEntryConfiguration.cs
    Persistence/Repositories/Quran/Tafsirs/
      EfBulkTafsirImportWriter.cs
      TafsirBulkCopier.cs
      TafsirSql.cs
      TafsirValidationRunner.cs
      TafsirImportReportBuilder.cs
    Reports/Quran/Tafsirs/
      MarkdownJsonTafsirReportWriter.cs
    Persistence/QuranDashboardDbContext.cs
    DependencyInjection.cs

  tools/QuranDashboard.DataImporter/
    Program.cs                      # add import-tafsirs verb only

  tests/QuranDashboard.Tests/Quran/Tafsirs/
    TafsirManifestReaderTests.cs
    TafsirSourceReaderTests.cs
    TafsirAssemblerTests.cs
    TafsirImportTests.cs
    TafsirValidationFailureTests.cs
    TafsirRefusalForceTests.cs
    TafsirReportShapeTests.cs
    TafsirImportTestFixture.cs
```

**Structure Decision**: Reuse the existing backend solution and `QuranDashboard.DataImporter` console host.
The feature belongs under `Quran/Tafsirs` because it is an ayah-level Quran research content foundation,
beside `Ayahs`, `Words`, and `Mutashabihat`. No frontend/API folders are touched. Contracts document the
operator CLI and Application-boundary abstractions; they do not define public HTTP endpoints.

## Complexity Tracking

No constitution or architecture violations.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| _(none)_ | — | — |
