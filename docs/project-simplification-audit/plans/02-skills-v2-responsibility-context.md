# Skills V2 — Responsibility & Context Simplification Implementation Plan

Either Claude or Sol/Codex can execute this plan directly and sequentially. It requires no
delegation, external Skill, or new execution framework.

**Goal:** Give each of the 10 project-authored Skills one explicit responsibility, reduce recurring
frontmatter and on-demand context, remove duplicated policy and hidden workflow stages, and keep
Claude and Sol/Codex behavior functionally equivalent.

**Architecture:** `.claude/skills/` remains the only canonical home of project Skill behavior.
Each `.agents/skills/` package contains a short, trigger-equivalent pointer to that canonical Skill;
it does not summarize the workflow or enumerate references. The implemented V2 native router →
area router → nearest README → triggered specialist-source chain remains unchanged.

**Mechanisms:** Short trigger-only descriptions, compact canonical Skill bodies, exact heading
pointers to existing owners, conditional reference loading, thin adapters, and focused static and
read-only behavior probes. No production code, test code, test cadence, review cadence, API,
architecture, styling, memory, or Spec Kit behavior changes.

The brief names `10-build-test-review-workflow-cost-audit.md`; the current repository source is
`docs/project-simplification-audit/10-build-workflow-cost-audit.md`, which is the evidence used by
this plan.

## Global constraints

- Treat Workflow & Instruction Routing V2 as fixed. Do not redesign or broaden any root or area
  `CLAUDE.md`/`AGENTS.md` route.
- Keep all 10 custom Skills. `backend-structure-review` overlaps the holistic review, but its
  explicit, cheaper structure-only decision/review remains a distinct responsibility; current
  evidence does not justify deleting or merging it.
- One Skill owns one result. A Skill may inspect evidence needed to produce that result, but must
  not silently add another Skill's build, test, review, fix, Git, PR, performance, dependency, or
  deployment stage.
- No project Skill automatically invokes another project Skill. `engineering-review` may consume an
  existing same-diff Test Guard result; when required Test Guard evidence is missing for changed test
  files, it reports the evidence as missing/incomplete. The caller/orchestrator/user decides whether
  to invoke `test-guard` separately, and the formal review retains the final verdict.
- Do not create a shared testing-policy reference. Replace copied testing rule bodies with exact
  `TESTING_STRATEGY.md` heading pointers, loaded only when the Skill must classify existing evidence.
- Keep the existing small Quran safety reference, but load it only for source-sensitive or
  Quran-rendering scope. Do not copy its rules back into consumer Skills.
- Move or point every unique safeguard before deleting a duplicate reference. A deletion is blocked
  by either an unrepaired inbound reference or a rule with no confirmed canonical destination.
- Keep project Skills independent of Superpowers and other environment-level Skills. Those tools
  remain optional and user/agent-invoked.
- Do not commit, push, open a PR, deploy, run a formal review, or execute broad verification while
  implementing this plan unless the user separately requests that action.

---

## 1. Current Skill-system problems

1. Canonical descriptions are long workflow summaries—several approach or exceed 1 KB—and Claude
   injects every description into every session's Skill roster. Adapter descriptions repeat a
   second, sometimes different summary.
2. Some descriptions still claim automatic triggers that V2 removed: `engineering-review` claims
   unrequested review work, `backend-structure-review` claims ordinary new files, and
   `deploy-smoke` claims pre-review/pre-PR execution.
3. `commit-workflow` calls itself Git-only but directs the agent to run Backend/Frontend checks;
   its repeated pre-commit inventory also obscures the small set of Git-integrity checks it owns.
4. `pr-context-prep` correctly knows how to consume existing evidence, but its merge-readiness and
   invariant language can turn packaging into a second engineering review. Its copied testing block
   duplicates canonical policy.
5. `engineering-review` has a large effective closure: whole policy documents, embedded Backend and
   Frontend checklists, a second Test Guard summary, and another copy of testing-evidence rules. Its
   mandatory output shape makes nominally conditional reads effectively unconditional.
6. `test-guard` owns nine test-code rules but also repeats test-lane/evidence-sufficiency policy,
   reads both native entrypoints, and can load irrelevant stack or future-technology references.
7. Both performance Skills repeat general architecture, test-quality, harness, and Quran-safety
   checks. `deploy-smoke` also expands into tests, dependency installation, Git status, and unsafe or
   ambiguous migration/runtime setup.
8. `dependency-audit` crosses from audit/reporting into package remediation, builds, tests, and
   runtime smoke. `backend-global-usings-cleanup` similarly restates layer and test-selection policy
   instead of owning only import consolidation plus focused compilation.
9. The clean-code reference pack repeats canonical principles and includes a second severity/output
   contract. The test reference pack contains generic Jest/React advice in an Angular/Vitest project,
   duplicated harness/runtime law, and an LLM reference with no current production consumer.
10. Thin adapters are structurally pointers, but their descriptions, summaries, reference lists,
    and a few `openai.yaml` starter prompts are enough to drift. The live test-guard adapter already
    omits one canonical reference from its enumeration.

## 2. Target ownership architecture

For every invocation, use this fixed shape:

```text
already-loaded native V2 route + user request
  -> one canonical .claude Skill
  -> its responsibility and local evidence
  -> only the exact conditional reference needed for that scope
  -> that Skill's result
```

The canonical Skill body contains only:

1. its single responsibility and explicit non-responsibilities;
2. the minimum workflow needed to produce its owned result;
3. safeguards specific to that workflow;
4. exact conditional pointers to existing canonical owners; and
5. a compact output contract.

The `.agents` adapter contains only matching discovery metadata, the exact canonical path, and an
instruction to read the canonical file in full. It contains no workflow summary, reference list,
safety restatement, or alternative output contract. Sol/Codex follows the canonical behavior after
the pointer; Claude loads it directly. Functional equivalence is measured by behavior probes, not
byte identity.

Evidence is separated from execution:

- A review or packaging Skill may consume current evidence and report missing, stale, skipped, or
  unknown evidence honestly; it does not produce new build/test/deploy evidence.
- An action Skill may perform only the focused verification necessary to establish that its own
  mutation works. It does not add formal review or delivery stages.
- A read-only operational Skill may perform observations inherent to its responsibility, such as a
  dependency scan, a read-only query plan, or a requested runtime health request. Those observations
  do not authorize remediation or unrelated mutation.

## 3. Responsibility and context matrix

V2 has already loaded the native router and nearest README. “Required” below is invocation evidence;
“conditional” means an exact heading/path, never the whole document.

| Skill | Owns | Never owns | Required; conditional context |
|---|---|---|---|
| `backend-global-usings-cleanup` | Import-only consolidation using the existing `>5`-files rule; preserve feature/generated imports; compile affected projects. | Tests, reviews, broad refactors, docs, Git. | Requested/touched C# projects; Global Usings/layer heading only if scope is ambiguous, Clean Architecture only for a disputed boundary. |
| `backend-structure-review` | Explicit placement/layer/file-responsibility advice or focused findings. | Auto-fire on new files, holistic/Quran/API/test/performance review, fixes, build, Git. | Requested paths and relevant Structure heading; Clean Architecture, API, size, or Quran owner only when implicated. |
| `commit-workflow` | Requested branch/stage/commit/push/PR-open/sync operation; explicit staging, cached-diff inspection, `git diff --cached --check`, `main` protection, `dev` sync. | Builds, tests, review, deploy, fixes, automatic PR prep. | Root Git state; remote/PR state only for an explicitly requested remote action. Existing evidence is not a Git gate. |
| `dependency-audit` | NuGet/npm vulnerability, advisory, transitive-parent, and staleness scan/report with remediation options. | Package/lock edits, restore/build/test/smoke, suppression, Git. | Real manifests/lockfiles and scan output; parent graph, outdated scan, harness, or Quran owner only when relevant. |
| `deploy-smoke` | Explicit deployment preflight/local runtime smoke; masked target, contract check, owned process lifecycle, health/runtime observations. It may build only a missing targeted deployable artifact required by the smoke. | Proactive gates, broad tests, route-smoke substitution, install, source/Git change, remote deploy, destructive data or unapproved migration action. | Requested target and deployment/runtime README; effective config, deploy files, scripts, frontend config, or Quran safety only for the requested path. |
| `engineering-review` | Explicit formal review findings/verdict with unchanged cadence/severity; consume current evidence and an existing same-diff Test Guard result when available, otherwise report required Test Guard evidence as missing/incomplete. | Fixes, builds/tests, Git/PR/deploy, dependency/performance audit, unrequested review, full-doc loading, or invoking Test Guard/another Skill. | Actual diff/content and active contract; only implicated principles/architecture/security/product/style/testing/Quran/Spec Kit headings. An existing same-diff Test Guard result is conditional evidence, not a Skill/reference-loading route. |
| `performance-angular-review` | Evidence-based UI render/state/DOM/network/bundle/CSS/memory/test-runtime performance review. | General engineering/architecture/accessibility/test-quality review, speculation, execution, fixes, other Skills. | Changed/reported path; exact frontend docs, package/harness, Quran safety, or browser evidence only when the finding requires it. |
| `performance-backend-review` | Evidence-based .NET/EF/PostgreSQL performance review using real paths and read-only measurement when needed. | General architecture/test-quality review, speculative caching/indexing, mutation, execution, fixes, other Skills. | Changed/reported path and callers/query path; exact architecture/EF/migration/test-runtime/security/audit/Quran owner only when implicated. |
| `pr-context-prep` | Copy-paste PR package from branch/three-dot diff and existing evidence. | File write, Git/PR mutation, evidence rerun, formal review, independent readiness verdict, fixes, other Skills. | Branch/status/diff; existing reports, active docs, exact testing headings, or Quran/identity owner only when relevant. |
| `test-guard` | Test-code quality guidance/review against the nine rules; formal verdict stays with engineering review. | Production review, test selection/execution/evidence sufficiency, test fixes, Git, unrelated stack refs. | Changed/proposed tests; `dotnet.md` or `jest.md` by stack, both only for cross-stack tests; harness/PostgreSQL/Quran owner only when implicated. |

## 4. Context-loading rule

No Skill rereads either root entrypoint, asks Claude to read `AGENTS.md`, asks Sol/Codex to read
`CLAUDE.md`, or treats a conditional route as permission to read a long owner in full.

## 5. Frontmatter and adapter contract

Use exactly one short discovery sentence as `description:` in both the canonical Skill and its
adapter. It describes **when** to use the Skill, not its workflow, exclusions, output, or safety
rules.

| Skill | Canonical and adapter description |
|---|---|
| `backend-global-usings-cleanup` | `Use when asked to consolidate repeated C# using directives or GlobalUsings.cs files in the Quran Dashboard backend.` |
| `backend-structure-review` | `Use when asked for a focused review or placement decision about Quran Dashboard backend folders, files, layer boundaries, or file-responsibility thresholds.` |
| `commit-workflow` | `Use when asked to inspect, branch, stage, commit, push, open a PR, or synchronize Git state in the Quran Dashboard monorepo.` |
| `dependency-audit` | `Use when asked to audit Quran Dashboard NuGet or npm dependencies for vulnerabilities, advisories, transitive exposure, or staleness.` |
| `deploy-smoke` | `Use when explicitly asked for a Quran Dashboard deployment preflight or local runtime smoke check.` |
| `engineering-review` | `Use when explicitly asked for the Quran Dashboard formal engineering review of code, a diff, branch, PR, phase, or completed implementation.` |
| `performance-angular-review` | `Use when explicitly asked for an Angular/frontend performance review or when a Quran Dashboard UI path is reported as slow, janky, memory-heavy, or request-heavy.` |
| `performance-backend-review` | `Use when explicitly asked for a backend/database performance review or when a Quran Dashboard query, endpoint, import, transaction, or backend test path is reported as slow.` |
| `pr-context-prep` | `Use when asked to prepare a PR title, description, scope, evidence summary, or reviewer context package for the Quran Dashboard.` |
| `test-guard` | `Use when explicitly asked for Quran Dashboard test-code quality guidance or review.` |

Each adapter keeps its existing Skill name, uses the matching sentence above, points to
`.claude/skills/<name>/SKILL.md`, and says to read that file in full. Remove adapter-owned summaries,
reference inventories, “source of truth” behavior lists, and exclusions. `agents/openai.yaml` remains
interface metadata, not a second rule source; update only the four stale/ambiguous starter prompts
listed in §7.

## 6. Reference and duplication simplification

### Engineering review references

- Keep and shorten `references/clean-code-guard/ai-failure-modes.md` to project-relevant AI review
  failure modes not already covered by `CODING_PRINCIPLES.md`; point each general principle to the
  exact canonical heading.
- Keep and shorten `references/clean-code-guard/review-checklist.md` to an optional traversal aid.
  Remove its independent severity, output, root-entrypoint, and mandatory-read rules; the parent
  Skill owns severity and output.
- Delete the generic `comments-and-formatting.md`, `dry-kiss-yagni.md`,
  `naming-and-functions.md`, `solid.md`, and `sources.md` copies after inbound pointers are repaired.
  `CODING_PRINCIPLES.md` §§2–4 and §7 remain canonical; project overrides already recorded there
  survive.
- Keep `references/quran-data-safety.md` as the existing small conditional safety reference. Make it
  consumer-neutral, point to `CODING_PRINCIPLES.md` §10,
  `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md` §13, and the nearest source-pipeline
  README, and retain only cross-area safeguards not fully expressed by those owners. Consumer Skills
  keep only severity/application wording, not copied rule bodies.
- In `SPEC_KIT_IMPLEMENTATION_REVIEW.md`, replace the circular/stale root-entrypoint lifecycle route
  with the exact `docs/README.md` Lifecycle heading. Do not change Spec Kit cadence or any Spec Kit
  Skill.

### Test Guard references

- Keep the filename `references/jest.md` so the implemented V2 area routers need no pointer change,
  but rewrite it as a concise Angular/Vitest reference using only dependencies and patterns present
  in this project. Remove React hooks, Testing Library, and `msw` assumptions.
- Trim `references/dotnet.md` to .NET/xUnit-specific applications of the nine rules. Replace copied
  PostgreSQL serialization, test-lane, API, and Quran policy with exact conditional owner pointers.
- Trim `references/frontend-test-harness-constraints.md` to the project-specific jsdom/browser,
  fork/resource, and known-safe harness constraints not already owned by the frontend package and
  testing READMEs. Remove copied lane/cadence/command bodies and its named-consumer list; each
  canonical Skill declares its own conditional route.
- Delete `references/llm-app-testing.md` and its inbound pointers. Current package manifests and
  production source contain no LLM client or agent/workflow dependency, and this dormant generic
  reference adds no current project behavior. If the implementation preflight finds such a real
  dependency, stop and retain the file rather than guessing.

### Embedded duplication

- Replace the TESTING_STRATEGY blocks in `engineering-review`, `test-guard`, and `pr-context-prep`
  with exact conditional heading pointers. Do not create the shared testing reference proposed by
  the older audit; the independent review identifies direct canonical pointers as the safer V2
  shape.
- Replace Backend/Frontend checklist copies in `engineering-review` and
  `backend-structure-review` with the exact V2-routed architecture headings while retaining compact
  project-specific severity/application rules.
- Remove copied layer tables from `backend-global-usings-cleanup`, copied harness/Quran bullets from
  both performance Skills, copied Quran invariant tables from `pr-context-prep`, and copied
  remediation verification from `dependency-audit`.
- Record the existing prohibition on religious “God service” terminology once in the already
  long-lived `SKILLS_AND_ARCHITECTURE_GUIDE.md` as `#### Review terminology` under its
  `engineering-review` section, using neutral terms such as “monolithic,” “overloaded,” or
  “multi-responsibility service”; both review Skills point to that heading. This preserves the
  currently orphaned project rule without creating a new meta-layer.

## 7. Exact implementation file set

### Canonical Skill bodies — modify all 10

- `.claude/skills/backend-global-usings-cleanup/SKILL.md`
- `.claude/skills/backend-structure-review/SKILL.md`
- `.claude/skills/commit-workflow/SKILL.md`
- `.claude/skills/dependency-audit/SKILL.md`
- `.claude/skills/deploy-smoke/SKILL.md`
- `.claude/skills/engineering-review/SKILL.md`
- `.claude/skills/performance-angular-review/SKILL.md`
- `.claude/skills/performance-backend-review/SKILL.md`
- `.claude/skills/pr-context-prep/SKILL.md`
- `.claude/skills/test-guard/SKILL.md`

### Sol/Codex adapter pointers — modify all 10

- `.agents/skills/backend-global-usings-cleanup/SKILL.md`
- `.agents/skills/backend-structure-review/SKILL.md`
- `.agents/skills/commit-workflow/SKILL.md`
- `.agents/skills/dependency-audit/SKILL.md`
- `.agents/skills/deploy-smoke/SKILL.md`
- `.agents/skills/engineering-review/SKILL.md`
- `.agents/skills/performance-angular-review/SKILL.md`
- `.agents/skills/performance-backend-review/SKILL.md`
- `.agents/skills/pr-context-prep/SKILL.md`
- `.agents/skills/test-guard/SKILL.md`

### Adapter interface metadata — modify only these four

| File | Exact repair |
|---|---|
| `.agents/skills/commit-workflow/agents/openai.yaml` | Describe performing the explicitly requested Git operation, not merely planning a commit. |
| `.agents/skills/dependency-audit/agents/openai.yaml` | Ask for an audit/report and remediation options, not “fixes.” |
| `.agents/skills/deploy-smoke/agents/openai.yaml` | Remove automatic build/pre-review/pre-commit language; request only deployment preflight or runtime smoke. |
| `.agents/skills/pr-context-prep/agents/openai.yaml` | Remove the independent merge-readiness verdict and request a package from existing diff/evidence. |

The other six sidecars already describe their owning responsibility and are not cleanup targets.

### Project-owned references and direct documentation — modify

- `.claude/skills/engineering-review/SPEC_KIT_IMPLEMENTATION_REVIEW.md`
- `.claude/skills/engineering-review/references/clean-code-guard/ai-failure-modes.md`
- `.claude/skills/engineering-review/references/clean-code-guard/review-checklist.md`
- `.claude/skills/engineering-review/references/quran-data-safety.md`
- `.claude/skills/test-guard/references/dotnet.md`
- `.claude/skills/test-guard/references/frontend-test-harness-constraints.md`
- `.claude/skills/test-guard/references/jest.md`
- `CODING_PRINCIPLES.md` — repair only the clean-code reference paragraph so §§2–4 and §7 remain
  canonical and the two retained on-demand references are named accurately.
- `SKILLS_AND_ARCHITECTURE_GUIDE.md` — replace detailed duplicate workflows with the ownership
  summary/pointers in this plan, update the reference inventory, and canonically retain the neutral
  terminology rule. Do not turn it into another workflow router.

### Project-owned references — delete after the reference gate

- `.claude/skills/engineering-review/references/clean-code-guard/comments-and-formatting.md`
- `.claude/skills/engineering-review/references/clean-code-guard/dry-kiss-yagni.md`
- `.claude/skills/engineering-review/references/clean-code-guard/naming-and-functions.md`
- `.claude/skills/engineering-review/references/clean-code-guard/solid.md`
- `.claude/skills/engineering-review/references/clean-code-guard/sources.md`
- `.claude/skills/test-guard/references/llm-app-testing.md`

No other file is expected to change. In particular, do not modify root/area `CLAUDE.md` or
`AGENTS.md`, `TESTING_STRATEGY.md`, any production/test/API/architecture/style file, persistent
memory, Spec Kit Skill, or historical audit report. The `jest.md` path is deliberately retained to
avoid a V2 router change.

## 8. Responsibility overlaps to remove

| Current overlap | Sole owner after this plan | Boundary |
|---|---|---|
| Pre-commit builds/tests/reviews | Implementation workflow and canonical testing/review triggers | `commit-workflow` performs only Git-integrity checks and the requested Git action. |
| PR evidence production and merge verdict | Evidence producer / `engineering-review` | `pr-context-prep` packages supplied/current evidence and states absence without rerunning or adjudicating readiness. |
| Test-code quality vs test-evidence sufficiency | Separately invoked `test-guard` for code quality; `engineering-review` for final review evidence | `TESTING_STRATEGY.md` remains the policy owner; neither Skill copies it. Engineering Review consumes an existing same-diff result or reports required Test Guard evidence as missing/incomplete; the caller/orchestrator/user owns any separate invocation. |
| Structure review inside ordinary Backend work | Explicit `backend-structure-review` or the requested formal `engineering-review` | The focused Skill never auto-fires; the formal Skill routes to canonical headings rather than a second checklist. |
| Test-quality checks inside performance reviews | `test-guard` | Performance Skills may report measured test-runtime cost, not assertion/mocking/style quality. |
| Architecture/accessibility inside performance reviews | Architecture/formal review owners | Performance Skills discuss only performance-caused boundary or accessibility tradeoffs. |
| Builds/tests/dependency/Git inside runtime smoke | Implementation/testing/dependency/commit owners | `deploy-smoke` owns only the minimum targeted artifact prerequisite and requested deployment/runtime observations. |
| Package remediation inside dependency audit | Later explicit implementation task | Audit returns evidence and options only. |
| Test selection inside action/review/package Skills | `TESTING_STRATEGY.md` and the explicitly requested implementation workflow | Skills point to policy only when they must label existing evidence; they do not run lanes. |
| Quran/harness/layer copied bullets | Existing canonical reference/document | Consumer Skills retain only scope-specific application and severity language. |

## 9. Safety rules that must remain reachable

| Protection | Canonical owner/reachability after Skills V2 | Acceptance condition |
|---|---|---|
| `main`/`dev`, staging, commit, push, PR, and destructive Git | V2 root kernel + `commit-workflow`; current `dev` sync contract | `main` remains protected production; explicit paths and staged inspection remain; no force/unsolicited push; commit/push/PR authority is explicit; post-merge sync requires confirmed merge and targets `dev`. |
| Auth/security | `docs/contracts/security-access.md`, `Backend/.architecture/API_GUIDELINES.md` §11, nearest auth/access README, conditionally routed by the formal/performance review | No Skill summary substitutes for bearer/identity/authorization contracts; relevant reviews cannot skip the exact owner. |
| Quran/source data and display | V2 kernel, `CODING_PRINCIPLES.md` §10, trimmed `quran-data-safety.md`, `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md` §13, nearest importer/pipeline README | No invention, silent correction, provenance loss, unsafe fallback, source mutation, readability/RTL/selection regression, or performance-over-correctness tradeoff disappears. |
| Migration/schema and target safety | `Backend/README.md` Invariants/Deployment, `Backend/scripts/README.md`, nearest migration README, `deploy-smoke` target gate | Target is confirmed and secrets masked before connecting; inspection is read-only by default; generation/apply/destructive work requires separate explicit authority. |
| Transactions and audit | Nearest feature/persistence README and implicated backend contract/architecture heading | Performance/deployment recommendations cannot weaken atomicity, audit records, rollback, or correctness. |
| PostgreSQL test serialization | `TESTING_STRATEGY.md` §3.3 + `Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/README.md` | Test Guard/engineering/performance routes there only for real PostgreSQL fixture/runtime scope; no copied divergent rule remains. |
| Canonical data fail-not-skip | `TESTING_STRATEGY.md` §§3.4 and 9 | Formal review classifies existing canonical evidence through those headings; Test Guard and PR prep do not own or restate the rule. |
| Route/auth/binding/serialization evidence | `TESTING_STRATEGY.md` §6 | Runtime curls are labeled smoke observations and never presented as route-smoke evidence. |
| Review severity/verdict and final-vs-phase cadence | `engineering-review` plus unchanged repository cadence | Compacting the Skill does not change when formal review occurs, its severity meanings, or its PASS/CHANGES-REQUESTED contract. |
| Neutral service terminology | `SKILLS_AND_ARCHITECTURE_GUIDE.md` `#### Review terminology` | Both structure and engineering reviews use neutral technical descriptions; the current orphaned prohibition is not lost during deduplication. |

## 10. Small implementation sequence

### Step 1 — Freeze the live graph and safety destinations

- [ ] Confirm the branch is not `main`, capture one root `git status --short`, and establish §7 as
  the cumulative allowlist.
- [ ] Run an inbound-reference scan for every reference named for deletion and every
  `CLAUDE.md`/`AGENTS.md` read inside the 10 Skill packages. Classify live consumers before editing.
- [ ] Reconfirm production/package manifests contain no LLM/agent framework. If they do, retain
  `llm-app-testing.md` and stop to revise this plan's deletion set.
- [ ] Map each protection in §9 to exact surviving text. Stop before deletion if any unique rule has
  no destination.

### Step 2 — Establish compact reference owners before removing copies

- [ ] Add the neutral terminology rule to the existing Skill guide and update only its 10-Skill
  ownership/reference sections.
- [ ] Slim the two retained clean-code references, the three retained test references, and the Quran
  reference as specified in §6. Repair the Spec Kit add-on's lifecycle pointer without changing
  Spec Kit behavior.
- [ ] Repair `CODING_PRINCIPLES.md`'s clean-code pointer, then delete only the six references listed
  in §7. Re-run the inbound-reference scan immediately; any live dangling consumer blocks progress.

### Step 3 — Narrow canonical behavior

- [ ] Rewrite the 10 canonical `SKILL.md` files to the responsibility, non-responsibility, context,
  and output contracts in §§3–4. Preserve the safety table and unchanged review cadence.
- [ ] Remove copied TESTING_STRATEGY/checklist/reference bodies and every automatic Skill chain.
  Engineering Review consumes a current same-diff Test Guard result when available and otherwise
  reports required evidence as missing/incomplete; it never invokes Test Guard.
- [ ] For the two action/operational exceptions, state the limits precisely: global-usings may compile
  affected projects, and deploy-smoke may build only a missing targeted deployable artifact required
  by the requested smoke. Neither may add broad verification.

### Step 4 — Replace adapter summaries with pointers

- [ ] Apply the exact §5 description to each canonical Skill and matching adapter.
- [ ] Reduce every adapter body to its canonical path and read-in-full instruction; remove its
  behavior summary and reference enumeration.
- [ ] Apply only the four `openai.yaml` repairs in §7. Check the remaining six for contradiction,
  but do not rewrite accurate metadata for style.

### Step 5 — Verify the cumulative result

- [ ] Run all static checks and fresh Claude/Sol probes in §11 against the same cumulative diff.
- [ ] Compare Claude and Sol/Codex results for selected Skill, conditional references, actions taken,
  and actions refused. Fix canonical/adaptor routing drift, not by copying workflow into adapters.
- [ ] Confirm the final diff contains only §7's allowlist plus this approved plan artifact and that no
  test lane, review cadence, router, production, test, API, architecture, styling, memory, or Spec Kit
  Skill file changed.

## 11. Focused verification and Claude/Sol probes

### Static checks

1. `git diff --check` passes, and `git diff --name-only` is limited to §7 plus this plan artifact.
2. All 10 canonical and adapter frontmatters parse, retain the existing `name`, and use the exact
   matching one-sentence description in §5. No description contains workflow steps, exclusions,
   reference inventories, or automatic “before review/commit/PR” language.
3. Every `.agents/skills/<name>/SKILL.md` contains exactly one canonical Skill path and no independent
   build/test/review/Git/safety/reference rule body.
4. A live `rg --hidden` scan outside the historical audit pack finds no pointer to the six deleted
   references and no project Skill instruction to read both `CLAUDE.md` and `AGENTS.md`.
5. The three former testing-policy copies are gone. Their surviving text names exact
   `TESTING_STRATEGY.md` headings only; no new shared testing-reference file exists.
6. No canonical project Skill instructs the agent to invoke another project Skill.
   `engineering-review` consumes an existing same-diff Test Guard result or reports it
   missing/incomplete; `commit-workflow` contains no build/test/review command; `pr-context-prep`
   produces no evidence or independent readiness verdict; `dependency-audit` contains no remediation
   mutation; `test-guard` contains no lane/evidence-sufficiency decision.
7. `deploy-smoke` contains no automatic pre-review/pre-commit trigger, broad test command, package
   install, Git status, remote deploy, or unapproved migration action. Its process cleanup and masked
   target confirmation are explicit.
8. Each performance Skill retains review-only/evidence/anti-noise/Quran-correctness safeguards but no
   general review or test-code-quality checklist. `backend-global-usings-cleanup` retains the
   threshold/import-only/generated-file rules and only focused compile verification.
9. The 10 Skills, 10 adapters, retained references, and four modified sidecars all exist where
   expected; no in-scope Skill was deleted or merged. The retained `jest.md` route resolves for both
   native frontend routers.
10. Manually compare every removed safeguard against §9. Reduced byte/line counts are useful
    observations, not acceptance criteria; protection and behavior win over size.

### Fresh-session behavior probes

Run every row once in a fresh Claude session and once in a fresh Sol/Codex session as a routing dry
run. Prefix each prompt with “Do not perform the task; return only the route and intended actions.”
Record only: selected Skill, always/conditional files, owned actions, refused/deferred actions, and
output owner. Both agents must be functionally equivalent.

| Probe | Prompt shape | Required result |
|---|---|---|
| Git vs PR package | “Commit these already-reviewed changes; do not push,” then separately “Prepare the PR body from this diff and the supplied evidence.” | Commit runs only Git-integrity/stage/commit work and does not rerun evidence or invoke PR prep. PR prep returns context, consumes evidence, and neither reviews nor opens the PR. |
| Formal review without/with tests | Review a small production-only diff, then a diff containing one .NET test file with an attached current Test Guard result, then the same test-bearing diff without that result. | First review loads no test/Quran/full architecture closure unless implicated. Second consumes the same-diff result and only necessary testing headings. Third reports required Test Guard evidence as missing/incomplete. None invokes or generates Test Guard evidence. |
| Test stack routing | Ask for review of one .NET test, then one Angular/Vitest test whose harness is not implicated. | Only `dotnet.md` or `jest.md` loads respectively; no other stack, harness, LLM, test-lane, or production review loads. |
| Performance boundaries | Ask for a backend slow-query review and a frontend jank review on representative changed paths. | Each selects only its performance Skill, uses evidence/conditional owners, makes no general-review/test-quality finding, mutation, build, or automatic Skill call. |
| Runtime and dependency boundaries | Ask for a local health smoke with a fresh artifact, then an npm/NuGet vulnerability audit. | Smoke confirms/masks target, owns process cleanup, and runs no tests/install/Git work. Audit scans/classifies/recommends only and makes no package/build/runtime mutation. |
| Focused Backend Skills | Ask to consolidate repeated imports in one project, then ask where a new handler belongs without requesting a holistic review. | Global-usings limits edits and focused compilation to the affected project. Structure review is read-only, loads exact structure context, and does not trigger engineering review/build/tests. |

Any probe that selects a different responsibility, loads an unrelated reference, or performs an
extra workflow stage fails Skills V2 even if its final prose looks reasonable.

## 12. Non-goals

- No root or area router redesign and no change to the V2 native-entrypoint architecture.
- No change to `TESTING_STRATEGY.md` lanes, commands, cadence, trigger matrix, failure semantics, or
  no-CI policy.
- No redesign of Engineering Review's final-vs-phase cadence, severity model, verdict, or larger
  lifecycle. A later plan owns any broader review workflow redesign.
- No production code, test file, API behavior, architecture rule, styling, deployment configuration,
  database/schema, or persistent-memory change.
- No Spec Kit Skill redesign or edit. The engineering-review add-on receives only a direct lifecycle
  pointer repair.
- No new fixer, orchestration Skill, shared testing card, giant reference pack, adapter generator,
  meta-router, test matrix, or mandatory external/Superpowers workflow.
- No custom Skill deletion or merge, no unrelated documentation cleanup, and no rewrite of historical
  audit evidence.
- No commit, push, PR opening, deployment, package upgrade, migration apply, or formal review as part
  of executing this implementation plan unless separately authorized.

## 13. Stop conditions

Stop and report instead of expanding scope when any of the following is true:

1. The current branch is `main`, the worktree contains overlapping user changes, or the required diff
   cannot be isolated to §7.
2. A deleted/trimmed reference contains a unique safety or project rule with no exact surviving
   owner, or a live inbound pointer cannot be repaired inside §7.
3. A real production LLM/agent dependency is found; retain `llm-app-testing.md` and request a plan
   correction rather than deleting relevant guidance.
4. Preserving behavior would require changing a root/area router, a test lane/cadence/trigger, the
   Engineering Review cadence/verdict, a Spec Kit Skill, or production/test/API/architecture/style
   behavior.
5. A Skill cannot be narrowed without moving genuinely owned work to a new Skill or shared
   meta-layer. Do not invent that layer in this plan.
6. Claude and Sol/Codex probes disagree after the canonical path and adapter pointer are verified.
   Diagnose the native mechanism; do not restore duplicated behavior in `.agents`.
7. Deployment target, database target, migration authority, Git destination, commit/push/PR authority,
   Quran/source provenance, or required verification evidence is ambiguous. Preserve the current
   safety boundary and ask for direction.

The implementation is complete only when all 10 responsibilities are exclusive, all safety owners
remain reachable, all adapters resolve to canonical behavior, the focused probes agree across Claude
and Sol/Codex, and the cumulative diff stays within the exact file set above.
