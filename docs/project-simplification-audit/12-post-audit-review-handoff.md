# 12 — Post-Audit Independent Review Handoff

Audited baseline: branch `dev`, commit `72792ba9ff589c66aa25632a464b56b8bf7787af`.

This file contains the two ready-to-use prompts required by brief §28. Paste each verbatim into the target model's session. Both prompts forbid invented memory access and request no chain-of-thought.

---

## A. Sol — independent audit review

**How to run:** open a fresh Sol session in the repository root at the audited commit (verify `git rev-parse HEAD` = `72792ba9ff58…`; the working tree may additionally contain only `docs/project-simplification-audit/`). Paste:

```text
You are performing an INDEPENDENT adversarial review of a completed project-simplification
audit of this repository (Quran Dashboard / المنهج القرآني monorepo).

The audit pack lives in docs/project-simplification-audit/:
- PROJECT_SIMPLIFICATION_AUDIT_BRIEF.md  (the audit contract — read first)
- 00-audit-index.md … 12-post-audit-review-handoff.md  (nine topic reports + synthesis)
- data/*.json  (machine-readable inventories and runtime measurements)
- api-explorer/ (static HTML endpoint explorer + data.json)

The audit was produced by Fable and already survived one internal adversarial-verification
round; your job is to be a HOSTILE second reviewer, not to summarize it.

Ground rules:
1. READ-ONLY. Modify nothing, run no destructive commands, no migrations, no DB mutation,
   no commits. Safe read/measure commands are allowed. Respect the repo's test-DB
   serialization rules if you run any test lane (Backend/scripts/test-backend; never two
   PostgreSQL test processes at once).
2. The repository is the source of truth, not the reports. For every conclusion you assess,
   independently inspect the load-bearing evidence (recompute counts, open cited file:line).
3. Use the audit's own evidence standard: CONFIRMED / LIKELY / NEEDS_MEASUREMENT / UNKNOWN.
   Flag any claim whose tag you cannot reproduce.
4. Do not reveal chain-of-thought; report conclusions and evidence only.

Required work:
A. Read the entire audit pack.
B. Challenge the conclusions: hunt specifically for (i) unsafe simplification
   recommendations — anything that would weaken authentication, authorization, Owner rules,
   direct permissions, account status, audit, optimistic concurrency, transactions, DB
   invariants, Quran text integrity, source provenance, import validation, canonical source
   checks, migration safety, OpenAPI parity, security-sensitive errors, or RTL/Quran
   typography (brief §29); (ii) savings arithmetic that does not reproduce; (iii) CONFIRMED
   tags you cannot reproduce; (iv) missing dependents of any proposed change.
C. Review each area on its own evidence:
   - test rationalization (report 02): are the MERGE/REWRITE clusters real duplication?
     is any DELETE_CANDIDATE missing replacement coverage?
   - instruction/context changes (03): is the pointer-stub direction safe given that Codex
     CLI activity is CONFIRMED (refs/codex checkpoints) and Cursor status is UNKNOWN?
   - custom skills (04): verify the two .agents adapter defects and the orphaned-rule claim.
   - README/document cleanup (06): could the proposed tail compression lose any
     safety-bearing invariant that exists only in a README?
   - Tailwind/style recommendations (07): does any path risk RTL/logical-property
     regressions or the token system?
   - API shrinkage/deprecation (09 + api-explorer): are the 2 deprecation candidates and
     55 unused-candidate fields safe under the out-of-repo-consumer caveat? is the
     audit-events projection change truly storage-preserving?
   - architecture simplification (08): is the words-state consolidation net-positive, and
     are the explicitly-rejected flattenings correctly rejected?
   - build/gate changes (10): does scoping the freshness re-verification loop create a
     regression escape hatch?
D. Produce a verdict PER WORKSTREAM (WS1–WS9 as defined in 11-cross-cutting-priorities.md):
   AGREE / AGREE-WITH-CHANGES (list them) / REJECT (with evidence), plus an overall
   assessment of whether the audit pack is a sufficient basis for small remediation plans
   without another repository-wide discovery pass.
E. SEPARATELY: report only the project memory/context YOU can actually access in this
   environment (persistent memories, injected instructions, tool-config context). Inventory
   it, identify stale/redundant entries, and classify each KEEP / MERGE / DELETE. Never
   claim access to hidden or private memory; if you have no persistent memory, say exactly
   that.

Deliverable: a single markdown review with sections A–E, every challenged claim carrying
file:line or recomputation evidence.
```

---

## B. Claude — memory/context review

**How to run:** open a fresh Claude Code session in this repository (any clean state). The audit's evidence base for this review is `05-memory-context-audit.md` §3 (the classification table). Paste:

```text
Perform a memory/context review for this project. Report ONLY memory and context you can
actually access in this session — never infer, never invent, and do not claim access to
model-internal memory beyond what your environment exposes as files or injected context.

Keep three categories strictly separate:
  (1) repository instructions (CLAUDE.md chain, READMEs, .architecture, skills),
  (2) tool/config context (settings, adapters, rules files),
  (3) persistent memory (your file-based memory directory and its index, if present).

Tasks:
1. Inventory category (3) exactly as found: file name, type, description line, size, and
   what the body asserts.
2. For each memory, verify its claims against the repository TODAY (files it references,
   commits it cites, skills it names), then classify:
     KEEP   — durable, true, not derivable from the repo;
     MERGE  — partially stale or duplicating repo truth; name the surviving residue
              (preserve non-derivable facts such as machine-local environment caveats);
     DELETE — stale, throwaway, feature-status chatter, or fully derivable from the repo.
   Pay special attention to: old feature status, branch status, test counts,
   completed-review chatter, duplicated product/design decisions, and facts that belong in
   repository docs instead of memory.
3. Cross-check against docs/project-simplification-audit/05-memory-context-audit.md §3:
   state where you agree or disagree with its KEEP/MERGE/DELETE calls and why. Its
   classifications are evidence, not orders — your own verification wins.
4. List any injected context you can observe this session (system-level instructions,
   auto-loaded indexes, hooks) WITHOUT reproducing secrets, and note anything that injects
   stale or per-feature information on every task.
5. Apply nothing yet: output the classification table and wait for explicit approval before
   deleting or editing any memory file.

Do not expose chain-of-thought. Output: one table (memory → classification → evidence →
action-on-approval) plus a short list of observed per-session injections.
```

---

## C. Notes for whoever runs these

- **Order:** Sol's review (A) should run before any remediation planning; the Claude memory review (B) can run anytime and gates only WS9.
- **Sol's memory addendum (A, part E)** and the Claude review (B) answer the same question from two environments; differences between their inventories are themselves findings (environment-injected vs repo-resident context).
- **Known open measurement gaps** the reviewers should not mistake for oversights (declared in `00-audit-index.md`): e2e runtime, canonical-data lane and dump timings, wire payload bytes, per-feature gate-firing frequency, runtime (vs prescribed) context reads, Cursor editor usage, and spec-artifact sizes (no feature was open at baseline).
- **After both reviews:** remediation is to be split into multiple small plans per workstream (report 11), each opened as its own feature under the repo's normal Spec-Kit + lifecycle rules. Per brief §33, this audit ends at review-readiness — nothing here is an implementation instruction.
