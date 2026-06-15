# Feature 006 — Source Safety Check

**Feature:** 006 Quran Mutashabihat Foundation
**Date:** 2026-06-14
**Method:** Inspection of the import pipeline + the recorded generated report’s source checks.

## Verdict: PASS — sources are read-only and Quran-safe

| Check | Evidence | Result |
| --- | --- | --- |
| Source files unchanged by the run | `MUT-SOURCE-UNCHANGED` hard check (size + sha256 before/after) | **PASS — unchanged** |
| Staged set matches manifest | `MUT-MANIFEST-SET` + `MUT-MANIFEST-CHECKSUM` | **PASS** |
| Reads only the staged package | Importer reads `resources/import-sources/mutashabihat/` (`manifest.json`, `mutashabihat-ul-quran/phrases.json`, `similar-ayahs/matching-ayah.json`) | **PASS** |
| No Quranic text invented | Verse references validated against `^\d+:\d+$` and resolved to existing `quran_ayahs` (`MUT-VERSEKEY-FORMAT`, `MUT-AYAH-RESOLVE`) | **PASS** |
| Foundation tables untouched | `tasks.md` invariant: never modify `quran_ayahs`, `quran_words`, the Quran text, or source files | **Honored** |
| Quranic test data source-safe | `tasks.md` note; test fixtures use fabricated keys, not real Quranic content as fixtures | **Honored** |

No secrets are printed in this report. The importer treats the staged package as read-only
provenance and persists only into the dedicated `quran_mutashabihat_*` / `quran_similar_ayah_links`
tables.
</content>
