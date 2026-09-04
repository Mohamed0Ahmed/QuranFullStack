# Pull-request observation matrix

`pr-observation-matrix.json` is the provider-neutral source of truth for four independent pre-merge
jobs. Local operators invoke the repository commands directly; no CI provider, remote runner, or cloud
resource is required for the adopted Local-first scope. The Backend job runs the thin
`test-backend pre-pr` delegate, which execs repository-root `scripts/test pre-pr` against the
persistent `quran_dashboard_test` capability. There is no compact-fixture or full-canonical dump
candidate.

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

The runner starts its clock immediately before the first provisioning command. Its one outer 12-minute
deadline therefore includes locked dependency
provisioning, database preparation, application startup, and test execution, but excludes provider
queue time. It writes `job-result.json` under `.pr-observation/` by default. Callers may pass a
results directory with `--results-dir`; Backend TRX and sealed Playwright evidence remain below that
job directory or in their existing harness evidence directory.

All four jobs have exactly one attempt. The Backend, contract/model, and Frontend policy/build jobs are
blocking Local-first regression gates. The critical Chromium job uses group-level enforcement: it still
executes the full nine-journey catalogue once, records the full first-attempt result, and evaluates each
declared journey group independently. Quran fidelity is blocking after its accepted #102 pilot, and
sessions/Permissions is blocking after the accepted shared #103 pilot. Linking is blocking after the
accepted shared #104 pilot. PhraseSearch is blocking after the accepted shared #105 pilot, and Abwab
projection is blocking after the accepted shared #106 pilot. All five groups are therefore enforced
independently. The group contract still preserves complete first-attempt evidence and would retain any
governed future observation failure without rewriting the failed catalogue status.

The final owner scope makes the three unchanged non-browser jobs blocking without another activation
pilot so failures in code, business logic, contracts, database invariants, policy, type-checking, or
build cannot exit successfully. This narrow decision adds no commands or tests and does not waive the
completed activation criteria for the five critical journey groups or for any future job.

`job-result.json` therefore keeps `status` and `firstAttemptStatus` for the full catalogue and adds
`enforcementStatus` plus separate `journeyGroups` timing/status evidence. Every configured journey must
produce exactly one result: missing, malformed, duplicated, undeclared, or retried evidence anywhere in
the catalogue fails closed even when the affected group is still observation-only. Sealed infrastructure
phases and the sanitized evidence inspection must also pass. Present test failures in observation groups
remain visible but non-blocking. Provisioning or timeout failures block because the required Quran group
and complete monitoring record were not proven. A results directory must be new or empty, so a later
invocation cannot reuse stale Playwright evidence. The runner never retries or converts a failed first
attempt into a flaky pass.

The accepted #102 window covered 20 full-catalogue first-attempt passes at commit
`e8ae1c3d92429f34bd03e13bac86362f3b2f1e04`, including five distinct initially empty provisioning/cache
roots. Every job completed below 12 minutes; nearest-rank p95 was 544,813 ms and the maximum was 560,318
ms. The two Quran journeys passed 40/40, with group p95 7,783 ms and maximum 8,400 ms. The repository has
no remote artifact-fetch cache, so the five cold observations are accurately retained as fresh isolated
dependency/browser provisioning runs. The earlier failed candidate
windows remain recorded on #102 and are not relabeled as qualifying evidence.

#103 reuses that same owner-approved 20-run full-catalogue window rather than claiming duplicate runs.
Both `device-session.lifecycle` and `permission.lifecycle` ran on every execution and passed 40/40 in
aggregate. Their per-run group maximum was 29,345 ms and nearest-rank p95 was 28,367 ms. The shared job
maximum and p95 remain 560,318 ms and 544,813 ms respectively, so sessions/Permissions is now blocking
without depending on Quran's activation decision.

#104 also reuses the same owner-approved window without claiming duplicate runs. Both
`linking.successful-owner` variants ran on every execution and passed 40/40 in aggregate. Their per-run
group maximum was 24,383 ms and nearest-rank p95 was 24,032 ms. The shared job maximum and p95 remain
560,318 ms and 544,813 ms respectively, so Linking is now blocking without depending on either earlier
journey-group activation decision.

#105 uses one owner-approved 20-run full-catalogue window at commit
`38abe5bd345888a28dcc7b95e89a602a9aa422e0`, including five initially empty provisioning/cache roots.
All 20 qualifying runs passed on their first attempt with no retry, each finished below 12 minutes, and
the shared job maximum and nearest-rank p95 were 642,868 ms and 542,083 ms. Both PhraseSearch journeys
passed 40/40; their per-run group maximum was 14,735 ms and nearest-rank p95 was 14,274 ms. The failed
Docker-registry attempt 11 and operator-aborted attempt 12 remain recorded and uncounted rather than
being retried or relabeled as qualifying passes. PhraseSearch is now blocking while Abwab remains
independently observable for #106.

#106 reuses the same owner-approved window without claiming duplicate runs. The Abwab mutation and
fresh projection journey ran once on every qualifying execution and passed 20/20. Its per-run group
maximum was 18,048 ms and nearest-rank p95 was 16,687 ms. The shared job maximum and p95 remain 642,868
ms and 542,083 ms respectively. The uncounted infrastructure failure and operator-aborted attempt remain
recorded with #105; neither is relabeled as Abwab evidence. Abwab is now blocking independently of the
other four journey-group decisions.

Monitoring remains active because the full catalogue and per-group timing evidence continue on every
run. Any emergency downgrade still requires the issue, owner, maintainer approval, rationale, affected
risk, and maximum seven-day expiry defined by the strategy governance.

The Backend job deliberately invokes the supported full `pre-pr` lane and names its current
full-canonical input requirement. There is no compact-fixture exception. If measurement later requires
one, the matrix must name the omitted full-canonical classes and their scheduled/release home; a
reduced lane must never be relabeled as the current full Backend lane.
