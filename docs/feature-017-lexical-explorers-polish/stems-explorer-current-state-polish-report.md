# Stems Explorer — Current-State & Implementation-Readiness Report

**Feature:** 017-lexical-explorers-polish
**Page:** `/dashboard/words/stems` (الأصول الصرفية)
**Scope:** Report-only. No code changed, no commits, no migrations/importers, no DB writes.
**Sibling reference:** Lemmas Explorer `/dashboard/words/lemmas` (just completed with segment-matched semantics).

---

## 1. Verdict

**READY_WITH_BACKEND_NOTES**

Implementation can proceed. The work is **not frontend-only** — it requires a small, safe set of backend changes plus frontend changes. Crucially, Stems must **not** be "segment-matched" the way Lemmas was: the segment table has no `stem_id`, and the existing word-level (head-stem) model is internally consistent and scientifically defensible. **No migration or importer is required or justified by this report.**

One scope decision is needed (see §9): whether to ship the Lemmas-style click-to-filter type chips for Stems now, or defer. Everything else (scroll fix, tashkeel projection fix, type-distribution placement) is unambiguous.

---

## 2. Executive Summary

| Area | Current state | Fix needed | Layer | Priority |
|---|---|---|---|---|
| Words-tab scroll | **Broken** — viewport clipped, never scrolls | Add Stems selectors to the shared scroll mixin | Frontend (SCSS) | **Required** |
| "بدون تشكيل" view | **Wrong** — displays tashkeel text (`TextUthmani`) | Project `TextUthmaniSimple` in the simple branch | **Backend** | **Required** |
| Type distribution placement | Shown on **every** tab | Gate to Ayahs tab only | Frontend | **Required** |
| Type distribution as a filter | **Read-only** display, no click, no "عرض الكل" | Convert to clickable filter (chip component) + backend `typeCode` | Backend + Frontend | Required for parity (decision) |
| "عرض الكل" single-type rule | Not present (no filter exists) | Comes with the filter component | Frontend | Required for parity |
| Headers wording | **Already unified** via `words-shared.labels.ts` | None | — | Done |
| Word-type correctness | Correct **for the head-stem model** (see §4) | None (do NOT add `stem_id` to segments) | — | Deferred/none |

**Bottom line:** Stems needs **backend + frontend** fixes, but the backend changes are bounded and require **no schema change**. Word types are *not* wrong in the way Lemmas' were — they are consistent at the word/head-stem level.

---

## 3. Current Behavior

Evidence: `stems-explorer-page.component.html`, `stem-details-panel.component.{ts,html,scss}`, `stem-words-list.component.{ts,html,scss}`, `stems-detail.facade.ts`.

### Details panel tabs
Four tabs, no overview tab: **الكلمات / الآيات / السور / الصيغ المعجمية** (`STEM_VIEW_KEYS = ['words','ayahs','surahs','lemmas']`). Tab chrome is `StemDetailsPanelComponent`; content is projected via `<ng-content>` from the page's `#stemsPanelContent` template. Inline panel on desktop, modal drawer below desktop breakpoint. This mirrors Lemmas structurally.

### Type distribution / word-type area
- Rendered by the **shared read-only** `qd-type-distribution-list` component.
- In `stems-explorer-page.component.html` it sits **outside** any `activeView()` guard:
  ```
  @if (panelState().summary || panelState().status === 'loading') {
    <qd-type-distribution-list [items]="panelState().summary?.typeDistribution ?? []" ... />
  }
  ```
  → It appears on **all four tabs** (الكلمات، الآيات، السور، الصيغ المعجمية), not just Ayahs.
- It is **not interactive**: no click handler, no selected state, no "عرض الكل" option, no filtering. It only shows each POS with the dominant row highlighted (`row.dominant = index === 0`).

This is the *old* Lemmas behavior — the exact thing that was removed from Lemmas during this feature.

### Words tab behavior
- Sub-tabs (بدون تشكيل / بالتشكيل) live in the **page** template (`stems-explorer__sub-tabs` nav), wired to `onWordViewChange()` → `detailFacade.setWordView()` → reloads with `wordView` + resets `detailPage` to 1. The toggle itself works and is reflected in the URL (`wordView` param).
- Rows are rendered by `qd-stem-words-list` (header + viewport + pagination).
- **Display bug:** both word views show identical (with-tashkeel) text — this is a backend projection issue (see §4), not a frontend one.

### Ayahs tab behavior
- Uses the **shared** `qd-ayah-matches-list` component. `StemAyahMatchDto extends AyahMatchDto` (`{ matchedQuranWordIds, words }`), so the backend shape already matches the shared component directly (no mapper needed, unlike Lemmas).
- Highlighting is driven by `MatchedQuranWordIds` (word-level matches). There is **no type filter** on this tab.

### Scroll behavior inside details tabs
- **Ayahs / السور tabs scroll correctly** (their list components are wired into the shared scroll mixin).
- **Words tab does NOT scroll** — the viewport is clipped. **الصيغ المعجمية (lemmas) tab has the same gap.** Root cause in §5.

### Headers wording
**Already concise and unified.** `stems.labels.ts` pulls every header from `words-shared.labels.ts` (`WORDS_SHARED_HEADERS`, `WORDS_SHARED_LIST_HEADERS`, `WORDS_SHARED_PANEL_TABS`, `WORDS_SHARED_WORD_VIEWS`), the same source Lemmas uses. Recent commits ("unified Words list headers") already landed this. **No header changes required for Stems.**

---

## 4. Backend Findings

Evidence: `EfStemsReader.cs`, `EfStemsReader.Summary.cs`, `StemsListDerivation.cs`, `StemsSummaryRow.cs`, `StemsController.cs`, `IStemsReader.cs`, Stems DTOs; compared against `EfLemmasReader.cs`. Domain: `QuranWord.cs` (`TextUthmani`, `TextUthmaniSimple`, `UniqueSimpleWordId`, `UniqueTashkeelWordId`), `WordMorphology.cs` (`HeadPos`, `StemId`).

### How Stems Explorer currently derives each thing

| Concern | Source | Granularity |
|---|---|---|
| Type distribution | `EfStemsReader.Summary.cs` — joins `quran_word_morphology m` → `PosTags` on **`m.HeadPos`** | **Word-level head POS** |
| Ayah matches | `GetStemAyahMatchesAsync` — `WordMorphologies.Where(m => m.StemId == id)` → word→ayah | **Word-level (head stem)** |
| Type filter | **None** — `GetStemAyahMatchesAsync` has no `typeCode` parameter | n/a |
| Words-tab rows | `LoadStemWordRowsAsync` — `m.StemId == id`, grouped by `UniqueSimpleWordId`/`UniqueTashkeelWordId` | **Word-level (head stem)** |
| Summary counts | `EfStemsReader.Summary.cs` SQL — `quran_word_morphology m JOIN quran_words w WHERE m.stem_id IS NOT NULL GROUP BY m.stem_id` | **Word-level (head stem)** |
| Catalogue counts | Same whole-summary aggregation → `StemsListDerivation.ToPage` | **Word-level (head stem)** |

So **every** Stems read is anchored on `quran_word_morphology.stem_id` (one head stem per word) and `quran_word_morphology.head_pos`. No segment-level data is used anywhere in Stems.

### Does Stems use segment-level data or shared Lemmas readers?
- `quran_word_morphology.stem_id`: **Yes** — the sole stem linkage everywhere.
- `quran_word_morphology.head_pos`: **Yes** — the sole POS source for the type distribution.
- Segment-level data (`quran_word_morphology_segments`): **No.**
- Shared Lemmas components/services/readers: **No.** `EfStemsReader` is independent of `EfLemmasReader`. It only shares small helpers (`ReadPaging`, `MorphologyRelatedItemsOrdering`, the `AyahWordForHighlightDto`/`PosTags` tables). The Lemmas segment-matched fix did **not** touch Stems.

### Are Stems word types currently correct?
**Yes — for the head-stem model.** The set of words matched to a stem is "words whose **head** stem = this stem" (`m.StemId == id`). The POS shown for that stem is `m.HeadPos`, i.e. the POS of that same head morphology. So the displayed/filterable type always belongs to the stem that defines the match. There is **no** head-vs-segment mismatch of the kind Lemmas had.

### Are there multi-STEM cases where word-level `head_pos` misrepresents the *selected* stem?
**No — not for the selected stem.** The Lemmas bug (e.g. `أَلَّا = أن + لا`) occurred because a lemma could be matched as a **non-head segment**, while `head_pos` described a *different* segment. Stems cannot reproduce this: `quran_word_morphology` stores only the **head** stem, so a stem is only ever matched as the head of a word, and `head_pos` is exactly that head's POS — always consistent.

There is a **coverage limitation** (different from a correctness bug): in a compound/multi-segment word with more than one STEM segment, only the **head** stem is recorded. Secondary stem segments are invisible to the Stems Explorer, and a stem's occurrence counts only count words it heads. This is a data-model boundary, not a wrong-type defect, and it should be stated honestly in any product copy — but it does not make the displayed types *incorrect*.

### Is there a reliable segment-level way to match stems without adding `stem_id` to segments?
**No.**
- `quran_word_morphology_segments` carries `lemma_id`, `root_id`, `pos`, `quran_word_id` — but **no `stem_id`** (confirmed by the data-id work that added only `lemma_id`/`root_id`, and by the explicit comment in `EfLemmasReader.GetLemmaStemsAsync`: *"Segment rows do not carry stem_id; related stems come from the word-level morphology row"*).
- There is no FK path from a segment to a stem. Text-matching `segment.text` against `quran_stems.stem_text` is unreliable (normalization/diacritics/ambiguity) and is **not** recommended.
- Therefore no trustworthy segment-level stem matching exists today.

### Should Stems remain word-level for now?
**Yes.** Word-level (head-stem) matching is consistent and correct within the recorded data. Mirroring the Lemmas "segment-matched" approach is **not possible** without schema change and is **not warranted**.

### Would fixing Stems "semantics" require a migration/importer?
- To **surface secondary stems / count every stem occurrence at segment granularity**: **yes** — it would require adding `stem_id` to `quran_word_morphology_segments` and populating it via an importer/migration. **This report does not prove that necessary, so it must be deferred** (constitution: no migrations unless proven necessary).
- For everything the current polish needs (correct simple-text display, type filter, placement, scroll): **no schema change** — solvable safely word-level.

### The one real backend bug: "بدون تشكيل" shows tashkeel text
`EfStemsReader.LoadStemWordRowsAsync` projects `w.TextUthmani` in **both** branches:
```
// Simple branch:   new StemWordOccurrenceRow(w.UniqueSimpleWordId,  w.TextUthmani, ...)
// Tashkeel branch: new StemWordOccurrenceRow(w.UniqueTashkeelWordId, w.TextUthmani, ...)
```
The grouping key differs (simple vs tashkeel unique id) but the **display text is `TextUthmani` (with tashkeel) in both** → "بدون تشكيل" renders with tashkeel.

This is the same defect Lemmas already fixed. `EfLemmasReader.LoadLemmaWordRowsAsync` uses `w.TextUthmaniSimple` for the simple branch and `w.TextUthmani` for tashkeel. `QuranWord.TextUthmaniSimple` exists in the domain. **Fix = change the Stems simple branch projection to `w.TextUthmaniSimple`.** Low risk, isolated to one method.

---

## 5. Frontend Findings

### 5.1 Words-tab scroll — exact cause
The detail panel uses a flex-column scroll chain:

1. `.explorer-detail-panel` (host card): `display:flex; flex-direction:column; height:100%; min-height:0` (`_components.scss:198`).
2. `.explorer-detail-panel__body` (the tab surface): global default is `overflow:auto` (`_components.scss:241–249`), **but** the component SCSS overrides it:
   `stem-details-panel.component.scss` → `:host .explorer-detail-panel__body { flex:1 1 auto; min-block-size:0; overflow: hidden; }` (identical override in `lemma-details-panel.component.scss`). So the body is a **non-scrolling, fixed-height** flex container; the **inner list viewport** is meant to be the scroller.
3. The inner scroller is wired in the shared **`src/styles/_explorer-detail-lists.scss`** by **enumerating component selectors**:
   ```
   .explorer-detail-panel__body, .explorer-detail-modal {
     qd-ayah-matches-list, qd-missing-surahs-list, qd-surah-occurrences-list,
     qd-root-words-list, qd-root-lemmas-list, qd-root-stems-list,
     qd-lemma-words-list { display:flex; flex-direction:column; flex:1; min-height:0; height:100%; }
   }
   .explorer-detail-panel__body {
     .ayah-matches-list, .root-words-list, .lemma-words-list { flex:1; min-height:0; height:100%; }
     .ayah-matches-list__viewport, .root-words-list__viewport, .lemma-words-list__viewport {
       flex:1; min-height:0; overflow:auto; scrollbar-gutter:stable;
     }
   }
   ```

**The Stems list selectors are missing from this file.** `qd-stem-words-list`, `.stem-words-list`, and `.stem-words-list__viewport` are **not** in the flex/height/overflow groups (they appear only in the border-color block at the bottom). Likewise `qd-stem-lemmas-list` / `.stem-lemmas-list`.

Result for the Words tab: `qd-stem-words-list` host stays `display:block` (its own `:host { display:block }`), `.stem-words-list__viewport` never receives `overflow:auto`, and the parent body is `overflow:hidden` with a fixed height → **content is clipped, nothing scrolls.** The Ayahs/Surahs tabs scroll only because their components (`qd-ayah-matches-list`, `qd-surah-occurrences-list`, `qd-missing-surahs-list`) *are* enumerated.

**Safest fix:** purely additive — append the Stems selectors alongside the existing Lemma/Root ones in `_explorer-detail-lists.scss`:
- to the flex/height group: `qd-stem-words-list` (and `qd-stem-lemmas-list`)
- to the `.stem-words-list { flex:1; min-height:0; height:100% }` group
- to the viewport-overflow group: `.stem-words-list__viewport` (and the `.stem-lemmas-list-shell` / `.stem-lemmas-list` pattern used by the related-lists)
- ensure `.stem-words-list__header` is in the `flex-shrink:0` header group (it is already listed in the border-color group; add it to the layout group).

The `stem-words-list` markup already matches the lemma structure (`.stem-words-list` > `.stem-words-list__header` + `.stem-words-list__viewport` + `qd-pagination`), so the existing rules will "just work" once the selectors include it. **No token changes, no risk to Lemmas/Roots** (additive selector list).

> Note: `_explorer-detail-lists.scss` is a shared stylesheet, not a design-token file. The change must be **additive** (add stem selectors to existing selector groups); do not restructure the groups.

### 5.2 Type distribution placement & interactivity
- **Placement:** currently unconditional (all tabs). Lemmas gates it to Ayahs only:
  `@if (activeView() === 'ayahs' && (summary || loading)) { <qd-lemma-ayah-type-filters ... /> }`.
- **Interactivity:** Stems uses read-only `qd-type-distribution-list`; Lemmas uses `qd-lemma-ayah-type-filters` (clickable chips, selected state, emits `typeCodeChange`).
- **"عرض الكل" rule:** Lemmas shows the "all" chip only when `items().length > 1` (`lemma-ayah-type-filters.component.html`) and treats a single type as implicitly selected (`isSelected()`), so a stem with one POS shows no "عرض الكل" — exactly the requested rule. Stems has none of this yet.

### 5.3 State / URL
- `StemsPanelState` has **no `ayahTypeCode`** field. `STEMS_QUERY_KEYS` has **no `typeCode`**. `stems-url-sync.ts` neither parses nor writes a type code. `stems.api.ts.getStemAyahMatches` sends only `page`/`pageSize`.
- Lemmas added all of these (`ayahTypeCode` in panel state, `typeCode` query key, URL sync that drops the param for "عرض الكل" and clears it when leaving the Ayahs tab / changing selection, and `onAyahTypeChange` that resets `detailPage` to 1).
- So enabling the filter on Stems requires the same wiring (see plan §7).

### 5.4 Minor frontend observations (not blockers)
- The shared `qd-type-distribution-list` imports its labels from `stems.labels.ts` (`STEMS_TYPE_DISTRIBUTION_*`). If Stems stops using the read-only list, confirm whether any other page (e.g. Roots) still renders it before considering removal — likely keep it.
- Stems ayah word projection includes ayah markers (`GetStemAyahMatchesAsync` does not filter `IsAyahMarker`), whereas Lemmas filters them out. The shared `qd-ayah-matches-list` tolerates markers; treat as cosmetic parity, not a bug.

---

## 6. Lemmas Comparison

| Dimension | Lemmas (final) | Stems (current) | Reuse for Stems? |
|---|---|---|---|
| Type-distribution component | `qd-lemma-ayah-type-filters` (clickable, "عرض الكل", selected state) | `qd-type-distribution-list` (read-only) | **Reuse the pattern** (generalize the component or add a thin Stems sibling). Do not reuse `lemma-ayah-type-filters` as-is — it imports `lemmas.labels`. |
| "عرض الكل" visibility | `@if (items().length > 1)` | absent | **Copy the rule** verbatim |
| Click-to-filter | `typeCodeChange` → reset detailPage=1, server filter | none | **Copy the behavior** |
| URL state | `typeCode` param; dropped for "all"; cleared on tab-leave/selection-change | no type code in URL | **Copy the wiring** into `stems-url-sync.ts` + `STEMS_QUERY_KEYS` |
| Detail tab behavior / shell | Identical 4-tab shell | Identical 4-tab shell | already aligned |
| Word-rows projection | simple→`TextUthmaniSimple`, tashkeel→`TextUthmani` | both→`TextUthmani` | **Copy the simple-branch projection** |
| Scroll CSS/layout | `qd-lemma-words-list` selectors present in shared mixin | Stems selectors **absent** | **Add Stems selectors** to the mixin |
| **Ayah matching semantics** | **segment-matched** (`segment.lemma_id` + `segment.pos`) | **word-level head stem** (`m.stem_id` + `m.head_pos`) | **DO NOT copy.** Stems has no `segment.stem_id`; word-level is correct for stems. |
| Type filter backend predicate | `s.Pos == typeCode` (segment POS) | would be `m.HeadPos == typeCode` (head POS) | **Adapt, not copy** — filter on `head_pos`, not segment pos |

**Must NOT be copied:** the segment-matched query strategy and any assumption that a `stem_id` exists on segments. Stems stays word-level.

---

## 7. Required Implementation Plan

### Backend fixes
1. **[REQUIRED] Fix "بدون تشكيل" projection.** In `EfStemsReader.LoadStemWordRowsAsync`, the simple branch must select `w.TextUthmaniSimple` (keep tashkeel branch on `w.TextUthmani`). Mirrors the Lemmas fix. Isolated, no contract change.
2. **[REQUIRED-FOR-PARITY] Add `typeCode` to stem ayah matches** *(only if the click-to-filter is in scope — see §9 decision).*
   - `IStemsReader.GetStemAyahMatchesAsync(... , string? typeCode, ...)`.
   - In `EfStemsReader`, filter the matched set on word-level head POS: `m.StemId == id && (typeCode == null || m.HeadPos == typeCode)` (both the `matchedAyahIds` and the per-ayah `matchedRows`). **Do not** introduce segment joins.
   - Thread `typeCode` through `GetStemAyahsQuery` / `GetStemAyahsHandler` / `StemsController.GetAyahs` (`[FromQuery] string? typeCode`), param name **`typeCode`** (not `type`/`pos`), optional, null/empty = "عرض الكل". No DTO/`ApiResponse` shape change.
   - Cache: extend the Stems ayah cache key to include `typeCode` (see `StemsCacheKeys`).
3. **[DEFERRED] Segment-level `stem_id`.** Not required, not justified here. Adding it (schema + importer) would be the only path to "true" segment-matched stems; defer until/unless a product need is proven.

### Frontend fixes
1. **[REQUIRED] Words-tab (and lemmas-tab) scroll.** Additive selectors in `src/styles/_explorer-detail-lists.scss` for `qd-stem-words-list` / `.stem-words-list` / `.stem-words-list__viewport` / `.stem-words-list__header` (and the `qd-stem-lemmas-list` / `.stem-lemmas-list` related-list group). Pure layout, no tokens.
2. **[REQUIRED] Type-distribution placement.** Gate the panel's type block to `activeView() === 'ayahs'` only (remove the unconditional render).
3. **[REQUIRED-FOR-PARITY] Click-to-filter chips in the Ayahs tab.**
   - Component: generalize `lemma-ayah-type-filters` into a shared `ayah-type-filters` (label via input) **or** add a thin `stem-ayah-type-filters` sibling that imports `stems.labels`. Keep the "عرض الكل only when `items().length > 1`" rule and single-type implicit selection.
   - State: add `ayahTypeCode: string | null` to `StemsPanelState`; on type click → set code, reset `detailPage` to 1, reload ayahs with `typeCode`.
   - URL: add `typeCode` to `STEMS_QUERY_KEYS`; in `stems-url-sync.ts` write it only when a type is selected, omit for "all", and clear it when leaving the Ayahs tab, clearing the selection, or when the summary's distribution no longer contains the code (mirror the Lemmas rules).
   - API: add optional `typeCode` to `StemsApi.getStemAyahMatches`.
   - Behavior to match Lemmas exactly: switch/focus Ayahs tab if needed, apply filter, reset to page 1, show active state, keep the rest of the page usable.

### Tests to add/update
- **Backend:** `EfStemsReader` words test asserting the simple view returns text **without** tashkeel and the tashkeel view **with** tashkeel (use the existing real-infrastructure pattern; do not mock the DbContext). If the filter ships: ayah-match tests for `typeCode == null` (all), a valid code (subset), and an absent code (empty page), plus a count-integrity assertion (Σ per-type ≤ total occurrences).
- **Frontend:** stems-url-sync spec for the new `typeCode` param (present/omitted/cleared transitions); stems-detail.facade spec for "type click resets detailPage to 1" and "leaving Ayahs clears typeCode"; a scroll/layout smoke check that `qd-stem-words-list` host gets the flex/overflow contract (or a visual/spec assertion if feasible under the Vitest builder). Respect the `VITEST_MAX_FORKS` worker cap and jsdom observer guards already recorded for this repo.
- Run only the changed Stems specs to keep the worker cap safe.

### Risks / decisions needed
- **Scope decision:** ship the click-to-filter parity now vs defer (see §9).
- Generalizing `lemma-ayah-type-filters` vs adding a Stems sibling — pick one without regressing Lemmas.
- Confirm `qd-type-distribution-list` is not orphaned elsewhere before removing its Stems usage.

---

## 8. Test Plan

| # | Test | Type | Asserts | Required? |
|---|---|---|---|---|
| T1 | Stem words simple view text | Backend (real DB) | simple branch → `TextUthmaniSimple` (no tashkeel); tashkeel branch → `TextUthmani` | **Yes** |
| T2 | Stem words grouping/counts unchanged | Backend | simple groups by `UniqueSimpleWordId`, tashkeel by `UniqueTashkeelWordId`; occurrence counts intact | Yes |
| T3 | Ayah filter — no typeCode | Backend | returns all matched ayahs (= current behavior) | If filter ships |
| T4 | Ayah filter — valid head-POS code | Backend | subset filtered by `m.HeadPos`; matched word ids respect filter | If filter ships |
| T5 | Ayah filter — code absent from stem | Backend | empty page, not an error | If filter ships |
| T6 | Count integrity | Backend | Σ(type occurrences) ≤ total stem occurrences; type set ⊆ distribution | If filter ships |
| T7 | URL `typeCode` lifecycle | Frontend (url-sync spec) | written when selected, omitted for "all", cleared on tab-leave/selection-clear | If filter ships |
| T8 | Type click resets detailPage | Frontend (facade spec) | selecting a type sets `detailPage=1` and reloads ayahs | If filter ships |
| T9 | "عرض الكل" hidden on single type | Frontend (component spec) | only one type ⇒ no "all" chip, that type implicitly selected | If filter ships |
| T10 | Words-tab scroll contract | Frontend | `qd-stem-words-list` host fills height; viewport scrollable | **Yes** |

---

## 9. Risks & Open Decisions

1. **DECISION — Click-to-filter scope.** The tashkeel fix, scroll fix, and Ayahs-only placement are unambiguous and should ship. The Lemmas-style **clickable type filter** is the larger piece (backend `typeCode` + frontend state/URL/component). It is safe and needs **no migration**, but it is a parity feature, not a bug fix. *Recommendation: include it (it is the headline "behave like Lemmas" requirement), implemented strictly word-level on `head_pos`.*
2. **Semantic honesty (low risk).** Stems are head-stem-only; secondary stems in compounds are not surfaced. This is a known data-model boundary, not a defect. Keep product copy accurate; do **not** attempt to "fix" it via text matching.
3. **Shared stylesheet edit.** The scroll fix touches a shared file (`_explorer-detail-lists.scss`). Keep it additive to avoid Lemmas/Roots regressions.
4. **Component reuse.** Generalizing `lemma-ayah-type-filters` risks touching Lemmas; a Stems sibling avoids that at the cost of minor duplication. Either is acceptable; choose to minimize Lemmas blast radius.
5. **Cache key.** If `typeCode` is added, the Stems ayah cache key must include it or cached unfiltered results will leak into filtered views.

---

## 10. Final Recommendation

Proceed under **READY_WITH_BACKEND_NOTES**. This is a **backend + frontend** effort, but bounded and migration-free:

- **Ship now (clear):**
  1. Backend: project `TextUthmaniSimple` for the "بدون تشكيل" view.
  2. Frontend: add Stems selectors to `_explorer-detail-lists.scss` to restore Words-tab (and lemmas-tab) scroll.
  3. Frontend: gate the type block to the Ayahs tab only.
- **Ship now for parity (recommended, one decision):** the Lemmas-style clickable type filter — backend optional `typeCode` filtering on **`m.HeadPos`** + frontend state/URL/chip component with the "عرض الكل only when >1 type" rule and detail-page reset.
- **Do NOT do:** add `stem_id` to `quran_word_morphology_segments`, run any importer/migration, copy the segment-matched query strategy, change global design tokens, or alter routes/`ApiResponse` shape.

Stems word types are **already correct** for the head-stem model — the Lemmas-style "segment-matched" correctness work does **not** apply and must not be replicated here.

### Safety constraints honored
No Quran text mutation · no importers · no migrations (none proven necessary) · no global design-token changes · additive shared-SCSS only · no Lemmas/Roots regressions · no route changes.
