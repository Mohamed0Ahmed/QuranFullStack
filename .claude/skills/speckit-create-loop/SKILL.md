---
name: speckit-create-loop
description: >-
  Use when the user asks to prepare, create, or generate a Spec Kit feature from
  an existing source plan or master plan and names both the plan path and an
  explicit scope — the whole plan, one phase, or a contiguous phase range.
  Not for implementing tasks, reviewing code, or running a single Spec Kit stage.
compatibility: >-
  Requires the Spec Kit project structure (`.specify/`, `specs/`), Bash + Read/Write, the Agent
  tool for remediation, and the installed `speckit-specify`, `speckit-clarify`, `speckit-plan`,
  `speckit-tasks`, and `speckit-analyze` skills.
---

# Spec Kit Create Loop

## Why this exists

The artifacts produced here are written by a strong planning model in a long conversation, and
executed later by a weaker model that has none of it — no clarification thread, no source plan in
context, no memory of what "the protection behavior" meant. Anything left implicit is lost between
those two moments. So the whole point of this loop is to push intent *out of the conversation and
into the files*, and to refuse to declare readiness while a hole remains.

Three failure modes justify the ceremony:

- **Silent scope drift.** A phase gets widened, shrunk, or reordered because it "obviously belongs
  together", and the resulting feature no longer matches the plan anyone approved.
- **Out-of-order phases.** Phase 5 is prepared on top of a Phase 4 that was never finished, so its
  assumptions are false before the first task runs.
- **Findings that never converge.** Analysis findings get triaged into "we can proceed anyway",
  and the artifacts ship with exactly the ambiguity that will stall the implementer.

This skill stops after artifact preparation. Implementation is `speckit-phase-loop`'s job.

## Roles and boundaries

| Role | Who | Runs | May write |
|---|---|---|---|
| Orchestrator | main session | this skill, every Spec Kit stage | whatever the stage skills write; the active feature's `source-scope.yml` |
| Fixer | one subagent | remediation of analysis findings | artifacts inside the active feature folder only |

Four boundaries make the result trustworthy. They hold under time pressure, long context, and a
large source plan:

- **Primary stages never leave the main session.** `speckit-specify`, `speckit-clarify`,
  `speckit-plan`, `speckit-tasks`, and `speckit-analyze` run here, invoked as skills. Delegating one
  to a subagent looks like a token saving and is actually a quality loss: the stage's output depends
  on everything decided before it, and a subagent gets a summary instead. Cost, context length, and
  plan size are not exceptions — they are the normal conditions this loop was built for. Reading the
  subagent's output carefully afterwards does not repair this; by then the decisions are made.
- **Never imitate a stage.** Invoke the installed skill. A hand-rolled "equivalent" of
  `speckit-tasks` skips its template, its checklist format, and its hooks.
- **Analyze is read-only.** It reports; it never edits. An analyzer that fixes what it found can no
  longer judge it, and the next run's report becomes self-congratulation.
- **Only remediation is delegated,** and the fixer never runs the authoritative analysis.

The loop also does not implement code, invoke `speckit-implement` or `speckit-taskstoissues`, create
issues, commit, amend, push, merge, or open a PR. It does not modify the source plan, the
constitution, core Spec Kit skills, or any other feature's folder.

### Spec Kit's own git hooks

Read `.specify/extensions.yml` during the run rather than working from memory of it — which hooks
exist, and whether each is mandatory or optional, is configuration that changes without this skill
changing. For each stage you invoke, honour what the file currently says:

- **Mandatory** hooks (`optional: false`) belonging to that stage run. Feature-branch creation
  arrives this way; a branch is not a commit, so it is not a boundary violation.
- **Optional** hooks are reported as the stage reports them, and declined. This loop does not commit.

A mandatory hook that would commit is a conflict between the configuration and this skill's
boundaries, not something to resolve by guessing: stop and report it. If the user wants a commit,
that is their call, made outside this skill.

## Step 0 — Validate the invocation

Two arguments are required and neither is ever inferred:

```
path   repository-relative or absolute path to the source plan
scope  exactly one of:  whole  |  phase N  |  phases N-M
```

Missing either one is a full stop, before any artifact exists. The temptation is real — usually one
plausible master plan sits in the repo, and `whole` reads like a natural default — but a wrong guess
only surfaces after `spec.md`, `plan.md`, and `tasks.md` have been written against it.

Reject, with the exact offending argument quoted:

| Input | Verdict |
|---|---|
| no `path` | stop — ask for the plan path; "it is the only plan in the repo" is not permission to assume it |
| no `scope` | stop — ask for the scope; `whole` is never the default |
| `phases 5-3` | stop — first phase is after the last; do not silently read it as `3-5` |
| `phases 2,4` / "phases 2 and 4" | stop — ranges must be contiguous; this is two features, not one |
| path does not exist or is unreadable | stop — report the resolved path you tried |
| requested phase label absent from the plan | stop — list the phase labels the plan does contain |
| phase label ambiguous | stop — see below |

**Ambiguity is a real case, not a formality.** In `docs/feature-abwab-management/MASTER_PLAN.md`,
"phase 4" could mean `## 4. Verified repository reality` or `### 18.4 030-abwab-relationships-templates`.
Resolve it only when one reading is uniquely defensible (e.g. the plan has one numbered phase
sequence and the other numbers are section headings of a different kind). Otherwise quote both
candidate headings and ask which one is meant.

Do not ask the user to restate what the invocation already contains.

## Step 1 — Resolve the source scope exactly

| Scope | What is in scope | What else you read |
|---|---|---|
| `whole` | the entire plan | — |
| `phase N` | that phase only | surrounding phases, read-only, for dependencies and vocabulary |
| `phases N-M` | all phases N..M as **one** feature | surrounding phases, read-only |

A multi-phase request produces one feature folder, not one per phase.

Read-only context exists so you can understand terms and prerequisites the phase depends on. It must
not become requirements. Pulling in a later phase's functionality because it is "closely related" is
the scope drift this skill exists to prevent; so is dropping part of the requested phase because it
looks like it belongs elsewhere.

## Step 2 — Sequence preflight

Preparing Phase N while Phase N-1 is unfinished produces a spec built on assumptions that are not
true yet. Run this before `speckit-specify` — after it, a feature folder and branch already exist and
backing out is messy.

This whole step is **read-only**: it inspects existing features and decides whether the run may
start. It writes nothing, in this feature or any other.

For a scope starting at Phase N, establish contiguous proven coverage of Phases 1..N-1. Shell
commands find candidates; they never deliver the verdict:

```bash
grep -rl "source_plan: <normalized path>" specs/*/source-scope.yml   # candidates, not conclusions
```

A prior feature proves a phase complete only when **all** of these hold:

1. its traceability names the same source plan;
2. its recorded scope covers that phase;
3. the feature folder exists;
4. `tasks.md` exists;
5. every real task row in `tasks.md` is checked `[X]` or `[x]`.

Decide point 5 by reading `tasks.md` as a document, not by trusting a count. These files are full of
lines that look like tasks and are not: template rows and worked examples inside fenced code blocks,
"how to use this file" checklists, checkpoint and acceptance checklists carrying no task ID. A
checked explanatory checklist is not evidence that a task ran; an unchecked example row is not
evidence that one is pending. Walk the structure, skip fenced blocks, keep only rows carrying a valid
generated task ID (`T001`-style here), and read the checkbox state of those.

A feature scoped `phases 2-3` with every real task checked completes both phases; the next allowed
start is Phase 4. Coverage must be contiguous — Phases 1-3 proven and Phase 4 unproven blocks a
Phase 5 request even when Phase 5 reads as independent, because "independent" is a judgment the plan
author already made differently by ordering them.

**Branch names, folder names, commit messages, and topical resemblance are never proof.** A branch
called `013-phase-4-order-pipeline-complete` says what someone intended, not what landed. They are
useful for *locating* a candidate feature folder; the folder's traceability plus a fully checked
`tasks.md` is what decides.

**Features prepared before this skill existed** have no `source-scope.yml`. Their `spec.md` may
serve as fallback evidence, but only when it states the mapping explicitly — naming the same plan
path and the same section or phase. Vague resemblance, a matching topic, or a similar title is not
a mapping. When the mapping stays ambiguous, stop and ask the user which phases that feature
covers; do not resolve it by inference.

Do not write `source-scope.yml` into an older feature folder to settle the question, even with the
user's agreement in the moment. This step is read-only, the folder is not this run's feature, and
backfilling legacy traceability is a separate piece of work with its own review — mixing it into a
preflight means a run that was supposed to only look has quietly edited a completed feature.

**Overlap is checked for every scope, not just `whole`.** If an existing feature already covers the
requested phases — most obviously the one `.specify/feature.json` currently points at — stop and
report it. `speckit-specify` would mint a second folder and repoint the active feature, orphaning
the first one's unfinished tasks. Resuming or superseding the existing feature is the user's call.

### Blocked report

```
Requested: <path> — <scope>
Proven completed coverage: Phases <a>-<b>
Blocked by: Phase <n>
Feature: specs/<feature>            (or: no feature folder found)
Reason: <no completed feature covers it | tasks.md missing | 3 real tasks unchecked>
Unchecked: T021, T024, T028
```

Then stop. This block has no override inside this skill — not by the user's insistence, not by
"proceed anyway and record the dependency as a known risk", not by urgency, and not by a confident
"phase N is independent anyway". A missing prerequisite phase is not a risk a spec can carry: it is
a spec written against behavior that does not exist yet, and the note recording that would be read
by the model least able to act on it.

What unblocks the run is the prerequisite phase actually completing — implement it (that is
`speckit-phase-loop`'s job) and its `tasks.md` will then say so. Anything else is a decision to make
deliberately, outside this loop, not a flag to pass into it.

## Step 3 — `speckit-specify`, then record traceability

Invoke `speckit-specify` with a description that carries: the normalized plan path, the exact scope,
the instruction to derive only from that scope, and the instruction to introduce no new product or
architecture decision. Name the plan sections by their real headings so the stage reads the right
text.

Immediately after the feature folder exists, write `specs/<feature>/source-scope.yml`:

```yaml
source_plan: docs/feature-abwab-management/MASTER_PLAN.md
source_scope: phases 2-3
first_source_phase: 2
last_source_phase: 3
scope_kind: phase-range          # whole | single-phase | phase-range
```

This file is the canonical machine-readable traceability every future preflight reads. `spec.md`
should also state the plan and scope in prose for a human reader, but `source-scope.yml` governs.
Write it before clarification starts — a run that dies mid-clarify still leaves the next run able to
see what this feature covers.

### What `spec.md` has to carry

The downstream implementer reads this file instead of talking to you. Verify it records, wherever
the source scope has something to say: roles and actor distinctions; permissions and authorization
boundaries; validation rules; state transitions; uniqueness and identity rules; lifecycle;
error and failure behavior; concurrency and conflict behavior; deletion, archival, restoration, and
rollback behavior; negative scenarios; edge cases; and explicit out-of-scope statements. Success
criteria must be measurable and acceptance scenarios testable. Terminology must be canonical and
consistent, with unfamiliar domain terms defined.

Vague requirements are the specific failure to hunt: "handle correctly", "support properly", "be
robust", "scalable", "efficient", "as needed", "appropriate validation", "good user experience".
Each of those is a decision deferred onto a model less able to make it. Replace with observable
behavior, an explicit constraint, or a measurable outcome. Requirements stay at what-and-why;
technical choices belong to planning, unless the source plan or constitution already locked them.

## Step 4 — `speckit-clarify`, to completion

Invoke `speckit-clarify` and let it run its full official workflow — up to five material questions,
one at a time, each answer integrated into `spec.md` before the next is asked.

The failure to avoid is stopping after the first answer because the momentum feels like progress.
One answered question is not a completed clarification workflow; the remaining questions are the
ones nobody thought to ask, which is exactly why they were queued. After each answer, return to the
clarify workflow in this same session and continue until it reports itself complete.

A detailed source plan does not remove the need for clarification — it is precisely a detailed plan
that hides its unstated decisions well. Do not answer a product question on the user's behalf, and
do not treat "this option is the common one" as an answer. Once clarify reports complete and the
answers are written into `spec.md`, go straight to planning; do not restart clarification unless a
later blocker genuinely returns to requirements.

## Step 5 — `speckit-plan`

Invoke `speckit-plan`. The plan and its supporting artifacts (`research.md`, `data-model.md`,
`contracts/`, `quickstart.md`) must be executable by someone with no access to this session.

Resolve now, with repository evidence, whatever can be resolved safely: affected projects and
architectural layers; domain components, entities, and value objects; tables, constraints, and
migrations; repositories and readers/writers; application services, commands, and queries; endpoints
and contracts; authorization behavior; validation boundaries; frontend routes, stores and state
ownership, UI components, and their error / loading / empty / conflict states; cache and
invalidation; transaction boundaries; concurrency rules; audit behavior; rollback and restore
implications; compatibility constraints; observability; test strategy; and implementation ordering.

Record the rationale wherever picking the other alternative later would create real risk. Do not
over-prescribe incidental code shape when several repository-compatible implementations are equally
fine — the plan fixes behavior, invariants, contracts, boundaries, dependencies, expected artifacts,
and verification, not keystrokes.

Use repository-relative paths for anything that already exists. When a new file's exact name cannot
be known safely, name the target project, the target directory or architectural area, the
responsibility, and the nearby pattern to follow.

## Step 6 — `speckit-tasks`, then the readiness gates

Invoke `speckit-tasks`. Carry into it the test expectation for this feature — `speckit-tasks` only
generates test tasks when they are requested, and the workspace `TESTING_STRATEGY.md` plus the source
scope's acceptance criteria usually require them.

### Every real task

The implementer works from the repository, these artifacts, and project instructions — nothing else.
Each task therefore states what changes, where (exact repository-relative path when knowable; else
project + directory + component/entity/endpoint/service/store/test file), the resulting behavior or
invariant, its dependency on earlier tasks, the requirement or story it serves, and how completion is
verified.

Two opposite failures, both common:

- **Too broad** — "Implement the backend", "Add the API", "Update the frontend", "Add validation",
  "Handle edge cases", "Add authorization", "Add tests", "Update documentation", "Refactor as
  needed", "Improve performance". Each hides several independently verifiable changes and no
  location.
- **Hidden context** — "apply the protection behavior we discussed", "as agreed above", "the usual
  pattern here". The implementer was not in this conversation. Either write the behavior out or cite
  the artifact section that defines it.

Nor should tasks be shredded into micro-steps ("create the file", "add the usings", "add the class
declaration") to make the list look thorough or to keep a phase under some number. One task is one
coherent, objectively verifiable change.

Traceability runs both ways: every functional requirement and every buildable success criterion maps
to at least one task, and every task maps back to a requirement, success criterion, user story,
acceptance scenario, contract, data-model element, technical prerequisite, or constitution
requirement. No orphan tasks, no uncovered requirements. Include the identifiers in task text where
the repo's task format allows.

Test tasks name the behavior being proved — the test project or spec file, the level (unit,
integration, contract, component, e2e), and which of success / negative / permission / validation /
conflict / rollback / failure path it covers, with the focused command when known. Follow
`TESTING_STRATEGY.md`; do not demand unrelated full-suite runs for focused work.

Order by real dependency: schema and migrations, contracts, entities, persistence, application logic,
APIs, frontend state, UI, integration, acceptance. Mark `[P]` only when the tasks genuinely can run
at once — no missing prerequisite, no shared file, no shared migration ordering, no shared contract,
no hidden sequencing. Every user-story or implementation phase ends with its own independent
verification or acceptance condition.

### Phase size and coherence

A phase is the unit `speckit-phase-loop` implements, reviews, fixes, and verifies in one cycle, so it
has to fit in one such cycle.

| Real implementation tasks | What it means |
|---|---|
| 8–15 | healthy |
| 15–20 | fine when the tasks are small, tightly related, and produce one verifiable outcome |
| >20 | triggers an explicit phase-coherence review — say out loud why it stays whole or how it splits |
| ≥30 | normally split into smaller dependency-ordered phases |

Keep 30+ together only when splitting would break atomicity, force a knowingly broken intermediate
state, or destroy independent verification — and write that reason into `tasks.md`. "It is one
vertical slice" and "an earlier feature in this repo shipped a phase that size" are observations, not
that reason; the question the gate asks is whether one implement→review→fix→verify cycle can carry
it. These numbers are a warning signal, not a rule to satisfy: the decision is coherence and
reviewability. Splitting by
coherent outcome (domain and persistence foundation; application commands and queries; API contracts
and authorization; frontend state; UI workflows; audit and rollback; acceptance) beats splitting by
arithmetic, and "it is all one feature" is not a reason to keep an incoherent phase whole.

Each resulting phase has one goal, related work, correct dependency order, a repository that still
builds, focused verification criteria, stated dependencies on earlier phases, and no reliance on
undocumented work from a later phase.

### Readiness gate before analysis

A model with none of this conversation must be able to understand each phase's goal and each task,
find every work area, execute in dependency order, tell parallel from sequential, know which tests
are required and what result is expected, decide objectively whether a task is done, and finish
without inventing product decisions. Fix whatever fails this before running analysis — analysis is
cheaper when the obvious holes are already closed.

## Step 7 — `speckit-analyze` (read-only)

Invoke `speckit-analyze` in the main session and present its full structured report to the user,
preserving finding IDs, severities, locations, summaries, and recommendations.

Seeing the fix while reading the report is normal, and editing right then is the trap: analysis that
mutates the artifacts it is judging cannot be re-run as evidence. Note the fix, finish the report,
hand it to the fixer.

Classify each finding as actionable or not. Actionable at **any** severity blocks completion,
including LOW and MEDIUM — "implementation could technically proceed" is not the bar; the bar is that
the downstream model does not have to guess. A note only stays a note when no edit would resolve it;
a note that describes something you would change is a finding wearing a different label.

Implementation-readiness problems are analysis findings and enter the same loop: vague task text;
missing locations; broad tasks bundling several changes; no observable completion criteria; tasks
needing hidden conversational context; missing or one-way requirement↔task traceability; orphan
tasks; unclear ordering; wrong `[P]` markers; oversized or incoherent phases; artificial
fragmentation; ambiguous Backend/Frontend/database/test/doc ownership; unresolved product or
architectural decisions; missing negative-path, permission, validation, conflict/concurrency, or
rollback coverage; terminology drift; contracts or data-model elements absent from tasks; and tasks
that contradict, silently extend beyond, or omit part of the source scope.

Invoking this skill is the user's advance authorization to remediate these findings automatically.
It is not authorization for analyze itself to write.

## Step 8 — Remediation loop

One fixer subagent handles remediation, and the **same** agent handles every round of this run
(continue it with `SendMessage`; load it with `ToolSearch("select:SendMessage")` if needed). A fresh
agent each round re-reads everything, loses why it shaped an artifact that way, and tends to produce
a new interpretation rather than a fix.

Give the fixer: the active feature folder, the normalized source-plan path, the exact scope, the
artifact paths, the **complete** latest analysis report, the accepted clarification decisions, and
the constitution and repository constraints that apply. Tell it explicitly to preserve source intent
and to ask rather than guess.

The fixer may edit only the active feature's `spec.md`, `plan.md`, `tasks.md`, `contracts/`, data
model, research, quickstart, and other artifacts inside that folder. It may not touch application
code, other feature folders, completed features, the source plan, the constitution, Spec Kit or
project skills, unrelated documentation, or CI/deployment configuration — and it does not commit,
push, or open PRs.

It addresses the whole actionable report, not the convenient half. Triaging LOWs away as "wording
churn" is the reflex to resist: the LOWs in these reports are usually terminology drift, an unmapped
task, or a missing location — each one a small decision the implementer would otherwise have to make
alone. A round that fixes CRITICAL and HIGH and defers the rest has not finished, and the next
analysis will simply report them again. Contradictions get removed, not out-voted by an added
sentence.

Then control returns here: **the main session reruns `speckit-analyze`.** Present the fresh report.
Actionable findings remain → send the new complete report to the same fixer. Repeat.

The last analysis must be newer than the last edit. A clean report from before the fixer's changes
describes files that no longer exist in that form, and reporting readiness from it is reporting on
work nobody checked.

### When the fixer needs a product decision

The fixer stops rather than guessing, and returns the exact question, the affected finding IDs and
artifacts, why the answer materially changes implementation or validation, and concise options. Ask
the user — one decision at a time when they are independent — then send the accepted answer to the
same fixer, which integrates it consistently everywhere it lands. Then rerun analysis.

Do not escalate what is already decided by the source scope, an accepted clarification, the
constitution, a locked product decision, the repository's architecture or conventions, an existing
contract, or an explicit user instruction. Read first, ask second, and never guess to keep things
moving.

### When to stop instead of looping

**One unchanged recurrence ends the loop.** If the fixer attempted a relevant repair of a finding
and the next analysis reports that finding unchanged, stop — do not send it back for another try.
Two identical reports mean one of three things, and another round distinguishes none of them: the
edit never landed, the edit landed somewhere other than what the finding is about, or the fixer and
the analyzer disagree about what the finding means. Each needs the user, not another attempt.

Also stop and hand the state to the user when: the fixer reports that safe remediation is
impossible; two accepted requirements
genuinely conflict; the source plan contradicts a locked project decision; repository evidence cannot
settle intended behavior; prior phase coverage will not map unambiguously; fixing one artifact must
break another authoritative one; or the only resolution would change the constitution or touch
application code.

Report the blocker, the finding IDs, the affected files, the evidence you inspected, and why it
cannot continue safely. Do not call the artifacts clean.

## Completion

Complete only when all of these are true:

```
explicit valid path and scope       source traceability recorded
exact scope resolved                specify / clarify / plan / tasks completed
sequence preflight passed           clarify's full workflow ran, answers integrated
task + phase readiness gates passed remediation rounds finished
fresh main-session analyze reports zero actionable findings
```

Non-actionable notes may remain; label them as such. Then stop — no implementation, no commit, no PR.

Final report:

```
Feature:        specs/<feature>
Source:         <plan path> — <scope>  (phases <a>-<b>)
Preflight:      proven coverage Phases <a>-<b>
Clarify:        <n> questions asked and integrated
Tasks:          <n> real tasks across <k> phases (<sizes>) — coherence review: <verdict>
Analyze rounds: <n>; final run: 0 actionable, <m> non-actionable notes
Next:           implement with speckit-phase-loop
```

## Red flags

Each of these means stop and re-read the step above:

| Thought | Reality |
|---|---|
| "Only one master plan here, so the path is obvious" | Guessing the path is guessing the feature. Ask. |
| "They didn't say a scope, so `whole`" | `whole` is a choice the user has to make. Ask. |
| "`phases 5-3` clearly means 3-5" | Reinterpreting the invocation is where drift starts. Stop. |
| "Phase 5 doesn't really depend on Phase 4" | The plan's order encodes a judgment already made. Blocked. |
| "Proceed and note the gap as a known risk" | The note lands on the implementer, who cannot act on it. Stop. |
| "31 tasks, but it's one vertical slice" | Slice shape isn't the test; one review cycle is. Review it. |
| "The LOWs are just wording — defer them" | LOWs here are drift, orphans, missing locations. Fix them. |
| "The branch says Phase 4 is done" | Branch names are intent. Checked `tasks.md` is evidence. |
| "The folder exists, so it shipped" | Unchecked real tasks mean unfinished work. |
| "Plan is huge — delegate planning to a subagent" | Size is the normal case, not an exception. Main session. |
| "They answered the clarify question, on to planning" | One answer ≠ completed clarify workflow. |
| "The source plan is detailed enough to skip clarify" | Detailed plans hide unstated decisions best. |
| "I can see the fix while analyzing — I'll just edit" | Then the next analysis judges your own edits. |
| "Only LOW findings left; good enough to ship" | Actionable at any severity blocks. |
| "Fixer says it's fixed, last report was clean" | That report predates the edits. Rerun analyze. |
| "Same finding again — one more round will do it" | One unchanged recurrence ends the loop. Escalate. |
| "Common practice answers this product question" | Common ≠ chosen. Ask the user. |
