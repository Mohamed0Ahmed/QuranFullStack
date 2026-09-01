# Release candidate lane

`release-candidate-lane.json` and `scripts/run-release-candidate-lane.mjs` define the
provider-neutral release-candidate gate. They do not schedule work, configure a provider, contact a
staging environment, or configure a Logto tenant.

An authorized release operator supplies a lock-pinned local artifact root, a new local results
directory outside the candidate repository, a capacity-backed `TMPDIR` outside the candidate
repository, and a directory containing only the three sanitized attestation documents below. Keeping
results and temporary database/recovery files outside the checkout lets the runner prove that the
candidate stays clean before and after execution. The runner creates a private execution home beneath
`TMPDIR`, propagates it as `TMPDIR`, `TMP`, and `TEMP` to every child, and removes it when execution
finishes. The runner does not print those locations or their contents.

```bash
TMPDIR=<capacity-backed-local-temp-root> node scripts/run-release-candidate-lane.mjs \
  --artifact-root <authorized-local-artifact-root> \
  --external-evidence-dir <sanitized-owner-attestations> \
  --results-dir <new-local-results-directory> \
  --confirm-backup \
  --candidate <current-immutable-git-commit>
```

The runner performs a locked restore followed by a no-restore build, then composes content-addressed full-canonical
artifact verification, previous-release upgrade rehearsal, backup/restore recovery rehearsal, and
`dependency-advisory-evaluation` with its existing `release` trigger. `--confirm-backup` is mandatory:
the lane never infers backup intent. The two database rehearsals require the artifact verification to
pass. Each command is a first attempt; no retry can convert a failed component into success.

## Owner attestations

The external evidence directory must contain exactly these JSON files. Each uses schema version `1`, the
immutable current Git candidate, a safe symbolic run ID, a UTC completion time, and a sanitization declaration that all five flags are
`false`: `credentials`, `rawUrls`, `requestBodies`, `responseBodies`, and `databaseDumps`.

| File | Required evidence |
| --- | --- |
| `isolated-staging-critical-journeys.json` | Dedicated non-shared staging state, an immutable deployment identity, the adopted canonical artifact SHA-256, complete first-attempt critical Playwright catalogue evidence, and artifact verification. |
| `real-logto-sentinel.json` | One serialized run, at least two dedicated identities, and passed redirect, callback, logout, identity-mapping, session-bootstrap, and approved profile/reconciliation checks. |
| `manual-release-charter.json` | Passed representative typography, assistive-technology, restore, and provider-configuration review. |

Every attestation must bind the exact candidate supplied to the invocation; the staging deployment identity is also mandatory. The contract intentionally permits no URLs, identity values, credentials, request/response bodies,
database artifacts, or free-form notes in these documents. The runner retains only component IDs and
their `passed`, `failed`, `stale`, or `unavailable` classification in its result. Missing documents are
`unavailable`; a reported failure or malformed/sensitive shape is `failed`; a passed but future or older
than 24-hour document is `stale`. None is silently promoted to completion.

The real Logto and manual steps remain owner-run evidence because this repository deliberately has no
provider credentials or staging authorization. Their absence therefore blocks the lane rather than
creating synthetic release evidence.
