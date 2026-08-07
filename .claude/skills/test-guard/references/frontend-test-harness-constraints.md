# Frontend Test Harness Constraints (Shared Reference)

Single source of truth for how the Quran Dashboard Angular frontend tests actually run,
and the traps that keep getting rediscovered. Consumed by `test-guard`,
`performance-angular-review`, and `engineering-review` (frontend test-execution
findings). The point is to stop re-learning the same environment facts on every review:
the frontend suite runs on Angular's Vitest builder over **jsdom**, which behaves
differently from both a plain Vitest project and a real browser.

## Running focused Angular specs

Run a named lane. `TESTING_STRATEGY.md` §4 lists them; each is an `npm run test:*` script that
delegates to `npm test -- --configuration=<name>`, and `npm test` bakes in the worker cap:

```
npm run test:fast
npm run test:feature:abwab      # …:auth, …:dashboard, …:mushaf, …:words
npm run test:authorization
npm run test:composition
npm run test:shared
npm run test:full
```

- **A lane, not a glob.** The configurations live in `angular.json` and are validated by
  `testing/verify-test-gates.mjs` (`npm run test:gates`), which checks that every spec file
  falls in exactly one primary area and that no include pattern is dead. An ad-hoc
  `npm test -- --include=<glob>` is neither validated nor reportable by name, so it is not
  acceptable as gate evidence.
- **Do not use `--run`** for Angular CLI test invocations in this project. The Angular
  `@angular/build:unit-test` builder drives Vitest itself; `--run` is a raw-Vitest flag
  and is not the supported way to invoke a one-shot run here.
- Invoking `ng test` / `npx ng test` directly bypasses the cap, because the cap is in the
  `test` npm script, not in `angular.json`. If you must, prefix it yourself:
  `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2 npx ng test --configuration=<name>`.

**Why the fork cap is not optional:** the builder starts Vitest with `config: false`, so a
`vitest.config.ts` is ignored and `angular.json` cannot carry `poolOptions`
(`additionalProperties: false`). The only lever is the `VITEST_MIN_FORKS` /
`VITEST_MAX_FORKS` env vars, which Vitest applies at config resolution even with
`config: false`. Uncapped, the runner spawns one `forks` worker per CPU core, each loading
jsdom plus the built app, which exhausts memory and freezes/crashes the machine. Keep the
cap on every `ng test` invocation; raising it much past `2`–`3` risks re-OOMing.

## Full-suite runs are worker/memory sensitive

The full suite is heavier than a focused run. If a full run through LeanCTX / `ctx_shell`
times out or the tool reports a hang, **do not declare the tests failing on that basis**.
Retry outside LeanCTX, or split into the narrower named lanes, before concluding
anything. A harness timeout is a runner/memory-pressure signal, not a test result.

## jsdom is not a real browser

The unit-test environment is jsdom (via `@angular/build:unit-test` + Vitest); `test-setup.ts`
imports only `zone.js/testing` with no browser polyfills. Several browser APIs are simply
absent or inert, and component code that touches them will throw or silently never fire
under test. Watch for:

- `window.matchMedia` — not defined.
- `ResizeObserver` and `IntersectionObserver` — not defined.
- Layout measurement — element sizes, scroll positions, and `getBoundingClientRect`
  geometry are not real; jsdom does not lay out or paint.
- Angular CDK behavior that depends on the above — notably virtual scroll / `cdk-virtual-scroll`,
  which needs real viewport measurement.

**Guard, don't assume.** Feature code should probe before use — e.g.
`typeof window?.matchMedia === 'function'`, `typeof ResizeObserver !== 'undefined'` — and
pick a default that keeps specs meaningful. For responsive layout, default to **desktop**
(e.g. `isDesktop = signal(true)`) so a real branch renders in jsdom; a virtual-scroll
component should have a non-virtual fallback, which is also what makes its rows assertable.

## Distinguish test-environment limits from real bugs

A missing browser API under jsdom is an environment limitation, **not** evidence of an
application defect on its own. Before flagging behavior as broken:

- Confirm the code path is genuinely wrong, not just unexercised because jsdom lacks the API.
- For performance / visual review, separate "this can't be measured under jsdom" from "this
  is slow or wrong in a real browser." Recommend a real-browser measurement (Angular
  DevTools profiler, a bundle-stats report, manual check) rather than asserting a runtime
  problem from test-environment behavior.

## Known safe patterns — preserve, do not "tidy" away

These exist to work around real harness behavior. Reverting them reintroduces the bug they
fixed, so treat a proposal to "simplify" them as a regression risk, not cleanup.

- **Words-feature label getters (TDZ workaround).** In the `words` feature, label modules
  participate in a circular import (via routing's `rootsRoutePath`). Reading those
  cross-module label consts from an Angular **class field initializer**
  (`protected readonly x = SOME_LABEL;`) can hit the temporal dead zone in the Vitest SSR
  test bundle while the labels module is still wiring its routing dependency — the field
  captures `undefined` and template bindings render empty (e.g. an `aria-label` comes back
  `null` in a spec). The fix is to read them via a getter
  (`protected get x() { return SOME_LABEL; }`), which runs at template-render time after
  module init completes. Components already do this (`roots-table.component.ts`,
  `word-drilldown-modal.component.ts`, `word-type-filter.component.ts`). Do **not** convert
  these getters back to `readonly` fields.
- **matchMedia / ResizeObserver guards + desktop default** (see the jsdom section above) —
  keep the guards and the desktop-default so responsive and virtual-scroll components stay
  testable.

Verify a suspected regression in these areas with the narrowest lane covering them, e.g.:

```
npm run test:feature:words
```
