---
name: performance-angular-review
description: Use when explicitly asked for an Angular/frontend performance review or when a Quran Dashboard UI path is reported as slow, janky, memory-heavy, or request-heavy.
---

# Performance Angular Review

## Responsibility

Evidence-based, review-only performance findings for the changed or reported Angular
frontend scope: rendering and change detection (Signals/computed/effect recomputation,
expensive template expressions, methods called from templates, re-render fan-out, OnPush
relevance), list rendering and DOM size (`@for` `track` and key stability, nested lists,
node density — the Mushaf reader renders large word/ayah lists and weighs heaviest),
RxJS/lifecycle cleanup and leak risks, frontend network efficiency (duplicate calls,
reload loops, cache-key correctness, lazy detail loading, over-fetching),
routing/lazy-loading/bundle cost, CSS/animation runtime cost, and measured frontend test
runtime.

**Not this skill's job:** general engineering, architecture, accessibility, or
test-code-quality review; speculation without evidence; executing, mutating, or fixing
anything; or invoking another Skill. Explicit invocation only — the word "review" alone
never selects it, and backend/database performance belongs to
`performance-backend-review`.

## Evidence rules (anti-noise)

Every finding points at a real code path in the changed scope with a plausible cost.
Severity scales with real heat: a missing `track` on a small static list is a NOTE; on a
large frequently-rebuilt list it is MAJOR. Do not demand caching, virtual scroll, or
OnPush without evidence of repeated cost or a genuinely heavy/hot component. When impact
is uncertain, mark it NOTE and recommend a measurement (profiler, bundle stats, timed
run) rather than asserting a problem. Fewer, well-evidenced findings beat a long list —
a false positive erodes trust in every other finding.

## Quran rendering correctness overrides optimization (hard constraint)

Readability, accuracy, selection/highlight semantics, RTL correctness, and accessibility
of Quran content always win over a render or visual micro-optimization. Apply
`.claude/skills/engineering-review/references/quran-data-safety.md` when the scope
touches Quran display; a recommendation that trades any of it away is itself the defect
— never propose it, and flag it if the diff already does it. "Slower but correct and
readable" is the right answer for Quran content.

## Conditional context

- `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md` and
  `API_INTEGRATION_GUIDELINES.md` — only the exact heading a finding depends on, plus the
  directly implicated Angular templates/styles when a recommendation depends on current UI
  implementation (so it does not break structure, rendering, or the API flow).
- `Frontend/quran-dashboard-ui/e2e/README.md` — only when a retained Playwright journey or its
  browser/runtime prerequisites are relevant to the finding.
- Browser/profiler measurement — only when a finding requires it, and read-only.
- Immediate parent/child components and the relevant facade/state service — only to
  judge whether a component is hot or a list is large; do not drift into auditing
  untouched features.

## Output

1. **Verdict** — PASS / PASS WITH NOTES / CHANGES REQUESTED (the last only with at
   least one MAJOR, evidence-backed finding), with one line of reasoning.
2. **Scope reviewed** — changed files/components inspected and context consulted.
3. **Findings** — per finding: severity (MAJOR / MINOR / NOTE), file/path, why it
   affects render/memory/network/bundle/test time, the evidence (quote the
   binding/subscription/request), suggested direction (never implemented). "None." when
   clean.
4. **Quran rendering safety** — confirm no finding trades away Quran readability,
   accuracy, selection/RTL semantics, or accessibility (or flag the diff if it already
   does).
5. **Next step** — proceed, fix first, or measure before trusting a verdict.
