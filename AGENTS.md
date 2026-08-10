# Sol/Codex Workspace Router

This is the only root repository entrypoint for Sol/Codex in the canonical FullStack monorepo.
Repository instructions become more specific in this order: this root router, the native area
router, the nearest relevant README, then a task-triggered neutral or specialist source. A more
specific source controls unless it weakens the universal kernel below or conflicts with the
user's explicit instruction.

## Universal safety and workflow kernel

- Keep work inside the requested scope. Stop and report before expanding a phase, contract,
  schema, or task boundary.
- Treat `main` as protected Railway production. Never change or commit to it directly; stop
  before editing when the current branch is `main`.
- Do not invent or silently correct Quran data. Preserve provenance, and do not mutate source
  resources without explicit authority.
- Before editing an area, read its native area router when applicable and the nearest relevant
  README. Amend that README in the same change if the truth it describes changes.
- Do not commit, push, open or synchronize a PR, run a formal review, or deploy unless the user
  explicitly requests it.

## Native routing order

1. Any `Backend/` path routes to `Backend/AGENTS.md`.
2. Any `Frontend/quran-dashboard-ui/` path routes to
   `Frontend/quran-dashboard-ui/AGENTS.md`.
3. Read the target folder's README before specialist material; when absent, walk upward to the
   nearest relevant parent README, with the area README as the fallback.
4. Read only the precise heading or specialist source activated by the task.

This repository chain stays within the Sol/Codex entrypoints. On-demand `.agents/skills/*`
adapters remain the native Skill route.

## Trigger routing

| Trigger | Load |
| --- | --- |
| Production-source implementation | Only the needed headings of `CODING_PRINCIPLES.md`: §2 Clean Code and `Comment Policy`, §3 SOLID, §4 DRY/KISS/YAGNI, or §7 Focused Changes. |
| Active phased, specification, or contract-bound work | The active spec/task/contract and the invoked native Spec Kit Skill. Stop before expanding either. |
| Selecting, running, or reporting tests | Read `TESTING_CONSTITUTION.md`, then the nearest test README. |
| Writing or reviewing test code | Apply that testing constitution, then read `.agents/skills/test-guard/SKILL.md` and only the reference for the affected stack. |
| Pre-delivery implementation self-check | `CODING_PRINCIPLES.md` §12 and already-triggered production-code headings; include the area's file-size heading only when its threshold applies. |
| Git branch, stage, commit, push, or PR work | `.agents/skills/commit-workflow/SKILL.md`. |
| Deployment or runtime smoke | `Backend/README.md` §Deployment and `.agents/skills/deploy-smoke/SKILL.md`. |
| Formal engineering, performance, or Spec Kit review | Only the explicitly invoked native Skill and its scoped references. |

## Branching workflow

For branch, staging, commit, push, PR, and post-merge synchronization rules, use
`.agents/skills/commit-workflow/SKILL.md` and the workflow section matching the requested action.
Deployment truth lives in `Backend/README.md` §Deployment.

## Workspace Path Conventions

Planning/report locations, feature-artifact lifecycle and per-file gate, and the exact long-lived
survivor list live in `docs/README.md` under *Where things live now*, §Lifecycle, and §Long-lived
documentation. Steady-state contract pointers live in `docs/contracts/README.md`.

## Comments are forbidden by default

The canonical production-source rule, three-part exception, scope exclusions, directive
exemptions, and README destination live in `CODING_PRINCIPLES.md` §2 `Comment Policy`. Load that
heading for production-source changes.

<!-- SPECKIT START -->

## Active Spec Kit Feature

None.

- When a feature opens, record it here as: feature slug, its `specs/<feature>/plan.md`, and
  its `docs/feature-XXX-*/` decision record. Clear this section back to "None" in the same
  deletion commit that removes those artifacts, per the lifecycle rule above.

<!-- SPECKIT END -->
