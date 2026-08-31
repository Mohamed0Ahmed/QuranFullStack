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
| `check-pending-model --build\|--no-build` | Reports whether the EF Core model has pending changes. Never adds and never applies a migration |
| `create-smoke-dump` | Regenerates the canonical `quran_*` data dump the backend smoke data tier restores: `resources/db-dumps/quran-canonical/{quran-canonical.dump,manifest.json}` |
| `test-artifacts status\|verify [--lane LANE\|--artifact ID]` | Read-only inspection of the tracked test-artifact lock; `verify` adds hashes and strict external-manifest checks |
| `test-artifacts provision-full-canonical\|verify-full-canonical ...` | Provider-neutral scheduled/release full-canonical provision-once and sealed shared-state verification; see the artifact tool README |
| `test-artifacts previous-release-upgrade` | Read-only fail-closed gate for previous-release migration rehearsal adoption evidence; it never opens or mutates a database |
| `wipe-abwab` | Empties the literal Abwab and Abwab-owned Linking reset closure on a local database, leaving canonical `quran_*`, access, and linking-workspace data intact |
| `add-mig <Name>` | `dotnet ef migrations add <Name>` against `Infrastructure` with `Api` as startup project |
| `update-db` | `dotnet ef database update` — applies pending migrations to the configured database |
| `access-admin` | Runs normalized-identity scan/backfill, permission-catalogue sync, Owner reconciliation, legacy-role inventory/conversion, and authorization preflight |
| `export-abwab-snapshot` | DataImporter verb that captures the current Abwab relational snapshot across the eight-table schema without Linking or Linking-dependent inclusion-sync rows; see the export-only workflow below |
| `import-abwab-snapshot` | DataImporter verb that restores one verified v4 Abwab snapshot into empty current-schema targets; see the standalone import workflow below |
| `clean-local-build` | Clears the NuGet caches, deletes every `bin`/`obj`, and restores the solution. Non-destructive to data |
| **`drop-db --yes`** | **DESTRUCTIVE.** `dotnet ef database drop --force` — drops the configured database outright, all data lost |
| **`reset-db --yes`** | **DESTRUCTIVE.** `drop-db --yes` followed by `update-db` — an empty database at migration head |
| **`reset-and-import-local --yes`** | **DESTRUCTIVE, local only.** Preserves Abwab v4, resets/migrates, and runs the complete curated import chain with durable stage evidence |

**The three destructive rows fail closed.** Each requires the exact single argument `--yes`.
`reset-and-import-local` additionally requires and verifies one explicit loopback connection before
calling the lower-level reset; it has no remote override.

**`drop-db`, `reset-db` and `update-db` are local-dev helpers and must never be pointed at the
Railway database.** There is a real production database on the other end of a
`ConnectionStrings__QuranDashboardDb` you may have exported for an importer run, and none of these
three ask a second time. Schema changes reach production by deploying, not by running `update-db`
against it.
`abwab_*` content is authored curation data. Preserve it with the v4 export/import workflow before
a full reset; prefer `wipe-abwab` when the goal is only to clear Abwab rows. A full reset also
discards canonical `quran_*` data, which then has to be re-imported.

## Verifying locked test artifacts

Backend projects opt into NuGet lock files through `Directory.Build.props`. Controlled test
provisioning uses `dotnet restore QuranDashboard.sln --locked-mode`; dependency changes therefore
require reviewed `packages.lock.json` updates before sealed execution.

The repository-root `test-artifacts.lock.json` and the schemas under `docs/testing/` are the tracked
trust contract. These commands are read-only: they do not fetch, extract, restore, refresh, publish,
or connect to PostgreSQL.

```bash
./scripts/test-artifacts status
./scripts/test-artifacts status --lane critical
./scripts/test-artifacts verify --artifact compact-cross-stack-base
```

`status` checks strict lock shape, required selection, staged presence and size, and repository
migration freshness. `verify` additionally checks every SHA-256, strictly parses the hashed external
manifest, validates PostgreSQL table identifiers, and compares manifest identity, migration,
producer, table scope, provenance, sentinels, and PhraseSearch expectations with the lock. An explicit
lane or artifact that has no lock entry fails closed. The initial lock intentionally has no entries;
the compact-fixture implementation adds its reviewed artifact rather than predeclaring invented
hashes or Quran sentinels.

## Exporting the current Railway Abwab snapshot

This workflow only reads and exports the current Railway Abwab data. It does not reset,
migrate, import, restore, or otherwise write to Railway.

Run it from `Backend/` immediately before every planned Railway database reset. The local,
gitignored API production settings already contain the Railway connection. Resolve it into a
temporary shell variable without printing it, pass it through the normal configuration environment
variable, then clear it; never paste it into a command argument, tracked file, snapshot, or report:

```bash
./scripts/qd-build

(
  set -euo pipefail
  railway_abwab_connection="$(jq -er \
    '.ConnectionStrings.QuranDashboardDb | select(type == "string" and length > 0)' \
    api/QuranDashboard.Api/appsettings.Production.json)"

  ConnectionStrings__QuranDashboardDb="$railway_abwab_connection" \
    DOTNET_ENVIRONMENT=Production \
    dotnet run --project tools/QuranDashboard.DataImporter/QuranDashboard.DataImporter.csproj --no-build -- \
    export-abwab-snapshot --output-dir ../resources/exports/abwab
)
```

The command prints only a masked host/database target, then opens a PostgreSQL
`REPEATABLE READ READ ONLY` transaction. Its literal scope is exactly:

- `abwab_sections`
- `abwab_doors`
- `abwab_door_aliases`
- `abwab_door_relations`
- `abwab_templates`
- `abwab_template_nodes`
- `abwab_door_inclusions`
- `abwab_door_inclusion_unit_syncs`

All persisted columns are exported except PostgreSQL's `xmin`. Every Linking row and Linking
summary is excluded. `abwab_door_inclusion_unit_syncs` stays in the literal table/schema allowlist,
but its rows are derived from deliberately excluded `linking_units`, so they are never serialized.
The snapshot and both audit reports record the source row count excluded from that table. The
command refuses to produce a snapshot if the live `abwab_*` table set, schema, serialized counts,
IDs, foreign references, door/template hierarchy, active inclusion graph, or relation types fail
validation. A validation failure writes only timestamped JSON and Markdown audit reports with
`persisted=false`; it does not write a snapshot or checksum and is not a valid backup.

A successful run creates four new timestamped files and refuses to overwrite any existing file:

- `abwab-snapshot-<UTC timestamp>.json`
- `abwab-snapshot-<UTC timestamp>.json.sha256`
- `abwab-snapshot-<UTC timestamp>-report.json`
- `abwab-snapshot-<UTC timestamp>-report.md`

Before resetting anything, require `verdict=pass` and `formatVersion=4`, confirm the eight
serialized counts and the source excluded inclusion-sync count in both audit reports, and verify
the snapshot checksum from the output directory. Any earlier v3 artifact is legacy and requires a
fresh v4 export; do not treat it as the current reset package.

```bash
sha256sum --check abwab-snapshot-<UTC timestamp>.json.sha256
```

Copy the four-file set to recoverable storage before the reset. A snapshot is not accepted as a
backup when the command exits non-zero, any artifact is missing, the checksum fails, or either
audit report does not say `pass`. This section intentionally stops at acquisition and
verification; restore/import is a separate workflow.

## Importing a verified v4 Abwab snapshot

`import-abwab-snapshot` is the standalone restore command, and the automated local workflow below
invokes it after every reset. The source must be a fresh v4 snapshot with its adjacent `.sha256`
sidecar. The ignored v3 Railway artifact is legacy and is deliberately refused.

```bash
ConnectionStrings__QuranDashboardDb='<local connection>' DOTNET_ENVIRONMENT=Production \
  dotnet run --project tools/QuranDashboard.DataImporter/QuranDashboard.DataImporter.csproj --no-build -- \
  import-abwab-snapshot --source ../resources/exports/abwab/abwab-snapshot-<UTC timestamp>.json
```

A non-loopback target fails closed unless the same command includes both `--allow-remote --yes`.
The target must be at the compiled current migration head with all eight Abwab tables empty. The
command never overwrites rows, imports inclusion-sync or Linking rows, or rewrites the source.
Require `verdict=pass`, `persisted=true`, and exact post-import counts before accepting the restore.

## Automated local reset and curated import

Run the complete local workflow only with one explicit Npgsql keyword connection string and the
PhraseSearch storage proof already verified by the operator:

```bash
ConnectionStrings__QuranDashboardDb='Host=localhost;Port=5432;Database=...;Username=...;Password=...' \
PhraseSearch__VerifiedDatabaseFreeBytes='<verified positive bytes>' \
PhraseSearch__DatabaseStorageProofContract='operator-verified-database-filesystem-v1' \
  ./scripts/reset-and-import-local --yes
```

The script accepts no connection fallback and no remote override. It parses the configured host,
then connects through libpq and requires the actual PostgreSQL server address to be loopback or a
Unix socket before any export or drop. It records the non-secret endpoint, port, database, and
PostgreSQL cluster system identifier, then revalidates the endpoint and cluster immediately before
every database-touching stage, including the export, and again before and after its child process.
Duplicate/multi-host connection keys fail closed. All EF and
DataImporter commands receive the same `ConnectionStrings__QuranDashboardDb` value. The exact
MASAQ sibling file
`resources/import-sources/masaq-corpus-aligned/masaq-search-words.dashboard-ready.json` and every
other required source are checked before database work. A local workflow lock refuses concurrent
reset/import runs.

If `resources/report/reset-and-import-local/recovery.json` exists from an interrupted run, the
script gives it precedence: it asks no question, performs no new export, and re-verifies the exact
v4 four-file artifact set recorded there. A recovery marker is bound to the verified local endpoint,
port, database, and cluster; a different target fails before mutation. Otherwise the script asks
whether Abwab data changed. Answer exactly `yes` to export and verify a fresh v4 snapshot, or `no`
to reuse the target-independent snapshot recorded in
`resources/report/reset-and-import-local/saved-abwab-snapshot.json`. Answering `no` fails before
mutation when no saved verified snapshot exists. Without a recovery marker the command requires an
interactive terminal, so a non-interactive invocation fails with guidance instead of guessing.

A fresh export atomically replaces the reusable saved-snapshot marker. Before the drop, the chosen
snapshot is copied into a distinct target-bound `recovery.json` marker atomically; it remains after
any failure so a rerun cannot export a partial or empty database. Snapshot, adjacent checksum,
JSON report, and Markdown report must all remain beneath the workflow report root and are
revalidated on every reuse. The export report must also prove non-empty `abwab_sections` and
`abwab_doors`. The recovery marker is removed only after the entire workflow succeeds, while the
reusable saved-snapshot marker remains for future local database targets. The selected Abwab
snapshot is always restored after reset regardless of whether the answer was `yes` or `no`.

After one Debug solution build whose child environment excludes the connection and libpq variables,
the script executes the built DataImporter and AccessAdmin DLLs directly. EF drop and update use
those fresh build outputs with `--no-build`. After migration it
restores and verifies Abwab first, then syncs the Access permission catalogue, imports foundation,
rebuilds display words, and performs the one-shot PhraseSearch build. Remaining imports follow:
enriched morphology, generated simple i3rab, Mutashabihat, navigation, full i3rab, curated-10
tafsirs, and curated-10 translations. Linking stays excluded and intentionally empty. Access users,
identities, sessions, grants, Owners, and authentication state are not restored.

Each run gets a unique directory under `resources/report/reset-and-import-local/` containing its
per-stage logs, wall times, exit codes, optional `/usr/bin/time -v` maximum RSS, and database size
before/after/delta for every database-touching stage when readable; build records `unavailable`
without opening a database connection. The run report also records whole-database size
before/after. A fresh export is stored in that run directory; a reused snapshot remains in its
original run directory and is referenced exactly by the current report. The first failed stage
stops the run; the evidence and recovery marker remain, and the report never claims full success.

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

Tafsir and translation imports default to `--profile curated-10`: exactly 10 tafsirs
(5 Arabic and 5 non-Arabic) and 10 translations in 10 languages. The complete source packages
remain available without modification through `--profile full`.

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

### PhraseSearch index operations

`build-phrase-index [--report-out <path>]` is dispatched through the DataImporter. The default
report root is `resources/report/quran-phrase-search/`; each attempt gets its own build-ID
directory. The command is one-shot: it acquires the builder fence, recovers abandoned status 1/2
attempts to metadata-only failed audits, then refuses before source bootstrap when any active,
previous, non-failed, or child-data-bearing generation remains. A full database reset is required
before rebuilding an existing index. Metadata-only failed audits do not block a retry.

On an eligible empty database, source bootstrap verifies every readable word has consistent simple
and tashkil identity links. Staging creates the only PhraseSearch data generation while the active
pointer remains null, so PhraseSearch stays unavailable until activation. Successful activation
points state to that sole ready generation and leaves the legacy `previous_build_id` null. The
legacy `Superseded` status is not emitted by this lifecycle. Failure or `Ctrl+C` deletes all child
data for the attempt and retains only failed audit metadata; crash recovery performs the same
child-to-parent cleanup under the builder fence.

Foundation import and display-word rebuild also take the builder fence before mutating PhraseSearch
source state. Their source transaction commits invalidation first, then a separately retryable
cleanup removes the old generation. Cleanup failure is reported as a persisted-success warning,
not as a rollback or ordinary pre-commit failure.

Before a build row is created, the preflight measures the current PostgreSQL database and existing
PhraseSearch relations. It conservatively reserves one full current database size as one-shot build
working space, one more for WAL, and the configured safety margin. This preserves the existing byte
safety formula; revising it requires separate measurement. Every environment, including loopback,
fails closed unless `PhraseSearch:VerifiedDatabaseFreeBytes` is supplied and
`PhraseSearch:DatabaseStorageProofContract` equals
`operator-verified-database-filesystem-v1`; no automatic filesystem measurement runs. Failed build
audit rows are
retained for `PhraseSearch:FailedBuildRetentionDays` (30 by default), while their child rows are
removed immediately. Recovery after activation is backup restore after a full database reset; there
is no operational previous-generation rollback or forward replacement build.

Run the deterministic one-shot build from `Backend/` with the exact target connection supplied
explicitly. Keep reports under the gitignored `resources/` tree unless a separate evidence task
authorizes a retained report:

```bash
./scripts/qd-build

ConnectionStrings__QuranDashboardDb='<target connection>' DOTNET_ENVIRONMENT=Production \
  dotnet run --project tools/QuranDashboard.DataImporter/QuranDashboard.DataImporter.csproj --no-build -- \
  build-phrase-index --report-out resources/report/quran-phrase-search
```

Never paste the resolved connection or a raw remote storage proof into a report. The capacity
formula and measured deployment guidance live in
[`Backend/README.md` §PhraseSearch build capacity](../README.md#phrasesearch-build-capacity).

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
of every dumped non-PhraseSearch `quran_*` table; the smoke data tier verifies the first two before
it starts a container.

`create-smoke-dump` deliberately excludes **DATA** for every `public.quran_phrase_*` table from
both the archive and manifest while migrations continue to own the schema. A fresh migrated
restore therefore keeps the seeded singleton `quran_phrase_index_state` row and no active,
previous, build, occurrence, or other derived PhraseSearch rows. This is an unavailable index,
not an empty successful index: every catalogued PhraseSearch route must answer `503` until an
operator runs `build-phrase-index`. The exclusion is implemented in
[`create-smoke-dump:161`](create-smoke-dump#L161) and asserted across the restored tables and all
ten routes by [`SmokeDataReadTests.cs:28`](../tests/QuranDashboard.Tests/Smoke/Data/SmokeDataReadTests.cs#L28)
and [`SmokeRouteCatalog.cs:137`](../tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs#L137).

Before replacing an existing canonical artifact, copy both the dump and manifest to a recoverable
operator backup, regenerate only from a source at the repository migration head, and compare every
non-PhraseSearch manifest table count with the prior artifact. The five built-in baseline counts
are necessary but do not prove that unrelated canonical families survived. The 2026-08-26 artifact
repair restored the prior artifact into a disposable database, migrated it to current head, and
preserved all 32 non-PhraseSearch manifest counts before regenerating with the PhraseSearch
exclusion; its tafsir source, entry, and ayah-entry counts remained 84, 382,704, and 523,824.
That is repair provenance for the external operator artifact, not a permanent promise that later
canonical imports can never change those counts.

Connection string resolution, in order: `ConnectionStrings__QuranDashboardDb`, then the
`ConnectionStrings:QuranDashboardDb` user secret of `api/QuranDashboard.Api`.

**Prerequisite: `pg_dump` 18 or newer**, matching the local PostgreSQL server. The smoke data
fixture restores into `postgres:18-alpine` for the same reason — a pg16 `pg_restore` rejects an
archive written by a newer `pg_dump`.

`resources/` is gitignored: the artifact, its manifest, operator backups, and build reports are
external operator products, regenerated or retained outside Git rather than committed. Do not put
connection strings, raw remote storage proofs, temporary paths, or volatile PhraseSearch build IDs
into tracked documentation.

### `wipe-abwab`

```bash
./scripts/wipe-abwab --yes
```

`TRUNCATE ... RESTART IDENTITY CASCADE` over a literal allowlist that owns the complete Abwab and
Abwab-to-Linking foreign-key closure: the eight `abwab_*` tables, door-scoped linking operations,
jobs, prepared-state tables, contributions, units, and door ayah/word projections. Linking
workspaces, `quran_*`, users, roles, and permissions are outside the closure. A schema change that
cannot survive existing Abwab rows therefore has a sanctioned local reset. Abwab content is
authored curation data: preserve and restore it through the v4 Abwab snapshot workflow; the
canonical dump covers `quran_*` only.

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

## `check-pending-model`

```bash
./scripts/check-pending-model --build|--no-build
```

Wraps `dotnet ef migrations has-pending-model-changes` for `QuranDashboardDbContext` with the
right project/startup pair and `DOTNET_ENVIRONMENT=Development` (`check-pending-model:53-59`). It
**never adds and never applies a migration**. `--no-build` requires
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
