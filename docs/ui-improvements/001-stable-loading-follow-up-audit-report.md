# UI-001 Follow-up Audit — Stable Loading Refinement for Mushaf Selection Panels

- **Status:** Reported
- **Area:** Mushaf Reader / Study Panels
- **Date reported:** 2026-06-20
- **Relates to:** [UI-001](001-stable-loading-layout-shift-report.md) (implemented, reviewed, fixed, committed in `f142adf`)
- **Scope:** Frontend UI / state only — report-only, no code changed (see Guardrails)

This is a focused follow-up audit after the UI-001 implementation shipped. The
stable-shell + skeleton approach fixed the original *collapse-to-one-line* jump, but
manual visual review surfaced residual UX issues: some static elements still vanish
during loading, the cards still resize, and the shimmer is nearly invisible in the
light theme. This report analyzes why, against the committed code, and recommends a
refined direction.

### Files inspected (post-commit `f142adf`)

| Concern                          | File                                                                                       |
| -------------------------------- | ------------------------------------------------------------------------------------------ |
| Word card template/style         | `components/selected-word-section/selected-word-section.component.{html,scss}`              |
| Ayah card template/style         | `components/selected-ayah-section/selected-ayah-section.component.{html,scss}`              |
| Study panel wrapper              | `components/study-context-section/study-context-section.component.{html,scss}`              |
| Skeleton/shimmer classes         | `src/styles/_components.scss` (lines 106–195)                                               |
| Theme tokens                     | `src/styles/_tokens.scss` (`:root`), `src/styles/_themes.scss` (dark overrides)            |
| Load orchestration               | `state/mushaf-reader.facade.ts`                                                             |
| Tests                            | `selected-word-section.component.spec.ts`, `selected-ayah-section.component.spec.ts`, `mushaf-reader.facade.url-sync.spec.ts`, `mushaf-reader.facade.ayah-study.spec.ts` |

---

## 1. Current Implementation Summary

**Selected-word loading.** The card uses a four-way `@if/@else if` chain (error →
"select a word" → failed/empty → `@else`). Inside the `@else`, every region branches
again on `loadState().isLoading`:

- A visually-hidden `<p class="qd-sr-only" role="status" aria-live="polite">` announces
  loading (`selected-word-section.component.html:15–19`).
- **Header:** the real `qd-segment-rendered-word` (the word glyph) is *replaced* by a
  `9rem × 2rem` skeleton bar (lines 22–33).
- **Content:** the real segment rows + morphology summary **and** the identity `<dl>`
  (including its static `<dt>` labels) are *replaced* by skeleton lines (lines 37–59).

**Selected-ayah/study loading.** Same shape (`selected-ayah-section.component.html`):

- Visually-hidden live-region announces loading (lines 15–19).
- **Source slot:** `qd-source-selector` is *replaced* by a `12rem × 2.25rem` skeleton
  (lines 22–26).
- **Tabs:** the three tab buttons stay mounted and are `[disabled]` while loading
  (lines 60–88) — these are the only data-independent controls kept in place.
- **Ayah text:** *replaced* by a skeleton bar inside a `--skeleton` slot with a reserved
  `min-height: 3.25rem` (lines 90–93).
- **Content body:** the active study card is *replaced* by a 6-line skeleton group; the
  `__content` gains `--loading` with `min-height: 10rem` (lines 100–123).

**Where skeleton/shimmer is defined.** Globally in `src/styles/_components.scss:106–195`:
`.qd-skeleton` (base tint = `--qd-surface-elevated`), width/height modifiers
(`--text`, `--w-25/50/75/90`, `--block`, `--rounded-md`), `.qd-skeleton-group`, a
`::after` moving-gradient wave (`qd-skeleton-wave`, 1.6s), and a `prefers-reduced-motion`
fallback to a static tint.

**Granularity: field/section-level, not card-level.** Loading is handled by **per-region
`@if` swaps** inside each card. There is **no overlay**; each data region is removed and
a sized skeleton is mounted in its place.

**Static elements: partially replaced.** The outer `<section>` shells, the section
`aria-label` wrappers, and (in the ayah card) the tabs nav remain mounted. But several
*data-independent* elements are removed during loading — most notably the **word
identity labels** (`التكرار (بالتشكيل)` / `التكرار (مبسّط)`), which are constant text, not
data.

**Facade behavior (relevant).** On a debounced switch, `scheduleWordAnalysisLoad` /
`scheduleAyahStudyLoad` now **clear the previous view model to `null`** and flip
`isLoading: true` *immediately* (`mushaf-reader.facade.ts` ~lines 296–320 and ~451–470).
This was the correct UI-001 fix for *stale content*, but it is also the main driver of
the residual *shrink* (see §3).

---

## 2. Remaining Visual Issues

1. **Static elements disappear during loading.** The word card's identity **labels**
   (`<dt>`) are inside the `@if (isLoading)` branch that swaps the whole `<dl>` for
   skeletons, so constant text vanishes and returns. The word glyph and the ayah source
   selector also disappear, though those are data-dependent.
2. **Cards still resize during loading.** `min-height` is only a **floor** (`14rem` word,
   `18rem` ayah; `10rem` for the ayah content body). Real loaded content is routinely
   **taller** than these floors (a full tafsir easily exceeds 10rem). On a switch the
   previous (tall) content is cleared to `null`, the box drops to the skeleton/floor
   height, then **grows** again when the new data arrives — a visible shrink-then-grow.
3. **Shimmer/wave invisible in light theme.** `.qd-skeleton` base is
   `--qd-surface-elevated` = `oklch(0.995 …)` sitting on `--qd-surface` = `oklch(0.985 …)`
   — a **+0.01 lightness** difference (the skeleton is actually *lighter* than the card).
   The wave `::after` is `color-mix(var(--qd-surface) 55%, transparent)` — a translucent
   near-white sweep over a near-white base. In light mode both are effectively
   imperceptible. (Dark theme has `surface 0.20` vs `elevated 0.25`, ΔL 0.05 — noticeably
   better, which is why the issue reads as "light-theme only".)
4. **Inner sections collapse despite the outer floor.** `min-height` lives on the **outer
   card** and on a couple of slots (`__header` 2.75rem, `__ayah--skeleton` 3.25rem,
   `__content--loading` 10rem). The `__section` blocks and the `__identity` rows have
   **no reserved height**, and skeleton text lines are only `0.75rem` tall vs ~`1.4–1.5rem`
   for real rows, so inner regions reflow even when the outer floor holds.
5. **`@if/@else` still removes stable structure.** Every data region is a hard
   mount/unmount swap. Nothing layers loading *over* persistent content; the body is
   physically rebuilt on each transition, which is what allows reflow.
6. **CSS permits shrink/reflow.** Cards are `height: auto` (embedded) flex columns whose
   height is intrinsic to content; with the previous content cleared and skeletons shorter
   than real content, the intrinsic height drops to the floor. In the embedded
   `study-context-section` column, the word card shrinking also **pushes the ayah card
   below it upward**, compounding the perceived shift.

---

## 3. Root Cause Analysis

- **Skeleton height ≠ real content height.** This is the core cause of the residual
  shrink. `0.75rem` text skeletons and small floors cannot reproduce the height of a real
  word analysis or a full tafsir, so any clear→skeleton→data cycle changes the box size.
- **`min-height` on the outer card, not on the inner sections.** The floor stops the
  *collapse-to-one-line* (UI-001's original win) but does not pin the inner regions, so
  sections still reflow within (and the card still grows past) the floor.
- **The stale-data fix traded stale for shrink.** Clearing the previous view model to
  `null` on switch (the right call for *stale content*) removes the only thing that was
  holding the box at its previous height. With nothing mounted, the box falls to the
  skeleton/floor height → shrink. Field-level skeletons cannot win both "no stale" and
  "no shrink" at once, because the skeleton can't match arbitrary content height.
- **Loading branch still replaces static UI.** Constant labels (word identity `<dt>`s) are
  caught inside the data-region swap instead of living outside the `isLoading` branch.
- **Skeleton tint derived from the wrong direction in light mode.** A skeleton needs a tint
  that is *darker/greyer* than its surroundings in light themes. Using
  `--qd-surface-elevated` (which is *lighter* than `--qd-surface`) inverts the contrast, so
  the placeholder and its shimmer disappear into the near-white surface.
- **Shimmer gradient too subtle.** The sweep mixes the surface color at 55% alpha; with a
  near-white surface there is almost no luminance delta to animate.
- **Field-level granularity is the wrong tool for "no layout shift."** Keeping layout
  perfectly stable means keeping the *box* stable; the most reliable way to keep the box
  stable is to keep real content (which defines the box) mounted and layer the loading cue
  over it — i.e. a card/content-level overlay — rather than removing content and rebuilding
  it at a different height.

---

## 4. Recommended UX Direction

**Recommendation: C — Hybrid (content-level overlay over a fully-mounted shell), with a
contrast fix.** This validates the suspected direction, with one important code-grounded
correction.

Why not the alternatives:

- **A (keep improving field-level skeletons):** cannot fully solve the shrink, because a
  skeleton can't match real content height; you would chase per-field reserved heights
  forever and still jump whenever real content differs from the reserved estimate.
- **B (pure card-level overlay over the *entire* card):** stable, but it would also veil
  the genuinely-static, still-useful controls (tabs, labels) and the section scaffolding,
  which the product wants kept visible and legible.

**The hybrid (recommended):**

- Keep the card **shell and all static/structural elements mounted and visible**: section
  wrappers + `aria-label`s, the ayah **tabs**, the word **identity labels**, and the
  source-selector container.
- Layer an **absolutely-positioned overlay over the data *body* region only** (the word
  segments/identity values; the ayah text + active study card), not the whole card.
- The overlay carries the **visible wave/shimmer + a subtle "loading" label**, and is
  opaque/tinted enough to read clearly as "loading" — including masking any content
  beneath it so it is not mistaken for current data.
- **Disable interactions** on the covered body while loading (`inert` / `pointer-events`),
  keep the static controls (tabs) `disabled` but visible, and keep the existing
  `aria-busy` + visually-hidden live region.
- **Preserve `min-height`** floors and responsive behavior.

**Important correction to the proposed plan — revisit the facade `set(null)`.** The
overlay direction works best if the body keeps its *previous* height while loading. The
cleanest way to get that for free is to **stop clearing the previous view model to `null`
on switch** and instead let the overlay mask the (now height-preserving) previous content.
That removes the shrink at its root *and* the opaque overlay handles the stale-data concern
that `set(null)` was added for. If product prefers to keep clearing the data, then the body
must instead **reserve the outgoing height** (capture it into a `min-height` on entering
loading) — more moving parts and worth avoiding. Net: the overlay and the `set(null)`
decision are coupled; choose "keep content + mask it" over "clear content + reserve height."

This direction satisfies all the stated criteria: minimal layout shift (real content holds
the box), no stale-data confusion (opaque overlay + label), static elements stay visible,
a clear loading cue, preserved accessibility (live region + `aria-busy` + `inert`), and
preserved responsive layout (no fixed heights introduced).

---

## 5. Proposed Refinement Plan (no code)

- **Phase A — Inventory and preserve static DOM.** Identify every data-independent element
  currently removed during loading (word identity labels; section scaffolding; ayah tabs
  already kept) and move them **outside** the `isLoading` branch so they never unmount.
- **Phase B — Add a content-level loading overlay.** Introduce a reusable overlay
  affordance (absolute over the body region) with the wave/shimmer and a subtle label;
  apply it to the word body and the ayah text + study-card body. Decide the facade
  coupling: prefer **keep previous content mounted + mask it** over clearing to `null`
  (see §4 correction).
- **Phase C — Strengthen skeleton/wave visibility in light mode.** Re-base the skeleton/
  overlay tint on a **darker** mix (e.g. mix `--qd-surface` toward `--qd-border`/`--qd-text`)
  so it reads against near-white surfaces, and increase the shimmer's luminance delta —
  all via existing design tokens, no new palette. Keep the reduced-motion fallback.
- **Phase D — Stabilize inner sections.** Add reserved `min-height` / matched heights to the
  inner sections and rows that still reflow (word `__section`/`__identity` rows; align
  skeleton line heights closer to real text rows), so nothing collapses inside the floor.
- **Phase E — Update tests.** Shift assertions from "real data is *removed* during loading"
  to "static elements **remain mounted** during loading and an overlay is present"
  (see §7) — the current specs assert the opposite and will need revising with the
  behavior change.
- **Phase F — Manual visual verification.** Run the scenarios in §7 across light/dark and
  breakpoints, watching specifically for box size constancy and overlay clarity.

---

## 6. Files Likely To Change (not edited here)

- `components/selected-word-section/selected-word-section.component.html` — keep static
  labels mounted; replace field swaps with an overlay over the body.
- `components/selected-word-section/selected-word-section.component.scss` — overlay
  positioning; inner-section reserved heights.
- `components/selected-ayah-section/selected-ayah-section.component.html` — overlay over
  ayah text + study body; keep static structure mounted.
- `components/selected-ayah-section/selected-ayah-section.component.scss` — overlay
  positioning; content-body height stability.
- `components/study-context-section/study-context-section.component.scss` — only if the
  column needs help keeping sibling cards from shifting when one card loads.
- `src/styles/_components.scss` — re-tint skeleton/wave for light-mode contrast; add the
  reusable overlay classes (`qd-loading-overlay` or similar) next to `.qd-skeleton`.
- `state/mushaf-reader.facade.ts` — the `set(null)` vs keep-and-mask decision in
  `scheduleWordAnalysisLoad` / `scheduleAyahStudyLoad` (Phase B). Behavior-affecting; only
  if the keep-content overlay path is chosen.
- Specs: `selected-word-section.component.spec.ts`,
  `selected-ayah-section.component.spec.ts`, and the facade specs
  (`mushaf-reader.facade.url-sync.spec.ts`, `mushaf-reader.facade.ayah-study.spec.ts`) if
  the `set(null)` behavior changes.

---

## 7. Test Plan

- **Word card static elements remain mounted while loading** — assert the identity labels
  (and section scaffolding) are present during `isLoading`, not just after.
- **Ayah card static shell remains mounted while loading** — tabs present + disabled
  (already covered); assert the structural shell stays and is not veiled.
- **Loading overlay appears during debounce/loading** — assert the overlay element is
  present while `isLoading` (including the immediate debounce window the facade now sets).
- **Card does not collapse when loading** — assert the loading box height is `>=` the
  previously-rendered height (or at least the floor), with no drop to skeleton height.
- **Errors/empty states still win over the overlay** — error and "failed/empty" branches
  must still take precedence and must not be masked by the overlay.
- **Reduced-motion remains acceptable** — overlay shows a static, sufficiently-visible
  tint with no sweep under `prefers-reduced-motion`.
- **Update existing assertions** — the current specs assert real data is *absent* during
  loading (e.g. `word-identity-summary` is null); these must be revised to match the
  keep-mounted + overlay behavior.

---

## 8. Guardrails

- **Frontend UI / state only.**
- **No backend changes.**
- **No API contract changes.**
- **No database / migration changes.**
- **No Quranic data changes.**
- **No Spec Kit artifacts.**
- **No redesign / theme overhaul** in this task — re-tinting is limited to existing design
  tokens for skeleton/overlay contrast, not a visual redesign.
- **No commit.**
