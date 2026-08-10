# Sol/Codex Frontend Router

For a Frontend change, begin with the closest relevant README and use
`Frontend/quran-dashboard-ui/README.md` only as the fallback. Read only the specialist sources
whose trigger matches. The root kernel is not repeated here.

| Trigger | Load |
| --- | --- |
| Any Frontend path | The nearest feature, core, shared, or testing README before specialist guidance; `docs/contracts/README.md` only indexes the contract owner. |
| Any UI-visible change (tokens, layout, components, templates, styles) | `Frontend/quran-dashboard-ui/FRONTEND_UI_RULES.md` — the short mandatory rule set (ownership ladder, light-only scope, approved fonts, protected Quran boundary, responsive bands, one gutter, prohibited effects, the five async owners). The permanent visual authority is `Frontend/quran-dashboard-ui/.architecture/golden-ui/`. |
| Structural organization of components, routes, tab/URL state, services, facades/stores/state, data access, or features | `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md`; visual-only edits to an existing component do not trigger this row. |
| Global styles, design tokens, reusable classes, shell/component visuals, themes, or shared UI patterns | Only relevant headings of `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md`; a partial read always includes §7 Typography, §8 RTL and Direction, §13 Quranic Data Display Safety, and only a matching token/component-contract heading when one is actually implicated. Do not substitute unrelated component material. |
| New UI surface, product behavior, user-facing copy, or change in visual direction | `PRODUCT.md` and `DESIGN.md`; ordinary non-product code edits do not require them. |
| API services/data access, `ApiResponse<T>`, DTO/view/state mapping, or API-backed loading, filters, search, and pagination | `Frontend/quran-dashboard-ui/.architecture/API_INTEGRATION_GUIDELINES.md`; add `docs/contracts/security-access.md` for auth, session, or permission behavior. |
| Frontend test selection, execution, or reporting | Read `TESTING_CONSTITUTION.md`, then `Frontend/quran-dashboard-ui/testing/README.md`. |
| Writing or reviewing Frontend tests | Apply that testing constitution, then read `.agents/skills/test-guard/SKILL.md` with `references/jest.md`. |
| A changed Frontend file meets a documented size threshold | `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md` §File Size and Responsibility Guidelines during pre-delivery. |
