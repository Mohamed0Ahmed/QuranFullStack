# Sol/Codex Workspace Router

This is the only root repository entrypoint for Sol/Codex in the canonical FullStack monorepo.
Repository instructions become more specific in this order: this root router, the native area
router, then a task-triggered neutral or specialist source. A more
specific source controls unless it weakens the universal kernel below or conflicts with the
user's explicit instruction.

## Universal safety and workflow kernel

- Keep work inside the requested scope. Stop and report before expanding a phase, contract,
  schema, or task boundary.
- Treat `main` as protected Railway production. Never change or commit to it directly; stop
  before editing when the current branch is `main`.
- Do not invent or silently correct Quran data. Preserve provenance, and do not mutate source
  resources without explicit authority.
- Before editing an area, read its native area router when applicable and only the sources selected
  by the matching trigger. Operational READMEs remain valid when the task actually concerns their
  commands, fixtures, tooling, deployment, or provenance.
- Do not commit, push, open or synchronize a PR, run a formal review, or deploy unless the user
  explicitly requests it.

## Native routing order

1. Any `Backend/` path routes to `Backend/AGENTS.md`.
2. Any `Frontend/quran-dashboard-ui/` path routes to
   `Frontend/quran-dashboard-ui/AGENTS.md`.
3. Read only the precise heading or specialist source activated by the task.
4. For active feature work, derive intent from its Spec Kit artifacts and implementation truth
   from code; do not create or expect code-area READMEs.

This repository chain stays within the Sol/Codex entrypoints. On-demand `.agents/skills/*`
adapters remain the native Skill route.

## Trigger routing

| Trigger                                                   | Load                                                                                                                                             |
| --------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| Production-source implementation                          | Only the needed headings of `CODING_PRINCIPLES.md`: §2 Clean Code and `Comment Policy`, §3 SOLID, §4 DRY/KISS/YAGNI, or §7 Focused Changes.      |
| Active phased, specification, or contract-bound work      | The active spec/task/contract and the invoked native Spec Kit Skill. Stop before expanding either.                                               |
| Selecting, running, or reporting tests                    | Read `TESTING_CONSTITUTION.md`; use the retained Backend test or Frontend E2E README only for the applicable commands and fixtures.              |
| Writing or reviewing retained Backend or Playwright tests | Read `TESTING_CONSTITUTION.md`, then `.agents/skills/test-guard/SKILL.md` and only its relevant retained-stack reference.                        |
| Pre-delivery implementation self-check                    | `CODING_PRINCIPLES.md` §12 and already-triggered production-code headings; include the area's file-size heading only when its threshold applies. |
| Git branch, stage, commit, push, or PR work               | `.agents/skills/commit-workflow/SKILL.md`.                                                                                                       |
| Deployment or runtime smoke                               | `Backend/README.md` §Deployment and `.agents/skills/deploy-smoke/SKILL.md`.                                                                      |
| Formal engineering, performance, or Spec Kit review       | Only the explicitly invoked native Skill and its scoped references.                                                                              |

## Branching workflow

For branch, staging, commit, push, PR, and post-merge synchronization rules, use
`.agents/skills/commit-workflow/SKILL.md` and the workflow section matching the requested action.
Deployment truth lives in `Backend/README.md` §Deployment.

## Workspace Path Conventions

Planning/report locations, feature-artifact lifecycle and per-file gate, and the exact long-lived
survivor list live in `docs/README.md` under _Where things live now_, §Lifecycle, and §Long-lived
documentation. Steady-state contract pointers live in `docs/contracts/README.md`.

## Comments are forbidden by default

The canonical production-source rule, three-part exception, scope exclusions, and directive
exemptions live in `CODING_PRINCIPLES.md` §2 `Comment Policy`. Load that
heading for production-source changes.
