# Feature Specification: Quran Navigation Metadata Foundation

**Feature Branch**: `009-quran-navigation-metadata-foundation`
**Created**: 2026-06-16
**Status**: Draft
**Input**: User description: "The importer source path must be configurable via --source, with the staged package path as the documented default; do not hard-code an absolute local path. Read our plan - and according to the best practices of Github's speckit, create the spec, Generation Only. The implementation will be done using a cheaper model, so the specification and everything should be super clear."

<!--
  GLOSSARY (plain-language; read this first)
  - Ayah: a single Quran verse. There are exactly 6,236 ayahs in total. Each ayah has a
    verse reference written "surah:ayah" (for example, "2:141" = surah 2, ayah 141).
  - Verse reference / verse_key: the "surah:ayah" string that uniquely identifies an ayah.
  - Juz (الجزء): one of the 30 large reading divisions of the Quran.
  - Hizb (الحزب): one of the 60 divisions; each juz contains exactly 2 hizbs.
  - Rub (الربع): one of the 240 quarter-hizb divisions; each hizb contains exactly 4 rubs.
  - Sajda (سجدة): one of the 15 ayahs at which a prostration is marked. Each has a type:
    "required" or "optional".
  - Verse mapping: for one division, the list of which ayah ranges (per surah) belong to it,
    written per surah as "from-to" (for example surah 2 -> "1-141" means ayahs 2:1 through 2:141).
  - Staged source package: the already-prepared, verified folder of input files this feature reads.
  - Manifest: the package's machine-readable description (file list, counts, checksums, sizes).
  - Quran foundation: the already-imported core data from a previous feature (surahs, ayahs,
    pages, lines, words). This feature ADDS to it; it does not re-import or change it.
-->

## Clarifications

### Session 2026-06-16

- Q: What should the `--source` argument point to? → A: The package **root** directory containing `manifest.json` and `sources/` (default = the staged package folder `App/resources/import-sources/quran-navigation-metadata/`).
- Q: When a division's source `verses_count` disagrees with the count computed from its ranges, which value is stored? → A: Store the **computed** range count; emit a non-blocking warning carrying the source value (source `verses_count` is informational, not authoritative).

## User Scenarios & Testing *(mandatory)*

This feature is a back-office data-import operation. The primary actor is a **backend operator**
(an admin/engineer) who runs the import to load Quran navigation metadata into the existing data
foundation. The eventual beneficiaries are **future features** (reader, ayah/surah details,
navigation) that will need to answer "which juz / hizb / rub does this ayah belong to?" — but
building those consuming features is explicitly out of scope here.

### User Story 1 - Make every ayah navigable by juz / hizb / rub, and list the sajda ayahs (Priority: P1)

A backend operator runs the import. Afterward, the data foundation knows, for **every one of the
6,236 ayahs**, which juz, hizb, and rub it belongs to, and it holds the 30 juz, 60 hizb, 240 rub
divisions plus the 15 sajda locations (each with its type). This is the core value of the feature:
it unlocks division-based navigation that did not exist before.

**Why this priority**: This is the entire reason the feature exists. Without it there is no
navigation metadata and no ayah-to-division lookup. Delivered alone, it is already a usable,
complete data foundation that later features can build on.

**Independent Test**: Run the import against the staged package on a foundation that already has
the 6,236 ayahs. Verify the four datasets are recorded with exact counts (30 / 60 / 240 / 15) and
that picking any ayah returns a juz, a hizb, and a rub, and that the 15 sajda ayahs are listed
with the correct type.

**Acceptance Scenarios**:

1. **Given** the Quran foundation already contains the 6,236 ayahs and the staged source package is available, **When** the operator runs the import, **Then** the system records 30 juz, 60 hizb, 240 rub, and 15 sajda, and assigns a juz, hizb, and rub to all 6,236 ayahs.
2. **Given** a successful import, **When** a consumer looks up any ayah by its verse reference, **Then** the system returns the juz number, hizb number, and rub number that ayah belongs to.
3. **Given** a successful import, **When** a consumer lists the sajda locations, **Then** exactly 15 are returned, each linked to a real ayah and labeled "required" or "optional".
4. **Given** a successful import, **When** the hierarchy is inspected, **Then** each hizb maps to exactly one juz and each rub maps to exactly one hizb.

---

### User Story 2 - Reject any import that is not provably correct (Priority: P2)

A backend operator runs the import against a source that is incomplete, tampered, or internally
inconsistent (wrong counts, a verse reference that points to no real ayah, ranges that leave a gap
or overlap, an ayah left with no division, an unknown sajda type, or files that don't match the
manifest). The system **refuses to persist anything** and reports exactly what failed.

**Why this priority**: Quran data must be trustworthy. A silent partial or wrong import would
corrupt navigation for every downstream feature. Strong validation is what makes the data safe to
rely on. This is testable and valuable independently of how the data is later consumed.

**Independent Test**: Run the import against deliberately broken inputs (e.g., a juz range with a
gap, a sajda verse reference that doesn't resolve, a file whose checksum doesn't match the
manifest) and confirm the system aborts with a clear reason and the database is unchanged.

**Acceptance Scenarios**:

1. **Given** a source whose file set, checksums, sizes, or record counts do not match the manifest, **When** the import runs, **Then** it aborts before persisting and reports which expectation was violated.
2. **Given** a division or sajda whose verse reference does not resolve to an existing ayah, **When** the import validates, **Then** it aborts and names the unresolved reference.
3. **Given** that the juz (or hizb, or rub) ranges do not cover all 6,236 ayahs exactly once, **When** the import validates, **Then** it aborts and reports the gap or overlap.
4. **Given** any hard validation failure, **When** the import stops, **Then** no navigation records and no ayah assignments are written (all-or-nothing).
5. **Given** a sajda whose type is neither "required" nor "optional", **When** the import validates, **Then** it aborts and reports the invalid value.

---

### User Story 3 - Run the import from a configurable source, safely and repeatably (Priority: P2)

A backend operator can point the import at a chosen source package by passing a source path. When
no path is given, the import uses the documented default staged-package location — which is
resolved relative to the workspace, never a hard-coded machine-specific absolute path, so the same
command works on any checkout/machine. Re-running is safe: the import refuses to overwrite existing
navigation data unless an explicit "force" option is given, and "force" reloads only the navigation
data without disturbing anything else.

**Why this priority**: Operability and repeatability. The import must be runnable in different
environments without editing the command, and must never silently clobber or be blocked
unrecoverably. It must also be impossible for this import to damage unrelated Quran data.

**Independent Test**: Run with an explicit source path and confirm it reads that location; run with
no path and confirm it uses the documented default (no absolute machine path involved); run twice
without "force" (second run refuses and changes nothing); run again with "force" (atomically
reloads and the result equals a fresh import); confirm unrelated data is untouched throughout.

**Acceptance Scenarios**:

1. **Given** an explicit source path argument, **When** the import runs, **Then** it reads the package from that path.
2. **Given** no source path argument, **When** the import runs, **Then** it uses the documented default staged-package location, resolved relative to the workspace (not an absolute machine-specific path).
3. **Given** navigation data already exists, **When** the import runs without the force option, **Then** it refuses and makes no changes.
4. **Given** navigation data already exists, **When** the import runs with the force option, **Then** it atomically clears and reloads only the navigation data and the ayah juz/hizb/rub assignments, and the final state equals a fresh successful import.
5. **Given** any run of this import, **When** it completes (success or failure), **Then** surah, ayah text, page, line, word, tafsir, translation, mutashabihat, morphology, and i3rab data are unchanged.

---

### User Story 4 - Produce an auditable import report (Priority: P3)

Every run produces a report — both machine-readable and human-readable — recording the verdict,
whether data was persisted, whether force was used, the resolved source path, per-dataset totals,
the ayah-coverage summary, each validation check's result, any warnings, and an explicit statement
that no Quran ayah text was read or stored.

**Why this priority**: Auditability and traceability. Operators and reviewers need a durable record
of what happened and proof of the data-safety guarantees. Valuable but secondary to the import and
its correctness.

**Independent Test**: Run the import (both a passing and a failing case) and confirm a report is
produced in both forms whose verdict, counts, and flags match the actual outcome and persisted
state, and that it asserts no Quran text was touched.

**Acceptance Scenarios**:

1. **Given** a completed run, **When** the operator opens the report, **Then** it shows the verdict, the persisted flag, the forced flag, the resolved source path, and per-dataset counts.
2. **Given** a completed run, **When** the report is read by a machine, **Then** it exposes the same information in a structured, parseable form.
3. **Given** any run, **When** the report is produced, **Then** it includes an explicit "no Quran ayah text was read or stored" assertion and an ayah-coverage summary (how many of the 6,236 ayahs received juz/hizb/rub).

---

### Edge Cases

- **Missing/incomplete package**: the source path (explicit or default) points to a folder missing one of the four source files or the manifest → abort with a clear error; no writes.
- **Manifest/content mismatch**: the manifest claims to be final but a file's checksum, byte size, or record count differs → abort; no writes.
- **Unresolved verse reference**: a division's first/last reference, or a sajda reference, points to an ayah that does not exist → abort; name the reference.
- **Gap or overlap**: the union of a division type's ranges misses an ayah or covers one twice → abort; report the specific ayah(s).
- **Incomplete ayah coverage**: after expanding the verse mappings, at least one ayah has no juz, hizb, or rub → abort (treated as a coverage failure).
- **Invalid sajda type**: a sajda type is anything other than "required" or "optional" → abort.
- **Broken hierarchy**: a hizb spans more than one juz, or a rub spans more than one hizb → abort.
- **Already populated, no force**: navigation data already exists and force is not supplied → refuse; make no changes.
- **Force with mid-run failure**: a forced reload fails partway → the operation rolls back so the database is left in a single consistent state (either the prior data intact, or the fully reloaded data), never a half-written mix.
- **Source changes mid-run**: a source file is modified between loading and persistence → detect and abort.
- **Count disagreement (non-blocking)**: a division's source `verses_count` disagrees with the computed range count (which is what gets stored), while coverage still holds → store the computed count, record a warning carrying the source value, and continue.
- **Sajda distribution differs (non-blocking)**: the type split is not the expected 11 optional / 4 required → record a warning and continue.
- **Source resources absent in an environment** (e.g., CI where local resources aren't present): the real-data run is skipped rather than failing the build; behavior verified with synthetic stand-in data.

## Requirements *(mandatory)*

### Functional Requirements

**Scope and data captured**

- **FR-001**: The system MUST import exactly four navigation datasets — juz, hizb, rub, and sajda — and no others, from the staged source package.
- **FR-002**: The system MUST record 30 juz divisions, 60 hizb divisions, 240 rub divisions, and 15 sajda locations, each identified by its own number (juz 1–30, hizb 1–60, rub 1–240, sajda 1–15).
- **FR-003**: For each juz, hizb, and rub division, the system MUST record its verse count and its first and last ayah (identified by verse reference). The stored verse count MUST be the count of ayahs **computed from the division's verse ranges**; the source `verses_count` field is treated as informational, not authoritative.
- **FR-004**: For each sajda, the system MUST record its number, the single ayah at which it occurs (by verse reference), and its type, where type is exactly one of "required" or "optional".
- **FR-005**: The system MUST associate every one of the 6,236 ayahs with the juz, hizb, and rub it belongs to, derived from the source verse mappings.
- **FR-006**: The system MUST record the division hierarchy so that the juz a hizb belongs to, and the hizb a rub belongs to, are both known and navigable.

**Linking and Quran-text safety**

- **FR-007**: The system MUST link every navigation record and sajda to ayahs by resolving the verse reference ("surah:ayah") against the existing ayah data. It MUST NOT depend on the source's internal numeric record ids aligning with stored ayah ids as the linking contract.
- **FR-008**: The system MUST NOT read, copy, store, derive from, or modify any Quran ayah text contained in the navigation sources. Existing ayah text MUST remain byte-for-byte unchanged by this feature.

**Source selection and source integrity**

- **FR-009**: Operators MUST be able to specify the source package location at run time via a source path argument (realized as `--source`). The argument MUST point to the package **root** directory — the folder that directly contains `manifest.json` and the `sources/` subfolder — and the system MUST resolve `manifest.json` and every source file relative to that root.
- **FR-010**: When no source path argument is provided, the system MUST use the documented default staged-package location (the package root `App/resources/import-sources/quran-navigation-metadata/`), resolved relative to the workspace/repository root. The default MUST NOT be a hard-coded, absolute, machine-specific path.
- **FR-011**: The system MUST treat the package manifest as the authoritative description of the source and MUST verify, before importing: the package type, the final-manifest flag, the exact expected set of source files (no missing and no unexpected files), and each file's checksum, byte size, and record count.
- **FR-012**: The system MUST verify that every record of each dataset contains its required fields (juz/hizb/rub: division number, verse count, first verse reference, last verse reference, verse mapping; sajda: sajda number, verse reference, type) and MUST reject any sajda type outside {"required", "optional"}.

**Validation (must all hold before persisting)**

- **FR-013**: The system MUST verify that every division's first and last verse reference, and every sajda's verse reference, resolves to an existing ayah.
- **FR-014**: The system MUST verify that, independently for juz, hizb, and rub, the union of the division ranges covers all 6,236 ayahs exactly once — with no gaps and no overlaps.
- **FR-015**: The system MUST verify the hierarchy: each hizb belongs to exactly one juz, and each rub belongs to exactly one hizb.
- **FR-016**: After a successful import, the system MUST guarantee that all 6,236 ayahs have a non-empty juz, hizb, and rub assignment (100% coverage).
- **FR-017**: The system MUST persist navigation data only if all hard validation checks pass. If any hard check fails, the system MUST abort and persist nothing (all-or-nothing; no partial writes).
- **FR-018**: The system MUST confirm that the source files are unchanged between the moment loading begins and the moment data is persisted.

**Re-run safety and isolation**

- **FR-019**: The system MUST refuse to run when navigation data already exists, unless an explicit force option is supplied; in the refusal case it MUST make no changes.
- **FR-020**: When the force option is supplied, the system MUST atomically clear and reload only the navigation data — the four division/sajda datasets and the ayahs' juz/hizb/rub assignments — such that on success the final state equals a fresh successful import.
- **FR-021**: The system MUST NOT add, change, or remove any surah, ayah (other than the added juz/hizb/rub assignments), page, line, word, tafsir, translation, mutashabihat, morphology, or i3rab data.
- **FR-022**: Adding the ayahs' juz/hizb/rub assignment attributes MUST be additive and MUST NOT change or remove any existing ayah attribute; these attributes MAY be empty at rest until populated by a successful import.

**Reporting**

- **FR-023**: Each run MUST produce both a machine-readable report and a human-readable report.
- **FR-024**: Each report MUST include: the overall verdict; whether data was persisted; whether the force option was used; the resolved source path; per-dataset totals (juz/hizb/rub/sajda); the ayah-coverage summary (how many of the 6,236 ayahs received juz/hizb/rub); the result of each validation check; all warnings and errors; and an explicit statement that no Quran ayah text was read or stored.
- **FR-025**: The system MUST raise non-blocking warnings when a division's source `verses_count` disagrees with the stored computed range count (the warning MUST carry the source value), or when the sajda type distribution differs from the expected 11 optional / 4 required, and MUST still proceed if all hard checks pass.

### Key Entities *(include if feature involves data)*

- **Juz**: a major reading division (30 total). Attributes: juz number; verse count; first and last ayah (by reference). Owns a set of ayahs.
- **Hizb**: a half-juz division (60 total). Attributes: hizb number; the juz it belongs to; verse count; first and last ayah. Each juz has exactly 2 hizbs.
- **Rub**: a quarter-hizb division (240 total). Attributes: rub number; the hizb it belongs to; verse count; first and last ayah. Each hizb has exactly 4 rubs.
- **Sajda**: a marked prostration location (15 total). Attributes: sajda number; the ayah it occurs at (by reference); type ("required" or "optional").
- **Ayah Navigation Assignment**: the juz, hizb, and rub that each existing ayah belongs to. Added to the existing ayah data; one juz + one hizb + one rub per ayah; covers all 6,236 ayahs after import.
- **Source Package & Manifest**: the staged input folder and its authoritative manifest (package type, final flag, file list, per-file checksum / size / record count, expected counts, allowed sajda types). The contract the import validates against.
- **Import Report**: the durable record of a run (verdict, persisted/forced flags, resolved source path, totals, coverage summary, per-check results, warnings/errors, no-Quran-text assertion).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After a successful import, 100% of the 6,236 ayahs can be navigated to their juz, hizb, and rub.
- **SC-002**: The recorded datasets match exact expected counts: 30 juz, 60 hizb, 240 rub, 15 sajda.
- **SC-003**: For each of juz, hizb, and rub, the divisions partition the Quran into ranges that cover every ayah exactly once — 0 gaps and 0 overlaps.
- **SC-004**: 100% of division and sajda verse references resolve to existing ayahs (0 unresolved).
- **SC-005**: An import whose source file set, checksums, sizes, or counts do not match the manifest is rejected with 0 records persisted.
- **SC-006**: Re-running the import without the force option changes 0 records; re-running with the force option yields a final state identical to a fresh successful import.
- **SC-007**: Existing Quran ayah text is unchanged by the import (0 modifications), verifiable by comparing ayah text before and after.
- **SC-008**: The import runs successfully on any checkout/machine without editing the command — either using the documented default source location or an explicit source argument — with no machine-specific absolute path required.
- **SC-009**: Every run produces a report whose verdict, counts, and flags match the actual persisted state, including the ayah-coverage summary and the no-Quran-text assertion.
- **SC-010**: 100% of the division hierarchy is consistent — every hizb maps to exactly one juz and every rub to exactly one hizb.

## Assumptions

- The Quran foundation from the previous feature is already imported: the 6,236 ayahs exist and are addressable by verse reference ("surah:ayah"). This feature adds to that foundation and does not re-import or alter surah/ayah/page/line/word data.
- The staged source package at the documented default location is the canonical input, and its manifest is final and authoritative. (A staged package already exists at the documented workspace location; its manifest declares counts 30 / 60 / 240 / 15.)
- "required" and "optional" are the only valid sajda types in scope.
- This is a back-office data-import operation performed by a backend operator. There is no end-user UI, API endpoint, search capability, or runtime/startup seeding in scope.
- Ruku, manzil, and audio-related metadata are out of scope.
- The default source location is resolved relative to the workspace/repository, not an absolute machine path. The local source resources may be absent in some environments (for example, CI); in that case the real-data run is skipped and behavior is verified with synthetic stand-in ayah data instead.
- A schema change is needed to add the ayahs' juz/hizb/rub assignment attributes; it is expected to be additive and to leave existing data intact (the actual mechanism is decided during planning, not here).
- The staged four input files do not contain Quran ayah text. They reference ayahs only by verse_key, first_verse_key, last_verse_key, and verse_mapping. Any upstream ayah text dataset remains excluded from this feature and must not be read or imported.
