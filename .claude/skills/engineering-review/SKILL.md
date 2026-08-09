---
name: engineering-review
description: Use when explicitly asked for a formal Quran Dashboard engineering review at any boundary, for the final review of a completed feature/change, or when the user explicitly names the engineering-review Skill.
---

# Engineering Review

## Responsibility

Produce the explicitly requested formal review — findings and a verdict — for the
requested scope (.NET backend and/or Angular frontend), judged against the canonical
project rules. The severity meanings, verdict contract, and review cadence below are
fixed; compacting this skill changes none of them.

Normal cadence is one formal review at the completed feature/change boundary, after
implementation has produced the fresh cumulative-final-diff evidence. A user may
explicitly request a formal review at any earlier boundary; that is a deliberate
override and uses this same contract. A generic narrow request to review one phase,
task, fix, or file set belongs to `focused-review`, not to this skill.

**Not this skill's job:** implementing fixes or refactors, running builds or tests,
Git/PR/deployment actions, dependency or performance audits (separate explicit skills),
reviewing without being asked, loading a whole policy document when only a heading is
implicated, or invoking another project Skill — including `test-guard` and
`focused-review`.

The reviewer must not fix its own findings. Fixes are separate, explicitly requested
implementation work; after fixes settle, finding closure happens through the re-review
path below.

Git tracking/staging state is never a finding, severity item, or verdict input. If Git
concerns are noticed, mention them only in an optional `Commit workflow reminder` line
outside the verdict.

## Initial formal review

1. Establish the base and the complete cumulative current diff/content, including
   generated and in-scope untracked files.
2. Read the active plan/spec/contracts before choosing any specialist context.
3. Inspect the full relevant final diff against those requirements and repository
   truth.
4. Consume the supplied final evidence and classify it through only the implicated
   `TESTING_STRATEGY.md` headings.
5. Load specialist policy/reference context only after the diff or a concrete
   candidate finding implicates it.
6. Report the seven-section formal output and verdict below.

## Evidence, not execution

Consume evidence that already exists in the conversation or that the user supplies;
never execute `TESTING_STRATEGY.md` §5 or its commands, never generate new build/test
evidence, and never assume success — unknown is unknown.

- Classify evidence through only the implicated headings among `TESTING_STRATEGY.md`
  §§1, 2.1, 3–6, 8, and 9.
- Report required evidence as sufficient, stale, missing, failed, unexpectedly
  skipped, or unknown. Do not claim PASS with deficient required final evidence.
- **Test Guard evidence:** consume a current same-diff `test-guard` result when
  supplied — the final verdict must account for it, and contract-critical weak tests
  or tests that can pass while the required behavior is broken prevent PASS. Report
  Test Guard evidence as missing only when the active plan/spec/contract explicitly
  requires that separate evidence; changed tests alone authorize no invocation and no
  hidden Test Guard stage.

## Finding identity and review state

- In **Scope reviewed**, record whether this is an initial review or a re-review, and
  the reviewed base/current-state identity.
- Give initial findings stable IDs (`ER-1`, `ER-2`, ...). A re-review retains each
  prior ID and marks it `CLOSED`, `OPEN`, or `REGRESSED`; new findings receive new
  IDs. These are finding states, not a new verdict taxonomy.
- No persisted reviewer-state artifact is created. Prefer the same reviewer session;
  otherwise the caller supplies the prior report, the original base/scope, and the
  current state.

## Re-review of formal findings

After a formal review, separate implementation fixes the selected findings, and after
the fixes settle it recomputes and runs the whole fresh final evidence once. Then:

1. Re-review inspects the original findings, the changed behavior, regressions
   reasonably introduced by the fixes, and the fresh final evidence — then issues a
   new verdict through the same output contract.
2. Report each original finding `CLOSED`, `OPEN`, or `REGRESSED`, plus any new finding
   under a new ID.
3. Reread only what a changed finding, a plausible regression, fresh evidence, a newly
   implicated owner, or lost continuity requires. Loading one newly implicated exact
   owner is the smallest safe escalation.
4. Continuity never permits stale evidence or hidden new scope.

Use a fresh full formal review instead of this reduced path when scope materially
expanded, unrelated code changed, the base/plan/spec/contract changed, fixes
introduced a new unreviewed safety area, continuity cannot be established, or explicit
risk requires it. If the prior report, original base/scope, or current state cannot be
supplied, run a full formal review when its inputs exist; otherwise report `BLOCKED`.

## Conditional context (exact headings, only when implicated)

Always: the actual cumulative diff/content under review, plus the active
plan/spec/contract when the work is contract-bound.

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
  and API errors/localization). Backend-only scope does not load Frontend or style
  context.
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
  only when product/design decisions are involved. Frontend-only scope does not load
  Backend or database context.
- **Testing evidence classification:** the exact `TESTING_STRATEGY.md` headings among
  §§1, 2.1, 3–6, 8, and 9, loaded only when classifying supplied verification evidence
  for the changed scope — for example §5 (execution-trigger matrix: which lanes the
  changed scope requires; do not demand more than it does), §1 (what counts as a lane),
  §3.3 (shard reporting), §3.4 and §9 (canonical-resource preflight and failure/skip
  semantics), §6 (the route-smoke gate and the Smoke data tier's lane), §8 (CI
  absence). Classify the supplied evidence through those headings; do not restate their
  rules. Test-code references load only when changed tests materially bear on a
  requirement, one stack only.
- **Quran data safety:** `CODING_PRINCIPLES.md` §10 and
  `references/quran-data-safety.md` — whenever source-sensitive data or Quran rendering
  is in scope. This is the highest-priority safety area; violations are BLOCKING or
  MAJOR by impact.
- **Spec Kit changes:** `SPEC_KIT_IMPLEMENTATION_REVIEW.md` plus the feature's
  `specs/<feature>/` artifacts — only when the change was implemented from Spec Kit
  (the request mentions a Phase, User Story, task IDs, or `specs/<feature>/` paths).
- **Terminology:** neutral terms per `SKILLS_AND_ARCHITECTURE_GUIDE.md` §Review
  terminology ("overloaded service", "oversized service" — never "God service").

If a referenced document is missing or unavailable, state that in the output rather
than inventing its rules.

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
2. **Scope reviewed** — files reviewed and the headings/documents consulted; initial
   review or re-review; the reviewed base/current-state identity.
3. **Spec Kit / task compliance** — only for Spec-Kit-based changes (per the add-on);
   otherwise omit.
4. **Findings** — per finding: stable ID, severity, file/path, issue, why it matters,
   suggested direction (not implemented). On re-review, each prior ID marked `CLOSED`,
   `OPEN`, or `REGRESSED`. Include threshold and architecture/responsibility findings
   here. "None." when clean.
5. **Quranic data safety check** — PASS / CONCERN / NOT APPLICABLE, one line.
6. **Verification check** — supplied evidence versus the lanes `TESTING_STRATEGY.md` §5
   requires for the changed scope (sufficient / insufficient / stale / missing), and
   the Test Guard evidence status when the active plan/spec/contract requires that
   separate evidence.
7. **Final recommendation** — one direct next step consistent with the verdict.

Optional, outside the verdict: `Commit workflow reminder` — only if Git
tracking/staging concerns were noticed; it never affects findings or the verdict.
