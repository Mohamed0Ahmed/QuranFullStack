---
name: implementation-orchestrator
description: Orchestrate a GitHub root or program issue through isolated workers for ready implementation tickets. Use when the main session should coordinate dependency-aware dispatch, landing, integration verification, and ticket progression.
---

# Implementation Orchestrator

## Responsibility

Run a requested GitHub ticket program as an orchestration-only main session. The main
session owns the dependency frontier, isolated worker state, dispatch, status tracking,
landing, integration verification, GitHub ticket progression, and cleanup. Each worker
owns exactly one ticket through the installed `implement` skill.

The main session does not implement product code and does not perform or delegate a
second code review. It may make only orchestration changes: worktree/branch operations,
landing/merge operations, integration checks, conflict-skill invocation, and GitHub
ticket updates.

## Select the execution mode

The invocation must select one mode:

- **Delegate mode:** use the delegate skill named by the user, such as
  `codex-delegate`, `cursor-delegate`, `opencode-delegate`, or `gemini-delegate`.
  Use that skill only for its dispatch, polling, and result-collection mechanics; this
  skill's worker prompt, isolation, review boundary, and landing sequence take
  precedence over a delegate skill's ordinary brief/review/landing loop.
- **Same-session mode:** start a separate, isolated-context subagent for each ticket by
  using the current host's native subagent facility.

If the invocation omits the mode, or delegate mode names no installed delegate skill,
ask for that one choice before dispatch. If the selected transport cannot invoke the
installed `implement` skill or cannot bind the worker to isolated working state without
changing its prompt, report the incompatibility as a blocker.

## Root issue vs implementation tickets

The issue named in the invocation is the program/root scope, not automatically an
implementation ticket. Complete this classification before dispatching any worker:

1. Read the requested root issue.
2. Discover its in-scope child/sub-issues and dependencies through both native GitHub
   relationships and repository-supported parent/blocking markers in issue bodies. An
   empty native sub-issue response alone does not establish that the root has no
   implementation children.
3. If the root has any in-scope implementation child tickets, keep the root
   orchestration-only. **Never dispatch the root issue number to a worker.**
4. Build the ready frontier only from open implementation tickets with no open
   blockers.
5. Select each worker ticket from that ready frontier and substitute that ticket's
   number in the exact worker prompt.

The root issue itself is eligible for dispatch only when it has no in-scope
implementation child tickets and it is explicitly confirmed to be the actual
implementation ticket.

For example, given this invocation:

```text
Use implementation-orchestrator for issue #149 using opencode-delegate.
```

If #149 owns implementation tickets #150 through #170 and only #150 is ready, dispatch
#150 with this prompt:

```text
Invoke implement skill for ticket #150 only.
```

Do not substitute #149: it remains the orchestration scope.

## Exact worker contract

Every worker receives exactly this instruction, with the selected ready implementation
ticket number substituted:

```text
Invoke implement skill for ticket #<NUMBER> only.
```

Add no implementation guidance, repository context, worktree path, acceptance criteria,
review request, or report contract around it. Bind repository and worktree paths through
the dispatch tool or process configuration. In same-session mode, start the subagent
without inherited conversation turns when the host supports that control.

The `implement` skill is the single owner of ticket understanding, implementation,
testing, code review, and worker completion behavior. A worker handles one ticket only.

## Orchestration loop

1. Read the repository instructions and `docs/agents/issue-tracker.md`. Use `gh` for
   GitHub issue operations and the repository's installed `commit-workflow` skill for
   Git operations involved in landing.
2. Apply the root-versus-implementation classification above, build the open dependency
   graph, and identify the ready frontier. Never infer readiness from issue order alone.
3. Keep a ledger for each ticket containing blockers, worker mode, isolated worktree and
   branch, run status, candidate commit or working state, landed ref, integration-check
   result, and GitHub state.
4. Maintain at most three running ticket workers across all modes. Choose independent
   ready implementation tickets, create one branch and worktree per ticket from the
   current integration tip, and bind one worker to each worktree. Shared HEAD, index, or
   working-directory mutations are not an acceptable fallback. Dispatch ready tickets
   in parallel when their graph and integration surfaces make that safe.
5. On a worker success, confirm its result artifacts/status and identify the candidate
   commit or working state. Do not re-read the diff for implementation correctness, rerun
   a code-review skill, or ask another agent to review it.
6. Land the candidate into the integration branch through the repository workflow. If
   the delegate transport leaves reviewed changes uncommitted, the main session may
   create the landing commit without editing their contents. If a merge conflict occurs,
   invoke the installed `resolving-merge-conflicts` skill; do not improvise the
   resolution.
7. Run only the build, test, or smoke checks needed to establish that the combined,
   landed integration branch remains valid. This is integration verification, not a
   second assessment of the ticket implementation or acceptance criteria.
8. Only after the result is landed and required integration verification passes, comment
   on/update and close the implementation ticket according to the repository issue
   workflow.
9. After each landed child ticket is closed, re-fetch the in-scope child and dependency
   state from GitHub and rebuild the ready frontier. Immediately fill each available
   slot from newly unblocked implementation tickets, without exceeding three workers.
10. Remove completed worktrees and temporary branches when their work is landed and the
    repository workflow says cleanup is safe. Continue until the requested ticket
    program has no running or ready tickets and every in-scope ticket is complete.

The completion sequence is invariant:

```text
worker completed
-> implement skill finished implementation, review, and tests
-> result landed/merged
-> required integration verification passed
-> GitHub ticket updated/closed
-> dependency frontier refreshed
```

A worker's “done” report alone never advances the GitHub ticket state.

## Failure handling

- A blocked or failed worker leaves its ticket open. Record and surface the blocker, and
  keep every dependent ticket out of the frontier.
- A failed landing or integration check leaves the ticket open and stops that dependency
  path. Preserve successful independent paths when safe.
- When a production defect or newly discovered dependency requires another ticket,
  record and link it through the repository issue workflow so the graph reflects the
  real blocker. Do not bypass the relationship.
- Preserve tests, declared dependencies, and acceptance criteria. A failure is a blocker,
  not permission to weaken them.
- If no tickets are ready or running while open in-scope tickets remain, report the
  unresolved blockers; the program is blocked, not complete.

## Invocation examples

Delegate mode:

```text
Use implementation-orchestrator for issue #149 using codex-delegate.
```

Same-session mode:

```text
Use implementation-orchestrator for issue #149 in this session using subagents.
```
