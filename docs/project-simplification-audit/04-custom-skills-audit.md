# 04 — Custom Skills Audit (Brief §11 + §12)

- **Branch:** `dev` · **HEAD:** `72792ba9` · **Audit date:** 2026-08-08
- **Scope:** the 10 project-authored, non-Spec-Kit skills under `.claude/skills/` named in brief §11, their reference packs, and their `.agents/skills/` adapters. Spec Kit (`speckit-*`, `.specify/`) is measured for size only (§8 below), per brief §31.
- **Inputs:** `data/skill-inventory.json` (full read of all 10 skills + references), `data/instruction-inventory.json` (routing/task-trace context), `data/workflow-gate-inventory.json` (skills as workflow gates).
- **Independent verification performed by this author:** full re-read of `.claude/skills/engineering-review/SKILL.md` (556 lines), `.claude/skills/test-guard/SKILL.md` (168 lines), `.claude/skills/performance-backend-review/SKILL.md` (lines 1–120), `.claude/skills/commit-workflow/SKILL.md` (lines 110–206), `.claude/skills/pr-context-prep/SKILL.md` (lines 90–129), `.claude/skills/backend-structure-review/SKILL.md` (lines 1–45, 160–174); `stat`/`wc` re-measurement of all 10 bodies, all 10 adapters, both reference packs, `TESTING_STRATEGY.md`, `CODING_PRINCIPLES.md`, `UI_STYLE_SYSTEM.md`; repo-wide `grep` for the two orphan-candidate rules; full read of 4 `.agents` adapters (engineering-review, commit-workflow, test-guard, performance-angular-review); byte-count re-verification of the speckit trees. Every re-measured number matched the inventory exactly.
- Token figures are the audit-standard bytes/4 approximation throughout.

---

## 1. Headline numbers

| Aggregate | Bytes | LOC | ~Tokens | Tag |
|---|---:|---:|---:|---|
| 10 skill bodies (`SKILL.md`) | 129,628 | 2,482 | 32,407 | CONFIRMED (re-measured) |
| Reference packs (13 files, 2 skills own them) | 88,671 | 1,751 | 22,167 | CONFIRMED |
| `.agents/` adapters for the 10 skills | 12,021 | 218 | 3,005 | CONFIRMED |
| **Grand total, 10-skill system** | **230,320** | — | **57,580** | CONFIRMED |
| engineering-review worst-case context closure (24 files) | 359,926 | — | 89,981 | CONFIRMED (§4.1) |
| engineering-review always-read floor (4 files) | 68,411 | — | 17,102 | CONFIRMED (§4.2) |
| speckit (out of redesign scope): `.claude` 15 skills + `.agents` 9 copies + `.specify/` | 406,252 | — | 101,563 | CONFIRMED (§8) |

Two context observations frame everything below:

1. **The skill system is not uniformly heavy.** Nine of ten bodies are 6.7–19 KB, and eight of the ten own no reference pack. The cost concentrates in one skill (engineering-review) and in what skills *route into* — `TESTING_STRATEGY.md` (33,427 B) and, for frontend reviews, `UI_STYLE_SYSTEM.md` (103,970 B). The routed documents, not the skill bodies, are the biggest single lever, and they belong to the instruction/doc audits (reports 03/06/07). CONFIRMED.
2. **The duplication pattern is consistent and self-aware.** Most duplicated blocks *cite* their canonical source ("read it there rather than from any restatement, including this one" — `.claude/skills/engineering-review/SKILL.md:384-385`). The system knows it duplicates; it duplicates anyway, three times in one case (§4.6). CONFIRMED.

---

## 2. Per-skill inventory (the 12 questions of brief §11)

### 2.1 Size and reading-behavior table (questions 1–6)

| Skill | Purpose (Q1) | Trigger (Q2) | Body LOC / B (Q3) | Ref LOC / B (Q4) | Auto-reads (Q5) | Conditional reads (Q6) |
|---|---|---|---:|---:|---|---|
| engineering-review | Review-only holistic code review, both stacks; findings + PASS/CHANGES-REQUESTED verdict; Spec Kit compliance; delegates test code to test-guard | Catch-all: any review/diff/PR/change-quality request, "even if they don't say the word review" (SKILL.md:4-15) | 556 / 26,670 | 1,440 / 68,660 (clean-code pack 7 files + quran-data-safety + SPEC_KIT review) | `CODING_PRINCIPLES.md` (:52); `TESTING_STRATEGY.md` (:53-54, effectively always — Verification Check is a mandatory output section); `quran-data-safety.md` (:320-325, "apply in full", mandatory output section 7) | Clean-code pack per-finding/thorough pass (:56-67); test-guard SKILL + up to 3 refs when diff has tests (:75-88); 3 backend `.architecture` docs if Backend changed (:96-102); 3 frontend `.architecture` docs + `PRODUCT.md` + `DESIGN.md` if Frontend/UI changed (:104-114); SPEC_KIT_IMPLEMENTATION_REVIEW + `specs/<feature>/*` on Spec Kit markers (:118-144) |
| backend-structure-review | Review-only backend structure/layering/placement/threshold check with structured verdict | Structure questions, "or when new backend folders or files are added, even if they don't say the word structure" (SKILL.md:9-10) | 174 / 7,257 | — | `CODING_PRINCIPLES.md`, `BACKEND_STRUCTURE.md` (:35-37) | `CLEAN_ARCHITECTURE.md` (layering in question), `API_GUIDELINES.md` (API boundary) (:38-43) |
| commit-workflow | Plans/executes safe staging, focused commits, push readiness, post-PR sync-to-dev | commit/stage/push verbs; commit order questions | 207 / 6,695 | — | none | root `CLAUDE.md` branching section (cited, already in loaded context) (:38-39) |
| dependency-audit | Audit-first NuGet/npm vulnerability + staleness scan; smallest safe remediation; no upgrades by default | CVE/advisory/npm-audit/package-safety phrasings; explicit NOT-for list | 158 / 9,568 | — | none | package manifests before naming commands (:48-56); `TESTING_STRATEGY.md` for post-remediation lanes (:89-90); harness-constraints ref, quran-data-safety ref when touched (:126-131) |
| deploy-smoke | Report-only local build/migrate/boot/health smoke before review/PR | Situational: after migrations/upgrades/cross-stack work, "before review and PR creation" | 160 / 8,834 | — | none | appsettings/launchSettings before migration actions (:54-74); frontend package.json (:75-77); harness-constraints (:109-114); quran-data-safety (:130-133) |
| backend-global-usings-cleanup | Action skill: promotes >5-file repeated, layer-safe namespaces to `GlobalUsings.cs`, verifies with build | Usings-cleanup phrasings "even if they don't say the exact words global usings" | 190 / 9,232 | — | `BACKEND_STRUCTURE.md` §Global Usings, `CODING_PRINCIPLES.md` §7 (:61-62,120) | `CLEAN_ARCHITECTURE.md` when layer direction in question (:63) |
| performance-backend-review | Explicit-invocation review-only backend/DB performance audit, 11-section report | Explicit perf intent only (N+1/slow/index/AsNoTracking/import runtime); negative-trigger clause excludes bare "review" (SKILL.md:14-17) | 273 / 15,476 | — | `quran-data-safety.md` ("apply in full", :87-89) | 3 backend `.architecture` docs "only when it sharpens a finding" (:105-107); EF Configurations dirs for index checks (:108-113) |
| performance-angular-review | Explicit-invocation review-only Angular performance audit, 13-section report, Mushaf-weighted | Explicit frontend perf intent only; same negative-trigger pattern | 319 / 18,989 | — | `quran-data-safety.md` (:105-107) | 3 frontend `.architecture` docs incl. 104 KB `UI_STYLE_SYSTEM.md` "only when it sharpens a finding" (:122-129); package.json (:130-133); harness-constraints (:134-137) |
| pr-context-prep | Copy-paste-ready PR package: title, scope, invariants, evidence, risk, merge readiness | PR title/description/reviewer-focus/risk/"merge-readiness check" | 278 / 14,375 | — | none | `TESTING_STRATEGY.md` §5 for the evidence section (:97-103); `specs/**`, feature docs when in the diff (:156-157) |
| test-guard | Reviews test code against nine universal rules + evidence sufficiency | Reactive after test writing/editing; test-specific requests; "when test files appear in a diff you are checking" | 167 / 12,532 | 311 / 20,011 (dotnet, jest, harness-constraints, llm-app-testing) | project agent instructions + testing docs (:29) | dotnet.md / jest.md by stack (:31-32); llm-app-testing.md if LLM APIs (:33); harness-constraints per-finding (:156); `TESTING_STRATEGY.md` when evidence is in front of it (:114-123) |

All rows CONFIRMED against the files at HEAD; sizes re-measured by this author.

### 2.2 Duplication, on-demand candidates, trigger risk, verdict (questions 7–12)

| Skill | Rules duplicated from docs (Q7) | Rules duplicated from other skills (Q8) | On-demand candidates (Q9) | Trigger overbreadth (Q10) | Worth having? (Q11) | Est. context saving (Q12) |
|---|---|---|---|---|---|---|
| engineering-review | File-size threshold behavior (SKILL.md:215-226,272-282 vs `BACKEND_STRUCTURE.md:394-404`/`FRONTEND_STRUCTURE.md:71-72`); Clean Architecture bullets (:186-193 vs `CLEAN_ARCHITECTURE.md:67,136,206`); dumping-folder ban (:196-199 vs `BACKEND_STRUCTURE.md:28-30`); TESTING_STRATEGY block (:381-420 vs `TESTING_STRATEGY.md` §1/§3.3/§3.4/§5/§6/§8) — all CONFIRMED | Threshold + layering + dumping-folder + "God service" ban shared with backend-structure-review; TESTING_STRATEGY block shared with test-guard + pr-context-prep; Test Guard Review criteria (:343-371) restate test-guard's scope | Backend/frontend checklists §§1-3 (the canonical docs are *co-read in the same invocation*); TESTING_STRATEGY restatement; threshold behavior | HIGH — deliberate catch-all; also collides with environment-level review skills (§7) | **Yes** — it is the formal review gate the whole workflow sequences on (root `CLAUDE.md`: deletion commit only after review passes) | ~2–3k tokens/invocation from dedupe; ~8–10 KB static (LIKELY) |
| backend-structure-review | Thresholds (:79-87 vs `BACKEND_STRUCTURE.md:389-404`); layer bullets (:68-75 vs `CLEAN_ARCHITECTURE.md`); dumping-folder ban (:62-66 vs `BACKEND_STRUCTURE.md:28-30`); thin-controllers rules (:94-99) | Entire scope ⊂ engineering-review Backend checklist §§1-3 (SKILL.md:184-226); "God service" ban (:89-90,172) | Its always-read pair (CODING_PRINCIPLES + BACKEND_STRUCTURE, 17,093 B) is the real cost, not the 7 KB body | HIGH — "when new backend folders or files are added" ≈ every backend feature (SKILL.md:9-10) | Marginal as a *separate* skill — its unique value is a cheaper structure-only pass (1.8k vs 6.7k+ token body) | Trigger fix: avoids ~1.8k+12k tokens per false co-fire; MERGE would remove 7,257 B outright (LIKELY) |
| commit-workflow | Branch model condensed with citation (:33-39 vs `CLAUDE.md:24-35`) — pointer-style, low risk | Subtree merge-commit rule shared with pr-context-prep, also stated in `SKILLS_AND_ARCHITECTURE_GUIDE.md:166,330` (§5) | none material | LOW — specific verbs, no broadening clause | **Yes** — only executable Git workflow; owns destructive-command guardrails | ~0.2 KB (negligible) |
| dependency-audit | none material | "Do not commit/stage/push" boilerplate sentence shared with deploy-smoke (:158/:159) | already pointer-based (harness-constraints, quran-data-safety) — this is the working model | LOW — domain keywords + explicit NOT-for list | **Yes** | ~0 |
| deploy-smoke | none material | same boilerplate sentence | already pointer-based | MODERATE — "before review and PR creation" licenses proactive chaining onto review/PR turns (LIKELY); also overlaps build gates at the same boundary (workflow-gate flag) | **Yes** — unique runtime boot + DB-target verification; bundled build re-verification is the redundant part | ~0.2 KB text; the real saving is cadence (report 10's scope) |
| backend-global-usings-cleanup | Layer-restriction table semantically copied (reformatted bullets→table; adds an `Application.Abstractions` row the doc's list lacks), labeled "hard gate — from BACKEND_STRUCTURE.md" (:107-116 vs `BACKEND_STRUCTURE.md:284-296`); global-usings policy (:47-55 vs :260-263 — skill adds the >5-files threshold the doc lacks) | none | the copied layer table | MODERATE — broadening clause fires an *action* skill on vague "tidy the imports"; explicit exclusions limit it | Marginal-but-cheap — niche, self-contained, rarely triggered | ~1 KB (table → pointer) |
| performance-backend-review | Quran-safety-over-speed bullets restated in-body despite pointing at the shared ref (:85-101,252-256 vs `quran-data-safety.md:27-36`) | Anti-noise + review-only guardrails near-verbatim twins with performance-angular-review (:49-77 vs its :62-93) | the restated safety bullets | LOW — model citizen: negative-trigger clause + defer-to list (:14-17, verified) | **Yes** — evidence discipline (anti-noise rules) is genuinely valuable and not stated anywhere else | ~1.5 KB (LIKELY) |
| performance-angular-review | Vitest fork cap restated (:130-133,213-215 vs `frontend-test-harness-constraints.md:34-39` + `package.json:9`); Quran rendering bullets restated (:102-120,295-299 vs `quran-data-safety.md:38-47`) | same twin guardrail block | the restated bullets; note its conditional route reaches the 104 KB `UI_STYLE_SYSTEM.md` | LOW — same negative-trigger pattern | **Yes** | ~2 KB (LIKELY) |
| pr-context-prep | TESTING_STRATEGY block, third copy (:96-123); base-branch rule (:45-48,274-275 vs `CLAUDE.md:33-35`); Quran-data invariant tables partially duplicating `quran-data-safety.md:15-47` **without citing it** (:82-84,190-206 — PARTIAL: the tables also carry invariants the reference lacks — the clean-imlaei-simple identity-key rule, idempotence, report-gate paths) — all CONFIRMED | Subtree rule with commit-workflow (§5 — also in `SKILLS_AND_ARCHITECTURE_GUIDE.md`); TESTING_STRATEGY block with engineering-review + test-guard | TESTING_STRATEGY block; Quran invariant tables → pointer | MODERATE — "merge-readiness check"/"risk assessment" overlap engineering-review's claimed territory; no tie-breaker text (LIKELY) | **Yes** — the package format is unique and the deliberate two-way split with commit-workflow works | ~3–4 KB (LIKELY) |
| test-guard | TESTING_STRATEGY block, second copy (:112-150) | Ownership-split sentences stated 3× (test-guard :14-16,159-167; engineering-review :333-341; root `CLAUDE.md` test-code self-check section) | TESTING_STRATEGY block | MODERATE **by design** — "test files in a diff you are checking" fires it on every review-with-tests; that is the intended sub-gate coupling, not accidental | **Yes** — single source for the nine rules; prevents AI test bloat | ~1.5 KB (LIKELY) |

### 2.3 Classification (brief §11 taxonomy, verbatim)

| Skill | Primary | Secondary |
|---|---|---|
| engineering-review | **KEEP** | **SIMPLIFY** (dedupe embedded checklists + TESTING_STRATEGY block), **REFERENCE_ON_DEMAND** (already partial; extend to the checklists) |
| backend-structure-review | **TRIGGER_NARROWING_NEEDED** | **MERGE** candidate (into engineering-review — its scope is a strict subset; evaluate against the value of a cheap structure-only pass) |
| commit-workflow | **KEEP** | — (subtree rule already has a non-skill home; whether it is the right one is report 06's call — §5) |
| dependency-audit | **KEEP** | — |
| deploy-smoke | **KEEP** | **TRIGGER_NARROWING_NEEDED** (drop/precondition the "before review and PR creation" clause) |
| backend-global-usings-cleanup | **KEEP** | **SIMPLIFY** (copied layer table → pointer) |
| performance-backend-review | **KEEP** | **SIMPLIFY** (restated safety bullets → pointer) |
| performance-angular-review | **KEEP** | **SIMPLIFY** (restated fork-cap + rendering bullets → pointers) |
| pr-context-prep | **SIMPLIFY** | **TRIGGER_NARROWING_NEEDED** (boundary sentence vs engineering-review on "merge-readiness") |
| test-guard | **KEEP** | **SIMPLIFY** (TESTING_STRATEGY block → shared pointer) |

No skill is a **DELETE_CANDIDATE**. Every skill encodes at least one behavior that exists nowhere else (engineering-review: verdict gate; commit-workflow: safe Git execution; test-guard: nine rules; perf skills: anti-noise discipline; deploy-smoke: DB-target verification; dependency-audit: transitive-parent analysis; pr-context-prep: package format; usings-cleanup: >5-files threshold; backend-structure-review is the only one whose *content* is fully replaceable, hence MERGE not DELETE). CONFIRMED for content uniqueness by the inventory's full-read duplication maps; the MERGE-vs-KEEP call for backend-structure-review is judgment, tagged LIKELY.

---

## 3. Why the two performance skills should NOT merge

The twin duplication (anti-noise + guardrails, ~30 lines each) invites a merge proposal. Evidence is against it: a merged skill would be ~34.5 KB (~8.6k tokens) loaded on every performance request, versus 15.5 KB or 19 KB today for single-stack requests — the common case. The stacks' checklists share almost nothing else (EF/SQL vs Signals/change-detection). Merging would *increase* per-invocation context to save ~2–3 KB of static text. The cheaper fix is trimming the twinned block or accepting it as stable boilerplate. LIKELY.

---

## 4. Engineering-review — the special case (brief §12)

### 4.1 Worst-case closure: 24 files, ~90k tokens — CONFIRMED

A worst-case "thorough" invocation (full-stack diff with tests, implemented from Spec Kit, deep clean-code pass) can route into **359,926 bytes ≈ 89,981 tokens across 24 files** (`data/skill-inventory.json` `engineering_review_closure`; group arithmetic re-checked by this author):

| Group | Files | Bytes | ~Tokens |
|---|---:|---:|---:|
| Skill body | 1 | 26,670 | 6,667 |
| Always-read (CODING_PRINCIPLES, TESTING_STRATEGY, quran-data-safety) | 3 | 41,741 | 10,435 |
| Backend `.architecture` docs | 3 | 35,318 | 8,829 |
| Frontend docs (incl. `UI_STYLE_SYSTEM.md` 103,970 B, `PRODUCT.md`, `DESIGN.md`) | 5 | 160,264 | 40,066 |
| Clean-code-guard pack | 7 | 56,840 | 14,210 |
| Test-guard chain (SKILL + 3 refs) | 4 | 30,397 | 7,599 |
| SPEC_KIT_IMPLEMENTATION_REVIEW | 1 | 8,696 | 2,174 |
| **Total** | **24** | **359,926** | **89,981** |

Excluded from that total: `specs/<feature>/` artifacts (variable; no feature open at HEAD) and `llm-app-testing.md`. The frontend group alone is 44.5% of the closure, and `UI_STYLE_SYSTEM.md` alone is 29% — a fact about the *documents*, not the skill. CONFIRMED.

### 4.2 The always-read floor: ~17.1k tokens — CONFIRMED

Body (26,670) + `CODING_PRINCIPLES.md` (5,190) + `TESTING_STRATEGY.md` (33,427) + `quran-data-safety.md` (3,124) = **68,411 B ≈ 17,102 tokens before a single line of diff is read**, on *every* invocation including a one-line fix review. `TESTING_STRATEGY.md` is nominally qualified ("when judging whether the executed tests were sufficient" — SKILL.md:53-54) but the Verification Check is mandatory output section 9 (SKILL.md:525-528), so it is effectively unconditional. Same pattern for `quran-data-safety.md` (mandatory output section 7, SKILL.md:502-504). CONFIRMED by this author's full read.

The instruction-inventory trace for a formal backend-diff-with-tests review measures **~51.6k tokens mandatory / ~65.2k with conditionals** (17 / 24 files) — the skill chain riding on top of the entrypoint instruction files.

### 4.3 What must remain embedded vs routed on demand

**Must remain embedded** (behavior that exists nowhere else, or that defines the skill's contract):

- The verdict model, severity levels, and 10-section output format (SKILL.md:422-540) — this *is* the gate other workflow rules sequence on.
- Review-only guardrails and the untracked-files/commit-workflow boundary (SKILL.md:23-42,547-550) — a genuine behavioral rule, repeatedly load-bearing, unique to this file.
- The delegation contracts: test-guard sub-gate mandate (:75-88) and the Spec Kit trigger list (:118-144).
- The reading-rules router itself (:44-147) — the routing logic is the skill's core value.

**Should route on demand instead of being embedded** (all duplicated with canonical docs that the same invocation is *already told to read*):

- Backend checklist §§1-3 and frontend checklist §§1-3 (:184-226, :239-282): when Backend changed, `BACKEND_STRUCTURE.md` + `CLEAN_ARCHITECTURE.md` are mandatory co-reads — the embedded summary is then a second copy in the same context window. Same for the frontend set. CONFIRMED duplication; routing them is LIKELY safe because the canonical read is already mandatory in exactly the invocations where the checklist applies.
- The TESTING_STRATEGY restatement block (:381-420) — see §4.6.
- The Test Guard Review criteria list (:345-358) — restates what test-guard (mandatorily loaded in those invocations) already defines; the output-format skeleton (:506-521) suffices.

**Deliberately fine as-is:** the clean-code pack is already per-finding/on-demand (:56-67), `sources.md` contested-only (:67), Spec Kit artifacts skipped for simple changes (:143-144).

### 4.4 Does scoped review avoid broad documents? Adjudication: partially — the routing is real, the floor is not scoped

Scoped-review paths exist and are genuine, not decorative — CONFIRMED by direct read:

- "Read only the docs relevant to what actually changed" — SKILL.md:44-45.
- Per-area conditional blocks (If Backend / If Frontend / both) — SKILL.md:96-116.
- "If the change is a simple, non–Spec-Kit change, do **not** read the Spec Kit artifacts" — SKILL.md:143-144.

But three structural limits blunt the scoping:

1. **The floor is unscoped.** ~17.1k tokens are prescribed regardless of diff size (§4.2). A one-line backend fix review carries the full `TESTING_STRATEGY.md` even though only §5 plus a handful of tree facts are needed for the Verification Check.
2. **The scoping unit is the whole file.** "If UI involved, read `UI_STYLE_SYSTEM.md`" scopes *which* documents, not *how much* of them — and that document is 104 KB. Any frontend visual change escalates the closure by ~26k tokens through a correctly-followed conditional rule. The skill's routing is sound; the routed monoliths are the problem (reports 03/06/07 territory).
3. **Re-review resets the bill.** Nothing in the skill carries state across invocations; a post-fix re-review re-prescribes the same set (`data/workflow-gate-inventory.json` redundancy flag "Review workflow front-loads a large instruction-reading budget", CONFIRMED there).

Verdict: **the skill's scoping logic should be kept as the model; the savings live in shrinking the always-read floor (dedupe + sectioned consumption of TESTING_STRATEGY) and in the routed documents' size.** LIKELY.

### 4.5 Test-guard delegation efficiency: structurally efficient, ~2 KB of seam overlap

The delegation avoids the worst outcome (a second full copy of the nine rules inside engineering-review). The boundary text is explicit in both directions: engineering-review mandates the sub-gate (:75-88) and keeps the final verdict; test-guard cedes it back (test-guard SKILL.md:15,149-150). Marginal cost of a review-with-tests: test-guard body 12,532 B + typically one stack reference (dotnet 9,742 B or jest 2,125 B) ≈ 3.7–5.6k tokens — proportionate to what it buys. CONFIRMED.

The inefficiencies at the seam:

- The same TESTING_STRATEGY block sits in **both** skills, so a review-with-tests holds **two copies in one context window** (§4.6).
- engineering-review's Test Guard Review criteria (:345-358) restate test-guard's scope in different words — a drift risk between two files that are always co-loaded in exactly these invocations.
- The ownership-split sentence exists in three places (test-guard :14-16, engineering-review :333-341, root `CLAUDE.md`). One canonical statement + pointers would do.

Verdict: **KEEP the delegation model; SIMPLIFY the seam.** CONFIRMED (overlaps), LIKELY (fix safety).

### 4.6 The TESTING_STRATEGY evidence-block triplication — the single largest cross-skill duplication

The same ~2–3 KB rule block (2,919 / 2,559 / 1,977 B per copy) — tier-b naming survives Tier A–E, hand-written `--filter` is not a lane, no CI exists, route-smoke gate + `SmokeRouteCatalog` parity, canonical resources *fail* rather than skip, shard reporting — is maintained in **three** skills:

- `.claude/skills/engineering-review/SKILL.md:381-420` (verified by full read)
- `.claude/skills/test-guard/SKILL.md:112-150` (verified by full read)
- `.claude/skills/pr-context-prep/SKILL.md:96-123` (verified by direct read)

against canonical `TESTING_STRATEGY.md:19,39-40,162-174,254-273,297,376`. Each copy self-identifies as a restatement and defers to the canonical doc. CONFIRMED.

Why it matters beyond static bytes: these three skills co-occur *in sequence on the same boundary* (review → test-evidence check → PR package), and two of them co-occur *in the same context window* (review-with-tests). Every drift in `TESTING_STRATEGY.md` §3/§5/§6 now requires four synchronized edits; the workflow-gate inventory already documents that the route-smoke rule alone is stated in five places (`TESTING_STRATEGY.md:256`, engineering-review:406, pr-context-prep:110, test-guard:133, plus §6 itself). The content is review-*critical* (BLOCKING-finding definitions), which is exactly why it was embedded — and exactly why a single shared reference (the `quran-data-safety.md` model, which these same skills already use successfully) is the right shape: one copy, N pointers, loaded on demand at the same moments the three copies load today. LIKELY (safety of consolidation); CONFIRMED (existence, locations, sizes).

---

## 5. Orphaned law — one rule that exists ONLY in skills (a second candidate refuted)

Repo-wide grep by this author (excluding the audit folder) checked two normative rules for a home outside skill bodies. One is a true orphan; the other is not:

1. **The "God service" terminology ban** ("religiously inappropriate terminology" — engineering-review SKILL.md:555): stated 5× across exactly two skills (engineering-review :224,282,555; backend-structure-review :90,172). CONFIRMED absent from all canonical docs — no README, no `CLAUDE.md`/`AGENTS.md`, no `.architecture/` doc, no `docs/contracts/` entry states it.
2. **The subtree merge-commit rule** (unsquashed subtree-import PRs require GitHub's merge-commit strategy; squash/rebase forbidden because imported tips would stop being ancestors): stated in two skills (commit-workflow :124-127,198; pr-context-prep :9,78,181,233) **and in `SKILLS_AND_ARCHITECTURE_GUIDE.md:166,330`** — a document on the workspace's long-lived root-law list. It is absent from `CLAUDE.md`, `AGENTS.md`, `TESTING_STRATEGY.md`, and `docs/contracts/`, but it is **not an orphan**: a canonical non-skill source states it twice. CONFIRMED (non-orphan status, by direct read of the guide).

The God-service ban is real project law — a product-register decision (this is a Quran scholarship product; the rationale is stated only inside engineering-review's guardrails). If the skills were ever simplified/merged/regenerated, that rule would silently vanish, because **no canonical source would notice**. Under the workspace's own documentation doctrine ("steady-state truth is code + nearest README, indexed by docs/contracts/"), a rule that lives only in skill bodies is undocumented law. Flagged for report 06 (doc/decision inventory): the ban needs one canonical home, with the skills pointing at it. For the subtree rule the question is narrower — whether `SKILLS_AND_ARCHITECTURE_GUIDE.md` is the right canonical home, which is also report 06's call. CONFIRMED (God-service orphan status); the destination choice is out of this report's scope.

---

## 6. `.agents/skills/` adapter verification (brief §11 pointer-only invariant)

**Pointer-only invariant for the 10 in-scope skills: CONFIRMED.** All 10 adapters are 18–34 lines / 655–2,066 B, each carrying frontmatter and the canonical path; eight also carry "Do not keep a second full copy … this file exists only to route agents to the single source of truth" verbatim. This author fully read 4 of 10 (engineering-review, commit-workflow, test-guard, performance-angular-review) and size-verified all 10; none duplicates substantive rules. The two performance adapters (31 and 34 lines) exceed the ~30-line norm with a summary paragraph but introduce no independent rules. Each adapter also carries an `agents/openai.yaml` (2,661 B total across the 10) absent under `.claude/` — harness metadata, not rules.

**Two content defects, both CONFIRMED by direct read:**

1. **commit-workflow adapter misdescribes the canonical behavior — twice.** `.agents/skills/commit-workflow/SKILL.md:7-8` says "the post-PR **sync-to-main** workflow" and `:17-18` says "including Section 7, the post-PR **sync-to-main** workflow". The canonical section 7 is "**Post-PR sync to dev**" (`.claude/skills/commit-workflow/SKILL.md:128`) and explicitly forbids the described behavior: "Do not switch to or sync `main` here" (:160-161). A non-Claude agent skimming only the pointer text is invited to sync the **protected production branch**. Severity is mitigated because the pointer directs the agent into the canonical file, whose text wins — but a pointer whose summary contradicts its target on a production-branch operation is a defect, not noise.
2. **test-guard adapter's reference list is stale.** `.agents/skills/test-guard/SKILL.md:17` enumerates `dotnet.md`, `jest.md`, `llm-app-testing.md` — omitting `frontend-test-harness-constraints.md`, which exists in `.claude/skills/test-guard/references/` and is the reference four other skills route into for harness behavior. A non-Claude agent trusting the enumeration would not discover the fork-cap/jsdom constraints file.

**Contrast that validates the pointer model:** the 9 `.agents/skills/speckit-*` files are near-full **forks** (100,271 B) whose content already diverges from the `.claude` versions (frontmatter and hook sections — divergence re-verified by this author on speckit-implement). The 10 project skills prove the pointer model costs ~12 KB total and one small drift surface (the two defects above); the speckit forks cost 100 KB and unbounded drift. CONFIRMED (measurement); the speckit tree is out of redesign scope per brief §31.

---

## 7. Trigger-collision analysis

Three collision layers, in decreasing order of guardedness:

**Layer 1 — deliberately guarded (works):** the two performance skills and test-guard carve sub-domains out of the "review" utterance space with explicit negative triggers ("It triggers only on explicit backend performance intent, not on the word review by itself" — performance-backend-review SKILL.md:14-17, verified) and defer-to lists. commit-workflow ↔ pr-context-prep is a deliberate two-way split with hand-off text in both directions. engineering-review ↔ test-guard double-firing is intentional coupling (sub-gate). CONFIRMED.

**Layer 2 — unguarded inside the 10 (two cases):**

- **backend-structure-review vs engineering-review.** "…or when new backend folders or files are added, even if they don't say the word structure" (backend-structure-review SKILL.md:9-10) describes virtually every backend feature change, which engineering-review's catch-all also claims. backend-structure-review's guardrail "do not duplicate the broader engineering-review skill" (:173-174, verified) governs its *output*, not its *firing*. Both descriptions match an ordinary "review this new backend feature" — and since engineering-review's Backend checklist §§1-3 is a superset of backend-structure-review's entire scope, a co-fire buys ~1.8k tokens of body plus ~4.3k tokens of its always-read pair for zero additional coverage. CONFIRMED (description overlap); actual co-fire frequency NEEDS_MEASUREMENT.
- **pr-context-prep vs engineering-review on "is this ready to merge".** engineering-review's description claims engineering-readiness; pr-context-prep's claims "merge-readiness check"; no tie-breaker text exists in either. LIKELY.

**Layer 3 — environment-level (outside the 10, colliding with them):** the active skill roster in this repository's agent environment contains at least five other claimants for a bare "review this" utterance: `coderabbit:code-review` (self-declared "Default code-review skill. Trigger for any explicit review request AND autonomously when the agent thinks a review is needed"), `code-review:code-review`, `/review`, `coderabbit:coderabbit-review`, and `superpowers:requesting-code-review`. CONFIRMED that these declarations exist and textually collide with engineering-review's catch-all; which skill actually fires for a given utterance is a harness-routing outcome that cannot be determined statically — NEEDS_MEASUREMENT. The risk is not context cost but **gate integrity**: if a generic reviewer wins the route, the project-specific BLOCKING conditions (route-smoke evidence, canonical fail-not-skip, Quran data safety section, verdict format the workflow sequences on) are silently skipped. Engineering-review's catch-all breadth is therefore *defensible as a defense* of the gate — narrowing it is NOT recommended until the environment-level routing is measured.

---

## 8. Spec Kit — measured size only (brief §31)

| Component | Files | Bytes | ~Tokens | Tag |
|---|---:|---:|---:|---|
| `.claude/skills/speckit-*` | 15 skills | 144,214 | 36,053 | CONFIRMED (re-measured) |
| `.agents/skills/speckit-*` | 9 near-full copies | 100,271 | 25,067 | CONFIRMED (re-measured) |
| `.specify/` | 37 files | 161,767 | 40,441 | CONFIRMED (re-measured) |
| **Combined** | 61 | **406,252** | **101,563** | CONFIRMED |

Observations recorded without redesign proposals: (a) the speckit text corpus is 1.76× the entire 10-skill system including references and adapters; (b) the `.agents` copies are forks, not pointers, and already diverge (§6); (c) six speckit skills exist only on the `.claude` side (`speckit-converge`, the five `speckit-git-*`), so the fork set is also incomplete; (d) one cadence conflict between `speckit-implement` and `TESTING_STRATEGY.md` is documented in `data/workflow-gate-inventory.json` (per-phase validation vs "do not run broad lanes per phase") and belongs to report 10.

---

## 9. Proposed simplifications (each answering the 7 questions of brief §4)

> Audit-mode note: these are candidate simplifications for later planning, with classification. No implementation instructions, no ordering.

### S1 — Consolidate the TESTING_STRATEGY evidence block (3 copies → 1 shared reference) — SIMPLIFY / REFERENCE_ON_DEMAND

1. **Value today:** puts review-critical evidence rules (no-CI, filter-not-a-lane, route-smoke, canonical fail-not-skip, sharding) directly in front of the reviewer/packager without a second file read.
2. **What depends on it:** engineering-review's Verification Check (output §9), test-guard's evidence-sufficiency section, pr-context-prep's evidence section; the route-smoke BLOCKING rule is invoked at all three points.
3. **Risk if it changes:** a reviewer misses a BLOCKING condition if the pointer is not followed; drift risk moves from 4 copies to 1 copy + 3 pointers (strictly better).
4. **Equivalent protection elsewhere:** yes — `TESTING_STRATEGY.md` is canonical, is already the always-read of engineering-review, and each copy already instructs reading it there; the `quran-data-safety.md` shared-reference pattern is proven with 5 consumers in these same skills.
5. **Smallest safe simplification:** one shared evidence reference under a skill `references/` dir, three skills pointing at it the way five skills point at `quran-data-safety.md`.
6. **Later verification:** grep shows exactly one copy of each tree fact (route-smoke, fail-not-skip, no-CI) across the three skills; a sample review transcript still cites the tree facts.
7. **Recurring cost removed:** ~4–5.5 KB static; ~1.5–2k tokens per review-with-tests context window (which currently holds two copies) and per pre-PR sequence (three loads across the boundary); 4-way sync burden on every TESTING_STRATEGY change → 1-way. CONFIRMED sizes; LIKELY safety.

### S2 — Route engineering-review's embedded backend/frontend checklists on demand — SIMPLIFY / REFERENCE_ON_DEMAND

1. **Value today:** the checklists (SKILL.md:184-318) give the reviewer a compact walk-through without opening the architecture docs.
2. **Depends on it:** every engineering review; the output's Threshold and Architecture sections.
3. **Risk:** a reviewer with only the body could miss layering/threshold checks if the pointer is ignored.
4. **Equivalent protection:** yes, and unusually strong — the very invocations where each checklist applies are the invocations where the canonical docs (`BACKEND_STRUCTURE.md`, `CLEAN_ARCHITECTURE.md`, `FRONTEND_STRUCTURE.md`, …) are *already mandatory reads* per SKILL.md:96-116. Today those invocations hold rule + restatement simultaneously.
5. **Smallest safe simplification:** compress each checklist section to the check *names* plus the canonical pointer (the pattern the skill itself already uses for thresholds: "You do not need to copy every number" — SKILL.md:204-205, and which backend-structure-review already follows: "It relies on the canonical backend architecture docs rather than restating them").
6. **Later verification:** review transcripts still produce Threshold/Architecture findings; body size drops measurably; no checklist item exists that lacks a canonical-doc anchor.
7. **Recurring cost removed:** ~6–8 KB body (~1.5–2k tokens) on *every* review invocation; removes the skill-vs-doc drift surface for thresholds/layering. CONFIRMED duplication; LIKELY safety.

### S3 — Narrow backend-structure-review's trigger; evaluate MERGE — TRIGGER_NARROWING_NEEDED / MERGE

1. **Value today:** a cheap structure-only review (7.3 KB body) for placement questions, without the full engineering-review apparatus.
2. **Depends on it:** users asking "where should this live"; nothing else routes *to* it.
3. **Risk:** narrowing the trigger loses proactive structure checks on new backend folders; merging loses the cheap focused pass.
4. **Equivalent protection:** yes — engineering-review's Backend checklist §§1-3 covers the identical checks (CONFIRMED superset), and fires on the same changes.
5. **Smallest safe simplification:** remove the "or when new backend folders or files are added, even if they don't say the word structure" clause so it fires only on explicit structure/placement intent; the MERGE decision can wait for firing-frequency evidence.
6. **Later verification:** ordinary backend feature reviews show a single review skill firing; explicit placement questions still route to it.
7. **Recurring cost removed:** per avoided co-fire, ~1.8k tokens body + ~4.3k tokens always-read pair; eliminates duplicate-verdict noise. CONFIRMED overlap; co-fire frequency NEEDS_MEASUREMENT.

### S4 — Fix the two `.agents` adapter defects — (correctness, not context)

1. **Value today:** the adapters route non-Claude agents to canonical skills; the defective texts currently *misroute meaning*.
2. **Depends on it:** any Codex/OpenCode/Cursor agent doing Git work or test review via `.agents/skills/`.
3. **Risk of the status quo:** the commit-workflow pointer twice advertises a sync-to-**main** workflow that the canonical skill explicitly forbids (`.claude/skills/commit-workflow/SKILL.md:160-161`) — an agent acting on the summary touches the protected production branch; the test-guard pointer hides one reference file.
4. **Equivalent protection:** partial — the canonical files are correct, and an agent that follows the pointer fully is safe; the defect bites exactly the agent that trusts the summary.
5. **Smallest safe simplification:** correct the two adapter texts ("dev" for the sync target; complete the reference enumeration).
6. **Later verification:** `grep -n "sync-to-main" .agents/` returns nothing; the adapter's reference list matches `ls .claude/skills/test-guard/references/`.
7. **Recurring cost removed:** none (bytes unchanged); removes a production-branch misdirection and a discovery gap. CONFIRMED defects.

### S5 — Give the God-service ban a canonical home; settle the subtree rule's — MOVE_TO_CANONICAL_SOURCE (report 06 taxonomy; doc-side, skills then point)

1. **Value today:** the God-service ban and the subtree merge-commit rule are enforced project law.
2. **Depends on it:** review terminology in two skills; merge strategy of every subtree-import PR; history-integrity checks in commit-workflow §7.
3. **Risk:** for the God-service ban, if the only carriers (skill bodies) are simplified or regenerated, the rule vanishes with no canonical source noticing (CONFIRMED: grep finds no other statement). The subtree rule does not share this failure mode — `SKILLS_AND_ARCHITECTURE_GUIDE.md:166,330` survives any skill regeneration.
4. **Equivalent protection:** for the God-service ban, none today — that is the finding. For the subtree rule, yes — `SKILLS_AND_ARCHITECTURE_GUIDE.md` (long-lived root law) states it twice; the open question is whether that guide is the right canonical home, which is report 06's call.
5. **Smallest safe simplification:** state the God-service ban once in a canonical doc surface (candidate homes are report 06's call); confirm or relocate the subtree rule's existing home; then have the skills cite the canonical statement like they cite the branching model.
6. **Later verification:** grep finds each rule in exactly one canonical location plus pointers.
7. **Recurring cost removed:** none direct; removes a silent-loss failure mode (God-service) that would otherwise tax every future skill simplification with archaeology. CONFIRMED (rule locations re-verified).

### S6 — pr-context-prep cites `quran-data-safety.md` instead of duplicating invariant tables — SIMPLIFY

1. **Value today:** the invariant tables (:82-84,190-206) let the packager fill the invariants section without a file read.
2. **Depends on it:** the PR package's invariants section.
3. **Risk:** invariants drift between the uncited duplicate and the shared reference — pr-context-prep is the *only* consumer of these rules that does not cite the reference (CONFIRMED).
4. **Equivalent protection:** partial — `quran-data-safety.md` (3,124 B) is canonical with 5 citing consumers for the shared invariants, but the tables also carry invariants the reference lacks (the clean-imlaei-simple identity-key rule, idempotence, report-gate paths); the identity-key rule's equivalent homes are the Backend/Frontend READMEs (e.g. `Backend/README.md:113`), not the cited reference.
5. **Smallest safe simplification:** replace the shared-invariant content with the pointer + the package-format skeleton, keeping the identity-key line (or adding a README pointer for it) alongside the `quran-data-safety.md` pointer.
6. **Later verification:** the reference's consumer count becomes 6; no divergent phrasing of the shared invariants remains in the skill; the identity-key rule is still stated or pointed to.
7. **Recurring cost removed:** ~1.5–2 KB static; one drift surface on the highest-protection rule set in the workspace (brief §29 territory — this proposal *strengthens* Quran-data protection by unifying its statement, and touches no protection content). CONFIRMED (PARTIAL duplication).

### S7 — Trim in-body restatements in the two performance skills and usings-cleanup — SIMPLIFY

1. **Value today:** stack-specific framing of the shared safety rules; a copied layer table "so the gate is visible".
2. **Depends on it:** the skills' own checklists.
3. **Risk:** low — each restatement sits next to an existing pointer to its canonical source.
4. **Equivalent protection:** yes — `quran-data-safety.md`, `frontend-test-harness-constraints.md`, `BACKEND_STRUCTURE.md:284-296` all canonical and cited.
5. **Smallest safe simplification:** keep the stack-specific severity framing (the reference itself licenses that), drop the near-verbatim bullet restatements and the copied table.
6. **Later verification:** each rule appears once in its canonical file, with pointers; skill bodies shrink measurably.
7. **Recurring cost removed:** ~4–5 KB static across three skills (~1–1.2k tokens per invocation of each). CONFIRMED duplication; LIKELY safety.

**Not proposed:** narrowing engineering-review's catch-all trigger (defends the gate against environment-level claimants — §7, Layer 3); merging the performance twins (§3); deleting any skill (§2.3); any change to speckit (out of scope).

---

## 10. Mandatory questions answered (brief §25, questions 16–21)

**Q16 — What are the sizes of the 10 custom Skills?**
Bodies: engineering-review 26,670 B/556 LOC; performance-angular-review 18,989/319; performance-backend-review 15,476/273; pr-context-prep 14,375/278; test-guard 12,532/167; dependency-audit 9,568/158; backend-global-usings-cleanup 9,232/190; deploy-smoke 8,834/160; backend-structure-review 7,257/174; commit-workflow 6,695/207. Total 129,628 B / 2,482 LOC ≈ 32.4k tokens; +88,671 B references (engineering-review 68,660 incl. the 56,840 B clean-code pack; test-guard 20,011); +12,021 B `.agents` adapters. All CONFIRMED by this author's `stat`/`wc` re-measurement.

**Q17 — What references do they pull?**
Only two skills own reference packs: engineering-review (clean-code-guard ×7, quran-data-safety, SPEC_KIT_IMPLEMENTATION_REVIEW) and test-guard (dotnet, jest, harness-constraints, llm-app-testing). Five skills cross-route into `quran-data-safety.md` and four into `frontend-test-harness-constraints.md` — shared-reference reuse, working as intended. The heavier pulls are workspace docs: `CODING_PRINCIPLES.md` (3 skills always), `TESTING_STRATEGY.md` (4 skills always/conditionally), the 6 `.architecture` docs conditionally, `PRODUCT.md`/`DESIGN.md` conditionally — worst case ~90k tokens for engineering-review (§4.1). CONFIRMED.

**Q18 — Which rules duplicate project docs?**
CONFIRMED skill-vs-doc duplications: file-size threshold behavior (engineering-review + backend-structure-review vs `BACKEND_STRUCTURE.md:394-404`/`FRONTEND_STRUCTURE.md:71-72`); Clean Architecture layer bullets (both review skills vs `CLEAN_ARCHITECTURE.md:67,136,206`); dumping-folder ban (both vs `BACKEND_STRUCTURE.md:28-30`); the TESTING_STRATEGY evidence block ×3 (§4.6); branching model (commit-workflow, pr-context-prep vs `CLAUDE.md:24-35` — condensed, cited, low-risk); the usings layer table (vs `BACKEND_STRUCTURE.md:284-296`, reformatted copy that adds an `Application.Abstractions` row); Quran-safety bullets (both perf skills vs `quran-data-safety.md`, pr-context-prep uncited and partial); fork-cap (perf-angular vs harness-constraints + `package.json:9`). Inverse finding: one rule (the God-service ban) exists *only* in skills with no doc; the subtree merge-commit rule does not — `SKILLS_AND_ARCHITECTURE_GUIDE.md:166,330` states it (§5).

**Q19 — Which references should become on-demand?**
Most already are — the clean-code pack, sources.md, Spec Kit artifacts, stack references, and the `.architecture` conditionals are demand-routed today (CONFIRMED, §4.4). The remaining candidates are the *embedded restatements*, not the reference files: engineering-review's backend/frontend checklists (S2), the three TESTING_STRATEGY blocks (S1), the perf skills' safety bullets and the usings table (S7), pr-context-prep's invariant tables (S6). The one always-read worth re-examining is the *full* `TESTING_STRATEGY.md` read (8.4k tokens) where §5 + the tree facts would serve — that is a document-structure question for reports 03/06, flagged here as the floor's dominant term.

**Q20 — Are `.agents` adapters truly pointer-only?**
For the 10 in-scope skills: **yes, CONFIRMED** — all ten are 18–34-line pointer stubs with no independent rules (4 fully read, 10 size-verified by this author). With **two CONFIRMED content defects**: the commit-workflow pointer says "post-PR sync-to-main" twice where the canonical skill is sync-to-**dev** and forbids touching main (:160-161); the test-guard pointer omits `frontend-test-harness-constraints.md` from its reference enumeration. For the out-of-scope speckit set: **no** — 9 near-full forks (100,271 B) with confirmed divergence.

**Q21 — Which Skills cause the most context overhead?**
(1) **engineering-review**, by a wide margin: ~17.1k-token unconditional floor, ~51.6k tokens mandatory on a measured formal-review trace, ~90k worst case (§4.1–4.2) — and it runs at least once per feature by workflow law. (2) **The review-with-tests chain** it mandates: +test-guard body and stack refs (~3.7–5.6k tokens) plus a second copy of the TESTING_STRATEGY block in the same window. (3) **performance-angular-review**: largest body after engineering-review (4.7k tokens) with a conditional route into the 104 KB `UI_STYLE_SYSTEM.md`. (4) **pr-context-prep**: 3.6k body + a full `TESTING_STRATEGY.md` consultation at every pre-PR boundary. Of the remaining six, performance-backend-review is mid-weight (~3.9k-token body plus the shared `quran-data-safety.md` always-read); the other five are cheap (1.7–2.4k tokens, self-contained). CONFIRMED sizes; invocation frequencies NEEDS_MEASUREMENT (no telemetry).

---

## 11. Measurement gaps

- **Actual skill-routing behavior** (which of ≥6 claimants fires on a bare "review this"; how often backend-structure-review co-fires; whether deploy-smoke chains proactively): declarations are static text; firing is a harness decision with no logs in the repo. NEEDS_MEASUREMENT — a later controlled-prompt probe can measure it.
- **Actual tokens read at runtime:** all totals are full-file static sums (bytes/4). Agents may read partially (offsets) or skip prescribed reads; trace totals are upper bounds on prescribed reading, not measurements of performed reading. NEEDS_MEASUREMENT.
- **Invocation frequency per skill:** no telemetry exists (no CI, all gates local — `data/workflow-gate-inventory.json` ci.present=false). Recurring-cost estimates assume the documented cadences (engineering-review ≥1×/feature, pr-context-prep 1×/PR). NEEDS_MEASUREMENT.
- **Whether non-Claude agents actually traverse `.agents` pointers** (and would therefore hit the two defects): no execution logs. UNKNOWN.
- **Token-approximation error:** bytes/4 is the audit-standard heuristic; true tokenizer counts for Markdown typically run ±15%. All token figures inherit this. LIKELY within that band.

---

*Report author: skills-audit agent (Phase 2). Independent adversarial review pending (Phase 3).*
