# Sol/Codex Frontend Router

For a Frontend change, read only the specialist sources whose trigger matches. Active Spec Kit
artifacts own feature intent; code owns implemented truth. The project README is operational and
does not replace the triggered architecture sources. The root kernel is not repeated here.

| Trigger | Load |
| --- | --- |
| Any Frontend path | The code in scope and, for phase-bound work, the active Spec Kit artifacts; `docs/contracts/README.md` only indexes code and architecture authorities. |
| Any UI-visible change (tokens, layout, components, templates, styles) | Follow the owner's explicit direction and the active feature scope. Preserve Arabic-first RTL behavior, accessibility, responsive behavior, and protected Quran rendering. No permanent visual rule set is active during the UI rebuild. |
| Structural organization of components, routes, tab/URL state, services, facades/stores/state, data access, or features | `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md`; visual-only edits to an existing component do not trigger this row. |
| Global styles, design tokens, reusable classes, shell/component visuals, themes, or shared UI patterns | Read the implementation in scope and reuse existing shared primitives where they still fit. Do not establish a permanent visual authority until the UI rebuild is complete. |
| New UI surface, product behavior, user-facing copy, or change in visual direction | `PRODUCT.md` for users and product purpose, plus the owner's explicit visual direction. Ordinary non-product code edits do not require it. |
| API services/data access, `ApiResponse<T>`, DTO/view/state mapping, or API-backed loading, filters, search, and pagination | `Frontend/quran-dashboard-ui/.architecture/API_INTEGRATION_GUIDELINES.md`; add `docs/contracts/security-access.md` for auth, session, or permission behavior. |
| Frontend verification | Read `TESTING_CONSTITUTION.md`; run `npm run check:no-unit-specs`, `npm run typecheck:app`, and `npm run build:verify` as three independent commands. |
| Writing or reviewing approved Playwright tests | Read `TESTING_CONSTITUTION.md`, then `.agents/skills/test-guard/SKILL.md`. |
| A changed Frontend file meets a documented size threshold | `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md` §File Size and Responsibility Guidelines during pre-delivery. |
