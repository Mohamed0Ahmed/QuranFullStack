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

A worker for a ticket receives exactly one of two message shapes, and never any other:

1. **Initial dispatch** — the first message sent to that worker for that ticket.
2. **Post-landing integration remediation** — a follow-up to the same worker after its
   work was landed and integration verification then failed.

### Initial dispatch

The first message to a worker is exactly this instruction, with the selected ready
implementation ticket number substituted:

```text
Invoke implement skill for ticket #<NUMBER> only.
```

Add no implementation guidance, repository context, worktree path, acceptance criteria,
review request, or report contract around it. Bind repository and worktree paths through
the dispatch tool or process configuration. In same-session mode, start the subagent
without inherited conversation turns when the host supports that control.

This exact one-line prompt is **only** for the first dispatch of a ticket. Never resend
it to retry, re-open, or remediate a ticket whose work has already been landed: a
re-sent dispatch prompt tells the worker to start the ticket over without the
integration evidence it needs.

The `implement` skill is the single owner of ticket understanding, implementation,
testing, code review, and worker completion behavior. A worker handles one ticket only.

### Post-landing integration remediation

Use this shape when landing/merging succeeded but the build, test, smoke, or integration
verification of the combined integration branch then failed — typically because the
ticket interacts with previously landed tickets. In that situation:

- Do not close the ticket.
- Do not start the ticket again from scratch, and do not resend the initial dispatch
  prompt.
- Do not open a new worker unless the original worker/session is genuinely unavailable.
- Return to the **same** delegate session or the **same** subagent that implemented that
  ticket; it owns the ticket context, so it owns the remediation.
- Keep that worker's worktree and branch alive until integration verification passes.
- Synchronize that worktree with the current integration tip **before** messaging the
  worker, as described under "Sync before post-landing remediation".
- Send it the actual integration failure evidence.

The follow-up message must identify the ticket number; that the ticket implementation
itself completed but integration verification failed after landing against the current
integration branch; the failing command/test/check; the relevant error output; and the
relevant file/test names when available. Use a concise follow-up of this form:

```text
Ticket #<NUMBER> was landed, but integration verification failed against the current integration branch.

Fix these integration failures in your existing ticket work:

- <failing command/test>
- <error/evidence>
- <relevant file/test if known>

Do not restart the ticket from scratch.
Keep the fix scoped to the integration failures caused or exposed by ticket #<NUMBER>.
Run the necessary focused verification and report when ready to re-land.
```

#### Sync before post-landing remediation

A worker may have finished on an isolated branch/worktree created from an older
integration tip, so after other tickets land it no longer contains the current combined
integration state. Remediation must happen against the current state, not the stale
pre-landing base. Before sending the remediation message:

1. Keep the original ticket worktree/branch alive.
2. Update that ticket branch/worktree to include the current integration tip using the
   repository's approved landing/Git workflow.
3. Do not edit product code during this synchronization.
4. If synchronization produces a merge conflict, invoke the installed
   `resolving-merge-conflicts` skill; do not improvise the resolution.
5. Only once the ticket worktree represents the current combined integration state, send
   the actual integration failure evidence to the **same** worker/session, using the
   message format above.
6. The worker then fixes the issue against that synchronized state.
7. Re-land the resulting fix and rerun integration verification.

Worked example — ticket A lands successfully; ticket B was implemented from an older
integration tip; ticket B lands, but combined integration verification fails because of
its interaction with ticket A:

```text
ticket B worker branch/worktree
-> synchronize with current integration tip containing ticket A
-> resolve any true merge conflict through `resolving-merge-conflicts`
-> send the SAME ticket B worker the integration failure evidence
-> worker fixes against the synchronized combined state
-> re-land
-> integration verification
-> close ticket B only after GREEN
```

Distinguish the two failure kinds:

- A true merge conflict during landing is handled by the installed
  `resolving-merge-conflicts` skill, as in the landing step below.
- A merge that succeeds while integration verification then fails is handled by
  same-worker remediation, not by the conflict skill.

Remediation does not change the main session's boundary: it still does not implement
product code and still does not perform or delegate a second code review of the ticket.

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
8. If that integration verification fails after a successful land, keep the ticket open
   and keep that worker's worktree and branch alive. First synchronize the ticket
   worktree with the current integration tip through the repository's Git workflow,
   using `resolving-merge-conflicts` for any conflict and editing no product code. Then
   send the same worker a post-landing integration remediation follow-up carrying the
   failing check and its error output, as specified in the worker contract. When the worker reports its fix, re-land and
   re-run integration verification. Repeat until verification passes, or record the path
   as blocked. Never resend the initial dispatch prompt, and never count a remediation
   round against a ticket as a new ticket.
9. Only after the result is landed and required integration verification passes, comment
   on/update and close the implementation ticket according to the repository issue
   workflow.
10. After each landed child ticket is closed, re-fetch the in-scope child and dependency
    state from GitHub and rebuild the ready frontier. Immediately fill each available
    slot from newly unblocked implementation tickets, without exceeding three workers.
    Refresh the frontier only after a successful closure, never after a bare land whose
    integration verification is still failing or pending.
11. Remove completed worktrees and temporary branches when their work is landed, its
    integration verification has passed, and the repository workflow says cleanup is
    safe. Continue until the requested ticket program has no running or ready tickets
    and every in-scope ticket is complete.

The completion sequence is invariant:

```text
worker completed
-> implement skill finished implementation, review, and tests
-> result landed/merged
-> required integration verification passed
-> GitHub ticket updated/closed
-> dependency frontier refreshed
```

When integration verification fails after landing, the sequence loops back into the same
worker rather than forward:

```text
result landed/merged
-> integration verification failed
-> ticket worktree synchronized with the current integration tip (ticket stays open)
-> same worker gets the integration failure evidence
-> worker reports fix
-> re-land and re-run integration verification
-> ... until it passes, then ticket updated/closed and frontier refreshed
```

A worker's “done” report alone never advances the GitHub ticket state.

## Failure handling

- A blocked or failed worker leaves its ticket open. Record and surface the blocker, and
  keep every dependent ticket out of the frontier.
- A failed landing or integration check leaves the ticket open. A post-landing
  integration failure goes back to the same worker with its evidence; only give up on
  the dependency path when that remediation loop cannot resolve it. Preserve successful
  independent paths when safe.
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
