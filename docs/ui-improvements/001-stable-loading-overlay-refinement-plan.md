# UI-001 Refinement — Loading Overlay Implementation Plan

- **Status:** Planned
- **Area:** Mushaf Reader / Study Panels
- **Date:** 2026-06-20
- **Source of truth:** [Follow-up Audit](001-stable-loading-follow-up-audit-report.md) (builds on [UI-001](001-stable-loading-layout-shift-report.md))
- **Scope:** Frontend UI / state only — implementation plan only, no code in this file (see Guardrails)

A small, safe plan to turn the follow-up audit's recommendation into implementation work
for GLM. Keep it tightly scoped: fix the residual shrink, the disappearing static
elements, and the invisible light-mode shimmer — nothing more.

---

## 1. Final Direction

**Hybrid / content-level overlay.** (Per the follow-up audit, §4.)

- ❌ Field-level skeletons — cannot fully remove the shrink (a skeleton can't match
  arbitrary real-content height).
- ❌ Full card overlay — would veil controls that must stay legible (tabs, labels).
- ✅ **Hybrid:** keep the card shell + all static structure mounted, and layer an overlay
  **over the data body region only**, carrying the shimmer/wave + a subtle loading label.

---

## 2. Facade / State Decision

**Keep the previous view models mounted during loading — do not set them to `null`.**

- In `scheduleWordAnalysisLoad` and `scheduleAyahStudyLoad`, **remove** the
  `this._wordAnalysis.set(null)` / `this._ayahStudy.set(null)` calls. Keep flipping
  `isLoading: true` immediately (that part stays).
- **Why:** the previous content is what holds the card's box height. Clearing it to `null`
  is the root cause of the shrink (the box drops to the skeleton/floor height, then grows
  when new data arrives). Keeping it mounted means the box keeps its size → no shrink.
- **Stale-data safety:** the opaque content-level overlay (with shimmer + "loading" label)
  masks the retained previous content so it cannot be read as current data. The overlay,
  not `set(null)`, now handles the stale-data concern that the original UI-001 fix added.
- **Keep unchanged:** the request-token guards (out-of-order safety), the 700 ms debounce,
  the immediate `isLoading: true`, the same-ayah no-reload logic, and the empty/error
  `set(null)` paths in `onSettled` (those are real "no data" cases, not loading).

---

## 3. Template Strategy

**Must remain mounted and visible during loading:**

- The card shell (`<section class="selected-word-section">` / `selected-ayah-section`).
- Static labels — word identity labels (`التكرار (بالتشكيل)` / `التكرار (مبسّط)`); any
  constant section text. Move these **outside** the `isLoading` branch so they never unmount.
- The ayah **tabs** nav (already kept; stay mounted + `disabled`).
- Structural sections + their `aria-label` wrappers.
- The **current content body** (word segments/identity values; ayah text + active study
  card) — kept mounted under the overlay rather than swapped out.

**Covered/disabled by the overlay (not removed from the DOM):**

- The data body region gets the absolute overlay on top.
- Interactions on the covered body are disabled (`inert` / `pointer-events: none`); the
  body is `aria-hidden` while the existing visually-hidden `role="status"` live region +
  `aria-busy` announce loading.
- Static controls outside the overlay (tabs) stay visible but `disabled`.

> Net change vs current: stop replacing data regions with sized skeleton swaps; instead
> keep them mounted and drop one overlay over the body.

---

## 4. CSS Strategy

- **Absolute overlay over the content body**: a reusable class (e.g. `qd-loading-overlay`)
  positioned `absolute; inset: 0` over a `position: relative` body container.
- **Visible shimmer/wave**: reuse/move the `qd-skeleton-wave` sweep into the overlay so the
  loading motion reads clearly.
- **Stronger light-mode contrast using existing tokens only**: re-base the overlay/skeleton
  tint on a **darker** mix (e.g. `color-mix(in oklch, var(--qd-surface), var(--qd-border))`
  / toward `--qd-text`) instead of `--qd-surface-elevated`, which is *lighter* than the
  card in light mode and thus invisible. No new palette, no theme overhaul.
- **Reduced-motion support**: keep the `prefers-reduced-motion` fallback — static,
  sufficiently-visible tint with no sweep.
- **No fixed heights unless absolutely necessary**: keep `min-height` floors as a safety
  net; do not introduce fixed `height`. Height stability now comes from the retained
  content, not from reserved skeleton heights.
- **Preserve responsive layout**: overlay is layout-neutral (absolute), so RTL and
  breakpoints are unaffected; the embedded study-context column no longer reflows because
  sibling cards keep their size.

---

## 5. Test Strategy

Update/add focused specs (Vitest, existing patterns):

- **Static elements remain mounted while loading** — word identity labels + structural
  shell present during `isLoading` (currently asserted *absent* — invert these).
- **Overlay appears during loading/debounce** — overlay element present while `isLoading`,
  including the immediate debounce window the facade sets.
- **Previous content is masked, not presented as current** — body still in the DOM but
  `aria-hidden` / covered by the overlay (not shown as normal interactive data).
- **Card does not shrink/collapse during loading** — loading box height `>=` the
  previously-rendered height (no drop to skeleton/floor).
- **Same-ayah selection still does not reload the ayah panel** — keep the existing UI-001
  regression test green (no `getAyahStudy` refetch, no `isLoading` flip).
- **Errors/empty states still win over loading** — error and failed/empty branches take
  precedence and are not masked by the overlay.
- **Reduced-motion** — overlay renders a static visible tint with no sweep.
- **Facade**: assert `scheduleWordAnalysisLoad` / `scheduleAyahStudyLoad` flip
  `isLoading: true` immediately **and now retain** the previous view model (no longer
  `null`) during the debounce window.

---

## 6. Files Likely To Change

- `components/selected-word-section/selected-word-section.component.html`
- `components/selected-word-section/selected-word-section.component.scss`
- `components/selected-ayah-section/selected-ayah-section.component.html`
- `components/selected-ayah-section/selected-ayah-section.component.scss`
- `components/study-context-section/study-context-section.component.scss` *(only if the
  column still needs help keeping sibling cards stable)*
- `src/styles/_components.scss` *(overlay class + light-mode tint/wave contrast)*
- `state/mushaf-reader.facade.ts` *(remove `set(null)` in the two schedule methods; keep
  everything else)*
- Specs: `selected-word-section.component.spec.ts`,
  `selected-ayah-section.component.spec.ts`,
  `state/mushaf-reader.facade.url-sync.spec.ts`,
  `state/mushaf-reader.facade.ayah-study.spec.ts`

---

## 7. Guardrails

- **Frontend UI / state only.**
- **No backend / API contract / database / migration changes.**
- **No Quranic data changes.**
- **No redesign / theme overhaul** — contrast fix uses existing design tokens only.
- **No Spec Kit artifacts.**
- **No commit.**
