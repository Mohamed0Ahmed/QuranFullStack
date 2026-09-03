# QuranDashboard.TestRuntime

`QuranDashboard.TestRuntime` is the Backend-owned control-plane seam for the persistent Test Database
Capability. Existing test runners continue to use their current lifecycle until the later atomic cutover.

## Validate the contract

From `Backend/`:

```bash
dotnet run --project tools/QuranDashboard.TestRuntime -- contract validate
```

The command validates `testing/test-database-contract.json` against the compiled EF Core model and emits
a structured JSON report. A missing, malformed, duplicate, unclassified, unexpectedly classified, or
misclassified table returns exit code `3`.

## Inspect a candidate capability

Set `ConnectionStrings__QuranDashboardTest` through the environment or another non-committed local secret
source, then run:

```bash
dotnet run --project tools/QuranDashboard.TestRuntime -- inspect
```

Inspection accepts only the exact `quran_dashboard_test` database through loopback or a local Unix socket.
It validates the contract and target before opening a connection, then executes catalogue queries inside
a read-only transaction. The JSON report includes database identity, PostgreSQL version and recovery
state, migration head, System Catalogue health, marker status, and effective privileges. It never emits
the connection string or raw marker values.

Exit codes are `0` for a healthy result, `2` for invalid command usage, `3` for a refused or unhealthy
contract/capability, and `4` when an accepted local target cannot be inspected.

## Administer capability roles

The role and safety-metadata workflow always names the existing local login that receives the four
capability-role memberships:

```bash
dotnet run --project tools/QuranDashboard.TestRuntime -- admin inspect --login <local-login>
dotnet run --project tools/QuranDashboard.TestRuntime -- admin dry-run --login <local-login>
dotnet run --project tools/QuranDashboard.TestRuntime -- admin apply --login <local-login> --run-id <run-id>
dotnet run --project tools/QuranDashboard.TestRuntime -- admin verify --login <local-login>
```

`inspect`, `dry-run`, and `verify` do not retain database changes. `apply` is the only mutating mode and
requires the selected login to be the connected session login with explicit role/database administration
authority. It accepts only local PostgreSQL 18, the exact `quran_dashboard_test` database, and a server
that is not in recovery. Repeating `apply` against compliant state is a no-op.

The workflow creates four stable `NOLOGIN` roles, removes unexpected direct or inherited membership,
grants all four roles only to the selected login, and installs the capability/reset, contract-version,
metadata-version, and migration-head settings on `quran_dashboard_test`. It emits identities and boolean
results, never the connection string, password, or raw credential material.

The reader can select every contracted table. The application role additionally receives insert, update,
and delete privileges only for Mutable Application State plus its owned sequences. The resetter can read
Mutable Application State, truncate only the 35 reset tables, and update only `linking_data_state`. The
scratch administrator receives `CREATEDB` but no ownership or mutation grants on the Development Database
or persistent Test Database. `verify` performs safe denial probes under each restricted role and rolls back
its transaction. When `quran_dashboard` exists, administration opens a read-only transaction that inspects
only its PostgreSQL catalog privileges; it never reads application tables or repairs unsafe grants there.

## Hold the global database lock

The committed contract owns the one cluster-wide lock key. A runner starts a dedicated keeper process for
its complete database-aware invocation:

```bash
dotnet run --project tools/QuranDashboard.TestRuntime -- \
  lock hold --mode shared --run-id <run-id> --command <command>

dotnet run --project tools/QuranDashboard.TestRuntime -- \
  lock hold --mode exclusive --run-id <run-id> --command <command>
```

The keeper uses a non-pooled connection and emits one compact JSON line after acquisition. The line reports
the fixed key, mode, run ID, command, keeper PostgreSQL process ID, configured timeout, and lock wait. Keep
the process alive for the whole guarded invocation. `Ctrl+C`, normal disposal, or process termination closes
the connection, and PostgreSQL session semantics release the lock. A graceful cancellation emits a second
line with `status=released`.

Acquisition defaults to 15 minutes; `--timeout-seconds <seconds>` overrides it. A timeout report contains
only credential-free holder process, run, command, mode, activity state, and wait-event diagnostics. Run IDs
are 1-32 ASCII letters, digits, dots, underscores, or hyphens; command IDs use the same vocabulary and are
1-24 characters. Both appear in the keeper connection's `application_name`.

Shared keepers coexist. An exclusive keeper excludes both shared and exclusive contenders. Every mutation
must verify that the expected run ID still owns the exclusive keeper before writing. `admin apply` performs
that acquisition and verification itself; bypassing its command interface is refused with supported-runner
guidance. Future reset and mutation runners must use the same ownership verifier rather than duplicating the
contract key or lock SQL.

System Catalogue reconciliation has a strict nested order: acquire and verify the global exclusive keeper
first, then begin the reconciliation transaction and acquire its narrower transaction-level catalogue lock.
Never acquire the catalogue lock while waiting for the global lock.
