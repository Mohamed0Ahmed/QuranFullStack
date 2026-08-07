# Frontend test gates

This folder holds one script, `verify-test-gates.mjs`, and this file. Together they are the
contract behind the `npm run test:*` lanes: `../angular.json` defines *what* each lane selects,
`../package.json` defines *how* it is invoked, and the script proves the two stay honest.

Which lane to run and when is `../../../TESTING_STRATEGY.md` §4 and §5. This file is what the
lanes **are**.

## The named configurations

`architect.test` in `../angular.json` uses the `@angular/build:unit-test` builder with the Vitest
runner. Its `options.include` is `**/*.spec.ts` — the full gate — and it carries exactly these
named configurations:

| Configuration | Selects |
|---|---|
| `feature-access-admin` | `src/app/features/access-admin/**/*.spec.ts` |
| `feature-abwab` | `src/app/features/abwab/**/*.spec.ts` |
| `feature-auth` | `src/app/features/auth/**/*.spec.ts` |
| `feature-dashboard` | `src/app/features/dashboard/**/*.spec.ts` |
| `feature-mushaf` | `src/app/features/mushaf/**/*.spec.ts` |
| `feature-words` | `src/app/features/words/**/*.spec.ts` |
| `shared` | `src/app/*.spec.ts`, `src/app/core/**`, `src/app/shared/**`, `src/environments/**` |
| `authorization` | the cross-cutting auth boundary: `src/app/app.config.auth.spec.ts`, `src/app/app.routes.spec.ts`, `src/app/core/auth/**`, `src/app/core/data-access/secure-url.interceptor.spec.ts`, `src/app/features/auth/**`, `src/environments/environment-guard.spec.ts` |
| `composition` | every `src/app/**/*.component.spec.ts` and `*.directive.spec.ts`, plus the named application/overlay compositions |
| `fast` | pure model/util/data/state/cache/URL-codec specs, listed pattern by pattern rather than by folder |

The first seven are the **primary areas** and they partition the tree: every spec under `src/`
belongs to exactly one of them. `authorization`, `composition`, and `fast` are deliberate
cross-cuts that overlap the areas — that is their purpose, and the script checks them differently
because of it.

## The commands

Run all of these from `Frontend/quran-dashboard-ui/`.

| Command | What it runs |
|---|---|
| `npm test` | the full gate, and **the one place the run environment is set** |
| `npm run test:fast` | `--configuration=fast` |
| `npm run test:feature:access-admin` | `--configuration=feature-access-admin` |
| `npm run test:feature:abwab` | `--configuration=feature-abwab` |
| `npm run test:feature:auth` | `--configuration=feature-auth` |
| `npm run test:feature:dashboard` | `--configuration=feature-dashboard` |
| `npm run test:feature:mushaf` | `--configuration=feature-mushaf` |
| `npm run test:feature:words` | `--configuration=feature-words` |
| `npm run test:authorization` | `--configuration=authorization` |
| `npm run test:composition` | `--configuration=composition` |
| `npm run test:shared` | `--configuration=shared` |
| `npm run test:full` | `npm test`, named so a broad run is legible in evidence |
| `npm run test:gates` | `node testing/verify-test-gates.mjs` — this folder's script; no Angular build |
| `npm run check:permission-catalogue` | compares the frontend typed permission codes to `Backend/.../AbwabPermissions.cs` |
| `npm run typecheck:app` | `tsc -p tsconfig.app.json --noEmit` |
| `npm run typecheck:spec` | `tsc -p tsconfig.spec.json --noEmit` |
| `npm run typecheck` | both of the above, in order |
| `npm run build:verify` | a timeout-bounded production `ng build` |
| `npm run test:pre-pr` | `check:permission-catalogue` → `typecheck` → `build:verify` → `test:full` |

`test:gates` is deliberately **not** part of `test:pre-pr`: it is a structural check on
`../angular.json`, so it belongs with the change that edits the configurations, not with the run
that executes them. Run it whenever a spec file is added, moved, renamed, or deleted, and
whenever an `include` pattern changes.

### The fork cap lives in exactly one place

`npm test` is `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2 timeout --signal=TERM --kill-after=30s 15m
ng test`. **Every lane above delegates to it** as `npm test -- --configuration=<name>`, which is
what keeps the cap and the run timeout in one line of `../package.json` instead of eleven.

Without the cap the run OOMs or freezes the machine. It cannot be moved into a `vitest.config.ts`:
the Angular unit-test builder starts Vitest with `config: false`, explicitly disabling
configuration-file resolution
(`node_modules/@angular/build/src/builders/unit-test/builder.js`, the `startVitest` call), so such
a file would be silently ignored. A bare `ng test` bypasses both the cap and the timeout and is
not a lane.

Two more consequences of the builder, both of which bite when writing specs rather than running
them: jsdom under it has no `matchMedia`, `ResizeObserver`, or `requestIdleCallback`, and
`../src/test-setup.ts` owns a global `afterEach` safety net. Both are documented in
`../README.md`.

### The builder takes `--reporters`, and nothing else about reporting

`npm test -- --reporters=default --reporters=junit` is valid: `reporters` is a real builder
option, passed straight through to Vitest
(`../node_modules/@angular/build/src/builders/unit-test/schema.json:91-97`). **There is no
`outputFile`.** The schema declares none and closes itself with `"additionalProperties": false`
(`:111`), so an added `--outputFile=…` is refused by option validation before a single spec runs
— it is not a slow failure, it is an immediate one. A JUnit reporter therefore writes into
stdout alongside the default reporter, and a report file has to be carved out of the captured
output rather than requested from the builder. Verified against `@angular/build` 20.3.27.

## What `verify-test-gates.mjs` proves

It reads `../angular.json` and the real file inventory — no Angular build, no test run — and
fails with a named problem list. It proves:

- **the configuration set exists**: every one of the ten names above is present and has a
  non-empty `include` array (`verify-test-gates.mjs:17-27, 139-157`);
- **the full gate is total**: `options.include` selects every `src/**/*.spec.ts` in the tree
  (`:167-172`);
- **the primary areas partition it**: each spec belongs to exactly one of `feature-access-admin`,
  `feature-abwab`, `feature-auth`, `feature-dashboard`, `feature-mushaf`, `feature-words`,
  `shared` — zero is a spec no area lane runs, two is a spec run twice (`:8-15, 174-181`);
- **the cross-cuts keep their membership**: every file matching a composition or authorization
  boundary pattern is actually selected by that configuration, and a boundary pattern that
  matches nothing fails rather than passing vacuously (`:29-46, 183-207`);
- **no pattern is dead**: every `include` pattern in the full gate and in all ten
  configurations matches at least one spec, so a renamed folder is caught instead of silently
  narrowing a lane (`:213-217`);
- **no pattern reaches `e2e/`**: no `include` pattern selects a file under `e2e/**/*.e2e.ts`
  (`:218-221`).

It prints the per-configuration selection sizes on the way through. Those numbers are run
evidence — quote them in a report if useful, never freeze one into a document.

**What it does not prove:** its `e2e` inventory collects only `*.e2e.ts` (`:125`), so the leak
check above cannot see a `*.spec.ts` placed under `e2e/`. It does not need to — see below.

## `*.e2e.ts` under `e2e/`, and what actually enforces it

Playwright specs live in `../e2e/` and must be named `*.e2e.ts`. The reason is **not** that the
Vitest gate would collect them: the builder globs its include patterns with `cwd` set to the
project's `sourceRoot` (`src`), so nothing outside `src/` is reachable by any pattern in
`angular.json`
(`node_modules/@angular/build/src/builders/unit-test/builder.js` → `../karma/find-tests.js`,
`glob(..., { cwd: projectSourceRoot })`; verified against `@angular/build` 20.3.27).

The real hazard is the opposite one. `../playwright.config.ts` sets `testDir: './e2e'` and
`testMatch: /.*\.e2e\.ts$/`, so a `*.spec.ts` dropped into `e2e/` is picked up by **nothing** —
not the Vitest gate, which cannot see outside `src/`, and not Playwright, which filters by that
pattern. It would sit in the tree looking like coverage and never run. Name it `*.e2e.ts`.

Browser E2E is opt-in and is never a required gate; see `../e2e/README.md` and
`../../../TESTING_STRATEGY.md` §11.
