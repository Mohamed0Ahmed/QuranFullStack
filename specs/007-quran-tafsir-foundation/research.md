# Phase 0 Research — Quran Tafsir Foundation

All decisions are settled from `spec.md` clarifications, the Feature 007 planning report, and the final
package manifest. No `NEEDS CLARIFICATION` items remain.

---

## R1. Import shape: local operator-only console verb

**Decision.** Add `import-tafsirs` to the existing `tools/QuranDashboard.DataImporter` console host.
Feature 007 has no API, UI, app-user permission model, public reader, search, translation, or startup
seeding path.

**Rationale.** The spec locks Feature 007 as local operator-only import behavior. Reusing the existing
importer host matches previous foundation imports and keeps data loading off request paths.

**Alternatives considered.**
- API endpoint: rejected because Feature 007 explicitly has no public/API access.
- Startup seeding: rejected because the import is large, source-driven, report-gated, and operator-run.
- New console project: rejected because the existing importer host already provides DI/config/verb dispatch.

## R2. Source package authority: final manifest only

**Decision.** Treat `resources/import-sources/quran-tafsirs/manifest.json` as the only import manifest.
The package must contain `README.md`, `manifest.json`, `package-report.md`, and `sources/`.

**Rationale.** The final package report states this is the approved package: 84 copied approved tafsir
sources and 9 excluded sources. Older draft curation files are planning/provenance history, not runtime
import inputs.

**Alternatives considered.**
- Import from draft curation manifest: rejected because it contains all inspected candidates, not only the
final package source set.
- Import directly from upstream/raw folders: rejected because workspace rules require staged canonical
packages.

## R3. Database model: sources + text blocks + ayah links

**Decision.** Use three feature-owned tables: `quran_tafsir_sources`, `quran_tafsir_entries`, and
`quran_tafsir_ayah_entries`.

**Rationale.** Many sources use grouped tafsir blocks where one text block covers multiple ayahs. Storing a
text block once and linking every covered ayah preserves source grouping and avoids duplicating large text
blocks while still supporting one-row-per-source/ayah lookup.

**Alternatives considered.**
- One expanded text row per source/ayah: rejected because it duplicates grouped text and obscures source
shape.
- One source table plus JSONB blob per source: rejected because it blocks relational ayah lookups and
foreign-key validation.
- Normalize languages/contributors into separate tables now: deferred; source-level metadata is enough for
the foundation and avoids premature catalog design.

## R4. Ayah identity: resolve all source keys to `quran_ayahs`

**Decision.** Resolve every source verse key, grouped `ayah_keys` item, and string pointer target to
canonical `quran_ayahs.id`. Store `ayah_id` relationships; do not copy Quran ayah text.

**Rationale.** `quran_ayahs` is the canonical Quran foundation table and already owns ayah identity/text.
Foreign-keyed `ayah_id` relationships give integrity and queryability without duplicating sacred text.

**Alternatives considered.**
- Store only raw verse keys: rejected because it loses referential integrity.
- Copy `text_uthmani`: rejected by spec, planning report, and Quranic data safety.
- Skip unresolved references: rejected because missing references invalidate a source package.

## R5. Tafsir text preservation

**Decision.** Store tafsir text exactly as imported, including inline markup and source-specific formatting.
Do not sanitize, normalize, strip markup, or generate a plain-text copy in Feature 007.

**Rationale.** The final package README states source contents are preserved exactly and not rewritten.
Rendering cleanup is public-reader/API/UI work and would risk changing imported scholarly source content.

**Alternatives considered.**
- Strip markup: rejected because it rewrites source text and loses evidence.
- Store both original and plain text: rejected as out of scope; search/rendering features can derive later.

## R6. Excluded sources: report-only

**Decision.** The 9 excluded sources are not persisted in tafsir foundation records. They appear in import
reports only with status and reason.

**Rationale.** The foundation stores approved tafsir sources. Persisting excluded metadata would create
catalog lifecycle and query ambiguity not needed for v1.

**Alternatives considered.**
- Persist excluded metadata in a separate table: rejected as unnecessary for this import foundation.
- Persist excluded metadata alongside approved sources: rejected because it risks exposing non-approved
sources to later read features.

## R7. Transaction, force, and report acceptance

**Decision.** Run rebuilds inside one transaction. Without explicit rebuild intent, refuse when target
tables have data. With explicit rebuild intent, replace only tafsir-owned tables. Commit only after hard
checks pass and both reports are written or safely guaranteed before acceptance; report-write failure means
the run is not accepted and no tafsir changes are kept.

**Rationale.** The spec clarifies that audit evidence is mandatory. Import reports prove counts, warnings,
and source integrity, so accepting data without reports would break reviewability.

**Alternatives considered.**
- Keep data when report writing fails: rejected by clarification.
- Append/import over existing data: rejected because it risks duplicates and mixed package versions.
- Partial-source commits: rejected because the package is curated as one complete v1 set.

## R8. Validation taxonomy

**Decision.** Use stable `TAFSIR-` check IDs. Hard checks gate commit; warnings and informational checks
record non-blocking risks.

**Rationale.** Stable IDs let reports, tests, and future reviews refer to the same invariants. License and
provenance unknowns must remain visible but do not block internal import.

**Alternatives considered.**
- Free-form report messages only: rejected because they are hard to test and compare.
- Treat license/provenance unknown as hard failure: rejected because spec allows internal import while
blocking public publishing claims.

## R9. Test strategy

**Decision.** Use source-safe synthetic fixtures for parser/assembler/import tests, plus integration tests
with PostgreSQL for FK, uniqueness, transaction, force/refusal, and report behavior.

**Rationale.** Quranic data safety forbids invented Quran/tafsir claims in tests. Synthetic verse keys and
synthetic tafsir text are acceptable when clearly marked and not presented as real content.

**Alternatives considered.**
- Tests with real tafsir excerpts: rejected unless copied from source package with explicit traceability;
synthetic fixtures are safer for behavior tests.
- Mock persistence for correctness paths: rejected where FK/transaction behavior matters.
