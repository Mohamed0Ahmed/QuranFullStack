---
name: speckit-phase-loop
description: >-
  Use when the user asks to implement all or remaining phases of the active
  Spec Kit feature, run its tasks phase by phase, or delegate a multi-phase
  Spec Kit implementation. Not for a single task, one-off code change,
  or review-only work.
compatibility: >-
  Requires `.specify/feature.json`, `specs/<feature>/tasks.md`, a Git working tree, the Agent +
  SendMessage + Bash tools, and the `speckit-implement`, `engineering-review`, and
  `commit-workflow` skills.
---

# Spec Kit Phase Loop

## Why this exists

Running a whole `tasks.md` in one shot produces a giant unreviewed diff, and re-spawning a fresh
reviewer after every fix never converges — a new reviewer invents new findings forever. This skill
fixes both by making the phase the unit of work and keeping agent identity stable across the fix
loop.

The main session is the orchestrator. It does not write feature code. Its jobs are: reconcile phase
state, decide the split, spawn agents, judge verdicts, run the final verification, own `tasks.md`,
and own commits.

## Roles and boundaries

| Role | Who | Runs | May write |
|---|---|---|---|
| Orchestrator | main session | this skill | `tasks.md` checkboxes, commits |
| Implementer | subagent | `speckit-implement` | feature code, tests, migrations |
| Reviewer | subagent | `engineering-review` | nothing — findings only |

Three boundaries make the loop trustworthy; keep them even under time pressure:

- **The reviewer never patches code.** `engineering-review` is review-only by its own contract. A
  reviewer that fixes what it found can no longer judge it.
- **Implementers never touch `tasks.md`.** They report completed task IDs; the orchestrator marks
  the checkboxes. This is a deliberate override of `speckit-implement`'s own step ("mark the task
  off as `[X]`") — say so in the agent prompt, or the agent will follow its skill and mark them.
  Two reasons: parallel agents clobber each other's writes to the same file, and a checkbox marked
  before the gates run would claim "done" for work that review or verification later rejects.
- **`tasks.md` is otherwise read-only.** No adding, rewording, reordering, splitting, or deleting
  tasks. A task that looks wrong or is blocked is a signal to stop and report, not to edit the plan.

## Step 0 — Resolve the feature and build the phase plan

Read `.specify/feature.json` → `feature_directory` (e.g. `specs/030-abwab-relationships-templates`).
That file is the single source of truth for which feature is live; never take the feature from the
branch name or from memory.

Read `<feature_dir>/tasks.md` and extract, per `## Phase N:` heading: the task IDs, the file paths
each task names, and any explicit ordering or `[P]` parallel markers. Also read the prose preamble
of `tasks.md` — Spec Kit task files carry a "how to use this file" contract that constrains the
implementer, and it must be forwarded to the agents.

### Reconciling phase state

A checkbox is a claim, not proof. A session can die between marking `[X]` and committing, so treat
completion as a two-part fact:

```text
A phase is complete only when:
1. all of its tasks are [X], and
2. a matching phase commit exists whose diff includes the phase task
   checkbox updates.
```

- **Both true** → the phase is done; skip it.
- **Neither true** → the phase is pending; run it normally.
- **Exactly one true** → the phase was interrupted, and you must not guess which way. Inspect
  `git log` for a commit touching this phase's files, and inspect staged, unstaged, and untracked
  changes, to establish whether work stopped before or after the commit. Resume on your own only
  when the evidence is unambiguous — e.g. tasks `[X]`, no matching commit, and a working tree
  containing exactly this phase's files means: review from the phase base, verify, then commit. A
  matching commit with unchecked tasks usually means the checkbox write was lost; confirm the
  commit's diff really carries the phase's implementation before repairing the checkboxes. When the
  evidence is ambiguous, or the tree holds anything you cannot attribute, stop and report the
  inconsistency instead of resolving it by assumption.

Apply the same reconciliation to a partially-checked phase: read the Git evidence first and resume
yourself when it is clear. Asking the user before looking wastes their time; asking once the
evidence stays ambiguous is the right call.

`tasks.md` stays owned exclusively by the orchestrator throughout.

### Phase table

Show the user the phase table (phase, task IDs, task count, split decision, reconciled status), then
continue immediately unless:

- the user explicitly asked for only a subset of the phases;
- a phase's state is inconsistent per the reconciliation above;
- task ownership or a split boundary is genuinely ambiguous.

A clean run does not need a confirmation gate — the per-phase reports keep the user informed as it
goes, and each phase ends in a reviewable commit.

## Step 1 — Phase preflight

Run this before dispatching any implementer for the phase:

```bash
git status --short
git rev-parse HEAD     # → phase_base_sha
```

The tree should be clean, because the previous phase ended in a commit. `phase_base_sha` is what
makes the rest of the phase auditable — review scope, the scope audit, and commit verification all
measure against it, and none of them work if you never captured it.

If the tree is not clean, establish who owns the changes before touching anything. Changes that
belong to an interrupted run of *this* phase can be resumed (Step 0's reconciliation). Anything
else — a user's own edit, leftovers from another branch, stray tooling output — is not yours:
never discard, reset, stash, check out over, or quietly absorb it into the phase, and stop and
report when ownership is unclear. A phase that starts on top of unexplained changes produces a
commit nobody can trust, and that is usually discovered long after it can be cheaply undone.

Record for the phase, so the later gates have something concrete to check against:

- the phase's task IDs;
- the file paths those tasks name;
- the split decision (Step 2);
- any directly required supporting files a task implies without naming — a migration, a DI or
  registry entry, the nearest `README.md` for a touched area.

## Step 2 — Decide the split (orchestrator judgment, not a rule)

Default to **one implementer per phase**. One agent holds the whole phase's context, and a phase is
usually small enough that a second agent adds coordination cost without adding speed.

Split into two agents only when the phase's task file-sets are genuinely disjoint. The reliable
case in this repo is a **Backend / Frontend** cut, or two independent workstreams that `tasks.md`
itself declares parallel. Before splitting, confirm:

- no file appears in both agents' task sets, including migrations and shared conflict-code files
  (`tasks.md` often flags these as "the only shared-file points" — those tasks are not splittable);
- neither half needs the other's output to compile or to run its tests;
- tests and their implementation stay with the **same** agent — Spec Kit phases are written
  tests-first-must-fail, and splitting that across agents destroys the red→green signal.

If any of those fail, run one agent. When in doubt, one agent: a serial phase that lands is worth
more than a parallel phase that conflicts.

Two agents go in a single message so they run concurrently; wait for both before reviewing. One
agent runs with `run_in_background: false` so the loop blocks on it naturally.

In a split phase, implementer test evidence is provisional because the other implementer may still
be modifying the working tree. Final verification (Step 6) begins only after both implementers have
returned and all phase changes are present together — that is the first moment the tree represents
the phase as a whole, and two individually-green halves can still fail combined.

## Step 3 — Implement

Spawn with `subagent_type: general-purpose`. Prompt template:

```
Implement ONLY Phase <N> ("<phase title>") of the Spec Kit feature at <feature_dir>.
Your tasks: <task IDs>. Files in scope: <paths>.

Use the `speckit-implement` skill (Skill tool) to do this work — it carries the project's
implementation contract. Restrict yourself to the tasks listed above.

Overrides for this run (these beat speckit-implement's defaults):
- Do NOT edit tasks.md at all — not even to mark [X]. Return the completed task IDs instead;
  the orchestrator marks the checkboxes once review and final verification have passed.
- Do NOT touch tasks from other phases, and do NOT commit, stage, push, or open a PR.
- If a task is blocked, ambiguous, or appears wrong, STOP and report it. Do not reinterpret or
  rewrite the task.

Read <feature_dir>/tasks.md's "how to use this file" preamble first — it is the binding contract
for this feature. Also follow the repo's CLAUDE.md files and CODING_PRINCIPLES.md.

Read the nearest README.md files governing every area you modify. If the implementation changes a
documented behavior, boundary, or invariant, update that README — but only when the phase task or
the required behavior justifies it. No unrelated documentation cleanup.

Before returning, run the relevant build and tests and report the actual command output. Do not
report success you have not observed.

Return: completed task IDs, every file created/modified including untracked ones, migration names
if any, build/test evidence, and anything you deliberately left undone with the reason.
```

## Step 4 — Review

Establish the change set yourself before spawning the reviewer:

```bash
git status --short
git diff --stat <phase_base_sha>
```

The phase scope is **everything since `phase_base_sha`** — tracked changes, staged and unstaged
alike, plus untracked files. Do not describe it as `git diff HEAD`: that misses staged work and
every new file, and an implementer that forgets to mention a file it created will not be caught by
anyone else if you take its report at face value. Reconcile the report against what Git actually
shows, and hand the reviewer the real file list.

```
Review the Phase <N> ("<phase title>") change set of <feature_dir>.
Scope: all changes since <phase_base_sha> — `git diff <phase_base_sha>` for tracked changes, plus
staged, unstaged, and untracked files. Changed files: <list>. Tasks implemented: <task IDs>.

Use the `engineering-review` skill (Skill tool). Check the work against <feature_dir>/spec.md,
plan.md, tasks.md, quickstart.md (when present), and contracts/; against the nearest README.md for
every area touched; and against the repo's CLAUDE.md/AGENTS.md and CODING_PRINCIPLES.md.

This is review-only: report findings, do not modify any file.

Return findings grouped by severity (critical / major / minor), each explicitly marked actionable
or non-actionable, with file:line and a concrete fix for the actionable ones, plus an overall
verdict. If nothing is wrong, say so plainly rather than manufacturing findings — a clean phase is
an acceptable result.
```

## Step 5 — Fix loop

```text
Only actionable critical, major, and minor findings block.

Purely informational and explicitly non-actionable notes do not block.
A note must never be used to hide a correctness, contract, security,
Quran-data-safety, scope, or test problem.
```

Minor still blocks — the point of the gate is that the phase lands clean, not that it lands fast,
and findings are not deferred to a backlog. The actionable/non-actionable split exists only so the
loop is not held hostage by observations no fix would resolve ("this area could use a follow-up
refactor one day"). A `PASS WITH NOTES` verdict continues only after you have read every note and
found each genuinely non-actionable; a note that describes something you would change in the code
is a finding wearing a different label, and it blocks.

1. `SendMessage` the findings to the **same implementer agent** (by ID). It still holds the context
   for why the code is shaped that way, so it fixes faster and with fewer regressions than a fresh
   agent. Tell it again: no `tasks.md`, no commits.
2. `SendMessage` the **same reviewer agent** to re-verdict — not a new one. The original reviewer
   knows what it flagged and can confirm resolution; a fresh reviewer starts from zero and produces
   a new finding set, so the loop never terminates.
3. Repeat until the reviewer reports no actionable findings, capped at **3 rounds** — a failed
   final verification (Step 6) consumes a round too, since it sends work back to the implementer.

`SendMessage` may need loading first: `ToolSearch("select:SendMessage")`.

If round 3 still has findings, stop the loop and hand the state to the user: what remains, which
severity, what was tried. Grinding a fourth round on a disagreement between two agents burns
context without converging — that is a decision for the user, not for more rounds.

If a fix round makes the implementer report that the finding is wrong, do not just relay it back and
forth. Read the code yourself and adjudicate; you are the orchestrator.

## Step 6 — Final verification (fresh, against the final tree)

A clean review is not proof the phase works. The implementer's test evidence was produced *before*
the fix rounds, and in a split phase before the other half even landed — so by the time the
reviewer signs off, that evidence describes a tree that no longer exists. Evidence produced before
the most recent code change is stale and cannot close a phase.

Derive the verification commands from the phase tasks, `plan.md`, `quickstart.md`, the affected
Backend/Frontend areas, and the implementation evidence — then run them yourself against the final
working tree. Targeted and relevant beats exhaustive: the point is that the commands you run
observe the code as it now stands.

If verification fails:

1. send the exact failure output to the same implementer;
2. leave `tasks.md` untouched — nothing is complete;
3. return to the same reviewer for another verdict once fixed;
4. run fresh verification again;
5. count it as another fix round against the cap of 3.

Only this combination closes a phase:

```text
zero actionable review findings
+ fresh passing verification
+ clean scope audit
```

Until all three hold, the phase is not complete — do not say it is.

## Step 7 — Scope audit, mark tasks, commit, verify the commit

**Scope audit first.** List every staged, unstaged, and untracked changed file since
`phase_base_sha`, and account for each one: it is either named by a phase task or is a directly
required supporting file you recorded in Step 1. Anything unexplained blocks the commit — report it
rather than sweeping it in. A user's unrelated edit swept into a phase commit is both a
correctness risk and a mess to unpick later.

Then, and only then:

1. Mark this phase's task IDs `[X]` in `tasks.md`. The checkbox now means *implemented,
   review-passed, and verified*, which is what makes Step 0's reconciliation trustworthy.
2. Commit via the `commit-workflow` skill — **exactly one commit per phase**, carrying the
   implementation, every fix-round change, and the `tasks.md` checkbox update together. Match the
   repo's existing message style, e.g.
   `feat(030): phase 3 US1 — category relationships vertical slice`.
3. Verify the commit rather than assuming it:

   ```bash
   git log --oneline <phase_base_sha>..HEAD
   git show --stat HEAD
   git status --short
   ```

   Confirm exactly one new commit exists since `phase_base_sha`, that it contains the phase's
   `tasks.md` checkbox updates and all intended implementation and fix-round changes, and that the
   working tree is now clean. Capture the SHA and subject for the phase report.

   More than one new commit means the one-commit-per-phase contract is already broken. Stop and
   report — repairing history is the user's call, not something to paper over by continuing.

4. Stop at the commit. No push, no PR — those are separate, explicitly-requested actions.

Then move to the next phase, starting again at Step 1.

## When to stop and ask

Stop the loop and hand back to the user when:

- an implementer reports a blocked, ambiguous, or apparently-wrong task;
- round 3 still has open actionable findings or failing verification;
- phase state stays ambiguous after the Step 0 reconciliation, or the preflight tree holds changes
  you cannot attribute;
- the scope audit finds a changed file no task explains;
- `commit-workflow` produced more or fewer than one commit for the phase;
- `tasks.md` conflicts with `spec.md`/`plan.md`/`contracts/` — that is a planning fix, and editing
  `tasks.md` to paper over it is exactly what this skill forbids;
- build or tests fail in a way the implementer cannot resolve within its phase scope.

Report what happened and what you recommend; do not improvise past the boundary of the plan.

## Per-phase report

After each phase, give the user a short block — enough to audit the gate without reading the diff:

```
Phase <N>: <title>
  Agents:       <1 or 2, with split rationale when applicable>
  Tasks:        <IDs> — all [X]
  Review:       <n> critical / <n> major / <n> minor actionable; <n> non-actionable notes
  Fix rounds:   <k> of 3
  Verification: <fresh commands run> — <pass/fail summary>
  Scope audit:  passed
  Commit:       <sha> <subject>
  Worktree:     clean
```

At the end of the run, summarise the phases completed, anything left open, and the next step.
