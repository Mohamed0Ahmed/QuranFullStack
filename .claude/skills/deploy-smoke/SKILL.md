---
name: deploy-smoke
description: Use when explicitly asked for a Quran Dashboard deployment preflight or local runtime smoke check.
---

# Deploy Smoke

## Responsibility

Run the explicitly requested deployment preflight or local runtime smoke and report what
was observed: build state of the requested target, migration and pending-model state, a
confirmed and masked local database target, health and changed-endpoint responses, and
deployment notes. It may build **only a missing targeted deployable artifact required by
the requested smoke** — nothing broader. It owns the lifecycle of any process it starts:
reuse an already-running local instance where possible, and stop what it started when
done.

**Not this skill's job:** running proactively before reviews, commits, or PRs; test
lanes or broad test commands of any kind; package installs; Git actions or Git status
reporting; source-code changes; remote deployment; or destructive data work. A runtime
curl here is a **smoke observation**.

## Database target safety (hard rules)

1. Read the connection from the config the requested target actually uses and **display
   the host/database with credentials masked** before any database action. Confirm it is
   local (`localhost` / `127.0.0.1` / a local container) — never a shared, staging, or
   production host.
2. Listing migrations and checking pending model changes are read-only and always fine.
   **Applying** a migration requires the user to have explicitly asked for a smoke with
   local migration application; otherwise report the pending state as a deployment note.
3. If the target is unclear, ambiguous, or non-local — stop and ask. Never guess and
   never apply against an unconfirmed target.
4. Never drop, reset, or recreate a database, and never run
   import/reseed/truncate/`--force` scripts as part of a smoke.

## Workflow

1. Confirm the explicitly requested scope (backend, frontend, or full-stack path).
2. Read `Backend/README.md` §Deployment (Docker / Railway) for the deployment truth of
   the requested target, and `Backend/scripts/README.md` when a
   project script is involved. Prefer the project's own scripts; confirm real names
   before running anything.
3. Observe only what the request touches; mark the rest not applicable. Distinguish "it
   compiled" from "it ran", and an applied local migration from a pending deployment
   note.

## Conditional context

- Effective configuration (`appsettings*.json`, `launchSettings.json`, environment,
  user-secrets presence) — only to confirm the target for the requested path, with
  secrets masked.
- `Frontend/quran-dashboard-ui/package.json` scripts — only when the requested smoke
  includes the frontend build or app.
- The implicated source and validation code when the smoke path touches Quran source/import
  behavior.

## Output

1. **Verdict** — PASS / PASS WITH DEPLOYMENT NOTE / CHANGES REQUESTED / BLOCKED.
2. **Scope checked** — and what was not applicable.
3. **Commands run** — in order, project scripts preferred.
4. **Evidence** — masked DB target, build/migration output, endpoint status codes.
5. **Deployment notes** — pending migrations, config the deployer must set.
6. **Skipped checks** — what was not run and why.
7. **Next recommended action** — one short, direct step.

Report-only: findings and evidence, never fixes. If a status is unknown, say unknown.
