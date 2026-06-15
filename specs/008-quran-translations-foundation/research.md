# Phase 0 Research — Quran Translations Foundation

All decisions are settled from `spec.md`, the Feature 008 planning report, the decisions addendum, and the
final package files. No clarification items remain.

---

## R1. Import shape: local operator-only console verb

**Decision.** Add `import-translations` to the existing `tools/QuranDashboard.DataImporter` console host.
Feature 008 has no API, UI, app-user permission model, public reader, search, word-by-word import, or
startup seeding path.

**Rationale.** The spec locks Feature 008 as backend data-foundation import behavior. Reusing the existing
importer host matches previous foundation imports and keeps data loading off request paths. Feature 007
already proves the host can support package validation, transactional bulk writes, and audit reports.

**Alternatives considered.**
- API endpoint: rejected because Feature 008 explicitly has no public/API access.
- Startup seeding: rejected because the import is large, source-driven, report-gated, and operator-run.
- New console project: rejected because the existing importer host already provides DI/config/verb dispatch.

## R2. Source package authority: final manifest plus final display metadata

**Decision.** Treat `resources/import-sources/quran-translations/manifest.json` as the file/hash/source
contract and `resources/import-sources/quran-translations/source-display-metadata.json` as the required
display metadata contract. Both must be valid and aligned before acceptance.

**Rationale.** The manifest has nullable Arabic display fields for some sources, while
`source-display-metadata.json` is the final display contract with 167 final display-ready records and
non-empty Arabic/English display names. Future selection depends on language names, translation type, and
display names, so the importer must not silently fall back to incomplete manifest labels.

**Alternatives considered.**
- Manifest only: rejected because display metadata was finalized separately and is import-blocking.
- Import directly from raw upstream folders: rejected because workspace rules require staged canonical
packages.
- Treat `source-display-metadata.review.json` as input: rejected because it is retained as a review overlay,
not the final contract.

## R3. Database model: source metadata plus expanded ayah entries

**Decision.** Use two feature-owned tables: `quran_translation_sources` and
`quran_translation_ayah_entries`.

**Rationale.** Translation source files are strictly one text per source and ayah. There are no grouped
leader-ayah blocks like tafsir, so a middle `quran_translation_entries` table would add complexity without
preserving additional source structure.

**Alternatives considered.**
- Tafsir-style three-table model: rejected because translations have no ranged/grouped text blocks.
- One JSON blob per source: rejected because it blocks relational ayah lookup and foreign-key validation.
- Separate language/contributor/footnote tables: rejected as v1 over-modeling; the locked decision is
denormalized source metadata and inline footnotes preserved in text.

## R4. Ayah identity: resolve every source key to `quran_ayahs`

**Decision.** Resolve every source verse key to canonical `quran_ayahs.id`. Store `ayah_id`
relationships and an optional/audit `verse_key`; never copy Arabic Quran ayah text into translation-owned
records.

**Rationale.** `quran_ayahs` is the canonical Quran foundation table and already owns ayah identity/text.
Foreign-keyed `ayah_id` relationships give integrity and future queryability without duplicating sacred
text.

**Alternatives considered.**
- Store only raw verse keys: rejected because it loses referential integrity.
- Copy `text_uthmani`: rejected by the spec and Quranic data safety.
- Skip unresolved references: rejected because one unresolved verse key invalidates an approved source.

## R5. Translation text preservation

**Decision.** Store translation text exactly as imported from each source `t` value, including inline
`[[...]]` footnotes, embedded HTML, whitespace, punctuation, diacritics, and script-specific characters.
Do not sanitize, normalize, strip markup, split footnotes, or generate plain-text derivatives in Feature
008.

**Rationale.** The locked D5 policy says source text must remain byte-equal. Rendering, sanitization,
footnote parsing, and public display are future UI/API/publishing concerns and would risk rewriting the
source data during foundation import.

**Alternatives considered.**
- Strip markup: rejected because it rewrites source text and loses evidence.
- Store parsed footnotes: rejected as out of scope and because no structured footnote field exists.
- Store both original and normalized text: rejected as out of scope; future search/rendering features can
derive additional forms from the source-preserved text.

## R6. Excluded sources: report-only

**Decision.** The 19 excluded sources are not persisted in translation foundation records. They appear in
import reports only with status and reason.

**Rationale.** The foundation stores approved translation sources. Persisting excluded metadata would
create catalog lifecycle and query ambiguity not needed for v1.

**Alternatives considered.**
- Persist excluded metadata in a separate table: rejected as unnecessary for this import foundation.
- Persist excluded metadata alongside approved sources: rejected because it risks exposing non-approved
sources to later read features.

## R7. Transaction, force, and report acceptance

**Decision.** Run imports and rebuilds inside one transaction. Without explicit rebuild intent, refuse when
translation target tables have data. With explicit rebuild intent, replace only translation-owned tables.
Commit only after hard checks pass and both reports are written; report-write failure means the run is not
accepted and no translation changes are kept.

**Rationale.** The spec makes audit evidence mandatory. Import reports prove counts, warnings, and source
integrity, so accepting data without reports would break reviewability.

**Alternatives considered.**
- Keep data when report writing fails: rejected by `TR-REPORT-WRITTEN`.
- Append/import over existing data: rejected because it risks duplicates and mixed package versions.
- Partial-source commits: rejected because the package is curated as one complete v1 set.

## R8. Validation taxonomy

**Decision.** Use stable `TR-` check IDs. Hard checks gate commit; warnings and informational checks record
non-blocking risks.

**Rationale.** Stable IDs let reports, tests, and future reviews refer to the same invariants. License and
provenance unknowns must remain visible but do not block internal import.

**Alternatives considered.**
- Free-form report messages only: rejected because they are hard to test and compare.
- Treat license/provenance unknown as hard failure: rejected because the spec allows internal import while
blocking publish-ready claims.

## R9. Test strategy

**Decision.** Use source-safe synthetic fixtures for parser/assembler/import tests, plus integration tests
with PostgreSQL for FK, uniqueness, transaction, force/refusal, and report behavior. Synthetic fixtures
must be clearly marked and must not invent real Quran text or real translation claims.

**Rationale.** Quranic data safety forbids invented Quran/translation content presented as real. Synthetic
verse keys and neutral synthetic translation strings are acceptable for behavior tests when they are
obviously artificial. Real-package spot checks may be added only with explicit traceability to the local
source package.

**Alternatives considered.**
- Tests with real translation excerpts by default: rejected unless copied from the source package with
explicit provenance; synthetic fixtures are safer for behavior tests.
- Mock persistence for correctness paths: rejected where FK/transaction behavior matters.
