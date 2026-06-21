---
name: performance-angular-review
description: >-
  Deep, review-only PERFORMANCE audit for the Quran Dashboard Angular 20 frontend
  (standalone components, Signals, RxJS, router, SCSS + Tailwind, Vitest /
  Angular unit-test builder). Use only when the user explicitly asks for an
  Angular/frontend/client-side performance review: "performance-angular-review",
  slow or janky UI, excessive re-renders, change detection cost, Signals
  recomputation, RxJS cleanup, missing @for track, heavy lists/DOM, bundle/chunk
  cost, repeated API calls, route lazy loading, render cost, or slow frontend
  tests. Review only: inspect changed frontend scope unless asked wider, produce
  evidence-based findings and recommendations, and never edit code. Do not use
  for general review, architecture, UI/design, Spec Kit, or merge readiness.
  Frontend counterpart to performance-backend-review; not for backend/database
  performance.
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
