# Docs / Reports File Organization Report

**Date:** 2026-06-10
**Scope:** File organization only — group `docs/` (plans) and `Backend/report/` (reports) into
one subfolder per feature/scope. No code, schema, migrations, imports, or report/plan **content**
changed (only path links, see §6). Nothing was deleted, duplicated, or committed.

**Verdict: PASS** — all 10 files moved and classified cleanly; 2 internal links fixed; and (in a
follow-up pass) the 7 live external pointers in workspace governance/spec files were updated to the
new paths (§7). The only old-path references left are 2 historical `User description:` quotes, kept
verbatim by design.

> **Note on location:** the plan/design docs live in the **workspace repo** at
> `/projects/Dashboard/App/docs/`, while the reports live in the **Backend submodule** at
> `Backend/report/`. Per the confirmed decision, each was organized **in place**; no file crossed
> the submodule boundary.

---

## 1. Before / After tree

### `docs/` (workspace repo)

**Before (flat):**
```
docs/
  feature-003-word-identity-links-implementation-plan.md
  manhaj-qurani-layout-foundation-plan.md
  manhaj-qurani-mushaf-words-layout-data-foundation-plan.md
  manhaj-qurani-quran-words-display-tables-foundation-plan.md
```

**After:**
```
docs/
  README.md                                   (new index)
  feature-001-layout-foundation/
    manhaj-qurani-layout-foundation-plan.md
  feature-002-quran-foundation/
    manhaj-qurani-mushaf-words-layout-data-foundation-plan.md
  feature-003-word-display-tables/
    manhaj-qurani-quran-words-display-tables-foundation-plan.md
  feature-003-word-identity-links/
    feature-003-word-identity-links-implementation-plan.md
```

### `Backend/report/` (Backend submodule)

**Before (flat):**
```
Backend/report/
  ayah-37-130-word-count-investigation.md
  feature-003-word-identity-links-restructure-report.md
  imlaei-clean-import-binding-report.md
  quran-foundation-import-source-readiness-report.md
  word-import-source-normalization-audit-report.md
  words-unique-tables-audit-report.md
```

**After:**
```
Backend/report/
  README.md                                   (new index)
  file-organization-report.md                 (this report)
  feature-002-quran-foundation/
    quran-foundation-import-source-readiness-report.md
    ayah-37-130-word-count-investigation.md
  feature-003-word-display-tables/
    word-import-source-normalization-audit-report.md
    words-unique-tables-audit-report.md
  feature-003-imlaei-clean-key/
    imlaei-clean-import-binding-report.md
  feature-003-word-identity-links/
    feature-003-word-identity-links-restructure-report.md
```

---

## 2. Moved files (10)

### Plans → `docs/`
| File | From | To |
| --- | --- | --- |
| `manhaj-qurani-layout-foundation-plan.md` | `docs/` | `docs/feature-001-layout-foundation/` |
| `manhaj-qurani-mushaf-words-layout-data-foundation-plan.md` | `docs/` | `docs/feature-002-quran-foundation/` |
| `manhaj-qurani-quran-words-display-tables-foundation-plan.md` | `docs/` | `docs/feature-003-word-display-tables/` |
| `feature-003-word-identity-links-implementation-plan.md` | `docs/` | `docs/feature-003-word-identity-links/` |

### Reports → `Backend/report/`
| File | From | To |
| --- | --- | --- |
| `quran-foundation-import-source-readiness-report.md` | `Backend/report/` | `Backend/report/feature-002-quran-foundation/` |
| `ayah-37-130-word-count-investigation.md` | `Backend/report/` | `Backend/report/feature-002-quran-foundation/` |
| `imlaei-clean-import-binding-report.md` | `Backend/report/` | `Backend/report/feature-003-imlaei-clean-key/` |
| `word-import-source-normalization-audit-report.md` | `Backend/report/` | `Backend/report/feature-003-word-display-tables/` |
| `words-unique-tables-audit-report.md` | `Backend/report/` | `Backend/report/feature-003-word-display-tables/` |
| `feature-003-word-identity-links-restructure-report.md` | `Backend/report/` | `Backend/report/feature-003-word-identity-links/` |

Filenames were preserved exactly (no renames, no typos found, no duplicate conflicts).

---

## 3. Classification reason per file (by content, not filename)

| File | Folder | Reason (from header/content) |
| --- | --- | --- |
| `manhaj-qurani-layout-foundation-plan.md` | `feature-001-layout-foundation` | Header: *"Layout & Foundation (Phase 0) … Foundation before the first real Quran data feature (not Words/Ayahs yet)."* The pre-words foundation phase = Feature 001. |
| `manhaj-qurani-mushaf-words-layout-data-foundation-plan.md` | `feature-002-quran-foundation` | Header: *"Quran Mushaf Words & Layout Data Foundation (the first real backend data feature)."* This is Feature 002 (`002-mushaf-words-foundation`). |
| `manhaj-qurani-quran-words-display-tables-foundation-plan.md` | `feature-003-word-display-tables` | Header: *"Quran Words Display Tables Foundation … Builds on Feature 002."* Feature 003 display tables. |
| `feature-003-word-identity-links-implementation-plan.md` | `feature-003-word-identity-links` | Header: *"Word Identity Links Restructure (Implementation Plan)."* Feature 003 identity-links scope. |
| `quran-foundation-import-source-readiness-report.md` | `feature-002-quran-foundation` | Header: *"Quran Foundation Import — Source Readiness Report,"* Branch `002-mushaf-words-foundation`. |
| `ayah-37-130-word-count-investigation.md` | `feature-002-quran-foundation` | Investigates the Feature 002 **import** word-count warning for 37:130; references `specs/002-mushaf-words-foundation/source-provenance.md`. Import-data investigation. |
| `imlaei-clean-import-binding-report.md` | `feature-003-imlaei-clean-key` | Enriches `imlaei-simple.json` with the clean identity key (`word_key_imlaei_simple`) and binds it; the clean-key work underpins Feature 003 without-tashkeel identity. |
| `word-import-source-normalization-audit-report.md` | `feature-003-word-display-tables` | Audits annotation/mark normalization across the pipeline; its proposed fix targets the **Feature 003 rebuild / display tables** word-identity keys. |
| `words-unique-tables-audit-report.md` | `feature-003-word-display-tables` | Header: *"Feature 003 `words-display-tables` — audit of the two unique display tables."* |
| `feature-003-word-identity-links-restructure-report.md` | `feature-003-word-identity-links` | Header: *"Word Identity Links Restructure (Analysis Report)."* Feature 003 identity-links scope. |

---

## 4. New index files

- `docs/README.md` — explains `docs/` holds planning/design docs, grouped by feature; lists contents.
- `Backend/report/README.md` — explains `report/` holds audit/verification reports, grouped by
  feature; lists contents; points to this organization report.

---

## 5. Files intentionally left in place

- **None inside `docs/` or `Backend/report/`** — every pre-existing file was moved into a
  feature subfolder; the only root-level files now are the two new `README.md` indexes and this
  report.
- **Generated source data files were not touched** (out of scope): nothing under
  `resources/…` or the `derived/` artifacts was moved.
- **No code, specs, migrations, or `resources/report/` outputs were moved.**

---

## 6. Link changes made (inside moved files only)

Two path references inside moved files pointed at other moved files; both were updated
("optional relative links if absolutely needed"):

| File | Old reference | New reference |
| --- | --- | --- |
| `docs/feature-003-word-identity-links/feature-003-word-identity-links-implementation-plan.md` | `Backend/report/feature-003-word-identity-links-restructure-report.md` | `Backend/report/feature-003-word-identity-links/feature-003-word-identity-links-restructure-report.md` |
| `Backend/report/feature-002-quran-foundation/quran-foundation-import-source-readiness-report.md` | `Backend/report/quran-foundation-import-source-readiness-report.md` (self-path) | `Backend/report/feature-002-quran-foundation/quran-foundation-import-source-readiness-report.md` |

No other content was altered. Bare in-prose filename mentions between same-folder reports (e.g.
`word-import-source-normalization-audit-report.md` ↔ `words-unique-tables-audit-report.md`, both now
in `feature-003-word-display-tables/`) remain valid as siblings and were left untouched.

---

## 7. External reference updates (DONE in follow-up pass)

Moving the **plan** files invalidated path references in workspace governance and spec files. The
**7 live pointers** below were updated to the new paths (verified: each new target resolves to an
existing file):

| File:line | Old path → New path | Status |
| --- | --- | --- |
| `CLAUDE.md:9` | `docs/manhaj-qurani-quran-words-display-tables-foundation-plan.md` → `docs/feature-003-word-display-tables/…` | ✅ updated |
| `AGENTS.md:9` | same as above | ✅ updated |
| `specs/003-words-display-tables/plan.md:7` | same as above | ✅ updated |
| `specs/002-mushaf-words-foundation/plan.md:7` | `docs/manhaj-qurani-mushaf-words-layout-data-foundation-plan.md` → `docs/feature-002-quran-foundation/…` | ✅ updated |
| `specs/002-mushaf-words-foundation/spec.md:8` | same (Reference-plan pointer) | ✅ updated |
| `specs/002-mushaf-words-foundation/research.md:3` | same | ✅ updated |
| `specs/002-mushaf-words-foundation/checklists/requirements.md:35` | same | ✅ updated |
| `specs/002-mushaf-words-foundation/spec.md:6` | historical `User description:` quote | ⏸ left verbatim (by design) |
| `specs/001-layout-foundation/spec.md:6` | historical `User description:` quote | ⏸ left verbatim (by design) |

> The two `User description:` lines record what the user originally typed; rewriting them would
> falsify a historical input, so they keep the old path verbatim. Note: the `CLAUDE.md` / `AGENTS.md`
> pointers sit inside a Spec-Kit-managed `<!-- SPECKIT START/END -->` block — if Spec Kit
> regenerates that block from the doc location, re-apply the path there.

---

## 8. Uncertainty / suggested renames

- **`feature-002-quran-foundation` vs branch name.** The Feature 002 branch is
  `002-mushaf-words-foundation`; the folder uses the suggested `quran-foundation` label. If strict
  branch alignment is preferred, rename to `feature-002-mushaf-words-foundation` (in both `docs/`
  and `Backend/report/`).
- **`imlaei-clean-import-binding-report.md` placement.** The clean-key binding modifies the
  Feature 002 **import**, but exists to power Feature 003 without-tashkeel identity. Filed under
  `feature-003-imlaei-clean-key` per the requested scope; it could alternatively sit under
  `feature-002-quran-foundation` if you consider it purely import work.
- **`word-import-source-normalization-audit-report.md` placement.** Bridges Feature 002 → 003;
  filed under `feature-003-word-display-tables` because its proposed fix targets the Feature 003
  rebuild. Could alternatively be a shared `feature-003-word-identity` scope.
- **`ayah-37-130-word-count-investigation.md` placement.** Filed under `feature-002-quran-foundation`
  (it investigates an import-time data warning). Could be seen as word-data/display; kept with the
  import where the warning originates.

---

## 9. Validation

- Final tree verified with `find` (see §1): every pre-existing file now lives under a feature
  subfolder; only the new `README.md` files (+ this report) remain at the roots.
- No file deleted, duplicated, or renamed; counts match (4 plans, 6 reports).
- No build/test run (no code references were changed).
- Rename diffs carry **0 content change** (`git diff --cached -M` shows the committed files as pure
  renames); the only content edits are the two path-link fixes in §6.
- **Not committed.** The moves are currently **staged in the index** (the environment auto-stages
  filesystem changes; renames preserved) but **no commit was made** — both repo HEADs are unchanged
  (workspace `489cc20`, Backend `5a9940b`). Unstage with `git restore --staged .` if a fully
  unstaged working tree is preferred before review.

**Verdict: PASS** — clean, content-preserving reorganization; the 7 live external pointers were
updated to the new paths (§7); the only intentional exceptions are 2 historical `User description:`
quotes left verbatim.
