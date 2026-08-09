# Workflow & Instruction Routing V2 Implementation Plan

Either Claude or Sol/Codex can execute this plan directly and sequentially. It requires no delegation, subagent workflow, or external execution framework.

**Goal:** Replace the mirrored Claude/Sol/Codex/Cursor instruction chain with two small native entrypoint chains that route an ordinary implementation task to about two to four relevant context sources before code, while retaining every protection-bearing rule.

**Architecture:** `CLAUDE.md` is Claude's only root entrypoint and `AGENTS.md` is Sol/Codex's only root entrypoint. Each contains the same small universal safety/workflow kernel, routes only to its own area entrypoint, then selects the nearest README and task-triggered headings or specialist material; neither native chain reads the other. `CODING_PRINCIPLES.md` remains conditional and heading-scoped rather than becoming a replacement always-read book.

**Mechanisms:** Markdown entrypoints, existing README and `.architecture` ownership, existing native Skill routing, static reference checks, and small read-only agent probes. No production code or test behavior changes.

## Global constraints

- Support Claude and Sol/Codex only. Retire Cursor support.
- Keep both native entrypoints small, direct, and independent; never point `CLAUDE.md` at `AGENTS.md` or `AGENTS.md` at `CLAUDE.md` at any level.
- Duplicate only a tiny, stable universal kernel in the two roots: scope discipline, `main`/production protection, Quran-data safety, local-instruction discovery, and no unrelated commit/push/PR/review/deploy work. Preserve functional equivalence without requiring byte identity.
- Do not create a third always-read shared instruction file or make all of `CODING_PRINCIPLES.md` mandatory for ordinary work. Route to its exact relevant heading where practical.
- Move rule bodies before removing their old copy. A pointer is acceptable only when it names an exact file and, for long documents, the relevant heading.
- Preserve branch/deployment, Quran/source-data, auth/security, migration, test-trigger, and nearest-README protections.
- Preserve the current nearest-README model. README shortening is not part of this plan.
- Keep test cadence and the contents of the execution-trigger matrix unchanged.
- Leave custom Skill design, engineering-review design, Spec Kit design, and persistent Claude/Codex memory untouched except for the two branch-safety pointer repairs named below.
- Outside the six entrypoints, change only the ownership moves and pointer/reference repairs enumerated in §3. No cleanup, reformatting, or prose improvement may ride along.
- Treat the audit's `41.7%` as an unweighted static six-scenario estimate based on `bytes/4`, not tokenizer output, observed reads, or a guaranteed saving. It also changes protection-bearing trigger assumptions; it is not an acceptance target. Sol notes that excluding the unusually large T2 scenario yields `34.6%` (`13-sol-independent-review.md:33-35,61-69`).

---

## 1. Problems this plan fixes

1. Root `CLAUDE.md` and `AGENTS.md` are byte-identical, the Backend pair is byte-identical, and the Frontend pair differs only by its title. This mirrors full rule books by hand and has already produced three root drift/resync cycles (`03-agent-context-instruction-audit.md:39-55`).
2. `AGENTS.md` sends non-Claude agents into root and area `CLAUDE.md` files, while Cursor's always-apply rule tells them not to rely on Claude files. The current route is contradictory and can load both mirrored chains (`03-agent-context-instruction-audit.md:57-62`; `.cursor/rules/always-read-agents.mdc:6-41`).
3. Root entrypoints mix routing with branch law, artifact lifecycle, comment policy, test workflow, design context, and Claude-specific mechanisms. Most of that content is neutral or specialized and need not be present in every session (`03-agent-context-instruction-audit.md:66-87`).
4. The area pairs repeat test summaries, source/report rules, comment policy, and API/localization reminders whose canonical owners already exist.
5. Cursor adds a third always-apply routing and safety copy and broadens conditional architecture reads into unconditional ones. Cursor has no live inbound dependency outside the baseline-stamped audit evidence, but its unique safety wording must be accounted for before deletion.
6. `.agents/skills/commit-workflow/SKILL.md` says post-PR sync targets `main`; the canonical workflow targets `dev` and forbids touching `main` without an explicit release or hotfix. Routing cannot be declared branch-safe while that pointer is wrong (`13-sol-independent-review.md:325-339`).

## 2. Target routing architecture

### Claude chain

```text
CLAUDE.md (Claude router + universal kernel)
  -> Backend/CLAUDE.md or Frontend/quran-dashboard-ui/CLAUDE.md, only if that area is touched
  -> nearest relevant README before changing that area
  -> only the exact CODING_PRINCIPLES.md heading or specialist source whose trigger matches
  -> relevant code
```

Claude never reads an `AGENTS.md` as part of repository routing.

### Sol/Codex chain

```text
AGENTS.md (Sol/Codex router + functionally equivalent universal kernel)
  -> Backend/AGENTS.md or Frontend/quran-dashboard-ui/AGENTS.md, only if that area is touched
  -> nearest relevant README before changing that area
  -> only the exact CODING_PRINCIPLES.md heading or specialist source whose trigger matches
  -> relevant code
```

Sol/Codex never reads a `CLAUDE.md` as part of repository entrypoint routing. Existing `.agents/skills/*` adapters remain a separate on-demand Skill mechanism; redesigning those adapters is not part of V2.

### Entrypoint content rule

Each root entrypoint contains only:

- its supported-agent identity and precedence rule;
- the tiny universal kernel: stay within requested scope; treat `main` as protected Railway production and never modify or commit to it directly; never invent or silently correct Quran data, preserve provenance, and never mutate source resources without explicit authority; load the native area router and nearest relevant README before changing an area, and update that README in the same change if its described truth changes; and do not commit, push, open/sync a PR, run formal review, or deploy unless requested;
- its native Backend/Frontend route;
- the nearest-README discovery order;
- short trigger-to-path pointers for implementation, tests, Git/deployment, review, and Spec Kit work;
- short compatibility headings using the legacy labels `Branching workflow`, `Workspace Path Conventions`, and `Comments are forbidden by default`; each points to its canonical owner without restating it;
- the existing `<!-- SPECKIT START -->` state block, unchanged in behavior for this plan.

Each area entrypoint contains only a trigger table pointing to the nearest README and existing canonical architecture/testing/product sources. It must not repeat the root kernel or restate those sources' rule bodies. The Claude and Sol/Codex routers must be functionally equivalent, but they are not maintained as byte mirrors and contain only native paths.

No entrypoint should carry test commands, test cadence summaries, detailed comment policy, Quran-data lists, migration procedures, API response rules, localization rules, report naming rules, or design doctrine.

## 3. Exact implementation file set

### Core routing files

| File | Exact responsibility |
|---|---|
| `CLAUDE.md` | Replace the full book with the small Claude-native router plus the universal kernel in §2. Keep no route to `AGENTS.md`; route `CODING_PRINCIPLES.md` by trigger and exact heading, route test authoring/review to Claude `test-guard`, and retain the current Spec Kit state block without redesigning it. |
| `AGENTS.md` | Replace the full book with the small Sol/Codex-native router plus a functionally equivalent universal kernel. Route only to area `AGENTS.md` files, triggered exact headings in neutral canonical sources, and existing native `.agents` adapters; do not invent a clean-code Skill adapter. |
| `Backend/CLAUDE.md` | Replace repeated Backend law with a Claude-native trigger table. |
| `Backend/AGENTS.md` | Replace the mirror with a Sol/Codex-native trigger table; no root or area Claude reference. |
| `Frontend/quran-dashboard-ui/CLAUDE.md` | Replace repeated Frontend law with a Claude-native trigger table. |
| `Frontend/quran-dashboard-ui/AGENTS.md` | Replace the mirror with a Sol/Codex-native trigger table; no root or area Claude reference. |
| `CODING_PRINCIPLES.md` | Remain a conditional neutral source, not a mandatory full read. Move the full root comment policy into `### Comment Policy` under §2; strengthen existing focused-change, Quran-safety, clean-code, and Definition-of-Done headings only enough to retain removed protections, including `git diff --check` and the Cursor-only Quran/source cases. Do not move the full branch workflow here: the roots retain the small `main`/production kernel, native `commit-workflow` owns Git actions, and `Backend/README.md` owns deployment truth. Remove root-Claude routing and the stale root-Impeccable claim; use neutral document pointers or native Skill names. Do not otherwise expand, reorder, or clean up this document. |
| `TESTING_STRATEGY.md` | Change only the §1 description of the six entrypoints: native routers point to this canonical strategy and do not carry a second test policy. Do not change lanes, cadence, triggers, or commands. |
| `.cursor/rules/always-read-agents.mdc` | Delete after the protection and inbound-reference gates pass. This retires Cursor; it is not converted to a stub. |
| `.claude/skills/commit-workflow/SKILL.md` | Remove the backward pointer to the root `CLAUDE.md` Branching section; this Skill's own Branch model remains canonical and unchanged. |
| `.agents/skills/commit-workflow/SKILL.md` | Correct the two `sync-to-main` phrases to `sync-to-dev`. Make no other Skill change. |

### Strictly required pointer repairs

These are routing repairs, not README cleanup. For every file below, change only the named ownership text or references; preserve all unrelated headings, wording, and formatting:

| File | Exact repair |
|---|---|
| `docs/README.md` | Own the shared planning-artifact lifecycle/per-file gate now removed from the roots, including the exact long-lived survivor list. Preserve the rule that no Frontend report-folder convention exists and one must not be invented without an explicit decision. Describe `CLAUDE.md` and `AGENTS.md` as native routers rather than law books. |
| `specs/README.md` | Point lifecycle details to `docs/README.md`; retain only Spec Kit-specific close steps and the existing dual state-block instruction. Do not redesign Spec Kit state. |
| `Backend/report/README.md` | Point shared lifecycle rules to `docs/README.md` and route agents through their native Backend entrypoint. Preserve report-specific exceptions and evidence rules. |
| `Backend/README.md` | Make `## Invariants` the neutral owner of the existing EF migration safety: EF tooling only, explicit request before adding/applying a migration, no hand-edited generated migration/snapshot except a documented exceptional fix, and the existing post-generation report fields (name, files, build/test status, database-update run/skip). Do not alter deployment truth. |
| `Backend/scripts/README.md` | Replace the `Backend/CLAUDE.md` migration pointer with `Backend/README.md#invariants`. |
| `Backend/tools/QuranDashboard.DataImporter/README.md` | Replace the `Backend/AGENTS.md` source/report pointer with `CODING_PRINCIPLES.md#10-quranic-data-safety`, this README's `Defaults & sources`/`Safety` sections, and `Backend/report/README.md` as applicable. Move the area-entrypoint details here: imports use staged/canonicalized packages under `resources/import-sources/`, never random upstream folders when staging is required, and upstream folders are provenance/read-only inputs unless staging is explicitly requested. |
| `Backend/api/QuranDashboard.Api/Controllers/README.md` | Replace both root-Claude comment-policy references with `CODING_PRINCIPLES.md#comment-policy`. |
| `Backend/.architecture/API_GUIDELINES.md` | Remove the obsolete reverse pointer at current line 175 that says localization rules live in the entrypoints; §10 remains the canonical owner. Change no API rule. |
| `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md` | Replace the planning-history `CLAUDE.md` pointer with `docs/README.md`'s lifecycle heading. Change nothing else in this large README. |
| `SKILLS_AND_ARCHITECTURE_GUIDE.md` | Update only the current entrypoint rows and normal-implementation/self-check routing at lines 50–51, 122, 209–210, 233, and 293–297. Remove the mirror claim; do not audit or redesign Skills. |

No other file is expected to change. Baseline-stamped reports under `docs/project-simplification-audit/` remain historical evidence and are not rewritten when they mention the retired Cursor path or old entrypoint sizes.

## 4. Always-read and on-demand boundaries

### Always-read for Claude

- Root `CLAUDE.md` only. Report 14 confirms this is the one repository file injected into every Claude session (`14-claude-memory-context-review.md:23-40`).
- It contains routing, precedence, and the tiny universal kernel from §2, not the downstream rule bodies.

### Always-read for Sol/Codex

- Root `AGENTS.md` only.
- It contains the equivalent native routing contract and a functionally equivalent universal kernel, and never points into the Claude entrypoint chain.

The two kernels must pass a semantic checklist, not a byte-identity check: requested scope only; `main` is protected Railway production and receives no direct modification or commit; no invented or silently corrected Quran data and no source-resource mutation without explicit authority, with provenance preserved; native area plus nearest-README discovery and same-change README maintenance when described truth changes; and no unrequested commit, push, PR, formal review, or deployment action.

### Conditional/on-demand

| Trigger | Load |
|---|---|
| Any folder change | The native area entrypoint, when applicable, and the nearest relevant README before editing. The root kernel already enforces scope and production safety; do not load all of `CODING_PRINCIPLES.md`. If behavior, commands, boundaries, data invariants, API/URL contracts, or test rules described in the README change, update that README in the same change. |
| Production-source implementation | Only the implicated headings of `CODING_PRINCIPLES.md`: §2 Clean Code, §3 SOLID, §4 DRY/KISS/YAGNI, or §7 Focused Changes as the change requires. Load §2 `Comment Policy` when changing production-source code; never read the document wholesale merely because a file changes. |
| Active phased/spec/contract-bound implementation | The active spec/task/contract plus the native Spec Kit Skill only when invoked. The root kernel supplies the ordinary scope boundary; the active artifact and relevant contract owner supply exact phase/schema values. Stop and report before broadening either. |
| Any Backend path | The native Backend entrypoint. Load `BACKEND_STRUCTURE.md` + `CLEAN_ARCHITECTURE.md` only for their current add/move/structure triggers; do not narrow those triggers in V2. |
| API endpoint/contract/middleware/configuration work | `Backend/.architecture/API_GUIDELINES.md`; add the security route below when auth/access is involved. |
| Logging, diagnostics, or importer/report output | `Backend/.architecture/LOGGING_GUIDELINES.md`. |
| Auth/access/Owner/identity work | `docs/contracts/security-access.md`, then only its directly implicated README(s); API route/security changes also load `API_GUIDELINES.md` §11. |
| Quran import/generation/source work | `CODING_PRINCIPLES.md` §10 and the nearest pipeline/DataImporter README. Provenance, refusal, rollback, hash/manifest, and source-mutation rules remain mandatory. |
| EF migration/schema work | `Backend/README.md` §Invariants, `Backend/scripts/README.md`, and the migration sections of `TESTING_STRATEGY.md`; applying a migration still requires explicit authority. |
| Frontend component/route/state organization | `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md`. |
| Frontend visual/style work | The relevant headings of `UI_STYLE_SYSTEM.md`; any narrowed partial read must include Typography, RTL and Direction, Quranic Data Display Safety, and the implicated token/component-contract heading. V2 creates no replacement style card. |
| New UI surface, product behavior, user-facing copy, or visual-direction change | `PRODUCT.md` and `DESIGN.md`. Routine non-product code edits do not load them. |
| Frontend API/data-access integration | `API_INTEGRATION_GUIDELINES.md`; add `security-access.md` for auth/session/permission behavior. |
| Selecting, running, or reporting tests | `TESTING_STRATEGY.md` §§1–2 and §5 plus the relevant lane section; also §3.2 for Backend output/hang safety, §3.3/§3.4 for database/canonical work, §6 for route/auth/binding, §§7–10 for build/no-CI/failure/workflow ownership, and §11 for browser E2E. Read the nearest test README. Do not substitute an entrypoint summary for these sections. |
| Writing or reviewing test code | The native `test-guard` Skill and only its stack-relevant reference, in addition to test-selection routing. This is test-quality guidance, not permission to change cadence. |
| Pre-delivery implementation self-check | Only `CODING_PRINCIPLES.md` §12 Definition of Done and any production-code headings already implicated above, plus the relevant Backend/Frontend structure document's file-size-threshold heading when that threshold applies. Do not load the whole principles document, load the deeper clean-code pack, or run a formal engineering review unless that review is actually requested. |
| Git branch/stage/commit/push/PR sync | The agent's native `commit-workflow` Skill. It must resolve to `dev`, never `main`, unless the user explicitly requests a release/hotfix. |
| Deployment/runtime smoke | `Backend/README.md` §Deployment and the native `deploy-smoke` Skill. Keep environment/DB-target confirmation and explicit migration authority. |
| Formal review, performance review, or Spec Kit | The explicitly invoked native Skill and its own scoped references. V2 does not change those Skill bodies or their read closures. |

An ordinary existing-code Backend or Frontend implementation should therefore reach code after about three or four sources: native root, native area router, nearest README, and at most the exact `CODING_PRINCIPLES.md` heading needed before editing. Some checks, including Definition of Done, are loaded at their real pre-delivery trigger rather than before code. A specialized or protected task may exceed four because its protection-bearing canonical source is required; it must not be trimmed to satisfy a count.

## 5. Safety ownership after the move

| Protection | Canonical/reachable owner after V2 | Proof required before removing the old copy |
|---|---|---|
| Branch, Git authority, and production boundary | Universal root kernel for the always-visible `main`/Railway-production and no-unrequested-action boundary; native `commit-workflow` for Git actions; `Backend/README.md` for deployment detail | Both roots prohibit direct modification/commit on `main`; new work and PR base are `dev`; post-PR sync is `dev`; `main` moves only for an explicitly requested release or hotfix; staging is explicit; commit/push require user authority. |
| Quran and source data | Universal root kernel for the no-invention/no-silent-change baseline; `CODING_PRINCIPLES.md` §10 + nearest importer/pipeline README only for Quran/source tasks; specialist review reference stays unchanged | Every Cursor-only item is mapped: data types/counts, no silent correction, obvious synthetic placeholders/traceable fixtures, local-resource test gating, resources not modified/committed, staged/canonical packages, upstream provenance/read-only treatment, provenance and uncertainty. |
| Auth/security | `docs/contracts/security-access.md` → Authentication/Core/Access README and `API_GUIDELINES.md` §11 | Auth probe preserves Bearer access-token identity, separate signed ID-token evidence, expected-sub binding (`sub` for provisioning, `newSub` for relink), and `email_verified=true`. |
| Test selection/operations | `TESTING_STRATEGY.md` exact headings | Trigger matrix, route-smoke parity, data-tier separation, canonical fail-not-skip, one-PostgreSQL ownership, visible output, and no-CI reporting remain reachable. |
| Local instruction discovery | Both small native roots and area routers | Tiny-task probes select the native area router and nearest README before any specialist document and do not fan out to unrelated READMEs. |
| Scope, phase, contracts, and delivery | Universal root kernel for ordinary scope/no-unrequested-action discipline; active specs/contracts and native Spec Kit Skill when applicable; `CODING_PRINCIPLES.md` §12 only at pre-delivery; file-size thresholds remain in the relevant structure doc | No future phase behavior or unapproved contract/schema values; changed README updated with behavior; final report states scope, contract, threshold, and source-data checks. |
| Code/test quality self-checks | Exact relevant `CODING_PRINCIPLES.md` headings for changed production code; native `test-guard` for written/reviewed tests | Existing comment/formatting, naming/functions, SOLID, DRY/KISS/YAGNI, AI-failure, behavior-not-implementation, real-boundary mocks, data-driven variants, no framework-guarantee tests, real DTO/entity/value-object construction, real persistence infrastructure, and Quran-test-data checks remain on demand rather than always read. No nonexistent clean-code adapter is introduced and the full principles document is not a default read. |
| Comment policy | `CODING_PRINCIPLES.md` §2 `Comment Policy` | The three-part exception, production-only scope, excluded test/tool/generated paths, directive exemptions, and README destination all survive verbatim in substance. |
| Planning/report lifecycle | `docs/README.md`, with local specialization in `specs/README.md` and `Backend/report/README.md` | Per-file gate, evidence-to-test rule, inbound-reference gate, deletion timing, exact survivor list, no invented Frontend report convention, and surviving-evidence exception remain reachable from a planning/report task. |
| EF migrations | `Backend/README.md` §Invariants + `Backend/scripts/README.md` | No hand-written/generated-file edits; add/apply only with explicit request; after generation report the migration name, generated files, build/test result, and whether database update ran or was skipped. |

## 6. Small implementation sequence

### Step 1 — Freeze the protection and reference map

- [ ] From the repository root, run `rg -n --hidden '(CLAUDE\.md|AGENTS\.md|always-read-agents\.mdc)' . --glob '!.git/**' --glob '!docs/project-simplification-audit/**'` and classify every live inbound reference to the six entrypoints and Cursor rule.
- [ ] Check every rule in the safety table above against its named destination. Stop if any old rule has no exact destination.
- [ ] Write the five-item kernel checklist from §4 into the implementation notes and use it as the semantic acceptance check for both roots; do not create a shared kernel file.
- [ ] Confirm the intended diff is limited to the file set in §3 and that every non-entrypoint hunk is one of the exact ownership moves or pointer repairs named there.

### Step 2 — Establish neutral owners before shrinking entrypoints

- [ ] In `CODING_PRINCIPLES.md`, move the canonical full comment policy into `### Comment Policy` under §2 and amend only the existing focused-change, Quran-safety, clean-code, and Definition-of-Done headings needed to receive removed protections. Add the missing Cursor Quran/source cases and `git diff --check`; replace Claude-only pointers. Do not move the full branch workflow here, make the document an unconditional route, create a clean-code Skill adapter, or clean up unrelated prose.
- [ ] Make `docs/README.md` the shared planning-lifecycle owner and apply only the named lifecycle pointer repairs.
- [ ] Make `Backend/README.md` §Invariants the neutral EF migration-safety owner and repair its direct dependents.
- [ ] Remove the canonical commit-workflow Skill's backward root pointer and correct `sync-to-main` to `sync-to-dev` in the Sol/Codex pointer; change no Git behavior.
- [ ] Update `TESTING_STRATEGY.md` §1 only to describe native routers and canonical test ownership.

### Step 3 — Replace all six entrypoints atomically

- [ ] Replace root `CLAUDE.md` and `AGENTS.md` with their native router shapes and functionally equivalent five-item kernels from §§2 and 4. Small kernel duplication is intentional; do not enforce byte identity.
- [ ] Replace both Backend entrypoints with native trigger tables using the current triggers and destinations in §4.
- [ ] Replace both Frontend entrypoints with native trigger tables using the current triggers and destinations in §4.
- [ ] Preserve the current Spec Kit state blocks without attempting to unify or redesign them.
- [ ] Apply the exact neutral-document pointer repairs in §3; do not touch unrelated prose.

### Step 4 — Retire Cursor and verify the final graph

- [ ] Re-run the inbound scan. Historical audit references may remain; any live dependency blocks deletion.
- [ ] Confirm all Cursor-only protections are reachable through `CODING_PRINCIPLES.md`, `TESTING_STRATEGY.md`, or a named nearest README.
- [ ] Delete `.cursor/rules/always-read-agents.mdc` and confirm no tracked Cursor routing file remains.
- [ ] Run the static checks and the six focused probes below against the final cumulative diff.

## 7. Focused verification and routing probes

### Static checks

1. `git diff --check` passes.
2. `rg -n 'CLAUDE\.md' AGENTS.md Backend/AGENTS.md Frontend/quran-dashboard-ui/AGENTS.md` returns no route; the converse `rg -n 'AGENTS\.md' CLAUDE.md Backend/CLAUDE.md Frontend/quran-dashboard-ui/CLAUDE.md` also returns no route.
3. A live-reference search outside the audit pack finds no dependency on `.cursor/rules/always-read-agents.mdc` and no neutral README that routes one supported agent through the other agent's entrypoint.
4. `git ls-files '.cursor/**'` returns no path.
5. `git diff --name-only` contains only §3's allowlist plus this already-approved plan artifact.
6. A manual rule-destination comparison accounts for every removed root/area heading; green links alone do not prove rule preservation.
7. A semantic kernel comparison confirms both roots contain all five kernel protections, while `rg -n 'read.*CODING_PRINCIPLES|CODING_PRINCIPLES.*before (any|every)'` over the six entrypoints finds no unconditional full-document route.
8. Review every non-entrypoint diff hunk against its single §3 responsibility; any cleanup, reformatting, or unrelated prose change fails the allowlist even when the file itself is allowed.

The three root compatibility headings retain their legacy labels. Existing Claude-native references from `speckit-git-feature` and engineering-review therefore land on a short pointer to the canonical owner rather than becoming dangling links; they do not require Skill edits. The canonical commit-workflow backward pointer is removed because otherwise Git routing would be circular.

### Read-only agent probes

Run each probe in a fresh Claude session and a fresh Sol/Codex session where applicable. Ask the agent to return only `files_before_code`, `conditional_files`, and the controlling safety statement; it must not edit files.

| Probe | Expected route/result |
|---|---|
| Tiny existing Backend logic fix | Native root + native Backend router + one nearest README before code; add only the implicated `CODING_PRINCIPLES.md` heading, such as `Comment Policy`, when its trigger applies. It stops before editing if the branch is `main`. No opposite-agent file, Frontend file, API guide, full principles file, full test strategy, or unrelated README. |
| One existing Frontend component visual adjustment | Native root + native Frontend router + nearest feature README + only the implicated `UI_STYLE_SYSTEM.md` headings, including Typography/RTL/Quran display safety. Load only a triggered `CODING_PRINCIPLES.md` heading, never the full file. No API guide or `PRODUCT.md`/`DESIGN.md` unless the prompt changes product/copy/direction. |
| `/api/access/me` provisioning or subject-relink change | Routes through `security-access.md`, the Authentication README, and `API_GUIDELINES.md` §11. States the two-token and expected-sub rules exactly; test selection reaches `TESTING_STRATEGY.md` §§5–6 and reports the Smoke data-tier status separately. |
| Quran importer test-fixture change | Routes through `CODING_PRINCIPLES.md` §10 and the nearest importer/pipeline README. Refuses authentic-looking invented Quran data, permits only obvious synthetic placeholders or traceable fixtures, and preserves staged-source provenance. |
| "Start a feature, open its PR, then the PR merged; sync and push" | Invokes the native commit workflow: branch from `dev`, target the PR to `dev`, sync `dev` after merge, never move `main` absent an explicit release/hotfix, and push only because the prompt explicitly requests it. It identifies `main` as Railway's production auto-deploy branch. This must pass for both Claude and Sol/Codex. |
| Deployment smoke with an unconfirmed or non-local database target | Routes to `Backend/README.md` §Deployment and native `deploy-smoke`; displays/confirms the target and stops before applying migrations without explicit authority. |

Do not recompute or advertise a token-saving percentage as proof. The acceptance evidence is the resolved routing graph, protection reachability, the ordinary three-to-four-source heading-scoped probe, and the absence of cross-agent/Cursor paths.

## 8. Explicit non-goals

- No custom Skill consolidation or redesign, including `.agents/skills/speckit-*` forks.
- No engineering-review, performance-review, test-guard, or test-strategy redesign.
- No test cadence, lane, trigger, test-file, test-count, or runtime change.
- No README shortening or content cleanup beyond the exact pointer/ownership repairs in §3.
- No opportunistic cleanup, restructuring, or prose rewrite in `CODING_PRINCIPLES.md`, Skills, READMEs, architecture documents, or `TESTING_STRATEGY.md`; their only allowed edits are the exact §3 ownership moves and pointer repairs.
- No architecture, API, styling-system, production feature, schema, migration, or database change.
- No Spec Kit state-model or integration redesign; the existing root state blocks remain for a later dedicated plan.
- No persistent Claude, Codex, or model-memory mutation.
- No changes to baseline audit reports, reports 01/11 synthesis, or audit measurement JSON.
- No token/byte/line percentage target.

## 9. Stop conditions

Stop implementation and report the exact blocker if any of the following occurs:

- A rule being removed cannot be mapped to one named canonical file and heading.
- A supported-agent route still requires the other agent's entrypoint.
- A live inbound dependency on the Cursor rule exists outside the baseline-stamped audit pack.
- Cursor contains a protection not yet present on a Claude and Sol/Codex implementation path.
- Test routing would replace `TESTING_STRATEGY.md` with an incomplete summary or omit a relevant §§1–2/§3.2/§3.3/§3.4/§5/§6/§§7–10/§11 route.
- Branch/deployment, auth, Quran/source, migration, or nearest-README probes fail.
- The change requires altering a Skill body beyond the two branch-pointer files named in §3, changing Spec Kit behavior, or changing test cadence.
- The exact file allowlist in §3 is insufficient; add nothing opportunistically. Record the newly discovered live dependency and revise this plan first.
- Either root omits one of the five universal-kernel protections, or their semantics diverge even though their wording may differ.
- Any of the six entrypoints makes the full `CODING_PRINCIPLES.md` an unconditional read, or an ordinary tiny-task route cannot stay within about three or four sources using heading-scoped reads without dropping a real protection.
- A non-entrypoint file requires a change beyond its exact §3 ownership move or pointer/reference repair.
- The `41.7%` scenario estimate is proposed as an observed result, guarantee, or acceptance gate.
