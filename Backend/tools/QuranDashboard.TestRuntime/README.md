# QuranDashboard.TestRuntime

`QuranDashboard.TestRuntime` is the Backend-owned control-plane seam for the persistent Test Database
Capability. Ticket #150 introduces only contract validation and read-only inspection; existing test
runners continue to use their current lifecycle until the later atomic cutover.

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
