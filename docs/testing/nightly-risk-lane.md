# Nightly risk lane

`nightly-risk-lane.json` and `scripts/run-nightly-risk-lane.mjs` define the repository's
provider-neutral nightly risk contract. The repository does not configure a remote scheduler, runner,
or artifact upload. A provider adapter may allocate an authorized runner and invoke the contract after
it supplies its local, immutable artifact root and a new local results directory.

```bash
node scripts/run-nightly-risk-lane.mjs \
  --artifact-root <authorized-local-artifact-root> \
  --results-dir <new-local-results-directory>
```

The runner accepts `--dry-run` for command inspection. It does not print command arguments, artifact
locations, child output, credentials, or connection information. Its input artifact root is forwarded
only to the existing artifact verification and PhraseSearch rehearsal contracts. Every child receives a
new private home, config, and package-cache root rather than the caller's connection strings,
credentials, or user configuration. The existing contracts remain responsible for their own private
disposable database and sealed browser setup.

## Evidence and execution

The primary attempt has a six-hour outer deadline, which covers all independently bounded primary
commands. It runs:

- Sealed browser provisioning owns the single locked Backend restore and build for all Backend contracts.
- Existing sealed browser provisioning, then the full Chromium suite. The structured Playwright evidence
  must contain the approved Mushaf and Linking mobile journeys. The sealed harness verifies its compact
  artifacts, retains approved reporter artifacts, and continues to block serious and critical
  accessibility violations while retaining lower-severity observations.
- Content-addressed full-canonical artifact verification from `<artifact-root>/sha256/<locked-payload-hash>`,
  followed by the release-only PhraseSearch one-shot
  build/activation rehearsal. The rehearsal owns its exclusive eligible state and its one restore; the
  lane does not add a second restore just to populate a nightly result.
- Existing isolated Abwab snapshot and Quran topics import protection classes. Their fixtures own the
  disposable state and safety rules; the lane does not recreate importer or snapshot logic.

Commands run after their explicit prerequisites pass, rather than stopping at the first unrelated
failure. Browser failure therefore does not suppress independently eligible artifact and operational
tracks. Each operational track must emit exactly one TRX file and a runner-owned lane receipt; the
nightly runner rejects zero executed tests, skips, failures, unexpected classes, or a mismatched lane.

Every command produces its ID, phase, status, duration, exit code, signal, and safe evidence-check IDs
in the atomic `nightly-risk-result.json`. Browser timing is retained in the same structured result as
non-blocking observation; no timing budget is enforced. The existing sealed reporter remains the only
retained browser diagnostic source and rejects database dumps, headers, bodies, and unapproved diagnostics.
Provider handoff may retain the structured nightly result, existing contract-approved structured
operational evidence, and approved sealed browser artifacts for the 14-day failed-diagnostic and
30-day aggregate-timing windows. Upload, retention storage, and access control remain provider-owned
and are deliberately not configured here. A timed-out browser attempt has its unverified evidence
directory removed rather than being eligible for handoff.

Dependency advisory evaluation is excluded. The manifest rejects advisory commands; the separate
weekly, lockfile-change, and release contract remains the only advisory path.

## Diagnostic retry

`--diagnostic-retry` permits one rerun only for the manifest-designated full Chromium command after all
eligible primary commands complete if its primary attempt fails or times out. It reuses the existing sealed provisioning receipt and
writes separate diagnostic evidence. The primary evidence is never changed, the primary status remains
authoritative, and a passing diagnostic rerun cannot produce a successful result or exit code. Any
failed prerequisite marks only its dependents not run. On browser timeout or runner cancellation, the
runner invokes the scoped sealed-runtime cleanup handshake before retry or exit. It does the same after
every failed browser attempt and verifies the sealed runner's cleaned ownership receipt after a successful
attempt. A failed zero-runtime proof blocks diagnostic retry and every later database-owning track.
Incomplete browser
evidence is deleted, while failed evidence is retained only after the same sanitization and completeness
checks used for a passed browser run.

## Runner-owned gates

An authorized scheduled runner must provide the lock-pinned local artifact root, required preloaded
dependencies/images/browser, and a private disposable Docker-capable environment. It must apply the
existing network-sealing and credential-free execution requirements, preserve the result directory as
new or empty, and select a retention destination that accepts only the allowed sanitized evidence.
These repository files neither schedule the lane nor authorize it against a shared, staging, or
production database.
