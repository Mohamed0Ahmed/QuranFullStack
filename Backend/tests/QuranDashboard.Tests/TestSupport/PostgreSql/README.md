# PostgreSQL test runtime

The whole Backend test project gets its databases from here. One `postgres:16-alpine` container
per test process, one migrated template inside it, and one database per collection cloned from
that template. No fixture starts its own container, and the one fixture that cannot join the
shared runtime takes an *exclusive* lease through the same code path so the two can never
overlap.

Which lane runs what is `../../../../../TESTING_STRATEGY.md` §3. Which fixture leases what is
`../../README.md`. This file is the mechanics, and every claim below names the line that
implements it.

## The two lease shapes, and which one a fixture may take

| Call | Template | Use it when |
|---|---|---|
| `PostgreSqlTestProcess.LeaseMigratedDatabaseAsync` (`PostgreSqlTestProcess.cs:26-32`) | a clone of the migrated template (`PostgreSqlTestServer.cs:146-148`) | the fixture's starting point **is** migration head |
| `PostgreSqlTestProcess.LeaseEmptyDatabaseAsync` (`PostgreSqlTestProcess.cs:34-40`) | `template0` — no schema at all (`PostgreSqlTestServer.cs:146-148`) | the test's subject is getting *to* head, or the absence of a schema |

**The migrated template is the default.** Every ordinary database fixture takes it: the Access
service and schema-drift fixtures, the route-Smoke API host, the Abwab schema fixture, and the
foundation/Translations/Tafsirs/Mutashabihat/Navigation/Morphology/SimpleI3rab/FullI3rab/
WordsDisplay pipeline fixtures, plus the five explorer fixtures — Roots, UniqueWords,
MushafReader, WordTypes, MorphologyExplorers — which reach it through
`ExternalReadOnlyDatabaseOptIn.TryLease(...) ?? LeaseMigratedDatabaseAsync(...)`
(`../../Quran/WordsRoots/RootsExplorerTestFixture.cs:23-25` is the shape all five share). There
are **no `EnsureCreatedAsync` call sites left in this test project** — `grep -rn EnsureCreated
Backend/tests --include='*.cs'` returns nothing — so "current-model schema creation" is not a
second template anyone has to reason about.

**The template is forbidden to a test whose subject is the migration path itself.**
`../../Api/Access/AccessMigrationTestFixture.cs:17` leases an *empty* database and hands each case its own
`PostgreSqlSchemaLease`, because a staged upgrade that starts from a named earlier migration
cannot begin at head — a head clone would assert nothing. The same reasoning excludes any
future deliberate pre-head schema mutation or refusal case. The EF pending-model check is not a
lease at all: it is `../../../../scripts/check-pending-model`, run against the developer's own
database.

The template is built lazily, exactly once per process, on the first migrated lease
(`PostgreSqlTestServer.cs:31-33, 196-237`): create from `template0`, run the real EF migration
chain (`:217`), fail if anything is still pending (`:219-225`), clear the build pool (`:228`),
then `ALLOW_CONNECTIONS false` and `IS_TEMPLATE true` (`:229-234`). Clones are named
`qdb_test_<owner-slug>_<pid>_<counter>_<random>`, bounded to PostgreSQL's 63-byte identifier
limit and quoted with `NpgsqlCommandBuilder.QuoteIdentifier`
(`PostgreSqlDatabaseName.cs:8-9, 32-35, 65-87`).

Concurrent leases are capped by `QURAN_DASHBOARD_TEST_DB_PARALLELISM`, an integer 1–4 defaulting
to 4; anything else is refused by name (`PostgreSqlTestServer.cs:40-62`). The runner exports 4
(`../../../../scripts/test-backend:456`). Raising the ceiling is a measured lifecycle decision,
not a config edit.

## Why `Smoke/Data` owns an exclusive `postgres:18-alpine` server

The canonical dump is written by an 18 `pg_dump` and a 16 `pg_restore` refuses it; restoring the
18 archive onto a 16 server with an 18 client fails too, on `transaction_timeout`. Both halves
were measured, and the measurement is recorded once, in `../../README.md` — *Why `Smoke/Data/`
runs postgres 18 while every other fixture runs 16*. Do not restate it here.

What matters at this layer is that the divergence is **structural**, so it was resolved at the
process level rather than by weakening either side. `../../Smoke/Data/SmokeDataFixture.cs:54-59` takes
`PostgreSqlTestProcess.LeaseExclusiveServerAsync` instead of joining the shared runtime, and
three independent mechanisms keep the two majors from ever running at once:

1. **Within one test process** the two are mutually exclusive. Asking for an exclusive server
   after the shared runtime was requested throws (`PostgreSqlTestProcess.cs:69-75`), and asking
   for the shared runtime while an exclusive lease is live throws
   (`PostgreSqlTestProcess.cs:94-104`).
2. **Across processes** both hold the same OS lock — the shared server at
   `PostgreSqlTestServer.cs:67-69`, the exclusive one at `ExclusivePostgreSqlLease.cs:46-48`.
3. **Across a lane** `../../../../scripts/test-backend` splits any selection containing
   `QuranDashboard.Tests.Smoke.Data.SmokeDataReadTests` *and* at least one other class into two
   sequential `dotnet test` invocations (`test-backend:19, 388-408`), waiting between them until
   no labelled PostgreSQL container is running (`test-backend:562-586`).
   `TestGateCatalogTests.PostgreSqlOwnershipShards_PartitionTheLaneTheyReplace`
   (`../Execution/TestGateCatalogTests.cs:196`) proves the two shards never overlap and always
   add back up to the lane they replace.

The exclusive lease is otherwise an ordinary member of this runtime: it carries the same five
labels, and it applies them **after** the caller's container configuration so a caller cannot
overwrite one (`ExclusivePostgreSqlLease.cs:52-63`).

## Ownership labels

Every project-owned PostgreSQL container carries all five
(`PostgreSqlResourceLabels.cs:9-17, 23-33`):

```text
com.qurandashboard.test.owner=backend-tests
com.qurandashboard.test.repository=quran-dashboard
com.qurandashboard.test.kind=postgresql
com.qurandashboard.test.run-id=<32 lowercase hex>
com.qurandashboard.test.host-pid=<decimal test-host PID>
```

The run ID comes from `QURAN_DASHBOARD_TEST_RUN_ID` when the runner exported one
(`test-backend:454-455`), and is otherwise a process-local GUID; a value that is not 32 lowercase
hex characters is refused at startup rather than producing containers cleanup cannot select
(`PostgreSqlResourceLabels.cs:35-57`).

That is what makes cleanup safe: `../../../../scripts/cleanup-test-runtime` filters on the three
fixed labels, the presence of `host-pid`, and the **exact** run ID
(`cleanup-test-runtime:69-75`), refuses a blank or malformed run ID (`:51-57`), never prunes, and
leaves the Testcontainers reaper alone (`:101-106`). The runner installs it on `EXIT`
(`test-backend:465-472`). Resource Reaper stays enabled and `.WithReuse(true)` is forbidden,
because a `SIGKILL` executes neither a managed handler nor a shell trap.

## The cross-process lock

`CrossProcessPostgreSqlLock` is one exclusive file handle at:

```text
${TMPDIR:-/tmp}/quran-dashboard-tests/<16-hex>-postgres.lock
```

The hash is the first 16 lowercase hex characters of SHA-256 over
`realpath(QuranDashboard.Tests.csproj) + "\n" + getuid()` (`CrossProcessPostgreSqlLock.cs:131-167`),
so the lock is scoped to this project and this user and never collides with another checkout.

- Exclusivity is `FileShare.None` on an `OpenOrCreate` handle (`:88-107`); the file is never
  deleted or recreated, which is what stops two processes from locking two different inodes, and
  the OS releases the handle if a process dies.
- The holder writes `pid=… owner=… startedUtc=…` to a sidecar `.holder` file (`:108-114`), which
  a waiter prints every 5 seconds (`:10, 62-67`) and quotes in its timeout message.
- A waiter gives up after 15 minutes with a `TimeoutException` naming the recorded holder
  (`:9, 54-60`).
- It is acquired **before** any container is created and released **after** the container is
  gone (`PostgreSqlTestServer.cs:67-69, 116-132`; `ExclusivePostgreSqlLease.cs:46-48, 85-101`).
- Container-free unit tests never touch it: the server is a `Lazy` that only the first lease
  forces (`PostgreSqlTestProcess.cs:7-9, 18`).

`PostgreSqlTestProcessContractTests` covers both halves with a real second process — a
`flock`-held lock makes the acquire wait and then proceed (`:336-367`), and a held lock times out
naming the first holder (`:370-392`).

**Two Backend test processes must therefore not run concurrently** — an IDE run alongside a
terminal run does not fail fast, it waits up to fifteen minutes.

## Disposal order

This directory owns steps 3–8 of the fixture teardown sequence. Steps 1–2 belong to the fixture
and to `../DependencyInjection/OwnedServiceProviderRegistry.cs`, which disposes fixture-owned
roots and scopes in **reverse creation order** and records rather than throws on an individual
failure (`OwnedServiceProviderRegistry.cs:36-69`).

1. dispose `HttpClient` and `WebApplicationFactory` (fixture);
2. dispose child scopes, then every registry-owned `ServiceProvider`/`NpgsqlDataSource`
   (fixture + registry);
3. clear the pool **for that lease's connection string only** (`PostgreSqlTestServer.cs:185`) —
   `NpgsqlConnection.ClearAllPools()` would drop the live connections of collections that are
   still running, and is called nowhere in this project;
4. drop the leased database, un-templating it first and then `DROP DATABASE IF EXISTS … WITH
   (FORCE)` (`PostgreSqlTestServer.cs:247-258`);
5. release the parallelism slot, in a `finally`, so a failed drop cannot strand it
   (`PostgreSqlTestServer.cs:181-194`);
6. at process exit, drop every still-registered database, dispose the container, release the OS
   lock — in that order (`PostgreSqlTestServer.cs:116-132`), bounded to 30 s and never throwing
   from the handler (`PostgreSqlTestProcess.cs:5, 13-16, 126-147`);
7. the runner's `EXIT` trap then removes anything left, by run ID (`test-backend:465-472`);
8. Ryuk remains the crash-only fallback.

Lease disposal is idempotent (`PostgreSqlDatabaseLease.cs:53-61`,
`PostgreSqlSchemaLease.cs:44-56`, `ExclusivePostgreSqlLease.cs:85-101`). All create/drop DDL
runs outside a transaction through one `SemaphoreSlim`, on an unpooled maintenance connection to
the `postgres` database (`PostgreSqlTestServer.cs:260-297`).

`PostgreSqlSchemaLease` is the sub-database unit the migration fixture uses: `CREATE SCHEMA` plus
a `SearchPath` connection string, dropped `CASCADE` on disposal after clearing only its own pool
(`PostgreSqlSchemaLease.cs:20-56`).

## External databases are refused by default

Five feature overrides can point a fixture at a database this process does not own
(`ExternalReadOnlyDatabaseOptIn.cs:5-21`):

```text
MUSHAF_READER_REAL_DB_CONNECTION
UNIQUE_WORDS_REAL_DB_CONNECTION
ROOTS_EXPLORER_REAL_DB_CONNECTION
MORPHOLOGY_EXPLORERS_REAL_DB_CONNECTION
WORD_TYPES_REAL_DB_CONNECTION
```

**Normal gates unset all five, and the opt-in with them** (`test-backend:457-462`), so a lane
always runs on Testcontainers. Outside the gates, one of them is honoured only when

```text
QURAN_DASHBOARD_TEST_EXTERNAL_DB_MODE=READ_ONLY_ACKNOWLEDGED
```

accompanies **exactly one** connection variable; a bare override throws, and two set together
throw naming the second (`ExternalReadOnlyDatabaseOptIn.cs:32-71`). An acknowledged variable that
is not the one being resolved leaves that fixture on its owned migrated lease.

An external lease is inert by construction. `UseExternalReadOnlyDatabase` requires an explicit
database name and a loopback/local host, refusing remote, shared, staging and production targets
(`PostgreSqlTestProcess.cs:42-61`); it never starts the shared server, and it is built with
`release: null`, which is exactly what makes `IsExternal` true and disposal a no-op
(`PostgreSqlDatabaseLease.cs:31, 43-61`) — it cannot drop, truncate, or clear a pool it does not
own. `PostgreSqlTestProcessContractTests.ExternalReadOnlyLease_LeavesItsDatabaseIntact_WhenDisposed`
(`:177`) wraps an owned database as external, disposes the wrapper, and proves the data survives.

**There is no mutating external-database path, and none may be added ad hoc.** Introducing one
means centralizing it behind an explicit guard that proves local, dedicated ownership — a
database name or connection string is never ownership evidence — and requires a design decision,
not a fixture edit.
