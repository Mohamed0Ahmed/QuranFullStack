# Frontend Test Harness Constraints

Project-specific constraints of the Angular unit-test builder + Vitest + jsdom harness
that keep getting rediscovered. Lane selection is owned by `TESTING_STRATEGY.md` §4; the
named configurations, the mandatory fork cap, and why a bare `ng test` is not a lane are
owned by `Frontend/quran-dashboard-ui/testing/README.md`; the jsdom API gaps and the global
`afterEach` safety net are documented in `Frontend/quran-dashboard-ui/README.md`. This file
keeps only what those do not own.

## `--run` is not supported here

Do not pass `--run` to `ng test` invocations: the `@angular/build:unit-test` builder drives
Vitest itself, and `--run` is a raw-Vitest flag, not a supported builder option.

## A harness timeout is not a test result

The full suite is worker/memory sensitive. If a full run through a constrained shell times
out or reports a hang, do not declare the tests failing on that basis — retry outside the
constrained shell, or split into narrower named lanes, before concluding anything.

## Distinguish jsdom limits from real bugs

jsdom does not lay out or paint: `matchMedia`, `ResizeObserver`, `IntersectionObserver`
are absent, and element geometry (`getBoundingClientRect`, scroll positions) is not real —
CDK virtual scroll needs real viewport measurement. Feature code probes before use and
picks a spec-meaningful default (desktop layout; a non-virtual fallback whose rows are
assertable). Before flagging behavior as broken, confirm the code path is genuinely wrong
rather than merely unexercised under jsdom; for performance or visual claims, recommend a
real-browser measurement instead of asserting a runtime problem from test-environment
behavior.

## Known safe patterns — preserve, do not "tidy" away

Reverting these reintroduces the bug they fixed; treat a proposal to "simplify" them as a
regression risk.

- **Words-feature label getters (TDZ workaround).** Label modules in the `words` feature
  participate in a circular import via routing's `rootsRoutePath`. Reading those
  cross-module label consts from a class **field initializer** can hit the temporal dead
  zone in the Vitest SSR bundle — the field captures `undefined` and template bindings
  render empty in specs. Read them via a **getter** instead, as
  `roots-table.component.ts`, `word-drilldown-modal.component.ts`, and
  `word-type-filter.component.ts` do. Do not convert these getters back to `readonly`
  fields; verify a suspected regression with the narrowest covering lane
  (`npm run test:feature:words`).
- **matchMedia/ResizeObserver guards + desktop default** (above) — keep the guards and the
  default; they are what keeps responsive and virtual-scroll components testable.
