# Performance Angular Review

Use the project Angular performance review skill as the checklist:

.claude/skills/performance-angular-review/SKILL.md

Explicit-invocation, review-only Angular 20 frontend performance audit. Inspect only the
changed frontend scope unless a wider audit is asked for. Cover rendering / change detection,
list rendering and DOM size (@for/track, large word/ayah lists), RxJS / Signals / lifecycle
cleanup (subscriptions, timers, takeUntilDestroyed/DestroyRef), API/network efficiency
(duplicate requests, lazy detail loading), routing / lazy loading / bundle cost,
CSS/animation runtime, and frontend test runtime (respect the VITEST_MAX_FORKS=2 cap).
Weight findings toward the Mushaf reader and Quran study UI under src/app/features/mushaf/.

Evidence-based findings only — no speculative micro-optimizations. Never recommend changes
that animate Quran glyphs/text, reduce readability, break selection/RTL semantics, or
compromise Quran display accuracy or accessibility. Review only: do not implement fixes
unless explicitly asked. Return the review using the output format defined in the skill.
