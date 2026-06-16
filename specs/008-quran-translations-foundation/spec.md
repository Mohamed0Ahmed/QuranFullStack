# Feature Specification: Quran Translations Foundation

**Feature Branch**: `008-quran-translations-foundation`  
**Created**: 2026-06-15  
**Status**: Draft  
**Input**: User description: "Read the Feature 008 Quran Translations Foundation planning report and create a GitHub Spec Kit specification, generation only, with enough clarity for a cheaper model to implement later."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Import Approved Translation Sources (Priority: P1)

As a data curator, I want the approved ayah-level Quran translation package imported into the dashboard data store so the project has a complete, verified translation foundation for future product work.

**Why this priority**: This is the core value of the feature. Without a complete accepted import, future translation browsing, review, search, or publishing work has no trustworthy data foundation.

**Independent Test**: Can be fully tested by running one import against the frozen local package and verifying that all approved translation sources and all ayah-level translation entries are accepted with the expected counts.

**Acceptance Scenarios**:

1. **Given** the frozen Quran translations source package is present and valid, **When** a maintainer starts a translation import, **Then** the system accepts exactly 167 approved sources and 1,041,412 ayah translation mappings.
2. **Given** the source package contains both simple and with-footnotes variants, **When** the import completes, **Then** the accepted source totals are exactly 129 simple sources and 38 with-footnotes sources.
3. **Given** every approved source contains the full Quran ayah key set, **When** the import validates coverage, **Then** every approved source is confirmed to contain exactly 6,236 ayah keys with no missing or extra ayahs.
4. **Given** a translation text contains inline footnote markers or embedded HTML, **When** that ayah is imported, **Then** the stored translation text remains byte-equal to the source text.

---

### User Story 2 - Reject Unsafe or Out-of-Scope Sources (Priority: P2)

As a maintainer, I want hard validation to block incomplete, empty, duplicate, word-by-word, or package-mismatched sources so the data foundation never silently includes unsafe translation data.

**Why this priority**: Quran translation data must be auditable and source-safe. A partial import or a count-only validation would create downstream scholarly and product risk.

**Independent Test**: Can be fully tested by introducing one invalid package condition at a time and verifying that the import refuses the run without committing any translation data.

**Acceptance Scenarios**:

1. **Given** a source file has any empty, missing, null, or non-string translation text, **When** the import validates that source, **Then** the run is rejected before acceptance.
2. **Given** a source file is word-by-word rather than ayah-level, **When** the import evaluates v1 eligibility, **Then** that source is excluded and is never persisted.
3. **Given** the package file set, size, or hash differs from the frozen manifest, **When** the import validates package integrity, **Then** the run is rejected.
4. **Given** a hard validation check fails after data has begun loading, **When** the run ends, **Then** no partial translation import remains committed.

---

### User Story 3 - Produce Acceptance Reports (Priority: P3)

As a maintainer, I want machine-readable and human-readable import reports so every accepted or rejected run can be reviewed, reproduced, and compared against the frozen package contract.

**Why this priority**: The source package has unknown publication provenance and must remain internally auditable. Reports are the v1 audit record for file integrity, counts, exclusions, warnings, and final acceptance.

**Independent Test**: Can be fully tested by completing a valid import and inspecting the generated JSON and Markdown reports for the required totals, validation checks, warnings, and final verdict.

**Acceptance Scenarios**:

1. **Given** a valid import completes all hard checks, **When** reports are written, **Then** both JSON and Markdown reports exist before the run is accepted.
2. **Given** the reports are inspected, **When** a maintainer reviews totals, **Then** the reports show approved sources, excluded sources, per-type counts, language coverage, ayah mapping totals, hard-check results, warnings, and the final verdict.
3. **Given** license and provenance are unknown for the translation corpus, **When** the reports are generated, **Then** they include a clear internal-use provenance warning and do not present the data as publish-ready.

---

### User Story 4 - Safely Replace a Previous Import (Priority: P4)

As a maintainer, I want repeated imports protected by an explicit replacement flow so accidental duplicate imports are refused and intentional replacements remain atomic.

**Why this priority**: Translation data is large and foundational. A re-run must not create duplicates, mix package versions, or leave a partial replacement.

**Independent Test**: Can be fully tested by attempting a second import after data already exists, then attempting an explicit replacement with a valid package and with an invalid package.

**Acceptance Scenarios**:

1. **Given** translation data already exists, **When** a maintainer starts another normal import, **Then** the run is refused and existing data is left unchanged.
2. **Given** translation data already exists and a maintainer explicitly requests replacement, **When** the package fully validates, **Then** the previous translation data is replaced atomically with the new accepted import.
3. **Given** translation data already exists and a maintainer explicitly requests replacement, **When** the replacement package fails any hard check, **Then** the previous accepted data remains unchanged.

### Edge Cases

- The package root is missing one of the required top-level files: `README.md`, `manifest.json`, `package-report.md`, `source-display-metadata.json`, or `sources/`.
- `source-display-metadata.json` is missing, invalid JSON, not final, incomplete, contains fewer or more than 167 records, contains an empty required display field, or its `sourceKey` set differs from the manifest.
- A source file listed by the manifest is absent from `sources/`, or an extra unlisted source file is present.
- A source file has the correct filename but the wrong size or hash.
- A source file has valid JSON but the wrong root shape, malformed verse keys, non-object values, missing `t`, non-string `t`, or empty-string `t`.
- A source has exactly 6,236 records but not the exact canonical 6,236 verse-key set.
- JSON key order does not match Mushaf order; ayahs must still resolve by verse key.
- A physical `simple` source contains inline `[[...]]` notes and must be classified by content as `with_footnotes`.
- A language has only one source and that source is excluded by the no-empty-text rule; the language must not be partially included.
- A translation text contains inline `[[...]]`, embedded HTML, anchor tags, diacritics, or non-Latin scripts; text must remain unchanged.
- A source direction differs from the most common direction for its language; direction must be source-level.
- A duplicate ayah mapping would be created for the same source and ayah.
- The import reaches report generation but one required report cannot be written.
- A hard check fails during an explicit replacement after existing accepted translation data is present.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST import only the frozen local package at `resources/import-sources/quran-translations/`.
- **FR-002**: The feature MUST require the package to contain `README.md`, `manifest.json`, `package-report.md`, `source-display-metadata.json`, and a `sources/` directory before any import can be accepted.
- **FR-003**: The feature MUST read both package contracts: `manifest.json` as the file, hash, source-set, coverage, and exclusion contract; and `source-display-metadata.json` as the display metadata contract.
- **FR-004**: The feature MUST reject the run if `manifest.json` is not the final import manifest.
- **FR-005**: The feature MUST reject the run if `source-display-metadata.json` is missing, invalid, not final, incomplete, has any empty required display field, or does not align exactly with the manifest source set.
- **FR-006**: The feature MUST accept exactly 167 approved ayah-level translation sources from the final package.
- **FR-007**: The feature MUST accept exactly 129 `simple` sources and exactly 38 `with_footnotes` sources.
- **FR-008**: The feature MUST treat the 19 excluded sources as non-importable: 11 word-by-word files, 6 empty-text files, and 2 unattributed near-duplicate files.
- **FR-009**: The feature MUST import no word-by-word translation resources in v1.
- **FR-010**: The feature MUST require every approved source to contain exactly the canonical 6,236 Quran verse keys, with no missing, extra, malformed, or position-derived ayah mappings.
- **FR-011**: The feature MUST reject any source containing an empty, null, missing, or non-string `t` value, even if the source has 6,236 keys.
- **FR-012**: The feature MUST resolve every source verse key to an existing Quran ayah before acceptance.
- **FR-013**: The feature MUST preserve each approved translation text exactly as supplied by the source, including inline `[[...]]` footnote markers, embedded HTML, whitespace, punctuation, diacritics, and script-specific characters.
- **FR-014**: The feature MUST NOT parse, strip, sanitize, normalize, restructure, or split inline footnotes or embedded markup in v1.
- **FR-015**: The feature MUST classify translation type by content when needed, so a physical simple-file containing inline footnote markers is stored as `with_footnotes`.
- **FR-016**: The feature MUST create one accepted translation source record per approved source and one accepted ayah translation entry per approved source and ayah.
- **FR-017**: The feature MUST accept exactly 1,041,412 ayah translation entries for the final package.
- **FR-018**: The feature MUST prevent duplicate entries for the same translation source and Quran ayah.
- **FR-019**: The feature MUST store source-level selection metadata needed for future app use: source key, language code, English language name, Arabic language name, native name, source direction, translation type, English display name, Arabic display name, optional translator key, optional English translator name, optional Arabic translator name, inline-footnote flag, HTML-markup flag, and content coverage count.
- **FR-020**: The feature MUST require English and Arabic display names for every imported source.
- **FR-021**: The feature MUST treat translator names as optional, non-blocking metadata and MUST NOT use them as the primary future user-facing selector.
- **FR-022**: The feature MUST keep source file paths, package file paths, hashes, sizes, license values, provenance values, and package-integrity metadata in the manifest and reports only, not in the v1 imported translation records.
- **FR-023**: The feature MUST NOT copy or persist Arabic Quran ayah text as part of translation import records.
- **FR-024**: The feature MUST complete package validation, data loading, hard checks, final package re-verification, and report writing as one acceptance unit; if any hard check or required report fails, the whole run is rejected.
- **FR-025**: The feature MUST leave no partial accepted import after any hard-check failure.
- **FR-026**: The feature MUST refuse a normal re-run when translation data already exists.
- **FR-027**: The feature MUST allow an explicit replacement run only after re-validating the package before replacement.
- **FR-028**: The feature MUST keep existing accepted translation data unchanged when an explicit replacement run fails validation or reporting.
- **FR-029**: The feature MUST produce a machine-readable JSON report for each attempted import after the report output path is resolved, including per-source results, totals, validation checks, warnings, timestamps, and final outcome.
- **FR-030**: The feature MUST produce a human-readable Markdown report for each attempted import after the report output path is resolved, including verdict, scope, package paths, approved and excluded summaries, reclassified sources, validation checks, warnings, and final confirmation.
- **Reporting exception**: Attempts that fail before report output can be resolved, or fail because report writing itself is unavailable, MUST return a non-zero console error and MUST NOT persist accepted translation data.
- **FR-031**: The feature MUST include the provenance warning `TR-PROVENANCE-WARNING` in reports because license and provenance are unknown for all sources and the v1 import is internal-use only, not publish-ready.
- **FR-032**: The feature MUST include these hard validation checks in acceptance evidence: `TR-PACKAGE-SHAPE`, `TR-MANIFEST-FINAL`, `TR-DISPLAY-METADATA-FINAL`, `TR-DISPLAY-METADATA-SET`, `TR-DISPLAY-METADATA-REQUIRED-FIELDS`, `TR-SOURCE-COUNT`, `TR-TYPE-COUNTS`, `TR-EXCLUDED-COUNT`, `TR-SOURCE-SET`, `TR-SOURCE-HASH`, `TR-NO-EXCLUDED-SOURCES`, `TR-JSON-SHAPE`, `TR-COVERAGE-COUNT`, `TR-NO-EMPTY-TEXT`, `TR-AYAH-KEYS-RESOLVE`, `TR-NO-DUPLICATE-AYAH-ENTRY`, `TR-TEXT-UNCHANGED`, `TR-NO-QURAN-TEXT-COPY`, `TR-POSTCOPY-SOURCE-ROWS`, `TR-POSTCOPY-AYAH-MAPPINGS`, `TR-SOURCE-UNCHANGED`, `TR-REPORT-WRITTEN`, `TR-ROLLBACK-ON-FAIL`, and `TR-RERUN-GUARD`.
- **FR-033**: The feature MUST include these informational checks in acceptance evidence: `TR-INLINE-MARKUP`, `TR-LANGUAGE-COVERAGE`, and `TR-RECLASSIFIED`.
- **FR-034**: The feature MUST exclude UI, API endpoints, search indexing, startup seeding, permissions and access changes, word-by-word import, footnote parsing, separate language cataloging, separate footnote cataloging, source license publication, and import-run history from v1.

### Key Entities *(include if feature involves data)*

- **Translation Source**: One approved translation edition from the frozen package. It represents a source-level translation selection unit with language names, source direction, translation type, display names, optional translator metadata, content flags, and a coverage count of 6,236.
- **Translation Ayah Entry**: One exact translation text for one Translation Source and one Quran ayah. It is keyed by source and resolved ayah, preserves the source text exactly, and never stores copied Arabic Quran ayah text.
- **Translation Source Package**: The frozen local import package containing source files plus package-level contracts. It defines the approved source set, excluded source set, expected counts, file hashes, file sizes, and package warnings.
- **Display Metadata Contract**: The required display metadata file aligned to the manifest source set. It supplies final source display names and language metadata for v1 future selection.
- **Import Run Report**: The JSON and Markdown acceptance evidence produced for an attempted import. It records counts, per-source validation, warnings, hard-check outcomes, and final verdict.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A valid final package import accepts exactly 167 translation sources, exactly 129 simple sources, exactly 38 with-footnotes sources, and exactly 1,041,412 ayah translation entries.
- **SC-002**: 100% of approved sources validate against the exact 6,236 canonical verse-key set before the run is accepted.
- **SC-003**: 100% of accepted ayah translation entries are byte-equal to their source `t` values, including inline notes and embedded markup.
- **SC-004**: 0 word-by-word, empty-text, missing-text, malformed-text, excluded, or unattributed near-duplicate sources are accepted into v1 data.
- **SC-005**: 0 duplicate source-and-ayah translation entries exist after a successful import.
- **SC-006**: 0 copied Arabic Quran ayah text values are introduced by the translation import.
- **SC-007**: 100% of hard validation checks listed in FR-032 pass before an import is accepted.
- **SC-008**: 100% of hard-check, package-integrity, source-resolution, and report-writing failures leave no partial accepted import.
- **SC-009**: A normal repeated import is refused 100% of the time when translation data already exists, unless the maintainer explicitly requests replacement.
- **SC-010**: 100% of accepted runs produce both JSON and Markdown reports before the run is considered accepted.
- **SC-011**: A maintainer can verify approved counts, excluded counts, language coverage, per-type split, warnings, and final verdict from the reports without inspecting source code.
- **SC-012**: Future product work can identify a translation by language names, translation type, display names, and source direction for 100% of accepted sources.

## Assumptions

- The feature is generation-only at this stage; implementation, migrations, tests, and backend source changes happen later.
- The final source package already exists locally at `resources/import-sources/quran-translations/` and is the only package in scope for this feature.
- `resources/` is local and gitignored, so the spec treats package files as required local input rather than committed source artifacts.
- The final package counts are frozen by the planning report and decisions addendum: 167 approved sources, 19 excluded sources, 83 covered languages, 129 simple sources, 38 with-footnotes sources, and 1,041,412 ayah mappings.
- The import is an internal data-curation workflow, not a public publishing feature.
- Unknown license and provenance are acceptable only for internal import curation; reports must preserve a warning that the data is not publish-ready.
- Existing Quran ayah records already provide the authoritative ayah identity and verse-key resolution target.
- Future UI, API, search, permissions, publishing, sanitization, and word-by-word translation work will be specified separately.
