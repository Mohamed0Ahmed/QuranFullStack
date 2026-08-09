---
name: engineering-review
description: Use when explicitly asked for the Quran Dashboard formal engineering review of code, a diff, branch, PR, phase, or completed implementation.
---

# Engineering Review

## Responsibility

Produce the explicitly requested formal review — findings and a verdict — for the
requested scope (.NET backend and/or Angular frontend), judged against the canonical
project rules. Review the actual changed content, including untracked files that are
part of the requested scope. The severity meanings, verdict contract, and review cadence
below are fixed; compacting this skill changes none of them.

**Not this skill's job:** implementing fixes or refactors, running builds or tests,
Git/PR/deployment actions, dependency or performance audits (separate explicit skills),
reviewing without being asked, loading a whole policy document when only a heading is
implicated, or invoking another project Skill — including `test-guard`.

Git tracking/staging state is never a finding, severity item, or verdict input. If Git
concerns are noticed, mention them only in an optional `Commit workflow reminder` line
outside the verdict.

## Evidence, not execution

Consume evidence that already exists in the conversation or that the user supplies;
report missing, stale, skipped, or unknown evidence honestly. Never generate new
build/test evidence and never assume success — unknown is unknown.

**Test Guard evidence:** when the diff changes test files, a current same-diff
`test-guard` result is required review evidence. If one exists, consume it — the final
verdict must account for it, and contract-critical weak tests or tests that can pass
while the required behavior is broken prevent PASS. If none exists, report the required
Test Guard evidence as **missing/incomplete** in Findings and the verification check. Do
not invoke `test-guard` or re-run its rules yourself; the caller decides whether to
invoke it separately, and this review keeps the final verdict.

## Conditional context (exact headings, only when implicated)

Always: the actual diff/content under review, plus the active spec/contract when the
work is contract-bound.

- **Clean code / naming / comments / SOLID / DRY / focused scope:** only the implicated
  headings of `CODING_PRINCIPLES.md` §§2–4 and §7 (§2 `Comment Policy` is canonical for
  comments, including its production-source-only scope boundary). For AI-specific
  patterns: `references/clean-code-guard/ai-failure-modes.md`; for a structured deep
  pass: `references/clean-code-guard/review-checklist.md` (optional traversal aid).
  Project overrides recorded in `CODING_PRINCIPLES.md` §2 (C# `I`-prefixed interfaces;
  the `ApiResponse` envelope at the API boundary) win over generic guidance.
- **Backend architecture:** the implicated headings of
  `Backend/.architecture/BACKEND_STRUCTURE.md` (placement; §File Size and Responsibility
  Guidelines for thresholds), `CLEAN_ARCHITECTURE.md` (layering and dependency
  direction), and `API_GUIDELINES.md` (§5 Response Shape for the `ApiResponse` envelope
  and API errors/localization).
- **Security/auth:** `docs/contracts/security-access.md` and
  `Backend/.architecture/API_GUIDELINES.md` §11 Security and Safety — mandatory whenever
  auth, identity, authorization, or session behavior changed; no summary substitutes for
  those owners.
- **Frontend architecture:** the implicated headings of
  `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md` (structure,
  routeable pages, §File Size and Responsibility Guidelines),
  `UI_STYLE_SYSTEM.md` (tokens/`qd-` classes/RTL/themes; §13 Quranic Data Display Safety
  for Quran-facing UI), and `API_INTEGRATION_GUIDELINES.md` (page → facade/store → API
  service flow, `ApiResponse<T>`, loading/error/empty states); `PRODUCT.md`/`DESIGN.md`
  only when product/design decisions are involved.
- **Testing evidence classification:** the exact `TESTING_STRATEGY.md` headings, loaded
  only when classifying existing verification evidence for the changed scope — §5
  (execution-trigger matrix: which lanes the changed scope requires; do not demand more
  than it does), §1 (what counts as a lane), §3.3 (shard reporting), §3.4 and §9
  (canonical-resource preflight and failure/skip semantics), §6 (the route-smoke gate
  and the Smoke data tier's lane), §8 (CI absence). Classify the supplied evidence
  through those headings; do not restate their rules.
- **Quran data safety:** `CODING_PRINCIPLES.md` §10 and
  `references/quran-data-safety.md` — whenever source-sensitive data or Quran rendering
  is in scope. This is the highest-priority safety area; violations are BLOCKING or
  MAJOR by impact.
- **Spec Kit changes:** `SPEC_KIT_IMPLEMENTATION_REVIEW.md` plus the feature's
  `specs/<feature>/` artifacts — only when the change was implemented from Spec Kit
  (the request mentions a Phase, User Story, task IDs, or `specs/<feature>/` paths).
- **Terminology:** neutral terms per `SKILLS_AND_ARCHITECTURE_GUIDE.md` §Review
  terminology ("overloaded service", "oversized service" — never "God service").

If a referenced document is missing or unavailable, state that in the output rather than
inventing its rules.

## Severity

- **BLOCKING** — must fix before merge/start/continue.
- **MAJOR** — should fix soon; risky but not necessarily blocking.
- **MINOR** — cleanup or clarity improvement.
- **NOTE** — observation only.

Separate findings by severity; do not inflate it, and do not request broad refactors
unless necessary.

## Output

# Engineering Review

1. **Verdict** — PASS / PASS WITH NOTES / CHANGES REQUESTED / BLOCKED.
2. **Scope reviewed** — files reviewed and the headings/documents consulted.
3. **Spec Kit / task compliance** — only for Spec-Kit-based changes (per the add-on);
   otherwise omit.
4. **Findings** — per finding: severity, file/path, issue, why it matters, suggested
   direction (not implemented). Include threshold and architecture/responsibility
   findings here. "None." when clean.
5. **Quranic data safety check** — PASS / CONCERN / NOT APPLICABLE, one line.
6. **Verification check** — supplied evidence versus the lanes `TESTING_STRATEGY.md` §5
   requires for the changed scope (sufficient / insufficient / stale / missing), and the
   Test Guard evidence status when test files changed.
7. **Final recommendation** — one direct next step consistent with the verdict.

Optional, outside the verdict: `Commit workflow reminder` — only if Git
tracking/staging concerns were noticed; it never affects findings or the verdict.
