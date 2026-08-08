# 01 — Executive Summary

Audited baseline: branch `dev`, commit `72792ba9ff589c66aa25632a464b56b8bf7787af`, audit dates 2026-08-08/09.
Method: 4 phases (static inventory → solo runtime measurement → per-topic reports → independent adversarial verification of every report, with corrections applied). Every number below survived that verification. Token figures are bytes/4 static estimates (upper bounds on prescribed reading) unless stated otherwise.

This file answers the eighteen questions of brief §27 directly. Detail and evidence live in reports 02–10; workstream grouping in report 11.

---

### 1. Is the project genuinely over-engineered?

**No in code architecture; yes in knowledge surfaces.** The backend layer chain survived a hostile pass looking for pass-through ceremony: all 78 application-abstraction interfaces resolve, the 56 one-implementation interfaces are the compile-time Application/Infrastructure boundary, 19 of the 22 two-implementation ones are working cache/invalidation decorator seams, there is exactly one DTO between EF projection and the wire, and no cache wraps a trivial read (report 08, CONFIRMED). What is over-built is everything agents must *read*: byte-identical instruction mirrors, a 104 KB style guide mandated for any visual change, rules restated up to 16 times, review skills prescribing up to ~90k tokens, and a 97.8 KB feature README on the nearest-README path (reports 03, 04, 06, 07).

### 2. Where?

In descending measured weight: (a) instruction routing — mirrors + always-fire triggers (report 03); (b) review-skill context closures (04); (c) the README tail — 5 files hold 48 % of all README bytes (06); (d) styling doctrine — two utility systems, both unused for layout, 76 repeated declaration blocks, UI_STYLE_SYSTEM.md at 104 KB (07); (e) the words feature — 5× per-entity state machinery (~31 files/~3,800 LOC per entity) and 92 spec files/24.2k LOC with ~84 %-identical sibling suites (02, 08); (f) test duplication pools worth ~8–14k LOC (02).

### 3. What percentage of repository size is handwritten production code?

**27.8 %** — 113,155 of 407,221 tracked LOC (backend 56,934; frontend 56,221). (Report 08, recounted independently twice.)

### 4. What percentage is tests?

**27.5 %** — 111,921 LOC (canonical repo-wide figure: backend tests 55,808 + frontend specs 54,145 + e2e 1,968; +1,969 LOC of test-support seed/catalog files if included). Test-to-product ratio ≈ 1:1. (Reports 02, 08.)

### 5. What percentage is generated/support?

**~35 %.** EF Designer/ModelSnapshot 61,068 LOC (15.0 %); embedded morphology-correction JSON 53,367 (13.1 %); swagger.json 12,730 lines; package-lock 11,413; generated API client 2,605 (models only — services are deliberately pruned after every generation). Together with tests, non-product categories are **62.9 %** of the repo — which is why it "feels large" (Q59, report 08).

### 6. What percentage is migrations/docs/tooling?

Real migrations are small: 3,194 LOC across 27 migrations (the 61k Designer/snapshot LOC above is generated). Documentation markdown ≈ 12.7k LOC (3.1 %); agent scaffolding (.claude/.agents/.specify skills) ≈ 13.7k LOC (3.4 %); backend scripts ≈ 2.0k; historical reports ≈ 3.0k. (Reports 06, 08.)

### 7. Why are agents reading excessive context?

Four mechanisms, all CONFIRMED (report 03): (1) every root/Backend law edit is duplicated by hand into a byte-identical mirror (drifted and re-synced 3 times in 9 weeks); (2) unconditional broad triggers — UI_STYLE_SYSTEM.md (104 KB) is mandatory for *any* visual change, TESTING_STRATEGY (33 KB) rides along on review paths; (3) the nearest-README rule is healthy at the median (~2k tokens of real invariants) but pathological at the tail (abwab README alone is ~24k tokens); (4) review skills re-prescribe their full closure on every invocation and re-review. Net: a tiny frontend fix mandates ~73.4k tokens; routing-only corrections cut the six normal-task traces by ~42 % (~103k tokens) without deleting a single rule.

### 8. Why is Codex reading Claude instructions?

Because `AGENTS.md` *is* `CLAUDE.md` — born as one byte-identical pair in the spec-kit init commit (2026-06-06) and deliberately hand-mirrored since (commit messages state the policy), and the copied text routes all readers into `Backend/CLAUDE.md` / `Frontend/.../CLAUDE.md`. This directly contradicts the also-in-force Cursor rule "do not rely on CLAUDE.md". Only ~11.4 % of the root file is Claude-specific; ~81 % is agent-neutral law; no Codex-specific rule exists anywhere. Codex is not hypothetical: its CLI activity is CONFIRMED days before the baseline (refs/codex checkpoints, 2026-08-05→07 UTC). (Reports 03, 05.)

### 9. Are READMEs helping or hurting?

**Helping at the median, hurting at the tail.** 40 READMEs, 489,912 B, median 7,910 B; content is genuinely local invariants (~84–97 %), near-zero architecture repetition; 33 of 40 already match the target bounded-context model. The tail (5 files, 48 % of bytes) carries folded feature-narrative mass — the abwab README accrued ~72.5 KB *during* its feature series and is 73 % of the mandatory README bytes on a cross-stack abwab task. Candidate tail compression ≈ 70–100 KB with every safety contract preserved. The nearest-README rule itself should stay. (Report 06.)

### 10. Which custom Skills are most expensive in context?

engineering-review by far: ~17.1k-token unconditional floor, ~51.6k measured on a formal review, ~90k worst-case closure (UI_STYLE_SYSTEM alone is 29 % of it). Then the two performance skills (~19 KB and ~15.5 KB bodies; their doc pulls are optional) and test-guard (~30.4 KB with its references). The whole 10-skill system is 230 KB (~58k tokens). No skill is deletable — each carries at least one behavior existing nowhere else; ~15–20 KB is removable duplication. Two `.agents` adapter defects need fixing, one safety-relevant (commit-workflow pointer says "sync-to-main"; canon is sync-to-dev and `main` auto-deploys). (Report 04.)

### 11. Which test areas dominate runtime?

None pathologically — **runtime is a solved problem**: all 8 measured backend lanes total 357.7 s for 3,231 tests (slowest lanes: smoke 72.6 s, tier-b 65.9 s; fixed ~10–15 s container/VSTest startup dominates small lanes); frontend `test:full` 232.2 s for 2,964 tests. Zero failures anywhere. The cost that matters is maintenance: words specs are 24.2k LOC (45 % of frontend spec LOC), importer fixtures 4,559 LOC, and the two giant page specs 2,148/1,134 LOC. Unmeasured: e2e (needs live DB), canonical-data lane, backend pre-pr (estimated 5–7 min). (Reports 02, 10.)

### 12. Is frontend styling unnecessarily duplicated?

**Yes, in a specific way.** Tailwind has been installed and wired since June at **0.0 % adoption** (0 of 3,101 template class tokens), while `_utilities.scss` hand-maintains a parallel utility set whose layout classes also have 0 uses — two utility systems, neither used for layout. 76 declaration blocks repeat across 3+ files (~8 of the top 20 map 1:1 to utilities; ~12 are semantic patterns that should stay shared); 1,091 LOC of "global" styles serve only the words feature. The counterweight is real: the token layer is excellent (114 tokens, 1,349 var() uses, zero hardcoded hex/px-font anywhere) and must survive any direction. (Report 07.)

### 13. How much component SCSS appears avoidable?

Modest, honestly: ~400–900 LOC of utility-replaceable duplication, ~60 LOC of dead classes, 24 near-empty files (≤10 LOC), 21 ≤5-line stubs. 108 component files at median 52 LOC is *not* pathological — 78 % carry real scoped content; the separate-`.scss`-by-default rule gets a KEEP verdict. The bigger prize is the ~26k-token UI_STYLE_SYSTEM read per styling task (§17 is 58 % of the file; §15 is a 174-line superseded era with live law embedded). (Report 07.)

### 14. How many API fields/endpoints appear unnecessary or over-fetching?

Out of 85 operations: **2 endpoints** with no consumer anywhere in the frontend (deprecate-and-observe, never direct delete — out-of-repo consumers unprovable), **7 payload models** never referenced, **55 fields** classified unused-candidates, and **3 SHRINK_RESPONSE** endpoints. Flagship: `GET /api/access/audit-events` fetches five jsonb document fields the template never renders (projection-level fix; audit storage untouched — §29). Biggest single lever: Arabic JSON serializes `\uXXXX`-escaped ≈6 bytes/char with no encoder override anywhere → plausibly 30–60 % payload on hot paths (LIKELY; wire measurement required). Words lists default to pageSize=1000 (~0.25–0.55 MB estimated). Otherwise the API is clean: zero Swagger drift, no duplicate calls, deliberate models-only client. (Report 09 + api-explorer.)

### 15. Which architecture layers appear pass-through?

Structurally none worth removing. ~25 % of sampled handlers are pass-throughs but are the cheapest files in the repo (9–30 LOC) and load-bearing for the DI-resolved test strategy and typed Outcome contracts. The genuine finding is not a layer but a pattern: the words feature instantiates the same 7-file state machine per entity 5× (similarity 0.47–0.77), and ~30–50 % of handler LOC is logging ceremony pinned by 1,583 LOC of logging tests (policy question, not refactor). Realistic safe reduction: ~1,700–2,500 LOC — architecture is explicitly **not** the size lever. (Report 08.)

### 16. What must remain untouched because it protects safety?

The brief-§29 list, made concrete: the 33 authorization/security test classes (7,096 LOC), SmokeAbwabWriteTests' concurrency/stale-version assertions, AccessMigrationPathTests, the 10 source-gated canonical Quran-data classes and 22 source/hash/manifest classes, audit **storage** and its persistence tests, the no-Quran-text-in-logs property, the two-scheme auth model and UnsafeEndpointMetadataValidator startup gate, optimistic-concurrency xmin/409 semantics, the token/RTL/logical-property styling layer and Quran typography, canonical import gates and staged-resource preflights, and OpenAPI contract parity (`export-swagger` + `check-api-contract` — which needs a *stronger* scheduled home, not a weaker one). No recommendation in this audit weakens any of these; several add protection.

### 17. Roughly how much recurring feature cost could be removed?

By layer of the feature cost stack (all bounds, not promises): **agent context** — ~42 % of instructed mandatory reading via routing-only corrections (~103k tokens across the six traces; up to ~85 % for tiny tasks after doc slimming, NEEDS_MEASUREMENT); **review loop** — 5–8k tokens per formal review statically, ~17k–90k per avoided re-read iteration; **test authoring** — ~8–14k LOC one-time plus most of the ~1,100–1,600 spec-LOC marginal cost of each future explorer page; **attended gate time** — ~2–20 min/feature (frequency-dependent, unmeasured); **payload** — 30–60 % on Arabic hot paths pending wire measurement; **code** — ~1,700–2,500 LOC (08) + ~410–460 LOC dead chains (09). One-time doc removal: ~70–100 KB READMEs + ~15–20 KB skills + UI_STYLE_SYSTEM split.

### 18. What are the highest-priority cleanup workstreams?

Per report 11's scoring (Impact × Frequency × Confidence ÷ Risk): **WS1 Agent Instruction Routing** (score ≈11) first — it is the largest measured cost, the cheapest to fix, and deletes nothing; then **WS3 Skill Simplification** (≈5.4, includes two immediate safety-relevant pointer fixes), **WS2 README tail** (≈4.3), **WS4 Styling direction + split** (≈3.7), **WS5 Test rationalization** (≈2.8), **WS7 Gates** (≈2.4), **WS6 API payload** (≈2.1), **WS9 Hygiene** (≈1.8), **WS8 Architecture** (≈0.9 — deliberately last: the code is not the problem). Five user decisions gate several of them (Cursor, Codex, styling direction, importer reruns, no-CI stance — report 11 §4).

---

**Bottom line.** This is a young (9-week), unusually disciplined codebase — fast tests, drift-proof gate catalogs, clean API parity, a strong token system, working artifact lifecycle — that has been paying for that discipline with an ever-growing, heavily duplicated *reading* burden. The cheapest large win is to fix how knowledge is routed, not to remove what the knowledge protects.
