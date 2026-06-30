# Phase 0 Research: Word Types Explorer

**Feature**: 019 — Word Types Explorer (أنواع الكلمات)
**Date**: 2026-06-30
**Status**: All unknowns resolved. The spec carries **0 `[NEEDS CLARIFICATION]`**; the locked pre-spec plan pre-resolved the product decisions. This document records the technical decisions, their rationale, and the alternatives rejected, so `/speckit.tasks` and the implementing model have a single source of truth.

---

## R1 — Single data source for types, filters, and counts

- **Decision**: Derive every tree predicate, secondary filter, table column, and tree count from the **word-level** table `quran_word_morphology` (entity `WordMorphology`), joined to `quran_words` (marker filter + tashkeel identity) and `quran_pos_tags` (category + Arabic label). **No segment table** is read for this feature.
- **Rationale**: `WordMorphology` carries exactly the word-level columns this feature needs and they are indexed: `HeadPos` (FK, NOT NULL), `IsVerb`, `VerbTense`, `VerbVoice`, `CaseFeature`, plus `RootId`/`LemmaId`/`StemId`. The "main type" is by definition the word's head POS, so reading the head row avoids any segment join, keeps counts one-row-per-occurrence, and structurally guarantees that segment/prefix/suffix POS cannot leak into counts (spec FR-024, FR-044 sibling concern; risk R12).
- **Alternatives rejected**:
  - *Segment table (`quran_word_morphology_segments`)* — used by the Stems explorer, but it would multi-count per word and reintroduce the segment-vs-head ambiguity this feature explicitly excludes (spec Out of Scope).
  - *Pre-aggregated counts on `quran_words_unique_tashkeel`* — see R3; they are not type-scoped.

## R2 — Main-type classification (head POS → 4 buckets)

- **Decision**: Map head POS to the four main types as locked in pre-spec §3.2:
  - اسم = `pt.category = 'noun'` (parent); children by every noun-category `head_pos` in the catalogue (currently `N`/`PN`/`ADJ`/`PRON`/`REL`/`DEM`/`T`/`LOC`/`TIM`/`IMPN`).
  - فعل = `IsVerb = true` (≡ `head_pos = 'V'`); children by `VerbTense` (`past`/`present`/`imperative`), optional `VerbVoice`.
  - حرف وأداة = `pt.category = 'particle' AND head_pos <> 'INL'` (parent); optional children by specific particle code.
  - حروف مقطّعة = `head_pos = 'INL'` (leaf).
- **Rationale**: Categories partition POS codes cleanly; `IsVerb` is the indexed, authoritative verb flag. The **mandatory** `head_pos <> 'INL'` exclusion on the particle parent prevents INL double-counting under both حرف وأداة and حروف مقطّعة (spec FR-009; risk R8).
- **Alternatives rejected**: Deriving verb-ness from `head_pos = 'V'` text comparison instead of `IsVerb` — equivalent but less explicit; `IsVerb` is the indexed flag and is preferred.

## R3 — Counts are recomputed, never read pre-aggregated

- **Decision**: Compute both count families per request (behind the existing cache). Tree node counts = `COUNT(DISTINCT <row grouping key>)` per node predicate and remain unscoped by secondary filters in v1. Table column counts (المواضع/الآيات/السور) and table `totalCount` = occurrence/row aggregates scoped to each row's exact context and active secondary filters.
- **Rationale**: The pre-aggregated `occurrences_count`/`ayahs_count`/`surahs_count` on `quran_words_unique_tashkeel` span **all** usages of a word and are **not** type-scoped, so they cannot serve filter-scoped columns or tree counts (spec FR-024–028; pre-spec §6.3). Pages are small (≈25 rows) and the filtered columns are indexed, so recomputation is cheap. Keeping E1 static avoids adding secondary-filter query params to the tree contract.
- **Alternatives rejected**: Materializing a new type-scoped aggregate table — violates the read-only / no-migration gate (G-A) and is unnecessary at this scale.

## R4 — Child nodes (resolves G4)

- **Decision (v1)**:
  - Under اسم: return **all noun-category head POS codes from `quran_pos_tags`** ordered by `SortOrder` (currently `N`, `PN`, `ADJ`, `PRON`, `REL`, `DEM`, `T`, `LOC`, `TIM`, `IMPN`).
  - Under فعل: return ماض / مضارع / أمر using `VerbTense` (`past`/`present`/`imperative`).
  - حرف وأداة and حروف مقطّعة: parent/leaf select, **no** child particle breakdown in v1.
- **Rationale**: The final spec requires catalogue-defined nominal subtypes beyond the core four to be included because the POS catalogue already owns their Arabic labels. Making noun children catalogue-driven avoids hardcoded omissions while still keeping particle-code browsing out of v1. The four main-type buckets remain exhaustive regardless.
- **Alternatives rejected**:
  - Deferring `REL`/`DEM`/ظرف-like nominal children — conflicts with FR-011 and the spec Assumptions.
  - Shipping every particle-code child now — larger label/interaction surface and explicitly optional in the spec.

## R5 — v1 column set & root/lemma/stem enrichment (resolves G3)

- **Decision (v1)**: Columns الكلمة / النوع / الجذر / المواضع / الآيات / السور ship in v1. الأصل (lemma) and الصيغة (stem) columns are **included if the new lemma/stem winner queries are low-risk to mirror; otherwise deferred** — they are explicitly deferrable without affecting the row model or counts. الجذر reuses the existing root-winner query (`LoadPrimaryRootsAsync`).
- **Rationale**: Root winner logic already exists and is reuse-ready; lemma/stem winners are new but mechanically identical (mirror the root query). Because a row is already pinned to one head-POS (and feature) context, root/lemma/stem are usually constant within the row; where they vary, take the dominant (winner) value among the row's occurrences (pre-spec §6.4). Marking لemma/stem deferrable lets scope shrink without touching the locked parts.
- **Alternatives rejected**: Blocking v1 on lemma/stem columns — unnecessary coupling; the spec lists them as deferrable (Assumptions).

## R6 — Row identity must be addressable across endpoints (`contextCode`)

- **Decision**: Each row carries an explicit, addressable **`contextCode`** capturing the dimension(s) not yet pinned by the active filter (the row's own `head_pos` under a parent node, or `tense`/`voice` under the verb branch). `contextCode` + `tashkeelWordId` + active `case`/`tense`/`voice` form the row key threaded through the summary (E3), ayahs (E4), and surahs (E5) endpoints and the deep-link URL.
- **Rationale**: A row is a *word + context*, not a word. Without an addressable context, E3–E5 and deep links would re-collapse a multi-usage word into one row and show the wrong (union) occurrences (spec FR-018a/018b; risk R14). This is the mechanism that makes the "no mixed rows" rule reproducible end-to-end.
- **Alternatives rejected**: Re-deriving the row from `tashkeelWordId` alone — ambiguous for homographs that span subtypes/types; would reintroduce mixed rows.

## R7 — Reuse surface (backend + frontend)

- **Decision**: Implement a **separate** `WordTypes` area at every backend layer (controller → `CachedWordTypesReader` → `EfWordTypesReader`), mirroring Roots/Lemmas/Stems; reuse **query shapes**, caching/logging patterns, and frontend `explorer-table-*` utilities + `ayah-matches-list` / `surah-occurrences-list` / `missing-surahs-list` / `highlighted-ayah` / `word-count-chip` components and the existing per-word analysis endpoint `GET api/mushaf/words/{location}/analysis`.
- **Rationale**: Four explorers already prove the split-view + cache + url-sync pattern; cloning the shape minimizes risk and review surface (G-C DRY). Keeping a separate read path (not overloading Unique-Words endpoints) protects the existing Unique-Words contract and cache (risk R11).
- **Alternatives rejected**: Extending `IUniqueWordsReader` / Unique-Words endpoints — couples a different count model into a stable contract; rejected.

## R8 — Display identity & label sourcing

- **Decision**: Display words in **Uthmani with tashkeel** using the existing `quran_words_unique_tashkeel` identity (`text_uthmani`); **no Simple/Tashkeel toggle** in v1. POS **type labels** come from `quran_pos_tags.arabic_label` (API-sourced). Only the four main-type display strings and the secondary-filter option strings are static UI labels (TDZ-safe getters).
- **Rationale**: The same unvowelled form can carry different word-type usages, so an unvowelled display would be misleading on this page (pre-spec §5.1). API-sourced POS labels avoid drift between DB and UI and keep the one corrected label (`PRO`) authoritative once reseeded.
- **Alternatives rejected**: Hardcoding Arabic POS labels in the frontend — drifts from the catalogue; rejected by the label-sourcing rule.

## R9 — `PRO` label data gate (informs G1)

- **Decision**: Before the first implementation task, verify the live `quran_pos_tags` row `PRO = حرف نهي / particle`. If stale (`ضمير منفصل` / `noun`), force a morphology reseed or apply a one-off `UPDATE`.
- **Rationale**: The seed (`PosTagSeed.cs`) is corrected, but the live DB may be stale; if so, the prohibitive particle *لا* (`PRO`) mis-buckets under اسم, breaking type correctness (spec FR-044; risk R1). The seed→`quran_pos_tags` chain reseeds only via a forced morphology import that truncates and recopies six tables.
- **Alternatives rejected**: Assuming the live DB matches the seed — unverified; the gate is a 1-query check, cheap insurance.

## R10 — Frontend route and empty-query default

- **Decision**: Add the page at `/dashboard/words/types` and normalize an empty/invalid `type` query param to `noun`.
- **Rationale**: `types` is concise and consistent with existing Words child routes (`roots`, `lemmas`, `stems`) while the backend route remains explicit at `api/words/word-types`. Defaulting to `noun` gives admins an immediately useful table and satisfies the spec's allowed "sensible default type selected" state without adding a separate no-selection branch.
- **Alternatives rejected**:
  - `/dashboard/words/word-types` — explicit but redundant inside the Words area.
  - No selected type prompt — valid under the spec, but it creates an extra empty state and delays the primary browse flow.

---

## Resolved unknowns summary

| # | Question | Resolution |
|---|----------|------------|
| R1 | Which table backs types/filters/counts? | `quran_word_morphology` (word-level), no segment join. |
| R2 | How are the 4 main types classified? | category + `IsVerb` + `head_pos`; particle parent excludes `INL`. |
| R3 | Use pre-aggregated counts? | No — recompute scoped table counts; E1 tree counts stay unscoped by secondary filters. |
| R4 | Which child nodes in v1? | All noun-category POS children; verb tense children; particle-code children deferred. |
| R5 | الأصل/الصيغة columns in v1? | Root yes (reuse); lemma/stem deferrable (new winner queries). |
| R6 | How is a row addressed across endpoints? | Explicit `contextCode` + `tashkeelWordId` + active feature. |
| R7 | Reuse vs new? | Separate `WordTypes` area; reuse shapes/utilities/components. |
| R8 | Display + labels? | Uthmani+tashkeel; POS labels API-sourced; no toggle. |
| R9 | Data gate? | Verify `PRO` POS row before implementation (G1). |
| R10 | Route and empty-query default? | `/dashboard/words/types`; missing/invalid `type` defaults to `noun`. |

**No open clarifications remain.** Proceed to Phase 1 artifacts (already generated: data-model.md, contracts/, quickstart.md).
