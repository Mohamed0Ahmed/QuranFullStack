# Browser E2E (Playwright)

**HOW rules:** `../../../TESTING_STRATEGY.md` §11 — E2E is opt-in and never a required gate.
This file is the WHAT.

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

Unsafe Abwab routes now require a real authorized persona. The older Abwab sandbox fixture still
seeds its local database through anonymous API writes, so those write-oriented specs receive
`401` until an approved E2E authentication/bootstrap mechanism is supplied. Do not weaken Backend
enforcement or fake client authorization to make that fixture pass; the anonymous Phase 9
supplement remains the valid public-read/browser check in the meantime.

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
below). Both share the same `webServer` config and `reuseExistingServer`, so the second
invocation does not re-pay startup cost when the first left the servers up. To run only one
group at a custom worker count: `npx playwright test --project=abwab --workers=1`.

`npm run e2e:headed` and `npm run e2e:ui` do **not** apply the split — both run every project at
the top-level `workers: 2`, which puts the Abwab specs back under the parallelism hazard below.
They are debug commands, not the gate; to debug an Abwab flow specifically, scope both the
project and the worker count yourself: `npx playwright test --project=abwab --workers=1 --headed`.

## Prerequisites

- mkcert certificates in the project root (`mkcert -install && mkcert localhost`) — without
  `localhost.pem` / `localhost-key.pem` the Angular dev server never starts, and Playwright
  reports it only as a port timeout.
- A migrated local `quran_dashboard` database with the DB password in backend user-secrets.
  Nothing migrates on startup.
- `dotnet build Backend/QuranDashboard.sln` first — the backend boots with `--no-build`.

## Invariants

- **Specs are `*.e2e.ts`, never `*.spec.ts`.** Not because Vitest would collect them — the
  Angular unit-test builder globs its `include` patterns with `cwd` at the project's `sourceRoot`
  (`src`), so it cannot see this folder at all. The reason is the opposite: `playwright.config.ts`
  matches only `/.*\.e2e\.ts$/`, so a `.spec.ts` here is run by **nothing** while looking like
  coverage. See `../testing/README.md`.
- **Fresh context per test — never add `storageState` reuse.** `qd-mushaf-reader-session`
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
- **A `Global` reorder's residue reaches outside the sandbox, and that is accepted.** Every root
  in the local dev database gets renumbered by `global_order_value`, sandbox or not — resequencing
  is order-preserving for untouched rows, and teardown removes the sandbox's own roots again
  afterward, so the residue is a permutation of nothing observable. It is still a write outside the
  sandbox's blast radius, same class as the archived-doors residue below, and is accepted on the
  same terms: a local, disposable dev DB, not a shared one.
- **Read-only flows and loose count assertions, except for Abwab.** Every suite but Abwab reads
  the live local dev DB without writing to it; exact row counts would break on the next reseed.
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
  **The residue that remains is archived doors, and it is permanent, not "self-cleaning":**
  there is no hard delete and no section restore, so every run leaves its sandbox doors
  **archived** in the local dev DB forever, and any future restore of one is refused until the
  user names a live destination section, since the one it belonged to is gone. What must **not**
  remain after a run is any live `e2e-sandbox-*` door or any `e2e-sandbox-*` section — either one
  is a teardown bug, not accepted residue. This is accepted on a local dev DB with loose, id-scoped
  assertions; it would need to be revisited (per the same `TESTING_STRATEGY.md` §11 note) before
  this suite runs anywhere but a disposable local database.
- Both servers boot with `reuseExistingServer`, and the backend readiness gate is
  `GET https://localhost:5015/api/health`, which answers 503 when the database is unreachable.
- **Never leave a server running on :4200 or :5015 outside Playwright's control.** A stray
  server is adopted by `reuseExistingServer` and silently poisons the whole run.

## Not this suite

This is not the backend route-smoke gate (`Backend/scripts/test-backend smoke`,
`TESTING_STRATEGY.md` §6), and running it never substitutes for it: that lane is required for
route, contract, auth, middleware, and binding changes, while this suite is not a required gate
at all — see §11.
