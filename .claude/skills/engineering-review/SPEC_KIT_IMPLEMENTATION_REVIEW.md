# Spec Kit Implementation Review (Add-on Rules)

These rules **extend** the Engineering Review checks; they do not replace them. Apply
them **only** when the reviewed change was implemented from Spec Kit artifacts (see
the trigger conditions in `SKILL.md`). For a simple, non–Spec-Kit change, ignore
this file entirely and review the code normally.

The point of these checks is trust: when work is built from a spec, the diff alone
is not enough. A change can be clean, well-typed, and still be *wrong* because it
implemented the wrong phase, skipped a required task, quietly did something the spec
deferred, or drifted from a contract that is supposed to be the single source of
truth. So here you review the code **against the spec, tasks, phase scope, and
contracts** — not just against good engineering taste.

## Before You Start: Establish the Baseline

You cannot judge scope without knowing what was asked. First pin down:

1. **Which feature** — the `specs/<feature>/` directory under review (e.g.
   `specs/001-layout-foundation/`).
2. **Which phase / tasks were requested** — read it from the user's request
   ("implemented Phase 3", "did T013–T018", "User Story 2"). If the request is vague,
   infer the intended scope from `tasks.md` and **state the assumption** you made.
3. **What the artifacts say** — read `spec.md` (requirements, Locked Decisions, Out of
   Scope), `plan.md` (technical context, structure, gates), `tasks.md` (the exact task
   IDs and their file paths), the relevant files under `contracts/`, and
   `quickstart.md` when acceptance/verification is in question.

`tasks.md` is the spine: each task lists an **exact file path**, a phase, and often a
User Story tag (US1–US5). That mapping is what makes the checks below objective rather
than a matter of opinion.

## What to Check

### 1. Phase / Task Scope Compliance

The core question: *did the implementation do exactly the requested phase/tasks — no
more, no less?*

- **Only the requested scope?** Confirm the change covers the phase/tasks that were
  asked for.
- **Future work leaked in?** Flag anything that implements tasks or behavior belonging
  to a *later* phase. Building ahead is not a favor here — it bypasses the phase gates
  the plan relies on (see Scope Creep below).
- **Anything skipped?** Flag required tasks in the requested phase that are missing or
  only partially done. A phase reported "done" with a missing task is a finding.

### 2. Task-to-Code Traceability

Make the mapping explicit so a reader can audit it:

- **Tasks completed** — list each task ID you can confirm was implemented.
- **Task → file mapping** — for each task, the file(s) that fulfill it. Compare against
  the exact path `tasks.md` names for that task; a task implemented in the *wrong* file
  is a finding.
- **Unexplained files** — list changed files that map to **no** task in the requested
  phase. Some are legitimate (e.g. the pointer/checkbox bookkeeping commit); the rest
  need a justification or they are scope creep.

### 3. Spec & Locked-Decisions Compliance

- **Locked Decisions respected?** `spec.md` records decisions the implementer must not
  re-litigate (exact strings, groupings, placeholder text, theme behavior, etc.).
  Verify the code matches them exactly — wording and structure included.
- **Out-of-Scope items honored?** Anything under the spec's **Out of Scope** section
  must not appear in the implementation.
- **Deferred behavior?** Flag behavior the spec explicitly pushes to a later phase/story
  that shows up early.

### 4. Contract Compliance

When a contract under `contracts/` is relevant to what changed, compare the
implementation against the contract file directly — not against memory:

- **API endpoints changed** → compare routes, verbs, status codes, and payloads against
  the matching `contracts/api-*.md` (e.g. `api-health.md`, `api-dashboard-info.md`).
- **Navigation changed** → compare against `contracts/ui-navigation.md`.
- **Design tokens / styling changed** → compare against `contracts/ui-design-tokens.md`
  (token names, OKLCH/parchment values, no raw `#fff`/`#000` where the contract forbids
  it).
- **API response shape changed** → compare against `contracts/api-response-envelope.md`
  (the `{ IsSuccess, Message, Data, Errors }` envelope and its `Ok`/`Fail` helpers).

For each relevant contract, state explicitly whether the implementation **matches**,
**deviates** (with the specific difference), or the contract is **not applicable** to
this change.

### 5. Acceptance Criteria & Quickstart

- **Acceptance criteria met?** Check the implementation against the acceptance criteria
  for the reviewed phase/story (in `spec.md`, and the manual steps in `quickstart.md`).
- **Verification reported?** This project verifies by build (`dotnet build`,
  `npm run build`) plus the manual `quickstart.md` checklist — there is no automated
  test suite this phase. Report whatever build/manual evidence was provided.
- **Verification skipped?** If build or manual verification was not run or not reported,
  say so plainly. Do not assume success; unknown is unknown.

### 6. Scope Creep (Premature Future Work)

Implementing later-phase work early is a real defect, not a bonus, because it
sidesteps the phase ordering and gates the plan depends on. Rate it by impact:

- **MAJOR** when it adds behavior or surface area beyond the phase but is contained and
  reversible.
- **BLOCKING** when it changes contracts, routing, or data behavior that later phases
  are supposed to own, or when it could mask a missing dependency.

> **Example:** adding the `**` wildcard route in Phase 3 when the wildcard belongs to
> Phase 4 (US2, task T021) is premature — it implements a later task's behavior under
> an earlier phase and should be flagged.

### 7. Source of Truth

When a contract names a **single source of truth**, verify the implementation actually
derives from it instead of hardcoding a parallel copy that can drift.

> **Example:** `contracts/ui-navigation.md` declares `core/navigation/nav-items.ts`
> (the `NavItem[]` config) the single navigation source of truth. Navbar links, the
> «المزيد» dropdown grouping, routes, and labels must be **driven by** that config —
> not duplicated as hardcoded markup in the navbar template. A hardcoded parallel list
> is a finding even if it currently renders identically.

## Quranic Data Safety Still Applies

Spec Kit review does **not** relax the Quranic data safety checks in `SKILL.md`. Specs
explicitly forbid fabricated stats, ayah text, or counts (e.g. overview cards with no
invented numbers); treat any fabricated or silently "corrected" source-sensitive data
as BLOCKING/MAJOR exactly as the main skill requires.

## How This Feeds the Output

Findings from these checks use the same severity levels (BLOCKING / MAJOR / MINOR /
NOTE) and flow into the normal **Findings** section. In addition, populate the
**Spec Kit / Task Compliance Check** section of the Review Output Format in `SKILL.md`
with the traceability summary (feature, phase/tasks, completed, skipped, future-work,
contract summary, scope verdict). If a change turns out not to be Spec-Kit-based after
all, write `Not applicable — review was not based on Spec Kit artifacts.` there.
