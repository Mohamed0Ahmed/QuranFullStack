# Performance Remediation Report — Roots Explorer Frontend (Feature 015)

**Date:** 2026-06-24  
**Trigger:** Angular performance review (PASS WITH NOTES) — findings F1, F2, F3  
**Verdict after remediation:** PASS — all noted findings addressed

## Summary

Three frontend performance findings from the Roots Explorer Angular review were remediated without changing Quran rendering semantics, accessibility, or API contracts.

| Finding | Severity | Status | Approach |
| --- | --- | --- | --- |
| F1 — `MutationObserver` with `subtree: true` on virtual-scroll tables | MINOR | Fixed | Observe `childList` only on the stable table scope |
| F2 — per-row template method href builders in detail lists | MINOR | Fixed | Precompute hrefs in `computed()` row view models |
| F3 — `rootExplorerHref()` template method in study panel | NOTE | Fixed | Converted to `computed()` signals (same for identity hrefs) |

## F1 — Gutter-sync observer scope on virtual-scroll hot path

### Problem

`syncTableScrollbarGutter` observed `{ childList: true, subtree: true }` on the table scope. CDK virtual scroll recycles row nodes inside the viewport on every scroll, firing the observer continuously and scheduling rAF layout reads (`offsetWidth - clientWidth`) on the scroll hot path.

### Change

`table-scrollbar-gutter-sync.ts` line 80: dropped `subtree: true`. The loading skeleton ⇄ viewport swap is a direct-child mutation at depth 1 and still triggers re-sync; virtual-scroll row recycles no longer do.

**Applies to:** `roots-table` and `unique-words-table` (shared util).

## F2 — Per-item deep-link hrefs rebuilt every CD cycle

### Problem

`ayah-matches-list` and `root-words-list` called `mushafHref(match)` / `uniqueWordHref(item)` in templates — re-evaluated on every change-detection pass for up to 100 rows.

### Changes

1. **`ayah-matches-list.component.ts`** — `rows` computed maps each `AyahMatchDto` to `{ match, mushafHref }` once when `page()` changes.
2. **`root-words-list.component.ts`** — `rows` computed maps each `RootWordItemDto` to `{ item, uniqueWordHref }` once when `page()` or `wordView()` changes.
3. Templates bind `row.mushafHref` / `row.uniqueWordHref` (plain fields, no template methods).

## F3 — Template-called methods in selected-word-section

### Problem

`rootExplorerHref()` and `uniqueWordIdentityHref(kind)` were component methods invoked from the template on each CD pass. Low impact (single instance), but inconsistent with signal-based patterns elsewhere.

### Changes

`selected-word-section.component.ts`:
- `rootExplorerHref` → `computed()` signal
- `tashkeelIdentityHref` / `simpleIdentityHref` → `computed()` signals
- Template binds signal reads instead of method calls

## Files changed

| File | Change |
| --- | --- |
| `utils/table-scrollbar-gutter-sync.ts` | Drop `subtree: true` |
| `components/ayah-matches-list/ayah-matches-list.component.ts` | `rows` computed with precomputed `mushafHref` |
| `components/ayah-matches-list/ayah-matches-list.component.html` | Bind `row.mushafHref` |
| `components/root-words-list/root-words-list.component.ts` | `rows` computed with precomputed `uniqueWordHref` |
| `components/root-words-list/root-words-list.component.html` | Bind `row.uniqueWordHref` |
| `components/selected-word-section/selected-word-section.component.ts` | Href `computed()` signals |
| `components/selected-word-section/selected-word-section.component.html` | Signal reads for href bindings |

## Verification

```bash
cd Frontend/quran-dashboard-ui
VITEST_MAX_FORKS=2 npm test -- --no-watch \
  --include='**/words/**/*.spec.ts' \
  --include='**/selected-word-section.component.spec.ts' \
  --include='**/roots-explorer-page.component.spec.ts'
```

**Results:** 190+ words-feature tests passed; `selected-word-section` (13 tests) and `roots-explorer-page` specs green.

## Quran rendering safety

No changes to Quran text display, highlight semantics, RTL layout, accessibility of Quran actions, or reduced-motion behavior. F1 removes incidental layout work during scroll; F2/F3 move string assembly from template methods to memoized computeds — href values are unchanged.

## Out of scope (unchanged)

- Virtual-scroll configuration (already correct for 1000-row roots list).
- `ApiResponseCache` LRU/dedup (already bounded).
- Lazy route chunking (already via `loadComponent`).
