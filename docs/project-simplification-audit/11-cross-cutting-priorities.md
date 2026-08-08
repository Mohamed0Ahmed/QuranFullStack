# 11 — Cross-Cutting Priorities

Audited baseline: branch `dev`, commit `72792ba9ff589c66aa25632a464b56b8bf7787af`, audit dates 2026-08-08/09.

This document groups the audit's findings into **independent future workstreams** per brief §26. It is **not an implementation plan** and deliberately contains no task lists, no sequencing inside workstreams, and no code-level instructions. Each workstream cites the detailed report that owns its evidence.

---

## 1. The central cross-cutting finding

The project's recurring cost is **not architecture and not machine time — it is duplicated knowledge surfaces read by agents**.

Three measured facts anchor this (all CONFIRMED):

1. **Compute gates are cheap.** The entire effective backend suite runs in ~6 minutes (3,231 tests, 0 failures); frontend `test:full` in 232 s (2,964 tests). Backend build is 34.6 s cold-ish / 6.7 s incremental (report 02, 10; `data/runtime-measurements.json`).
2. **Handwritten product code is only 27.8 % of the repo** (113,155 of 407,221 LOC). Architecture layer analysis found zero deletable interfaces, a single-DTO wire chain, and a justified cache tier — the layers earn their keep (report 08).
3. **Context is expensive.** The instruction surface is ~896 KB (~224k tokens) across 74 files; a tiny frontend UI fix mandates ~73.4k tokens of reading; a formal engineering review prescribes ~51.6k tokens measured (~90k worst case); the same rules are restated up to 16 times (reports 03, 04, 06).

Consequently the highest-leverage workstreams are the ones that reduce **what must be read and maintained per task**, and the lower-leverage ones are those that reduce code that is written once and rarely re-read.

### The representative feature cost stack (brief §21)

| Layer | Evidence | Verdict |
|---|---|---|
| Instruction loading | 9.5k–73.4k tokens per task trace (03 §10) | **Dominant, reducible ~42 % by routing alone** |
| README/architecture reading | up to 44k tokens README share on a cross-stack abwab task (06 §6) | Reducible at the tail, keep the median |
| Skill loading (reviews) | 17.1k floor / 51.6k measured / ~90k worst case (04 §4) | Second-largest; partially reducible |
| Planning/spec reading | unmeasured — no feature open at baseline | NEEDS_MEASUREMENT |
| Implementation boilerplate | words feature: one entity slice ≈ 31 files / ~3,800 LOC (08 §5.2) | Reducible for repeated-entity features |
| Frontend style boilerplate | 76 repeated declaration blocks; two unused utility systems (07) | Reducible after one direction decision |
| DTO/API/client boilerplate | single-DTO chain; models-only generated client (08, 09) | Already lean |
| Test authoring | new explorer page ≈ 1,100–1,600 spec LOC today (02 §6) | Reducible via shared harnesses |
| Focused tests | 7.5–72.6 s per backend lane; 72.5 s test:fast (1b) | Already cheap — leave alone |
| Broad gates | pre-pr ≈ 5–7 min backend + ≥4.4 min frontend | Cheap; the problem is the **re-run loop** |
| Post-review fix re-verification | ~3.4–9.5+ min per iteration (10 §3) | Reducible by scoping |
| Review reference loading | re-prescribed in full on every re-review (10 §5) | Reducible via 03/04 |
| Commit workflow | seconds; two `.agents` pointer defects found (04 §6) | Fix defects, otherwise fine |

---

## 2. Workstreams

Scoring model (brief §26): **Impact × Frequency × Confidence ÷ Risk**, each 1–5 (Confidence 0–1). Scores are coarse rankings, not measurements.

### WS1 — Agent Instruction Separation & Routing (owner: report 03)

- **What it is.** Correct the routing/trigger layer first (no rule deleted, no document rewritten): stop the AGENTS/CLAUDE byte-mirror maintenance via pointer-stubs (variant b, the pattern already proven by the 10 `.agents` skill adapters), narrow the always-fire triggers (UI_STYLE_SYSTEM on any visual change; nearest-README at the 98 KB tail), and resolve the on-record AGENTS↔Cursor contradiction.
- **Expected recurring benefit.** ~42 % of instructed mandatory context across the six normal-work traces (~103k tokens; tiny frontend fix 73.4k → ~30k). Ends the hand-sync regime that drifted 3 times in 9 weeks. Second-stage doc slimming (WS2/WS4) plausibly reaches ~85 % for tiny tasks (NEEDS_MEASUREMENT).
- **Risk.** Low–medium. §29 constraint: the 16 restatements of Quranic-data safety must collapse to canonical + pointers, never net-delete. Codex CLI is a **CONFIRMED live consumer** of the AGENTS surface (refs/codex checkpoints, 2026-08-05→07 UTC) — the AGENTS entrypoint must keep working.
- **Prerequisites.** Two one-question user decisions: is Cursor still a supported consumer (UNKNOWN from repo)? Is Codex to be supported forward (activity CONFIRMED, level NEEDS_MEASUREMENT)?
- **Effort.** Small (stage 1); medium (stage 2, shared with WS2/WS4).
- **Confidence.** High — all savings arithmetic is static and was adversarially recomputed.
- **Success criteria.** Re-run the 03 §10 trace model: tiny-task traces ≤ ~30k tokens; zero byte-identical instruction pairs; repo-wide grep shows no dangling route and exactly one canonical statement per rule with pointers elsewhere.
- **Score:** Impact 5 × Freq 5 × Conf 0.9 ÷ Risk 2 ≈ **11.3**

### WS2 — README / Documentation Simplification (owner: report 06)

- **What it is.** Compress the five-file README tail (abwab 97.8 KB, access-admin 42.6 KB, words 37.8 KB, scripts 26.2 KB, tests 19.7 KB) to the bounded-context register (what the area does, unique invariants, contracts, where to start); de-narrate TESTING_DEBT's ~55 lines of paid-row prose; fix the one CONFIRMED inter-doc conflict (font weights, jointly with WS4); repair the `docs/contracts/security-access.md` drift.
- **Expected recurring benefit.** ~70–100 KB (~18–25k tokens) off the mandatory-read pool; abwab-task README burden −40–50 %. 33 of 40 READMEs already match the target model — this is a tail problem, not a system problem.
- **Risk.** Medium — the exact failure mode the lifecycle rule warns about: *folding wrong is worse than deleting*. Register-1 safety contracts that exist only in READMEs (xmin/409 semantics, auth two-scheme facts, word identity keys) must survive verbatim. Growth attribution matters: the mass accrued **during** feature work (ux-slice series), not on sweep day — so the fix is a size register for READMEs, not a lifecycle change.
- **Prerequisites.** None hard; coordinate with WS1 stage 2. UI_STYLE_SYSTEM is owned by WS4, not here.
- **Effort.** Medium. **Confidence.** High on numbers, medium on achievable compression.
- **Success criteria.** Tail READMEs at target size with every removed claim either moved to a canonical owner (with `file:LINE` proof) or shown code-derivable; zero dangling inbound references; nearest-README median cost unchanged.
- **Score:** 4 × 4 × 0.8 ÷ 3 ≈ **4.3**

### WS3 — Custom Skill Simplification (owner: report 04)

- **What it is.** Deduplicate the triplicated TESTING_STRATEGY evidence block (~2–3 KB per copy across engineering-review / test-guard / pr-context-prep); give the one true orphaned rule (God-service ban) a canonical doc home; fix the two `.agents` adapter defects — including the **commit-workflow pointer saying "sync-to-main" twice** where canon is sync-to-dev (safety-relevant: `main` auto-deploys and Codex is a confirmed active consumer); consider TRIGGER_NARROWING for backend-structure-review only after routing is measured.
- **Expected recurring benefit.** ~15–20 KB (~4–5k tokens) static removal; formal-review invocation −5–8k tokens; elimination of a real mis-routing hazard.
- **Risk.** Low for dedup/defect fixes. Medium for trigger changes: ≥6 skills claim a bare "review this"; narrowing before measuring routing could hand reviews to a generic reviewer that skips Quran-data-safety gates.
- **Prerequisites.** None for the defect fixes and dedup. A controlled-prompt routing probe before any trigger narrowing.
- **Effort.** Small. **Confidence.** High. **No skill is a DELETE candidate** — every one carries at least one behavior existing nowhere else.
- **Success criteria.** Zero rules existing only in skill bodies; adapters pointer-only and accurate; exactly one copy of the evidence block; review invocations measured at reduced prescribed bytes.
- **Score:** 3 × 4 × 0.9 ÷ 2 ≈ **5.4**

### WS4 — Frontend Styling Simplification (owner: report 07)

- **What it is.** First a **direction decision** — the only indefensible option is the current hybrid: Tailwind installed since June at 0.0 % adoption while `_utilities.scss` maintains a parallel, also-unused utility set. Either (a) Tailwind-dominant as a package deal (theme generated from the 114 `--qd-*` tokens, logical-property utilities for RTL, semantic `qd-` layer retained, all ~10 policy-loop sites rewritten together — blast-radius lists R1–R5 are ready in 07 §8) or (b) qd-consolidation (remove Tailwind, promote the missing micro-layout vocabulary). Then: split UI_STYLE_SYSTEM.md (104 KB; §17 alone 58 %, §15 superseded-era 174 lines with live law embedded) into a routine-read contract and an on-demand archive; fold the ~8 Tailwind-mappable repeated blocks; adjudicate the 1,091 LOC of words-only "global" partials (a doc **tension**, not a contradiction — styles/README sanctions them).
- **Expected recurring benefit.** Most of the ~26k-token mandatory styling read per visual task; 400–900 component LOC of utility-replaceable duplication + ~60 LOC dead classes; one utility system instead of two; ends the doctrine loop that keeps reproducing 0 % adoption.
- **Risk.** Medium–high on path (a): RTL correctness is §29-protected and the codebase consistently uses logical properties — physical-property utilities would regress it. Low on the split/dedup-only path. The token layer is genuinely strong (zero hardcoded hex/px-font in 10,346 LOC) and must survive any direction.
- **Prerequisites.** The direction decision (user-level, design-register constrained by PRODUCT/DESIGN). WS2 and WS3 touch some of the same policy sites — sequence the doc rewrites together.
- **Effort.** Medium (split+dedup) to large (package deal). **Confidence.** High on measurements; medium on migration outcome.
- **Success criteria.** One utility system; UI_STYLE_SYSTEM routine path at target size; policy-loop grep finds zero contradictory styling law; measured Tailwind (or qd-utility) adoption in new templates > 0.
- **Score:** 4 × 4 × 0.7 ÷ 3 ≈ **3.7**

### WS5 — Test Rationalization (owner: report 02)

- **What it is.** MERGE/REWRITE the verified duplication pools while explicitly keeping runtime and protection: P1 parameterized shared harnesses for the words explorer-page quintet (stems↔lemmas specs ~84–85 % title-identical); P2 shared backend pipeline-invariant harnesses (RefusalForce ×7 = 795 LOC etc.) keeping per-pipeline instantiation and pipeline-specific edges; P3 rewrite of the 1,583-LOC explorer Logging quartet around the one genuine safety property (no Quran text / search terms in logs); P4 pilot-first importer-fixture skeleton (4,559 LOC pool); P5 the two giant page-spec rewrites.
- **Expected recurring benefit.** ~8,000–14,000 test LOC (7–12 % of the ~112k estate; LIKELY at ~8k) and a large drop in the marginal authoring cost of every future explorer page and pipeline. **Runtime is a non-goal** — suites are already fast; zero RUN_LESS_OFTEN and zero MOVE_TO_E2E proposals.
- **Risk.** Medium. Vacuous-pass risk in parameterized suites; P4 touches DB-safety infrastructure shared by 122 classes (pilot mandatory); the 472 aria/role assertions are §29 protection, not waste. Every DELETE candidate names replacement coverage.
- **Prerequisites.** None; P1 first as the lowest-risk highest-value slice. WS8's import-plumbing item sequences after this.
- **Effort.** Medium–large. **Confidence.** Medium-high (cluster LOC verified; net savings are authoring estimates).
- **Success criteria.** Per-lane runtime case counts unchanged or explicitly accounted; 268/268 TSV catalog still drift-free; the named protection-map classes untouched.
- **Score:** 4 × 3 × 0.7 ÷ 3 ≈ **2.8**

### WS6 — API Payload / Surface Cleanup (owner: report 09 + api-explorer)

- **What it is.** In evidence order: (1) the JSON-encoder/compression decision — Arabic serializes `\uXXXX`-escaped at ~6 bytes/char with no `AddJsonOptions` anywhere (LIKELY; wire measurement required first); (2) SHRINK_RESPONSE on `GET /api/access/audit-events` — five jsonb document fields fetched-never-rendered (projection change only; audit **storage** untouched, §29 risk analysis in 09 §5.4); (3) deprecate-and-observe the 2 consumer-less endpoints and 7 never-referenced models; (4) review the pageSize=1000 defaults (0.25–0.55 MB estimated responses) and the one lazy-load candidate.
- **Expected recurring benefit.** Plausibly 30–60 % payload bytes on Arabic-heavy hot paths (encoder/compression), 40–70 % of audit-event item bytes, ~260 backend + ~150–200 frontend/spec LOC of dead chains, 85→83 operations. The API is otherwise **clean**: zero Swagger drift, 70/85 endpoints KEEP, no duplicate calls, well-designed client caching.
- **Risk.** Medium, concentrated: audit area is §29-protected; anonymous GETs may have out-of-repo consumers (deprecation-plus-observation, never direct deletion); every payload figure is a static estimate until measured.
- **Prerequisites.** Runtime wire measurement of the hot endpoints (NEEDS_MEASUREMENT — the top gap in this area).
- **Effort.** Small–medium. **Confidence.** High on inventory, medium on byte savings until measured.
- **Success criteria.** Measured wire bytes before/after; `check-api-contract` green across the change; audit rows remain complete in the database; deprecated endpoints observed unused before removal.
- **Score:** 3 × 3 × 0.7 ÷ 3 ≈ **2.1**

### WS7 — Build / Gate Optimization (owner: report 10)

- **What it is.** Scope the dominant loop — the freshness rule's full boundary-lane re-run after any post-review fix (~3.4–9.5+ min per iteration) — to the fix's blast radius, keeping full re-runs for route/auth/migration/canonical triggers and the release composite as backstop; give `check-api-contract` a scheduled home (it is absent from TESTING_STRATEGY and its staleness incident is on record); measure the canonical/dump gates before debating their cadence.
- **Expected recurring benefit.** ~2–20 attended minutes per feature (assumption-dependent; gate-firing frequency is UNKNOWN — no CI, no run log), plus the much larger agent-side saving of ~17k–90k tokens per avoided review re-read (owned by WS1/WS3).
- **Risk.** Medium: scoped re-verification can miss cross-scope effects; the no-CI doctrine means every gate is discipline-enforced — nothing here may weaken a §29 gate (none is proposed).
- **Prerequisites.** A per-feature gate-invocation log over 2–3 features (the audit's highest-value follow-up measurement); canonical-data lane + create-smoke-dump timing.
- **Effort.** Small (matrix edits) once measured. **Confidence.** Medium.
- **Success criteria.** Updated trigger matrix in TESTING_STRATEGY; logged re-verification minutes per feature drop with zero regressions traced to scoping.
- **Score:** 3 × 4 × 0.6 ÷ 3 ≈ **2.4**

### WS8 — Architecture / LOC Cleanup (owner: report 08)

- **What it is.** The measured, bounded set: consolidate the words feature's 5× per-entity state machinery (prototype-first — net LOC after abstraction overhead is NEEDS_MEASUREMENT); delete the CONFIRMED-dead `type-distribution-list` component (4 files + its scss selector blocks); tidy the 21 stub scss files (jointly with WS4). **Explicitly rejected:** interface deletion (the 56 one-impl interfaces are the compile-time layer boundary; 19 are decorator seams in active use), handler flattening (the DI-resolved read-test strategy and typed Outcome contracts depend on the chain), cache removal (none wraps a trivial read).
- **Expected recurring benefit.** ~1,700–2,500 LOC / ~45–65 files, plus a cheaper sixth word-entity should one ever be added (~31 files/~3,800 LOC today). Architecture is **not** where the repo's size lives — this workstream is deliberately last-but-one.
- **Risk.** Medium: genericized state machinery can read worse than the duplication it replaces (a prior partial unification already stalled); the import-plumbing item borders §29 seams and sequences after WS5.
- **Prerequisites.** G1 prototype diff. WS5 decisions for anything touching importer tests.
- **Effort.** Medium. **Confidence.** Medium.
- **Success criteria.** Prototype-measured net LOC; feature lanes green; zero layer/boundary changes; dead-code deletion confirmed by build + words lane.
- **Score:** 2 × 2 × 0.7 ÷ 3 ≈ **0.9**

### WS9 — Memory & Config Hygiene (owner: report 05)

- **What it is.** Delete the tracked `.claude/settings.local.json.bak` (CONFIRMED stale lean-ctx leftover, carries permission entries that bypass current lane rules); run the §28-B Claude memory review over the 5 persistent-memory files (1 DELETE, 1 MERGE-with-residue-preserved, 2 KEEP + index); nothing else — no memory system exists in the repo.
- **Expected recurring benefit.** Tiny and honest about it: ~456 B tracked noise + ~2.3 KB stale memory text. This is record-correctness, not cost reduction.
- **Risk.** Minimal. The design-memory trim must preserve the machine-local inotify/ENOSPC caveat (non-derivable). Memory deletions only via the dedicated review (prompts in report 12).
- **Prerequisites.** Report 12's Claude-review prompt. **Effort.** Trivial. **Confidence.** High.
- **Success criteria.** `.bak` untracked via normal commit workflow; memory corpus matches the review's verdicts.
- **Score:** 1 × 2 × 0.9 ÷ 1 ≈ **1.8**

---

## 3. Recommended order and dependencies

```
WS1 stage 1 (routing)  ──────────────► WS1 stage 2 (doc slimming)
WS3 defect fixes (immediately, tiny) ─┐
WS2 README tail          ─────────────┼── coordinate the shared policy-site rewrites
WS4 direction decision → split/dedup ─┘
WS5 P1 → P2/P3 → P4 (pilot) → (unlocks WS8 import item)
WS6 after wire measurement
WS7 after gate-frequency log
WS8 after WS5 decisions + G1 prototype
WS9 anytime
```

Ranked by score: **WS1 (11.3) › WS3 (5.4) › WS2 (4.3) › WS4 (3.7) › WS5 (2.8) › WS7 (2.4) › WS6 (2.1) › WS9 (1.8) › WS8 (0.9)**.

Two items justify jumping the queue on safety grounds regardless of score: the **commit-workflow `.agents` pointer "sync-to-main" defect** (WS3) and the **font-weight conflict resolution direction** (WS2/WS4 — resolve toward the bundled 400/700 faces or explicitly change them, never silently).

## 4. Decisions only the user can make (gate multiple workstreams)

1. Is **Cursor** still a supported consumer? (gates WS1 scope; rule file maintained, usage UNKNOWN)
2. Is **Codex** supported forward? (activity CONFIRMED to 2026-08-07; gates WS1 target shape and WS3 adapter investment)
3. **Styling direction**: Tailwind-dominant package deal vs qd-consolidation? (gates WS4, parts of WS2/WS5)
4. Will the completed **importer pipelines ever rerun**? (gates WS5 P2/P4 depth and WS8's import item)
5. Does the **no-CI doctrine** stand? (gates WS7's shape; the audit takes no position without frequency data)

## 5. What this document is not

Per brief §26 and §33: this is not a master implementation plan, contains no remediation steps, and its workstreams await the independent Sol/Claude reviews (report 12) before any of them is turned into small plans.
