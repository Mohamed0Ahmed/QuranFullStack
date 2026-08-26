# Dev CLI shortcuts

Short commands to build/run the backend API and Angular dev server from any directory.

## Commands

| Command | What it does |
|---------|----------------|
| `qd-build` | `dotnet build QuranDashboard.sln` for backend changes |
| `qd-api` | `dotnet run --launch-profile https --no-build`; opens Swagger when the API is ready |
| `qd-ui` | `npm run start:https` for the Angular dashboard |
| `export-swagger` | Builds the API (Release) without build servers, defaults the Swagger host to `Development` for startup-option validation, and writes the OpenAPI spec to `Frontend/quran-dashboard-ui/openapi/swagger.json` via the Swashbuckle CLI (`Backend/dotnet-tools.json` manifest); no running server or database needed |
| `check-api-contract` | Runs `export-swagger`, regenerates the frontend API models (`npm run generate:api`), then fails with `git diff --exit-code` if either committed output is stale. It checks the spec and the generated client — the two things a caller breaks against — and deliberately not the browsable Redoc bundle, which is untracked and therefore invisible to `git diff` |
| `test-backend <lane>` | **The only supported way to run Backend tests.** Selects a lane from the test catalog, builds once or refuses a stale `--no-build`, shards when two PostgreSQL majors would collide, and cleans up its own containers. See *Backend test commands* below |
| `cleanup-test-runtime --run-id RUN_ID` | Removes the Docker resources of one `test-backend` run, selected by all five project test labels **and** that exact run ID. Never prunes |
| `check-pending-model --build\|--no-build` | Reports whether the EF Core model has pending changes. Never adds and never applies a migration |
| `create-smoke-dump` | Regenerates the canonical `quran_*` data dump the backend smoke data tier restores: `resources/db-dumps/quran-canonical/{quran-canonical.dump,manifest.json}` |
| `wipe-abwab` | Empties the literal Abwab and Abwab-owned Linking reset closure on a local database, leaving canonical `quran_*`, access, and linking-workspace data intact |
| `add-mig <Name>` | `dotnet ef migrations add <Name>` against `Infrastructure` with `Api` as startup project. EF tooling only — never hand-write a migration (`Backend/README.md` §Invariants) |
| `update-db` | `dotnet ef database update` — applies pending migrations to the configured database |
| `access-admin` | Runs normalized-identity scan/backfill, permission-catalogue sync, Owner reconciliation, legacy-role inventory/conversion, and authorization preflight |
| `clean-local-build` | Clears the NuGet caches, deletes every `bin`/`obj`, and restores the solution. Non-destructive to data |
| **`drop-db --yes`** | **DESTRUCTIVE.** `dotnet ef database drop --force` — drops the configured database outright, all data lost |
| **`reset-db --yes`** | **DESTRUCTIVE.** `drop-db --yes` followed by `update-db` — an empty database at migration head |

**The two destructive rows fail closed.** `drop-db` and `reset-db` both refuse to run unless
their single argument is exactly `--yes`; without it they print the warning and exit non-zero
having done nothing. There is no `--allow-remote`-style escape on either: they act on whatever
`dotnet ef` resolves as the configured connection, so check what that is before typing `--yes`.

**`drop-db`, `reset-db` and `update-db` are local-dev helpers and must never be pointed at the
Railway database.** There is a real production database on the other end of a
`ConnectionStrings__QuranDashboardDb` you may have exported for an importer run, and none of these
three ask a second time. Schema changes reach production by deploying, not by running `update-db`
against it.
`abwab_*` content is authored curation data that nothing restores — prefer `wipe-abwab` when the
goal is only to clear abwab rows, and note that a full reset also discards the canonical
`quran_*` data, which then has to be re-imported.

## Rebuilding the local database from nothing

The reset → migrate → seed runbook. It lived under `Backend/report/` until 2026-08-04, where it
went stale and cost one confusing `create-smoke-dump` failure; it is here now, next to the
commands it drives.

**Provide the connection string via the `ConnectionStrings__QuranDashboardDb` environment
variable when running any importer verb.** The DataImporter does not read the API's user secrets,
and its own `appsettings.json` default password is not the local one.

### 1. Reset and migrate

```bash
./scripts/reset-db --yes
```

`reset-db` is literally `drop-db --yes` followed by `update-db`, so it lands on an empty database
at migration head. Run `clean-local-build` first if stale sandbox assets are in the way — both
`drop-db` and `update-db` run a preflight check for them and will refuse otherwise.

**Migrations are applied in filename order and that order is not transcribed anywhere.** The set
is whatever is in `infrastructure/QuranDashboard.Infrastructure/Migrations/`, ordered by its
timestamp prefix; `dotnet ef database update` applies them in exactly that order. A written list
is what went stale before — it claimed 15 migrations long after the tree held more, and
`create-smoke-dump` refuses to run when the applied count and the file count disagree, so the
document sent an operator hunting for a database problem that did not exist. That guard is the
enforcement; read the directory for the list.

### 2. Seed, in dependency order

Verbs are dispatched from `tools/QuranDashboard.DataImporter/Program.cs` and documented in
`tools/QuranDashboard.DataImporter/README.md`, which owns each verb's flags and source package.
Only two dependencies actually constrain the order:

1. **`import-foundation` first.** Everything else resolves against `quran_ayahs` or
   `quran_words`, and nothing else creates them.
2. **`rebuild-words` → `import-morphology` → `generate-i3rab`.** Display words come from
   foundation words; morphology attaches to display words; simple i3rab is generated from
   morphology segments.
3. **`rebuild-words` → `build-phrase-index`.** PhraseSearch consumes the foundation words and
   both rebuilt exact word-identity links. It is source-free and does not depend on morphology,
   tafsirs, translations, navigation, Mutashabihat, Abwab, or i3rab.

Every other verb — `import-mutashabihat`, `import-tafsirs`, `import-translations`,
`import-navigation-metadata`, `import-full-i3rab` — resolves `verse_key → ayah_id` against
`quran_ayahs` alone and may run at any point after `import-foundation`, in any order relative to
each other. `validate-enriched-morphology` is a check, not a seeding step.

Verify the dependency claim rather than trusting this list: each importer's resolver is in
`infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/<pipeline>/`, and the
hard checks that enforce it are in the matching
`Persistence/DataPipelines/Quran/<pipeline>/` validator.

### 3. Refresh the smoke dump

After a reseed, regenerate the canonical dump the backend smoke data tier restores — see
`create-smoke-dump` below. Its migration-count and baseline-row guards are what tell you the
reseed actually landed.

**What is not verified here.** No end-to-end reset → full reseed of the whole chain has ever been
captured in one run. The reset → migrate → `import-foundation` → `rebuild-words` head of the chain
has been; the individual imports have each been run against an already-foundation-seeded database.
Treat the ordering above as derived from the code, not as a replayed transcript.

`build-phrase-index [--report-out <path>] [--force]` is dispatched through the DataImporter.
The default report root is `resources/report/quran-phrase-search/`; each build gets its own
build-ID directory. A first build initializes the migration-seeded null PhraseSearch source
fingerprint only after verifying every readable word has consistent simple and tashkil identity
links. A later build requires `--force` while an active generation exists. The command never
deletes the active or compatible previous generation before activation, and `Ctrl+C` cancels
staging while preserving the prior active pointer.

Before a build row is created, the preflight measures the current PostgreSQL database and phrase
relations. It reserves one full current database size for a new generation including indexes, one
more for WAL, and the configured safety margin. Local loopback runs read free bytes from the
PostgreSQL data filesystem; remote runs fail closed unless the operator provides verified free
bytes with the `operator-verified-database-filesystem-v1` contract. Failed build audit rows are
retained for `PhraseSearch:FailedBuildRetentionDays` (30 by default) after their durable report;
child rows are removed only when the build is Failed and neither active nor previous.
Eligible superseded-generation cleanup runs after activation commits, never during preflight,
source approval, staging, or activation. If that housekeeping fails, the activated generation
remains active and the report plus command message records `post-activation-cleanup-failed`.

`rollback-phrase-index` is the matching operational rollback verb. It takes no arguments and
refuses unless both pointers exist, the current source fingerprint still matches, and the previous
generation uses the current format with exact and similarity readiness. The pointer and status swap
is one source-fenced transaction; migration down is not the runtime rollback path.

### `create-smoke-dump`

```bash
./scripts/create-smoke-dump --yes [--allow-remote]
```

| Flag | Effect |
|------|--------|
| `--yes` | Required. Without it the script prints the dump and manifest it would replace, plus the source database, and exits non-zero |
| `--allow-remote` | Required to target any host other than `localhost` / `127.0.0.1`. Without it a non-local host is refused outright, so a deployed database cannot be dumped by accident |

Guards, each of which exits non-zero and dumps nothing:

- the applied `__EFMigrationsHistory` count must equal the number of migration files in
  `infrastructure/QuranDashboard.Infrastructure/Migrations/` — a database behind or ahead of
  the tree produces data that does not fit the schema the tier migrates to;
- the five baseline tables must match the canonical counts the script pins
  (`quran_roots`, `quran_lemmas`, `quran_stems`, `quran_word_morphology`,
  `quran_word_morphology_segments`). Restore the canonical data rather than relaxing a
  baseline.

The dump is written to a temp file in the destination directory and renamed into place, so an
interrupted run never leaves a truncated archive where the tests expect a complete one. The
manifest records the migration head, the dump's sha256, the `pg_dump` version, and the row count
of every `quran_*` table; the smoke data tier verifies the first two before it starts a container.

Connection string resolution, in order: `ConnectionStrings__QuranDashboardDb`, then the
`ConnectionStrings:QuranDashboardDb` user secret of `api/QuranDashboard.Api`.

**Prerequisite: `pg_dump` 18 or newer**, matching the local PostgreSQL server. The smoke data
fixture restores into `postgres:18-alpine` for the same reason — a pg16 `pg_restore` rejects an
archive written by a newer `pg_dump`.

`resources/` is gitignored: the artifact is a local operator product, regenerated by this command
rather than committed.

### `wipe-abwab`

```bash
./scripts/wipe-abwab --yes
```

`TRUNCATE ... RESTART IDENTITY CASCADE` over a literal allowlist that owns the complete Abwab and
Abwab-to-Linking foreign-key closure: the eight `abwab_*` tables, door-scoped linking operations,
jobs, prepared-state tables, contributions, units, and door ayah/word projections. Linking
workspaces, `quran_*`, users, roles, and permissions are outside the closure. A schema change that
cannot survive existing Abwab rows therefore has a sanctioned local reset. Abwab content is
authored curation data: nothing restores it, and the canonical dump covers `quran_*` only.

That hazard is not hypothetical: `20260802062011_RequireAbwabDoorSection` makes
`abwab_doors.section_id` `NOT NULL` with no backfill and no guard
(`../infrastructure/QuranDashboard.Infrastructure/Migrations/20260802062011_RequireAbwabDoorSection.cs:13-20`),
and nothing in the running app auto-migrates — `MigrateAsync` exists only in test fixtures. The
deployed database already has it applied (recorded 2026-08, an operational fact no code proves),
so the exposure is forward-looking only: replaying the migration chain against a database that
holds a NULL `section_id` row — a pre-2026-08-02 backup restore, or a new environment seeded from
old data — fails loud at that migration and rolls back. Postgres `SET NOT NULL` refuses the whole
statement; nothing is silently coerced. This script is the sanctioned local remedy; a deployed
restore needs the NULL rows resolved by hand first.

| Flag | Effect |
|------|--------|
| `--yes` | Required. Without it the script prints the six tables and the target database, and exits non-zero having wiped nothing |

Guards, each of which exits non-zero:

- **local only.** Any host other than `localhost` / `127.0.0.1` is refused. Deliberately
  stricter than `create-smoke-dump`: there is no `--allow-remote` escape, because a deployed
  database must not be wipeable by any flag this script accepts;
- **a literal closure allowlist** — no wildcard, no catalog query. It includes every table whose
  rows are owned by an Abwab door and excludes canonical Quran, access, and linking-workspace
  tables. Any new Abwab or door-scoped Linking foreign key requires a deliberate list update;
- **a post-wipe tripwire**: `quran_surahs` must still hold 114 rows. It does not prevent damage —
  it refuses to let damage pass silently.

Connection string resolution matches `create-smoke-dump`: `ConnectionStrings__QuranDashboardDb`,
then the `ConnectionStrings:QuranDashboardDb` user secret of `api/QuranDashboard.Api`.

Typical daily flow:

```bash
qd-build
qd-api
qd-ui
```

### `access-admin`

`access-admin` runs the already-built `tools/QuranDashboard.AccessAdmin/` and anchors its copied
tool configuration, so it can be invoked from any directory. Run `qd-build` after backend code
changes before using it. The wrapper defaults `DOTNET_ENVIRONMENT` to `Development` without
overriding a value the caller already exported, so the tool's Development user secrets load on the
documented local path. The connection source is the tool's `appsettings.json`, then those user
secrets, then `ConnectionStrings__QuranDashboardDb` as the final override.

From `Backend/`, the staged Phase 2 deployment order is:

```bash
./scripts/access-admin identity scan
dotnet ef database update --migration AddAuthorizationAccessFoundation --project infrastructure/QuranDashboard.Infrastructure --startup-project api/QuranDashboard.Api --context QuranDashboardDbContext
./scripts/access-admin identity scan
./scripts/access-admin identity backfill --apply
./scripts/access-admin identity scan
dotnet ef database update --migration RequireNormalizedEmail --project infrastructure/QuranDashboard.Infrastructure --startup-project api/QuranDashboard.Api --context QuranDashboardDbContext
./scripts/access-admin catalogue sync
./scripts/access-admin authorization preflight
```

The first scan runs before any Phase 2 DDL and reads only legacy `users.id` and `users.email`; a
collision exits non-zero without mutation. The additive migration intentionally leaves
`normalized_email` nullable and without its unique index while creating the access tables and audit
document constraints. The final identity migration succeeds only after the explicit
normalizer-backed backfill.
`authorization preflight` additionally inspects the live Phase 2 schema — column types, nullability
and identity generation, plus index and constraint definitions compared verbatim — before checking
migration history, normalized identities, and the catalogue. Catalogue parity is over active codes:
a canonical permission carrying `retired_at` fails as `catalogue_retired=`. The explicit
Interactive OIDC sign-in alone can add a configured Owner after verified email evidence; the
`owners reconcile --apply` command can only remove safely resolved Owners or revoke conflicting
direct grants. It requires a reason and `--confirm-production` in Production. The tool does not run
migrations itself. A clean `authorization preflight` result is readiness evidence only: it neither
deploys an artifact nor activates authorization.

### Authorization activation and rollback (prospective)

**This is a future runbook, not a completed rollout.** Authorization enforcement has not been
activated in production, and this repository contains no production or production-like activation
or rollout evidence.

Before an approved production activation, record the chosen gate: the preferred path is a reviewed
Phase 5 enforcement and Phase 6 administration release together. An earlier Phase 5-only path
requires explicit acceptance of a temporary Owner-only write period or a verified trusted operator
command that uses the same active-Owner authorization, validation, transaction, grant-delta, and
append-only audit services as Phase 6.

- Backend enforcement must be live before, or atomically with, frontend permission-aware controls.
- During a rolling or mixed-version deployment, deny unsafe methods at the edge or place
  administrative writes in maintenance/deny mode until no open Backend instance can serve them.
  Keep public GETs online.
- A frontend rollback may be independent, but never roll the Backend back to an open-write build.
  Roll Backend code back only to a schema-compatible build that protects every unsafe route. If no
  protected rollback artifact is available, keep administrative unsafe methods denied at the
  platform/edge while public GETs remain available, then repair forward.

Before real authorization users, grants, or audit history exist, an explicitly disposable
development or pre-release database may be dropped and recreated from the current migration head.
That reset is not a production rollback strategy. Once real authorization data exists, never run a
destructive authorization `Down` migration or drop its tables to roll back. Keep unsafe routes
protected and use a schema-compatible code rollback, a data-preserving restore, or repair forward.

### Legacy Admin/Editor cleanup

The Phase 10 operator sequence is deliberately separate from `update-db`: first run
`./scripts/access-admin legacy-roles inventory`, resolve every reported violation and verify the
configured/verified Owner state, then run `./scripts/access-admin legacy-roles convert --apply` under the
short access-admin write freeze. The converter locks and rereads the role-bearing users, refuses a former
Admin/Editor user with any direct grant, clears only the legacy `RoleId`, writes no inferred grant, and
emits audit events. Run the inventory again, retain the before/after non-secret output, and confirm that
`authorization preflight` reports no legacy-role reference before the generated cleanup migration is
deployed. It stays non-zero before that deployment because the migration is pending.

The cleanup migration deletes only the Admin and Editor seeds. `users.role_id` remains nullable and its
restrictive foreign key is the safety gate: PostgreSQL rejects the migration if conversion left any user
referencing either role; it does not cascade.

For a clean database created from the current migration head, there is no populated legacy
authorization state to convert and no conversion rehearsal is required. If a release instead upgrades
a populated database that carries legacy Admin/Editor identities, rehearse and retain the
inventory/convert/reinventory sequence against a production-like copy before applying the cleanup
migration. Do not use migrations to fabricate rehearsal evidence.

Exit codes are stable: `0` clean, `2` usage, `3` a reported preflight/catalogue failure, and `4` a
configuration or database failure the tool reports as `access_admin_failure=<type>` without a stack
trace.

After the first successful build, use `qd-api` directly until backend code changes.

## Backend test commands

`../../TESTING_CONSTITUTION.md` and the active plan's `Testing Decision` select which checks to run.
This section and `../tests/QuranDashboard.Tests/README.md` own the Backend command mechanics and
lane membership.

### `test-backend`

```bash
./scripts/test-backend --help
```

`--help` prints the authoritative usage and is the thing to trust when this file and the script
disagree. The lanes are `fast`, `access`, `access-db`, `migration`, `process`, `smoke`,
`tier-b`, `gate-contract`, `canonical-data`, `feature`, `pipeline`, and `pre-pr`; an unknown lane exits 2 with the
usage text (`test-backend:72-78`).

| Purpose | Lanes | Selection |
|---|---|---|
| Daily verification | `smoke`, `tier-b` | Permanent classes only; the union is the complete Permanent set |
| Concern gate | `gate-contract` | Every retained row with a non-empty `Concerns` value |
| Pipeline gate | `pipeline` | `Gate=Pipeline` **and** `Kind!=Canonical`; `canonical-data` owns every excluded canonical class |
| Canonical-data gate | `canonical-data` | Every `Kind=Canonical` row, including the nine canonical rows excluded from `pipeline` and the canonical smoke-data class |
| Migration gate | `migration` | `Kind=Migration` |
| Access database gate | `access-db` | `Feature=Access` **and** (`Kind=Database` **or** `Concerns` contains `Schema`); `gate-contract` owns the excluded non-Access `AbwabSchemaTests` and `WordTypesChildCatalogueDriftTests` |
| Mandatory pre-release gate | `pre-pr` | Every retained catalog row |

The focused `fast`, `access`, `process`, and `feature` lanes remain diagnostic entry points. They do
not replace the daily pair or a change/release gate. Before release, `pre-pr` is mandatory and runs
the complete retained estate.

| Flag | Effect |
|------|--------|
| `--build` / `--no-build` | **Required, exactly one**, on every lane except `pre-pr` (`test-backend:144-155`). `--build` builds `QuranDashboard.sln` once, single-threaded. `--no-build` verifies the outputs the selection actually needs — the test assembly always, `QuranDashboard.AccessAdmin` for `Kind=Migration`/`Kind=Process`, `QuranDashboard.DataImporter` for `Gate=Pipeline`/`Kind=Canonical` — and names the missing path instead of running stale (`:299-326`) |
| `--list-tests` | Discovery only. Starts no container, never shards, and additionally fails if a selected catalog class discovers no tests or discovery finds a class outside the selection (`:602-631`) |
| `--results-dir PATH` | Passed through as `--results-directory`; a relative path resolves against your working directory, not the repo root (`:361-366`). **It does not produce a TRX file.** The script registers no VSTest logger — `:536-555` is the whole argument list — so the directory receives the blame-hang output and nothing machine-readable. When you need parseable results, prefix the invocation with `VSTestLogger=trx`, which MSBuild reads as a property; the script itself needs no change |
| `--feature KEY` | `pipeline` only — narrows the lane to one validated Pipeline feature; a non-Pipeline feature exits 2 |
| `--class NAME` / `--test NAME` | `feature` only, as the two exact alternatives to a `FEATURE_KEY`. `--test` is additionally validated against VSTest discovery, so a typo'd method name fails before anything runs (`:328-359`) |

`pre-pr` is the exception: executing it always builds, so it rejects `--no-build`; discovering it
requires the exact form `pre-pr --list-tests --no-build` (`:144-152`).

Selection comes from `../tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv`, whose
header the script verifies before reading a row (`:168-178`). A `FEATURE_KEY` that no row carries
exits 2 rather than running nothing (`:268-279`). The script prints its lane, the catalog path,
every selected row, the expanded VSTest filter, and the resource kinds the selection needs —
**that block is the evidence**; do not pipe a run into `tail`.

Three behaviors are worth knowing before you read an unfamiliar failure:

- **It can run twice.** A selection containing `QuranDashboard.Tests.Smoke.Data.SmokeDataReadTests`
  alongside any other class runs as two sequential `dotnet test` invocations — the shared
  `postgres:16-alpine` classes, then that one class on its exclusive `postgres:18-alpine` server —
  with a bounded wait in between until no labelled PostgreSQL container is running (`:388-408,
  562-586`). That is `pre-pr` and `canonical-data`; `smoke` excludes the data tier and stays one
  invocation. Shard exit statuses are combined (`:693-698`).
- **A missing canonical resource fails the lane.** When the selection needs the foundation
  sources, the enriched morphology artifact, or the dump plus manifest, the script checks for them
  before starting anything and exits 1 with `canonical data tier: failed preflight` (`:476-519`).
  It prints one of `ran` / `failed` / `not selected` / `discovery only` either way — quote that
  line as the canonical skip accounting. It is scoped to the shards that actually carry a
  `Kind=Canonical` class, which is not only the exclusive one: every `Quran.Import` class is
  canonical and runs on the shared runtime. `ran` means every canonical-bearing shard started
  **and** exited zero; `failed` means one of them came back non-zero or never started because an
  earlier shard stopped the lane (`:701-731`). A failure confined to a shard that carries no
  canonical class no longer reports the tier as failed — that was the misattribution. Which
  shard, and its own exit code, is on the `canonical shard status:` line printed beside it.
- **It owns its cleanup.** Each run generates a 32-hex run ID, exports it as
  `QURAN_DASHBOARD_TEST_RUN_ID`, unsets the five external-database overrides and their opt-in, and
  installs an `EXIT` trap that calls `cleanup-test-runtime` for that ID (`:454-474`).

Backend test processes **must not run concurrently** — the shared database runtime is guarded by
a cross-process OS lock, so a second run waits rather than failing fast. See
`../tests/QuranDashboard.Tests/TestSupport/PostgreSql/README.md`.

### `cleanup-test-runtime`

```bash
./scripts/cleanup-test-runtime --run-id RUN_ID [--dry-run]
```

`test-backend` calls this itself; run it by hand only to clear a run that was killed. It selects
Docker resources by the three fixed project labels, the presence of `host-pid`, and the **exact**
run ID (`cleanup-test-runtime:69-83`) — so it can only ever reach containers this project
labelled. It refuses a missing or non-32-hex run ID (`:51-57`), never prunes, never touches
unlabelled resources or development volumes, and reports the Testcontainers reaper separately
while leaving it running (`:101-106`). A container the test host already removed itself counts as
cleaned, not as a failure (`:130-148`). `--dry-run` prints the candidates and removes nothing.

### `check-pending-model`

```bash
./scripts/check-pending-model --build|--no-build
```

Wraps `dotnet ef migrations has-pending-model-changes` for `QuranDashboardDbContext` with the
right project/startup pair and `DOTNET_ENVIRONMENT=Development` (`check-pending-model:53-59`). It
**never adds and never applies a migration**; it is a separate command from every test lane, run
alongside the `migration` lane whenever the EF model or schema is in scope. `--no-build` requires
existing Infrastructure and Api output and names the missing path otherwise (`:41-46`).

## One-time setup (zsh)

Add the scripts folder to your `PATH` in `~/.zshrc`:

```bash
export PATH="/projects/Dashboard/App/Backend/scripts:$PATH"
```

Then reload:

```bash
source ~/.zshrc
```

**Alternative:** aliases instead of `PATH`:

```bash
alias qd-api='/projects/Dashboard/App/Backend/scripts/qd-api'
alias qd-build='/projects/Dashboard/App/Backend/scripts/qd-build'
alias qd-ui='/projects/Dashboard/App/Backend/scripts/qd-ui'
```

## Prerequisites

### Backend (`qd-build`, `qd-api`)

- .NET 10 SDK
- PostgreSQL with the seeded `quran_dashboard` database
- Trusted HTTPS dev certificate:

```bash
dotnet dev-certs https --trust
```

- Database connection in user secrets (do not commit secrets):

```bash
cd Backend/api/QuranDashboard.Api
dotnet user-secrets set "ConnectionStrings:QuranDashboardDb" "Host=localhost;Port=5432;Database=quran_dashboard;Username=postgres;Password=<your-password>"
```

### Frontend (`qd-ui`)

- Node.js and npm
- Dependencies installed:

```bash
cd Frontend/quran-dashboard-ui
npm install
```

- Local HTTPS certificates in the frontend project root:

```bash
cd Frontend/quran-dashboard-ui
mkcert -install
mkcert localhost
```

This produces `localhost.pem` and `localhost-key.pem`, used by `npm run start:https`.

## URLs

| Service | URL |
|---------|-----|
| API | `https://localhost:5015` |
| Swagger | `https://localhost:5015/swagger` |
| Health | `https://localhost:5015/api/health` |
| Angular UI | `https://localhost:4200` |

## Troubleshooting

| Problem | Fix |
|---------|-----|
| `command not found: qd-api` | Add `Backend/scripts` to `PATH` or use the full path |
| `qd-api` says the app was not built | Run `qd-build` first |
| Build fails | Run `qd-build` and fix compile errors |
| API won't start | Check PostgreSQL is running and user secrets are set |
| `node_modules not found` | Run `npm install` in `Frontend/quran-dashboard-ui` |
| SSL cert missing for UI | Run `mkcert localhost` in the frontend project |
| Browser shows certificate warning | Trust .NET dev cert and/or mkcert root (`mkcert -install`) |
