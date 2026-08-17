# Claude Workspace Router

This is Claude's only root repository entrypoint for the canonical FullStack monorepo.
Repository instructions become more specific in this order: this root router, the native area
router, then a task-triggered neutral or specialist source. A more
specific source controls unless it weakens the universal kernel below or conflicts with the
user's explicit instruction.

## Universal safety and workflow kernel

- Stay within the requested scope. Stop and report before broadening a phase, contract, schema,
  or task boundary.
- `main` is protected Railway production. Never modify or commit to it directly; if the current
  branch is `main`, stop before editing.
- Never invent or silently correct Quran data. Preserve source provenance, and never mutate
  source resources without explicit authority.
- Before changing an area, load its native area router when applicable and only the sources selected
  by the matching trigger. Operational READMEs remain valid when the task actually concerns their
  commands, fixtures, tooling, deployment, or provenance.
- Do not commit, push, open or synchronize a PR, run a formal review, or deploy unless the user
  requests that action.

## Native routing order

1. For any `Backend/` path, read `Backend/CLAUDE.md`.
2. For any `Frontend/quran-dashboard-ui/` path, read
   `Frontend/quran-dashboard-ui/CLAUDE.md`.
3. Load only the exact heading or specialist source whose trigger matches the task.
4. For active feature work, derive intent from its Spec Kit artifacts and implementation truth
   from code; do not create or expect code-area READMEs.

Claude does not load a different agent's repository entrypoint as part of this routing chain.

## Trigger routing

| Trigger                                                   | Load                                                                                                                                                             |
| --------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Production-source implementation                          | Only the implicated headings of `CODING_PRINCIPLES.md`: §2 Clean Code and `Comment Policy`, §3 SOLID, §4 DRY/KISS/YAGNI, or §7 Focused Changes.                  |
| Active phased, specification, or contract-bound work      | The active spec/task/contract plus the invoked native Spec Kit Skill. Stop before broadening either.                                                             |
| Selecting, running, or reporting tests                    | Read `TESTING_CONSTITUTION.md`; use the retained Backend test or Frontend E2E README only for the applicable commands and fixtures.                              |
| Writing or reviewing retained Backend or Playwright tests | Read `TESTING_CONSTITUTION.md`, then `.claude/skills/test-guard/SKILL.md` and only its relevant retained-stack reference.                                        |
| Pre-delivery implementation self-check                    | `CODING_PRINCIPLES.md` §12 and production-code headings already implicated; add the area structure document's file-size heading only when its threshold applies. |
| Git branch, stage, commit, push, or PR work               | `.claude/skills/commit-workflow/SKILL.md`.                                                                                                                       |
| Deployment or runtime smoke                               | `Backend/README.md` §Deployment and `.claude/skills/deploy-smoke/SKILL.md`.                                                                                      |
| Formal engineering, performance, or Spec Kit review       | Only the explicitly invoked native Skill and its scoped references.                                                                                              |

## Branching workflow

For branch, staging, commit, push, PR, and post-merge synchronization rules, use
`.claude/skills/commit-workflow/SKILL.md` §Branch model and the workflow section matching the
requested action. Deployment truth lives in `Backend/README.md` §Deployment.

## Workspace Path Conventions

Planning/report locations, the feature-artifact lifecycle and per-file gate, and the exact
long-lived survivor list live in `docs/README.md` under _Where things live now_, §Lifecycle, and
§Long-lived documentation. Steady-state contract pointers live in `docs/contracts/README.md`.

## Comments are forbidden by default

The canonical production-source rule, its three-part exception, scope exclusions, and directive
exemptions live in `CODING_PRINCIPLES.md` §2 `Comment Policy`. Load that
heading when changing production-source code.

<!-- SPECKIT START -->

## Active Spec Kit Feature

`specs/001-abwab-door-inclusions/`

<!-- SPECKIT END -->
