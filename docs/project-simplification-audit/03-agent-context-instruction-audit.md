# 03 — Agent Context & Instruction Loading Audit (Audit B)

- **Scope:** brief §10 (Audit B) + §7 (confirmed instruction duplication), mandatory questions §25 Q1–Q8.
- **Baseline:** branch `dev`, HEAD `72792ba9`, audited 2026-08-08.
- **Evidence base:** `data/instruction-inventory.json` (74 files, routing graph, 8 task traces, 17 duplicated rules), `data/history-evidence.json` (git archaeology), `data/markdown-decision-inventory.json` (law-duplication section), `data/skill-inventory.json` (skills as context consumers). Every load-bearing number below was independently re-verified against the working tree by this report's author (sha1, byte counts, line citations, and trace arithmetic recomputed); verification notes are inline.
- **Token rule:** tokens ≈ bytes/4 throughout, per the audit data convention. These are static full-file upper bounds; real agents may read partially (see Measurement gaps).
- **Mode:** audit only. This report proposes and classifies; it does not instruct implementation.

---

## 1. The instruction surface, in numbers

The full instruction/context surface an agent can be routed to is **74 files, 896,114 bytes, ~224,000 tokens** (`data/instruction-inventory.json` notes). CONFIRMED for the inventory contents; the five largest files were re-measured by this author with `wc -c` and match exactly.

| File | Bytes | ~Tokens | Role |
|---|---:|---:|---|
| `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md` | 103,970 | 25,992 | arch doc — largest single instructed read |
| `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md` | 97,772 | 24,443 | feature README (nearest-README target) |
| `SKILLS_AND_ARCHITECTURE_GUIDE.md` | 42,729 | 10,682 | meta guide — on no task path (see §8.5) |
| `Frontend/.../features/access-admin/README.md` | 42,594 | 10,648 | feature README |
| `docs/TESTING_DEBT.md` | 41,767 | 10,442 | debt ledger — on no mandatory task path |
| `TESTING_STRATEGY.md` | 33,427 | 8,357 | testing law — instructed on every test-running trace |
| `AGENTS.md` = `CLAUDE.md` (root pair) | 14,915 ×2 | 3,729 ×2 | byte-identical entrypoints |
| `Backend/AGENTS.md` = `Backend/CLAUDE.md` | 7,335 ×2 | 1,834 ×2 | byte-identical entrypoints |
| `Frontend/.../AGENTS.md` ≈ `CLAUDE.md` | 4,016 / 4,025 | ~1,005 ×2 | identical except the H1 title |
| `.cursor/rules/always-read-agents.mdc` | 9,387 | 2,347 | `alwaysApply: true` third routing copy |

Two structural facts frame everything else:

1. **The Claude harness auto-loads only the CLAUDE chain.** Root `CLAUDE.md` loads into every session; `Backend/CLAUDE.md` and `Frontend/quran-dashboard-ui/CLAUDE.md` load when work touches those directories. The AGENTS files are never loaded by the Claude harness — they exist solely for non-Claude agents, yet are maintained as byte mirrors of the CLAUDE files. CONFIRMED for this audit environment (observed directly in the audit session's own context; `data/instruction-inventory.json` notes).
2. **Deleting or shrinking AGENTS.md saves Claude zero runtime tokens.** Its cost is (a) double maintenance of every law edit, (b) drift risk, and (c) what non-Claude agents are made to read. This asymmetry matters for prioritization: the mirror problem is a *maintenance and correctness* problem first, a token problem only for Cursor/Codex sessions. CONFIRMED.

---

## 2. Why the duplication exists (brief §7 — history, CONFIRMED)

This section answers the brief's demand to determine *why* the duplication was created, not merely that it exists. All facts below are from `data/history-evidence.json` and were spot-verified by this author against `git show`/`git log` output.

### 2.1 Born identical from tooling, on day one

- Root `AGENTS.md` and `CLAUDE.md` were **created byte-identical in the same commit**: `babb51c9` (2026-06-06) *"chore: initialize spec kit and agent tooling"* — the commit that installed Spec Kit and both skill trees (`.claude/skills/speckit-*` and `.agents/skills/speckit-*`). Neither file predates the other; the duplication is a **spec-kit/agent-tooling initialization artifact**, not an organic decision that one agent needed different rules. CONFIRMED (verified: `git show --stat babb51c9`).
- The Backend pair was created together in the pre-monorepo Backend repo (`fe542351`, 2026-06-06, "Add backend project instruction files") and imported byte-identical via git-subtree on 2026-07-10 (`258a8d1b`). Identical at import and in all 9 monorepo commits touching the pair. CONFIRMED (verified: `sha1sum` both = `9336224f…`).
- The Frontend pair was created together (`6b2c2084`, 2026-06-06) and has **never been byte-identical in monorepo history — but the entire diff is the H1 title line** ("Frontend Agent Guide" vs "Frontend Project Instructions"). Verified by this author: `diff` of both files minus line 1 is empty. CONFIRMED. This one-line difference means hash-based mirror checks silently pass over a file that is a mirror in substance — the mirror system doesn't even register its third pair.

### 2.2 Ratified as deliberate hand-maintained mirror policy

The mirroring is not an accident that nobody noticed. It is **explicit, on the record, and enforced by hand**:

- `f5abff7a` (2026-07-27) commit body: *"Rule text added to CLAUDE.md §Workspace Path Conventions (**mirrored byte-identical into AGENTS.md**)"* and *"Verified: … root and Backend CLAUDE.md/AGENTS.md pairs byte-identical."* Verified by this author against `git show -s --format=%b f5abff7a`. CONFIRMED.
- `ebddf2a5` (2026-08-04) commit body: *"The two files **are mirrors; they are byte-identical again**"* — closing an audit finding (F02) in which `AGENTS.md` still named a feature open while `CLAUDE.md` said "None". CONFIRMED (verified).
- The same `f5abff7a` deleted `.opencode/` (15 tracked files) declaring *"this is a **Claude-only workspace** and stale duplicate instruction copies are exactly the hazard this rule exists to kill"* — yet `.agents/` (Codex-facing) and `.cursor/` survived that declaration. CONFIRMED.

### 2.3 The mirror policy has already failed three times in two months

Root-pair identity transitions: SAME (2026-06-06) → DIFF (2026-06-06) → SAME (2026-07-27) → DIFF (2026-07-28) → SAME (2026-07-28) → DIFF (2026-07-29) → SAME (2026-08-04, identical since). **Three documented drift→resync cycles in nine weeks**; each drift happened because `CLAUDE.md` was edited alone (64 commits vs 42 for `AGENTS.md`, no-follow counts) and each resync required a dedicated audit finding or sweep to notice. CONFIRMED (`data/history-evidence.json` `identity_transitions`; commit counts verified: 64/42).

**Why does AGENTS.md route non-Claude agents INTO CLAUDE files?** Because `AGENTS.md` *is* `CLAUDE.md` — a byte-copy. Its routing section was written from Claude's perspective ("The root `CLAUDE.md` contains general instructions…", "read and follow: `Backend/CLAUDE.md`", "`Frontend/quran-dashboard-ui/CLAUDE.md`" — `AGENTS.md:7,11,15`, verified) and the mirror policy copies that Claude-oriented routing verbatim into the file whose entire purpose is to serve agents that are *not* Claude. The result is an on-record contradiction:

> `AGENTS.md:11,15` routes every non-Claude agent into `Backend/CLAUDE.md` and `Frontend/quran-dashboard-ui/CLAUDE.md`,
> while `.cursor/rules/always-read-agents.mdc:41` (alwaysApply: true) commands: *"Do not rely on `CLAUDE.md` unless the user explicitly asks. `CLAUDE.md` is for Claude-specific behavior."*

Both statements are simultaneously in force at HEAD. Verified by this author (both lines read directly). CONFIRMED. A Cursor agent obeying its rule must not read CLAUDE.md; the AGENTS.md that same rule tells it to read sends it there anyway. The system is internally inconsistent, and it is only harmless because the files happen to be byte-identical — which is exactly the condition that has silently broken three times.

---

## 3. Anatomy of the root entrypoint: what is agent-specific vs neutral law

The brief (§7, §25 Q4–Q5) asks which rules are genuinely agent-specific and which are neutral project law. This author measured the 14,915-byte root file section by section (byte counts computed from the working tree; section boundaries from `grep -n '^#'`):

| Lines | Section | Bytes | Share | Classification |
|---|---|---:|---:|---|
| 1–20 | Workspace Project Instructions (routing) | 745 | 5.0% | **Entrypoint routing** — per-agent by nature; currently routes into CLAUDE-named files (`CLAUDE.md:11,15`) |
| 21–37 | Branching workflow | 781 | 5.2% | **Neutral law** (git-flow; any agent, any human) |
| 38–88 | Workspace Path Conventions (incl. planning-artifact lifecycle) | 4,466 | 29.9% | **Neutral law** — mentions `.claude/`, `.agents/`, `.specify/` only as repo paths to grep, not as behavior |
| 89–104 | Local README Context | 1,103 | 7.4% | **Neutral law** (nearest-README rule) |
| 105–177 | Coding Principles + *Comments are forbidden by default* (canonical) | 4,005 | 26.9% | **Neutral law** — the canonical comment rule; scope list names `.claude/` etc. as out-of-scope paths only |
| 178–201 | Clean-code self-check before delivery | 1,027 | 6.9% | **Claude-workflow-specific mechanism** — routes into `.claude/skills/engineering-review/references/clean-code-guard/` (`CLAUDE.md:181`), names the `engineering-review` skill |
| 202–221 | Test-code self-check before delivery | 678 | 4.5% | **Claude-workflow-specific mechanism** — routes into `.claude/skills/test-guard/` (`CLAUDE.md:214`) |
| 222–238 | Scope-aware test execution | 988 | 6.6% | **Neutral law** (defers to `TESTING_STRATEGY.md`; "formal reviewer" is a skill reference but the rule is neutral) |
| 239–255 | Design Context | 804 | 5.4% | **Neutral law** (product register, RTL, anti-references) |
| 256–264 | Active Spec Kit Feature | 318 | 2.1% | **Workflow state** — Spec Kit is currently installed for Claude only (`.specify/integration.json`), so today this is Claude-workflow-adjacent |

**Totals: ~81.4% (12,147 B) of the root file is agent-neutral project law. ~11.4% (1,705 B) is Claude-workflow-specific — and even there, the *obligation* (self-check before delivery) is neutral; only the *mechanism* (`.claude/skills/…` paths, skill names) is Claude's. Routing is 5.0%; Spec Kit state 2.1%.** CONFIRMED (bytes measured; classification is this author's judgment on verified content — the per-section labels are individually defensible from the quoted line evidence).

The project-level pairs are even cleaner: `Backend/CLAUDE.md` (7,335 B) and `Frontend/quran-dashboard-ui/CLAUDE.md` (4,025 B) contain **zero Claude-workflow-specific content**. Verified: their only "claude"/"skill" matches are pointers to the root canonical rules (`Backend/CLAUDE.md:70,75`; `Frontend/.../CLAUDE.md:53`). They are 100% neutral area law (architecture-doc triggers, test-lane selection, README lists, comment-policy language detail, ApiResponse pointer). CONFIRMED.

**Implication (LIKELY):** the "AGENTS vs CLAUDE" framing overstates how much is actually agent-specific. There is no meaningful body of Claude-only *rules* to separate from Codex-only *rules* — there is one body of neutral law (~95% of the pair content), a thin per-agent mechanism layer (~1.7 KB naming Claude skills), and a routing header. The duplication exists because tooling created two files for one text and a mirror policy preserved that shape, not because two agents ever needed different content.

---

## 4. The routing graph and the third copy: Cursor

The routing graph (`data/instruction-inventory.json` `routing_edges`, 146 edges) shows every edge from `CLAUDE.md` duplicated as an identical edge from `AGENTS.md` — 35 logical routes maintained twice (70 of the 146 edges; verified per pair: root 13+13, Backend 12+12, Frontend 10+10). The other 76 edges come from non-mirrored sources (the Cursor rule, skills, arch docs, READMEs). On top of the mirrored pairs sits a **third full copy** of the routing:

### 4.1 `.cursor/rules/always-read-agents.mdc` — classification

| Property | Evidence | Status |
|---|---|---|
| `alwaysApply: true` | `.mdc:2` (verified) | CONFIRMED |
| Actively maintained | 5 commits 2026-06-08 → 2026-08-06 (two days before baseline), updated in the same doc sweeps as AGENTS/CLAUDE (`9ed3a5d8`) | CONFIRMED |
| Contains its own restatement of | routing, Spec Kit reading list, file-size thresholds, Quranic data safety (`.mdc:89-117`, verified :89), commit workflow, the full test-lane matrix incl. Vitest fork-cap mechanism | CONFIRMED |
| Contradicts AGENTS.md routing | `.mdc:41` "Do not rely on `CLAUDE.md`…" vs `AGENTS.md:11,15` routing into CLAUDE files (both verified) | CONFIRMED |
| Whether the Cursor editor is still used | no Cursor-identifiable commits; cannot be determined from the repo | **UNKNOWN** |

**Classification: actively maintained support for a possibly-inactive consumer** — "maintained-in-sync, LIKELY active support; editor usage UNKNOWN". The file itself is the single most aggressive context mandate in the repo: for **any** frontend task it requires `AGENTS.md` (14,915) + `CODING_PRINCIPLES.md` (5,190) + frontend `AGENTS.md` (4,016) + `FRONTEND_STRUCTURE.md` (18,929) + **`UI_STYLE_SYSTEM.md` (103,970, unconditionally — `.mdc:27`, verified)** + `API_INTEGRATION_GUIDELINES.md` (11,920) + itself (9,387) = **168,327 bytes (~42,082 tokens) before touching code**. CONFIRMED.

### 4.2 Codex: stale integration, live entrypoints, confirmed recent activity

| Artifact | State | Status |
|---|---|---|
| `.codex/` | exists on disk, empty, never git-tracked (verified: `git ls-files`, `git log` empty); mtime 2026-08-07 | CONFIRMED empty/untracked; origin LIKELY a recent Codex CLI launch; date of first appearance UNKNOWN |
| `.specify/integration.json` | `installed_integrations: ["claude"]`, `default_integration: "claude"` (verified by reading the file) | CONFIRMED — **Codex spec-kit integration is not installed** |
| `.specify/integrations/codex.manifest.json` | still present (verified `ls`), registers `.agents/skills/speckit-*` hashes, installed 2026-06-06 v0.8.3 | CONFIRMED stale/superseded |
| `.agents/skills/speckit-*` (9 files, 100,271 B) | **full forks** of the `.claude` versions, not pointers; hook sections already diverge (`data/skill-inventory.json`) | CONFIRMED — structurally drift-capable |
| `.agents/skills/<10 project skills>` | pointer-only stubs (verified sample: engineering-review adapter says "Pointer for non-Claude agents. Canonical … `.claude/skills/…`") — the proven in-repo pattern for serving two agent families one text | CONFIRMED |
| Pointer defects | `.agents/skills/commit-workflow/SKILL.md:7,18` says "post-PR **sync-to-main**" (verified) while the canonical skill defines sync-to-**dev** and forbids touching main; `.agents/skills/test-guard/SKILL.md:17` omits one existing reference file | CONFIRMED — live evidence that even tiny hand-kept copies drift, and this one misstates a branch-safety behavior |
| Codex actually used | six `refs/codex/turn-diffs/checkpoints/*` tree refs with embedded timestamps decoding to 2026-08-05 15:37 → 2026-08-07 21:37 UTC (verified: `git for-each-ref refs/codex`; cross-ref report 05 §3.5) — Codex CLI turns on three of the four days before baseline; also: Codex-facing skills added `6c944e8d` (2026-06-11); Codex as PR reviewer `872f0adf` (2026-07-15); empty `.codex/` mtime one day before audit | **CONFIRMED recent Codex CLI activity (2026-08-05 → 2026-08-07 UTC); usage LEVEL still NEEDS_MEASUREMENT** |

---

## 5. What tasks actually read: the 8 task traces

From `data/instruction-inventory.json` `task_traces`. **This author independently recomputed the byte arithmetic of the headline traces from the verified per-file sizes; T1 and T2 sum exactly** (T1: 14,915+7,335+5,190+17,452+33,427+19,724 = 98,043; T2: …+103,970+97,772+… = 293,528). Nearest-README entries use the Abwab area as the documented representative; totals are full-file upper bounds.

| # | Trace | Mandatory files | Mandatory bytes | ~Tokens | With conditional | Dominant cost |
|---|---|---:|---:|---:|---:|---|
| T1 | Tiny backend bug fix | 6 | 98,043 | 24,511 | 192,423 (~48k) | TESTING_STRATEGY 33.4 KB + tests README 19.7 KB |
| T2 | **Tiny frontend UI fix** | 9 | **293,528** | **73,382** | 368,119 (~92k) | UI_STYLE_SYSTEM 104 KB + abwab README 97.8 KB |
| T3 | New backend GET endpoint | 10 | 148,259 | 37,065 | 228,004 (~57k) | 3 arch docs + 2 READMEs + testing law |
| T4 | Authorization change | 8 | 98,538 | 24,634 | 214,528 (~54k) | 5-README Access fan-out + testing law |
| T5 | Abwab cross-stack change | 10 | 239,860 | 59,965 | 442,424 (~111k) | 3 area READMEs (146.5 KB) + 3 entry files |
| T6 | Approved-plan phase impl. | 11 | 110,305* | 27,576* | 191,918* (~48k) | speckit-implement body + testing law (*excl. unsized specs/) |
| T7 | Engineering review (backend+tests) | 17 | 206,417 | 51,604 | 260,782 (~65k) | skill body 26.7 KB + 6 clean-code refs 54.2 KB + test-guard chain |
| T8 | Performance review (backend) | 6 | 73,044 | 18,261 | 123,878 (~31k) | 3 arch docs mandated by the skill |

Headline findings, each CONFIRMED from trigger wording verified in the working tree:

- **A one-line spacing/color fix carries ~73,000 tokens of instructed mandatory reading — the heaviest of the two "tiny" traces.** The trigger is `Frontend/quran-dashboard-ui/CLAUDE.md:3-9` (verified): *"Before creating or changing global styles, theme tokens, reusable UI classes, layout shell styles, **component visual styles**, dark/light theme behavior, or shared UI patterns, read and follow: `.architecture/UI_STYLE_SYSTEM.md`."* "Component visual styles" matches a one-line color change, pulling a 103,970-byte document of which ~58% is per-component contract appendix and ~174 lines are an explicitly superseded era (§15 navy+gold, retained in place — `data/history-evidence.json` `ui_style_system`).
- **`TESTING_STRATEGY.md` (33.4 KB) is instructed before selecting tests on every trace that runs tests** — it appears in all 8 traces' mandatory or conditional sets (`CLAUDE.md:224`, `Backend/CLAUDE.md:18`, `Frontend/.../CLAUDE.md:22`). Yet each project entrypoint *already contains* a lane-selection summary (`Backend/CLAUDE.md:16-31`, `Frontend/.../CLAUDE.md:20-31`, verified) — the full-document read duplicates what the entrypoint just said.
- **The nearest-README rule has quietly become the second-largest cost.** Feature READMEs have grown to architecture-doc scale: abwab 97,772 B, access-admin 42,594 B, words 37,786 B, Backend Writes/Abwab 31,236 B. "Read the nearest README first" was designed for small local-truth files; at these sizes it is a 10–24k-token toll per touched area. (Sizing verified; the README-content question belongs to report 06 — this report flags the *routing* consequence.)
- **The engineering-review worst case is ~90,000 tokens** (359,926 B full closure across body + references + arch docs + PRODUCT/DESIGN — `data/skill-inventory.json` `engineering_review_closure`), with a floor of ~17,100 tokens before any conditional doc. Scoped-review paths exist in the skill (verified citations in the data), so the worst case is not every case. Deduplication inside the skill belongs to report 04.

---

## 6. The 17 duplicated rules

`data/instruction-inventory.json` `duplicated_rules` catalogs 17 rules stated in 2–16 places each. Instance counts verified by this author for the top five by re-counting the `stated_in` lists and spot-checking citations in the tree (`.cursor/...mdc:89` Quranic safety, `Backend/CLAUDE.md:101-106` ApiResponse pointer, `TESTING_STRATEGY.md:7` six-entrypoints — all verified).

| Rule | Copies | Canonical (where declared) | Notes |
|---|---:|---|---|
| Quranic data safety — never invent/alter | **16** | `CODING_PRINCIPLES.md:102-110` + shared ref `quran-data-safety.md` | The most-duplicated rule in the repo. Protection itself is untouchable (brief §29); the *duplication* is the target — collapse to canonical + pointers, never delete the rule. |
| ApiResponse envelope contract | **16** | `Backend/.architecture/API_GUIDELINES.md` | Most copies are already pointers; ~5 restate content. |
| Test-gate selection, narrowest lane first | **12** | `TESTING_STRATEGY.md` §5 | Six entrypoints + cursor rule + READMEs + guide + skill all restate. |
| Nearest-README-first | **10** | `CLAUDE.md:89-104` | Meta-rule duplicated across the very files it routes between. |
| Comment policy (forbidden by default) | **9** | `CLAUDE.md:113-176` (self-declares canonical) | Healthiest topology: copies explicitly defer; the mirror pairs still double them. |
| No-CI, every gate local | 11 | `TESTING_STRATEGY.md` §8 | |
| Review routing (engineering-review is formal reviewer) | 12 | — (stated 3× inside root file alone) | |
| RTL/Arabic-first register | 11 | `PRODUCT.md`/`DESIGN.md` | |
| Spec Kit routing + artifact locations | 9 | `CLAUDE.md:38-88` | |
| File-size thresholds | 7 | structure docs | |
| Vitest two-fork cap | 8 | `TESTING_STRATEGY.md` §4 | Cursor rule restates the full mechanism (`.mdc:162-182`). |
| Single-postgres-container serialization | 6 | `TESTING_STRATEGY.md` §3.3 | |
| Planning-artifact lifecycle | 5 | `CLAUDE.md:51-88` | |
| Branching (dev git-flow) | 3 | `CLAUDE.md:21-37` | |
| separate-SCSS-by-default | 3 | `FRONTEND_STRUCTURE.md` | Policy-loop member (brief §22) — see report 07. |
| qd-* class vocabulary | 9 | `UI_STYLE_SYSTEM.md` | Policy-loop member — see report 07. |
| Tailwind-supports-not-replaces | 2 | `UI_STYLE_SYSTEM.md:254-266` | |

**Structural observation (CONFIRMED):** the duplication topology is *mostly healthy by design* — most copies are short restatements that name their canonical source and defer (`data/markdown-decision-inventory.json` notes). The two mechanisms that multiply copies wholesale are (1) the **AGENTS/CLAUDE mirror pairs**, which automatically double every rule they carry (turning 8 logical statements of the test-gate rule into 12 file-copies), and (2) the **Cursor rule**, which restates entire rule bodies (safety, lanes, thresholds) rather than pointing. Kill those two multipliers and the 17-rule table collapses substantially without touching a single canonical rule.

**Change-cost consequence (brief §22):** if the test-lane matrix changes, 12 places must change; if Quranic-safety wording changes, 16 places. Each mirror-pair edit is a hand-synced double edit, which history shows failing 3 times in 9 weeks. CONFIRMED.

---

## 7. Evaluating the brief's target model (evaluate only — brief §7, §10)

> Target to evaluate: `Claude → CLAUDE.md`, `Codex → AGENTS.md`, with shared neutral law referenced narrowly rather than duplicated wholesale.

**Verdict: the direction is sound and the content analysis (§3) shows it is cheap in substance — but the honest evaluation is that the *shape* matters less than killing the hand-mirror, and one variant is materially safer than the others.** Three variants, assessed:

| Variant | Description | Gain | Risk / cost |
|---|---|---|---|
| **(a) Status quo** — hand-maintained byte mirrors | current | none | Proven failure mode: 3 drifts in 9 weeks, each caught only by audit; every law edit is a double edit; Frontend pair invisible to hash checks; contradiction of §2.3 persists. |
| **(b) Pointer stub** — AGENTS.md becomes a ~10-line pointer to the CLAUDE file (or vice versa) | smallest possible change; the pattern is already proven in-repo by the 10 `.agents/skills` adapters | eliminates double edits and drift **content**; one text, one edit | Perpetuates non-Claude agents reading a Claude-named file — the exact thing `.cursor/...mdc:41` forbids; pointer files can still drift in their *description* (the commit-workflow "sync-to-main" pointer defect is a live example, §4.2); requires updating `TESTING_STRATEGY.md:7` ("six entrypoints") and the Cursor rule in the same change. |
| **(c) Neutral-law extraction** (the brief's target) — each agent gets a slim entrypoint (routing + its own mechanism layer); the ~81% neutral law moves to agent-neutral file(s) | the semantically correct shape; resolves the naming contradiction; each agent reads only its mechanism + shared law | most moved text; the Claude harness auto-loads only `CLAUDE.md`, so the shared law file becomes a **pointer hop** for Claude (an extra instructed read on every session) unless the harness-loaded file inlines it — which recreates the duplication; splitting law across more files multiplies the N-way drift surface if done badly; every inbound reference (`TESTING_STRATEGY.md:7`, Cursor rule, skills, READMEs) must be repointed. |

**Assessment (LIKELY):** (b) is the smallest safe step and captures ~90% of the value (single text, zero drift, no content change, no new files); (c) is the better end-state *if and only if* the Codex/Cursor question (§4) is first answered — Codex has CONFIRMED recent CLI activity (§4.2), so the non-Claude entrypoint surface has at least one live consumer, which strengthens the case for (b)'s pointer stub serving it one canonical text; Cursor remains UNKNOWN, and extracting shared law before the forward-support decision is made is still speculative work. A defensible sequence the remediation planner could evaluate: (b) first at all three levels; (c) only after deciding whether Cursor/Codex remain supported. Under (b) with CLAUDE.md as canonical, the router contradiction resolves trivially (the pointer says "the workspace law lives in CLAUDE.md" — an explicit ask, satisfying `.mdc:41`'s own escape clause). **This is an evaluation, not an instruction; no variant is implemented by this audit.**

The mirror-*verification* alternative (keep two files, add a byte-equality check-script) is strictly worse than (b): it preserves the double-edit cost and the misrouted naming, buying only drift detection. NOT RECOMMENDED for evaluation priority. LIKELY.

---

## 8. Proposed simplifications (each answering brief §4's seven questions)

Classifications use the brief's taxonomy verbatim: `KEEP / SHORTEN / MERGE / DELETE_CANDIDATE / HISTORICAL_ONLY / MOVE_TO_CANONICAL_SOURCE / ON_DEMAND_ONLY`.

### 8.1 Root + Backend + Frontend AGENTS/CLAUDE mirror pairs → `MERGE`

1. **Value today:** entrypoint coverage for two agent families; the AGENTS convention is an ecosystem standard some tools look for.
2. **Dependents:** Claude harness (auto-loads the CLAUDE chain — CONFIRMED); `.cursor/rules/always-read-agents.mdc:10-28` (reads the AGENTS chain); `TESTING_STRATEGY.md:7` names "the six entrypoints (`AGENTS.md`, …)" — verified; spec-kit codex manifest hashes; grep-able references across docs.
3. **Risk if merged:** a non-Claude tool that hard-expects full content at `AGENTS.md` gets a pointer instead (low: the 10 `.agents/skills` pointer adapters already prove non-Claude agents follow pointers here); missed inbound reference (mitigated by the repo's own "repoint before you delete" law, `CLAUDE.md:74`).
4. **Equivalent protection elsewhere:** git history preserves all text; the content itself is unchanged under variant (b).
5. **Smallest safe simplification:** variant (b) of §7 — one canonical text per level, the twin becomes a pointer stub; Frontend pair included (its title-only diff makes it the cheapest of the three).
6. **Later verification:** `grep -rn` for dangling references; sha1 comparison becomes obsolete; re-run the static trace computation; confirm `TESTING_STRATEGY.md:7` and the Cursor rule were updated in the same change.
7. **Recurring cost removed:** the double edit on every law change (root pair alone: 64 vs 42 commits in 9 weeks); the drift-resync incidents (3 so far, each consuming an audit finding); ~22,250 B of hand-synced identical bytes; ~5.6k tokens of duplicate text per non-Claude session; the §2.3 contradiction.

Confidence: problem CONFIRMED; proposed direction LIKELY safe with the named dependents handled.

### 8.2 Over-broad mandatory read triggers → `SHORTEN` (trigger scope) + `ON_DEMAND_ONLY` (the reads)

Three triggers create most of the tiny-task burden (all trigger texts verified):

- **(a) `UI_STYLE_SYSTEM.md` for any "component visual styles" change** (`Frontend/.../CLAUDE.md:5-9`). 1. Value: token/pattern discipline on visual work. 2. Dependents: the Cursor rule mandates it even harder (`.mdc:27`, unconditional for all frontend work); engineering-review routes into it for UI review. 3. Risk of narrowing: an agent invents ad-hoc colors/spacing instead of tokens; and — brief §29 protected dimension — **RTL/Quran typography correctness**: the de-mandated document carries §7 Typography, §8 RTL and Direction, and §13 Quranic Data Display Safety (`UI_STYLE_SYSTEM.md:268,287,380`), which a routine visual fix would no longer read under the narrowed trigger. 4. Equivalent protection: engineering-review checks UI-style conformance post-hoc; the quran-safety rule stays in every chain via `CODING_PRINCIPLES.md`; a slim token/utility reference card could carry the day-to-day rules (size NEEDS_MEASUREMENT — depends on report 07's split of the 104 KB file) — **carrying the RTL/Quran-display rules on that card is a condition of the trigger narrowing**. 5. Smallest step: scope the trigger to token/theme/shared-pattern *definition* changes, with consumption-side work reading a small card. 6. Verify: re-run trace T2; spot-review of visual diffs for token violations. 7. Removes: up to ~26k tokens from every routine visual change.
- **(b) Full `TESTING_STRATEGY.md` before selecting tests** (`CLAUDE.md:223-224`). 1. Value: correct lane choice, DB-safety, no-CI discipline. 2. Dependents: all 8 traces; the entrypoints' own summaries already restate the operative subset (`Backend/CLAUDE.md:16-31`, `Frontend/.../CLAUDE.md:20-31` — verified). 3. Risk: wrong lane, concurrent DB runs. 4. Equivalent protection: the entrypoint summaries + the runner's own serialization logic (`Backend/scripts/test-backend` enforces container serialization mechanically, per brief §9). 5. Smallest step: make the entrypoint summary authoritative for routine lane selection; full doc on-demand for gate-matrix or infrastructure questions. 6. Verify: trace recomputation; observed lane choices in review. 7. Removes: ~8.4k tokens from nearly every task.
- **(c) `PRODUCT.md` + `DESIGN.md` for UI work** (`CLAUDE.md:245-248` — wording is already "when product/design context is relevant"; the traces show it read as mandatory-in-practice). 1. Value: register/tone protection. 2. Dependents: impeccable/design flows. 3. Risk: register drift on *new* UI. 4. Equivalent protection: the Design Context section of the root file (804 B) already carries the register summary every session. 5. Smallest step: clarify the trigger to *new UI surfaces or copy changes*, not spacing/color fixes. 6. Verify: trace T2 recomputation. 7. Removes: ~6.4k tokens from routine visual fixes. LIKELY.

### 8.3 `.cursor/rules/always-read-agents.mdc` → `SHORTEN` now; `DELETE_CANDIDATE` pending one question

1. **Value:** makes Cursor sessions follow workspace law. 2. **Dependents:** none in-repo (nothing references the file; it is a leaf — its routing edges point outward). 3. **Risk:** if Cursor is in active use, deletion strands it without law; the file is also the only place stating "don't rely on CLAUDE.md". 4. **Equivalent protection:** under §8.1(b) the AGENTS entry chain serves any agent; the rule's restated content (safety, lanes, thresholds) all has canonical owners. 5. **Smallest safe step:** first, **answer the UNKNOWN — is Cursor still used?** (a one-question user decision; unmeasurable from the repo). If yes: shrink to a pointer at the entry chain and delete the restatements (its 2,347 tokens re-earn their place as ~200). If no: `DELETE_CANDIDATE`. 6. **Verify:** grep for inbound refs (none expected); a Cursor smoke session if retained. 7. **Removes:** the third hand-synced routing copy (5 sync commits in 9 weeks), ~2.3k always-on tokens per Cursor session, and the 168 KB unconditional frontend chain (§4.1).

### 8.4 Stale Codex spec-kit remnants → `DELETE_CANDIDATE` (conditional on the Codex decision)

`.specify/integrations/codex.manifest.json` + the 9 `.agents/skills/speckit-*` full forks (100,271 B, hook sections already diverged) vs `.specify/integration.json` listing Claude only. 1. Value today: none demonstrable — the integration they serve is not installed. 2. Dependents: the manifest references the fork hashes; nothing else found. 3. Risk: if Codex spec-kit use resumes, the forks would be regenerated by the integration installer anyway (they were installed by tooling in `babb51c9`/`6c944e8d`). 4. Equivalent protection: `.claude/skills/speckit-*` remains canonical; git history retains the forks. 5. Smallest step: decide Codex's spec-kit status first — Codex CLI activity is CONFIRMED-recent (§4.2), but the spec-kit integration it would use remains uninstalled, so the user question is forward support, not whether Codex is used; if unsupported, remove manifest + forks together; if supported, reinstall/regenerate rather than hand-repair. 6. Verify: grep for references; spec-kit commands still function for Claude. 7. Removes: ~25k tokens of drift-capable fork text and a false signal that Codex spec-kit is configured. CONFIRMED stale; deletion safety LIKELY pending the decision. The two `.agents` pointer defects (commit-workflow "sync-to-main" misdescription — a branch-safety misstatement — and test-guard's missing reference) are **defects to fix regardless of any simplification**; cross-ref report 04.

### 8.5 `SKILLS_AND_ARCHITECTURE_GUIDE.md` (42,729 B) → `ON_DEMAND_ONLY` (evaluate `HISTORICAL_ONLY` in report 06)

No entrypoint instructs reading it on any task path; root `CLAUDE.md:81` lists it only as long-lived law (CONFIRMED, `data/instruction-inventory.json` notes). It self-acknowledges drift against the actual skill trees (`data/markdown-decision-inventory.json` notes). 1. Value: onboarding/meta-orientation. 2. Dependents: none on task paths. 3. Risk: minimal — nothing routes through it. 4. Equivalent protection: the skill descriptions themselves + `docs/contracts/`. 5. Smallest step: reclassify as on-demand orientation; report 06 owns the shorten/retire call. 6. Verify: grep for inbound references. 7. Removes: nothing per-task today (it is already unread — which is itself the finding: 10.7k tokens of "law" that no path consumes).

### 8.6 `.claude/settings.local.json.bak` → `DELETE_CANDIDATE` (classification owned by report 05 §3.2)

Tracked backup from an incompletely-removed tool integration (lean-ctx, `ae4871d2`; cleanup `1a9a3ef8` missed it; untouched 43 days). Contains a stale permission allowlist including direct test invocations the current law forbids as lane bypasses. CONFIRMED (`data/history-evidence.json` `settings_bak`). Small (456 B) but actively misleading. Classification and the brief-§4 seven-question adjudication are owned by report 05 (§3.2); recorded here for cross-reference only because it is instruction-adjacent config.

---

## 9. Smallest safe instruction chain per agent type (Q6, Q7)

**Claude (today, already true):** root `CLAUDE.md` (harness-auto) → area `CLAUDE.md` (harness-auto on area work) → `CODING_PRINCIPLES.md` before implementation → *trigger-scoped* architecture doc(s) → *targeted* nearest-README → code. Claude never needs any `AGENTS.md`. The smallest *safe* chain is exactly this minus the three over-broad triggers of §8.2 — no file removal required, only trigger-scope correction. CONFIRMED structure; savings LIKELY (computed in §10).

**Codex / non-Claude (today):** root `AGENTS.md` → area `AGENTS.md` — which, being byte-mirrors, is the identical burden, plus the §2.3 contradiction. **Smallest safe chain under §8.1(b):** `AGENTS.md` pointer (≈10 lines) → the single canonical text → same trigger-scoped tail as Claude. Net Codex burden becomes identical to Claude's plus one ~100-token pointer hop. Under §7(c) it would instead be: slim `AGENTS.md` (routing + any Codex-specific mechanism, of which **none currently exists** — §3 found no Codex-specific rules anywhere) → shared law. That "none currently exists" is the strongest evidence that (c) is currently over-engineering: there is no Codex-specific content to separate. CONFIRMED content analysis; chain design LIKELY.

**Cursor:** whatever the §8.3 decision yields — either no chain (unused) or pointer-stub `.mdc` → the AGENTS entry chain. Its current chain (own 9.4 KB rule + AGENTS chain + unconditional arch docs) is the largest per-task mandate in the repo and is UNKNOWN-consumer.

---

## 10. How much context can normal tasks save? (Q8) — computed per trace

**Model:** "minimal chain, routing-only" — *no document is rewritten or shrunk*; the only change is narrowing which reads are mandatory (§8.2 trigger corrections + test-doc summary-first). Every retained read is counted at its full current size, so these are conservative. Retained in all traces: harness entry files + `CODING_PRINCIPLES.md` + nearest area README(s) (the rule's value is real even when the files are oversized) + anything protection-relevant.

| Trace | Current mandatory | Minimal chain (routing-only) | Saving | % | What moved to on-demand |
|---|---:|---:|---:|---:|---|
| T1 tiny backend fix | 98,043 B / 24,511 t | 44,892 B / 11,223 t | 53,151 B / **13,288 t** | 54% | TESTING_STRATEGY (lane summary already in entrypoint), tests README (until tests are authored) |
| T2 tiny frontend fix | 293,528 B / 73,382 t | 121,902 B / 30,476 t | 171,626 B / **42,906 t** | 58% | UI_STYLE_SYSTEM, PRODUCT, DESIGN, TESTING_STRATEGY, testing README |
| T3 new GET endpoint | 148,259 B / 37,065 t | 104,879 B / 26,220 t | 43,380 B / 10,845 t | 29% | CLEAN_ARCHITECTURE (covered by BACKEND_STRUCTURE for placement), TESTING_STRATEGY; tests README **retained** (endpoint work always authors tests) |
| T4 authorization change | 98,538 B / 24,634 t | 68,199 B / 17,050 t | 30,339 B / 7,585 t | 31% | TESTING_STRATEGY moved out; all access READMEs + tests README kept, and the security-access contract (3,088 B) **promoted from conditional to mandatory** as a protection read — protected area (brief §29), deliberately the smallest cut |
| T5 abwab cross-stack | 239,860 B / 59,965 t | 179,397 B / 44,849 t | 60,463 B / 15,116 t | 25% | TESTING_STRATEGY + both test READMEs moved out; abwab contract (1,472 B) **promoted from conditional to mandatory**; residual dominated by 146.5 KB of area READMEs |
| T6 plan-phase impl. | 110,305 B / 27,576 t | 57,154 B / 14,289 t | 53,151 B / 13,288 t | 48% | TESTING_STRATEGY, tests README (both re-enter when the phase reaches test tasks); specs/ unsized in both columns |
| **Total T1–T6** | **988,533 B / ~247k t** | **576,423 B / ~144k t** | **412,110 B / ~103k t** | **41.7%** | |

T7 (engineering review) and T8 (performance review) are deliberately excluded from the savings claim: they are the safety gates the narrowed implementation chain leans on, and their reduction potential is deduplication *inside* the skills (~52 KB of self-acknowledged restatement — `data/skill-inventory.json` `embedded_in_body`), owned by report 04.

**Headline: routing-only corrections — no document rewritten, no rule deleted — remove ~42% of instructed mandatory context across normal-work traces (25–58% per trace), ~103k tokens across the six modeled tasks, with the tiny-frontend-fix falling from ~73k to ~30k tokens.** LIKELY (trigger analysis CONFIRMED; the minimal-chain retention judgments are this author's modeling).

**Second-stage upside (owned by other reports, NEEDS_MEASUREMENT):** the residual is dominated by document *size* — abwab README 97.8 KB (report 06), UI_STYLE_SYSTEM 104 KB with its superseded §15 (report 07), TESTING_STRATEGY quick-card extraction (report 02/10). If those land, a tiny task plausibly reaches ~10–12k tokens of instructed context — an ~85% reduction from today's T2 — but that number depends on split outcomes that have not been designed, so it is a direction, not a promise.

### Recommended measurable context budget (recommendation only)

A future budget should be *per task class*, in the same statically-computable unit used here (sum of instructed-mandatory file bytes ÷ 4, recomputed from trigger wording — exactly the method of `data/instruction-inventory.json`, so the measurement is reproducible and does not require runtime telemetry):

| Task class | Proposed budget (instructed mandatory tokens before first production-code read) | Today |
|---|---:|---:|
| Tiny fix (either stack) | ≤ 12,000 | 24.5k / **73.4k** |
| Single-area feature work (endpoint, component) | ≤ 25,000 | 37.1k |
| Protected-area change (auth, import, Quran data) | ≤ 25,000 (protection reads exempt from trimming) | 24.6k |
| Cross-stack feature / plan phase | ≤ 35,000 (+ live spec artifacts) | 60.0k |
| Formal review (mandatory set) | ≤ 45,000 | 51.6k |

The repo's own law provides the enforcement shape: *"Evidence worth keeping becomes a test that fails on drift"* (`CLAUDE.md:70-73`) — the budget can be a checked assertion re-run when instruction files change, making regressions visible the same way the mirror drift was not. Recommendation only; design belongs to remediation planning.

---

## 11. Out-of-repo context (honesty note)

Claude sessions in this workspace also always load **out-of-repo context**: the user's global `~/.claude/CLAUDE.md` and the auto-memory `MEMORY.md` (both observed present in this audit session's environment — CONFIRMED they exist for this environment). They are outside repository scope and outside this report's remit; their size and content burden for typical sessions is **UNKNOWN here** and belongs to report 05 (memory/context audit). No repo-side simplification changes them.

---

## Mandatory questions answered (brief §25, Q1–Q8)

| # | Question | Answer |
|---|---|---|
| 1 | Why are root AGENTS and CLAUDE duplicated? | **CONFIRMED:** created byte-identical in one commit (`babb51c9`, 2026-06-06, spec-kit/agent-tooling initialization), then ratified as an explicit hand-maintained mirror policy — commit bodies state "mirrored byte-identical into AGENTS.md" (`f5abff7a`) and "they are byte-identical again" (`ebddf2a5`). Tooling created the shape; policy preserved it. It has drifted and been hand-resynced 3 times in 9 weeks. §2. |
| 2 | Why are Backend AGENTS and CLAUDE duplicated? | **CONFIRMED:** created together in the pre-monorepo Backend repo (`fe542351`, 2026-06-06), imported byte-identical via subtree (2026-07-10), maintained in lockstep since (identical in all 9 monorepo commits touching the pair). Same mirror policy, same mechanism. §2.1. |
| 3 | Why does AGENTS route non-Claude agents into CLAUDE files? | **CONFIRMED:** because AGENTS.md is a byte-copy of CLAUDE.md, whose routing header was written from Claude's perspective (`AGENTS.md:7,11,15`). The mirror copies Claude-oriented routing verbatim into the non-Claude entrypoint — directly contradicting `.cursor/rules/always-read-agents.mdc:41` ("Do not rely on `CLAUDE.md`…"), both simultaneously in force. §2.3, §4. |
| 4 | What unique information actually needs to be agent-specific? | **CONFIRMED (measured):** almost nothing. Of the root 14,915 B: ~81.4% neutral law, ~11.4% Claude-workflow mechanism (skill names + `.claude/skills` paths in the two self-check sections — and even their obligations are neutral), 5.0% routing, 2.1% Spec Kit state. The Backend/Frontend pairs contain zero agent-specific content. No Codex-specific rule exists anywhere in the repo. §3. |
| 5 | What neutral project law should be shared? | Branching workflow, path conventions + planning-artifact lifecycle, nearest-README rule, coding principles + canonical comment policy, scope-aware test execution, design context — 12,147 B of the root file (§3 table), plus the entirety of both project-level pairs. Shared today by byte-copying; should be shared by single canonical text + pointers (§7). |
| 6 | Smallest safe instruction chain for Claude? | The existing harness chain (root CLAUDE.md → area CLAUDE.md → CODING_PRINCIPLES.md) with the three over-broad triggers narrowed (§8.2): trigger-scoped arch docs, targeted nearest-README, testing summary-first. No AGENTS file ever. §9. |
| 7 | Smallest safe instruction chain for Codex? | Today: the AGENTS chain (identical burden to Claude's + the contradiction) — and that chain has a live consumer: Codex CLI activity is CONFIRMED for 2026-08-05 → 2026-08-07 UTC (§4.2, report 05 §3.5). Target: AGENTS.md as a ~10-line pointer to the single canonical text (the pattern the 10 `.agents/skills` adapters already prove), then the same trigger-scoped tail. No Codex-specific rule content currently exists to justify more. §9. |
| 8 | How much context can normal tasks save? | **~42% of instructed mandatory context (≈103k tokens across the six normal-work traces; 25–58% per trace) from routing-only corrections — no document rewritten.** Tiny frontend fix: 73.4k → ~30k tokens. Second stage (document slimming, owned by reports 02/06/07) plausibly reaches ~85% for tiny tasks — NEEDS_MEASUREMENT. Plus, independent of tokens: every root/Backend law edit stops being a double edit. §10. LIKELY (model), CONFIRMED (inputs). |

---

## Measurement gaps

- **Actual runtime read behavior — NEEDS_MEASUREMENT.** All trace totals are static full-file sums (upper bounds). Real agents may read files partially (offsets/greps); no instrumentation of actual tokens-consumed-per-task exists. The budget in §10 is deliberately defined on the static measure so it stays checkable without telemetry.
- **Whether Cursor is still used — UNKNOWN.** The rule file is actively maintained (last edit 2 days before baseline) but no Cursor-attributable commits are identifiable. §8.3 is gated on a one-question user decision.
- **Current Codex usage LEVEL — NEEDS_MEASUREMENT; recent activity itself is CONFIRMED.** Six `refs/codex/turn-diffs/checkpoints/*` tree refs decode to 2026-08-05 15:37 → 2026-08-07 21:37 UTC (`git for-each-ref refs/codex`; cross-ref report 05 §3.5). Supporting evidence: empty untracked `.codex/` (mtime 2026-08-07, origin date unknowable from git), Codex-as-PR-reviewer in history, spec-kit integration not installed. How heavily Codex is used remains unmeasured; the §8.4 deletion is gated on the forward-support decision.
- **Out-of-repo context size (user-global CLAUDE.md, auto-memory) — UNKNOWN here;** exists (CONFIRMED for this environment), owned by report 05.
- **Spec-artifact sizes for plan-phase tasks — UNKNOWN at this baseline:** no feature is open ("Active Spec Kit Feature: None", `specs/` contains only its README), so T6's totals exclude them; historical features put them at tens of KB.
- **Token estimates are bytes/4** — a convention, not a tokenizer measurement; Arabic-heavy files (PRODUCT/DESIGN/READMEs) may tokenize differently. Relative comparisons are robust; absolute token figures are approximate.
- **The minimal-chain retention judgments in §10 are modeling choices** (e.g., keeping full nearest-READMEs mandatory, keeping all protection reads in T4). A different reasonable modeler would land within roughly ±10 percentage points; the direction and order of magnitude are stable.
