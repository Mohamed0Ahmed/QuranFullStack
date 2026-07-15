# Backend → Railway (Docker) + fresh Railway Postgres (Neon abandoned) — Implementation Plan

**Status:** Plan only. No code, Dockerfile, config, migration, script, or database action has been
performed. This document is the plan, not the implementation.
**Method:** Normal implementation plan (not Spec Kit). Bounded deployment/infra work: no new product
feature, no API contract change, no schema change, no Quran-data derivation.
**Author session date:** 2026-07-15.
**Grounded on:** the deployment-readiness inspection produced earlier this session, re-verified against
the working tree (see “Repository reality check” below).

---

## 0. Repository reality check (locked decisions vs. repo)

| Locked decision | Repo reality | Verdict |
|---|---|---|
| .NET 10 across backend | 8/8 `.csproj` = `net10.0`; no `global.json` | ✅ matches |
| Latest migration = `20260704102858_AddQuranLemmaAnalyses` (17th) | `ls Migrations/` → 17 non-designer classes; newest by timestamp = `20260704102858_AddQuranLemmaAnalyses` | ✅ matches |
| Frontend prod URL = `https://manhaj.runasp.net` | `environment.ts:3` = `https://manhaj.runasp.net`; `environment.development.ts:3` = `https://localhost:5015` | ✅ matches |
| Prod config only via env vars | `appsettings.Production.json` gitignored + untracked (not in image); `appsettings.json` (tracked) holds placeholders | ✅ matches |
| Kestrel needs explicit port | No `UseUrls`/`UseKestrel`/`ListenAnyIP`/`PORT` in code | ✅ matches |
| No startup auto-migrate | `.Migrate()/MigrateAsync` only in test fixtures; `Program.cs`/DI have none | ✅ matches |
| Health = `GET /api/health` returns 200 | `HealthController` at `Route("api/health")`, always `Ok(...)` | ✅ matches |
| CORS array gotcha | `Cors:AllowedOrigins` bound as `string[]`, throws if empty | ✅ matches |
| `pg_trgm` required | `QuranDashboardDbContext.cs:53` `HasPostgresExtension("pg_trgm")` | ✅ matches |

**Locked decision (revised 2026-07-15): Neon is ABANDONED — treat as non-existent.** The Railway
Postgres is built **fresh** from the repo's canonical staged source packages via the DataImporter (no
`pg_dump`/`pg_restore` from Neon). Feature 026 is merged to `main`, so the deployed code is at migration
17. This **RESOLVES** the two conflicts the earlier draft flagged (see [§9]):
1. ~~Undefined deployed git ref~~ → **RESOLVED:** deploy `main` (contains migration 17).
2. ~~Empty `quran_lemma_analyses` under a no-re-import rule~~ → **RESOLVED:** a fresh enriched
   `import-morphology` populates `quran_lemma_analyses` (and every other table) as its canonical output.

---

## 1. Objective & target-state architecture

Move the backend onto a single Railway (Hobby) project in region **EU-West (Amsterdam)** and build its
Postgres **fresh from the repo's canonical staged source packages** (Neon is abandoned, not a source),
with **zero C# change**, then repoint the Vercel frontend to the new backend URL.

**Current state**
```
Browser → Vercel (Angular) → https://manhaj.runasp.net (ASP.NET host) → Neon Postgres (ABANDONED)
```

**Target state**
```
Browser → Vercel (Angular) → Railway API (Docker, .NET 10, EU-West) → Railway Postgres (EU-West, same project)
                                   env vars only ─┘                     ├─ pg_trgm extension
                                                                        └─ built fresh via canonical enriched import
```

- API and Postgres live in the **same Railway project**, same region, private networking between them.
- The Railway Postgres is populated by running EF migrations (to id 17) + the DataImporter enriched
  seeding chain **locally against the Railway public endpoint** — **not** copied from Neon.
- TLS terminated at Railway’s edge; the container serves plain HTTP on `$PORT`.
- Production configuration is supplied **entirely** by Railway environment variables.

## 2. Scope & non-goals

**In scope**
- New container/build artifacts for `api/QuranDashboard.Api` (Dockerfile, `.dockerignore`, optional
  `railway.json`).
- Railway project + Postgres provisioning and environment-variable configuration.
- Fresh Railway Postgres: EF migrate to id 17 + canonical enriched DataImporter seeding chain, run
  locally against the Railway endpoint. **Neon is abandoned as a source.**
- One frontend env line (`apiBaseUrl`) + Vercel redeploy.
- A deployment runbook doc.

**Explicit non-goals (do NOT do)**
- ❌ No C# change of any kind (no refactor, no `Program.cs`/pipeline edit, no health-endpoint change).
- ❌ No EF schema change / no new migration.
- ❌ No data sourced from Neon (no `pg_dump`/`pg_restore`; Neon is abandoned). The canonical enriched
  import is the ONLY population path; no byte-faithful copy, no re-derivation shortcuts.
- ❌ No startup auto-migrate.
- ❌ No use of the destructive dev scripts (`drop-db`, `reset-db`, `update-db`) against Railway.
- ❌ No `environment.development.ts` change.
- ❌ No unrelated cleanup, dependency bump, or “while we’re here” edits.
- ❌ No commit/push/PR from this plan; commits happen later on explicit user request (see §11).

## 3. Affected file areas / net-new files

All net-new except the one frontend line and the docs.

| Path | New/Edit | Purpose |
|---|---|---|
| `Backend/Dockerfile` | **new** | Multi-stage build (SDK 10 → aspnet 10 runtime), publishes `api/QuranDashboard.Api`, binds `$PORT`. |
| `Backend/.dockerignore` | **new** | Exclude `bin/`, `obj/`, `appsettings.Production.json`, user-secrets, `resources/`, `.git/`, test/tooling noise from the build context. |
| `Backend/railway.json` | **new (optional)** | Pin builder = Dockerfile, `healthcheckPath=/api/health`, restart policy. |
| `Frontend/quran-dashboard-ui/src/environments/environment.ts` | **edit (1 line)** | `apiBaseUrl` → Railway public URL. |
| `docs/deployment-railway/plan.md` | **this file** | The plan. |
| `docs/deployment-railway/runbook.md` | **new (Phase f)** | Operational runbook: env-var set, migrate + enriched-import commands, count gate, rollback. Created during implementation, not now. |

**Nearest-README obligations (per root & `Backend/CLAUDE.md`):**
- `Backend/README.md` — add a short “Deploy” subsection pointer to `docs/deployment-railway/` in the
  same change that lands the Dockerfile (WHAT changed: the backend is now containerized/Railway-hosted).
- `docs/README.md` — no change required (it documents planning-doc conventions; a topical
  `deployment-railway/` folder is consistent with existing topical folders `api-reference/`,
  `contracts/`). Do not add a `feature-XXX` folder (this is infra, not a feature).

## 4. Phases (ordered, with dependencies & stop points)

Each phase has a hard **STOP** — do not begin the next phase until its exit check passes.

### Phase (a) — Railway project + Postgres provisioning
**Depends on:** nothing.
**Do:**
1. Create a Railway **Hobby** project, region **EU-West (Amsterdam)**.
2. Add a **Postgres** service in the same project/region.
3. Size storage from the known corpus footprint (Neon is not queried — it is abandoned). The dataset is
   text-heavy: ~1.04M translation rows + ~906K tafsir rows + 130 indexes (per
   `current-database-inventory.md`); provision a few GB with headroom. After the fresh import (Phase d),
   confirm actual usage with `SELECT pg_size_pretty(pg_database_size(current_database()));` on Railway.
4. Confirm the Postgres role can `CREATE EXTENSION pg_trgm` (Railway’s default superuser can).
**STOP / exit check:** Railway project exists in EU-West with a reachable empty Postgres; storage sized
from the corpus footprint; `pg_trgm` creatable. **No app deployed yet.**

### Phase (b) — Dockerfile + .dockerignore (+ railway.json) — ✅ IMPLEMENTED on branch `dockerization`
**Depends on:** (a) not required; can run in parallel with (a).
**Status:** done — `Backend/Dockerfile`, `Backend/.dockerignore`, `Backend/railway.json` created and the
image builds/boots/binds `$PORT` as a non-root user with no secret in the image (verified locally).
**Do:**
1. `Backend/Dockerfile`, multi-stage, intended behavior:
   - **build stage** `mcr.microsoft.com/dotnet/sdk:10.0`: copy solution + project files, `restore`,
     copy source, `dotnet publish api/QuranDashboard.Api -c Release -o /app/publish` (no `global.json`,
     so the base-image tag is the only version pin — verify tag availability, see §9).
   - **runtime stage** `mcr.microsoft.com/dotnet/aspnet:10.0`: copy `/app/publish`, set
     `ENTRYPOINT ["dotnet","QuranDashboard.Api.dll"]`. Do **not** bake any secret, connection string,
     or `appsettings.Production.json` into the image. Do **not** set an HTTPS port. The container listens
     on `$PORT` via the env var set in Phase (c) (`ASPNETCORE_URLS=http://0.0.0.0:${PORT}`); do not
     hard-code a port in the Dockerfile.
2. `Backend/.dockerignore`: at minimum `bin/`, `obj/`, `**/appsettings.Production.json`,
   `**/appsettings.*.local.json`, `**/secrets.json`, `resources/`, `.git/`, `tests/`, `tools/`
   (the API image does not need the test or importer projects), `**/*.user`.
3. `Backend/railway.json` (optional but recommended):
   `{ "build": { "builder": "DOCKERFILE", "dockerfilePath": "Dockerfile" },
   "deploy": { "healthcheckPath": "/api/health", "restartPolicyType": "ON_FAILURE" } }`.
**STOP / exit check:** Files exist and are internally consistent (build context root = `Backend/`,
publish path correct). **Do not build or push in this session** — build happens in Railway during
implementation.

### Phase (c) — Railway env-var configuration + API deploy
**Depends on:** (a) (Postgres connection details), (b) (image).
**Do:** deploy the container image from `main` (Railway builds the `Backend/` Dockerfile) and set on the
Railway API service, exactly:
```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ConnectionStrings__QuranDashboardDb=Host=<railway-pg-host>;Port=5432;Database=<db>;Username=<u>;Password=<pw>;SSL Mode=Prefer
Cors__AllowedOrigins__0=https://manhag-qurany-ui.vercel.app
Cors__VercelPreviewHostPrefix=manhag-qurany
```
- Use Railway’s provided Postgres reference for the connection string; keep `SSL Mode=Prefer`.
- **Array gotcha:** `Cors:AllowedOrigins` is bound as `string[]`; it MUST be indexed
  (`Cors__AllowedOrigins__0`, `__1`, …). A single comma-joined value leaves the section empty and the
  app throws `InvalidOperationException` on the first CORS request.
- Secrets exist only here — never in a committed file.
**STOP / exit check:** all five keys present; `${PORT}` resolves; connection string points at the Railway
Postgres (not Neon). **Do not cut frontend over yet.**

### Phase (d) — Fresh Railway Postgres build: migrate (id 17) + canonical enriched import + count gate
**Depends on:** (a), (c). **Neon is abandoned — it is NOT a data source; no `pg_dump`/`pg_restore`.**
**Preconditions:**
- The canonical staged source packages under `resources/import-sources/` are present **locally** — they
  are local + gitignored, **not** in the repo or the container. Confirm they exist before starting.
- The Railway Postgres is a **fresh empty** instance; `pg_trgm` is creatable (default superuser).
**Do (every step runs LOCALLY, pointed at the Railway PUBLIC Postgres endpoint, `SSL Mode=Prefer`; keep
the connection string in local user-secrets, never committed):**
1. **(d1) Migrate to 17.** Apply all migrations to the empty Railway DB to reach migration
   `20260704102858_AddQuranLemmaAnalyses` (17):
   `dotnet ef database update --project infrastructure/QuranDashboard.Infrastructure --startup-project api/QuranDashboard.Api --context QuranDashboardDbContext`,
   with the Railway connection supplied via `ConnectionStrings__QuranDashboardDb`. `main` contains all 17
   migrations, so they apply cleanly on the empty DB.
   - **NEVER** run `scripts/drop-db` / `reset-db` / `update-db` against Railway (local-dev helpers;
     drop/reset are destructive). The DB is fresh — migrate + import only.
2. **(d2) Canonical enriched import.** Run the DataImporter CLI locally against the Railway DB, in the
   seeding order from `Backend/report/database-inventory/database-reset-and-seeding-order.md` §3 (connection
   via `ConnectionStrings__QuranDashboardDb` — the importer host does not read API user-secrets):
   1. `import-foundation` → 2. `rebuild-words` → 3. `import-morphology` (**enriched** — populates
   `quran_word_morphology`, `_segments`, `quran_roots`, `quran_lemmas`, `quran_stems`, `quran_pos_tags`,
   **and `quran_lemma_analyses`**) → 4. `generate-i3rab` → 5. `import-mutashabihat` → 6. `import-tafsirs`
   → 7. `import-translations` → 8. `import-navigation-metadata` → 9. `import-full-i3rab`.
   Confirm each verb's `--source`/`--report-out`/`--force` against its feature quickstart. No byte-faithful
   copy, no re-derivation shortcut — the enriched import is the canonical default.
3. **(d3) Quran-data safety count gate — FAIL CLOSED.** Verify the resulting counts EXACTLY match the
   accepted enriched baseline (§5), plus `content_coverage_count = 6236` on the three content families.
   Any mismatch → **STOP**; do not proceed to cutover.
**STOP / exit check:** migration level = 17; every §5 count matches exactly; `content_coverage_count =
6236` holds. Any mismatch → **STOP** — the current live backend (`manhaj.runasp.net`) keeps serving; do
not repoint the frontend.

### Phase (e) — Frontend repoint + Vercel redeploy
**Depends on:** (b)+(c)+(d) green and the Railway API reachable at its public URL.
**Do:**
1. Edit `Frontend/quran-dashboard-ui/src/environments/environment.ts` line 3 only:
   `apiBaseUrl: 'https://manhaj.runasp.net'` → `apiBaseUrl: '<railway-public-url>'`.
   Leave `environment.development.ts` unchanged.
2. Redeploy the frontend on Vercel.
**STOP / exit check:** production Vercel build serves the new `apiBaseUrl`; the origin
`https://manhag-qurany-ui.vercel.app` reaches the Railway API and CORS passes (§7).

### Phase (f) — Cutover verification + retire old stack + docs
**Depends on:** (e).
**Do:**
1. Full cutover smoke (§7 final block).
2. Author `docs/deployment-railway/runbook.md` and the `Backend/README.md` deploy pointer (§8).
3. Only **after** verified cutover: retire the old backend (`manhaj.runasp.net`) and its Neon DB. Neon is
   already abandoned as a source, so no data extraction or credential rotation is gated on it — the old
   stack is simply kept alive until cutover is verified, then torn down.
**STOP / exit check:** acceptance criteria (§10) all met; runbook written; old stack retired last.

## 5. Data rules & integrity

- **Fresh canonical build (no Neon):** the Railway DB is populated by EF migrate (id 17) + the enriched
  DataImporter chain (§4d), run locally against the Railway endpoint. No `pg_dump`/`pg_restore`, no
  `EnsureCreated`, no re-derivation shortcut. Derived read-models (`quran_words_ordered_*`,
  `quran_words_unique_*`, `quran_lemma_analyses`, …) are produced by the importer, not copied.
- **Extension:** `pg_trgm` must exist on Railway before the import (`HasPostgresExtension` in the
  DbContext; several `gin_trgm`/search indexes depend on it). The foundation migration issues
  `CREATE EXTENSION pg_trgm` during (d1).
- **SSL:** connection string keeps `SSL Mode=Prefer`.
- **Migration target:** id `17` (`20260704102858_AddQuranLemmaAnalyses`), applied by `dotnet ef database
  update` on the empty DB → `__EFMigrationsHistory` = 17.
- **Count gate — accepted enriched baseline (authoritative; FAIL CLOSED on any mismatch):**

  Enriched morphology output (the locked baseline for the feature-026 / migration-17 tables):

  | Table | Expected rows |
  |---|---:|
  | `quran_word_morphology` (readable words) | 77,432 |
  | `quran_word_morphology_segments` | 128,219 |
  | `quran_roots` | 1,642 |
  | `quran_lemmas` | 4,817 |
  | `quran_lemma_analyses` | 4,832 |
  | `quran_stems` | 11,843 |
  | `quran_pos_tags` | 49 |

  Stable tables cross-checked against `current-database-inventory.md` (2026-06-29):

  | Table | Expected rows |
  |---|---:|
  | `quran_surahs` / `quran_ayahs` / `quran_mushaf_pages` | 114 / 6,236 / 604 |
  | `quran_words` | 83,668 |
  | `quran_words_ordered_simple` / `_tashkeel` | 77,432 each |
  | `quran_words_unique_simple` / `_tashkeel` | 14,783 / 21,294 |
  | `quran_tafsir_entries` / `quran_tafsir_ayah_entries` | 382,704 / 523,824 |
  | `quran_translation_ayah_entries` | 1,041,412 |
  | `quran_full_i3rab_ayah_entries` | 24,944 |
  | `__EFMigrationsHistory` | 17 |

  Also verify: `content_coverage_count = 6236` on the tafsir/translation/full-i3rab source families;
  52 FKs resolve (0 orphans on cascade FKs); `quran_lemma_analyses` present (migration 17).
- **⚠ Baseline supersedes the inventory for the enriched tables.** The 2026-06-29 inventory predates the
  feature-026 enriched import and lists `quran_lemmas` = 4,790, `quran_stems` = 12,108, and no
  `quran_lemma_analyses`. The enriched baseline above (`quran_lemmas` 4,817, `quran_stems` 11,843,
  `quran_lemma_analyses` 4,832) is **authoritative** for those three; the inventory remains the reference
  for all other (unchanged) tables. A difference in those three rows is EXPECTED, not a data-loss signal.

## 6. Configuration & runtime rules

- **Port binding:** `ASPNETCORE_URLS=http://0.0.0.0:${PORT}`. Kestrel does not read Railway’s `$PORT`
  on its own; the env var is what makes it bind the injected port on all interfaces.
- **CORS array:** indexed env vars only (`Cors__AllowedOrigins__0`); empty section → app throws.
- **HTTPS redirect:** leave `app.UseHttpsRedirection()` as-is. With no HTTPS port configured inside the
  container it logs one warning and no-ops (Railway terminates TLS at the edge, forwards HTTP). Do not
  add an HTTPS port or forwarded-headers middleware.
- **Stateless API:** no local filesystem writes on the request path; importers/report-writers are
  CLI-only and not in the API image. Ephemeral container FS is fine.
- **Environment:** `ASPNETCORE_ENVIRONMENT=Production` (Swagger stays off; only the tracked
  `appsettings.json` placeholders ship in the image — all real values come from env).

## 7. Verification per phase

- **(b) container:** on the Railway build/deploy, container starts and **binds `$PORT`** (Railway shows
  the service healthy); logs show Kestrel listening on `http://0.0.0.0:<port>`; no secret string appears
  in build logs or image layers (`appsettings.Production.json` absent from the image — confirm via the
  `.dockerignore` and by inspecting the deployed filesystem/listing).
- **(c) env:** app boots without the `ConnectionStrings` / `Cors:AllowedOrigins` startup exceptions;
  `GET /api/health` returns `200` with `status: healthy` (DB check green ⇒ Railway Postgres reachable
  with SSL).
- **(d) data:** the §5 count gate passes exactly (enriched baseline + stable tables + `content_coverage_count = 6236`); migration level = 17; `pg_trgm` present; spot-read a few API endpoints backed by trigram/search indexes to confirm the enriched import built them.
- **(e) frontend:** the deployed Vercel bundle requests `<railway-url>/api/...`; a cross-origin request
  from `https://manhag-qurany-ui.vercel.app` succeeds (response carries
  `Access-Control-Allow-Origin` for that origin, credentials allowed); a Vercel **preview** origin
  `https://manhag-qurany-*.vercel.app` is also accepted (validates `VercelPreviewHostPrefix`).
- **(f) cutover smoke:** representative read flows work end-to-end through Railway (mushaf page, a words
  explorer list, health); no CORS errors in the browser console; latency acceptable (API + DB
  co-located in EU-West).

## 8. Documentation updates (same change)

- **`docs/deployment-railway/runbook.md`** (new, Phase f): the operational runbook — exact env-var set,
  the `dotnet ef database update` command and the ordered DataImporter seeding chain actually used, the §5
  count gate, rollback steps, and the retire-old-stack checklist. Lives under `docs/deployment-railway/`
  (topical folder, consistent with `docs/api-reference/`, `docs/contracts/`).
- **`Backend/README.md`**: add a brief “Deployment” pointer (backend is containerized and Railway-hosted;
  see `docs/deployment-railway/`) in the same commit as the Dockerfile — required because the change
  alters how the backend is hosted/run (a WHAT change the README must reflect).
- No `docs/README.md` change; no `feature-XXX` folder (infra, not a feature). The fresh import **follows**
  `database-reset-and-seeding-order.md` but does not require editing it (its migration list is dev-facing
  and intentionally lags; the authoritative migration target here is id 17).

## 9. Risks, rollback & stop conditions

### ✅ Earlier conflicts — RESOLVED by the revised decision (Neon abandoned, fresh canonical import)
1. **Deployed git ref** — **RESOLVED.** Feature 026 is merged to `main`; Railway builds and deploys
   `main`, which contains migration 17. No ambiguity remains.
2. **`quran_lemma_analyses` population** — **RESOLVED.** The fresh enriched `import-morphology`
   (`EfBulkMorphologyWriter` → `MorphologyBulkCopier.CopyLemmaAnalysesAsync`) populates it as canonical
   output (accepted baseline 4,832 rows). Because we build fresh rather than copy from Neon, there is no
   “empty table” gap and no re-import constraint to violate — the import IS the method.

**Locked decision recorded:** Neon is abandoned (treated as non-existent); the Railway Postgres is built
fresh from the repo's canonical staged source packages via the DataImporter. No `pg_dump`/`pg_restore`.

### Other risks & mitigations
- **.NET 10 base-image tag availability.** `mcr.microsoft.com/dotnet/{sdk,aspnet}:10.0` must exist/pull;
  no `global.json` pins a patch. Mitigation: confirm the tag pulls in Phase (b); pin a specific tag once
  confirmed.
- **CORS misconfig (array).** Mitigation: verify indexed env keys; §7(c) catches it at boot.
- **Port not bound.** Mitigation: `ASPNETCORE_URLS`; §7(b) Railway healthcheck catches it.
- **Health probe is liveness-only.** `/api/health` returns 200 even when the DB check is degraded (body
  carries status). Railway’s healthcheck therefore verifies “process up,” not “DB healthy.” Accepted
  as-is (no code change). Operators read the JSON body for DB status.
- **Secret leakage into image.** Mitigation: `.dockerignore` excludes `appsettings.Production.json` +
  user-secrets; §7(b) inspects the image.

### Rollback
- **Before frontend cutover (Phases a–d):** discard the Railway services; nothing user-facing changed;
  the current live backend (`manhaj.runasp.net` + its Neon DB) keeps serving prod untouched.
- **After cutover (Phase e), if problems:** revert `environment.ts` `apiBaseUrl` to
  `https://manhaj.runasp.net` and redeploy Vercel → traffic returns to the old backend. The old stack is
  kept alive until Railway is import-verified and cut over, so rollback is a single frontend redeploy.

### Hard STOP conditions
- **Post-import count mismatch** against the §5 enriched baseline / stable tables (or
  `content_coverage_count ≠ 6236`) → **STOP** before the frontend repoint; the old stack keeps serving.
- `pg_trgm` cannot be created on Railway → **STOP** (search indexes would fail).
- Canonical staged source packages under `resources/import-sources/` missing locally → **STOP** (the
  fresh import cannot run).

## 10. Acceptance criteria

1. Railway API (Docker, .NET 10) deployed in EU-West, binding `$PORT`, `GET /api/health` → `200`
   `healthy`.
2. Railway Postgres in the same project/region is built fresh (migrate to id 17 + canonical enriched
   import), with every §5 count matching the accepted enriched baseline exactly and
   `content_coverage_count = 6236`, `pg_trgm` present. Built from canonical sources, not copied from Neon.
3. No secret is present in the built image or any committed file; all prod config is Railway env vars.
4. Vercel frontend serves the new `apiBaseUrl` and reaches Railway; CORS passes for the production and
   preview Vercel origins.
5. Zero C# changes; only net-new infra artifacts + one frontend line + docs.
6. Old stack (`manhaj.runasp.net` + Neon) retired **only after** 1–4 verified.
7. `docs/deployment-railway/runbook.md` and the `Backend/README.md` deploy pointer exist.

## 11. Expected commit boundary (commits later, on explicit request)

- **Commit 1 (backend, infra):** `Backend/Dockerfile`, `Backend/.dockerignore`,
  `Backend/railway.json` (if used), `Backend/README.md` deploy pointer, `docs/deployment-railway/*`.
  Suggested type: `chore(deploy)` / `docs(deploy)`.
- **Commit 2 (frontend):** `Frontend/quran-dashboard-ui/src/environments/environment.ts` one-line
  `apiBaseUrl` change. Suggested type: `chore(frontend)`.
- Railway env vars, the `ef database update`, and the DataImporter enriched seeding chain are
  **operations**, not commits — recorded in the runbook, not in git.
- No commit, push, or PR is created from this plan; do so only when the user explicitly asks.
