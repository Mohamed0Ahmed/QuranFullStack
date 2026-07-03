---
name: performance-angular-review
description: >-
  Deep, review-only PERFORMANCE audit for the Quran Dashboard Angular 20 frontend
  (standalone components, Signals, RxJS, Angular router, SCSS + Tailwind, Vitest / the
  Angular unit-test builder). Explicit-invocation only — use this skill when the user
  explicitly asks for an Angular / frontend / client-side performance review, e.g. running
  "performance-angular-review", a slow or janky Angular UI, excessive re-renders, change
  detection cost, Signals / computed / effect recomputation cost, RxJS leaks or
  subscription / timer / router cleanup, missing @for track, large DOM / heavy lists,
  bundle or chunk cost, duplicate or repeated frontend API calls, route lazy loading, render
  cost, or slow frontend tests (Vitest worker cost, heavy component setup). It inspects only
  the changed frontend scope unless a wider audit is requested, and produces evidence-based
  findings, severities, and recommendations — never code fixes. Do NOT use this skill for
  general code review, engineering review, PR review, clean-code review, architecture or
  structure review, UI / design review, Spec Kit / phase review, or "is this safe to merge"
  — those belong to engineering-review (and impeccable / frontend-design for visual design).
  It is the frontend counterpart to performance-backend-review; do not use it for backend /
  EF Core / database performance. It triggers only on explicit Angular/frontend performance
  intent, not on the word "review" by itself.
---

# Performance Angular Review

A deep, **review-only** performance audit of the Quran Dashboard Angular 20 frontend
(standalone components, Signals, RxJS, Angular router, SCSS + Tailwind, centralized `qd-`
style system; feature-first under `src/app/features/<feature>/` with page components →
facades/state services → API services → reusable components; tested with Vitest through the
Angular unit-test builder).

The job is to find **real, evidence-backed** render-time, change-detection, memory, network,
bundle, and test-time costs in the changed code — and to recommend how to fix them without
ever degrading Quran text rendering, readability, RTL correctness, or accessibility. You
report; you do not implement.

The **Mushaf reader and Quran study UI** (`src/app/features/mushaf/` — `mushaf-word`,
`mushaf-line`, `mushaf-page-view`, `study-context-section`, ayah cards, study panels,
modals, tabs, lazy-loaded details) are the performance-sensitive heart of the app: they
render large numbers of Quran word/ayah elements. Weight findings there accordingly.

## When to use / when NOT to use

This is an **explicit-invocation** skill. It sits alongside `engineering-review` (general
code review, architecture, clean-code, Spec Kit compliance, merge-readiness, UI style-system
checks), `impeccable` / `frontend-design` (visual/UX design), and `performance-backend-review`
(its backend counterpart). To avoid stepping on those skills, stay in your lane:

**Use this skill only when the request carries explicit frontend performance intent** — the
user says "performance", "slow", "janky", "laggy", "re-renders", "change detection", "Signals
/ effect cost", "memory leak", "subscriptions not cleaned up", "missing track", "huge DOM",
"bundle size", "duplicate requests", "lazy load", "render cost", "tests are slow", or names
this skill directly.

**Do NOT use this skill** (defer to `engineering-review`, or `impeccable`/`frontend-design`
for design) for: "review this PR", "engineering review", "is this safe to merge",
"architecture review", "UI review", "design review", "clean code review", "review the
implementation", "phase review", or any general frontend review with **no** performance
intent. And do **not** use it for backend/EF Core/database performance — that is
`performance-backend-review`. If the user wants both a general review and a performance pass,
do the performance pass here and say the general/design review is a separate skill.

## Review-only guardrails

- Do **not** modify application source code, templates, styles, or tests.
- Do **not** refactor, "quickly fix", or rewrite anything.
- Produce findings, severities, and recommendations only. Applying fixes is a separate,
  explicitly requested task.

## Evidence-based findings only (anti-noise)

Frontend performance review degrades into noise the moment it becomes a generic Angular
best-practices checklist. Every finding must point at a **real code path in the diff** with
a plausible cost. These rules keep the report short and trustworthy:

- Do **not** flag missing `OnPush` as MAJOR unless there is a genuinely **heavy or
  frequently-rendered** component or a clear re-render risk. Otherwise MINOR/NOTE.
- Do **not** demand **virtual scroll** for small lists. It is for large, frequently-updated
  lists (think full-page word/ayah rendering), not a 10-row table.
- Do **not** demand **caching** unless there is repeated expensive work or duplicate requests
  with evidence.
- Do **not** flag every missing `@for` **`track`** as blocking — severity scales with list
  size and how often the list re-renders. A small static list is a NOTE; a large
  frequently-rebuilt list is MAJOR.
- Do **not** recommend broad rewrites, premature abstractions, or speculative configurability
  in the name of performance.
- If the performance impact is **uncertain**, mark it **NOTE** and recommend a measurement
  (Angular DevTools profiler, a flame chart, a bundle-stats report, a timed test run) rather
  than asserting a problem.
- Keep the report focused. Do not pad sections with generic advice that does not apply to the
  changed code.

A false-positive finding sends engineers to "optimize" code that was fine and erodes trust in
the next finding. Prefer fewer, well-evidenced findings.

## Scope discipline

Review **only the changed frontend scope** (the diff / the files the user points at) unless
the user explicitly asks for a wider audit. To judge whether a component is hot or a list is
large, you may read the immediate parent/child components, the relevant facade/state service,
and the template it renders — but do not drift into auditing untouched features.

## Quran rendering safety overrides micro-optimization (hard constraint)

This is an Arabic-first (RTL) product that renders Quran text. **Readability, accuracy, and
correct semantics always win over a render or visual micro-optimization.** The general Quran
data-safety rules apply in full — see the shared reference:
`.claude/skills/engineering-review/references/quran-data-safety.md`. In a frontend
performance context specifically, a recommendation that does any of the following is itself
the defect — never propose it, and flag it if the diff already does it:

- Animates or transitions Quran glyphs/text, or applies motion to Quran content.
- Reduces Quran text readability, contrast, or sizing.
- Breaks text **selection / highlight** semantics on Quran words/ayat.
- Breaks **RTL** layout correctness.
- Compromises Quran display **accuracy** (e.g. swapping the correct Mushaf font/rendering for
  a "lighter" one that mis-renders glyphs or marks).
- Removes accessibility of Quran-related actions or ignores reduced-motion for Quran content.

If something cannot be made faster without touching one of these, say so plainly and stop
there. "Slower but correct and readable" is the right answer for Quran content.

## Context you may consult (optional, only when it sharpens a finding)

- `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md` — feature-first layout,
  page/facade/data-access/state separation, file-size thresholds.
- `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md` — centralized `qd-` classes,
  CSS variables, theme/RTL rules (so a CSS perf change does not break the style system).
- `Frontend/quran-dashboard-ui/.architecture/API_INTEGRATION_GUIDELINES.md` — the
  page → facade/store → API-service flow and `ApiResponse<T>` handling, for the network check.
- `package.json` test script — the project runs `ng test` (Vitest) with
  `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`. That worker cap is **deliberate** (the suite OOMs
  / freezes the machine without it). Do not recommend raising it to "speed up" tests, and
  treat the Angular-builder Vitest setup (no standalone `vitest.config.ts`) as the baseline.
- `.claude/skills/test-guard/references/frontend-test-harness-constraints.md` — how Angular
  specs run here (focused-run command, the fork cap, jsdom's missing browser APIs, and the
  test-env-vs-real-browser distinction). Consult it before treating a jsdom limitation as a
  runtime/performance problem, or a harness timeout as a test failure.

If a referenced document is missing, say so rather than inventing its contents.

## What to inspect

The sections below define what to evaluate; they map onto output sections 4–11. Walk them
against the changed code and skip a section with "Not applicable in this diff" when nothing
in scope touches it (do not invent material to fill it).

### Angular rendering / change detection
- **Unnecessary re-renders** — broad state updates that re-render large subtrees.
- **Expensive template expressions** — non-trivial work re-evaluated on every change cycle.
- **Methods called directly from templates** (`{{ compute() }}`, `[x]="build()"`) that run
  every change detection — prefer a signal/computed or a precomputed field.
- **Signal / computed / effect recomputation cost** — computeds doing heavy work, effects
  doing more than they need, recompute fan-out.
- **State updates that fan out too broadly** — one change invalidating far more than it should.
- **`OnPush` relevance** for heavy or frequently-rendered components (severity per anti-noise).
- **DOM updates caused by URL/state sync** — router/query-param sync that re-renders more
  than the changed slice.
- **Repeated class/style bindings on large word/ayah lists** — per-item `[ngClass]`/`[ngStyle]`
  recomputation across hundreds of Mushaf words.
- **Quran text rendering safety and readability** (see the hard constraint).

### List rendering / DOM size
- **`@for` / `track`** usage and key stability (severity scales with list size/churn).
- **Repeated lists without stable keys** causing full re-creation of DOM nodes.
- **Large lists** that may need pagination, slicing, lazy rendering, or virtual scroll —
  only when genuinely large (e.g. full-page word/ayah rendering), not small lists.
- **Nested list rendering cost** — lists within lists (lines × words) multiplying node counts.
- **Ayah/word/segment rendering density** — node count per rendered ayah/word.
- **Unnecessary DOM nodes or wrapper layers** in repeated content (each wrapper multiplies
  across every item).

### RxJS / Signals / lifecycle
- **Subscriptions, timers, intervals, router subscriptions** and whether they are cleaned up.
- **`takeUntilDestroyed` / `DestroyRef`** (or `async` pipe / equivalent) lifecycle handling.
- **Effects that trigger duplicate work** or feedback loops.
- **Stale timers or async callbacks firing after destroy.**
- **Memory-leak risks** — long-lived subscriptions in components that mount/unmount often.
- **Repeated state hydration or URL-sync loops** — state→URL→state cycles re-fetching or
  re-rendering.

### API / network efficiency (frontend perspective)
- **Duplicate frontend API calls** — the same request issued more than once.
- **Unnecessary reloads on URL/state changes** — refetching data that did not change.
- **Cache key correctness** in facades/state services (keys capture every input).
- **Lazy loading of heavy details** — fetch heavy detail only when actually needed/opened.
- **Over-fetching** from the frontend perspective — requesting more than the view uses.
- **Repeated requests caused by** tab switching, auto-clear, selection changes, navigation,
  or effects re-running.
- **Whether the UI requests details only when needed** (e.g. study panel/modal opens).

### Routing / lazy loading / bundle
- **Route-level lazy loading** for feature areas (`loadComponent`/`loadChildren`).
- **Accidental eager imports of heavy feature code** into shared/common paths.
- **Large shared modules/components pulled into common chunks** they don't belong in.
- **Expensive dependencies added to hot paths** (heavy libs imported where rarely needed).
- **Bundle/chunk concerns** when the diff plausibly affects them (note them; suggest a
  bundle-stats measurement rather than guessing numbers).
- **Theme/font/image loading** if touched by the diff (e.g. Quran font loading strategy).

### CSS / animation / visual runtime
- **Expensive animations/transitions on frequently repeated elements** (per-word/per-ayah).
- **Hover effects or shadows applied to very large lists** (repaint cost across many nodes).
- **Layout thrashing risks** — patterns forcing synchronous reflow.
- **Transitions on Quran glyphs/text** — never animate Quran text (hard constraint).
- **Reduced-motion behavior** — respect `prefers-reduced-motion`, especially for Quran content.
- **Readability/contrast impact** of any performance-related visual change.

### Frontend test runtime
- **Slow specs caused by full component trees** where shallow/unit scope would prove the same.
- **Repeated expensive setup** that a shared `beforeEach`/helper could amortize.
- **Fake timers and lifecycle cleanup tests** — using fake timers instead of real waits.
- **Unnecessary real async waits** (`setTimeout`/real delays) that slow the suite.
- **Test runner cost and worker limits** — respect the deliberate `VITEST_MAX_FORKS=2` cap;
  do not recommend raising it (it exists to avoid OOM/freeze).
- Do **not** recommend weakening assertions, accessibility checks, or Quran rendering safety
  to make tests faster.

### Accessibility / performance tradeoff
- Do **not** remove semantic `<button>`s, `focus-visible` states, aria labels, keyboard
  behavior, or readable text just to shrink DOM/CSS.
- Any performance change must preserve accessibility and **Arabic RTL** usability.

## Required review output

Produce the report in **exactly** this structure. Keep it focused; sections 4–11 contain only
real findings, not restated checklists.

```
# Performance Angular Review

## 1. Verdict
One of: PASS / PASS WITH NOTES / CHANGES REQUESTED
(One line of reasoning. CHANGES REQUESTED only when there is at least one MAJOR,
blocking, evidence-backed finding.)

## 2. Scope Reviewed
- Frontend files / components / services / specs inspected (the changed scope).
- Any context files read.

## 3. Performance Findings
For each finding:
- **Severity:** MAJOR (likely real runtime/render/network/memory/test-time issue before
  merge) / MINOR (useful improvement, not merge-blocking) / NOTE (watch item, future
  scaling concern, or measurement suggestion)
- **File / path & code area**
- **Why it affects** render time / change detection / memory / network / bundle size / test runtime
- **Evidence** from the diff or code path (quote the template/binding/subscription/request)
- **Suggested fix** (describe it; do not implement it)
- **Blocking?** yes / no
If none: "None."

## 4. Angular Rendering / Change Detection Check
Re-renders, expensive template expressions, methods called from templates, signal/computed/
effect cost, broad fan-out, OnPush relevance, URL/state-sync DOM updates, repeated
class/style bindings on large lists, Quran text rendering safety.
If nothing in scope: "Not applicable in this diff."

## 5. List Rendering / DOM Size Check
@for/track, stable keys, large lists (pagination/slicing/lazy/virtual scroll only when truly
large), nested list cost, ayah/word density, unnecessary nodes/wrappers.
If nothing in scope: "Not applicable in this diff."

## 6. RxJS / Signals / Lifecycle Check
Subscription/timer/router cleanup, takeUntilDestroyed/DestroyRef, duplicate-work effects,
stale callbacks after destroy, leak risks, state/URL hydration loops.
If nothing in scope: "Not applicable in this diff."

## 7. API / Network Efficiency Check
Duplicate calls, unnecessary reloads on URL/state change, cache key correctness, lazy detail
loading, over-fetching, repeated requests from tab/selection/navigation/effects, details
fetched only when needed.
If nothing in scope: "Not applicable in this diff."

## 8. Routing / Lazy Loading / Bundle Check
Route-level lazy loading, accidental eager imports, heavy code in common chunks, expensive
deps on hot paths, bundle/chunk concerns, theme/font/image loading if touched.
If nothing in scope: "Not applicable in this diff."

## 9. CSS / Animation / Visual Runtime Check
Expensive animations on repeated elements, hover/shadow on large lists, layout thrashing,
no transitions on Quran glyphs, reduced-motion, readability/contrast impact.
If nothing in scope: "Not applicable in this diff."

## 10. Frontend Test Runtime Check
Full-tree specs where shallow would do, repeated setup, fake timers/cleanup, unnecessary real
waits, runner/worker cost (respect VITEST_MAX_FORKS=2). Never weaken assertions/a11y/Quran
safety for speed.
If nothing in scope: "Not applicable in this diff."

## 11. Accessibility / Performance Tradeoff Check
Confirm no semantic buttons, focus-visible, aria, keyboard behavior, or readable text were
removed for performance; RTL usability preserved.
If nothing in scope: "Not applicable in this diff."

## 12. Quran Rendering Safety Performance Rule
State explicitly that performance improvements must never compromise: Quran text/glyph
readability, Quran text accuracy, selection/highlight semantics, RTL layout correctness,
accessibility of Quran-related actions, or reduced-motion safety for Quran content. Confirm
none of the findings above ask for such a trade-off (or flag it if the diff already makes one).

## 13. Final Recommendation
Whether the diff can proceed as-is, needs fixes first, or needs measurement/profiling before a
verdict can be trusted. One short, direct next step.
```

## Guardrails

- Be direct and practical; prefer fewer, well-evidenced findings over a long list.
- Do not invent costs. If the diff or file tree is unavailable, request it.
- If you cannot tell whether a component is hot or a list is large, say so and mark the
  finding NOTE with a measurement suggestion — do not inflate it to MAJOR.
- Never recommend trading Quran text readability, accuracy, selection semantics, RTL
  correctness, or accessibility for a render/visual micro-optimization.
- Do not implement fixes unless the user explicitly asks.
- This is a frontend performance skill only. General code quality, architecture, clean-code,
  Spec Kit compliance, UI style-system, and merge-readiness belong to `engineering-review`;
  visual/UX design belongs to `impeccable` / `frontend-design`; backend/database performance
  belongs to `performance-backend-review`; test-*code* quality belongs to `test-guard`. Stay
  in the frontend-performance lane.
