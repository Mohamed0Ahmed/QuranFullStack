# Pull-request observation matrix

`pr-observation-matrix.json` is the provider-neutral source of truth for four independent pull-request
jobs. A CI provider should fan out one allocated runner per job and invoke the same repository command:

```bash
node scripts/run-pr-observation-job.mjs --job backend-pr
node scripts/run-pr-observation-job.mjs --job api-contract-model
node scripts/run-pr-observation-job.mjs --job frontend-policy-build
node scripts/run-pr-observation-job.mjs --job critical-chromium
```

List or inspect the jobs without executing them:

```bash
node scripts/run-pr-observation-job.mjs --list
node scripts/run-pr-observation-job.mjs --job critical-chromium --dry-run
node scripts/verify-pr-observation-matrix.mjs
```

The runner starts its clock after provider allocation, immediately before the first provisioning
command. Its one outer 12-minute deadline therefore includes locked dependency and artifact
provisioning, database preparation, application startup, and test execution, but excludes provider
queue time. It writes `job-result.json` under `.pr-observation/` by default. Providers may pass an
artifact directory with `--results-dir`; Backend TRX and sealed Playwright evidence remain below that
job directory or in their existing harness evidence directory.

All four jobs are observation-only and have exactly one attempt. A failed command stops that job, is
recorded as the first-attempt status, and is returned to the provider as non-blocking evidence. The
runner does not retry or convert a failure to a pass. Enforcement is separate future activation work
after the required timing and flake pilot.

The Backend job deliberately invokes the supported full `pre-pr` lane and names its current
full-canonical input requirement. There is no compact-fixture exception. If measurement later requires
one, the matrix must name the omitted full-canonical classes and their scheduled/release home; a
reduced lane must never be relabeled as the current full Backend lane.
