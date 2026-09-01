# Local-first pre-merge verification

`release-candidate-lane.json` and `scripts/run-release-candidate-lane.mjs` define the final Local-first
verification gate for a candidate branch before integration. The historical executable identifier
remains `release-candidate`, while manifest schema version `2` declares the
`local-first-pre-merge` execution scope.

The gate does not provision or contact staging, cloud, live Logto, deployment, or Production resources.
It accepts no external attestations and runs no automated checks against Production. Live-provider and
deployment verification are explicitly deferred owner concerns outside this regression-testing scope.

The operator supplies the adopted content-addressed local artifact root, a new local results directory
outside the candidate repository, and a capacity-backed `TMPDIR` outside the candidate repository.
Keeping results and temporary database/recovery files outside the checkout lets the runner prove that
the exact candidate stays clean before and after execution. The runner creates a private execution home
beneath `TMPDIR`, propagates it as `TMPDIR`, `TMP`, and `TEMP` to every child, and removes it when
execution finishes.

```bash
TMPDIR=<capacity-backed-local-temp-root> node scripts/run-release-candidate-lane.mjs \
  --artifact-root <authorized-local-artifact-root> \
  --results-dir <new-local-results-directory> \
  --confirm-backup \
  --candidate <current-immutable-git-commit>
```

The runner executes exactly six existing gates:

1. Locked Backend restore.
2. No-restore Backend build.
3. Content-addressed full-canonical artifact verification.
4. Previous-release upgrade rehearsal in isolated disposable PostgreSQL.
5. Full-canonical backup/restore recovery rehearsal in isolated disposable PostgreSQL.
6. Release-trigger dependency advisory evaluation.

`--confirm-backup` remains mandatory because backup intent is never inferred. The two database
rehearsals depend on artifact verification, use project-owned disposable containers, and must prove
cleanup. Every command is a single first attempt; there is no retry conversion. A candidate passes only
when all six gates, their evidence validators, candidate checks, and required cleanup pass. Otherwise
the receipt is `failed`, `timed-out`, or `cancelled`.

Critical Playwright regression journeys retain their existing activated Local-first protection and
evidence under the PR observation matrix. This gate does not duplicate their completed activation
pilots or create another journey catalogue.
