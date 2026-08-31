# Playwright database modes

Playwright starts the real Angular application and API against one disposable PostgreSQL stack per
command. Critical and full commands default to deterministic `artifact` mode:

```bash
npm run e2e:critical
npm run e2e
```

`artifact` verifies `compact-cross-stack-base` through the repository trust tool, requires the exact
PostgreSQL image digest from `test-artifacts.lock.json` to be preloaded, starts it with
`--pull=never` on an internal Docker network, restores the compact dump once, and records the
ephemeral private-container connection under the gitignored `.playwright/` directory for reset
tooling. It never reads an ambient application database or user secret.

Image acquisition belongs to controlled provisioning, before sealed execution:

```bash
docker pull postgres@sha256:7341002d2b8c7c5bdd7542a671a95b36196c0b5b888daf454ae4fc33ba5346d7
```

For local iteration against a disposable clone of an existing loopback database, opt in explicitly:

```bash
E2E_DATABASE_MODE=clone-local npm run e2e
```

`clone-local` is non-canonical evidence. It accepts loopback PostgreSQL only, is rejected whenever a
CI environment marker is present, and is never selected because a connection string or user secret
happens to exist.

Tests annotated `mutating` reset state before and after their scenario. The reset truncates only the
literal allowlist in `e2e/harness/database-contract.mjs`; `permissions`, every `quran_*` table, and
therefore all PhraseSearch tables stay outside it. The reset verifies every allowlisted table is
empty, requires Linking background processors to be idle, and compares deterministic before/after
SHA-256 fingerprints of all Quran and PhraseSearch table data with the baseline captured immediately
after restore. A scenario-side mutation or reset-side mismatch fails the test run.

Critical execution discovers annotated journeys from the specifications, then runs the selected
file/line locations. It does not maintain a second journey catalogue.
