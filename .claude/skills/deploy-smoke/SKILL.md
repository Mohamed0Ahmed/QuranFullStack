---
name: deploy-smoke
description: >-
  Local deployment / runtime smoke-check for the Quran Dashboard (المنهج القرآني)
  fullstack workspace (.NET backend + Angular frontend). Use this skill whenever you need
  to confirm a change still builds, migrates, and runs locally before it is reviewed or
  committed — after applying or generating an EF Core migration, after a dependency or
  security upgrade, before a final engineering review or opening a PR, before committing an
  App submodule-pointer bump, or after backend/frontend performance changes — even if the
  user only says "does this still run", "smoke test it", "sanity-check the build", or "is
  this safe to commit". It is a report-only check skill: it builds, inspects the local DB
  target, optionally applies local migrations only with explicit approval, and smoke-tests
  health / changed endpoints and the frontend build, then returns a verdict with evidence.
  It never drops or resets a database, never runs import/reseed/destructive scripts unless
  explicitly asked, and never targets a remote or production database silently. Not for
  implementing features or auto-fixing failures, not a full engineering or performance
  review (use engineering-review / performance-backend-review / performance-angular-review),
  and not a dependency vulnerability audit (recommend a separate dependency-audit pass).
---

# Deploy Smoke

A fast, **report-only** local smoke check: does this change still restore, build, migrate,
and run on this machine? The goal is to catch build breakage, pending/broken migrations,
and dead endpoints locally — before the change reaches engineering review, a PR, or a
submodule-pointer commit — without touching application code or destroying local data.

This skill produces findings, evidence, and a verdict. It does not fix failures; when it
finds a break it reports it and hands off.

## When to use

- **After an EF Core migration** — generated or applied; confirm it builds and the expected
  schema objects exist locally.
- **Before a final review / before opening a PR** — a green build + runtime smoke saves a
  review round-trip.
- **After a dependency or security upgrade** — confirm the app still restores, builds, and
  boots (then recommend a separate `dependency-audit` for vulnerability/version findings).
- **Before committing an App submodule-pointer bump** — make sure the pointed-at child
  commits actually build and run together.
- **After backend or frontend performance changes** — confirm the optimization did not break
  the build or runtime path (perf *quality* stays with the performance-* review skills).

## What this skill is NOT

- Not an implementation or auto-fix skill — it reports, it does not repair.
- It **must not** drop, reset, or recreate a database.
- It **must not** run import / reseed / rebuild / truncate / `--force` scripts unless the
  user explicitly asks for that run.
- It **must not** target a remote or production database. Local only, and only after the DB
  target is confirmed (see below).
- It is not a full review (that is `engineering-review` and the `performance-*` skills) and
  not a vulnerability audit (recommend `dependency-audit` separately instead of duplicating
  it here).

## Database safety (hard rules)

Migrations are the one place this skill can do harm, so treat the DB target as untrusted
until proven local:

1. **Verify and display the DB target before any migration action.** Read the connection
   from the backend config the project actually uses — `appsettings*.json`,
   `launchSettings.json`, environment variables, or `dotnet user-secrets` — and show the
   host/database (mask credentials). Confirm it is local (`localhost` / `127.0.0.1` / a
   local container), not a shared/staging/production host.
2. **Applying a migration is allowed only** when the user explicitly asked for a deploy
   smoke *with local DB migration/application*, or the task clearly says to apply local
   migrations. Otherwise stop at "migrations list / pending check" and report.
3. **If the DB target is unclear, ambiguous, or non-local — ask, or stop** and record a
   Deployment Note. Never guess and never apply against an unconfirmed target.
4. Listing migrations and checking for pending model changes are read-only and always fine.

## Before running: inspect, don't assume

Prefer the project's own scripts and confirm real names before running anything — do not
invent commands.

- **Backend:** locate the solution file (e.g. `QuranDashboard.sln`) under `Backend/` rather
  than assuming its name; identify the API startup project and the `DbContext` project;
  check for any build/migration helper scripts. Read the config files above to learn the DB
  target and which environment is active.
- **Frontend:** read `Frontend/quran-dashboard-ui/package.json` `scripts` for the real
  `build` / `test` entries and note whether a lockfile exists. The test command and its
  worker cap are documented — see the pointer under Frontend checks; do not hand-roll it.
- If a script encodes the intended command, use the script; only fall back to explicit CLI
  commands when no script exists.

## Checks

Run only what the change touches; mark untouched areas "not applicable". Capture the actual
command output as evidence.

### Backend

- **Restore + build** the backend solution (`dotnet build` on the located `.sln`). Build
  failure is BLOCKING.
- **Migrations list** (`dotnet ef migrations list`) for the API/DbContext project, and a
  **pending-model-changes check** where the project supports it (e.g.
  `dotnet ef migrations has-pending-model-changes`). Uncommitted model drift is a finding.
- **DB target verification** (see Database safety) — display it before any apply.
- **Apply local migration only with explicit local-DB approval** (`dotnet ef database
  update`). Otherwise report pending state as a Deployment Note.
- **Verify expected migration objects** when a migration was part of the change: after a
  confirmed local apply, read-only-check that the expected tables/columns/indexes/constraints
  exist (inspect the migration `Up()` and query the local schema). Do not mutate data.
- **Run the API locally** (or reuse an already-running local instance — check before
  starting a second one).
- **Smoke `/api/health`** and any endpoint the change touched; record status codes and the
  `ApiResponse` shape. A changed endpoint that 500s locally is a finding.

### Frontend

- **`npm install` only if needed** — lockfile changed, or `node_modules` missing. Skip
  otherwise.
- **`npm run build`** (confirm the script name first). Build failure is BLOCKING.
- **Focused tests:** for how to run Angular specs on this project's Vitest/jsdom harness
  (the mandatory fork cap, the `--run` caveat, jsdom's missing browser APIs, and how to
  read a harness timeout), follow
  `.claude/skills/test-guard/references/frontend-test-harness-constraints.md`. Do not
  reinvent the command.
- **Smoke the Angular app** only if the user asked or it is already part of the task.

### Full-stack

- **Distinguish build success from runtime smoke success** — "it compiled" is not "it ran".
  Say which you actually observed.
- **Distinguish a locally-applied migration from a pending deployment note** — a migration
  that only exists in code (not applied to a confirmed local DB) is a Deployment Note, not a
  pass.
- **Submodule / status awareness** — note dirty or pointer-drifted repos so the user knows
  what a commit would capture, but **do not commit** (that is `commit-workflow`).
- **Dependency / security upgrades** — recommend a separate `dependency-audit` pass rather
  than duplicating vulnerability/version checks here.

## Quran data safety

A smoke check must never become a reason to skip source-integrity checks. If a "make it
run" shortcut would bypass Quran text/data validation, provenance/traceability, or
source-unchanged checks, that is a finding, not an acceptable smoke shortcut. Apply the
shared rules: `.claude/skills/engineering-review/references/quran-data-safety.md`.

## Output format

Return the result in this structure:

1. **Verdict** — one of: `PASS` / `PASS WITH DEPLOYMENT NOTE` / `CHANGES REQUESTED` /
   `BLOCKED`.
2. **Scope checked** — backend / frontend / full-stack; which areas were in scope and which
   were not applicable.
3. **Commands run** — the actual commands (project scripts preferred), in order.
4. **Evidence** — real output: build result, migrations list / pending check, DB target
   shown, health/endpoint status codes, frontend build result, any test run.
5. **Deployment notes** — pending migrations not applied, unconfirmed DB target, config the
   deployer must set, anything that is fine locally but must be done for a real deploy.
6. **Risks / skipped checks** — what was not run and why (e.g. DB target unclear, app not
   started because runtime smoke was out of scope).
7. **Next recommended action** — one short, direct step (e.g. "apply migration on confirmed
   local DB then re-smoke", "hand to engineering-review", "run dependency-audit").

## Guardrails

- Report findings and evidence; do not implement or auto-fix.
- Local only; never touch a remote/production DB; verify and display the target first.
- No drop/reset/import/reseed/destructive runs unless the user explicitly asks.
- If build/test/runtime status is unknown, say unknown — do not assume success.
- Do not commit, stage, or push (that is `commit-workflow`).
- Prefer project scripts; inspect them before naming exact commands.
