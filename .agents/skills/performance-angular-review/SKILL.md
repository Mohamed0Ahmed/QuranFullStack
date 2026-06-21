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

OpenCode / Codex pointer for the project skill. The canonical definition — the full
checklist, severity model, anti-noise rules, Quran-rendering-safety constraint, and the
exact 13-section output format — lives in the Claude Code skill:

`.claude/skills/performance-angular-review/SKILL.md`

Use that file as the authoritative checklist. In short: this is an **explicit-invocation,
review-only** Angular 20 frontend performance audit (change detection, Signals/RxJS,
list/DOM rendering, network efficiency, routing/lazy-loading/bundle, CSS/animation, and
frontend test runtime), weighted toward the performance-sensitive Mushaf reader and Quran
study UI under `src/app/features/mushaf/`. Inspect only the changed frontend scope unless a
wider audit is requested. Produce evidence-based findings, severities, and recommendations
only — do not modify code, and never recommend changes that animate Quran glyphs/text,
reduce readability, break selection/RTL semantics, or compromise Quran display accuracy or
accessibility. Return the review using the output format defined in the canonical skill.
