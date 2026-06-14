# Feature Specification: Quran Tafsir Foundation

**Feature Branch**: `007-quran-tafsir-foundation`  
**Created**: 2026-06-14  
**Status**: Draft  
**Input**: User description: "Read our plan and, according to GitHub Spec Kit best practices, create the Feature 007 Quran Tafsir Foundation specification only. The implementation will be done using a cheaper model, so the specification must be super clear."

## Clarifications

### Session 2026-06-14

- Q: If validation passes but the import report cannot be written, should the run still be accepted? → A: No; the run is not accepted and no tafsir changes are kept.
- Q: Should Feature 007 normalize or sanitize tafsir text during import? → A: No; preserve tafsir text exactly as imported, including inline markup.
- Q: Should excluded source metadata be persisted in tafsir foundation records? → A: No; excluded sources are report-only.
- Q: Who can operate Feature 007 import behavior? → A: Operator-only local import; no app user permissions or UI/API access.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Import approved tafsir package (Priority: P1)

As a backend maintainer, I need to import the final curated Quran tafsir package so the system has stable tafsir source records and tafsir content linked to canonical Quran ayahs before any reader, comparison, search, API, or frontend feature is built.

**Why this priority**: This is the minimum useful foundation. Without a successful controlled import, later tafsir-facing features would have no reliable source catalog, no verified ayah links, and no auditable provenance warning.

**Independent Test**: Can be fully tested by starting an import from the approved package and confirming that only the 84 approved tafsir sources are accepted, every imported tafsir item resolves to a canonical ayah, and the import completes with a passing report.

**Acceptance Scenarios**:

1. **Given** the final package exists at `resources/import-sources/quran-tafsirs/` with its manifest, package report, README, and source files, **When** the backend maintainer starts the tafsir import, **Then** the system imports exactly 84 approved tafsir sources and rejects any source outside the approved set.
2. **Given** the final package includes approved Arabic and non-Arabic sources, **When** the import completes, **Then** the imported source catalog includes exactly 35 Arabic sources, 49 non-Arabic sources, and 33 languages.
3. **Given** a source entry addresses an ayah by verse key, **When** the import resolves that entry, **Then** the resulting tafsir content is linked to the existing canonical ayah and no Quran ayah text is copied into tafsir-owned records.
4. **Given** a source uses grouped tafsir text that covers multiple ayahs, **When** the import processes the source, **Then** the tafsir text is preserved once as a text block and each covered ayah is linked to that block.
5. **Given** a tafsir source contains inline markup or source-specific formatting in its tafsir text, **When** the import stores the tafsir text, **Then** the stored tafsir text matches the source text exactly.
6. **Given** an application user or public client attempts to access Feature 007 import behavior, **When** they look for UI or API access, **Then** no Feature 007 UI/API access path exists because the import is local operator-only.

---

### User Story 2 - Protect import integrity and scope (Priority: P2)

As a reviewer, I need the import to refuse unsafe or out-of-scope input so the curated package cannot silently drift, excluded sources cannot enter the foundation, and Quran foundation data remains unchanged.

**Why this priority**: Tafsir content is sensitive scholarly data. Package drift, excluded-source leakage, missing ayah links, or accidental mutation of Quran foundation data would undermine trust in every later feature.

**Independent Test**: Can be tested by changing source package contents, removing files, adding excluded sources, altering checksums, or using unresolved ayah keys and confirming that the import refuses the run without persisting partial tafsir data.

**Acceptance Scenarios**:

1. **Given** a package where the source file set does not exactly match the final manifest, **When** the maintainer starts the import, **Then** the system refuses the import and records the mismatch in the report.
2. **Given** a package that contains any of the 9 excluded sources, **When** the maintainer starts the import, **Then** the system refuses the import, identifies the excluded source in the report, and keeps no tafsir foundation record for that excluded source.
3. **Given** a tafsir entry references an ayah key that cannot be resolved to a canonical ayah, **When** the maintainer starts the import, **Then** the system refuses the import and persists no partial tafsir data.
4. **Given** the Quran foundation ayah records already exist, **When** the tafsir import runs, **Then** the import does not add, update, delete, or copy canonical Quran ayah text.

---

### User Story 3 - Re-run safely with explicit operator intent (Priority: P3)

As a backend maintainer, I need safe re-run behavior so I can rebuild the tafsir foundation intentionally without accidentally appending duplicate data or replacing unrelated Quran data.

**Why this priority**: Foundation imports often need to be repeated during development and validation. Re-runs must be predictable, auditable, and limited to tafsir-owned data.

**Independent Test**: Can be tested by running the import once, attempting a second run without explicit rebuild intent, and then running with explicit rebuild intent while confirming only tafsir-owned data is replaced.

**Acceptance Scenarios**:

1. **Given** tafsir foundation data already exists, **When** the maintainer starts another import without explicit rebuild intent, **Then** the system refuses the run and explains that tafsir data already exists.
2. **Given** tafsir foundation data already exists, **When** the maintainer starts an import with explicit rebuild intent, **Then** the system replaces only tafsir-owned data and leaves Quran foundation data unchanged.
3. **Given** validation fails after a rebuild has started, **When** the system detects the failure, **Then** the system rolls back the attempted tafsir changes and reports that no partial rebuild was kept.

---

### User Story 4 - Produce audit-ready import reports (Priority: P4)

As a reviewer or product owner, I need human-readable and machine-readable import reports so I can verify counts, source identities, excluded sources, warnings, failures, and whether data was persisted.

**Why this priority**: The import is not just a data load; it is an auditable scholarly-data gate. Reports are the evidence that later API/UI work is standing on a verified foundation.

**Independent Test**: Can be tested by completing a passing import and by forcing known failures, then reviewing the generated reports for verdict, counts, source summaries, warnings, hard checks, and rollback status.

**Acceptance Scenarios**:

1. **Given** an import completes successfully, **When** the reviewer opens the reports, **Then** the reports show a passing verdict, imported source counts, language counts, imported row counts, and all hard checks.
2. **Given** an import is refused before data is written, **When** the reviewer opens the reports, **Then** the reports explain the refusal reason and show that no tafsir data was persisted.
3. **Given** an import fails after validation checks run, **When** the reviewer opens the reports, **Then** the reports identify failed checks and state whether attempted changes were rolled back.
4. **Given** all sources have unknown license and provenance, **When** any import report is generated, **Then** the report prominently includes the internal-use-only warning.
5. **Given** validation passes but the required import reports cannot be written, **When** the import attempts to finish, **Then** the run is not accepted and no tafsir changes are kept.

### Edge Cases

- The package folder is missing, or one of `README.md`, `manifest.json`, `package-report.md`, or `sources/` is missing.
- The package manifest is not marked as the final import manifest.
- The package contains more or fewer than 84 approved source files.
- The package manifest reports counts other than 84 approved, 9 excluded, 35 Arabic, 49 non-Arabic, or 33 languages.
- A source file is unreadable, malformed, empty, or does not match its recorded size or checksum.
- A source file has fewer or more than 6,236 top-level ayah keys.
- A source entry has empty tafsir text after grouped-pointer resolution.
- A source entry contains inline markup or source-specific formatting; the import preserves it exactly and does not attempt public rendering cleanup.
- A grouped source points an ayah to a missing or invalid tafsir text block.
- Two entries in the same source resolve to duplicate tafsir mappings for the same ayah.
- Canonical ayah data is missing or does not contain all required verse keys.
- A run starts while tafsir data already exists and the maintainer has not explicitly requested a rebuild.
- Validation passes, but the required import reports cannot be written.
- An application user, public client, or frontend attempts to trigger the import; Feature 007 exposes no app-facing access path.
- License/provenance remains unknown for all sources; the import may proceed for internal use, but later public exposure must not treat this as publication clearance.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST accept only the final staged tafsir package located at `resources/import-sources/quran-tafsirs/` unless an operator explicitly provides an alternate package path for controlled validation.
- **FR-002**: The system MUST require the package to contain `README.md`, `manifest.json`, `package-report.md`, and a `sources/` folder.
- **FR-003**: The system MUST treat the package manifest as the authoritative source of source identities, source files, counts, language metadata, contributor metadata, checksum metadata, exclusion summaries, and license/provenance warnings.
- **FR-004**: The system MUST refuse to import from any manifest that is not identified as the final import manifest.
- **FR-005**: The system MUST import exactly 84 approved tafsir sources from the final package.
- **FR-006**: The system MUST verify that the approved imported set contains exactly 35 Arabic sources and 49 non-Arabic sources across 33 languages.
- **FR-007**: The system MUST preserve each approved source's stable source key, display names, short names, language code, language direction, contributor identity, tafsir category, package file path, original source path, checksum, file size, license status, and provenance status.
- **FR-008**: The system MUST preserve `license = unknown` and `provenance = unknown` for every imported source until a later licensing process changes those values.
- **FR-009**: The system MUST treat unknown license/provenance as an import warning and MUST NOT present internal import as public publishing clearance.
- **FR-010**: The system MUST keep the 9 excluded sources out of tafsir foundation records while listing them in import reports with their exclusion reasons.
- **FR-011**: The system MUST refuse the import if any excluded, incomplete-coverage, non-tafsir, or suspect-quality source appears in the importable source set.
- **FR-012**: The system MUST verify each approved source file is readable, structurally valid, and matches the file size and checksum recorded in the manifest before data is accepted.
- **FR-013**: The system MUST verify each approved source contains exactly 6,236 ayah-addressed entries.
- **FR-014**: The system MUST resolve every source ayah key, grouped ayah key, and grouped pointer target to an existing canonical Quran ayah.
- **FR-015**: The system MUST refuse the import if any tafsir entry cannot be linked to a canonical ayah.
- **FR-016**: The system MUST store tafsir text exactly as imported, including inline markup and source-specific formatting.
- **FR-017**: The system MUST NOT copy Quran ayah text into tafsir-owned records.
- **FR-018**: The system MUST preserve grouped tafsir behavior by storing each tafsir text block once and linking every covered ayah to that text block.
- **FR-019**: The system MUST support multiple tafsir sources for the same ayah.
- **FR-020**: The system MUST support Arabic and non-Arabic tafsir sources, including source language direction.
- **FR-021**: The system MUST prevent duplicate tafsir mappings for the same source and ayah.
- **FR-022**: The system MUST refuse a normal run when tafsir foundation data already exists.
- **FR-023**: The system MUST allow an explicit rebuild run that replaces tafsir-owned data only.
- **FR-024**: The system MUST keep all attempted changes from a failed import out of the accepted tafsir foundation.
- **FR-025**: The system MUST verify the source package has not changed during the import before accepting the run as successful.
- **FR-026**: The system MUST produce both human-readable and machine-readable import reports for successful, refused, and failed runs whenever report writing is possible.
- **FR-027**: Each import report MUST include verdict, persistence status, source path, package counts, source summaries, excluded source summaries, language summaries, imported row totals, hard check results, warnings, errors, and informational notes.
- **FR-028**: Each import report MUST include hard check identifiers using the `TAFSIR-` prefix so future reviewers and tests can refer to stable validation names.
- **FR-029**: The system MUST NOT accept an import run as successful if the required import reports cannot be written; in that case, no tafsir changes may be kept.
- **FR-030**: Feature 007 import behavior MUST be local operator-only and MUST NOT define app-user permissions, UI access, or API access.
- **FR-031**: The feature MUST NOT create public reader behavior, search indexing, comparison UI, frontend screens, public API behavior, translation features, startup seeding, or licensing-clearance workflows.
- **FR-032**: The feature MUST NOT add, update, delete, or reseed Quran foundation data.

### Key Entities *(include if feature involves data)*

- **Tafsir Source**: A curated tafsir work or edition approved for import. Key attributes include source key, language, direction, display names, contributor, tafsir category, package file, original source reference, checksum, file size, license status, and provenance status.
- **Tafsir Text Block**: A unit of tafsir content from a source. A block may explain one ayah or a grouped range of ayahs. Key attributes include source, leader ayah, source entry key, tafsir text, text format, and covered ayah keys.
- **Tafsir Ayah Link**: The relationship between one canonical ayah and the tafsir text block that covers it for a specific source. It enables later features to find all tafsir content for an ayah without copying Quran ayah text.
- **Excluded Source Summary**: A report-only summary of a curated source that was inspected but not approved for Feature 007 import, including status and reason.
- **Import Run Report**: The audit record for an import attempt, including verdict, counts, warnings, hard checks, errors, and whether data was persisted.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A valid import run accepts exactly 84 approved tafsir sources and reports zero imported excluded sources.
- **SC-002**: A valid import run reports exactly 35 Arabic sources, 49 non-Arabic sources, and 33 languages.
- **SC-003**: A valid import run links tafsir content to all 6,236 canonical ayahs for every approved source, producing 523,824 source-to-ayah tafsir links.
- **SC-004**: 100% of imported source files match their manifest file size and checksum at validation time.
- **SC-005**: 100% of imported ayah keys and grouped pointer targets resolve to canonical ayahs.
- **SC-006**: 0 Quran ayah text values are copied into tafsir-owned records.
- **SC-007**: 100% of stored tafsir text values match the imported source text exactly after grouped-pointer resolution.
- **SC-008**: A package containing any excluded source is refused before any accepted tafsir foundation data is kept.
- **SC-009**: A failed validation run keeps 0 partial tafsir changes.
- **SC-010**: Every completed, refused, or failed import attempt with report access produces reports containing verdict, counts, hard checks, warnings, and persistence status.
- **SC-011**: Every import report includes the unknown license/provenance warning for all sources until licensing is explicitly cleared in a later feature.
- **SC-012**: 0 successful imports are accepted without both required report formats being written.
- **SC-013**: 0 Feature 007 import actions are exposed through application UI or public/API access paths.

## Assumptions

- The final curated package at `resources/import-sources/quran-tafsirs/` is the intended Feature 007 input package.
- The package counts are locked for this feature: 84 approved sources, 9 excluded sources, 35 Arabic approved sources, 49 non-Arabic approved sources, and 33 languages.
- The approved package intentionally includes both Arabic and non-Arabic tafsir sources in Feature 007.
- The 9 excluded sources remain out of scope for import, are not persisted in tafsir foundation records, and appear only in reports.
- Unknown license/provenance is acceptable for internal foundation import only and does not grant public publishing rights.
- Canonical Quran ayah data already exists and is the only source for Quran ayah text.
- Grouped tafsir text should be preserved without duplicating the same text for every ayah it covers.
- Source tafsir text may include inline markup or source-specific formatting; Feature 007 preserves it exactly and defers rendering cleanup to later API/UI/public-reader features.
- Source metadata remains source-level metadata in Feature 007; separate public catalog, licensing workflow, search, API, and UI behavior are later features.
- Feature 007 is operated locally by maintainers; app-user roles and permissions for viewing or publishing tafsir are out of scope.
