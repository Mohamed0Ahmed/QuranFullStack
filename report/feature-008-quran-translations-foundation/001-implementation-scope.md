# Feature 008 — Implementation Scope

**Feature**: Quran Translations Foundation  
**Branch**: `008-quran-translations-foundation`  
**Created**: 2026-06-15 (Phase 1 setup)

This note records the locked implementation boundaries for Feature 008. All later phases must stay within
these constraints unless the spec, plan, or an explicit user decision changes them.

## In scope

- Backend Clean Architecture import foundation under `Quran/Translations/`
- New `import-translations` verb on the existing `QuranDashboard.DataImporter` console host
- Two translation-owned tables: `quran_translation_sources`, `quran_translation_ayah_entries`
- Read-only use of canonical `quran_ayahs` for `verse_key → ayah_id` resolution
- Import from the staged local package at `resources/import-sources/quran-translations/`
- Markdown + JSON audit reports for accepted, refused, and failed runs
- xUnit tests under `Backend/tests/QuranDashboard.Tests/Quran/Translations/`

## Out of scope (do not implement in Feature 008)

- **No frontend** — no Angular routes, components, or UI work
- **No API endpoints** — no controllers, `ApiResponse`, or public HTTP surface
- **No search** — no query/index/search features
- **No startup seeding** — import is operator-triggered only
- **No permissions** — no app-user authorization work
- **No word-by-word import** — excluded sources remain report-only
- **No source package mutation** — never edit files under `resources/import-sources/quran-translations/`
- **No hand-written migrations** — EF Core tooling only, and only after explicit migration approval
- **No `dotnet ef database update`** unless explicitly requested
- **No mutation of non-translation tables** — Quran foundation rows remain read-only

## Data safety

- Preserve translation `t` text **exactly** as imported (inline footnotes, HTML, whitespace)
- Never copy Arabic Quran ayah text into translation-owned records
- Never invent, normalize, or fabricate Quranic or translation content in tests (use obvious synthetic placeholders)

## Planned DI mapping

_To be recorded in Phase 2 (T019) without editing `DependencyInjection.cs` until implementations exist (T042)._
