# Backend Planning & Design Docs

This folder holds **planning and design documents** for the Quran Dashboard / المنهج القرآني
backend: foundation plans, design documents, and implementation plans (the *intended HOW*,
authored before or alongside Spec Kit specs). Post-work **audits/verification reports** live
separately under [`Backend/report/`](../Backend/report/README.md).

Documents are grouped into one subfolder per feature/scope.

## Layout

| Folder | Scope |
| --- | --- |
| `feature-001-layout-foundation/` | Phase 0 layout & foundation plan (before the first real Quran data feature). |
| `feature-002-quran-foundation/` | Quran Mushaf Words & Layout Data Foundation import plan (`002-mushaf-words-foundation`). |
| `feature-003-word-display-tables/` | Quran Words Display Tables Foundation plan (`003-words-display-tables`). |
| `feature-003-word-identity-links/` | Word identity links restructure — implementation plan + dev reset/reseed quickstart (unique-simple by clean imlaei key + `quran_words` link columns). |

## Contents

- `feature-001-layout-foundation/`
  - `manhaj-qurani-layout-foundation-plan.md`
- `feature-002-quran-foundation/`
  - `manhaj-qurani-mushaf-words-layout-data-foundation-plan.md`
- `feature-003-word-display-tables/`
  - `manhaj-qurani-quran-words-display-tables-foundation-plan.md`
- `feature-003-word-identity-links/`
  - `feature-003-word-identity-links-implementation-plan.md`
  - `quickstart.md` — dev reset → migrate → import → rebuild → verify
