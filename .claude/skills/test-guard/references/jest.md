# Test Guard — Angular / TypeScript / Vitest Patterns

Concrete applications of the nine rules for this project's Angular specs. They run on the
Angular unit-test builder with Vitest over jsdom; when a finding depends on how that
harness behaves, see
[frontend-test-harness-constraints.md](frontend-test-harness-constraints.md).

## Rule 2: Mock only true boundaries

Justified mock targets:

- **HTTP:** Angular's HTTP testing support (`provideHttpClientTesting` +
  `HttpTestingController`) — mock at the transport boundary, not your own API service.
- **Timers and randomness:** `vi.useFakeTimers()`, seeded/injected values.
- **Browser APIs jsdom lacks** (`matchMedia`, `ResizeObserver`) — via the project's
  existing guard-and-default patterns, not ad-hoc globals.

Unjustified:

- `vi.mock('../…')` on the project's own modules to isolate a "unit" (Rule 2).
- Patching a component's private methods or spying on internal calls (Rule 1).
- Hand-built object literals pretending to be DTOs/view models/state objects when the real
  type or a small factory exists (Rule 8). Facades, stores, caches, and state services are
  real objects — construct them; mock a facade in a child-component spec only when it is a
  genuine input/output boundary for that component.

When you mock a boundary, assert what the code **does with the response**, not that the
mock received specific arguments.

## Rule 3: Data-driven variants

Use `test.each` / `it.each` when specs share setup and differ only in input/output values.
Keep specs separate when setup, assertions, or boundary mocks genuinely differ.

## Rules 1 + 7: Behavior, not the framework

Assert what the caller or user observes — rendered DOM text/attributes/roles, emitted
outputs, signal and observable values, router/URL effects — not private fields or wiring.
Do not re-test Angular guarantees: that inputs bind, routes resolve, change detection runs,
or DI provides. A spec that would still pass with the project's logic deleted proves
nothing.

## Snapshot discipline

A snapshot is justified only when the snapshot *is* the contract (a public JSON shape, CLI
help text). Avoid full component-tree snapshots (brittle, Rule 1) and large unreviewed
snapshots (Rule 4); prefer targeted assertions by role/label/text.
