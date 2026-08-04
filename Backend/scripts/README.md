# Dev CLI shortcuts

Short commands to build/run the backend API and Angular dev server from any directory.

## Commands

| Command | What it does |
|---------|----------------|
| `qd-build` | `dotnet build QuranDashboard.sln` for backend changes |
| `qd-api` | `dotnet run --launch-profile https --no-build`; opens Swagger when the API is ready |
| `qd-ui` | `npm run start:https` for the Angular dashboard |
| `export-swagger` | Builds the API (Release) and writes the OpenAPI spec to `Frontend/quran-dashboard-ui/openapi/swagger.json` via the Swashbuckle CLI (`Backend/dotnet-tools.json` manifest); no running server or database needed |
| `check-api-contract` | Runs `export-swagger`, regenerates the frontend API models (`npm run generate:api`), then fails with `git diff --exit-code` if either committed output is stale. It checks the spec and the generated client — the two things a caller breaks against — and deliberately not the browsable Redoc bundle, which is untracked and therefore invisible to `git diff` |
| `create-smoke-dump` | Regenerates the canonical `quran_*` data dump the backend smoke data tier restores: `resources/db-dumps/quran-canonical/{quran-canonical.dump,manifest.json}` |
| `wipe-abwab` | Empties the six `abwab_*` tables on a local database, leaving the canonical `quran_*` data intact |
| `add-mig <Name>` | `dotnet ef migrations add <Name>` against `Infrastructure` with `Api` as startup project. EF tooling only — never hand-write a migration (`Backend/CLAUDE.md`) |
| `update-db` | `dotnet ef database update` — applies pending migrations to the configured database |
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

`TRUNCATE ... RESTART IDENTITY CASCADE` over exactly six tables — `abwab_sections`,
`abwab_doors`, `abwab_door_aliases`, `abwab_door_relations`, `abwab_templates`,
`abwab_template_nodes` — so a schema change that cannot survive existing abwab rows (a column
becoming `NOT NULL`, say) has a sanctioned local reset. Abwab content is authored curation data:
nothing restores it, and the canonical dump covers `quran_*` only.

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
- **a literal six-table allowlist** — no wildcard, no catalog query. `CASCADE` is safe only
  because every foreign key pointing into an abwab table originates from another table in that
  same list, so the cascade closure cannot reach `quran_*`, `users`, or `roles`. A seventh abwab
  table means editing the list deliberately;
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

After the first successful build, use `qd-api` directly until backend code changes.

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
