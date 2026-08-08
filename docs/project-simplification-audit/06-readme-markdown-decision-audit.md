# 06 — README / Markdown Decision Audit (Audit E) + Documentation Lifecycle Evaluation (§23)

- **Audited branch/commit:** `dev` @ `72792ba9` — audit date 2026-08-08
- **Brief sections covered:** §14 (Audit E — README / Markdown Decision Inventory), §23 (documentation lifecycle), mandatory questions 9–15
- **Input data:** `data/markdown-decision-inventory.json`, `data/history-evidence.json`, `data/instruction-inventory.json`
- **Spot-verification:** every headline number below (README count/size/median, decision count, the font conflict, TESTING_DEBT size/churn, contracts-index size, report leftovers, abwab README growth) was independently re-measured in the repo by this author before being asserted. Six READMEs were re-read in source, including the three the task names (abwab 97.8KB, scripts 26.2KB, styles 9.4KB).

This is an audit. It proposes and classifies. It contains no implementation steps.

---

## 1. Scope and method

The inventory scope is all tracked Markdown outside `.claude/`, `.agents/`, `.specify/`, `resources/`, and the audit folder itself (those are owned by reports 03–05): **69 files, 884,042 bytes (~221k tokens at bytes/4), 13,448 LOC** (`data/markdown-decision-inventory.json` "totals"). The full-repo Markdown surface including the excluded sets is 138 files / ~1,446KB (same file, "notes").

Three files alone are 28% of the included bytes: `UI_STYLE_SYSTEM.md` (104.0KB), the abwab feature README (97.8KB), and `SKILLS_AND_ARCHITECTURE_GUIDE.md` (42.7KB) — CONFIRMED (re-measured; see §2 and §5).

Method: the Phase-1 inventory agent's judgments were treated as claims, not facts. For each load-bearing claim I re-ran the measurement (`find`/`wc`/`git show`/`grep`) or re-read the cited lines. Where I could not independently confirm, the conclusion carries LIKELY or NEEDS_MEASUREMENT.

---

## 2. README inventory — the numbers (Q9, Q10)

Independently re-measured on 2026-08-08 (same exclusions as the inventory):

| Metric | Value | Tag |
|---|---|---|
| README.md files | **40** | CONFIRMED |
| Total bytes | **489,912 (~478KB, ~122k tokens)** | CONFIRMED |
| Median size | **7,910 B (~2.0k tokens)** | CONFIRMED |
| Mean size | **12,248 B (~3.1k tokens)** | CONFIRMED |
| Largest | abwab feature README, **97,772 B** — 2.30× the next largest | CONFIRMED |

The distribution is sharply skewed. The five largest files hold 48% of all README bytes in 12.5% of the files:

| # | README | Bytes | LOC |
|---|---|---|---|
| 1 | `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md` | 97,772 | 1,179 |
| 2 | `Frontend/quran-dashboard-ui/src/app/features/access-admin/README.md` | 42,594 | 520 |
| 3 | `Frontend/quran-dashboard-ui/src/app/features/words/README.md` | 37,786 | 427 |
| 4 | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/README.md` | 31,236 | 358 |
| 5 | `Backend/scripts/README.md` | 26,229 | 445 |

Below the tail, the population is healthy: 24–25 of 40 READMEs are under 10KB (24 below 10,000 B; 25 below 10,240 B), and the small ones (pipeline, access-layer, contract-owner READMEs) are near-pure invariant statements — spot-verified on `.../DataPipelines/Words/MorphologyImporting/README.md` (3.7KB: area boundary, two source shapes, key pieces, correction passes — nothing generic; CONFIRMED for that file, LIKELY for the class per the inventory's aggregate scan).

---

## 3. Unique invariants vs repetition vs history (Q11, Q12, Q13)

The inventory judges README content 85–97% "unique local invariants" with generic-architecture repetition at 1–3% and history at 0–15% (`markdown-decision-inventory.json` "readme_judgements"). My spot-verification **confirms the uniqueness percentages but forces a refinement**: "unique" is not one category. It splits into three registers with very different value-per-byte:

1. **Hard invariants and contracts** — URL contracts, concurrency contracts, identity keys, measured ratios, fail-closed rules. Irreplaceable; re-derivation is expensive and error-prone.
2. **Decision-rationale narrative** — why a behavior is what it is, which alternative was rejected, which slice decided it. Unique, but it is folded planning-artifact content wearing a README's clothes.
3. **Explicit history** — superseded eras, reversal records, deleted-artifact markers.

### 3.1 The 97.8KB abwab README — adjudication (the task's central case)

**Verdict: the content is genuinely unique (inventory's 85% figure is credible), but a large middle band of it is register-2 feature narrative that outgrew its home — CONFIRMED.**

Evidence:

- **It is self-declared as the successor of the deleted plans.** `features/abwab/README.md:1170-1174`: *"the feature's plans and the UX slice series that followed were swept per the planning-artifact lifecycle rule … **This file is the current record** — it is where a decision those plans made should be read from now."* This is not drift; it is the lifecycle working exactly as written — naming this file the series' sole surviving record.
- **The growth curve matches the feature's own docs commits, not the fold-then-delete events.** Re-measured via `git show` at every commit touching the file: **7,119 B at creation (2026-07-29) → 29,674 B by end of that day → 79,684 B by end of 2026-08-02** (on `dev` that day via PR #63) **→ 97,252 B by end of 2026-08-04 → 97,772 B at HEAD.** ~72.5KB accrued continuously across the `docs(ux-slice-*)`/`docs(abwab)` commits of the UX-slice series (07-29→08-02), days before the sweeps. The sweep day (2026-08-04 — lifecycle tightened `ae66c4da`, sweeps `a675286d`/`0d339472`) added ~16.7KB of mixed doc-truing and folds, of which the fold commit proper, `8b9f4c99` *"fold what the artifacts knew into the READMEs that outlive them"*, contributed only ~2.2KB — essentially the `:1133` reversal-record section itself; and `fdcc8ede` (2026-08-05) *"fold what only the reviews knew, then delete them"* did not touch this file at all (its 24 inserted lines went to the scripts README, the styles README, and TESTING_STRATEGY.md). The mass was written as the feature made its decisions; the folds deposited only the last few KB. CONFIRMED.
- **Register sample (lines 88–117):** the search-box section records not just the current behavior (tree marks, cards/archive filter, per-view empty-state wording) but the rejected alternative, the reason it "was a lie about the data", the 500ms `role="status"` debounce rationale, and a superseded sub-decision (*"supersedes ux-slice-l's accumulation decision"*). Each fact is unique; the register is a UX-review narrative.
- **The efficient counter-example is in the same file.** "Decisions that reversed mid-series" (`:1133-1169`) records four shipped-then-reversed decisions in 37 lines with symbol anchors and an explicit keep-rationale. That is the high-density form of register-2/3 content. The middle 1,000 lines do not hold themselves to that density.
- **Sections that are pure register-1 and must not be casually touched:** the URL contract (`:486` — per-key table, cross-key rules, fail-closed forms pinned to `abwab-url-sync.spec.ts`'s negative table), the Gotchas (`:657`), and the e2e-membership rule (`:1104-1110` — the glob *is* the membership test).

### 3.2 The other spot-verified files

| README | Spot-verified finding | Tag |
|---|---|---|
| `Backend/scripts/README.md` (26.2KB) | Operational command truth (DB rebuild sequence, smoke-dump pinning, test runner). Two sections are not steady-state: "Authorization activation and rollback (**prospective**)" (`:224-250` — explicitly *"a future runbook, not a completed rollout"*) and "Legacy Admin/Editor cleanup" (`:251-277` — a one-time Phase-10 operator sequence). ~50 lines of forward-looking runbook inside a current-truth file. | CONFIRMED |
| `Frontend/.../src/styles/README.md` (9.4KB) | High-value register-1: the nine measured WCAG contrast ratios pinned to token lines (`:110-127`, "measured, not derived… re-measure rather than adjust"), the breakpoint sync invariant, and the `.qd-badge` line-box coupling to the dashboard skeleton (`_components.scss:132-140` ↔ `dashboard-home.component.scss:40-48`). Nothing asserts the ratios — the file itself points at TESTING_DEBT row P2. This is exactly what a README should be. | CONFIRMED |
| `Backend/.../Persistence/Writes/Abwab/README.md` (31.2KB) | Dense register-1 concurrency contract (xmin in every UPDATE's WHERE via `IsRowVersion()`, which save path can raise which branch, section-create's single-INSERT narrowing — `README.md:36-45`). Inventory's 96%-unique judgment credible. Large but load-bearing. | CONFIRMED |
| `Backend/tests/QuranDashboard.Tests/README.md` (19.7KB) | Best-in-class large README: folder map, invariants, one measured why-section (postgres 18 vs 16). 97%-unique judgment credible. | CONFIRMED |
| `docs/README.md` (3.2KB) | Highest generic-repetition share (~48%) — by design; it is a router that re-points at CLAUDE.md/TESTING_STRATEGY/contracts. Tiny, so the repetition costs little. | CONFIRMED |
| `features/access-admin/README.md` (42.6KB) | Structure check only: contract-shaped headings (failure model, draft/revert, per-status semantics, boundaries), just 2 feature/slice markers. Closer to register-1 than abwab; the 95%-unique judgment is plausible. Full adjudication not performed. | LIKELY |

### 3.3 Which files mostly repeat architecture/instructions? (Q12)

Almost none — this failure mode is largely absent. CONFIRMED. The measured repetition shares are 1–3% for the large READMEs and 0–5% for the small ones; the only file above 10% is `docs/README.md` (a deliberate router) and `Backend/report/README.md` (30% history-as-rule). The real duplication problem in this repository is in the law files (CLAUDE/AGENTS twins — report 03's territory), not the READMEs.

### 3.4 Which contain historical/superseded material? (Q13)

The inventory's 12 historical sections were spot-verified at the top instances; the material clusters as:

| Location | Lines | Nature | Tag |
|---|---|---|---|
| `UI_STYLE_SYSTEM.md` §15 | 399–572 (~174 LOC) | Explicitly superseded navy+gold contract, kept as sole surviving extraction record (*"nothing else holds the prototype's values"* — `:414-415`); **but §15A typography and §15F motion remain in force inside the superseded section** (`:409-411`) | CONFIRMED |
| abwab README | :1133–1169 (37 LOC) | Deliberate reversal record with keep-rationale | CONFIRMED |
| `docs/TESTING_DEBT.md` | :20-23, :128-139, :219-234, :277-299 (~55 LOC) | Paid-row adjudication narratives retained after their rows were deleted | CONFIRMED |
| `Backend/scripts/README.md` | :224–277 (~50 LOC) | Prospective/one-time operator material (future-facing, not superseded) | CONFIRMED |
| words README | 27 marker lines; feature-numbered headings | Current contracts under historical framing | LIKELY |
| DESIGN.md §2 dark theme (:110-124), PRODUCT.md (:46-52), TESTING_STRATEGY.md (:17-29, :391-427), `Backend/report/README.md` (:28-40), `docs/README.md` (:18-21) | small | Transitional-by-decision or one-sentence history justifying a current rule — mostly earning its keep | CONFIRMED |

Total genuinely-historical or forward-runbook material across the live doc set is roughly 350–400 lines — real but modest. The larger cost is register-2 narrative, which is *not* historical and therefore never triggers the existing "superseded" cleanup instinct.

---

## 4. Decision inventory analysis — 111 decisions

Re-counted from the data file: **111 decision records** (the file's own prose note says "104 decisions captured" — a stale internal note; the array holds 111. CONFIRMED by count). Breakdown:

| Property | Count | Notes |
|---|---|---|
| With duplicate sources | 56 (50%) | Overwhelmingly *deliberate defer-topology*: law files state short policy and defer (TESTING_STRATEGY §1 names its six deferring entrypoints); copies annotated "(defers)" / "(canonical parent)" |
| With conflicts | **2** | One real inter-doc conflict (§4.1); one deliberate, ledgered code-vs-comment contradiction (TESTING_DEBT row C1, BulkMoveAsync ordering) |
| With stale evidence | 7 | Mostly self-aware (files that document their own removed content) |
| Provable from code | 21 yes / 21 partially / 69 no | The 69 are process/policy decisions — unprovable by nature, correctly owned by law files |
| Top sources | TESTING_STRATEGY (20), CLAUDE.md (19), DESIGN.md (9), TESTING_DEBT (9) | |

**The duplication topology is healthy by design — CONFIRMED.** Short policy in entrypoints, canonical detail in one named owner, copies that say they defer. The two exceptions that require manual synchronization and have already produced measured drift are (a) the CLAUDE/AGENTS twin files (3 root-pair drift→resync cycles in 2 months, each resync needing an audit finding — `history-evidence.json` "agents_claude_history"; owned by report 03) and (b) the DESIGN.md ↔ UI_STYLE_SYSTEM §16.3 allowed-green mirror, declared as a mirror that "must stay in sync" with no mechanism.

### 4.1 The one confirmed conflict: IBM Plex font weights

**CONFIRMED, and spot-verified all the way into the font files:**

- `DESIGN.md:190-194`: use weights **400/500/600/700** "where available — mid-weights (500/600) carry the nav, cards, labels…"
- `UI_STYLE_SYSTEM.md:428-434` (§15A — explicitly **still in force** per `:409-411`): use **400 and 700 only**; "no 500/600 (medium/semibold) faces exist. Do not use `font-weight: 500` or `600`" — a mid-weight declaration is a faux weight.
- **Code sides with §15A:** `public/fonts/` bundles exactly `*-regular.woff2` and `*-bold.woff2` for both faces — no 500/600 files exist (verified by `ls`).
- **And the conflict has already leaked into code:** two production stylesheets declare `font-weight: 600` — `abwab-relations-modal.component.scss:142` and `abwab-templates-page.component.scss:50` — silent faux weights that DESIGN.md's wording would bless and §15A forbids. CONFIRMED.

This is the textbook cost of a two-owner decision: the newer, more prominent document (DESIGN.md) states the losing rule, and code written under it is already drifting.

### 4.2 Staleness

No decision was found asserting something the code disproves (the C1 contradiction is deliberately ledgered as a code question). The staleness that exists is *risk*, not falsity: DESIGN.md:62-64's "verified WCAG AA" claim is measurement-era truth with no asserting test (TESTING_DEBT row P2 is the owed guard). CONFIRMED.

---

## 5. Classification

Taxonomy per brief §14: `KEEP / SHORTEN / MERGE / DELETE_CANDIDATE / HISTORICAL_ONLY / MOVE_TO_CANONICAL_SOURCE / ON_DEMAND_ONLY`. Every non-KEEP row's seven-question analysis (brief §4) follows in §5.1, except the `SKILLS_AND_ARCHITECTURE_GUIDE.md` ON_DEMAND_ONLY row, whose seven-question analysis is owned by reports 03/04 (as its row states).

| Target | Classification | Tag |
|---|---|---|
| abwab feature README (97.8KB) | **SHORTEN** (register-1 contracts stay; register-2 narrative compressed to the density of its own `:1133` reversal-record section) | CONFIRMED problem, LIKELY sizing |
| access-admin README (42.6KB) | **KEEP**, re-adjudicate for SHORTEN only after a full read — structure suggests contract-register | LIKELY |
| words README (37.8KB) | **SHORTEN** (drop feature-number framing, compress the 15% historical band) | LIKELY |
| Writes/Abwab README (31.2KB) | **KEEP** (dense safety contract: concurrency/409 semantics) | CONFIRMED |
| scripts README core (~20KB) | **KEEP** (operational truth, read on-demand by nature) | CONFIRMED |
| scripts README `:224-277` (activation runbook + legacy cleanup) | **ON_DEMAND_ONLY** (future/one-time operator material; does not belong in the every-touch read path of the folder) | CONFIRMED |
| styles README (9.4KB) | **KEEP**; contrast table → **MOVE_TO_CANONICAL_SOURCE** *only when* the P2 test lands (test becomes the assertion; README keeps the pointer) | CONFIRMED |
| tests README (19.7KB) | **KEEP** (the model the tail should converge to) | CONFIRMED |
| the 33 remaining READMEs (aggregate ~220–225KB; 29 smallest ≈ 152KB) | **KEEP** | CONFIRMED for spot-checked members, LIKELY for the class |
| `docs/README.md`, `specs/README.md`, `Backend/report/README.md` | **KEEP** (routers/charters, tiny) | CONFIRMED |
| `docs/contracts/**` (9 files, 13.4KB) | **KEEP**; `security-access.md:20-38` → **SHORTEN** (mild charter drift: it names response fields the index elsewhere refuses to restate) | CONFIRMED |
| `UI_STYLE_SYSTEM.md` §15 (:399-572) | **HISTORICAL_ONLY** — already marked superseded, but the still-in-force §15A/§15F rules are trapped inside it; smallest safe step is extracting the live rules so the block becomes purely historical | CONFIRMED |
| DESIGN.md font-weight sentence (:190-194) | **MOVE_TO_CANONICAL_SOURCE** — one owner for the weight rule; code proves the 400/700 side | CONFIRMED |
| DESIGN.md ↔ UI_STYLE_SYSTEM §16.3 allowed-green mirror | **MERGE** (one authoritative list + pointer, replacing the declared manual mirror) | CONFIRMED |
| TESTING_DEBT paid-row narratives (~55 lines) | **DELETE_CANDIDATE** (git history already holds the payoff story; the file's own delete-when-paid policy argues for it) | CONFIRMED |
| TESTING_DEBT open rows (33) + intro negative-space rules | **KEEP** | CONFIRMED |
| `Backend/report/feature-008/-009` import reports | **KEEP** (chartered exception with a named expiry trigger — see §7.4) | CONFIRMED |
| `SKILLS_AND_ARCHITECTURE_GUIDE.md` (42.7KB) | **ON_DEMAND_ONLY** — referenced by no entrypoint on any task path (`instruction-inventory.json` notes); primary ownership: reports 03/04 | LIKELY |

### 5.1 Seven-question analysis for each proposed change

**A. SHORTEN the abwab README (and, pattern-wise, words README).**
1. *Value today:* sole record of the abwab UX decision series and its per-key URL/interaction contracts; self-declared successor of the deleted plans.
2. *Dependents:* the nearest-README rule (root `CLAUDE.md` §Local README Context, Frontend `CLAUDE.md` §Frontend Local READMEs) makes it a mandatory read for every abwab frontend task; `docs/contracts/abwab.md` points at it; TESTING_DEBT rows reference it.
3. *Risk:* folding/compressing register-2 rationale wrongly loses reversal knowledge — the exact failure the lifecycle rule warns about ("folding wrong is worse than deleting"). A decision whose rationale is deleted gets re-derived and re-shipped wrong.
4. *Equivalent protection elsewhere:* partial — behavioral contracts are pinned by specs (`abwab-url-sync.spec.ts` negative table, e2e flows); rationale is protected nowhere else. So the *contracts* can lean on tests; the *reversals* cannot.
5. *Smallest safe step:* not deletion — **re-registering**: keep every register-1 contract and the reversal-record section verbatim; compress register-2 narrative to the reversal-record density (behavior + one-line why + symbol anchor), section by section, with the engineering review as the gate.
6. *Verification later:* README byte count and section map; every kept invariant still provable from a named `file:LINE` or spec; no inbound reference (contracts index, TESTING_DEBT, other READMEs) dangling.
7. *Recurring cost removed:* the abwab README is ~24.4k tokens read on **every** abwab frontend task (33% of the tiny-frontend-fix trace's mandatory bytes; the abwab cross-stack trace reads ~175KB of README, ~44k tokens — `instruction-inventory.json` task traces). Halving the file removes ~12k tokens per abwab task, the single largest README saving available.

**B. ON_DEMAND_ONLY for scripts README `:224-277`.**
1. *Value:* a real future runbook (authorization activation/rollback) and a one-time operator sequence — genuinely load-bearing *when that day comes*.
2. *Dependents:* future activation operator; no current task path needs it.
3. *Risk:* losing the runbook before activation would be costly; it must remain findable.
4. *Equivalent protection:* none elsewhere — this is its only home.
5. *Smallest safe step:* mark/relocate within the documentation system so it is not part of the folder's every-touch read (the lifecycle's closed list constrains where it may live; that constraint is itself evaluated in §7.5).
6. *Verification:* the runbook text still reachable from `docs/contracts/security-access.md` or the scripts README head.
7. *Recurring cost removed:* small (~1.5k tokens per scripts-touching task) — this is hygiene, not headline.

**C. Resolve the font-weight decision to one owner (MOVE_TO_CANONICAL_SOURCE) and MERGE the allowed-green mirror.**
1. *Value:* both documents are live design law; the weight rule prevents faux-weight CSS.
2. *Dependents:* all frontend styling work; the two existing `font-weight: 600` declarations prove active reliance on the *wrong* copy.
3. *Risk:* minimal — this removes a contradiction; the only decision needed is which rule wins, and the bundled fonts already answer it (400/700 only) unless the user chooses to bundle mid-weights instead.
4. *Equivalent protection:* none — no test asserts weight discipline (a trivial grep-style guard is conceivable; none exists).
5. *Smallest safe step:* one document owns the rule, the other points; same for the green list.
6. *Verification:* grep for `font-weight: 5|font-weight: 6` in `src/`; single authoritative statement remaining.
7. *Recurring cost removed:* one manual-sync obligation and one class of silent styling drift.

**D. DELETE_CANDIDATE: TESTING_DEBT paid-row narratives (~55 lines).**
1. *Value:* they explain why closed rows closed — comfort, not contract.
2. *Dependents:* none found; open rows never reference the paid narratives.
3. *Risk:* near-zero; the same story exists in the payoff commits (e.g. `b9acdd45` "pay mandatory smoke debt").
4. *Equivalent protection:* git history, which the file's own policy already designates as where paid rows go.
5. *Smallest safe step:* remove the four narrative bands; keep the intro's negative-space rules (`:10-16`), which are load-bearing.
6. *Verification:* file shrinks ~15%; every remaining line is either policy or an open row.
7. *Recurring cost removed:* modest tokens, but it arrests the observed pattern (sections only ever added — 14 added, 0 removed; `history-evidence.json` "testing_debt").

**E. HISTORICAL_ONLY completion for UI_STYLE_SYSTEM §15.** (Primary owner: report 07; recorded here because it is the largest superseded block in the decision inventory.) Value: sole surviving prototype extraction record. Dependents: §15A/§15F are *live rules* trapped inside a superseded section — any reader skipping "superseded" content skips live law. Risk of extraction: low; the section header itself enumerates which sub-parts remain in force (`:409-411`). Smallest step: pull the live rules out; leave the historical record intact and clearly dead. Verification: no in-force rule remains inside a section marked superseded. Recurring cost removed: reader ambiguity in a 104KB file that is mandatory for any visual change.

**F. SHORTEN `docs/contracts/security-access.md:20-38`.**
1. *Value today:* the access area's index page — it routes to the authoritative access READMEs; the drifting lines restate concrete response fields (`{items, assignmentReady}`, audit-event name fields).
2. *Dependents:* the authorization trace reads it conditionally; nothing was found depending on the field names being restated *in the index* — the owning access READMEs and the API contract itself carry them.
3. *Risk:* near-zero — the charter already says the index never restates content; removing the restatement removes a manual-sync channel, not information.
4. *Equivalent protection elsewhere:* yes — the fields live in the access-area READMEs and the API surface (report 09's territory).
5. *Smallest safe step:* replace the `:20-38` field restatement with the pointer form the other eight pages already use.
6. *Verification later:* the page shrinks toward the 1.5KB index average; no response-field names remain in `docs/contracts/`; no inbound reference dangles.
7. *Recurring cost removed:* small (a fraction of the 3.1KB page per conditional read) plus one drift channel closed — hygiene, not headline.

**G. MOVE_TO_CANONICAL_SOURCE for the styles-README contrast table (conditional on the P2 test).**
1. *Value today:* the nine measured WCAG ratios are the only record asserting the palette's compliance ("measured, not derived… re-measure rather than adjust").
2. *Dependents:* frontend styling work; DESIGN.md's "verified WCAG AA" claim (§4.2) rests on these measurements.
3. *Risk:* moving or shrinking the table before the P2 test exists would leave the ratios asserted nowhere — which is why the classification is conditional; until P2 lands the table is KEEP.
4. *Equivalent protection elsewhere:* none today; TESTING_DEBT row P2 is the designated future assertion (the README itself points at it).
5. *Smallest safe step:* land P2, make the test the canonical assertion, reduce the table to a pointer plus the re-measure-not-adjust rule.
6. *Verification later:* P2 green; no ratio stated in two places; the README keeps the pointer.
7. *Recurring cost removed:* minimal bytes — the gain is replacing a manually-synced measurement record with an asserting test.

---

## 6. The nearest-README question, head-on (Q14)

> Is "read nearest README before touching an area" reducing discovery cost, or increasing total context cost?

**Answer: beneficial at the median, pathological at the tail — CONFIRMED by the numbers, not just plausible.**

The evidence, from `instruction-inventory.json` task traces cross-checked against re-measured file sizes:

| Task trace | Mandatory read total | Nearest-README share | Judgment |
|---|---|---|---|
| tiny-backend-bug-fix | 98,043 B (~24.5k tok) | Reads/Abwab 17.5KB + tests README 19.7KB ≈ **38%** | READMEs are the useful part; the burden here is TESTING_STRATEGY (33.4KB) |
| tiny-frontend-ui-fix | 293,528 B (~73.4k tok) | abwab README 97.8KB = **33%** (UI_STYLE_SYSTEM is another 35%) | One README is a third of a "tiny" fix |
| abwab-change (cross-stack) | 239,860 B (~60k tok) | five READMEs ≈ 175KB = **73%** (~44k tokens) | The rule fans out across every touched layer |
| authorization-change | 98,538 B (~24.6k tok) | 3–5 access READMEs, all small-to-mid | Healthy: many small contract READMEs, none dominant |

Why the rule earns its keep at the median: a 7.9KB median README costs ~2k tokens and delivers area-local, otherwise-underivable facts — xmin/409 semantics, identity-key normalization, e2e membership globs, measured contrast ratios. Re-deriving any one of those costs more than the read, and getting them wrong breaks documented invariants (several sit in brief-§29 protected areas: optimistic concurrency, Quran typography, authorization). The authorization trace shows the rule at its best: four small-to-mid layer READMEs totaling ~38KB — less than any large architecture doc.

Why it fails at the tail: the rule is unconditional and size-blind. The abwab frontend task pays 97.8KB for one README — more than `TESTING_STRATEGY.md` and approaching `UI_STYLE_SYSTEM.md` — because a whole UX-slice series wrote its decision record into the file the rule makes mandatory, and the lifecycle then made that file the series' sole surviving record. The repository has size thresholds for component files (`FRONTEND_STRUCTURE.md`: 150/200/300) and for backend files (`BACKEND_STRUCTURE.md`: split review at 600), **but no size discipline of any kind for READMEs** — the only long-lived documents whose reading is mandated per-task. UNKNOWN whether real agent runs read the full file or partial-read it (Read offsets); the trace totals are upper bounds for full reads — NEEDS_MEASUREMENT.

**Conclusion: keep the rule; bound the artifact.** The failure is not "read the nearest README" — it is that nothing stops the nearest README from becoming a 24k-token narrative. The future model in §8 addresses this.

---

## 7. Lifecycle evaluation (brief §23)

### 7.1 Does deleting plans reduce clutter without losing important decisions? — Yes on both halves, with one measured side-effect. CONFIRMED.

The lifecycle *runs as written*: 13 deletion commits (2026-07-04 → 2026-08-08); after the two bulk catch-up sweeps (138 and 73 files), deletions became small and per-feature (1–4 files), and the last four features each got their deletion commit within a day of closing (`history-evidence.json` "lifecycle_health"). The current tree is clean: `specs/` holds only its README, zero `docs/feature-*` folders, `Backend/report/` holds only its README plus the two chartered exceptions. No evidence of lost decisions was found — the fold gate appears to have been applied (the abwab README's reversal record is exactly a fold that preserved otherwise-unrecoverable knowledge).

### 7.2 Are too many decisions being pushed into READMEs / are READMEs becoming oversized historical narratives? — Partially; the growth is measured, and its driver is the feature's own decision-recording, not the fold events. CONFIRMED for abwab; LIKELY as the systemic trend.

The precise finding: READMEs are **not** becoming *historical* narratives (historical share is 0–15%; §3.4). They are becoming **decision-rationale** narratives. The documentation system gives a feature's decision record exactly one long-lived destination — the nearest README (the fold-then-delete pipeline then makes that file the deleted plans' sole successor) — and the abwab case shows what happens when a feature runs a long UX-review series: **+90.7KB into one README in 10 days — ~72.5KB written continuously by the series' own docs commits as each slice recorded its decisions, ~16.7KB on the 08-04 sweep day, and only ~2.2KB in the explicit fold commit `8b9f4c99` (`fdcc8ede` folded into other files)** (§3.1). The fold commits themselves were compact and high-quality; the mass arrived via the same-change README-update discipline during the feature. One more data point: the lifecycle rule requires folded facts to be "provable from code with a file:LINE", which register-1 content satisfies — but much of what this README accumulated (and what the fold gate admits) is *rationale*, which is unprovable by nature; the rule's own fold gate ("fact not recoverable from code") admits it anyway. The gate filters what to keep correctly; it imposes no form on how compactly to keep it — and no rule bounds what the feature itself writes in along the way.

### 7.3 Is `docs/contracts/` genuinely a thin index or another layer to traverse? — Genuinely thin. CONFIRMED.

Re-measured: 9 files, **13,434 bytes**, 192 LOC, average 1.5KB/page. Pointer-to-prose ratio is honest (62 pointer lines; the 80 "prose" lines are mostly scope/precedence statements). One mild drift, spot-verified: `security-access.md:20-38` names concrete response fields (`{items, assignmentReady}`, audit-event name fields) — content the charter says the index never restates. It is the largest page (3.1KB) and the only one drifting. As a layer it costs one small read and pays for itself by naming the authoritative README — the authorization trace uses it exactly as designed.

### 7.4 Are the `Backend/report/` leftovers lifecycle drift? — No. Deliberate, documented, triple-evidenced exception. CONFIRMED.

The 008/009 import-report folders survive because: (a) `f5abff7a`'s commit body explicitly charters them ("sole record of source verification, exclusions, and per-source hashes"); (b) `DataImporterDefaults.cs:23`/`:71` hardcode both directories as importer output targets; (c) `Backend/report/README.md:47` charters them with a named expiry — TESTING_DEBT row C5: the canonical smoke dump pins only the five morphology baseline tables, so translation/navigation counts currently have no test to live in; *"when it lands, these files go."* This is the lifecycle's "evidence worth keeping becomes a test" rule operating with a documented IOU, not an enforcement gap. Normal features demonstrably do get deletion commits (`1bf46b00`, 2026-08-08).

### 7.5 Should some long-lived decisions live in a small number of canonical policy files instead? — For two classes, yes. LIKELY.

- **Cross-cutting design law** already has canonical files (DESIGN.md, UI_STYLE_SYSTEM) — the problem there is two-owner rules (§4.1), solvable inside the existing structure.
- **Operator runbooks** (scripts README `:224-277`) have no sanctioned home: the closed survivor list (`CLAUDE.md` §Workspace Path Conventions) forces anything long-lived into a README, an architecture doc, or law. A future-facing runbook is none of those. This is a genuine, if small, gap in the lifecycle's taxonomy.
- **Feature UX rationale** is the big class. Today it has exactly one home (the feature README). Whether it deserves a second sanctioned home is a design decision for the remediation phase; the audit's finding is only that the current single-destination rule measurably bloats the mandatory read path (§6).

### 7.6 Does `TESTING_DEBT.md` remain useful, or too large/noisy? — Useful; high signal; degrading row readability. CONFIRMED (judgment LIKELY on trend).

Re-measured: 302 lines, 14 dated sections, 33 open rows, 41.8KB; 32 commits in its 10 days of life (~3/day — it is genuinely *live*, and its churn is also a recurring write cost). Signal: all 33 rows name a concrete file and a concrete paying trigger; the delete-when-paid policy is followed for rows (74 added / 41 removed across history). Noise: (a) ~55 lines of paid-row narrative that outlived their rows (§5.1-D); (b) single rows growing to 1.2–2.8KB (re-measured: row AC2 = 2,770 bytes; A1 = 1,513; P2 = 1,484) — the trigger drowns in qualification; (c) sections only ever append (14 added, 0 removed), so the file's structure grows monotonically even as rows are paid. The intro's negative-space rules (`:10-16`) are load-bearing and must survive any cleanup.

---

## 8. Future model evaluation (brief §14): READMEs only at bounded-context boundaries, containing what-it-does / unique invariants / important contracts / where-to-start

**Verdict: the repository is already ~85% of the way there, by file count. The model's binding constraint is a size/register discipline for the tail, not a relocation of the population.** CONFIRMED for the population shape; LIKELY for the savings estimate.

- The 33 remaining READMEs already match the model (near-pure invariant statements at bounded areas — pipelines, access layers, auth, fonts). The contracts index already provides the "where is the authoritative README" routing.
- The tests README (19.7KB, 97% unique, zero narrative) is the demonstrated ceiling for how large a README can be while staying pure-contract. The access-admin README suggests contract-register is sustainable even at 42KB — size alone is not the defect; register is.
- Applying the model to the tail (abwab to contract-register with reversal-record density; words de-framed; scripts runbook re-homed; TESTING_DEBT de-narrated) plausibly removes **~70–100KB (~18–25k tokens) from the mandatory-read pool**. The spread is the abwab compression assumption: halving it (§5.1-A.7) contributes ~49KB for a ~75KB total; compressing it toward ~25KB contributes ~73KB for a ~100KB total, at which point the abwab task's README burden drops by roughly 40–50%. The README-only share (excluding TESTING_DEBT, which is not a README) is ~68–92KB ≈ 14–19% of all README bytes. These figures are sizing estimates, not measurements: LIKELY, and each file needs its own fold-gate pass at remediation time.
- What the model must **not** do (brief §29 guard): thin out the Writes/Abwab concurrency contract, the auth README's two-scheme/`MapInboundClaims` facts, the styles contrast table (until the P2 test exists), word identity keys, or the e2e membership rules. Every one of these is protection for a listed high-risk area, and several exist *only* in their README.

---

## Mandatory questions answered

| # | Question | Answer |
|---|---|---|
| **9** | How many READMEs exist? | **40** `README.md` files in the audited scope (excludes `.claude/`, `.agents/`, `.specify/`, `resources/`, audit folder). CONFIRMED by independent `find`+`wc` re-measurement. |
| **10** | Total/median/average size? | **Total 489,912 B (~478KB, ~122k tokens); median 7,910 B; mean 12,248 B.** Skew: top 5 files hold 48% of bytes; largest (abwab, 97,772 B) is 2.30× the second largest. CONFIRMED. |
| **11** | Which contain unique invariants? | Nearly all — measured 84–97% unique across the population, spot-confirmed on six files. Highest-value register-1 examples: Writes/Abwab (xmin/409 contract), api/Authentication (two JWT schemes, `MapInboundClaims=false`), styles README (measured contrast table, breakpoint sync), Words reads README (identity keys/ordering-as-contract), tests README (fixture/DB invariants), abwab README's URL-contract and Gotchas sections. CONFIRMED. |
| **12** | Which mostly repeat architecture/instructions? | Effectively none. Only `docs/README.md` (~48%, a deliberate 3.2KB router) and `Backend/report/README.md` (30% history-as-rule, 4.2KB) have material repetition shares; large READMEs measure 1–3%. The repository's duplication problem lives in the law-file twins, not the READMEs. CONFIRMED. |
| **13** | Which contain historical/superseded material? | `UI_STYLE_SYSTEM.md` §15 (174 lines, marked superseded, with live §15A/§15F trapped inside); TESTING_DEBT paid-row narratives (~55 lines); abwab README reversal record (37 lines, deliberate and efficient); words README (~15% historical framing); scripts README :224-277 (prospective, not superseded); small earned-history markers in DESIGN/PRODUCT/TESTING_STRATEGY/report-README/docs-README. Total ≈ 350–400 lines — modest; the larger burden is non-historical decision narrative. CONFIRMED. |
| **14** | Is nearest-README reading beneficial overall? | **Yes at the median, no at the tail.** Median README ≈ 2k tokens of otherwise-underivable local invariants — cheaper than re-derivation and protective of §29 areas (authorization trace: several small READMEs, all earning their read). Tail: one README costs an abwab frontend task ~24.4k tokens (33% of its mandatory reads; the cross-stack abwab trace reads ~44k tokens of README = 73% of its mandatory bytes), a direct product of a feature's decision record having exactly one destination — the nearest README, written continuously during the feature and made the sole surviving record by the lifecycle — with no size/register discipline anywhere. Keep the rule; bound the artifact. CONFIRMED (trace numbers), NEEDS_MEASUREMENT (whether real runs full-read vs partial-read). |
| **15** | Which docs should become on-demand only? | From this report's scope: `SKILLS_AND_ARCHITECTURE_GUIDE.md` (42.7KB — already referenced by no task-path entrypoint; LIKELY), scripts README's activation/cleanup runbook sections (:224-277; CONFIRMED), `docs/TESTING_DEBT.md` (already effectively on-demand — on no mandatory trace path; keep it so; CONFIRMED), and the historical blocks inside `UI_STYLE_SYSTEM.md` §15 once live rules are extracted (CONFIRMED). The contracts index should *stay* on-demand routing and not grow content. Feature READMEs cannot become on-demand while the nearest-README rule stands — for them the lever is size/register (Q14), not routing. |

---

## Measurement gaps

1. **Actual read behavior vs file size.** All context-cost figures assume a full read of each instructed file. Agents may partial-read large READMEs via offsets; no telemetry exists either way. The tail-cost conclusion (Q14) is robust to this (the file must still be navigated), but the token figures are upper bounds. NEEDS_MEASUREMENT.
2. **Access-admin and words READMEs were not line-by-line adjudicated.** Structure and marker-density checks only; their SHORTEN/KEEP classifications carry LIKELY and need the same treatment the abwab README received here before any remediation. NEEDS_MEASUREMENT.
3. **The 33-remaining-README aggregate judgment** (93% unique) rests on the inventory agent's heuristic scan plus two spot checks by this author. LIKELY, not CONFIRMED, for unspot-checked members.
4. **Savings estimate (~70–100KB)** is a sizing judgment from observed register density (the reversal-record section as the compression benchmark), not a performed rewrite. LIKELY.
5. **Whether any decision exists only in git-history commit bodies** (i.e., was deleted without folding) was not exhaustively audited; the lifecycle's fold commits sampled here (fdcc8ede, 6e140759) all folded before deleting, and no orphaned reference was found, but a full deleted-artifact → fold-target reconciliation was out of scope. UNKNOWN.
6. **The inventory data file self-reports "104 decisions" in its notes while its array holds 111** — an internal inconsistency in the Phase-1 artifact (the 111 count is what this report verified and uses). Flagged for the Phase-3 adversarial pass. CONFIRMED (the discrepancy itself).
