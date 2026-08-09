# Claude Frontend Router

For any Frontend change, read the nearest relevant README first, falling back to
`Frontend/quran-dashboard-ui/README.md`. Then load only the triggered sources below. This file
routes Frontend work and does not repeat the root kernel.

| Trigger | Load |
| --- | --- |
| Any Frontend path | The nearest feature/core/shared/testing README before a specialist document; use `docs/contracts/README.md` only to locate the owning contract. |
| Structural organization of components, routes, tab/URL-state, services, facades/stores/state, data access, or features | `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md`; visual-only edits to an existing component do not trigger this row. |
| Global styles, design tokens, reusable classes, shell/component visuals, themes, or shared UI patterns | Only relevant headings of `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md`; a partial read always includes §7 Typography, §8 RTL and Direction, §13 Quranic Data Display Safety, and only a matching token/component-contract heading when one is actually implicated. Do not substitute unrelated component material. |
| New UI surface, product behavior, user-facing copy, or visual-direction change | `PRODUCT.md` and `DESIGN.md`. Routine non-product code edits do not load them. |
| Frontend API service, data access, `ApiResponse<T>`, DTO/view/state mapping, or API-backed loading/filter/search work | `Frontend/quran-dashboard-ui/.architecture/API_INTEGRATION_GUIDELINES.md`; add `docs/contracts/security-access.md` for auth/session/permission behavior. |
| Select, run, or report Frontend tests | `TESTING_STRATEGY.md` §§1–2, §4, and §5; add §§7–10 for build/no-CI/failure/workflow ownership and §11 for browser E2E. Read `Frontend/quran-dashboard-ui/testing/README.md`. |
| Write or review Frontend tests | `.claude/skills/test-guard/SKILL.md` and `references/jest.md`. |
| A changed Frontend file reaches a documented size threshold | `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md` §File Size and Responsibility Guidelines at pre-delivery. |
