# QuranFullStack — Project Simplification, Cost Reduction & Agent Context Audit

## Canonical Audit Brief

This is the **single source of truth** for a read-only, repository-wide audit of the Quran Dashboard / **المنهج القرآني** project.

Do not look for a second audit brief.  
Do not create a master implementation plan from this file.

The purpose of this audit is to answer:

> How can this project become substantially simpler, smaller, faster to understand, cheaper to develop, cheaper to test, and cheaper for coding agents to operate — **without weakening security, authorization, auditability, database correctness, Quran-data safety, source integrity, API correctness, or maintainability?**

The required result is a set of **high-quality evidence-based reports** that will later be reviewed independently and converted into **multiple small remediation plans**.

This audit is not itself a remediation plan.

---

# 1. Operating principle

The project has accumulated multiple possible sources of recurring cost:

- large and slow test suites,
- duplicated or excessive agent instructions,
- project-authored Skills that may pull too much context,
- persistent/retrieved memory that may contain stale feature chatter,
- many local READMEs and Markdown decision sources,
- architecture layers that may be useful or may be pass-through,
- large frontend style volume despite Tailwind being installed,
- repeated component SCSS and repeated visual patterns,
- API endpoints and response fields that may over-fetch,
- generated/support code that can make the repository look larger than the handwritten product really is,
- build/test/review gates that may execute more often than necessary.

The audit must distinguish:

> **complexity that buys real safety/value**

from:

> **complexity that buys little or nothing and makes every future feature more expensive.**

Do not optimize for “fewest files”, “fewest tests”, “fewest layers”, or “Tailwind everywhere” blindly.

Optimize for:

- lower recurring feature cost,
- lower agent context cost,
- lower maintenance cost,
- shorter feedback loops,
- simpler reasoning,
- fewer accidental abstractions,
- less duplicated knowledge,
- smaller API/query/payload surface,
- strong protection of high-risk invariants.

---

# 2. Repository baseline and start gate

Repository:

```text
Mohamed0Ahmed/QuranFullStack
```

Expected branch:

```text
dev
```

Remote `dev` was inspected after the Access Management merge.

Observed remote baseline at audit-brief preparation time:

```text
commit: 72792ba9ff589c66aa25632a464b56b8bf7787af
message: Merge access catalogue readiness feature
date: 2026-08-08T16:21:59Z
```

This is orientation evidence only. The local repository is the audit source of truth.

Before any audit work, verify locally:

```bash
git branch --show-current
git status --short
git rev-parse HEAD
```

Required state:

```text
branch = dev
working tree = clean
HEAD = the intended current dev baseline
```

If the working tree is dirty, the branch is not `dev`, or the local branch is stale/incorrect:

```text
BLOCKED — baseline is not clean/stable
```

Do not silently audit a feature branch or dirty tree.

Record the actual local commit SHA in the audit index.

---

# 3. Audit mode

## Read-only

Allowed:

- inspect/search repository files,
- inspect Git history when it materially helps,
- run non-destructive repository commands,
- measure LOC/file counts,
- run safe builds/tests/benchmarks when needed,
- inspect Swagger/OpenAPI,
- inspect generated code,
- inspect query/projection code,
- inspect test/runtime scripts,
- generate audit reports and audit-only HTML/JSON artifacts under the audit output folder.

Forbidden:

- no production-code refactor,
- no test deletion,
- no Skill modification,
- no AGENTS/CLAUDE/README modification,
- no style migration,
- no endpoint contract changes,
- no migrations,
- no database drop/reset,
- no importer/reseed mutation unless separately approved,
- no remote/shared/staging/production database mutation,
- no commit,
- no push.

The workflow/orchestration strategy is Fable's responsibility.

Do not treat this brief as instructions for how many agents, workers, branches, or concurrent tasks to use.

Choose whatever internal workflow best produces reliable evidence, while respecting repository safety constraints and avoiding measurements distorted by resource contention.

---

# 4. Evidence standard

Every important conclusion must be classified as one of:

- `CONFIRMED`
- `LIKELY`
- `NEEDS_MEASUREMENT`
- `UNKNOWN`

Every proposed simplification must answer:

1. What value does the current thing provide?
2. What depends on it?
3. What risk exists if it changes or disappears?
4. Is equivalent protection already present elsewhere?
5. What is the smallest safe simplification?
6. How would that simplification be verified later?
7. What recurring cost would it remove?

Never recommend deletion merely because something “looks unused”.

Never assume:

- one implementation means an interface is useless,
- no Angular consumer means an API field is safe to delete,
- a README is waste because it is long,
- a test is waste because it is slow,
- Tailwind should replace every semantic/shared style,
- generated code is handwritten complexity,
- fewer endpoints automatically means better API design.

---

# 5. Required output structure

Create the audit under:

```text
docs/project-simplification-audit/
```

Use multiple focused reports.

Recommended structure:

```text
docs/project-simplification-audit/
├── 00-audit-index.md
├── 01-executive-summary.md
├── 02-test-suite-audit.md
├── 03-agent-context-instruction-audit.md
├── 04-custom-skills-audit.md
├── 05-memory-context-audit.md
├── 06-readme-markdown-decision-audit.md
├── 07-frontend-styling-audit.md
├── 08-architecture-code-size-audit.md
├── 09-api-surface-payload-audit.md
├── 10-build-workflow-cost-audit.md
├── 11-cross-cutting-priorities.md
├── 12-post-audit-review-handoff.md
├── data/
│   ├── test-inventory.json
│   ├── loc-inventory.json
│   ├── instruction-inventory.json
│   ├── skill-inventory.json
│   ├── markdown-decision-inventory.json
│   ├── style-inventory.json
│   └── endpoint-inventory.json
└── api-explorer/
    ├── index.html
    └── data.json
```

Exact names may change if there is a strong repository reason, but keep the topics separate.

`00-audit-index.md` must link all outputs and record:

- audited branch,
- audited commit SHA,
- audit date,
- measurement limitations,
- final audit status.

---

# 6. Current repository orientation

The project is a single monorepo.

Primary roots:

```text
Backend/
Frontend/quran-dashboard-ui/
```

Backend solution:

```text
Backend/QuranDashboard.sln
```

Current Backend production areas include:

```text
Backend/api/
Backend/application/
Backend/domain/
Backend/infrastructure/
Backend/shared/
Backend/tools/
```

Main API project:

```text
Backend/api/QuranDashboard.Api/QuranDashboard.Api.csproj
```

Current target framework:

```text
net10.0
```

Current frontend toolchain includes at least:

```text
Angular 20.3.x
TypeScript 5.9.x
Vitest 3.2.6
Playwright 1.62.0
Tailwind CSS 3.4.19
ng-openapi-gen 1.0.5
Redocly CLI 2.39.0
Redoc 2.5.3
```

Do not infer architecture quality from these versions.

---

# 7. Confirmed instruction duplication that must be investigated

This is a current repository fact, not merely a suspicion.

## Root

Current files:

```text
AGENTS.md
CLAUDE.md
```

At the inspected `dev` baseline they shared the same blob SHA and size:

```text
SHA:  ad73795f052b95b5214c43e6ec32e1c2bd5f9b39
size: 14,915 bytes
```

They were byte-identical.

`AGENTS.md` itself says that the root `CLAUDE.md` contains general workspace instructions and routes Backend/Frontend work into Claude instruction files.

Therefore the current repository itself contributes to non-Claude agents reading Claude-oriented instruction context.

## Backend

Current files:

```text
Backend/AGENTS.md
Backend/CLAUDE.md
```

At the inspected baseline:

```text
SHA:  8a2bf334be04e1aec4182f4bb3848df06029d005
size: 7,335 bytes
```

They were also byte-identical.

`Backend/AGENTS.md` refers back to root `CLAUDE.md` for canonical rules such as the comment policy and planning-artifact lifecycle.

## Frontend

Current files:

```text
Frontend/quran-dashboard-ui/AGENTS.md
Frontend/quran-dashboard-ui/CLAUDE.md
```

They are near-duplicates rather than identical.

Observed sizes:

```text
AGENTS.md  4,016 bytes
CLAUDE.md  4,025 bytes
```

The frontend AGENTS file also points readers to root `CLAUDE.md` for canonical rules.

## Audit requirement

Do not waste the audit merely proving that this duplication exists.

Determine:

- why it was created,
- what depends on it,
- which rules are genuinely agent-specific,
- which rules are neutral project law,
- whether Claude should load only CLAUDE entrypoints,
- whether Codex should load only AGENTS entrypoints,
- whether shared neutral policies should be routed separately on demand,
- what the minimal safe instruction chain should be,
- how much context/token/file-reading cost can be removed.

The desired future direction to evaluate is:

```text
Claude → CLAUDE.md
Codex  → AGENTS.md
```

with shared project truth referenced narrowly rather than duplicated wholesale.

Do not implement that change during this audit.

---

# 8. Current long-lived context hotspots

The following current files are large enough that the audit must determine whether normal tasks are reading too much context.

Observed sizes at orientation time:

```text
AGENTS.md                                              14,915 bytes
CLAUDE.md                                              14,915 bytes
CODING_PRINCIPLES.md                                   5,190 bytes
PRODUCT.md                                              6,577 bytes
DESIGN.md                                              18,868 bytes
TESTING_STRATEGY.md                                    33,427 bytes
SKILLS_AND_ARCHITECTURE_GUIDE.md                       42,729 bytes
docs/TESTING_DEBT.md                                   41,767 bytes

Backend/AGENTS.md                                       7,335 bytes
Backend/CLAUDE.md                                       7,335 bytes
Backend/README.md                                       6,742 bytes
Backend/scripts/README.md                              26,229 bytes

Backend/.architecture/API_GUIDELINES.md                13,462 bytes
Backend/.architecture/BACKEND_STRUCTURE.md             11,903 bytes
Backend/.architecture/CLEAN_ARCHITECTURE.md             9,953 bytes
Backend/.architecture/LOGGING_GUIDELINES.md             4,243 bytes

Frontend/quran-dashboard-ui/AGENTS.md                   4,016 bytes
Frontend/quran-dashboard-ui/CLAUDE.md                   4,025 bytes
Frontend/quran-dashboard-ui/README.md                   7,541 bytes
Frontend/quran-dashboard-ui/.architecture/
  API_INTEGRATION_GUIDELINES.md                        11,920 bytes
  FRONTEND_STRUCTURE.md                                18,929 bytes
  UI_STYLE_SYSTEM.md                                  103,970 bytes

Frontend/quran-dashboard-ui/src/styles/README.md        9,367 bytes
```

Large does not automatically mean bad.

The audit must model real task-specific reading paths and duplication.

---

# 9. Audit A — Test Suite Rationalization

## Goal

Reduce recurring test runtime and maintenance while preserving high-value protection.

The target is not:

```text
fewer tests at any cost
```

The target is:

```text
fewer, stronger, faster, risk-aligned tests
```

## Inventory Backend tests

Measure:

- test projects,
- test files,
- test cases,
- test LOC,
- database-backed tests,
- Testcontainers usage,
- authorization/security tests,
- Access tests,
- API read tests,
- API mutation tests,
- migration tests,
- smoke tests,
- contract tests,
- importer/pipeline tests,
- canonical Quran-data tests,
- source/hash/manifest tests,
- process/CLI tests,
- duplicated setup/bootstrap patterns.

## Inventory Frontend tests

Measure:

- `.spec.ts` files,
- test cases,
- spec LOC,
- component tests,
- facade/store/state tests,
- API boundary tests,
- route/guard/URL-state tests,
- authorization tests,
- markup/selector-heavy tests,
- accessibility tests,
- repeated variants,
- tests that pin implementation details.

## Inventory E2E

Measure:

- Playwright tests,
- journeys covered,
- runtime,
- overlap with Vitest,
- flakiness evidence if available.

## Current Backend runner

Canonical runner:

```text
Backend/scripts/test-backend
```

Current lanes include:

```text
fast
access
access-db
migration
process
smoke
tier-b
canonical-data
feature
pipeline
pre-pr
```

Focused selectors include:

```text
feature FEATURE_KEY
feature --class FULL_CLASS_NAME
feature --test FULL_METHOD_NAME
```

Catalog:

```text
Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv
```

The runner contains explicit PostgreSQL/container serialization logic.

Respect it.

Do not create invalid runtime measurements by bypassing repository DB-safety constraints.

## Current Frontend scripts

Current package scripts include:

```text
test:fast

test:feature:access-admin
test:feature:abwab
test:feature:auth
test:feature:dashboard
test:feature:mushaf
test:feature:words

test:authorization
test:composition
test:shared
test:full
test:gates

typecheck
build:verify
test:pre-pr

e2e
e2e:typecheck
```

`npm test` intentionally caps Vitest forks:

```text
VITEST_MIN_FORKS=1
VITEST_MAX_FORKS=2
```

Respect that cap.

## Runtime measurement

Measure useful lanes and identify:

- slowest suites,
- slowest test classes/specs,
- repeated host startup,
- repeated DB/bootstrap cost,
- repeated importer/canonical setup,
- repeated Angular TestBed/jsdom setup,
- redundant broad gates,
- cost disproportionate to risk.

## Classification

Classify meaningful test groups/files:

- `KEEP`
- `MERGE`
- `DELETE_CANDIDATE`
- `REWRITE`
- `MOVE_TO_E2E`
- `MOVE_FROM_E2E`
- `RUN_LESS_OFTEN`
- `NEEDS_MEASUREMENT`

For every deletion candidate, name replacement coverage.

## High-protection areas

Preserve strong coverage for:

- authentication/authorization,
- Owner rules,
- direct permissions,
- account status,
- security boundaries,
- transactions,
- optimistic concurrency,
- audit,
- rollback/safety-point behavior,
- critical DB invariants,
- Quran text/data integrity,
- source provenance,
- import validation,
- canonical source safety,
- API writes,
- important frontend state/guards/URL restoration,
- critical user journeys.

## Likely lighter areas

Evaluate lighter coverage for:

- ordinary GET endpoints,
- trivial mappings,
- simple presentational components,
- repeated markup assertions,
- static text,
- framework guarantees,
- closed constants/getters.

## Importer tests

Separate:

- current operational Quran-data safety gates,
- morphology/i3rab pipelines,
- tafsir/translation/navigation pipelines,
- deterministic seed/catalog checks,
- obsolete/historical pipelines,
- repeated assertions of the same invariant.

Do not delete source-safety tests merely because they are expensive.

## GET endpoint tests

For ordinary read APIs, identify the minimum meaningful coverage:

- success,
- important filters/search/paging,
- invalid input,
- not-found where relevant,
- auth boundary where relevant,
- critical projection semantics.

Flag unnecessary combinatorial permutations.

## Required report

Produce:

- current runtime profile,
- current test LOC,
- highest-value tests,
- lowest-value tests,
- redundant clusters,
- expensive setup patterns,
- future lane strategy,
- candidate runtime reduction,
- candidate LOC reduction,
- risks.

No test deletion during the audit.

---

# 10. Audit B — Agent Context & Instruction Loading

## Goal

Determine what Claude, Codex, and other active coding environments are required to read before useful code work begins.

The key metric:

> How many instruction/context sources are consumed before the first relevant production-code read?

## Inventory

Inspect:

- root `AGENTS.md`,
- root `CLAUDE.md`,
- Backend AGENTS/CLAUDE,
- Frontend AGENTS/CLAUDE,
- nested instruction files if any,
- `.architecture/**`,
- `CODING_PRINCIPLES.md`,
- `TESTING_STRATEGY.md`,
- `PRODUCT.md`,
- `DESIGN.md`,
- `SKILLS_AND_ARCHITECTURE_GUIDE.md`,
- README reading rules,
- Skill reading rules,
- Spec Kit interaction,
- Cursor rules,
- prompt/orchestrator templates if stored,
- scripts/tooling that inject instructions.

## Cursor evidence

Current tracked file:

```text
.cursor/rules/always-read-agents.mdc
```

is `alwaysApply: true`.

It already conceptually says:

```text
use AGENTS
do not rely on CLAUDE unless explicitly asked
```

Treat this as useful historical/current evidence, not automatic future design.

Classify whether Cursor is still active workflow, stale support, or historical.

## Representative task traces

Model at least:

1. tiny Backend bug fix,
2. tiny Frontend UI fix,
3. new Backend read endpoint,
4. authorization change,
5. Abwab change,
6. approved-plan phase implementation,
7. engineering review,
8. performance review.

For each trace report:

- entry instruction files,
- additional mandatory docs,
- nearest README(s),
- architecture docs,
- Skill references,
- plan/spec files,
- total files,
- approximate text/LOC/token burden,
- duplicated rules,
- reads that could be on-demand,
- reads that are genuinely necessary.

## Target model to evaluate

A healthy ordinary task may look like:

```text
agent-specific entrypoint
→ nearest concise feature truth only if relevant
→ current plan only if plan-backed
→ relevant code
→ specialist references only when triggered
```

rather than:

```text
AGENTS
+ CLAUDE
+ root law
+ project law
+ multiple architecture docs
+ parent README
+ child README
+ Skill body
+ Skill reference pack
+ product/design docs
+ plan/spec/contracts
+ code
```

Recommend a measurable future context budget.

Do not modify instructions during the audit.

---

# 11. Audit C — Project-authored custom Skills

## Scope

Canonical custom Skill root:

```text
.claude/skills/
```

Current non-Spec-Kit project-authored top-level custom Skills are:

```text
backend-global-usings-cleanup
backend-structure-review
commit-workflow
dependency-audit
deploy-smoke
engineering-review
performance-angular-review
performance-backend-review
pr-context-prep
test-guard
```

These are in scope.

## Out of scope for redesign

Do not redesign:

```text
speckit-*
```

or external/plugin Skills.

Spec Kit may be inspected only enough to understand context/routing interactions.

## `.agents/skills`

Treat:

```text
.agents/skills/
```

as adapter/pointer space.

A current sample:

```text
.agents/skills/engineering-review/SKILL.md
```

explicitly points to:

```text
.claude/skills/engineering-review/SKILL.md
```

as canonical.

Verify the pointer-only invariant across the in-scope Skills.

Report any `.agents` adapter that duplicates substantial logic.

## For each custom Skill answer

1. Purpose.
2. Trigger.
3. Canonical body LOC.
4. Reference-pack LOC.
5. Files it requires automatically.
6. Files it requests conditionally.
7. Rules duplicated from AGENTS/CLAUDE/architecture docs.
8. Rules duplicated from another Skill.
9. References that can become on-demand.
10. Trigger ambiguity/overbreadth.
11. Whether it remains worth having.
12. Estimated context saving from simplification.

Classify:

- `KEEP`
- `SIMPLIFY`
- `MERGE`
- `DELETE_CANDIDATE`
- `REFERENCE_ON_DEMAND`
- `TRIGGER_NARROWING_NEEDED`

Do not edit Skills.

---

# 12. Engineering-review as a special case

Current canonical file:

```text
.claude/skills/engineering-review/SKILL.md
```

A thorough invocation may currently route into:

```text
CODING_PRINCIPLES.md
TESTING_STRATEGY.md
clean-code reference pack
test-guard + its references
Backend architecture docs
Frontend architecture docs
PRODUCT.md
DESIGN.md
Spec Kit artifacts when applicable
Quran data safety references
```

The Skill also embeds significant review guidance in its body.

Do not weaken engineering review blindly.

Determine:

- what must remain embedded,
- what should route on demand,
- what is already duplicated in project law,
- whether a scoped review can avoid broad documents,
- whether Test Guard delegation is efficient,
- whether review Skills are contributing materially to context bloat.

---

# 13. Audit D — Memory & Retrieved Context

## Goal

Understand persistent/retrieved project context and distinguish it from repository documentation.

Keep three categories separate:

### Repository instructions

Examples:

```text
AGENTS.md
CLAUDE.md
README.md
.architecture/*.md
Skills
PRODUCT.md
DESIGN.md
TESTING_STRATEGY.md
```

### Tool/config context

Examples:

```text
Claude settings
Codex/agent adapters
Cursor rules
Spec Kit wrappers
memory integration configuration if discoverable
```

### Model/persistent memory

Only what the model/tool environment can actually expose.

Do not invent access to hidden/private model memory.

## Repository/config audit

Search for:

- memory configuration,
- Mem0 or equivalent,
- automatic memory search,
- automatic save behavior,
- project IDs/scopes,
- retrieval rules,
- duplicated durable decisions,
- stale feature/status context,
- configuration that injects memory on every task.

No repo search result was found during orientation for literal:

```text
mem0
auto_search
```

That does not prove no external memory exists.

## `.claude` orientation

Current top-level `.claude/` includes:

```text
skills/
settings.local.json.bak
```

The tracked backup contains older command-permission examples.

Audit whether that backup is:

- intentional reference,
- stale noise,
- misleading,
- harmless archive.

Do not delete it during the audit.

## Required future handoff

The audit must prepare two later prompts:

### Claude memory/context review

Ask Claude to report only context/memory it can actually access and classify:

- `KEEP`
- `MERGE`
- `DELETE`

with special focus on:

- old feature status,
- branch status,
- test counts,
- completed-review chatter,
- duplicated product decisions,
- facts better stored in repo docs.

### Sol independent audit + memory review

Ask Sol to independently review the entire audit pack and separately report only memory/context it can actually access.

No chain-of-thought request.

No invented private memory.

---

# 14. Audit E — README / Markdown Decision Inventory

## Goal

Find every active long-lived decision and determine whether the documentation system helps agents or makes tasks heavier.

## Current documentation policy

Root instructions currently treat as long-lived truth:

```text
every README.md
root/per-project law files
all .architecture/**
docs/contracts/**
docs/TESTING_DEBT.md
.claude/**
.agents/**
.specify/**
code/tests/config
```

They also require agents to read the nearest relevant README before changing an area.

This may be useful.

It may also create a large transitive reading chain.

## Inventory

Inspect active:

- root docs,
- Backend READMEs,
- Frontend READMEs,
- feature READMEs,
- test READMEs,
- style README,
- scripts README,
- `.architecture/**`,
- contracts index,
- product/design docs,
- testing strategy,
- coding principles,
- Skills guide,
- testing debt,
- any surviving reports/historical files that still influence decisions.

Distinguish:

- live policy,
- live product truth,
- navigation/index,
- historical record,
- superseded sections,
- feature artifacts that should no longer be live.

## Decision extraction

Create a machine-readable decision inventory.

For each decision record:

- source file,
- section,
- summary,
- scope,
- intended consumer,
- duplicate sources,
- conflicts,
- stale/superseded evidence,
- whether code/tests prove it,
- recommended canonical owner.

Classify:

- `KEEP`
- `SHORTEN`
- `MERGE`
- `DELETE_CANDIDATE`
- `HISTORICAL_ONLY`
- `MOVE_TO_CANONICAL_SOURCE`
- `ON_DEMAND_ONLY`

## Nearest README question

Answer specifically:

> Is “read nearest README before touching an area” reducing discovery cost, or increasing total context cost?

Measure:

- number of READMEs,
- average/median size,
- duplication with parent instructions,
- duplication with architecture docs,
- typical task read chain,
- whether each README contains unique local invariants or generic architecture prose.

Evaluate a future model where READMEs exist only at meaningful bounded-context/feature boundaries and contain:

- what the area does,
- unique invariants,
- important contracts,
- where to start.

Not generic Clean Architecture repetition.

---

# 15. Audit F — Frontend Styling Strategy

## Goal

Determine how to reduce CSS/SCSS volume and styling duplication while keeping a coherent design system.

The intended direction to evaluate is:

> **Tailwind should become the dominant day-to-day styling mechanism**, supported by a small global design/token layer and truly reusable shared visual blocks.

This is a direction to evaluate with evidence, not an instruction to rewrite everything.

## Current confirmed baseline

Tailwind is installed:

```text
tailwindcss ^3.4.19
```

Config:

```text
Frontend/quran-dashboard-ui/tailwind.config.js
```

Current config is minimal:

```text
content: ['./src/**/*.{html,ts}']
theme.extend: {}
plugins: []
```

Global entry:

```text
Frontend/quran-dashboard-ui/src/styles.scss
```

currently imports substantial SCSS modules before Tailwind:

```text
styles/tokens
styles/themes
styles/typography
styles/layout
styles/components
styles/words-explorer-layout
styles/words-explainer
styles/explorer-tables
styles/explorer-detail-lists
styles/forms
styles/utilities
```

then:

```scss
@tailwind base;
@tailwind components;
@tailwind utilities;
```

## Current global SCSS examples

Observed file sizes include:

```text
_components.scss               16,630 bytes
_explorer-detail-lists.scss     8,028 bytes
_explorer-tables.scss           7,428 bytes
_tokens.scss                    6,089 bytes
_words-explainer.scss           3,443 bytes
_typography.scss                3,129 bytes
_themes.scss                    2,891 bytes
_forms.scss                     2,389 bytes
_layout.scss                    1,718 bytes
_utilities.scss                   810 bytes
```

Measure the whole styling tree, not only these files.

## Current style guide is itself a major audit target

Current file:

```text
Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md
```

Observed size:

```text
103,970 bytes
```

It currently says, among other things:

```text
Repeated design patterns should become qd- classes.
Tailwind supports the design system; it does not replace it.
```

It also contains historical/superseded styling sections.

Determine whether this document is:

- a useful live contract,
- partly a historical narrative,
- duplicated with DESIGN.md,
- too large for routine reads,
- directly contributing to custom-CSS growth.

## Current component-SCSS rule

Current:

```text
Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md
```

explicitly says components use separate `.html` and `.scss` files by default.

It defines component SCSS thresholds:

```text
Ideal: under 150 lines
Soft:  200 lines
Hard:  300 lines
```

The canonical engineering-review Skill repeats the separate `.html` + `.scss` expectation.

This may be actively encouraging creation of stylesheets even when no meaningful component-specific CSS is required.

Measure before recommending change.

## Required style measurements

Report:

- total frontend CSS/SCSS LOC,
- global SCSS LOC,
- component SCSS LOC,
- number of component `.scss` files,
- empty/nearly-empty component SCSS files,
- Tailwind utility usage frequency,
- `qd-*` usage frequency,
- token usage,
- hardcoded values,
- repeated declarations,
- repeated layout/spacing blocks,
- repeated card/button/input/table/modal patterns,
- feature-specific styles promoted into global files,
- duplicated responsive rules.

## Future ownership model to evaluate

### Tailwind

Prefer for:

- layout,
- flex/grid,
- spacing,
- sizing,
- simple responsive behavior,
- simple state utilities,
- common typography utilities.

### Small global design layer

Keep where it provides real value:

- theme tokens,
- CSS variables,
- Quran typography,
- font faces,
- global theme behavior,
- accessibility/browser-level rules,
- carefully chosen semantic primitives.

### Shared reusable visual blocks/components

Use where the repeated pattern is semantic/structural and should remain consistent across features.

### Component SCSS

Keep only when genuinely needed:

- complex component-specific visuals,
- pseudo-elements,
- special selectors,
- Quran glyph/text rendering specifics,
- unique interaction styling,
- cases where Tailwind would become less readable than focused CSS.

Do not mechanically eliminate all SCSS.

## Audit every rule that would conflict with a Tailwind-dominant future

Likely sources include:

```text
Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md
Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md
Frontend/quran-dashboard-ui/AGENTS.md
Frontend/quran-dashboard-ui/CLAUDE.md
.claude/skills/engineering-review/SKILL.md
.cursor/rules/always-read-agents.mdc
```

Search for additional duplicates.

---

# 16. Audit G — Architecture & Code Size

## Goal

Answer whether the project is genuinely too large/over-engineered or merely appears large because support/generated/test/doc code dominates.

## LOC categories

Report separately.

### Handwritten Backend production

At minimum:

```text
api
application
application abstractions
domain
infrastructure
shared
relevant handwritten tools
```

### Handwritten Frontend production

At minimum:

```text
TypeScript
HTML
SCSS
core
shared
features
```

### Support/non-product categories

Separate:

```text
Backend tests
Frontend specs
E2E
generated OpenAPI client
Swagger/OpenAPI JSON
EF migrations
EF snapshots
scripts
Skills
READMEs/docs
Spec Kit
historical reports
build artifacts
package lockfiles
```

Do not mix generated/support code with handwritten product logic.

## Required numbers

Report:

- total repository LOC,
- handwritten production LOC,
- Backend handwritten LOC,
- Frontend handwritten LOC,
- test LOC,
- generated LOC,
- migration/snapshot LOC,
- scripts/tooling LOC,
- Skills LOC,
- documentation LOC.

Also report percentages.

## Largest areas

List:

- largest directories,
- largest handwritten files,
- largest components,
- largest services,
- largest handlers,
- largest readers/repositories,
- largest facades/stores,
- largest test groups.

## Architecture simplification candidates

Search for:

- one-implementation interfaces with no meaningful boundary value,
- handlers that only pass through,
- wrappers that add no policy,
- repeated DTO/model layers,
- duplicate mapping code,
- reader abstractions mirroring EF queries,
- caches around trivial reads,
- unnecessary service/manager/facade layers,
- deep folder chains,
- many tiny files implementing one behavior,
- abstractions with one caller,
- frontend component splits that multiply files without reducing cognitive load,
- repeated page/facade/data-access plumbing,
- custom infrastructure already solved by framework primitives.

Do not assume these are defects.

For each candidate report:

- current value,
- callers/implementations,
- safety boundary?,
- simplification option,
- risk,
- estimated files/LOC removed,
- expected recurring feature benefit.

## Boundaries to preserve

Do not flatten away real safety boundaries around:

- authentication,
- authorization,
- Owner/direct permissions,
- transactions,
- optimistic concurrency,
- audit,
- DB invariants,
- Quran data safety,
- importer/source validation,
- external identity boundaries,
- security-sensitive state,
- API contract integrity.

Target:

> the fewest layers that preserve the same safety and clarity.

---

# 17. Audit H — API Surface & Payload

## Goal

Inventory the entire API and identify:

- unnecessary response fields,
- over-fetching,
- large payloads,
- expensive joins/subqueries/counts,
- chatty pages,
- duplicate endpoints,
- detail data loaded too early,
- endpoints with no known consumer,
- payload/query work that can safely shrink.

## Existing API metadata pipeline

Current Swagger:

```text
Frontend/quran-dashboard-ui/openapi/swagger.json
```

Observed size:

```text
309,064 bytes
```

Current Backend helpers:

```text
Backend/scripts/export-swagger
Backend/scripts/check-api-contract
```

Current frontend generator:

```text
Frontend/quran-dashboard-ui/ng-openapi-gen.json
```

with:

```text
input:  openapi/swagger.json
output: src/app/core/api/generated
removeStaleFiles: true
ignoreUnusedModels: false
```

Use Swagger as a major inventory source, then verify against code.

Do not rediscover route/schema metadata manually when current OpenAPI already provides it.

Do not trust Swagger alone for real query cost or consumer use.

## Existing HTML API docs command

Current frontend script:

```text
npm run docs:api
```

uses Redocly/Redoc to build:

```text
docs/api-reference/index.html
```

At orientation time `docs/api-reference/` was not committed/present in current root `docs/`.

Evaluate whether the audit explorer should:

- extend/reuse this Redoc path,
- remain a separate audit-only explorer,
- or combine Swagger/Redoc output with audit enrichment.

A plain Redoc document is not enough because it does not know:

- actual frontend consumers,
- unused response fields,
- DB/query paths,
- query cost,
- payload classification,
- lazy-load opportunities.

## Endpoint inventory

For every endpoint capture:

- method,
- route,
- feature/domain,
- auth requirement,
- required permission/Owner rule,
- controller/action,
- handler/use case,
- query/reader/repository path,
- request body,
- query parameters,
- response DTO,
- pagination,
- cache behavior,
- main DB tables/joins where determinable,
- known frontend consumers,
- known tools/scripts/admin consumers,
- tests.

## Response-field inventory

For each field capture:

- name,
- meaning,
- source,
- frontend consumer(s),
- other known consumer(s),
- unknown consumer state,
- apparently unused?,
- expensive to compute/join?,
- duplicated elsewhere?,
- safe-removal confidence.

Classify field:

- `USED`
- `UNUSED_CANDIDATE`
- `EXPENSIVE_USED`
- `EXPENSIVE_UNUSED_CANDIDATE`
- `CONTRACT_REQUIRED`
- `UNKNOWN_CONSUMER`

## Endpoint classification

Classify:

- `KEEP`
- `SHRINK_RESPONSE`
- `MERGE_CANDIDATE`
- `SPLIT_OR_LAZY_LOAD`
- `DELETE_OR_DEPRECATE_CANDIDATE`
- `INTERNAL_ADMIN`
- `NEEDS_MEASUREMENT`

## Query-cost caution

Do not say:

```text
remove JSON field = fewer DB queries
```

unless the actual query path proves it.

Distinguish:

1. DTO-only reduction.
2. Smaller EF projection.
3. Removed join/subquery/count.
4. Reduced query count.
5. Serialization/network-only improvement.

Where needed inspect SQL/projection safely.

## Chatty screen analysis

Identify screens that perform many reads to render one user-visible state.

Determine whether calls are:

- correctly lazy and independent,
- duplicated,
- sequential without need,
- repeating the same count/join,
- candidates for summary endpoints,
- candidates for delayed detail loading.

Do not merge endpoints merely to reduce endpoint count.

## Payload measurement

For important/hot endpoints estimate or measure:

- typical payload bytes,
- high-end payload size,
- row counts,
- nested arrays,
- repeated large text,
- HTML/tafsir/translation fields,
- duplicated metadata.

Prioritize user-facing hot paths.

---

# 18. Static HTML API Explorer

Create:

```text
docs/project-simplification-audit/api-explorer/index.html
```

and optionally:

```text
docs/project-simplification-audit/api-explorer/data.json
```

It must open directly as a static local file.

No backend/server required.

## Use existing Swagger data

Seed route/schema metadata from:

```text
Frontend/quran-dashboard-ui/openapi/swagger.json
```

Then enrich from real Backend and Frontend code.

## Search/filter

Support useful filtering by:

- route,
- feature,
- HTTP method,
- auth level,
- classification,
- suspected over-fetching,
- no known consumer,
- heavy payload,
- lazy-load candidate.

## Endpoint view

Show:

- method + route,
- purpose,
- auth requirement,
- params/body,
- small synthetic request example,
- small synthetic response example,
- response fields + meaning,
- known frontend consumer,
- other known consumer,
- apparently unused fields,
- DB/query path,
- pagination,
- cache,
- payload notes,
- classification,
- recommendation,
- repository evidence.

Do not expose:

- secrets,
- tokens,
- real personal data.

Use synthetic examples.

This explorer should become an easy audit reference for future API cleanup.

---

# 19. Audit I — Build / Test / Review Workflow Cost

## Goal

Find redundant or badly timed verification cost.

Inventory:

- Backend builds,
- Backend focused test lanes,
- Backend pre-PR,
- Frontend focused lanes,
- Frontend `test:pre-pr`,
- production builds,
- typecheck,
- API contract checks,
- permission/catalog checks,
- smoke gates,
- E2E,
- engineering review,
- performance review,
- commit workflow,
- deployment smoke,
- Spec Kit implementation/review interactions.

Map when each is currently expected to run:

- per edit,
- per phase,
- per feature,
- engineering review,
- pre-PR,
- release,
- deployment.

Identify:

- builds repeated without code changes,
- broad suites repeated after focused suites,
- canonical/import tests triggered by unrelated work,
- Backend/Frontend cross-testing with no relevant contract change,
- repeated API generation/contract work,
- repeated review context reads,
- expensive gate combinations that can be milestone-only.

Recommend a future risk-based trigger matrix.

Preserve high-risk safety gates.

---

# 20. Current Backend scripts worth inventorying

Current scripts include at least:

```text
Backend/scripts/test-backend
Backend/scripts/check-api-contract
Backend/scripts/check-pending-model
Backend/scripts/export-swagger
Backend/scripts/cleanup-test-runtime
Backend/scripts/create-smoke-dump
Backend/scripts/access-admin
Backend/scripts/add-mig
Backend/scripts/drop-db
Backend/scripts/qd-api
Backend/scripts/qd-build
```

Inventory the full scripts directory.

Some scripts are destructive/stateful.

Do not run destructive commands during the audit merely to understand them.

---

# 21. Required cross-cutting investigation: why future features are expensive

The final synthesis must estimate where a typical feature pays cost today.

Build a representative “feature cost stack”:

```text
instruction loading
+ README/architecture reading
+ Skill loading
+ planning/spec reading
+ implementation boilerplate
+ frontend style boilerplate
+ DTO/API/client boilerplate
+ test authoring
+ focused tests
+ broad tests
+ engineering review
+ review reference loading
+ commit workflow
```

Estimate which layers are:

- necessary,
- duplicated,
- too frequent,
- too broad,
- reducible.

The user goal is not only repository cleanup.

It is:

> make every future feature cheaper and faster to build safely.

---

# 22. Current frontend rule interactions that must be traced

A future styling simplification will fail if old rules remain active in other files.

Audit all duplicated instructions related to:

```text
separate component SCSS by default
qd-* classes
Tailwind role
file-size thresholds
component splitting
shared visual patterns
global style ownership
```

If one rule changes later, list every document/Skill that must change with it.

The audit should explicitly identify “policy loops” where:

```text
architecture doc says X
→ AGENTS repeats X
→ CLAUDE repeats X
→ engineering-review enforces X
→ agent creates code matching X
→ README records X
```

These loops are high-value cleanup targets when X is no longer a good rule.

---

# 23. Current documentation lifecycle must be evaluated, not assumed

The repository currently removes feature planning/report artifacts before merge and treats code/tests/nearest README as steady-state truth.

The current root `docs/` baseline contains:

```text
README.md
TESTING_DEBT.md
contracts/
```

This is evidence that feature cleanup is active.

Evaluate whether this lifecycle is working well.

Questions:

- Does deleting plans reduce clutter without losing important decisions?
- Are too many decisions being pushed into READMEs?
- Are READMEs becoming oversized historical narratives?
- Should some long-lived decisions instead live in a small number of canonical policy files?
- Is `docs/contracts/` genuinely a thin index or another layer agents must traverse?
- Does `TESTING_DEBT.md` remain useful or has it become too large/noisy?

---

# 24. Generated code must be excluded from architecture blame

Current generated frontend API path:

```text
Frontend/quran-dashboard-ui/src/app/core/api/generated
```

When reporting project size:

- separate generated client/models,
- separate Swagger JSON,
- separate EF migrations/snapshots,
- separate package lockfiles.

Do not claim that generated LOC proves over-engineering.

When recommending API shrinkage, trace:

```text
EF/SQL projection
→ Backend DTO
→ Swagger/OpenAPI
→ generated client/model
→ handwritten frontend mapping/state
→ template use
```

This makes it possible to estimate both runtime and code-size benefit accurately.

---

# 25. Specific mandatory questions

The reports must answer these directly.

## Instruction/context

1. Why are root AGENTS and CLAUDE duplicated?
2. Why are Backend AGENTS and CLAUDE duplicated?
3. Why does AGENTS route non-Claude agents into CLAUDE files?
4. What unique information actually needs to be agent-specific?
5. What neutral project law should be shared?
6. What is the smallest safe instruction chain for Claude?
7. What is the smallest safe instruction chain for Codex?
8. How much context can normal tasks save?

## READMEs/docs

9. How many READMEs exist?
10. What is their total/median/average size?
11. Which contain unique invariants?
12. Which mostly repeat architecture/instructions?
13. Which contain historical/superseded material?
14. Is nearest-README reading beneficial overall?
15. Which docs should become on-demand only?

## Skills

16. What are the sizes of the 10 custom Skills?
17. What references do they pull?
18. Which rules duplicate project docs?
19. Which references should become on-demand?
20. Are `.agents` adapters truly pointer-only?
21. Which Skills cause the most context overhead?

## Testing

22. Total test LOC?
23. Total test count?
24. Backend runtime by lane?
25. Frontend runtime by lane?
26. Slowest test groups?
27. Highest-value safety tests?
28. Redundant/low-value clusters?
29. Which GET tests can be consolidated?
30. Which importer/canonical tests are essential?
31. Which checks need full-suite cadence vs focused cadence?

## Frontend styling

32. Total SCSS/CSS LOC?
33. Global vs component SCSS?
34. Number of component SCSS files?
35. Empty/tiny SCSS files?
36. Tailwind usage share?
37. `qd-*` usage share?
38. Repeated CSS that Tailwind can replace?
39. Shared semantic patterns that should remain reusable?
40. Is `UI_STYLE_SYSTEM.md` too large/historical?
41. Is separate `.scss` by default increasing unnecessary files?
42. What would a Tailwind-dominant future safely look like?

## API

43. Number of Swagger operations?
44. Backend endpoints missing/stale in Swagger?
45. Number of response fields with no known consumer?
46. Which unused fields have real DB/query cost?
47. Which are network/serialization-only overhead?
48. Which endpoints over-fetch?
49. Which screens are chatty?
50. Which detail fields should lazy-load?
51. Which endpoints appear unused/deprecation candidates?
52. How much payload/query/code reduction is plausible?

## Architecture/code size

53. Total repository LOC?
54. Handwritten production LOC?
55. Test LOC?
56. Generated LOC?
57. Migration/snapshot LOC?
58. Documentation/Skill LOC?
59. Why does the project feel large?
60. Which layers are genuine boundaries?
61. Which are pass-through?
62. Which frontend component splits reduce complexity?
63. Which merely multiply files?
64. What safe LOC/file reduction is realistically possible?

## Workflow cost

65. Which gates run too often?
66. Which builds/tests are duplicated?
67. Which review workflows reread excessive context?
68. What future risk-based gate matrix would reduce feature cost safely?

---

# 26. Cross-cutting priority report

After the detailed reports, create:

```text
11-cross-cutting-priorities.md
```

Do not turn it into one master implementation plan.

Group future remediation into independent workstreams such as:

```text
Test Rationalization
Agent Instruction Separation
Custom Skill Simplification
Memory Cleanup
README/Documentation Simplification
Frontend Styling Simplification
API Payload/Surface Cleanup
Architecture/LOC Simplification
Build/Gate Optimization
```

The actual grouping should follow evidence.

For each workstream report:

- expected recurring benefit,
- risk,
- prerequisites,
- approximate effort,
- confidence,
- recommended order,
- measurable success criteria.

Use a simple prioritization model if useful, for example:

```text
Impact × Frequency × Confidence ÷ Risk
```

No implementation plan yet.

---

# 27. Executive summary requirements

`01-executive-summary.md` must answer plainly:

1. Is the project genuinely over-engineered?
2. Where?
3. What percentage of repository size is handwritten production code?
4. What percentage is tests?
5. What percentage is generated/support?
6. What percentage is migrations/docs/tooling?
7. Why are agents reading excessive context?
8. Why is Codex reading Claude instructions?
9. Are READMEs helping or hurting?
10. Which custom Skills are most expensive in context?
11. Which test areas dominate runtime?
12. Is frontend styling unnecessarily duplicated?
13. How much component SCSS appears avoidable?
14. How many API fields/endpoints appear unnecessary or over-fetching?
15. Which architecture layers appear pass-through?
16. What must remain untouched because it protects safety?
17. Roughly how much recurring feature cost could be removed?
18. What are the highest-priority cleanup workstreams?

Use measured numbers wherever possible.

---

# 28. Post-audit independent review handoff

Create:

```text
12-post-audit-review-handoff.md
```

It must contain ready-to-use prompts.

## A. Sol independent review

The Sol prompt should instruct Sol to:

- read the entire audit pack,
- independently inspect load-bearing repository evidence,
- challenge Fable conclusions,
- identify unsafe simplification recommendations,
- review test rationalization,
- review instruction/context changes,
- review custom Skills,
- review README/document cleanup,
- review Tailwind/style recommendations,
- review API shrinkage/deprecation recommendations,
- review architecture simplification,
- review build/gate changes,
- produce a verdict per workstream,
- separately report only project memory/context Sol can actually access,
- identify stale/redundant context,
- never invent hidden/private memory access.

## B. Claude memory/context review

The Claude prompt should instruct Claude to:

- report durable project memory/context it can actually access,
- distinguish persistent memory from repository instructions,
- identify stale/duplicated/feature-status memories,
- classify `KEEP / MERGE / DELETE`,
- identify facts better stored in repository docs,
- not expose private chain-of-thought,
- not claim inaccessible memory.

---

# 29. What must not be simplified away casually

Any proposed simplification that touches these areas requires explicit risk analysis:

```text
authentication
authorization
Owner bypass
direct permissions
account status
audit
optimistic concurrency
transactions
database invariants
Quran text integrity
Quran source provenance
import validation
canonical source checks
migration safety
OpenAPI contract parity
security-sensitive errors
RTL/Quran typography correctness
```

The audit may still find accidental complexity around these systems.

It must not treat the protections themselves as waste without strong evidence.

---

# 30. Do not let current documentation dictate the conclusion

Existing Markdown is evidence of current policy, not proof that the policy remains correct.

Known examples that require evaluation:

- AGENTS routes non-Claude readers to CLAUDE.
- agent instruction files are duplicated.
- Tailwind is installed but the style guide says it only supports the custom style system.
- frontend structure says separate SCSS is the default.
- engineering-review enforces that structure.
- the UI style guide contains substantial historical/superseded material.
- multiple docs can describe the same rule.

Reconstruct why the rules exist.

Then evaluate whether their current recurring cost is justified.

---

# 31. Do not over-focus on Spec Kit

Spec Kit is present under:

```text
.claude/skills/speckit-*
.agents/skills/speckit-*
.specify/
```

It may be measured when it contributes to total context/workflow cost.

It is outside the scope of redesign.

The custom Skill cleanup scope is the project-authored non-Spec-Kit set listed in this brief.

---

# 32. Measurement gaps and honesty

If a fact cannot be measured safely, say:

```text
NEEDS_MEASUREMENT
```

Examples may include:

- real production DB query latency,
- external API consumers not represented in repo,
- actual model-internal memory,
- historical test flakiness without records,
- production payload distribution without telemetry.

Do not convert uncertainty into confident deletion advice.

---

# 33. Audit completion statuses

End `00-audit-index.md` with exactly one:

```text
AUDIT_COMPLETE_READY_FOR_REVIEW
```

or:

```text
AUDIT_COMPLETE_WITH_MEASUREMENT_GAPS
```

or:

```text
BLOCKED
```

Do not output:

```text
READY_FOR_IMPLEMENTATION
```

The next step is independent Sol/Claude review.

Only after that will remediation be split into small plans.

---

# 34. Success criteria

The audit is successful only if we can answer, with evidence:

- which tests to keep,
- which tests to merge/rewrite/delete later,
- why tests are slow,
- how to reduce test cadence,
- why agents load too much context,
- how Claude and Codex instruction entrypoints should differ,
- which project Skills are too heavy,
- which memory/context is stale,
- which READMEs/docs are useful,
- which docs are repetitive/historical,
- how frontend styling should be simplified,
- how much SCSS can plausibly disappear,
- whether Tailwind should become dominant,
- how much handwritten production code actually exists,
- which architecture layers are valuable,
- which layers are pass-through,
- what every API endpoint returns,
- which API fields are consumed,
- which fields/endpoints over-fetch,
- what query/payload savings are possible,
- where build/test/review time is being wasted,
- what cleanup workstreams should happen first,

**without requiring another repository-wide discovery pass before planning the cleanup work.**

---

# 35. Final instruction

Treat this file as the canonical audit contract.

Use the repository itself as the source of truth.

Verify load-bearing facts independently.

Produce excellent, evidence-heavy reports.

Do not implement fixes.

Do not create one giant cleanup plan.

The goal is to leave us with a trustworthy map of the project's real complexity and the safest, highest-value ways to reduce it for the long term.
