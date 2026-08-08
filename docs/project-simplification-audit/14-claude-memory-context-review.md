# 14 — Claude Memory & Context Review (Independent, This Session)

- **Required by:** `05-memory-context-audit.md` (§4, §8 — the prepared evidence base for the
  brief §28-B independent Claude memory review) and `13-sol-independent-review.md` (§4 WS9,
  §7 decision 10, §9.4).
- **Session:** a live Claude Code session on branch `audit/project-simplification`, HEAD
  `8ee6f3a6`, review date 2026-08-09.
- **Mode:** review-only. **Nothing was modified or deleted** — no memory file, no index line,
  no repository file other than creating this report. No chain-of-thought is exposed.
- **Access statement:** this report covers only memory and context this Claude session can
  actually observe or read. The only Claude persistent memory the environment exposes is the
  five-file directory at `/home/mohamed/.claude/projects/-projects-Dashboard-App/memory/`.
  Model-internal memory beyond those files is UNKNOWN and no access to it is claimed or
  invented. Every classification below was re-verified fresh against the working tree at
  review time, not inherited from reports 05/13.

---

## 1. Repository instructions/context

What the repository itself contributes to this session's context:

| Source | How it reaches the session | Observed |
|---|---|---|
| Root `CLAUDE.md` (`/projects/Dashboard/App/CLAUDE.md`) | Auto-injected in full at session start | Yes — full text present in this session's system context |
| `Backend/CLAUDE.md`, `Frontend/quran-dashboard-ui/CLAUDE.md` | On demand — the root file routes to them per task area | Not injected; loaded only when the work touches that area |
| `CODING_PRINCIPLES.md`, `TESTING_STRATEGY.md`, `PRODUCT.md`, `DESIGN.md`, READMEs, `.architecture/**` | On demand via the root file's reading rules | Not injected |
| `.claude/skills/*/SKILL.md` frontmatter descriptions | The `description:` line of every project skill is injected into the session skill roster | Yes — all project skills (10 project + 15 speckit) appear as roster entries |

The root `CLAUDE.md` is the single repository artifact injected into every Claude session for
this project; its size and content are Audit B's subject (report 03) and are not re-audited
here. One repo-side observation belongs to this report: several project-skill `description:`
frontmatter fields are paragraph-length (e.g. `performance-angular-review`,
`performance-backend-review`, `dependency-audit`), and each of those paragraphs is injected
into **every** session's skill roster regardless of task. That is a small, repo-controllable
recurring cost that report 04 (skills audit) owns; recorded here because it is the one place
repository content leaks into per-session injection outside the `CLAUDE.md` chain.

This session was not given `AGENTS.md` — the Claude harness injects the `CLAUDE.md` chain
only, consistent with report 03's routing model.

## 2. Tool/config/injected context

Everything below is observable in this session's own prompt and is configured **outside the
repository** (user-level harness, plugins, MCP servers) unless noted:

| Injection | Content | Recurring? |
|---|---|---|
| User-global `~/.claude/CLAUDE.md` | The memory-write policy ("store durable facts, not chatter") | Every session |
| Memory-system instruction block + `MEMORY.md` index | Directions for the file-based memory plus the 552 B index (3 entries) | Every session for this project |
| Environment/git snapshot | cwd, platform, branch, clean-status snapshot, 5 recent commits, user email, current date | Every session |
| SessionStart hook (superpowers plugin) | The **full body** of the `using-superpowers` skill, wrapped in mandatory-invocation framing | Every session — the single largest non-repo injection observed |
| Plugin skill roster | Descriptions for dozens of non-repo skills (superpowers ×14, coderabbit, chrome-devtools-mcp, microsoft-docs, claude-code-setup, skill-creator, frontend-design, plannotator, dataviz, …) | Every session |
| Deferred MCP tool listing | ~150 tool names (claude-in-chrome, Gmail, Google Calendar, Google Drive, Playwright, chrome-devtools, GitHub, context7, microsoft-learn) — names only; schemas load on demand | Every session |
| MCP server instruction blocks | Usage instructions for claude-in-chrome, context7, GitHub, microsoft-learn | Every session |
| Agent-type roster | 9 agent-type descriptions | Every session |
| Point-in-time reminders on memory reads | Each memory file read is stamped "point-in-time observation … verify against current code" | Per read — observed firsthand on all five files |

**The "store 1–3 memories per interaction" nudge that the user's global policy overrides was
not observed in this session's injected context.** Report 05 §2.2 rated its existence LIKELY
by inference from the policy text; this session can neither confirm nor locate it. What it
can confirm: no injected text in this session mandates a memory-write cadence.

No repository configuration injects memory into any session, and nothing repo-side beyond
the root `CLAUDE.md` and the skill-roster descriptions is injected per session — confirming
report 05 §6.

## 3. Persistent Claude memory — inventory and classification

Location: `/home/mohamed/.claude/projects/-projects-Dashboard-App/memory/` — 5 files,
6,174 bytes, unchanged since report 05's audit (newest mtime 2026-08-02). Every file was
read and every checkable claim re-verified against the working tree **this session**.

| # | File | Bytes | Classification | One-line reason |
|---|---|---|---|---|
| 1 | `MEMORY.md` (index) | 552 | **KEEP** | 3 entries, all durable, all pointers resolve |
| 2 | `design-preview-flat-green-direction.md` | 1,905 | **MERGE** | Doctrine body now duplicates repo docs; one dangling pointer; one non-derivable machine fact worth keeping |
| 3 | `fix-agent-context-threshold.md` | 1,293 | **KEEP** (edit recommended) | Durable user workflow rule; anchored to a skill that no longer exists |
| 4 | `local-https-dev-cert-mismatch.md` | 2,002 | **KEEP** | Machine-local environment facts; every repo-checkable claim verified exact |
| 5 | `memory-system-smoke-test.md` | 422 | **DELETE** | Self-declared throwaway; never indexed |

### 3.1 `MEMORY.md` — KEEP

Three entries; each was checked against its target file and its target file's current truth.
All resolve. If the DELETE and MERGE below are later executed, the index needs no edit for
the smoke test (it was never indexed) and one shortened line for the design memory.

### 3.2 `design-preview-flat-green-direction.md` — MERGE

Verified this session:

- **Doctrine still true in the repo:** the allowed-green list is locked at `DESIGN.md:134`
  and mirrored at `UI_STYLE_SYSTEM.md:654` (§16.3); the green `#2f6d5f` primary/accent is at
  `DESIGN.md:78,80`; dark-theme-is-interim is stated at `UI_STYLE_SYSTEM.md:49` and `:418`,
  with the gold-interim accent rows at `:589,594,603`. The memory duplicates all of this —
  which under the user's own policy ("never mirror a file that already exists") makes the
  doctrinal body repo-derivable duplication.
- **Stale pointer, re-confirmed:** `docs/design-preview/` does not exist in the working tree
  (deleted 2026-08-04 by `a675286d` per report 05 §4.2). The memory calls it "the approved
  visual spec"; the pointer dangles.
- **Non-derivable residue worth keeping:** the machine-local caveat that
  `fs.inotify.max_user_watches=65536` is too low for `ng serve` watch mode (ENOSPC) — an
  environment fact findable nowhere in the repository.

**MERGE:** trim to the inotify caveat plus at most a one-line pointer to
`DESIGN.md`/`UI_STYLE_SYSTEM.md` §16 as the doctrine's home; drop the superseded-navy
framing and the deleted `docs/design-preview/` reference; shorten the index line.

### 3.3 `fix-agent-context-threshold.md` — KEEP, with one edit

The rule — past ~350k cumulative implementer tokens, route review-fix rounds to a fresh
agent with a written handoff, and always re-verdict with the same reviewer — is durable user
feedback about multi-agent workflow, not derivable from the repository, and generalizes to
any phased implementer/reviewer loop. Squarely the policy's DO-store category.

**Stale anchor, re-confirmed this session:** the memory opens with "In `speckit-phase-loop`
runs". No such skill exists in `.claude/skills/` on this branch, and a repo-wide grep finds
the name only inside the audit reports themselves (report 05 traced it to archived commit
`b5447500`, reachable only via the `archive/abwab-attempt-1` tags). Recommended edit when a
memory-write pass is authorized: de-anchor the rule from the archived skill name ("in any
review-gated phase loop" or similar). The rule itself stays.

### 3.4 `local-https-dev-cert-mismatch.md` — KEEP

Every repo-checkable claim re-verified exact at review time:

- `Frontend/quran-dashboard-ui/localhost.pem` and `localhost-key.pem` exist.
- `package.json:7` defines `start:https` with exactly the cert flags the memory quotes.
- `playwright.config.ts` confirms the memory's Playwright preference end-to-end:
  `ignoreHTTPSErrors` (`:12`, `:28`), the `abwab` project (`:38`), the `webServer` pair
  (`:40`), and the backend launched with `--no-build` (`:53`) — matching the memory's
  "backend must be pre-built" caveat.

The rest (Chrome trust store lacks the ASP.NET dev cert, no `mkcert`/`certutil` installed,
API password in user-secrets, local Postgres DB name) is machine-local fact that cannot live
in the repository. This file and its index line are the model of what the memory-write
policy wants stored.

### 3.5 `memory-system-smoke-test.md` — DELETE

Self-description: "Throwaway smoke-test entry … safe to delete … No durable fact — delete
anytime." Written 2026-07-10, never listed in the index. Deleting it requires no index edit.

## 4. Focus checklist

| Concern | Finding in the Claude corpus |
|---|---|
| Stale feature status | **None stored.** |
| Branch/status history | **None stored.** |
| Old test counts | **None stored.** |
| Completed review chatter | **None stored.** |
| Duplicated repository truth | **One instance** — the design memory's doctrinal body (§3.2); the repo docs are the correct owner. |
| Stale skill/file references | **Two** — the `speckit-phase-loop` anchor (§3.3) and the `docs/design-preview/` pointer (§3.2). Both are the exact edits the MERGE/KEEP-with-edit classifications target. |
| Machine-local facts genuinely useful and non-derivable | **Three clusters, all worth keeping:** the inotify watch limit (§3.2), the cert/trust-store/user-secrets environment (§3.4), and the 350k fix-agent threshold (§3.3 — user judgment, not machine state, but equally non-derivable). |

The corpus contains zero entries of the categories the user's purge policy targets. Its two
blemishes are a self-labeled throwaway and pointer drift caused by the repository catching
up with (and then out-evolving) a memory written 24 days ago.

## 5. Recurring context cost

**The memory system itself creates no meaningful recurring cost.** The per-session injection
is the 552 B index; topic files load only on demand and are stamped with point-in-time
reminders. Nothing here needs mechanism changes.

Where the observed per-session injection cost actually concentrates:

1. **Repo-side (owned by reports 03/04):** the root `CLAUDE.md` (~15 KB every session) and
   the paragraph-length frontmatter descriptions of several project skills (injected into
   every session's roster regardless of task).
2. **Environment-side (outside repo remediation scope, user-configurable):** the superpowers
   SessionStart hook injecting a full skill body every session, the plugin skill roster
   (dozens of descriptions), the ~150-name deferred MCP tool listing, and four MCP server
   instruction blocks. These dwarf the memory system by orders of magnitude. Whether they
   earn their keep is a user configuration choice, not a repository defect; recorded so the
   cost is attributed to its actual source rather than to "memory".

## 6. Agreement with reports 05 and 13

This session's independent verification reproduces report 05 §4 and Sol §9.4 **exactly**:
same five files, same byte counts, same KEEP/KEEP/KEEP/MERGE/DELETE classifications, and the
directory is unchanged since both reviews (newest mtime 2026-08-02). Two boundary notes:

- **Codex corpus:** `/home/mohamed/.codex/memories/` exists and is filesystem-readable from
  this session (~791 KB incl. `rollout_summaries/`), matching Sol §9.3's inventory. It is
  **not part of this Claude session's memory system** — nothing from it is injected or
  indexed here — so it is out of this review's adjudication scope. Sol §9.3's MERGE/DELETE
  classifications for it stand un-reviewed; any action there needs its own environment-
  specific, explicitly authorized pass (Sol §7, decision 10).
- **Injection proof:** Sol noted filesystem visibility does not prove injection. This
  session **can** confirm injection for the Claude corpus: the `MEMORY.md` index content was
  demonstrably present in this session's context at start, before any file was read.

## 7. Boundaries and obligations honored

- No memory file, index line, or repository file (beyond this report) was created, edited,
  or deleted. Executing the DELETE/MERGE/edit above requires the separate explicit
  authorization Sol §7 (decision 10) reserves for persistent-memory mutation.
- Model-internal memory beyond the five exposed files: UNKNOWN; no access claimed.
- Per the user's memory-write policy and report 05 §5, **nothing from this review session
  qualifies for memory storage** — its findings live in this report and git history, and no
  new memories were written.
