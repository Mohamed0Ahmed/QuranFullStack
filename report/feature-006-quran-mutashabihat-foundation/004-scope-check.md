# Feature 006 — Scope Check

**Feature:** 006 Quran Mutashabihat Foundation
**Date:** 2026-06-14
**Method:** Read-only inspection of `Backend/api/`, `Frontend/quran-dashboard-ui/src/`, and the
DataImporter.

## Verdict: PASS — no out-of-scope additions

Feature 006 is a **backend data foundation only** (per its spec/plan). Confirmed boundaries:

| Surface | Check | Result |
| --- | --- | --- |
| Backend API (`Backend/api/`) | Mutashabihat controllers / endpoints / startup seeding | **None** |
| Frontend (`Frontend/quran-dashboard-ui/src/`) | Feature 006 pages/components/services | **None added** |
| Frontend nav | `core/navigation/nav-items.ts` references mutashabihat | **Pre-existing app-shell placeholder only** (same pattern as Feature 007’s `/tafsirs` route) — not part of this feature’s implementation |
| Search / public reader | Indexing or reader code for mutashabihat | **None** |
| DataImporter | `import-mutashabihat` verb | **In scope** |

All Feature 006 code lives under `Backend/{domain,application,application.abstractions,infrastructure,
tests}/Quran/Mutashabihat/` and the one DataImporter verb. No API surface, no frontend feature, no
search/seeding/public-reader code was introduced.
</content>
