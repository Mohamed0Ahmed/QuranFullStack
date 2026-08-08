# 10 — Build / Test / Review Workflow Cost Audit (Brief §19 Audit I + §20 scripts)

Audited branch: `dev` · HEAD `72792ba9` · audit date 2026-08-08 · read-only.

Evidence base: `data/workflow-gate-inventory.json` (scripts, gates, cadence matrix, redundancy
flags), `data/runtime-measurements.json` (first real gate timings this project has ever had),
`data/test-inventory-backend.json` (lane composition), `data/instruction-inventory.json`
(review-context reading traces), `data/skill-inventory.json` (review-skill context closure).
All load-bearing claims below were independently re-verified against the repository by this
author; citations are `path:line`.

A deliberate project fact frames this report: **TESTING_STRATEGY.md forbids recording test
counts or durations as standing facts in prose** (TESTING_STRATEGY.md:17-24), precisely
because they drift and there is no CI to re-check them. Consequently no gate in this
repository has ever had a documented cost. The numbers in this report are dated, one-run
measurements attached to HEAD `72792ba9` on one machine (i7-6820HQ, 8 threads, 14 GB RAM,
solo load — `data/runtime-measurements.json` environment block). They are audit evidence, not
standing facts, and they must not be folded back into TESTING_STRATEGY.md or any README —
that would violate the repo's own anti-drift rule.

Measurement caveats that apply to every number below (`data/runtime-measurements.json`
caveats block): single run per lane, no variance data; each backend lane carries ~10–15 s of
fixed dotnet-test/VSTest startup plus per-run container provisioning, so lane walls are not
additive test cost; `build_cold_s` was measured with warm bin/obj and NuGet cache (a true
cold build is slower); tier-b overlaps most other lanes, so summing lane walls double-counts.

---

## 1. Gate inventory with measured cost

### 1.1 Backend builds

| Gate | Command | Measured wall | Tag |
| --- | --- | --- | --- |
| Solution build, warm-cold | `dotnet build QuranDashboard.sln` (first of session) | 34.6 s | CONFIRMED (one run; true cold slower) |
| Solution build, incremental | same, no changes | 6.7 s | CONFIRMED |
| Release build of Api (inside `export-swagger`) | `dotnet build -c Release -m:1` (Backend/scripts/export-swagger:15-20) | not measured | NEEDS_MEASUREMENT (separate Release obj; order-of-magnitude comparable to Debug) |

All gate-script build invocations (test-backend, check-pending-model, export-swagger) are
deliberately single-threaded (`-m:1`, `-p:BuildInParallel=false` —
Backend/scripts/test-backend:291-297, Backend/scripts/check-pending-model:34-39,
Backend/scripts/export-swagger:15-20); `qd-build`, the daily dev build, is parallel-default
(plain `dotnet build QuranDashboard.sln` — Backend/scripts/qd-build:10). The gate-script
serialization is a correctness/serialization choice, not an accident; it also means every
gate-scripted build pays a serial penalty on an 8-thread machine. Cost of the choice: NEEDS_MEASUREMENT (no parallel
baseline was taken; taking one would require changing scripts, out of scope).

### 1.2 Backend test lanes (`Backend/scripts/test-backend`, catalog of 268 classes)

Lane composition from `data/test-inventory-backend.json` (verified against
TESTING_STRATEGY.md §3 lane table, lines 77-97); walls from `data/runtime-measurements.json`.

| Lane | Classes | Tests passed | Wall | s/test | Documented trigger (TESTING_STRATEGY.md §3/§5) |
| --- | ---: | ---: | ---: | ---: | --- |
| `fast` | 69 | 559 | 7.5 s | 0.013 | per-edit: small logic iterations |
| `access` | 28 | 249 | 61.3 s | 0.246 | authorization slice completion + formal review |
| `access-db` | 19 | 193 | 43.0 s | 0.223 | Access persistence/schema change |
| `migration` | 1 | 9 | 21.9 s | 2.43 | migration/EF-model/backfill work |
| `process` | 1 | 10 | 21.4 s | 2.14 | AccessAdmin wrapper/operator boundary change |
| `smoke` | 15 | 256 | 72.6 s | 0.284 | REQUIRED for any route/contract/auth/middleware/binding change (§6) |
| `tier-b` | 130 | 1,372 | 65.9 s | 0.048 | milestone, engineering review, ordinary Backend pre-PR |
| `pipeline` | 113 | 583 | 64.1 s | 0.110 | importer/pipeline/shared-persistence change only |
| `canonical-data` | 10 | — | **not measured** | — | canonical source/manifest/hash/dump trigger, release acceptance |
| `pre-pr` (full, 2 shards) | 268 | — | **est. ~307 s (5–7 min)** | — | shared-infra prerequisite, release/full-regression, explicit formal-review trigger |

Tags: measured rows CONFIRMED (single run). `pre-pr` estimate LIKELY (basis: measured
superset lanes + one build + ~60 s allowance for the exclusive postgres:18
`SmokeDataReadTests` shard — `data/runtime-measurements.json` backend.not_measured).
`canonical-data` UNKNOWN — deliberately unestimated; it runs real DataImporter imports plus
the exclusive dump-restore shard, and no measured shard covers those classes.

Two structural observations from the measured profile:

- **Fixed overhead dominates the small lanes.** `migration` (9 tests, 21.9 s) and `process`
  (10 tests, 21.4 s) spend most of their wall on host startup + container provisioning
  (~10–15 s fixed per lane run). Their per-test cost (~2.1–2.4 s/test) is two orders of
  magnitude above `tier-b`'s (0.048 s/test). This is not an argument to delete them — both
  protect §29 areas (migration safety, operator boundary) — but it is an argument that
  *lane-run count*, not test count, is the backend cost driver. CONFIRMED.
- **`tier-b` is remarkably cheap for what it covers**: 1,372 tests in 65.9 s. The broad
  backend milestone gate is ~1 minute, not the multi-minute monster the cadence anxiety in
  the docs might suggest. The expensive backend gates are `smoke` (72.6 s for 256 tests —
  real HTTP pipeline against containers) and anything canonical. CONFIRMED.

### 1.3 Frontend gates (`Frontend/quran-dashboard-ui/package.json`)

| Gate | Measured wall | Detail | Tag |
| --- | ---: | --- | --- |
| `typecheck` (app + spec tsc) | 14.6 s | package.json:24-26 | CONFIRMED |
| `build:verify` (ng build) | 18.3 s | 3 warning-level budget overruns observed (bundle 598.75 kB vs 500 kB budget; 2 mushaf SCSS files over 4 kB) | CONFIRMED |
| `test:fast` | 72.5 s | 61 files, 810 tests (vitest-reported 46.8 s → ~26 s bootstrap) | CONFIRMED |
| `test:feature:words` (largest feature lane) | 114.3 s | 92 files, 1,379 tests | CONFIRMED |
| `test:full` | 232.2 s | 223 files, 2,964 tests (vitest-reported 207.5 s) | CONFIRMED |
| `check:permission-catalogue`, `check:audit-action-types` | not measured | plain node scripts, no Angular build (package.json:22-23) | LIKELY seconds-scale |
| `test:pre-pr` composite | **≥ 265 s (~4.5 min)** | = checks + typecheck (14.6) + build:verify (18.3) + test:full (232.2), in that order (package.json:28) | LIKELY (sum of measured legs; checks unmeasured) |
| `test:gates` | not measured | node script, not part of test:pre-pr (TESTING_STRATEGY.md:214-218) | LIKELY seconds-scale |
| `e2e` (2 sequential Playwright projects) | **not measured** | needs live DB + both servers; opt-in only, never a required gate (TESTING_STRATEGY.md:374) | NEEDS_MEASUREMENT |

The frontend cost profile is the inverse of the backend's: the broad gate (`test:full`,
232 s) costs 3.5× the backend broad gate (`tier-b`, 66 s), and even `test:fast` costs 72 s
because every Vitest run pays the Angular test-builder bundle compile (~26 s bootstrap
observed) under the deliberate two-fork cap. CONFIRMED.

### 1.4 Backend scripts (17 scripts + README — Brief §20)

Full inventory in `data/workflow-gate-inventory.json` backend_scripts; verified against
`ls Backend/scripts/` (17 scripts + README.md). Cost-relevant summary; none were executed
during the audit except as part of the measured lanes above.

| Script | Role | Cost class | Destructive? |
| --- | --- | --- | --- |
| `test-backend` | the single supported test runner (11 lanes) | measured above | starts/removes own labelled containers only |
| `check-api-contract` | contract-parity gate: export-swagger + `npm run generate:api` + `git diff --exit-code` | Release build + codegen, **not measured** — NEEDS_MEASUREMENT | rewrites committed spec/client in place |
| `export-swagger` | Release build + Swashbuckle CLI to committed swagger.json | Release build each run | tree-only |
| `check-pending-model` | EF model-vs-snapshot check; `--build` compiles the full solution single-threaded | build cost when `--build` | no |
| `create-smoke-dump` | regenerates canonical ~⅓ GB dump + manifest; **required in the same change as any migration** (TESTING_STRATEGY.md:186-188) | **not measured** — UNKNOWN | overwrites local artifact only |
| `cleanup-test-runtime` | scoped Docker cleanup, auto-invoked by test-backend EXIT trap | seconds | scoped force-rm of own run's resources |
| `qd-build` / `qd-api` / `qd-ui` | daily dev loop | build cost / dev servers | no |
| `add-mig`, `update-db`, `check-pending-model` | EF migration workflow | small | update-db mutates schema, **no `--yes` gate** (only doc guard — Backend/scripts/README.md:31-35) |
| `drop-db`, `reset-db`, `wipe-abwab` | destructive local DB resets, `--yes`-gated | n/a | YES (documented, gated) |
| `access-admin` | operator runbook tool (deployment cadence, not a per-feature gate) | n/a | write verbs mutate DB |
| `clean-local-build`, `_preflight-sandbox.sh` | sandbox-asset hygiene | n/a | build state only |

One genuine scripts-level duplication was inventoried and verified: ~3–4 KB of test-runner
behavioral description is maintained in both `Backend/scripts/README.md` (:290-336) and
TESTING_STRATEGY.md (:70-160), by explicit design — the README states it "deliberately does
not repeat the selection policy" (Backend/scripts/README.md:280-281) and the split is
mechanics-vs-policy. CONFIRMED, and mostly clean; the residual overlap is a documentation
concern for report 06, not a compute cost.

### 1.5 Review/workflow gates that cost context, not compute

These are gates in every practical sense — the docs schedule them at boundaries — but their
cost is agent reading, not CPU (fully treated in §5 below):

| Gate | When (per docs) | Cost per invocation | Tag |
| --- | --- | --- | --- |
| `engineering-review` skill | per feature, after implementation (CLAUDE.md "the formal post-implementation review skill"; deletion commit only after it passes) | always-reads floor ~68 KB (~17k tokens); backend-diff-with-tests trace ~206 KB (~52k tokens) mandatory; worst-case closure 359,926 B (~90k tokens) | CONFIRMED |
| `test-guard` skill | whenever a diff contains test files (mandatory sub-gate of review — engineering-review SKILL.md:75-85) | SKILL + 3 refs ≈ 30.4 KB (32.5 KB only via test-guard's own 4-ref chain incl. llm-app-testing.md) | CONFIRMED |
| `pr-context-prep` skill | pre-PR; gathers evidence, runs nothing | body ~KB-scale; requires lane evidence lines incl. smoke counts (SKILL.md:110) | CONFIRMED |
| `commit-workflow` skill | per commit | invites re-running "relevant checks... when feasible" (SKILL.md:117) | CONFIRMED |
| `deploy-smoke` skill | after migrations / before PR / cross-stack | repeats restore+build+boot | CONFIRMED trigger; cost NEEDS_MEASUREMENT |

### 1.6 Deployment gates

- Railway (backend): Dockerfile build + `/api/health` healthcheck, 120 s timeout, deploys
  from `main`. **Zero tests** (Backend/railway.json — verified; no test step exists).
- Vercel (frontend): builds from the committed spec/generated client ("no dotnet in" the
  Vercel build — Frontend/quran-dashboard-ui/README.md:45-46). **Zero tests.** No
  `vercel.json` in the repo; the Vercel configuration lives outside the tree (UNKNOWN
  contents).

CONFIRMED: nothing between a `dev→main` merge and production runs a single test.

---

## 2. Cadence map — when each gate is expected to run

Consolidated from `data/workflow-gate-inventory.json` cadence_matrix (22 gate rows,
doc-cited) and re-verified against TESTING_STRATEGY.md §5 (:232-252). Measured cost attached.

| Boundary | Gates the docs schedule there | Measured cost at that boundary |
| --- | --- | --- |
| **Per edit** | exact method/class (`feature --test/--class`), `fast` (7.5 s), narrowest frontend spec or `test:fast` (72.5 s) | seconds → ~1 min |
| **Per phase / slice completion** | feature lane (access 61.3 s / access-db 43.0 s / pipeline-feature), frontend feature lane (words: 114.3 s); build once per code state (6.7–34.6 s) | ~1–2 min |
| **Per change-type trigger** | `migration` (21.9 s) + `check-pending-model`; `smoke` (72.6 s) for any route/contract/auth/middleware/binding change; `process` (21.4 s); `pipeline` (64.1 s); `canonical-data` (UNKNOWN); `create-smoke-dump` after any migration (UNKNOWN); `check-api-contract` after any exporter-visible change (NEEDS_MEASUREMENT, and unscheduled — see §4.6); `test:gates`, `generate:permission-codes` (seconds) | ~20 s → minutes |
| **Engineering review** | scoped lane set per §5 row (e.g. Access: `access`+`smoke`+`tier-b` ≈ 200 s), plus the review's own ~52k-token context read; test-guard when tests in diff | ~3.5 min compute + the dominant context cost |
| **Pre-PR** | same evidence reused if tree unchanged (TESTING_STRATEGY.md:250-252); otherwise Backend scoped set or full `pre-pr` (~307 s est.); frontend `test:pre-pr` (≥265 s) when frontend changed | 0 (reuse) → ~10 min |
| **Release (dev→main, ~every 5 features)** | full Backend `pre-pr` (2 shards, ~307 s+) + `canonical-data` (UNKNOWN) + frontend `test:pre-pr` (≥265 s) with staged resources (TESTING_STRATEGY.md:355-358) | ≥ ~10 min + canonical UNKNOWN |
| **Deployment** | Railway Docker build + healthcheck; Vercel build. No tests | build-time only |
| **Opt-in** | `e2e` (never required — TESTING_STRATEGY.md:374) | NEEDS_MEASUREMENT |

The documented cadence is genuinely risk-scoped on paper — the §5 matrix's "Explicitly
excluded" column actively forbids over-running (e.g., pipeline/canonical never run for
isolated authorization work, no frontend tests for backend-only changes with no contract
diff). The measured problem is not the matrix; it is (a) what happens when boundaries repeat
(§3 flag 1), and (b) the context cost of the review boundary, which the matrix does not
price at all (§5). CONFIRMED.

**Real per-feature firing frequency is UNKNOWN for every gate.** The docs give triggers, not
telemetry; there is no CI history and no recorded run log to count actual invocations. Every
per-feature cost figure in §6 therefore states its frequency assumptions explicitly.

---

## 3. Adjudication of the 10 redundancy flags (with the measured numbers)

The inventory raised 10 flags (`data/workflow-gate-inventory.json` redundancy_flags). Each is
adjudicated here: is it real, what does it actually cost, and what classification does the
evidence support. Classifications use the Brief §9 taxonomy (`KEEP`, `MERGE`,
`DELETE_CANDIDATE`, `REWRITE`, `RUN_LESS_OFTEN`, `NEEDS_MEASUREMENT`) applied to gates.

### 3.1 Freshness rule × boundary stacking — THE dominant measured compute loop. CONFIRMED

TESTING_STRATEGY.md:49-51: evidence produced before the most recent change is stale and MUST
NOT close a phase, PR, or release gate. TESTING_STRATEGY.md:250-252 deduplicates review and
pre-PR evidence *only* "when the tree and environment have not changed." So any post-review
fix — a one-line finding fix — invalidates the boundary evidence and forces the boundary
lanes to rerun. Review producing findings is the common case, so the repetition is
designed-in.

Measured cost per post-review fix iteration:

| Scope of the boundary | Lanes re-run | Measured cost |
| --- | --- | --- |
| Ordinary backend (Access example, §5 row) | `access` 61.3 + `smoke` 72.6 + `tier-b` 65.9 + incremental build 6.7 | **~3.4 min** |
| Frontend in scope | + `test:pre-pr` ≥ 265 s | **+ ~4.5 min** |
| Full-regression / shared-infra boundary | Backend `pre-pr` ~307 s + frontend `test:pre-pr` ≥ 265 s | **~9.5–12 min** (canonical excluded, UNKNOWN) |

This is the single largest measured recurring compute cost in the workflow. Note the
freshness rule itself is *correct* — stale evidence genuinely proves nothing about the final
tree. The cost lever is not the rule but the **scope of what must be re-run**: the strategy
re-runs the whole boundary set rather than the lanes whose covered scope the fix touched.
Evaluation in §6 proposal A. Classification: **REWRITE** (of the re-verification scope rule,
not of the freshness principle). The freshness principle itself: **KEEP**.

### 3.2 `pre-pr` always rebuilds — real but small. CONFIRMED

`test-backend` rejects `--no-build` for executing `pre-pr` (Backend/scripts/test-backend:
144-152, verified) and rebuilds the solution single-threaded even seconds after a `tier-b
--no-build` run on the identical tree. Measured waste: an incremental rebuild is 6.7 s; a
warm-cold one 34.6 s. Against a ~307 s lane run this is 2–10% overhead, and the rule buys a
real guarantee (the full gate always runs against freshly compiled output; no stale-dll
false pass). Classification: **KEEP** (the ~7–35 s is cheap insurance at a gate that runs
approximately once per feature/release; not worth the failure mode its removal invites).

### 3.3 `test:pre-pr` compiles overlapping TypeScript three times — real, bounded. CONFIRMED

package.json:28 (verified): checks → `typecheck` (14.6 s, two tsc projects) →
`build:verify` (18.3 s, full AOT compile) → `test:full` (232.2 s, the test builder compiles
its own bundle again — TESTING_STRATEGY.md:289-290 says so itself). Three compilations of
largely the same surface. But the measured bound on any saving is small: dropping
`typecheck` would save ~15 s of a 265 s composite (~5%) and would lose the only leg that
type-checks the spec project with full strictness diagnostics; the compiles inside
`build:verify` and the test builder are not separable without changing the toolchain. The
real cost center of the composite is `test:full` at 232.2 s (88% of it) — that is a test-
suite-size question for report 02, not a compile-duplication question. Classification:
**KEEP** (the duplication is real but its removal saves ~15–30 s and weakens diagnostics;
disproportionate risk-to-reward). The two parity checks re-running when the catalogue is
untouched: seconds-scale, immaterial.

### 3.4 Any migration forces canonical dump regeneration — cost UNKNOWN, protection real. NEEDS_MEASUREMENT

TESTING_STRATEGY.md:186-188 (verified): "Any migration invalidates the dump. Regenerate it
with `Backend/scripts/create-smoke-dump --yes` in the same change... this has bitten
repeatedly." So an access-only or abwab-only migration with zero Quran-data impact still
forces a pg_dump of a ~⅓ GB artifact plus baseline verification, and Backend `pre-pr`
always includes the canonical-bearing shard. Neither `create-smoke-dump` nor
`canonical-data` was measured (read-only audit; the dump script is stateful). Two things are
simultaneously true: the blanket trigger is coarser than strictly necessary (the dump's
*restore* depends on the migration head id, which any migration moves —
`SmokeDumpGate` checks manifest migration id vs tree head, TESTING_STRATEGY.md:183-186 —
so the coarseness is actually structural: the gate keys on migration head, not on data
shape), and this sits squarely in Brief §29 (canonical source checks, migration safety).
Classification: **NEEDS_MEASUREMENT** — measure `create-smoke-dump` and `canonical-data`
wall time once before anyone debates the cadence; if regeneration is ~1–2 min, this flag
dissolves; if it is ~10+ min, a keyed-manifest design question exists for a future plan. No
weakening of the gate itself is proposed.

### 3.5 Backend contract tweak cascades into the full frontend composite. LIKELY, and mostly by design

The Backend-only exemption (TESTING_STRATEGY.md:223-228, verified) is void the moment
`check-api-contract` regenerates the committed client: regenerated files under `Frontend/`
are frontend changes, which per the same rule trigger the focused contract/authorization
lane, typecheck — and `test:pre-pr` "only because Frontend files then changed." A one-line
response-shape change buys ≥265 s of frontend composite plus the unmeasured
`check-api-contract` run. Adjudication: the *coupling* is correct — OpenAPI contract parity
is a §29 item and the generated client genuinely changed — but the strategy's own wording
already scopes the requirement ("run the focused Frontend contract/authorization lane and
the required type-check; run `test:pre-pr` only because Frontend files then changed"), and
the marginal question is whether a models-only regeneration with a passing `typecheck` needs
the full `test:full` leg or only the contract/authorization lanes. That is a cadence
evaluation, not a redundancy: **RUN_LESS_OFTEN** candidate for the `test:full` leg on
regeneration-only frontend diffs, with the focused lanes + typecheck retained. Risk: a
generated-model change that silently alters runtime behavior a focused lane misses;
mitigation exists (typecheck catches shape breaks; authorization lane covers auth models).

### 3.6 `check-api-contract` has no scheduled home — the one under-scheduled gate. CONFIRMED

Verified independently: `grep -c check-api-contract TESTING_STRATEGY.md` returns **0**. The
gate appears in no lane, no pre-PR composite, and no §5 row. Its only cadence anchor is a
README sentence ("run it after any change that alters what the exporter reads" —
Backend/api/QuranDashboard.Api/Controllers/README.md:169-170), and that same README records
the consequence: **the committed spec stayed stale for several commits** after `78d70f04`
stripped controller XML docs, "because `check-api-contract` compares
regenerated-against-committed and cannot see a spec that nothing has regenerated"
(Controllers/README.md:168-170, verified). This is the mirror image of every other flag:
everything else runs too often; this §29-relevant gate (OpenAPI contract parity) is not
scheduled at all, and its staleness failure mode has already occurred once. Each run also
costs a full Release build + npm codegen (unmeasured), which discourages defensive manual
runs. Classification: **REWRITE** (of its cadence: it needs a named home at a boundary —
most plausibly the pre-PR checklist for any change touching `Backend/api/` contracts —
evaluated in §6 proposal D). The gate itself: **KEEP** unambiguously.

### 3.7 Review workflows front-load a large reading budget — the dominant *agent* cost. CONFIRMED

See §5. The flag is upheld with sharper numbers than the inventory's "~100KB+": the
engineering-review trace measured **206,417 B (~51.6k tokens) mandatory** for a backend diff
with tests, 260,782 B with conditionals; worst-case closure **359,926 B (~90k tokens)**; a
frontend review swaps in UI_STYLE_SYSTEM.md alone at 103,970 B.

### 3.8 `deploy-smoke` overlaps build gates at the same boundary. LIKELY, minor

Its trigger list ("before a final review / before opening a PR" — deploy-smoke SKILL.md:
26-30, verified) repeats restore+build work the lanes and `build:verify` already performed
at the same boundary; its unique value (runtime boot, migration-target inspection, health of
changed endpoints) is bundled with the redundant build legs. The duplicated build costs
~7–35 s per invocation — small. Classification: **KEEP** with a **MERGE**-shaped future
option (accepting fresh build evidence instead of rebuilding), only worth doing if the skill
is being edited anyway. Frequency per feature: UNKNOWN (triggers are migration/cross-stack
conditioned).

### 3.9 `commit-workflow` invites a third round of checks. LIKELY, unpriced

"Run relevant Backend/Frontend checks for changed areas when feasible" before committing
(commit-workflow SKILL.md:117, verified) is unscoped; at a boundary where review and pre-PR
evidence already exist, a literal agent re-runs lanes a third time — up to another ~3.4 min
backend / ~4.5 min frontend per commit. Whether agents actually do this is UNKNOWN (no run
telemetry). Classification: **REWRITE** candidate (one sentence: defer to
TESTING_STRATEGY.md's evidence-reuse rule instead of inviting re-runs) — smallest change in
this report, bounded by a one-line skill edit in a future plan.

### 3.10 `speckit-implement` per-phase validation vs TESTING_STRATEGY. LIKELY tension, resolved on paper

Verified both sides: the upstream speckit-implement template mandates "Validation
checkpoints: Verify each phase completion before proceeding" (SKILL.md:157) and completion
"Validate that tests pass and coverage meets requirements" (SKILL.md:177), while
TESTING_STRATEGY.md:341-343 orders the phase orchestrator to NOT run broad or pipeline lanes
automatically for every phase, and the strategy self-declares authority over every skill on
test selection (TESTING_STRATEGY.md:11-15). The speckit wording never names a broad lane —
per-phase validation via narrow lanes is exactly what the strategy requires — so the "broad
gate per phase" reading is an inference from unqualified wording, not a confirmed textual
conflict: LIKELY tension. Cost if an agent reads speckit
that way: ~66 s (`tier-b`) × (phases−1) per feature plus container churn — ~2–4 min per
typical feature (assumption: 3–4 phases), and worse if "coverage meets requirements" is read
as full-suite. Speckit is out of redesign scope (Brief §31), so the classification attaches
to the *conflict*, not the skill: **KEEP** the TESTING_STRATEGY rule as the winner (it
already claims authority); the residual risk is an agent that reads the speckit template
first. Whether that mis-firing actually happens is UNKNOWN — no run logs exist.

---

## 4. The no-CI doctrine — an honest evaluation, not dogma

The facts (all CONFIRMED, independently verified):

- No CI exists anywhere in the tree: no `.github/`, and TESTING_STRATEGY.md:296-298
  declares it and forbids ever claiming "CI is green."
- Every gate in this report is local and human/agent-triggered; **nothing verifies any gate
  ran** (TESTING_STRATEGY.md:300-310). Evidence is recorded command output, and only that.
- Deploys run zero tests: Railway = Dockerfile build + `/api/health` healthcheck from
  `main` (Backend/railway.json); Vercel builds from the committed generated client
  (Frontend/quran-dashboard-ui/README.md:45-46).
- The doctrine has teeth elsewhere in the system: the no-durations rule (:17-24), the
  evidence-recording obligations (:63-68), the reviewer's duty to check lanes ran
  (engineering-review SKILL.md:381-410), and pr-context-prep's evidence requirements all
  exist *because* there is no CI. The doctrine is load-bearing across at least five
  documents.

What the doctrine costs, on the measured numbers:

1. **The freshness loop (§3.1) runs on the developer's machine and the agent's clock.** In a
   CI world, post-review fix → push → machine re-runs the boundary set (~3.4–12 min)
   asynchronously while the agent does other work. Here the same wall time is synchronous,
   attended cost, per iteration.
2. **Enforcement is review labor.** Fork caps, hang timeouts, lane sufficiency — all are
   "review obligations because nothing enforces them" (TESTING_STRATEGY.md:305-306). That is
   part of why the review skill's context budget (§5) is as large as it is: the reviewer is
   the CI.
3. **The deploy path trusts the release ritual entirely.** One skipped or dishonestly
   recorded release gate and untested code reaches production with only a healthcheck
   between it and users.

What the doctrine buys, honestly stated:

1. Zero infrastructure cost and zero CI-maintenance surface for what the memory and history
   evidence indicates is a solo-developer project with a ~5-feature release cadence
   (CLAUDE.md branching section).
2. The gates that *do* run are the right ones: the §5 matrix is more precisely risk-scoped
   than most CI configurations (which run everything, always). CI would likely *increase*
   total compute while decreasing attended time.
3. Canonical lanes genuinely execute — they need staged local resources that a hosted runner
   would not have (TESTING_STRATEGY.md:307-310). A naive CI would run *fewer* of the
   high-value gates, or fake-skip them.

Verdict: **the risk/cost tradeoff of no-CI is a judgment call that this audit cannot settle
with present evidence — NEEDS_MEASUREMENT / judgment.** The decisive unknowns: how often
release gates are actually run in full versus partially reused (no run log exists), how often
the freshness loop iterates per feature (UNKNOWN), and whether a minimal post-merge check
(build + tier-b + typecheck on `dev`, ~1.5 min of machine time) could be added without
inheriting the drift/false-confidence pathologies the strategy explicitly designed against
(TESTING_STRATEGY.md §8 anticipates its own rewrite if CI is added). What this audit *can*
say: any future CI evaluation must not casually move canonical/dump gates to hosted runners
(§29: canonical source checks depend on local staged resources), and must not resurrect
"CI is green" as substitute evidence — the strategy's evidence rules are the stronger
invention and should survive either outcome.

---

## 5. Review workflows reread ~50–90k tokens per invocation — the cost that dwarfs the compute gates

From `data/instruction-inventory.json` task traces and `data/skill-inventory.json`
engineering_review_closure (spot-verified against the skill file):

| Review invocation | Mandatory read | With conditionals | Tag |
| --- | --- | --- | --- |
| engineering-review, backend diff with tests | 17 files, 206,417 B (~51.6k tokens) | 24 files, 260,782 B (~65.2k tokens) | CONFIRMED |
| engineering-review, always-reads floor (any scope) | body 26,670 B + CODING_PRINCIPLES 5,190 B + TESTING_STRATEGY 33,427 B + quran-data-safety 3,124 B = **68,411 B (~17.1k tokens)** before any conditional | — | CONFIRMED |
| engineering-review, worst case (full-stack Spec-Kit diff, deep pass) | — | **359,926 B (~90k tokens)** | CONFIRMED |
| frontend review increment | UI_STYLE_SYSTEM.md alone +103,970 B (~26k tokens); frontend group total 160,264 B | — | CONFIRMED |
| performance-backend-review | 73,044 B trace total per instruction-inventory, but the arch-doc reads are optional per the skill (SKILL.md:103) | 123,878 B | CONFIRMED bytes; "mandatory" is LIKELY only for the ~18.6 KB body + quran-data-safety floor |
| performance-angular-review | ~19 KB body mandatory (+3.1 KB quran-data-safety); up to ~166 KB if all optional docs are pulled (SKILL.md:122 "optional") | — | CONFIRMED floor; ceiling LIKELY |

Scale comparison, same units of "cost to run the gate once": `tier-b` verifies 1,372 tests
in 66 s of unattended machine time; a single engineering-review invocation *reads* ~52k
tokens of instructions and references before reading the diff. In agent-operation terms
(tokens ≈ money and latency), **one review round costs more than every backend compute gate
of an ordinary feature combined** — and §3.1 shows review rounds repeat: the freshness rule
plus the re-review after fixes re-prescribes the same reading set each iteration, since
nothing in the skill carries state between invocations. CONFIRMED.

Two mitigating facts, verified: scoped review paths genuinely exist (SKILL.md:44-45 "Read
only the docs relevant to what actually changed"; per-area conditional blocks; clean-code
pack per-finding; Spec-Kit artifacts skipped for simple changes — SKILL.md:143-144), so the
90k figure is a ceiling, not the norm. And TESTING_STRATEGY.md (33.4 KB, ~8.4k tokens of
the floor) is genuinely load-bearing for the reviewer's lane-sufficiency check
(SKILL.md:381-385). The simplification target is therefore not "read less law" but "stop
re-reading unchanged law every iteration" and "route the packs on demand" — this is
report 04's territory (skill redesign); this report prices it: **each avoided full re-read
of the floor saves ~17k tokens; each avoided worst-case re-read saves up to ~90k tokens per
review iteration.** The compute gates are a rounding error next to this. CONFIRMED.

---

## 6. Candidate simplifications — each answering Brief §4's seven questions

These are evaluations for future plans, not instructions. High-risk gates from Brief §29
(authentication/authorization lanes, smoke, migration safety, canonical source checks,
OpenAPI contract parity, audit/concurrency coverage) are preserved in every proposal.

### A. Scope the freshness re-verification (the §3.1 loop) — REWRITE

1. **Value today:** guarantees boundary evidence matches the final tree; absolute staleness
   safety.
2. **Depends on it:** engineering-review's verification check, pr-context-prep evidence,
   release acceptance — all consume "fresh at boundary" claims.
3. **Risk if changed:** a post-review fix with effects outside its apparent scope passes on
   focused re-runs; cross-lane regression reaches the PR.
4. **Equivalent protection elsewhere:** partially — the §5 matrix already encodes
   scope→lane mapping; the release composite (§5 release row) still runs everything at the
   release boundary regardless.
5. **Smallest safe simplification:** a strategy rule distinguishing *initial* boundary
   verification (full scoped set) from *fix re-verification* (the lane(s) covering the
   fix's changed scope + the single broadest lane of the boundary, e.g. `tier-b` alone
   rather than `access`+`smoke`+`tier-b`), with the full set still required when the fix
   touched route/auth/migration/canonical triggers.
6. **Verified later by:** the release composite still running in full; reviewer spot-checks
   that fix-scope→lane mapping was honest.
7. **Recurring cost removed:** per fix iteration, backend ordinary boundary ~200 s → ~73 s
   (tier-b + incremental build): **~2.1 min/iteration**; full-regression boundaries
   ~572 s → ~150–200 s: **~6–7 min/iteration**. Assuming 1–2 fix iterations per feature
   (assumption — real frequency UNKNOWN): **~2–14 min per feature**, the largest available
   compute saving. Tag: LIKELY (savings measured; iteration frequency assumed).

### B. Give `check-api-contract` a scheduled home — REWRITE (cadence only)

1. **Value:** the only automated OpenAPI-parity gate (§29 item); already caught real drift
   once it was finally run (Controllers/README.md:168-170).
2. **Depends on it:** Vercel builds from the committed client; frontend typecheck substance;
   generated-model truthfulness.
3. **Risk if changed:** none from scheduling it; the risk is the status quo (proven
   staleness incident).
4. **Equivalent protection:** none — nothing else compares committed spec against the tree.
5. **Smallest step:** name it in the pre-PR checklist (TESTING_STRATEGY §9 pre-PR workflow
   paragraph and/or pr-context-prep's evidence list) for changes touching `Backend/api/`
   contract surface.
6. **Verified later by:** absence of stale-spec commits; pr-context-prep evidence lines.
7. **Recurring cost removed:** removes *defensive* manual runs (each a Release build +
   codegen) and removes the staleness-incident class. Adds ~40–70 s (NEEDS_MEASUREMENT)
   when it fires, roughly once per API-touching feature. Net: small positive cost, large
   risk reduction. Tag: CONFIRMED problem, LIKELY cheap fix.

### C. Resolve the speckit-implement cadence conflict in TESTING_STRATEGY's favor — KEEP (strategy) / conflict removal

1. **Value of the current speckit wording:** generic upstream template completeness.
2. **Depends on it:** nothing project-specific — the strategy already claims authority
   (TESTING_STRATEGY.md:11-15).
3. **Risk:** none to safety; per-phase validation via *focused* lanes remains required.
4. **Equivalent protection:** TESTING_STRATEGY.md:341-343 already states the correct rule.
5. **Smallest step:** since speckit skills are out of redesign scope (Brief §31), the
   candidate is a one-line precedence note where the orchestrator already looks (the
   strategy's phase-orchestrator paragraph naming the speckit template as superseded on
   cadence).
6. **Verified later by:** phase-completion evidence citing focused lanes, not tier-b/full.
7. **Recurring cost removed:** up to ~66 s × (phases−1) + container churn per feature
   *if* agents currently obey the speckit wording — **~2–4 min/feature, conditional on a
   mis-firing whose actual frequency is UNKNOWN**. Tag: LIKELY tension,
   NEEDS_MEASUREMENT impact.

### D. Scope commit-workflow's check-invitation — REWRITE (one sentence)

1. **Value:** catches uncommitted-state surprises before commit.
2. **Depends on it:** nothing; it duplicates the strategy's selection authority.
3. **Risk:** near-zero — evidence-reuse is already the strategy's rule (:250-252).
4. **Equivalent protection:** TESTING_STRATEGY §5 + freshness rule govern completely.
5. **Smallest step:** the sentence defers to existing fresh evidence instead of inviting
   re-runs.
6. **Verified later by:** commit flows citing existing evidence.
7. **Recurring cost removed:** up to ~3.4 min per commit *if* followed literally today
   (frequency UNKNOWN). Tag: LIKELY.

### E. Review-context re-read reduction — priced here, owned by report 04

In brief: value today = full-context review rigor (1); depends on it = release/PR
confidence built on review verdicts (2); risk if changed = shallower re-reviews that miss
fix-induced regressions (3); equivalent protection = the recorded findings scope the second
pass while the compute lanes still verify behavior (4). The deeper redesign of these is the
review skill's own (report 04). Priced from §5: **~17k tokens
floor, ~52k typical, ~90k worst case per review iteration**; with 1–3 review iterations per
feature (assumption), the recurring agent cost is **~50k–270k tokens per feature** — larger
than every compute gate combined. Smallest safe step (evaluation): on-demand routing of the
clean-code pack and per-area docs (already partially true), plus an explicit re-review rule
that scopes the second pass to the findings and their fixes rather than re-prescribing the
full read. Tag: CONFIRMED cost; saving LIKELY.

### F. Non-candidates — measured and deliberately left alone

- `pre-pr` always-builds (§3.2): **KEEP** — ~7–35 s insurance at a once-per-feature gate.
- `test:pre-pr` triple compile (§3.3): **KEEP** — ~15–30 s bound, diagnostics loss.
- Migration→dump regeneration (§3.4): **NEEDS_MEASUREMENT** before any cadence debate;
  §29-protected.
- Contract-change→frontend cascade (§3.5): coupling **KEEP**; only the `test:full` leg on
  regeneration-only diffs is a **RUN_LESS_OFTEN** evaluation candidate.
- `deploy-smoke` build overlap (§3.8): **KEEP**, minor.
- The destructive scripts (`drop-db`, `reset-db`, `wipe-abwab`): correctly gated, not
  workflow cost; out of simplification scope. The one observed asymmetry — `update-db`
  mutates schema with no `--yes` gate where its siblings have one
  (Backend/scripts/update-db:14-17 vs drop-db:7-24) — is a hardening observation for a
  future plan, not a simplification. Tag: CONFIRMED.

---

## 7. Q68 — a future risk-based trigger matrix (evaluation, not implementation)

The existing §5 matrix is already risk-based; the future matrix differs in three ways only:
it distinguishes initial-verification from fix-re-verification (proposal A), it schedules
the one orphaned gate (proposal B), and it attaches measured cost so future cadence debates
stop being rhetorical. §29 gates are untouched: smoke for any route/contract/auth/
middleware/binding change, access lanes for authorization scope, migration +
check-pending-model + dump regeneration for schema scope, canonical-data on its triggers
and at release, both parity checks, and the full release composite all keep their current
triggers.

| Trigger | Gate set (unchanged high-risk gates in bold) | Cost now | Cost under evaluation | Delta/feature (assumptions below) |
| --- | --- | ---: | ---: | --- |
| Per edit | exact test / `fast` / narrowest spec | 7.5–72.5 s | unchanged | 0 |
| Slice/phase done | feature lane + build once | ~1–2 min | unchanged (speckit conflict resolved: no tier-b per phase) | 0 to −4 min |
| Route/contract/auth/middleware/binding changed | **`smoke`** + family lane | 72.6 s+ | unchanged | 0 |
| Migration/schema | **`migration` + `check-pending-model` + dump regen** | 21.9 s + UNKNOWN | unchanged | 0 |
| API contract surface changed | (unscheduled today) | 0, plus staleness incidents | **`check-api-contract`** at pre-PR | +~40–70 s, −incident class |
| Contract regen touched only generated frontend files | focused contract/auth lanes + typecheck + `test:full` via test:pre-pr | ≥265 s | focused lanes + typecheck only; `test:full` at release | −up to ~3.5 min when it fires, minus the (unmeasured) focused-lane cost |
| Review boundary (initial) | scoped set per §5 (e.g. ~200 s) + review read ~52k tokens | ~3.5 min + context | unchanged compute; on-demand refs + scoped re-review for context | −tens of k tokens/iteration |
| Post-review fix | full boundary set re-run (~200–572 s) | ~3.4–9.5 min/iteration | fix-scope lane + broadest boundary lane (~73–200 s) | **−2 to −7 min per iteration** |
| Pre-PR | reuse or full set | 0–10 min | unchanged rule, benefits from A | included above |
| Release (~every 5 features) | **full `pre-pr` + `canonical-data` + frontend `test:pre-pr`** | ≥10 min + UNKNOWN | unchanged — the full set is the backstop that makes A safe | 0 |
| Deploy | build + healthcheck, zero tests | — | unchanged pending the no-CI evaluation (§4) | 0 |

**Expected per-feature time saved** (all frequency assumptions explicit, real frequencies
UNKNOWN — no run telemetry exists): assuming per feature ~3 phases, 1 engineering review
with 1–2 fix iterations, 1 pre-PR, and an API-touching change in half of features:
proposal A saves ~2–14 min; C saves 0–4 min (conditional on the mis-firing occurring);
the §3.5 `test:full` scoping saves up to ~3.5 min, minus the (unmeasured) focused-lane
cost, on contract-touching features; B *adds* ~1 min
but retires a proven incident class. **Net candidate compute saving: roughly 2–20 minutes
of attended wall time per feature (LIKELY), plus the dominant agent-side saving of tens of
thousands of tokens per review iteration (CONFIRMED cost, LIKELY saving — owned by report
04).** Tag on the headline: LIKELY — measured costs, assumed frequencies.

---

## Mandatory questions answered

**Q65 — Which gates run too often?** On the *documented* cadence, almost none — the §5
matrix is genuinely risk-scoped and its "Explicitly excluded" column actively suppresses
over-running (CONFIRMED). The over-running is emergent, at repeated boundaries: (1) the
freshness rule forces the *entire* boundary lane set to re-run after any post-review fix —
~3.4 min (backend ordinary) to ~9.5+ min (full-regression + frontend) per iteration, the
dominant measured loop (CONFIRMED, §3.1); (2) `create-smoke-dump` + canonical exposure fire
on *every* migration regardless of data scope, at UNKNOWN cost (§3.4); (3) a backend
contract tweak escalates to the full ≥265 s frontend composite when focused lanes +
typecheck may suffice (LIKELY, §3.5); (4) speckit-implement's template can be read as inviting a broad lane
per phase against the strategy's explicit prohibition (LIKELY tension, UNKNOWN
incidence, §3.10); (5) commit-workflow invites a third round of already-produced evidence
(LIKELY, §3.9). One gate runs too *rarely*: `check-api-contract` — never scheduled anywhere,
with a staleness incident on record (CONFIRMED, §3.6).

**Q66 — Which builds/tests are duplicated?** Builds: `pre-pr` always rebuilds even over a
fresh identical tree (~7–35 s, KEEP — §3.2); `check-pending-model --build`, `export-swagger`
and `check-api-contract` each run their own full/Release single-threaded build per
invocation regardless of freshness (CONFIRMED; unmeasured for Release); `deploy-smoke`
re-verifies restore+build at boundaries where lanes and `build:verify` already built
(LIKELY, minor — §3.8). Tests/compiles: `test:pre-pr` compiles overlapping TS three times
(typecheck + ng build + the test builder's own bundle) — real but bounded at ~15–30 s
against a 265 s composite whose true cost center is `test:full` at 232.2 s (CONFIRMED,
§3.3); `tier-b` re-runs most access/smoke-adjacent classes after focused lanes at stacked
boundaries — by design, and cheap in isolation (65.9 s), expensive only through the
§3.1 repetition loop (CONFIRMED).

**Q67 — Which review workflows reread excessive context?** engineering-review is the
outlier: ~68 KB (~17k tokens) always-read floor, 206 KB (~52k tokens) measured mandatory
for a backend diff with tests, 360 KB (~90k tokens) worst-case closure; a frontend review
adds UI_STYLE_SYSTEM.md's 104 KB alone; performance-angular-review's mandatory floor is
~22 KB (body + quran-data-safety) but can pull up to ~166 KB if all its optional docs are
consulted (CONFIRMED floor, LIKELY ceiling);
test-guard adds ~30.4 KB whenever tests are in a diff (engineering-review and test-guard
numbers CONFIRMED, §5). Because no state
carries between invocations, every fix→re-review iteration re-prescribes the same set. In
agent-cost terms this dwarfs every compute gate in the inventory — one review read
out-costs a feature's entire backend lane compute (CONFIRMED). Scoped-read provisions
already exist in the skill (verified), so the ceiling is not always paid — but the floor
alone (~17k tokens) exceeds the token-equivalent of any lane's runtime.

**Q68 — What future risk-based gate matrix would reduce feature cost safely?** §7 in full.
Essence: keep the §5 matrix and every §29 gate exactly where it is (smoke, access,
migration + dump regeneration, canonical-data, parity checks, full release composite);
change only (a) fix-re-verification scope — re-run the fix's scope lane plus the single
broadest boundary lane instead of the whole set, with the full release composite as the
backstop (−2 to −7 min per fix iteration); (b) schedule `check-api-contract` at the pre-PR
boundary for API-contract-touching changes (+~1 min, retires a proven incident class);
(c) neutralize the speckit per-phase broad-lane invitation (0–4 min); (d) scope the
`test:full` leg for regeneration-only frontend diffs (up to −~3.5 min when firing, minus
the unmeasured focused-lane cost); (e) reduce
review re-reads via on-demand references and scoped re-review (tens of k tokens per
iteration — the largest single saving in the whole workflow). Net: ~2–20 attended minutes
per feature (LIKELY, frequencies assumed) with no §29 gate weakened.

---

## Measurement gaps

- **`canonical-data` lane and `create-smoke-dump` wall time — UNKNOWN.** Never measured
  (real imports + exclusive postgres:18 shard; dump script is stateful). Adjudication of
  flag §3.4 and any future cadence debate is blocked on one measured run of each.
- **`check-api-contract` end-to-end cost — NEEDS_MEASUREMENT.** Release build + codegen +
  diff; needed to price proposal B precisely.
- **Backend `pre-pr` actual wall — estimated (~307 s), never executed** per audit protocol;
  the exclusive `SmokeDataReadTests` shard allowance (~60 s) is unverified.
- **E2E runtime — NEEDS_MEASUREMENT** (live DB + dual servers; out of read-only scope).
  Opt-in only, so absent from all per-feature figures.
- **Frontend focused contract/authorization lane wall — not measured**
  (`data/runtime-measurements.json` frontend.gates has no entry for it); the §3.5/§7
  `test:full`-scoping saving is therefore an upper bound (≤ ~3.5 min) until the retained
  lane is priced.
- **Real gate firing frequency per feature — UNKNOWN across the board.** No CI, no run log,
  no telemetry; every per-feature saving in §6/§7 states its assumed frequencies (1–2 fix
  iterations, ~3 phases, API-touching in ~half of features). The single highest-value
  follow-up measurement is a per-feature gate-invocation log for two or three features.
- **Single-run timings, no variance.** All walls are one solo-load run at HEAD `72792ba9`;
  `build_cold_s` had warm bin/obj and NuGet caches.
- **`deploy-smoke` and review-skill wall-clock cost — not measured** (context bytes/tokens
  measured instead; token-to-cost conversion is environment-dependent).
- **Whether agents actually obey the speckit per-phase wording or commit-workflow's check
  invitation — UNKNOWN**; both costs are conditional on behavior nothing records.
- **Parallel-build baseline — not taken** (would require altering scripts); the cost of the
  deliberate `-m:1` serialization is unquantified.
