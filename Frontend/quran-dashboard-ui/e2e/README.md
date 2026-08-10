# Browser E2E (Playwright)

**Policy:** `../../../TESTING_CONSTITUTION.md`. `../../../TESTING_STRATEGY.md` §11 is only the
transitional command reference until Phase 7 removes it. This file owns the E2E fixtures and runtime
invariants.

Chromium-only browser flow tests over the public browse surfaces: dashboard home, the app-shell
navbar and its dropdown menus, the Mushaf reader (paging, deep link, surah jump, fonts) with the
ayah study — tafsir / translation / full-إعراب / similar-ayahs / متشابهات tabs — and the word
analysis panel, the words hub, the five explorers (roots, lemmas, stems, word types, unique
words), and the placeholder / wildcard-fallback routes — **plus the Abwab doors/sections flows**
(`abwab-structure.e2e.ts`, `abwab-operations.e2e.ts`, `abwab-archive.e2e.ts`,
`abwab-url-and-a11y.e2e.ts`, `abwab-global-order.e2e.ts`, `abwab-relations.e2e.ts`), which are the
one deliberate exception to the read-only invariant below. `abwab-relations.e2e.ts` covers the
mutual relation seen from both doors, dormancy through an archived endpoint and back, the
client-side list cache, and the reveal's retained `relations-<id>-closed` key (restore naming and
reopening the source, Back after a reveal, and the reload case) — the cache claims asserted by
counting relation GETs on a passive `request` listener, never by network idle.
`abwab-operations.e2e.ts` also owns the `qd-context-menu` placement contract, which needs a real
layout engine: jsdom reports zero-sized rects, so inline-start extension and both flips are
browser-only truths.

`abwab-permissions.e2e.ts` is the anonymous Phase 9 supplement: it proves that public Abwab and
template navigation remain available while write controls and a URL-restored create overlay do not,
then sends a handcrafted anonymous write directly to the Backend and expects its `401` envelope.
It does not create a sandbox because denial must leave no data behind.

`fixtures/auth.ts` owns the authenticated E2E persona. Playwright supplies a public JWKS to the API,
keeps the matching private key in the fixture, mints RS256 access and identity-evidence tokens, and
seeds a fresh `angular-auth-oidc-client` session per test. The API trusts that issuer only when its
host environment is exactly `Testing` and the explicit test-issuer flag is enabled. `run-backend.mjs`
keeps the production Logto Management client in composition and serves its `e2e-*` profiles from a
local stub. The fixture provisions identities through `/api/access/me`, uses an active test Owner to
assign the exact direct grant through the real access-administration endpoints, then removes the grant
and disables the persona during teardown. The Owner and its audit history exist only in the disposable
database clone, which is dropped with the backend process. No authentication or authorization handler
is replaced or bypassed.

The older Abwab sandbox fixture still seeds through anonymous API writes, so its write-oriented
specs continue to receive `401` until they adopt the authenticated persona in the later auth-journey
phase. The anonymous permission supplement remains the valid public-read and denial check meanwhile.

`abwab-tree-row-budget.e2e.ts`, like the one below it, measures rather than drives: it pins the
tree row's height budget, which needs a real layout engine.

`abwab-slice-j-widths.e2e.ts` is the odd one out: it asserts **measured modal geometry** — the
`.qd-modal--wide` 52rem step and the widths of the modals that deliberately did not adopt it
(`UI_STYLE_SYSTEM.md` §17's ladder) — across three viewports and both themes. It lives here
rather than in a unit spec because a computed width needs a real layout engine. It carries the
`abwab-` prefix to inherit the serial worker, since it creates a sandbox door.

## Commands

```bash
npm run e2e                                        # headless (the gate) — two sequential runs, see below
npm run e2e:headed                                 # visible browser
npm run e2e:ui                                      # Playwright UI mode
npx playwright test e2e/mushaf-reader.e2e.ts        # one flow file, any worker count
npm run e2e:typecheck                               # tsc over e2e/ + playwright.config.ts
```

`npm run e2e` runs two Playwright projects in sequence: `default` (every non-Abwab spec, 2
workers) then `abwab` (every `abwab-*.e2e.ts` spec, 1 worker — see the parallelism note
below). Each invocation owns a fresh backend process and disposable database clone; only the
frontend may be reused. To run only one group at a custom worker count:
`npx playwright test --project=abwab --workers=1`.

`npm run e2e:headed` and `npm run e2e:ui` do **not** apply the split — both run every project at
the top-level `workers: 2`, which puts the Abwab specs back under the parallelism hazard below.
They are debug commands, not the gate; to debug an Abwab flow specifically, scope both the
project and the worker count yourself: `npx playwright test --project=abwab --workers=1 --headed`.

## Prerequisites

- mkcert certificates in the project root (`mkcert -install && mkcert localhost`) — without
  `localhost.pem` / `localhost-key.pem` the Angular dev server never starts, and Playwright
  reports it only as a port timeout.
- A migrated local `quran_dashboard` source database with its connection string in backend
  user-secrets or `ConnectionStrings__QuranDashboardDb`. `createdb`, `dropdb`, `pg_dump`, and
  `pg_restore` must be available. The Playwright-owned backend wrapper refuses non-local PostgreSQL,
  copies the source into a uniquely named disposable database, and drops that clone on shutdown.
  Nothing migrates on startup and the source database is never the E2E write target.
- `dotnet build Backend/QuranDashboard.sln` first — the backend boots with `--no-build`.

## Invariants

- **Specs are `*.e2e.ts`, never `*.spec.ts`.** Not because Vitest would collect them — the
  Angular unit-test builder globs its `include` patterns with `cwd` at the project's `sourceRoot`
  (`src`), so it cannot see this folder at all. The reason is the opposite: `playwright.config.ts`
  matches only `/.*\.e2e\.ts$/`, so a `.spec.ts` here is run by **nothing** while looking like
  coverage. See `../testing/README.md`.
- **Fresh context per test — never add `storageState` reuse.** The auth fixture seeds only its own
  new context and tears its server-side direct grant down after the test; the wrapper drops the
  disposable database, including the temporary Owner, when the backend exits. `qd-mushaf-reader-session`
  (sessionStorage) restores the last reader page on a bare entry, and `qd-theme` decides the
  theme; leaking either between tests makes results order-dependent.
- **Zero external network calls.** `fixtures/app-test.ts` stubs the Logto origin and fails any
  test whose browser context talked to a non-localhost host.
- **The Abwab specs run single-worker, in their own `abwab` Playwright project.** A `Global`
  reorder resequences `global_order_value` on **every live root in the database**, not just the
  sandbox's own doors — so two Abwab specs racing in different workers can both hold a version of
  a root the other worker's write (or teardown archive) just invalidated, and the second one either
  `409`s or, worse, computes a target position against a live-root list that changed underneath it
  between the read and the write. Retry-on-409 is not the fix: this feature's own policy is that
  `409`s are always surfaced, never swallowed or auto-retried (`features/abwab/README.md`).
  Measured directly: at the default 2 workers, `abwab-global-order.e2e.ts` failed on a
  wrong-position assertion (not even a `409` — a silently different result) after another worker's
  teardown resequenced the global order mid-test; at 1 worker, the Abwab project passes repeatably.
  `playwright.config.ts` therefore splits `projects` into `default` (`testIgnore` on
  `abwab-*.e2e.ts`, 2 workers) and `abwab` (`testMatch` on `abwab-*.e2e.ts`, 1 worker), and
  `npm run e2e` runs both in sequence. This costs real wall-clock time — the previously parallel
  full gate is now two sequential projects — in exchange for not shipping a suite that can pass or
  fail depending on scheduling luck. **Measured 2026-08-02:** 68 passed — `default` 28 in
  ~59 s, `abwab` 40 in ~2.6 m.
- **A `Global` reorder reaches outside the sandbox but never outside the disposable clone.** Every
  live root in that clone gets renumbered by `global_order_value`, sandbox or not. The source database
  remains untouched, and dropping the clone removes the resequencing and archived-door residue.
- **Read-only flows and loose count assertions, except for Abwab.** Every suite but Abwab reads
  the source snapshot in the disposable clone without writing to it; exact row counts would break on
  the next source reseed.
  The Abwab specs are a deliberate, named exception (`TESTING_STRATEGY.md` §11, which this
  amendment mirrors; the slice plan that first recorded it is in git history): each test creates its own
  uniquely-named sandbox section over the API (`fixtures/abwab.ts`) and drives real writes
  against it through the UI. Teardown archives **every live door in that section** — swept from
  the tree by `sectionId`, not just the ids the fixture handed out, because flows also create
  doors through the UI — and then deletes the now-empty section. That order is forced, not
  stylistic: section delete `409`s while live doors remain. Each archive re-reads the door's
  current version first, since every write resequences the scope and bumps its siblings'
  `xmin`; archiving from one up-front snapshot succeeds once and then `409`s silently for the
  rest, which is what used to leave live sandbox doors and undeleted sandbox sections behind.
  Teardown is best-effort and never masks a test's own failure (R19), and no Abwab test asserts
  a global count — each one only ever asserts on the ids its own sandbox produced (R18).
  Archived doors can remain inside the clone after per-test teardown because the feature has no hard
  delete. They disappear when the wrapper drops the clone. What must not remain during a run is any
  live `e2e-sandbox-*` door or `e2e-sandbox-*` section after its test teardown; either one is still a
  teardown bug.
- The frontend may reuse an existing server. The backend never does: Playwright must own its Testing
  process, disposable database, and shutdown. Its readiness gate is `GET https://localhost:5015/api/health`,
  which answers 503 when the database is unreachable.
- **Never leave a backend running on :5015 outside Playwright's control.** Backend reuse is disabled,
  so a stray process makes startup fail instead of being adopted. The frontend on :4200 may be reused
  deliberately.

## Not this suite

This is not the backend route-smoke gate (`Backend/scripts/test-backend smoke`,
`TESTING_STRATEGY.md` §6), and running it never substitutes for a separately selected route-smoke
lane. Route-smoke and browser-journey selection both follow the testing constitution and the active
plan's `Testing Decision`.
