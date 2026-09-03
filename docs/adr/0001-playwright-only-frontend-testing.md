---
status: accepted
date: 2026-08-30
---

# Playwright-only Frontend testing with Chromium

Quran Dashboard uses Playwright as the Angular Frontend's only automated test convention. Frontend
behavior is protected through focused risk-based browser journeys against the real API and PostgreSQL,
while exhaustive business, persistence, HTTP, and security permutations remain in the owning Backend
test layers. Angular unit-test generation stays disabled, `src/**/*.spec.ts` remains prohibited, and no
Karma, Jest, Vitest, or parallel Frontend test convention is introduced.

Required browser coverage uses Chromium desktop plus mobile emulation only where responsive risk
justifies it. Firefox and WebKit lanes are intentionally excluded. This keeps the suite centered on
Arabic-first RTL user journeys, deterministic cross-stack behavior, and the hard 12-minute PR feedback
budget instead of multiplying the same scenarios across runners and browsers.

This choice trades cheap isolated component tests and broad browser-engine coverage for a single,
cohesive convention. It therefore requires compact source-traceable fixtures, hermetic execution,
semantic selectors, independent outcome verification, lower-layer Backend tests, and a small critical
journey portfolio. Playwright tests must not become exhaustive workflow scripts or substitutes for
Backend invariants.

Reconsider this decision only when evidence shows that a recurring high-risk Frontend behavior cannot
be protected faithfully and economically through Playwright and the owning Backend seam, when supported
product requirements add another browser engine, or when measured execution cost makes the policy
incompatible with the approved feedback budget. Reconsideration requires an explicit replacement or
superseding ADR; adding a second convention ad hoc is not allowed.

The complete journey, gate, and artifact policy is defined in the
[Risk-Based Testing Strategy](../testing/risk-based-strategy.md).

## Database lifecycle supersession

[ADR 0002](./0002-persistent-full-data-test-database.md) supersedes this decision's compact-fixture and
hermetic-database requirements and changes the hard 12-minute gate into a measured active-gate target.
The Playwright-only convention, Chromium scope, risk-based journey selection, independent oracle
requirement, semantic selectors, and Backend ownership of exhaustive business and persistence coverage
remain accepted.
