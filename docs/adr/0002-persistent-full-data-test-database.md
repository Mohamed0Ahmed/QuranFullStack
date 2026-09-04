---
status: accepted
date: 2026-09-03
---

# Use a persistent full-data database for local automated testing

Quran Dashboard will run ordinary Backend and Playwright database tests against a persistent PostgreSQL
18 database named `quran_dashboard_test`. It is independently provisioned from committed migrations and
the repository's canonical import, rebuild, and generation pipelines; normal test runs never rebuild,
clone, restore, recreate, or migrate it. Its Quran data, System Catalogue, and schema are protected,
while its explicitly classified Mutable Application State is disposable and reset between stateful
scenarios. The developer-owned `quran_dashboard` database is independent state and automated testing
neither reads nor mutates it.

All database-backed execution goes through one Backend-owned test-runtime control plane. The control
plane enforces PostgreSQL roles, database markers, shared/exclusive advisory locking, API activity
profiles, protected-state fingerprints, reset boundaries, scratch ownership, and structured evidence.
Destructive import, migration, catalogue, recovery, index-build, and schema-drift tests use runner-owned
empty scratch databases or an explicitly and manually provisioned full rehearsal database. There is no
automatic Testcontainers, compact-artifact, database-clone, or dump-restore fallback.

This replaces the test database lifecycle built around disposable PostgreSQL containers and compact or
full-canonical test artifacts. The trade-off is an explicit, occasionally expensive capability refresh
and serialized mutation in exchange for testing against complete canonical data, eliminating per-run
database reconstruction, and making protection of Quran data enforceable. The 12-minute pre-PR budget
remains a measured target for active gate time rather than a correctness gate.

## Retained decisions

[ADR 0001](./0001-playwright-only-frontend-testing.md) remains authoritative for Playwright as the only
Frontend test convention, Chromium scope, and risk-based browser coverage. Independent reviewed Quran
and PhraseSearch oracles, locked dependency/browser provisioning, credential stripping, loopback-only
egress, and sanitized diagnostics also remain required.

## Consequences

The target architecture and its rollout boundary are specified in
[Persistent Full-Data Test Database Architecture](../testing/persistent-test-database-architecture.md).
That document also lists every testing document and machine-readable contract superseded by this ADR.
The decision is accepted. Ticket #169 contracted the former container and dump lifecycle; current code
and operational commands are implementation truth for the persistent Test Database architecture.
