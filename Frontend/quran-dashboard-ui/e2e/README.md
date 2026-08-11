# Browser E2E (Playwright)

Policy lives in `../../../TESTING_CONSTITUTION.md`. This file owns the retained Playwright journeys,
fixtures, prerequisites, and runtime invariants.

## Retained journeys

- `authenticated-smoke.e2e.ts` — authenticated session and application entry.
- `abwab-permissions.e2e.ts` — public Abwab access, hidden write controls, and anonymous write denial.
- `shell-nav.e2e.ts` — application-shell navigation.
- `mushaf-reader.e2e.ts` — reader paging, deep links, surah navigation, and fonts.
- `mushaf-ayah-study.e2e.ts` — ayah-study panels and related Quran data views.
- `words-explorers.e2e.ts` — the words hub and surviving explorer journeys.

`abwab-permissions.e2e.ts` is intentionally read-only: it sends a handcrafted anonymous write and
expects the Backend's `401` envelope, so it creates no sandbox or domain residue.

## Commands

```bash
npm run e2e
npm run e2e:headed
npm run e2e:ui
npm run e2e:typecheck
npx playwright test e2e/mushaf-reader.e2e.ts --project=default
npx playwright test e2e/abwab-permissions.e2e.ts --project=abwab --workers=1
```

`npm run e2e` runs the `default` project and then the `abwab` project with one worker. Both projects
remain non-empty. The headed and UI commands are debugging entry points; scope the `abwab` project
to one worker when running it directly.

## Prerequisites

- mkcert certificates `localhost.pem` and `localhost-key.pem` in the project root.
- A migrated local `quran_dashboard` source database and the `createdb`, `dropdb`, `pg_dump`, and
  `pg_restore` tools.
- `dotnet build Backend/QuranDashboard.sln` before the run; the Playwright backend starts with
  `--no-build`.

`run-backend.mjs` refuses non-local PostgreSQL, clones the source database into a uniquely named
disposable database, and drops the clone when the backend exits. The source database is never the
E2E write target.

## Fixtures and invariants

- Journey files are named `*.e2e.ts`; `playwright.config.ts` discovers only that suffix.
- Every test gets a fresh browser context. Do not add shared `storageState` reuse.
- `fixtures/app-test.ts` stubs the Logto origin and fails external browser requests.
- `fixtures/auth.ts` owns the authenticated persona. It supplies a test-only JWKS, mints matching
  tokens, provisions through the real access endpoints, removes its direct grant, and disables the
  persona during teardown. The temporary Owner and audit history exist only in the disposable clone.
- Authentication and authorization handlers remain the production composition; fixtures do not
  replace or bypass them.
- The frontend server may be reused. The backend on port 5015 must be Playwright-owned so its clone
  lifecycle and teardown remain coupled to the run.
- Read assertions avoid exact source-data totals so a legitimate source refresh does not make a
  browser journey brittle.

## Not this suite

Playwright does not replace Backend route-smoke or catalog gates. Backend lane selection lives in
`../../../Backend/tests/QuranDashboard.Tests/README.md`; which verification is required lives in
`../../../TESTING_CONSTITUTION.md` and an active plan's Testing Decision.
