# 05 — Memory & Retrieved Context Audit (Audit D)

- **Brief section:** 13 (Audit D — Memory & Retrieved Context), with sections 1, 4, 25, 29, 30, 32 applied.
- **Baseline:** branch `dev`, HEAD `72792ba9` ("Merge access catalogue readiness feature"), audit date 2026-08-08.
- **Mode:** read-only. Nothing was modified, including the persistent-memory directory.
- **Inputs:** `data/instruction-inventory.json`, `data/history-evidence.json`, `data/skill-inventory.json`, plus independent re-verification of every load-bearing claim directly against the repository, git history, and the memory directory this session can actually read.
- **Author's access statement:** this audit session runs with a file-based persistent memory at `/home/mohamed/.claude/projects/-projects-Dashboard-App/memory/`. That directory was read (read-only) and is inventoried below as the *only* model/persistent memory the tool environment actually exposes. No other model memory is accessible, and none is invented (§7).

The brief requires three categories to stay strictly separate. This report holds that line throughout:

| Category | What it is here | Where treated |
|---|---|---|
| **Repository instructions** | `AGENTS.md`/`CLAUDE.md` chains, READMEs, `.architecture/**`, Skills bodies | Reports 03, 04, 06 — *not* re-audited here; referenced only where a config artifact routes into them |
| **Tool/config context** | `.claude/` settings artifacts, `.cursor/` rules, `.codex/`, `.specify/` integration config, `.agents/` adapters, local git excludes, codex git refs | §3 of this report |
| **Model/persistent memory** | The five files in the exposed memory directory, and nothing else | §4–§5 of this report |

---

## 1. Headline findings

1. **CONFIRMED — the repository contains no memory system.** No mem0, no auto-save, no retrieval configuration, no memory-injection hooks anywhere in the tracked tree (§2). The orientation claim in the brief (§13: no hits for `mem0` / `auto_search`) is verified and extended.
2. **CONFIRMED — `.claude/settings.local.json.bak` is stale noise**, the sole survivor of a two-stage removal of a June "lean-ctx" tool integration. It is the *only tracked settings artifact* in the repo, unchanged since creation on 2026-06-26, and its allowlist contains commands the current testing law explicitly names as lane bypasses. Adjudication with full evidence in §3.2. One detail in `data/history-evidence.json` is corrected (§3.2.1).
3. **CONFIRMED — actual persistent memory is tiny and largely healthy**: 5 files, 6,174 bytes total, zero status chatter, zero session bookkeeping. One file is a self-labeled throwaway (`DELETE`), one has drifted partly into repo-derivable content with a dangling pointer (`MERGE`), two are model examples of policy-compliant durable memory (`KEEP`). Inventory and per-file verification in §4.
4. **CONFIRMED — one memory references a skill that no longer exists on `dev`**: `fix-agent-context-threshold` anchors to `speckit-phase-loop`, which was added on 2026-07-25 (`b5447500`) and survives only via the `archive/abwab-attempt-1` tags — the commit is not an ancestor of `dev` (§4.3).
5. **CONFIRMED — Codex CLI is an active, current consumer of this repo**: six `refs/codex/turn-diffs/checkpoints/*` tree refs dated 2026-08-05 → 2026-08-07 UTC (through 2026-08-08 00:37 local UTC+3) exist in `.git`, upgrading the instruction inventory's "LIKELY recent-but-light" Codex usage to confirmed recent activity (§3.5). This matters for tool-config decisions elsewhere (Audits B/C): the `AGENTS.md`/`.agents` surface has a live consumer.
6. **CONFIRMED — no repo config injects memory or context on every task for Claude.** The per-task injections that do exist are: the Claude harness's own CLAUDE.md auto-load behavior (repository instructions, Audit B's subject), Cursor's `alwaysApply: true` rule (Cursor sessions only), and environment-level items outside the repo (user-global CLAUDE.md, the 552-byte auto-memory index, plugin skill listings) that are observable in this session but configured elsewhere (§6).
7. **Honest scale statement:** this audit area is *not* a major recurring-cost center. Total persistent memory is ~6 KB; the per-session injected index is 552 bytes. The memory-adjacent cost that actually hurts lives in instruction files and skills (reports 03/04). The value of this report is hygiene, correctness of the record, and the evidence base for the later independent Claude/Sol memory reviews (brief §13, §28).

---

## 2. Repository/config search for memory systems

### 2.1 What was searched

Independent searches at HEAD (excluding `node_modules`, `.git`, build output, and the audit's own output folder):

- `mem0`, `auto_search`, `auto-save`, `autosave` — case-insensitive, whole tree: **zero hits**.
- `\bmemory\b` across `.claude/`, `.cursor/`, `.specify/`, `.agents/`, root `CLAUDE.md`/`AGENTS.md`: every hit is incidental prose — performance-review discussion of RAM/leaks (e.g. `.claude/skills/performance-backend-review/SKILL.md:135`), "working memory"-style phrasing (e.g. `.claude/skills/speckit-clarify/SKILL.md:170`), or Spec Kit's *constitution* path `.specify/memory/constitution.md`, always guarded with "IF EXISTS" (e.g. `.claude/skills/speckit-plan/SKILL.md:62`, which itself states "this repository has no constitution"). None is memory-system configuration.
- **`.specify/memory/` does not exist on disk** — verified by directory listing. The Spec Kit "memory" is an unpopulated config convention, not a memory system.
- Hook configuration: the only `hooks` in repo config are `.specify/extensions.yml:4` — Spec-Kit *git workflow* hooks (auto-commit before/after speckit commands, `auto_execute_hooks: true` at `.specify/extensions.yml:3`). They automate commits around Spec Kit invocations; they inject no memory and no context, and run only when a speckit skill runs.
- `.claude/settings.local.json` (live, untracked) and `.claude/settings.local.json.bak` (tracked): both contain a `permissions.allow` list only — no hooks, no context injection.
- No `.mcp.json`, no tracked `settings.json`, no `.github/copilot-instructions.md`, no Windsurf/Cline/Aider/Roo config files anywhere in the tracked tree.

**Conclusion: CONFIRMED — the repository and its tracked tool config contain no memory integration of any kind.**

### 2.2 Scope limits of that conclusion (UNKNOWN boundary)

The search proves absence *in the repository tree* only. It cannot rule out:

- memory or retrieval systems configured at user level (`~/.claude/settings.json`, plugin/MCP config) — not inspected; outside this report's granted access. **UNKNOWN.**
- the Claude harness's internal memory behavior beyond what it exposes as files. **UNKNOWN.**
- One indirect piece of evidence that an environment-level memory *nudge* exists: the user's global `CLAUDE.md` memory-write policy (loaded into this session) opens with "Ignore any 'store 1–3 memories per interaction' cadence from tool hooks". A policy written to override a hook implies the hook exists somewhere in the environment. Its location and configuration are not in the repo. **LIKELY (existence), UNKNOWN (location/config).**

What *is* directly observable: the harness surfaces the auto-memory index (`MEMORY.md`, 552 B) into every session's system prompt for this project, and stamps read memories with a "point-in-time observation" reminder. Both behaviors were observed first-hand in this session. **CONFIRMED (for this environment).**

---

## 3. Tool/config context inventory

### 3.1 `.claude/` tree (in-repo)

39 tracked files (`git ls-files .claude`): 38 are skill files (10 project skills + 15 speckit skills + reference packs — sized and audited in report 04), plus exactly one tracked settings artifact (`settings.local.json.bak`); a second, untracked settings file (`settings.local.json`) exists alongside it:

| File | Bytes | Tracked | Content | Status |
|---|---|---|---|---|
| `.claude/settings.local.json.bak` | 456 | **yes — the only tracked settings artifact** | `permissions.allow` (5 entries) | stale noise — adjudicated in §3.2 |
| `.claude/settings.local.json` | 540 | no (ignored by user-global git ignore, `~/.config/git/ignore:1`) | `permissions.allow` (6 entries) | live local config, out of audit scope for change |

No hooks, no memory config, no context injection in either file. **CONFIRMED.**

Local (unversioned) git excludes are themselves tool-config context worth recording: `.git/info/exclude` carries harness-state globs (`**/.claude/scheduled_tasks.json`, `**/.claude/routines/.state/`, `**/.claude/checkpoints/`, `**/.claude/mailbox/`, `**/.claude/agent-memory-local`, …). None of those paths currently exist under the repo's `.claude/`, except an empty `.claude/worktrees/` directory (mtime 2026-07-22), which contains nothing. They are defensive ignores for harness-local state, not evidence of active systems. **CONFIRMED (contents); the tooling that wrote them is UNKNOWN.**

### 3.2 Adjudication: `.claude/settings.local.json.bak`

The brief (§13) requires classifying this file as intentional reference / stale noise / misleading / harmless archive. `data/history-evidence.json` says **stale noise, CONFIRMED**. Independent re-verification agrees, with one correction and one nuance the data file missed.

**The verified life story:**

1. **Created 2026-06-26** by `ae4871d2` "add lean-ctx integration" — a commit that added a context-tooling integration: `.claude/settings.local.json.bak`, `AGENTS.md.bak`, `.cursorrules`, `.cursor/rules/lean-ctx.mdc`, `LEAN-CTX.md` (+9-line stanzas in `AGENTS.md`/`CLAUDE.md`); 7 files, 258 insertions. The `.bak` files read as backups the tool made of existing configs before rewriting them, swept into the commit by accident of scope — an origin inference git cannot prove: **LIKELY**, matching §3.4's treatment of the `.codex` origin. The commit facts themselves — creation in `ae4871d2`, its **only commit** (`git log --all` shows exactly one), byte-identical then and now (`diff` of `ae4871d2:` vs `HEAD:` blob is empty) — are **CONFIRMED.**
2. **Cleanup pass 1, 2026-07-10** (`1a9a3ef8` "Remove lean-ctx tooling from workspace docs"): deleted `.cursorrules`, `.cursor/rules/lean-ctx.mdc`, `LEAN-CTX.md` and removed the lean-ctx stanzas from `AGENTS.md`/`CLAUDE.md`. Missed both `.bak` files.
3. **Cleanup pass 2, 2026-07-14** (`ad9613b3`): deleted `AGENTS.md.bak` (110 lines), commit message calling it "the stray AGENTS.md.bak". Missed `.claude/settings.local.json.bak`.
4. **Since then: untouched for 43 days** at audit date, surviving two dedicated cleanup passes and multiple documentation sweeps.

**3.2.1 Correction to `data/history-evidence.json`:** its `settings_bak.cleanup` field states `1a9a3ef8` "deleted AGENTS.md.bak, .cursorrules, LEAN-CTX.md and .cursor/rules/lean-ctx.mdc". Verified against `git show --stat`: `1a9a3ef8` did **not** delete `AGENTS.md.bak` — it edited `AGENTS.md`. `AGENTS.md.bak` was deleted four days later by `ad9613b3`. The data file's overall adjudication (stale leftover, missed by cleanup) stands — indeed it was missed by *both* cleanup passes — but the commit attribution needed the correction. **CONFIRMED.**

**3.2.2 Nuance the data file missed:** the `.bak` content is a **strict subset of the live untracked `.claude/settings.local.json`** — the live file is the same five allowlist entries plus one addition (`dotnet run --launch-profile https`). So the `.bak` is not a divergent relic of foreign config; it is an old snapshot of the same local settings lineage. That makes it *useless as a reference* (the live file supersedes it entirely) while making its tracked status pure noise. **CONFIRMED (diff of `HEAD:` blob against the live file: one added line).**

**3.2.3 The misleading edge:** both the `.bak` and the live file allow direct `npx ng test --include=…` invocations (`.claude/settings.local.json.bak:5-6`). Current testing law explicitly forbids that pattern: `TESTING_STRATEGY.md:201` — "a direct `ng test` invocation bypasses both and is not a lane" — and `.cursor/rules/always-read-agents.mdc:174` — "`ng test` / `npx ng test` called directly BYPASS the cap". The `.bak` is the only *tracked, repo-visible* artifact carrying that forbidden pattern, so an agent inspecting tracked config could read it as sanctioned precedent. The live file shares the defect but is local-only and out of this audit's scope.

**Verdict: stale noise (CONFIRMED), with a mild misleading edge — not an intentional reference, not a harmless archive** (a harmless archive would not showcase a forbidden command pattern as the repo's only tracked permission example).

**Proposed simplification P1 — remove the tracked `.bak` (classification: `DELETE_CANDIDATE`; not executed in this audit):**

| Brief §4 question | Answer |
|---|---|
| 1. Value today | None. Superseded strict subset of the live local settings file; sole purpose was a tool's pre-rewrite backup in June. |
| 2. Dependents | None found: `grep -rn "settings.local.json.bak"` across the tracked tree matches nothing outside this audit's own folder. **CONFIRMED.** |
| 3. Risk if removed | Effectively zero — content is preserved in git history (`ae4871d2`) and in the live local file. |
| 4. Equivalent protection elsewhere | Yes: git history retains the blob permanently; the live `.claude/settings.local.json` carries the current allowlist. |
| 5. Smallest safe step | Delete the single tracked file (`git rm`) in a docs/chore commit — the same one-file pattern as `ad9613b3`'s removal of "the stray AGENTS.md.bak". |
| 6. Later verification | `git ls-files .claude` shows no settings artifact; repo-wide grep for the filename stays empty. |
| 7. Recurring cost removed | Small but real: 456 B of tracked noise, one misleading permission example visible to every agent that inventories `.claude/`, and the recurring "what is this?" cost this very audit just paid for the third time (two cleanup passes, one audit). |

### 3.3 `.cursor/` rules

One file: `.cursor/rules/always-read-agents.mdc` (9,387 B, `alwaysApply: true`, tracked, last touched 2026-08-06 — two days before HEAD; five commits 2026-06-08 → 2026-08-06 per `data/instruction-inventory.json`). For **Cursor sessions only**, this is genuine per-task context injection: the rule itself plus a mandated reading chain the instruction inventory measures at ~168 KB (~42k tokens) for any frontend task. It also contains the explicit routing rule "Do not rely on `CLAUDE.md` unless the user explicitly asks" (`always-read-agents.mdc:41`). Whether the Cursor editor is still actively used cannot be determined from the repo (**UNKNOWN**); the rule file is actively maintained (**CONFIRMED**). Cost treatment belongs to Audit B (report 03); recorded here as the one repo artifact that injects context on every task for a specific tool.

### 3.4 `.codex/` directory

Exists on disk, empty, untracked, never in any commit (`git log -- .codex` empty), mtime 2026-08-07. Consistent with a Codex CLI auto-creating its workspace directory on launch. **CONFIRMED (state); LIKELY (tool-created origin).** No context, no config, no memory. Nothing to simplify — an empty untracked directory is invisible to the repo.

### 3.5 New evidence: Codex checkpoint refs in `.git`

`git for-each-ref` reveals six tree refs under `refs/codex/turn-diffs/checkpoints/…` with embedded millisecond timestamps decoding to **2026-08-05T15:37Z through 2026-08-07T21:37Z** (UTC; the local UTC+3 rendering is 2026-08-05 18:37 → 2026-08-08 00:37) — the four days up to the audit baseline. These are checkpoint artifacts written by Codex-family CLI tooling into the repo's `.git` (refs only; no tracked files). This **upgrades the instruction inventory's "Codex CLI usage: LIKELY recent-but-light" to CONFIRMED recent activity** (the ref namespace and naming convention make the tool identity LIKELY; the refs' existence and dates are CONFIRMED). Downstream consequence for Audits B/C: proposals that treat the `AGENTS.md` entrypoints or `.agents/` adapters as dead weight must account for a live non-Claude consumer active days before the audit.

### 3.6 `.specify/` integration config

| File | Finding |
|---|---|
| `.specify/integration.json` | `installed_integrations: ["claude"]`, `default_integration: "claude"` — Claude is the only installed integration. **CONFIRMED.** |
| `.specify/init-options.json` | `ai: "claude"`, `ai_skills: true`. **CONFIRMED.** |
| `.specify/feature.json` | `feature_directory: ""` — no active feature, consistent with root `CLAUDE.md` "Active Spec Kit Feature: None". **CONFIRMED.** |
| `.specify/integrations/codex.manifest.json` | version 0.8.3, installed_at 2026-06-06 — a stale manifest for an integration no longer listed as installed (claude manifest is v0.14.2, 2026-07-27). **CONFIRMED stale**, matching `data/instruction-inventory.json`'s "superseded/stale" call. Note the tension with §3.5: the Spec-Kit *codex integration* is stale, while Codex *CLI usage* is current — two different things. |
| `.specify/extensions.yml` | git workflow hooks only (§2.1). Not context injection. **CONFIRMED.** |
| `.specify/memory/` | does not exist (§2.1). **CONFIRMED.** |

### 3.7 `.agents/` adapters

29 tracked files: 10 project-skill pointer adapters + `agents/openai.yaml` sidecars (2,661 B total) + 9 speckit near-full copies. Spot-verified against `data/skill-inventory.json`:

- The pointer-only invariant holds for the 10 project skills — sampled `engineering-review` ("this file exists only to route agents to the single source of truth") and `commit-workflow`. **CONFIRMED.**
- **CONFIRMED defect (misleading tool-config):** `.agents/skills/commit-workflow/SKILL.md` describes "the post-PR **sync-to-main** workflow" twice (front-matter description and body line "including Section 7, the post-PR sync-to-main workflow"), while the canonical `.claude/skills/commit-workflow/SKILL.md:128` defines "Post-PR sync to **dev**" and line 160 explicitly forbids it: "Do not switch to or sync `main` here." A non-Claude agent trusting the pointer's summary would be pointed at the exact operation the canonical skill prohibits — on a repo whose `main` auto-deploys (root `CLAUDE.md`, Branching workflow). Ownership of the fix belongs to Audit C (report 04); recorded here because it is the clearest current instance of *misleading* tool-config context, and §3.5 proves non-Claude consumers are active.
- The speckit `.agents` copies are full forks, not pointers (per `data/skill-inventory.json`, diff-verified there) — outside redesign scope (brief §31), noted as structural drift risk only.

---

## 4. Persistent memory inventory (the files the environment actually exposes)

Location: `/home/mohamed/.claude/projects/-projects-Dashboard-App/memory/` — 5 files, **6,174 bytes total**. The index (`MEMORY.md`) is injected into every session for this project; topic files load on demand. Every classification below is **evidence for the later independent Claude memory review (brief §13/§28) — nothing was deleted, merged, or edited by this audit.**

| # | File | Type (self-declared) | Bytes | Written / modified | Still true? | Better stored in repo? | Classification |
|---|---|---|---|---|---|---|---|
| 1 | `MEMORY.md` (index) | index | 552 | 2026-08-02 | yes — 3 entries, all pointing at live files | no — this is the memory system's own index | **KEEP** |
| 2 | `design-preview-flat-green-direction.md` | project | 1,905 | 2026-07-16 | **partly** — doctrine still true; one pointer dangles | **mostly already is** — see §4.2 | **MERGE** (trim to non-derivable residue) |
| 3 | `fix-agent-context-threshold.md` | feedback | 1,293 | 2026-07-26 | rule yes; its named anchor skill is gone from `dev` | no — user workflow preference, not repo fact | **KEEP** (with a caveat) |
| 4 | `local-https-dev-cert-mismatch.md` | reference | 2,002 | 2026-08-02 | **yes — fully re-verified** | no — machine-local environment facts | **KEEP** |
| 5 | `memory-system-smoke-test.md` | reference | 422 | 2026-07-10 | n/a — self-declared throwaway | n/a | **DELETE** |

### 4.1 `memory-system-smoke-test.md` — DELETE (CONFIRMED)

Self-description: "Throwaway smoke-test entry created to verify file-based memory writes work; safe to delete … No durable fact — delete anytime." Written 2026-07-10; 29 days old; **not listed in the `MEMORY.md` index** (the index has 3 entries; the directory has 4 topic files). This is exactly the non-durable noise the user's memory-write policy exists to purge, it says so itself, and deleting it requires no index edit. The only historical fact it carries — that the file-based memory system was verified working on 2026-07-10 — is not worth a standing file.

### 4.2 `design-preview-flat-green-direction.md` — MERGE (trim)

Verification against HEAD:

- **Doctrine still true:** flat parchment+green as light-theme law is recorded in the repo — `DESIGN.md:134` ("The allowed-green list (locked)"), `UI_STYLE_SYSTEM.md:654` (§16.3, same list), and the dark-theme-is-interim status at `UI_STYLE_SYSTEM.md:48-50` and `:418-419` ("dark interim-runs the prototype-derived navy + gold values … full dark reconciliation … open follow-up"). **CONFIRMED.**
- **Stale pointer:** the memory calls "the static comps under `docs/design-preview/`" the approved visual spec. That directory **was deleted on 2026-08-04** by `a675286d` "docs: delete the artifacts that competed with the READMEs". The pointer dangles. **CONFIRMED.**
- **Derivability:** the memory itself states that `DESIGN.md`, `PRODUCT.md` and `UI_STYLE_SYSTEM.md` all record the doctrine. Under the user's own policy ("if it can be found in code, git, reports, or specs, do not store it"; "never mirror a file that already exists"), the doctrinal body of this memory is now repo-derivable duplication.
- **Non-derivable residue worth keeping:** the machine-local caveat — `fs.inotify.max_user_watches=65536` is too low for `ng serve` watch mode (ENOSPC); raise it or serve `dist/` statically. That is a durable environment/setup fact (policy DO-store category) findable nowhere in the repo.

**Evidence-classification: MERGE** — the later memory review should trim this file to the environment caveat (plus, at most, a one-line pointer to `DESIGN.md`/`UI_STYLE_SYSTEM.md` §16 as the doctrine's home) and update the index line. The superseded-navy framing and the deleted `docs/design-preview/` reference should not survive.

### 4.3 `fix-agent-context-threshold.md` — KEEP (with caveat)

The rule: in Spec Kit phase loops, once an implementer agent passes ~350k cumulative tokens, route review-fix rounds to a fresh agent with a written handoff. This is durable *user feedback* about multi-agent workflow ("the user's judgment is that past ~350k that benefit is outweighed by degraded reliability") — squarely the policy's DO-store category ("workflow / tooling rules still in force"), and not derivable from the repo.

**Caveat (CONFIRMED):** the memory anchors to "`speckit-phase-loop` runs". That skill exists nowhere on `dev`: it was added 2026-07-25 by `b5447500` ("feat(skills): add speckit-phase-loop review-gated phase delivery"), which is **not an ancestor of `dev`** (`git merge-base --is-ancestor` fails) and is reachable only via the `archive/abwab-attempt-1` tags (2026-07-27, "dev tip before reset to main"). The memory was last modified 2026-07-26 — during that later-archived attempt. The rule itself outlives the skill (it generalizes to any phased implementer/reviewer loop, including the orchestration of this very audit), so the classification is **KEEP**, with a recommended one-line edit by the later memory review to de-anchor the rule from the archived skill name.

### 4.4 `local-https-dev-cert-mismatch.md` — KEEP

Every checkable claim re-verified at HEAD: `Frontend/quran-dashboard-ui/localhost.pem` and `localhost-key.pem` exist; `package.json:7` defines `start:https` with exactly those cert flags; `playwright.config.ts:40` defines the `webServer` pair the memory credits for booting both servers. The remaining content is machine-local environment fact (Chrome trust store, user-secrets location, no `mkcert`/`certutil` installed) that cannot live in the repo and is not derivable from it. This file — and the auto-memory index entry pointing at it — is the model of what the memory-write policy wants stored. **CONFIRMED, KEEP.**

### 4.5 `MEMORY.md` (index) — KEEP

552 bytes, 3 entries, all durable, all pointing at live files; injected each session at trivial cost. Two follow-on edits become due if the review executes §4.1/§4.2: no index change for the smoke test (it was never indexed), and a shortened line for the design memory.

---

## 5. Memory-write policy compliance

The user's global `CLAUDE.md` (loaded into this session; quoted as evidence of the active policy) forbids storing: status/progress ("PRs accepted, tests passed, 'committed and pushed'"), session bookkeeping, per-feature implementation narrative, verbatim copies of repo docs, and anything scoped to merged branches/features. It mandates storing only durable, reusable, non-repo-derivable facts.

**Compliance verdict on the current corpus (CONFIRMED):**

| Policy rule | Current state |
|---|---|
| No status/progress chatter | **Compliant** — zero entries of this kind across all 5 files |
| No session bookkeeping | **Compliant** — none |
| No per-feature narrative for merged work | **Compliant** — none |
| No mirrors of repo docs | **Mostly compliant** — the design memory (§4.2) has drifted into partial repo-derivability as the repo docs caught up with it; it predates none of the facts but now duplicates them |
| One fact per memory, durable, reusable | **Compliant** for #3 and #4; violated only by the self-labeled smoke test (#5), which predates nothing durable by design |
| Volume discipline ("most interactions store ZERO memories") | **Compliant** — 4 topic files accumulated across ~9 weeks of heavy multi-session development is strong restraint |

The corpus is what a purge-discipline policy looks like when it works. The two blemishes (smoke-test file, drifted design memory) are exactly what the scheduled independent Claude memory review (brief §28-B) exists to clear, and §4's table is that review's prepared evidence.

**This session's own artifacts:** per the same policy, nothing from this audit session qualifies for memory storage — audit findings, report paths, phase status, and completion state are all session bookkeeping / report-artifact content whose home is `docs/project-simplification-audit/` and git history. **The audit must leave zero new memories behind**, and this report records that obligation explicitly.

---

## 6. Is any config injecting memory/context on every task?

Strictly separated by what is observable where:

| Source | Injected on | Observable from | Verdict |
|---|---|---|---|
| Root `CLAUDE.md` (14,915 B) auto-load; `Backend/`- and `Frontend/`-`CLAUDE.md` on area work | every Claude session / area task | repo + observed in this session | **CONFIRMED** — but this is *repository instructions* (Audit B's subject, report 03), not memory |
| `.cursor/rules/always-read-agents.mdc` (`alwaysApply: true`) | every Cursor task | repo | **CONFIRMED** for the rule; whether Cursor is still used: **UNKNOWN** (§3.3) |
| `.specify/extensions.yml` hooks (`auto_execute_hooks: true`) | speckit command invocations only | repo | **CONFIRMED** — workflow automation, not context/memory injection |
| `.claude/settings.local.json{,.bak}` | never (permissions only, no hooks) | repo | **CONFIRMED** — no injection |
| Auto-memory index `MEMORY.md` (552 B) | every session for this project | memory dir + this session's own prompt | **CONFIRMED (this environment)** — the only true per-session *memory* injection, and it is 552 bytes |
| User-global `~/.claude/CLAUDE.md` | every session | observed in this session | **CONFIRMED present**; content is user policy, not repo-manageable |
| Plugin skill listings (superpowers, coderabbit, etc. — dozens of descriptions, incl. superpowers' "requiring Skill tool invocation before ANY response") | every session | observed in this session; **zero references anywhere in the repo** (grep confirmed) | **CONFIRMED present in this environment; configured outside the repo — configuration details UNKNOWN** and out of this audit's write scope |
| Memory-write nudge hook ("store 1–3 memories per interaction" cadence the user policy overrides) | unknown cadence | inferred from user policy text only | **LIKELY exists, UNKNOWN location** (§2.2) |

**Answer: no repo-side configuration injects memory on any task, and no repo-side configuration injects context on every task for Claude beyond the CLAUDE.md instruction chain that Audit B already owns.** The heavyweight per-task injection risk in this repo is instruction files, not memory — and for Cursor specifically, the `alwaysApply` rule chain (~168 KB for frontend tasks per `data/instruction-inventory.json`).

---

## 7. Honest boundaries

- **Model-internal memory beyond the five files in §4 is inaccessible and UNKNOWN.** This report claims nothing about it, per brief §13 ("Do not invent access to hidden/private model memory") and §32.
- User-level harness configuration (`~/.claude/settings.json`, plugin installs, MCP config) was **not read** — outside granted scope. Statements about environment-level injection in §6 rest solely on what this session's own prompt demonstrably contains.
- The memory files carry `originSessionId` metadata referencing sessions whose transcripts are not accessible; provenance beyond the files' own front-matter is **UNKNOWN**.
- Whether the Cursor editor and the lean-ctx tool are still in use cannot be determined from the repo: **UNKNOWN** (rule file maintenance and git refs prove *maintenance* and *Codex activity* respectively, not Cursor usage).

---

## 8. Proposed simplifications (summary)

All are propose-and-classify only; none were executed. P2–P4 are the prepared evidence base for the brief §28-B independent Claude memory review, which is the correct executor for memory changes; P1 is ordinary repo hygiene for a later remediation plan; P5 is recorded here but owned by Audit C.

| # | Item | Classification | §29 sensitive area? | Owner of later action |
|---|---|---|---|---|
| P1 | `.claude/settings.local.json.bak` (tracked) | `DELETE_CANDIDATE` (full 7-question analysis §3.2) | No | future remediation plan |
| P2 | `memory-system-smoke-test.md` | `DELETE` | No | Claude memory review (§28-B) |
| P3 | `design-preview-flat-green-direction.md` | `MERGE` (trim to environment caveat; drop dangling `docs/design-preview/` pointer) | No — the doctrine itself stays canonical in `DESIGN.md`/`UI_STYLE_SYSTEM.md` | Claude memory review (§28-B) |
| P4 | `fix-agent-context-threshold.md` | `KEEP` (one-line de-anchor from archived skill name recommended) | No | Claude memory review (§28-B) |
| P5 | `.agents/skills/commit-workflow/SKILL.md` "sync-to-main" misdescription | defect, misleading tool-config (fix, don't simplify) | Adjacent (branch/deploy safety — `main` auto-deploys) | Audit C / report 04 |

For P2–P4 jointly, the brief §4 questions in compact form: their current value is durable-fact recall across sessions (P2: none, self-declared); nothing in the repo depends on any memory file (memories point *at* the repo, never the reverse — verified by the repo-wide absence of any reference to the memory directory); the risk of the proposed changes is losing the inotify caveat or the 350k rule if trimming is done carelessly, mitigated by executing via the dedicated review with this report's per-file verification in hand; equivalent protection for P3's doctrinal content already exists in `DESIGN.md:134` and `UI_STYLE_SYSTEM.md` §16; the smallest safe steps are one file deletion, one file trim, one line edit; verification is re-listing the directory and re-checking the index's 3 pointers resolve; the recurring cost removed is small in bytes (~2.3 KB of stale/derivable memory text) but real in correctness — a dangling visual-spec pointer and an archived-skill anchor stop being re-loaded as if live.

**Not proposed:** no change to the memory system itself (there is none in the repo to change), no change to the auto-memory index mechanism (552 B/session is negligible and working), and no memory-related instruction changes (the user's global policy is already stricter than anything this audit would recommend).

---

## Mandatory questions answered

Brief §25 contains no numbered questions dedicated to Audit D (memory has no block in §25); the §13 obligations are answered in the table below. One §25 question is directly served by this report's evidence as independent corroboration:

| §25 Q | Answer |
|---|---|
| 20 — Are `.agents` adapters truly pointer-only? | **Corroborated for the 10 project skills: yes** — spot-verified samples declare themselves routing-only and contain no independent rule bodies (§3.7). Two qualifications, both CONFIRMED: the `commit-workflow` adapter's *summary text* misdescribes the canonical behavior ("sync-to-main" vs canonical "sync to dev", `.claude/skills/commit-workflow/SKILL.md:128,160`), and the 9 speckit `.agents` files are full forks, not pointers (outside redesign scope per brief §31). Primary owner: report 04. |

**§13 (Audit D) obligations:**

| Obligation | Answer |
|---|---|
| Keep the three categories separate | Done throughout — §0 table; repository instructions were deliberately *not* re-audited here (reports 03/04/06 own them). |
| Repo/config search: memory config, Mem0, auto-search, auto-save, project IDs/scopes, retrieval rules, per-task memory injection | **CONFIRMED none in the repository or its tracked tool config** (§2.1, §6); scope limits and the environment-level UNKNOWNs stated honestly (§2.2). Orientation's negative `mem0`/`auto_search` result verified and extended. |
| Stale feature/status context in memory | **CONFIRMED near-zero**: one throwaway smoke-test file (DELETE), one partially drifted design memory (MERGE); no status/branch/test-count/review chatter at all (§5). |
| Duplicated durable decisions | One instance: the flat-green doctrine exists in both a memory file and canonical repo docs; the repo docs are the correct owner and the memory should shrink to the non-derivable residue (§4.2). |
| `.claude` orientation — adjudicate `settings.local.json.bak` | **Stale noise, CONFIRMED** — two-stage lean-ctx cleanup missed it; unchanged since 2026-06-26; strict subset of the live local file; mild misleading edge via forbidden lane-bypass allows; `DELETE_CANDIDATE` with full 7-question analysis (§3.2). Data-file commit attribution corrected (§3.2.1). |
| Required future handoff (Claude memory review / Sol review) | This report supplies the prepared inputs: the per-file KEEP/MERGE/DELETE table with verification evidence (§4), the compliance analysis (§5), and the access-boundary statements (§7). The ready-to-use prompts themselves are report 12's deliverable. |
| No invented memory access; no chain-of-thought exposure | Honored — §7. |
| This session's artifacts must not become memory | Stated as an explicit obligation with policy citation (§5, final paragraph). |

---

## Measurement gaps

1. **Model-internal memory:** anything beyond the five exposed files is unmeasurable by design. **UNKNOWN** — the later Claude/Sol reviews must likewise report only what they can access.
2. **Environment-level configuration** (user `~/.claude/settings.json`, plugin installs, MCP servers, the inferred memory-nudge hook): existence of per-session injections is observable in this session's prompt, but their configuration, size, and cadence were not inspected — outside granted scope. **UNKNOWN / NEEDS_MEASUREMENT** (measurable only by a session with explicit access to user-level config).
3. **Cursor usage:** the `alwaysApply` rule is maintained, but no repo evidence proves the Cursor editor is still driving sessions. **UNKNOWN.** (Codex, by contrast, is CONFIRMED active via §3.5.)
4. **Actual per-session token cost of memory injection:** the 552-byte index is a static measure; the harness's exact injection format (reminders, wrappers) adds overhead not measurable from the repo. **NEEDS_MEASUREMENT**, though bounded and trivially small relative to instruction files.
5. **Memory provenance:** `originSessionId` values in memory front-matter reference inaccessible session transcripts; write-time context can't be audited. **UNKNOWN.**
6. **`refs/codex` writer identity:** the checkpoint refs' naming strongly indicates Codex CLI, but no repo artifact names the tool version or invocation. **LIKELY**, not CONFIRMED, on identity; dates are CONFIRMED.
