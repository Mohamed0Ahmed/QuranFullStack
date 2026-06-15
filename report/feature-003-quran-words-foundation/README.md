# Feature 003 — Quran Words Foundation: Backend Reports

This folder holds the **backend audit, investigation, implementation, and verification reports**
for Feature 003 (Quran words foundation). It consolidates reports that previously lived in three
sibling folders (`feature-003-word-display-tables/`, `feature-003-imlaei-clean-key/`,
`feature-003-word-identity-links/`) under one umbrella scope: display/statistics tables, imlaei
clean identity key binding, and word identity links restructure.

Spec Kit artifacts live under `specs/003-words-display-tables/`. Forward-looking planning lives
under `docs/feature-003-*`. **This folder is only for backend reports.**

## Where reports go

- **Human-authored reports** (audits, investigations, phase reports, completion) belong **here**:
  `Backend/report/feature-003-quran-words-foundation/`
  (absolute: `/projects/Dashboard/App/Backend/report/feature-003-quran-words-foundation/`).
- **Importer/tool-generated reports** (e.g. `words-display-report.md`) default under
  `/projects/Dashboard/App/resources/report/words-display/` when the importer is run without an
  explicit `--report-out` directory. `resources/` is local and gitignored.

## Filename conventions

These reports now use the Feature 006+ **three-digit chronological prefix** convention per
`Backend/report/README.md`. The numeric order below reflects the report workflow (source binding →
audits → restructure analysis → phase reports); report **content** is unchanged. New human-authored
reports should append the next available number.

## Report index

| Report | Status | Summary |
| --- | --- | --- |
| [001-imlaei-clean-import-binding.md](./001-imlaei-clean-import-binding.md) | PASS | Enriches imlaei-simple source with clean identity key and binds through Feature 002 import |
| [002-word-import-source-normalization-audit.md](./002-word-import-source-normalization-audit.md) | PASS WITH ACTION REQUIRED | Audits where annotation marks enter the word pipeline; proposes normalized identity keys for display tables |
| [003-words-unique-tables-audit.md](./003-words-unique-tables-audit.md) | PASS WITH ACTION REQUIRED | Database audit of `quran_words_unique_tashkeel` and `quran_words_unique_simple` |
| [004-word-identity-links-restructure.md](./004-word-identity-links-restructure.md) | Recommendation | Analysis for unique-simple identity switch and per-occurrence identity links on `quran_words` |
| [005-unique-simple-identity-switch.md](./005-unique-simple-identity-switch.md) | PASS | Phase 1 — switches unique-simple grouping to `word_key_imlaei_simple` |
| [006-dev-reset-reseed.md](./006-dev-reset-reseed.md) | PASS | Phase 7 — dev reset, migrate, import, rebuild, and audit workflow |

> Status: Folder consolidated and filenames normalized to numeric prefixes (2026-06-14). Report
> content is unchanged.
