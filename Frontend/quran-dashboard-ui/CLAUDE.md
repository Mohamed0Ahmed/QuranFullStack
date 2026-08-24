# Claude Frontend Router

For any Frontend change, load only the triggered sources below. Active Spec Kit artifacts own
feature intent; code owns implemented truth. The project README is operational and does not
replace the triggered architecture sources. This file routes Frontend work and does not repeat
the root kernel.

| Trigger | Load |
| --- | --- |
| Any Frontend path | The code in scope and, for phase-bound work, the active Spec Kit artifacts; use `docs/contracts/README.md` only to locate code and architecture authorities. |
| Any UI-visible change (tokens, layout, components, templates, styles) | Follow the owner's explicit direction and the active feature scope. Preserve Arabic-first RTL behavior, accessibility, responsive behavior, and protected Quran rendering. No permanent visual rule set is active during the UI rebuild. |
| Structural organization of components, routes, tab/URL-state, services, facades/stores/state, data access, or features | `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md`; visual-only edits to an existing component do not trigger this row. |
| Global styles, design tokens, reusable classes, shell/component visuals, themes, or shared UI patterns | Read the implementation in scope and reuse existing shared primitives where they still fit. Do not establish a permanent visual authority until the UI rebuild is complete. |
| New UI surface, product behavior, user-facing copy, or visual-direction change | `PRODUCT.md` for users and product purpose, plus the owner's explicit visual direction. Routine non-product code edits do not load it. |
| Frontend API service, data access, `ApiResponse<T>`, DTO/view/state mapping, or API-backed loading/filter/search work | `Frontend/quran-dashboard-ui/.architecture/API_INTEGRATION_GUIDELINES.md`; add `docs/contracts/security-access.md` for auth/session/permission behavior. |
| Frontend verification | Read `TESTING_CONSTITUTION.md`; run `npm run check:no-unit-specs`, `npm run typecheck:app`, and `npm run build:verify` as three independent commands. |
| Write or review approved Playwright tests | Read `TESTING_CONSTITUTION.md`, then `.claude/skills/test-guard/SKILL.md`. |
| A changed Frontend file reaches a documented size threshold | `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md` §File Size and Responsibility Guidelines at pre-delivery. |
