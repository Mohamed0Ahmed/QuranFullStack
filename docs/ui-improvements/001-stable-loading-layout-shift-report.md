# UI-001 — Stable Loading & No Layout Shift for Mushaf Selection Panels

- **Status:** Reported
- **Area:** Mushaf Reader / Study Panels
- **Date reported:** 2026-06-20
- **Scope:** Frontend UI / state only (see Guardrails)

## Context

When the user selects a word in the Mushaf UI, the selected-word card briefly shrinks
or changes size while new word data loads, then expands again once the data arrives.
This produces a visible layout shift (a "jump").

A related issue happens in the selected-ayah / study area:

- If the newly selected word belongs to the **same ayah**, the ayah panel should not
  reload or resize.
- If the newly selected word belongs to a **different ayah**, the ayah panel may load
  new data, but its outer size and layout must remain stable. It should show a
  skeleton / shimmer / wave loading state instead of collapsing and then expanding.

This report documents the current implementation, the root causes, the desired UX, a
proposed state model, and a phased implementation plan. **No code is changed by this
report.**

### Components inspected

| Concern                          | File                                                                                             |
| -------------------------------- | ------------------------------------------------------------------------------------------------ |
| Page-state facade                | `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.ts`               |
| URL → state hydration            | `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-url-hydration.ts`               |
| Study panel wrapper              | `.../components/study-context-section/study-context-section.component.{ts,html,scss}`             |
| Selected word card               | `.../components/selected-word-section/selected-word-section.component.{ts,html,scss}`             |
| Selected ayah / study card       | `.../components/selected-ayah-section/selected-ayah-section.component.{ts,html,scss}`             |
| Shared loading/empty/error style | `Frontend/quran-dashboard-ui/src/styles/_components.scss`                                         |
| Load-state model                 | `.../models/mushaf.models.ts` (`ResourceLoadState`)                                               |

---

## 1. Current State

### How selected-word loading currently works

- Word selection flows through the URL. `selectWord(wordLocation)` in the facade pushes
  the word (and its derived ayah verse-key) into the query params, which re-triggers
  hydration (`mushaf-reader.facade.ts` `selectWord`, lines ~236–250).
- Hydration (`applyAuthoritativeUrlSnapshot` in `mushaf-url-hydration.ts`) compares the
  new word against the current one. On change it calls `setWord(word, reload=true)`.
- In the facade `setWord` handler:
  - **First** word selection (no previous word) → `loadWordAnalysis()` runs immediately.
  - **Subsequent** selection (a word was already selected) → `scheduleWordAnalysisLoad()`
    debounces the request by `WORD_ANALYSIS_SWITCH_DELAY_MS = 700` ms.
- `runWordAnalysisLoad()` sets `_wordAnalysisLoadState = { isLoading: true, ... }`,
  fetches via the reader cache, and on success sets `_wordAnalysis` to the new view model.
  An incrementing `wordAnalysisRequestToken` guards against out-of-order responses.

### How selected-ayah / study loading currently works

- `selectWord` also writes the word's verse-key into the `ayah` query param, and
  `selectAyah` writes the verse-key directly.
- Hydration computes `ayahChanged = snapshot.ayah !== current.selectedAyahKey` and
  `sourcesChanged`, then calls `setAyah(verseKey, reload = ayahChanged || sourcesChanged)`.
- `loadAyahStudy()` sets `_ayahStudyLoadState = { isLoading: true, ... }`, fetches, and
  on success sets `_ayahStudy` to the new view model.

### Is selected word / ayah data cleared to null/empty before replacement?

- **No — not at load start.** Neither `runWordAnalysisLoad()` nor `loadAyahStudy()` nulls
  the data signal when loading begins. The previous `_wordAnalysis` / `_ayahStudy` value
  stays in the signal until success or an explicit empty result.
- Data **is** nulled in two cases: (a) `onSettled` when the result is empty
  (`isEmpty` → `set(null)`), and (b) explicit clears — `clearWordSelection()` /
  `clearAyahSelection()` — when the corresponding URL param is removed.
- **Important nuance:** even though the data signal keeps the previous value during
  loading, the **templates do not render it** while `isLoading` is true (see §2). So the
  retained data is effectively discarded *visually* during the load.

### One shared loading state, or separate states?

- **Separate, per-resource load states** already exist in the facade:
  `_pageLoadState`, `_ayahStudyLoadState`, `_wordAnalysisLoadState`, each a
  `ResourceLoadState { isLoading, isEmpty, errorMessage }` (`mushaf.models.ts:328`).
- These are wired independently into the two cards via `study-context-section`
  (`[wordLoadState]` and `[ayahLoadState]` are distinct inputs). There is **no** single
  global spinner gating both panels.

### Are DOM blocks conditionally removed during loading?

- **Yes.** Both cards use an `@if / @else if` chain where the **loading branch replaces
  the entire inner content** of the card:
  - `selected-word-section.component.html:10–13` — when `loadState().isLoading`, the
    whole card body (header `__rendered-word` + `__content`) is removed and replaced by a
    single centered `.qd-loading-state` text line ("جارٍ تحميل تحليل الكلمة...").
  - `selected-ayah-section.component.html:6–9` — when `loadState().isLoading`, the source
    selector + tabs + ayah text + content are all removed and replaced by a single
    `.qd-loading-state` text line ("جارٍ تحميل دراسة الآية...").
- The wrapper `study-context-section.component.html` additionally mounts/unmounts the
  ayah block as a whole via `@if (selectedVerseKey())` (lines 18–35), and toggles the
  entire panel between an empty-state and the cards via
  `@if (!selectedWordLocation() && !selectedVerseKey())` (line 6).

### Where the layout shift most likely comes from

1. **Loading branch collapses the card.** Because the loading branch renders only a
   one-line `.qd-loading-state`, each card's height collapses from "full content" to
   "one centered line," then expands again when data arrives. This is the primary jump.
2. **No stable shell dimensions.** In embedded mode (used inside the study panel),
   `.selected-word-section` / `.selected-ayah-section` set `height: auto` and have **no
   `min-height`** (`selected-word-section.component.scss:24–30`,
   `selected-ayah-section.component.scss:24–30`). The flex column sizes to its content,
   so collapsing content collapses the card.
3. **No skeleton anywhere.** A repository-wide search for `skeleton`/`shimmer`/`wave`/
   `pulse` returns nothing. The only loading affordance is `.qd-loading-state` (centered
   muted text, `opacity: 0.7`, `padding: var(--qd-space-6)` — `_components.scss:85–100`).

---

## 2. Root Cause Analysis

Ranked by likelihood / impact:

1. **Template replaces the whole shell during loading (primary cause).** In both cards
   the `@else if (loadState().isLoading)` branch sits *before* the data branch and
   renders a single loading line instead of the card's structure. Headers, tabs, source
   selectors, and content blocks are unmounted while loading, so the card's box collapses
   and re-expands. The retained previous data in the signal is never shown during load.
2. **Missing `min-height` / stable shell dimensions.** Embedded cards use `height: auto`
   with no `min-height`, allowing the box to shrink to whatever the loading line needs.
3. **`@if` / `@else if` removing card content.** Angular control flow physically removes
   the DOM subtrees (header/tabs/content) rather than keeping them mounted and swapping
   inner fields for placeholders.
4. **Structural mount/unmount at the wrapper.** `study-context-section` mounts/unmounts
   the ayah section + divider as a block, and toggles the whole panel against an empty
   state, which can add a secondary jump when a selection first appears or changes type
   (word-only → word+ayah).
5. **CSS flex behavior allows height collapse.** The cards are flex columns
   (`flex-direction: column`, `min-height: 0`); with `height: auto` and no floor, child
   removal lets the column collapse.

### What is **not** the root cause (already handled correctly)

- **Same-ayah reload is already suppressed.** `selectWord` writes the word's own
  verse-key into the `ayah` param; hydration only reloads the ayah when
  `ayahChanged || sourcesChanged`. Selecting another word in the **same** ayah therefore
  does **not** set `_ayahStudyLoadState.isLoading` and does **not** reload the ayah card.
  (This should be preserved and covered by a regression test, not rebuilt.)
- **Out-of-order / fast-click responses are already guarded** by `wordAnalysisRequestToken`
  plus the 700 ms debounce on word switching.
- **Load states are already separate per resource** — no shared global spinner couples
  the panels.

**Net:** the problem is overwhelmingly a **template + CSS** problem (collapsing shell, no
skeleton, no min-height), not a state-model problem. The state model already carries the
right signals and already retains previous data during load.

---

## 3. Desired UX Behavior

- The **word card keeps the same outer size** during loading; it does not shrink/expand.
- The **ayah / study card keeps the same outer size** during loading.
- **Headers, tabs, source selectors, and the card shells stay mounted** during loading.
- **Skeleton / shimmer / wave placeholders** appear *inside* the existing shell (for the
  word glyph row, segment rows, identity rows; for the ayah text and the active study
  card body), instead of collapsing to a single line.
- **Actions may be disabled while loading** (e.g. source selector, tab buttons) but should
  not disappear unless strictly necessary.
- If the new word is in the **same ayah**, the ayah panel is **not** reset or reloaded
  (already true — keep it).
- If the new word is in a **different ayah**, the ayah data loads **independently** of the
  word card, with a stable skeleton UI inside the unchanged shell.
- Errors and empty results remain clearly visible (skeletons must not hide them).
- Respect `prefers-reduced-motion`: shimmer/wave animation reduces to a static placeholder.

---

## 4. Proposed State Model

The existing facade already provides nearly the entire model the task asks for. The
recommendation is to **keep the current separate-state model** and map the requested
fields onto it, rather than introduce a parallel model.

| Requested field        | Existing source of truth                                          |
| ---------------------- | ----------------------------------------------------------------- |
| `selectedWord`         | `_selectedWordLocation` + `_wordAnalysis`                         |
| `selectedWordLoading`  | `wordAnalysisLoadState().isLoading`                              |
| `selectedWordError`    | `wordAnalysisLoadState().errorMessage`                          |
| `selectedAyah`         | `_ayahStudy`                                                     |
| `selectedAyahLoading`  | `ayahStudyLoadState().isLoading`                                |
| `selectedAyahError`    | `ayahStudyLoadState().errorMessage`                            |
| `selectedAyahKey`      | `_selectedAyahKey`                                              |
| `pendingWordKey`       | implicit today via `wordAnalysisRequestToken` + debounce timer   |
| `pendingAyahKey`       | not tracked separately (ayah load is not debounced)              |

Optional, low-risk additions (only if needed by the chosen UI approach):

- Surface a `pendingWordLocation` (or `isWordSwitching`) so the word card can show a
  skeleton *during the 700 ms debounce*, not only after the request starts. Without this,
  there is a short window where a word is selected but neither old nor new state reads as
  "loading."

### Overlay vs. skeleton — recommendation

Two viable approaches; the state already supports both because data is not nulled at load
start:

- **(A) Keep previous data visible under a loading overlay** — lowest motion, but risks
  showing **stale data** for a *different* ayah/word while the new one loads.
- **(B) Keep the outer shell mounted and replace inner fields with skeleton placeholders**
  — recommended. The shell (header/tabs/source selector) stays put for stable layout,
  while only the data regions become skeletons. This avoids stale-content confusion and
  delivers the requested shimmer/wave affordance.

**Recommendation: approach (B)** for both cards. For the **same-ayah** word switch, the
ayah card neither reloads nor skeletons (no change at all), which is the desired calm
behavior.

---

## 5. Proposed Implementation Plan (no code in this report)

- **Phase A — Separate loading flags (verify / minimal).** Confirm the two cards already
  consume independent `wordLoadState` / `ayahLoadState`. Optionally surface a
  `pendingWordLocation` flag for the debounce window. No global spinner.
- **Phase B — No ayah reload for same-ayah selection (verify + lock in).** This is already
  implemented in `selectWord` + hydration. Add a regression test so it cannot silently
  break; do not rebuild it.
- **Phase C — Keep card shells mounted during loading.** Restructure the two card
  templates so the loading branch no longer replaces the whole card. Keep the header,
  tabs, and source selector mounted; swap only the inner data regions for placeholders.
- **Phase D — Reusable skeleton / shimmer UI.** Add design-system-consistent skeleton
  classes (e.g. `qd-skeleton`, `qd-skeleton--text`, shimmer/wave keyframes) alongside the
  existing state classes in `src/styles/_components.scss`, honoring
  `prefers-reduced-motion`. Reuse the existing token palette (surfaces, radii, spacing).
- **Phase E — Stable layout dimensions.** Add `min-height` / stable shell sizing to the
  word and ayah cards (and, if needed, the wrapper) so the box does not collapse between
  states. Keep it responsive (prefer `min-height` over fixed `height`).
- **Phase F — Tests.** Add/update specs for: same-ayah word selection (ayah panel not
  reloaded), loading-state rendering (shell stays mounted, skeleton shown, actions
  disabled not removed), and cross-ayah selection (ayah skeleton inside stable shell).

---

## 6. Files Likely To Change (not edited here)

Frontend only:

- `.../components/selected-word-section/selected-word-section.component.html` — restructure
  loading branch; keep shell mounted; add skeleton placeholders.
- `.../components/selected-word-section/selected-word-section.component.scss` — `min-height`
  / stable dimensions; skeleton styles (or consume shared classes).
- `.../components/selected-ayah-section/selected-ayah-section.component.html` — same
  restructure (keep source selector + tabs mounted; skeleton the ayah text + active card).
- `.../components/selected-ayah-section/selected-ayah-section.component.scss` — `min-height`
  / stable dimensions; skeleton styles.
- `.../components/study-context-section/study-context-section.component.{html,scss}` —
  optional: stabilize divider/block transitions and min-heights to avoid wrapper-level jumps.
- `Frontend/quran-dashboard-ui/src/styles/_components.scss` — add reusable
  skeleton/shimmer/wave classes next to `.qd-loading-state` / `.qd-empty-state`.
- `.../state/mushaf-reader.facade.ts` — *optional, minimal*: surface a
  `pendingWordLocation` / `isWordSwitching` flag for the debounce window if Phase A needs it.
- `.../models/mushaf.models.ts` — *optional*: add the pending flag to the state model only
  if introduced above.
- Specs (Phase F): `selected-word-section.component.spec.ts`,
  `selected-ayah-section.component.spec.ts`, `study-context-section.component.spec.ts`,
  and a facade/hydration regression test for same-ayah no-reload.

---

## 7. Risks

- **Stale data during loading** — if approach (A)/an overlay is used, a different ayah's
  tafsir or a different word's analysis could remain visible momentarily. Approach (B)
  (skeleton inner regions) avoids this.
- **Out-of-order responses on fast clicking** — already mitigated by the request token +
  700 ms debounce; the template change must not bypass that guard.
- **Overusing fixed heights** — hard-coded `height` values would break responsive layout
  and RTL/Arabic text reflow. Prefer `min-height` and intrinsic sizing.
- **Hiding errors/empty states behind skeletons** — error and empty branches must still
  win over the skeleton; do not show a shimmer when there is a real error.
- **Accidental backend/API change** — none required; the data flow and contracts are
  unchanged. Any change here must stay in the frontend.
- **Regressing the same-ayah behavior** — the existing no-reload logic must be preserved;
  add a test to protect it.
- **Motion accessibility** — shimmer/wave must respect `prefers-reduced-motion`.

---

## 8. Guardrails

- **Frontend UI / state only.**
- **No backend changes.**
- **No API contract changes.**
- **No database / migration changes.**
- **No Quranic data changes.**
- **No Spec Kit artifacts.**
- **No commit.**

---

## 9. Suggested Verification (for a later implementation task)

- Select multiple words **within the same ayah**; confirm the ayah study panel does **not**
  reload, reset, or resize.
- Select words **across different ayahs**; confirm the ayah panel loads independently with a
  stable skeleton inside an unchanged shell.
- Click words **quickly** in succession; confirm no flicker, no out-of-order content, and
  no card collapse.
- Test **desktop / tablet / mobile** breakpoints; confirm stable layout in each.
- Confirm **no visible card shrink/expand** for the word card on selection.
- Confirm the **selected-ayah study panel does not reload** for a same-ayah word selection.
- Confirm **skeleton / shimmer / wave** appears only where data is loading, not over errors
  or empty states.
- Confirm **reduced-motion** users get a static placeholder rather than animation.
