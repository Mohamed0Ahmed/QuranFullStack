# Engineering Review Workflow V2 — Focused Checkpoints and Final Review Implementation Plan

Either Claude or Sol/Codex can execute this plan directly and sequentially. It requires no generic
orchestrator, fixer Skill, automatic Skill chain, or external implementation framework.

**Goal:** Separate narrow implementation checkpoints from the authoritative whole-change formal
review, make formal review the normal completed-feature/change boundary, and make re-review reuse its
original finding context without weakening final cumulative-diff verification.

**Architecture:** Add one small canonical `focused-review` Skill under `.claude/skills/` with a thin
`.agents/skills/` adapter. Keep `engineering-review` as the sole formal findings/verdict owner and add
its re-review path there. `TESTING_STRATEGY.md` remains the sole verification-selection/freshness owner;
implementation produces evidence and both review Skills remain read-only consumers.

**Fixed baseline:** Workflow & Instruction Routing V2, Skills V2, and Testing Strategy V2 are already
implemented and are not redesigned. The historical context-cost audit justifies conditional loading
and scoped re-review, but its byte/token estimates are not current runtime measurements.

## Global constraints

- A normal phase gets the focused/protected verification selected by `TESTING_STRATEGY.md`, not a
  formal review by default.
- Formal review normally happens once at the completed feature/change boundary. A user may explicitly
  request a formal review at any earlier boundary.
- `focused-review` and `engineering-review` own different results; neither invokes the other or any
  project Skill.
- Neither review Skill runs builds/tests, creates evidence, fixes findings, edits files, performs
  Git/PR/deploy work, or starts another specialist workflow.
- The reviewer must not fix its own findings. Fixes are separate explicitly requested implementation.
- Testing Strategy V2 retains `FOCUSED`, `PROTECTED_TRIGGER`, `FINAL_BOUNDARY`, `RELEASE_ONLY`, whole-
  diff selection, freshness, failure, skip, and evidence semantics.
- Existing same-diff Test Guard evidence may be consumed when supplied. Changed tests alone must not
  create an invocation or hidden Test Guard stage.
- `.claude/skills/` remains canonical behavior; `.agents/skills/` remains thin trigger-equivalent
  adapters plus interface metadata.
- Preserve formal severity/verdict semantics, `main` protection, Quran/source provenance,
  security/access, transaction/audit, route/API-contract, neutral-terminology, and no-CI honesty.
- No commit, push, PR, deploy, formal review, product build, or product test run is part of executing
  this Skill/docs-only plan unless separately authorized.

---

## 1. Current review-workflow problems

1. There is no general narrow-review owner for one phase, task, fix, file set, or risk checkpoint.
2. `engineering-review` discovery currently names generic code/diff/branch/PR/phase reviews; its
   sidecar also prompts broad diff readiness, so narrow requests can select the formal workflow.
3. `SKILLS_AND_ARCHITECTURE_GUIDE.md` still recommends formal review after every phase and routes
   several narrow examples to it.
4. The formal Skill claims a cadence “below” but defines none. It has no initial/re-review split,
   stable finding/state identity, or safe full-review fallback.
5. Re-review therefore repeats the initial whole-review contract instead of concentrating on recorded
   findings, their fixes, plausible fix regressions, and fresh evidence.
6. The formal Skill makes Test Guard universally required when tests changed; the locked V2 contract
   is to consume supplied same-diff evidence and report absence only when the active contract requires
   it, without invoking Test Guard.
7. The Skill guide still describes ten project Skills and lacks the focused/final discovery boundary,
   exact four testing boundaries, and formal-finding closure loop.

These are review-workflow defects, not authority to change tests, routers, Spec Kit, product code, CI,
or delivery workflows.

## 2. Target Review Workflow V2

```text
normal implementation phase
  -> focused verification
  -> any genuinely triggered protected verification
  -> continue

optional intermediate checkpoint
  -> user/orchestrator explicitly requests focused-review
  -> scoped findings only
  -> separate fixes if later requested
  -> implementation continues

completed feature/change
  -> implementation computes/runs fresh cumulative-final-diff evidence
  -> user/orchestrator explicitly invokes engineering-review
  -> formal verdict

formal findings
  -> separate implementation fixes
  -> focused/protected verification while fixes are in motion
  -> after fixes settle, recompute/run the whole final union once
  -> return to the same reviewer session when practical
  -> re-review findings, affected changes, plausible regressions, and fresh evidence
  -> formal verdict
```

No arrow is an automatic Skill invocation.

## 3. `focused-review` responsibility and contract

Use this exact canonical/adapter discovery sentence:

```text
Use when asked for a scoped Quran Dashboard review of one phase, task, fix, selected file set, or explicit architecture, security, or data-safety checkpoint.
```

### Owned result

1. Freeze the requested scope and state it.
2. Inspect only that code/diff plus minimum adjacent code needed to understand it.
3. Compare it with only the relevant active plan/spec/contract slice.
4. Load only context implicated by the scope or a concrete candidate finding.
5. Report scoped findings and what was not reviewed; stop.

It never expands from selected files to the branch, from one phase to the feature, or from one
checkpoint to final readiness. It does not run verification, produce a formal verdict, compute final
evidence sufficiency, invoke another Skill, fix findings, perform delivery work, or load the Spec Kit
formal add-on/deep formal-review closure. Performance belongs to the performance Skill when that is
the requested review; test-code quality remains `test-guard`; Backend placement/layering remains
`backend-structure-review`.

### Context and evidence

Always load the requested scope and its relevant contract slice. Conditionally load only:

- implicated `CODING_PRINCIPLES.md` §§2–4/§7 for clean-code responsibility;
- exact Backend or Frontend architecture/API/style headings for that area, never both by default;
- security contract/nearest auth README/API security heading for auth/access scope;
- `PRODUCT.md`/`DESIGN.md` only for user-facing/product/visual scope;
- Quran safety plus the nearest source/rendering owner only for Quran scope; and
- an exact `TESTING_STRATEGY.md` heading only to label supplied checkpoint evidence.

It may state that relevant supplied evidence is current, missing, failed, skipped, or unknown, but it
does not run it or judge the whole feature's final union. Same-diff Test Guard evidence may be consumed
when supplied; absence never causes an invocation or promotion to formal review.

### Small output contract

```text
# Focused Review
- Status: CLEAR | FINDINGS
- Scope reviewed: exact scope and context consulted
- Findings: numbered and ordered BLOCKING -> MAJOR -> MINOR -> NOTE; “None.” when clear
- Evidence observed: only when part of the requested checkpoint
- Not reviewed: explicit exclusions
```

Severity terms reuse current project meanings for ordering only; `CLEAR`/`FINDINGS` is not a formal
verdict. A later focused re-review is another explicit narrow request and closes no final boundary.

## 4. Revised formal `engineering-review` responsibility and contract

Use this exact canonical/adapter discovery sentence:

```text
Use when explicitly asked for a formal Quran Dashboard engineering review at any boundary, for the final review of a completed feature/change, or when the user explicitly names the engineering-review Skill.
```

Thus generic phase/file/task/fix review selects `focused-review`; “formal” at any boundary, “final” for
a completed change, or explicit Skill naming selects `engineering-review`.

### Initial formal review

1. Establish the base and complete cumulative current diff/content, including generated and in-scope
   untracked files.
2. Read the active plan/spec/contracts before choosing specialist context.
3. Inspect the full relevant final diff against those requirements and repository truth.
4. Consume supplied final evidence and classify it through only implicated Testing Strategy headings.
5. Load specialist policy/reference context only after the diff or a candidate finding implicates it.
6. Report the existing seven-section formal output and verdict.

Normal cadence is once after the completed change and final evidence. An explicitly requested earlier
formal review uses the same contract as a deliberate override.

### Evidence and output

- Consume evidence only; never execute `TESTING_STRATEGY.md` §5 or its commands.
- Use only implicated headings among §§1, 2.1, 3–6, 8, and 9. Report required evidence as sufficient,
  stale, missing, failed, unexpectedly skipped, or unknown; do not claim PASS with deficient required
  final evidence.
- Consume a same-diff Test Guard result when supplied. Report it missing only when the active
  plan/spec/contract explicitly requires that separate evidence; changed tests alone authorize no
  invocation or hidden stage.
- Preserve the existing `BLOCKING`/`MAJOR`/`MINOR`/`NOTE` meanings, `PASS`/`PASS WITH NOTES`/
  `CHANGES REQUESTED`/`BLOCKED` verdicts, seven output sections, Quran safety line, verification check,
  and optional non-verdict commit reminder.
- In **Scope reviewed**, record initial versus re-review and the reviewed base/current-state identity.
  Give initial findings stable IDs (`ER-1`, `ER-2`, ...). Re-review retains each ID and marks it
  `CLOSED`, `OPEN`, or `REGRESSED`; new findings receive new IDs. These are finding states, not a new
  verdict taxonomy.

No persisted reviewer-state artifact is created. Prefer the same session; otherwise the caller
supplies the prior report, original base/scope, and current state.

## 5. Discovery rules preventing narrow-to-formal escalation

| Request | Owner |
|---|---|
| “Review Phase 2 only.” / “Review these three changed files only.” | `focused-review` |
| “Review this auth/schema/Quran foundation checkpoint.” | `focused-review` |
| “Run a formal engineering review of Phase 2 now.” | `engineering-review` explicit override |
| “Run the formal/final engineering review for the completed feature.” | `engineering-review` initial path |
| “We fixed all formal review findings; re-review them.” | `engineering-review` re-review path |
| Test-code quality / Backend placement / reported slowness | Existing `test-guard` / `backend-structure-review` / performance Skill |

Frontmatter is the discovery boundary; canonical bodies own behavior. The guide documents examples
without becoming another router, and adapters do not copy this table.

## 6. Final-boundary default cadence and optional high-risk checkpoints

Normal phases continue after Testing Strategy V2 verification. The user/orchestrator may explicitly
request `focused-review` for a migration/schema foundation, auth boundary, Quran source/import/
persistence integrity, transaction/rollback/audit boundary, major public API foundation, or another
risky checkpoint. These examples are optional recommendations, never automatic invocations.

After the feature/change and any pre-review fixes settle, implementation runs the cumulative final
union, then the user/orchestrator requests the formal review. Commit/PR/deploy workflows happen later.

## 7. Evidence integration and same-reviewer fix loop

1. Initial formal review records reviewed-state identity and stable findings; the reviewer stops.
2. Separate implementation fixes selected findings and runs focused/protected verification while the
   fix set is in motion.
3. After fixes settle, implementation restarts `TESTING_STRATEGY.md` §5 from the original feature base,
   recomputes the whole remaining diff, and runs fresh final evidence once.
4. Prefer the same formal reviewer session. Re-review inspects original findings, changed behavior,
   regressions reasonably introduced by fixes, and fresh final evidence, then issues a new verdict.
5. Report each original finding `CLOSED`, `OPEN`, or `REGRESSED`, plus any new finding.

Use a fresh full formal review rather than the reduced path when scope materially expanded, unrelated
code changed, the base/plan/spec/contract changed, fixes introduced a new unreviewed safety area,
continuity cannot be established, or explicit risk requires it. Loading one newly implicated exact
owner is the smallest safe escalation; continuity never permits stale evidence or hidden new scope.

Focused Review may observe checkpoint evidence but never owns final sufficiency. Engineering Review
reports final evidence deficiencies but never recreates them.

## 8. Context/reference-loading simplification

Formal review reads the active contract and actual cumulative diff before specialist context; focused
review starts from its named scope/contract slice. Both follow:

```text
scope/diff -> concrete requirement or candidate finding -> exact deciding heading/reference -> output
```

- No unconditional clean-code/reference closure.
- Backend-only does not load Frontend/style; Frontend-only does not load Backend/database.
- Test references load only when changed tests materially bear on the requirement, one stack only.
- Quran, product/design, and security owners load only for their respective scopes.
- The Spec Kit formal add-on loads only for formal Spec-Kit review, not ordinary focused phase review.
- Re-review rereads only what a changed finding, plausible regression, fresh evidence, newly implicated
  owner, or lost continuity requires.

Correct minimal context, not a token-percentage target, is the acceptance criterion.

## 9. Exact implementation file set

### Create

| File | Responsibility |
|---|---|
| `.claude/skills/focused-review/SKILL.md` | Canonical §3 contract; small/self-contained, no reference pack. |
| `.agents/skills/focused-review/SKILL.md` | Matching frontmatter and canonical pointer only. |
| `.agents/skills/focused-review/agents/openai.yaml` | `display_name: "Focused Review"`; `short_description: "Review one explicit implementation scope"`; `default_prompt: "Use $focused-review to review only this named phase, task, fix, file set, or risk checkpoint."` |

### Modify

| File | Responsibility |
|---|---|
| `.claude/skills/engineering-review/SKILL.md` | Exact §4 discovery, initial/final contract, evidence rule, state/finding IDs, §7 re-review/fallback, no-fix rule; preserve formal output/safety routes. |
| `.agents/skills/engineering-review/SKILL.md` | Exact §4 description; retain canonical pointer-only body. |
| `.agents/skills/engineering-review/agents/openai.yaml` | Keep display name; use `short_description: "Formally review a completed change"`; `default_prompt: "Use $engineering-review to run the formal final review of this completed change and its supplied verification evidence."` |
| `SKILLS_AND_ARCHITECTURE_GUIDE.md` | Eleven-Skill roster/ownership, four testing boundaries, focused/final workflows, discovery matrix, re-review pointer, anti-patterns, inventory; no copied Skill body. |

Delete nothing. These seven paths are the implementation allowlist. The separately approved plan
artifact `docs/project-simplification-audit/plans/04-engineering-review-workflow-v2.md` may coexist in
the cumulative branch diff but is not an implementation path.

No router, `TESTING_STRATEGY.md`, `docs/README.md`, coding principle, formal review reference/Spec Kit
add-on, Test Guard, PR context, performance Skill, Spec Kit artifact, production/test/API/architecture/
style file, CI/deploy file, database/data, audit report, or persistent memory changes. Current native
routes already discover explicit Skills; `docs/README.md` already places passing formal review before
feature-artifact deletion.

## 10. Small sequential implementation steps

### Step 1 — Freeze scope and inspect current review state

- [ ] Confirm branch is not `main`, capture root status, and freeze §9's seven implementation paths.
- [ ] Statically inspect the canonical `engineering-review`, its Sol/Codex adapter/sidecar, and the
  directly relevant Skill-guide sections to confirm the current discovery/adapter/guide state in §1.
  The completed audit and Skills V2 evidence already establish baseline behavior; run no fresh session.
- [ ] Stop if static inspection materially differs or discovery would require a router edit.

### Step 2 — Create focused-review

- [ ] Write the canonical §3 contract, thin matching adapter, and exact §9 sidecar.
- [ ] Add no reference pack, script, asset, README, dependency, policy override, or second framework.
- [ ] Check the narrow/specialist routing probes before changing formal review.

### Step 3 — Narrow formal review and add re-review

- [ ] Apply §4's exact canonical/adapter description and sidecar values.
- [ ] Make initial review start from cumulative diff/contracts, correct Test Guard evidence semantics,
  add state/finding IDs and §7 re-review/fallback, and state reviewer-must-not-fix.
- [ ] Preserve all existing formal severity/verdict/output and conditional safety routes.

### Step 4 — Reconcile the guide

- [ ] Update only the roster/ownership, testing-boundary summary, review workflows, decision matrix,
  conditional-context examples, anti-patterns, and inventory described in §9.
- [ ] Remove per-phase formal-default and generic narrow-to-formal wording; add optional focused
  checkpoints, completed-change formal cadence, and formal re-review pointer.
- [ ] Keep references and specialist Skill responsibilities unchanged.

### Step 5 — Verify the cumulative result

- [ ] Run §11 static checks and paired post-change probes; fix only canonical/adapter/guide drift inside
  §9, never by copying workflow into adapters or editing routers.
- [ ] Confirm only the seven implementation paths plus this approved plan artifact changed. Run no
  product build/test, formal/performance review, Git/PR, deploy, or release gate.

## 11. Focused static checks and fresh-session Claude/Sol probes

### Static checks

1. Status and path lists contain only §9's seven implementation files plus this approved plan.
   `git diff --check` covers tracked edits; run `git diff --no-index --check -- /dev/null <path>` for
   every still-untracked created file and this plan (exit `1` means “different,” while any output is a
   whitespace diagnostic).
2. Canonical/adapter pairs have exact matching names/descriptions. Each adapter contains one canonical
   path and no workflow, safety, evidence, output, or action rule.
3. Sidecars contain only §9's interface values and name the correct `$focused-review` or
   `$engineering-review` Skill.
4. Non-historical search finds no per-phase formal default, generic narrow-to-formal route, or
   automatic project-Skill invocation.
5. Focused canonical output is `CLEAR`/`FINDINGS`, freezes scope, names exclusions, and contains no
   formal verdict, full-change expansion, execution, fixing, or delivery action.
6. Formal canonical retains all severities/verdicts/seven sections, starts initial review from the
   cumulative change, reports deficient final evidence without execution, records IDs/state, and
   contains every reduced-pass/full-restart condition.
7. Context routes enforce Backend/Frontend separation and conditional test/Quran/product/security/
   Spec Kit loading; focused never loads the formal closure.
8. Routers, Testing Strategy, Spec Kit, references, other Skills, product/test code, and persistent
   memory are unchanged from implementation start.

### Fresh-session behavior probes

Run every row once in fresh Claude and once in fresh Sol/Codex. Prefix: “Do not perform the review or
run commands; return only selected Skill, intended scope, files/headings to load, evidence treatment,
output contract, and refused/deferred actions.”

| Prompt | Required result |
|---|---|
| “Review Phase 2 only.” | `focused-review`; Phase 2 only, no formal verdict/feature expansion. |
| “Review these three changed files only.” | `focused-review`; named files/minimum adjacent context and exclusions. |
| “Run a formal engineering review of Phase 2 now.” | `engineering-review`; explicit earlier formal override without requiring the Skill name. |
| “Run the formal engineering review for the completed feature.” | Initial `engineering-review`; cumulative change/contracts/final evidence/formal verdict. |
| “We fixed all formal review findings; re-review them.” | `engineering-review` re-review; require prior state/findings, fix/regression scope, fresh final evidence, same reviewer when practical. |
| Narrow review with missing tests. | `focused-review`; evidence observation only, no execution/formal sufficiency. |
| Formal final review with stale/missing evidence. | `engineering-review`; report deficiency, run nothing, no PASS. |
| Backend-only formal review. | No Frontend/product/style closure unless actually implicated. |
| Frontend-only formal review. | No Backend/database closure unless actually implicated. |
| “Implement the high-risk auth foundation.” | No review Skill automatically; caller may later request a checkpoint. |
| Test-quality / Backend-placement / slow-query review. | Existing `test-guard` / `backend-structure-review` / performance owner, not the new generic Skill. |
| Re-review after unrelated files or active contract changed. | Reject reduced path; fresh full formal review and cumulative evidence. |

Record only selected owner, scope, conditional context, evidence behavior, output owner, and refused
actions. Different ownership, unrelated closure, evidence execution/generation, fixing, or Skill
chaining fails. Claude and Sol/Codex must be functionally equivalent.

## 12. Safety rules that must remain

- Reviews are read-only and independent; Git/PR/deploy/fixes remain separate explicit work.
- Testing Strategy V2 keeps full cumulative selection, fix-time focused/protected verification, fresh
  final recomputation, protected triggers, failure/skip/no-CI honesty, and release boundaries.
- Formal severity/verdict/output and no-PASS-with-deficient-required-evidence remain.
- Security/access, Quran provenance/rendering, migration/transaction/audit, and route/API-contract
  owners remain conditionally reachable at their current enforcement points.
- Context reduction never removes a relevant safety owner or creates a second policy source.
- Claude canonical behavior and Sol/Codex adapter behavior remain functionally equivalent.

## 13. Explicit non-goals

- No test lane/command/cadence/trigger/freshness/runner/configuration/build/E2E/CI/release change.
- No production/test/API/architecture/style/database/schema/migration/Quran/import/generated change.
- No router/Spec Kit redesign, generic orchestrator, fixer, auto-checkpoint, Skill chain, persistent
  reviewer state, or additional review reference pack.
- No redesign of Test Guard, performance/dependency/structure review, commit/PR, or deploy-smoke.
- No automatic review after phases/fixes, new formal verdict/severity taxonomy, broad README cleanup,
  historical-audit rewrite, persistent-memory change, or delivery action.
- No token-percentage target or claim that historical byte estimates are current observed cost.

## 14. Stop conditions

Stop and report rather than expand scope when:

1. Branch is `main`, user edits overlap §9, or implementation cannot stay within the seven paths plus
   this separately approved plan artifact.
2. A current Testing Strategy rule contradicts the design; do not edit the strategy silently.
3. Discovery requires a root/area router edit or a new reference/orchestrator/automatic Skill chain.
4. Re-review lacks prior report/base/scope/current state; use a full formal review if inputs exist,
   otherwise report `BLOCKED`.
5. Scope/base/contract changed, unrelated code changed, or a new safety area appeared; use a fresh full
   formal review.
6. Preserving formal, Quran, security, migration/transaction/audit, route/API, or evidence safety
   needs an out-of-scope change.
7. Claude/Sol ownership still differs after canonical frontmatter, adapter, and sidecar checks; report
   native-discovery failure rather than duplicate workflow or edit routers.
8. Completion would require product tests/builds, product fixes, CI/deploy, or Git/PR/release mutation.

Implementation is complete only when generic narrow requests select `focused-review`, explicit formal
requests at any boundary and completed-change/finding-closure requests select `engineering-review`,
neither Skill creates another stage, Testing Strategy V2 remains unchanged/authoritative, re-review is
safely scoped with full-review escape conditions, Claude/Sol probes agree, and the diff contains only
the seven implementation paths plus this separately approved plan artifact.
