# Slice H — Navbar (UX audit)

Source: `docs/abwab-ux-audit.md` "Slice H — Navbar" (`:1128-1131`) — item 22 (`:890-932`), the
audit's fourth recorded reversal: «الأبواب» becomes a hover dropdown («الرئيسية» / «قوالب
الأبواب» / «الأرشيف»), built by generalizing the words dropdown into a data-driven
`NavItem.children` branch rather than adding a second hard-coded special case. One item,
frontend only, no route and no backend surface. The commission widens the audit's mobile note
(`:929-932`) into scope by user decision: the flat mobile list gains **all** children — the
words children (absent today) and the new abwab children.

**Mode when this plan was written:** plan-only. No code, no docs, no Git action. Everything
below is scheduled, nothing is done.

**Slice G status at plan time:** merged. `ux-slice-g` merged into `dev`; ancestry checked at
plan time — the slice-G commits (`00a17261`…`50b7eef5`) are on `dev`, and the workshop tree's
three menu paths plus the children-only apply are present at the tip. This plan is measured
against `dev` (`50b7eef5`, clean). **The G-DEPENDENT fact list is empty** — nothing in Slice H
consumes a Slice G primitive. Slice G touched the templates feature; this slice touches
`core/navigation/` and `core/layout/` and composes nothing G built. One G-adjacent fact is
consumed as *data*, not mechanism: `/abwab/templates` and its shipped title string predate G
and are what the new nav children point at.

**Audit line references are stale and are superseded by this table.** Item 22 cites
`top-navbar.component.html:8-61`/`:62-74`/`:286-300` and `.ts:29-31,82-94,45-56,58-71,134-141`;
the file has shifted since (Slice B2 landed the inert/sticky chrome work). Every claim below
carries the line numbers measured at `50b7eef5`, and those are the ones that bind.

## Precondition — VERIFIED on `dev` (`50b7eef5`, clean) at plan time

| Consumed primitive / mechanism | Where it lives | Verified |
|---|---|---|
| Slices A–G merged to `dev` | `dev` tip `50b7eef5` | ✅ |
| **The recorded decision this slice reverses** — *"…adding one would put an item in the nav nobody asked for"* | `features/abwab/abwab.routes.ts:19-21` | ✅ amended here (§5.3), not litigated |
| … the mechanical fact beside it: `navLabel` **throws** on a key `NAV_ITEMS` does not carry | `core/navigation/route-paths.ts:3-9` (`navItem` throws `Unknown navigation key`), `navLabel` at `:15-17` — verified by reading the implementation, not the comment | ✅ **and it stays true for the children**, because the children never enter `NAV_ITEMS` (§4.2-1); `navLabel`'s domain does not change |
| … the two call-sites the comment governs | `abwab.routes.ts:16` (`navLabel('abwab')` — key exists, fine), `:24` (`ABWAB_LABELS.templatesPageTitle` — **kept**, `navLabel` still cannot serve it) | ✅ both unchanged in behavior |
| `NavItem` — `{ key, labelAr, labelEn, route, group }`, `group` required | `core/navigation/nav-items.ts:1-7` | ✅ gains `children?`, `queryParams?` (§4.2-1) |
| `NAV_ITEMS` — 11 flat entries; `abwab` at `:14` (`route: '/abwab'`, `group: 'primary'`); the file imports **nothing** | `nav-items.ts:9-21` | ✅ entries untouched; only the interface grows |
| `WORDS_MENU_ITEMS` — 6 `{ labelAr, route }` entries; routes from `route-paths` **functions**; labels "owned here in core" by recorded comment | `core/navigation/words-nav-items.ts:15-24`, comment `:15-16` | ✅ retyped to `NavItem[]` (§4.2-2); the labels-owned-in-core rule is the precedent §4.2-6 copies |
| … its only consumer is the navbar | grep: `WORDS_MENU_ITEMS` appears in `words-nav-items.ts` and `top-navbar.component.ts:8,38` only | ✅ safe to reshape; `WordsNavItem` has no other consumer either |
| **`route-paths.ts` imports `NAV_ITEMS` and derives every route constant from it** | `route-paths.ts:1,19-22` (`navRoute('dashboard')` etc. run at module init) | ✅ **this is why the children cannot live in `nav-items.ts`** — DRIFT-1 |
| The complete `NAV_ITEMS` consumer census (grep, not assumption) | `top-navbar.component.ts:6,32-35`; `route-paths.ts:1,4`; `app.routes.ts:2,10-13`; `route-paths.spec.ts:3,15-16`; `words.routes.spec.ts:8,103-112`; `abwab.routes.spec.ts` (via `navLabel`, `:9,21`); `abwab.routes.ts:3,16` | ✅ every consumer reads top-level `key`/`route`/`labelAr`/`group` only — an **optional** `children` field breaks none of them (stop condition 2 checked and clear) |
| The hard-coded words branch this slice replaces | `top-navbar.component.html:14-67` (`@if (item.key === 'words')`), generic `@else` at `:68-80` | ✅ |
| … the words trigger's contract: `id="words-menu"`, `data-testid="nav-words-trigger"`, `aria-haspopup`/`aria-controls`/`[attr.aria-expanded]`, the chevron svg | `.html:26-29,51`, chevron `:32-48` | ✅ all preserved byte-for-byte by the generic branch's `item.key` interpolation (§4.2-3) |
| … hover-open / hover-close on the `<li>`, click-toggle on the button | `.html:18-19` (`mouseenter`/`mouseleave`), `:25` | ✅ behavior copied to the generic branch; the **more** menu keeps click-only (§4.2-4) |
| … the words children's active options: `[routerLinkActiveOptions]="{ exact: sub.route === wordsHubRoute }"` — the **deprecated boolean shorthand**, not the object form | `.html:58` | ✅ **the shorthand is sufficient for the query-param entry too** — DRIFT-2, §5.2 |
| … the words children's `@for` tracks `sub.route` | `.html:52` | ✅ **cannot be generalized as-is** — two abwab children share `route: '/abwab'` — DRIFT-3 |
| `wordsOpen` / `moreOpen` / `mobileOpen` and the pairwise mutual exclusion | `top-navbar.component.ts:41-43`; `toggleMore` clears `wordsOpen` (`:81-84`), `toggleWords`/`openWords` clear `moreOpen` (`:90-98`) | ✅ collapsed into `openMenuKey` (§4.2-5) |
| … Escape closes every open surface in one press | `.ts:53-64` (`@HostListener('document:keydown.escape')`) | ✅ preserved: one press still closes the open dropdown *and* the mobile panel |
| … outside-click dismissal, keyed on the `.more-dropdown` / `.words-dropdown` **class selectors** | `.ts:66-79` | ✅ generalized to one selector (§4.2-5); grep confirms no test or e2e selects either class |
| … `closeWords` clears only `wordsOpen` (a `mouseleave` after moving to the other menu must not close it) | `.ts:100-102` | ✅ the generalized `closeMenu(key)` clears only if `openMenuKey === key` |
| … the active-parent state: `isWordsActive()` = `router.isActive(wordsHubRoute, { paths: 'subset', queryParams: 'ignored', … })` | `.ts:142-149` | ✅ generalized to `isMenuActive(item)` — for abwab this is exactly the "parent lights on any `/abwab*` URL" rule (§6a) |
| … `isMoreActive()` (per-item `paths: 'exact'` over the more group) | `.ts:131-140` | ✅ untouched |
| The generic `@else` link renders `data-testid="nav-link--abwab"` today | `.html:75` | ✅ disappears when abwab becomes a trigger; grep: **nothing** selects `nav-link--abwab` (only `nav-link--mushaf` in `shell-nav.e2e.ts:6`) |
| The flat mobile list: `allItems` → one link per top-level item, `{ exact: item.route === '/dashboard' }`, **no children anywhere** | `.html:292-306`; `allItems` at `.ts:32` | ✅ the second gap decision 5 closes; parent rows' options untouched |
| The dropdown/mobile z-story: `.dropdown-menu` and `.mobile-menu` sit on `--qd-z-mobile-nav` (45), below `--qd-z-menu-backdrop`/`--qd-z-modal-backdrop` (49/50) | `top-navbar.component.scss:72-87` (comment `:76-78`), `:148-154`; scale at `src/styles/_tokens.scss:213-220` | ✅ **no z change is owed or scheduled** — a new dropdown on the same `.dropdown-menu` class inherits the rung |
| … and the navbar goes `inert` while any modal holds the scroll lock, so an open dropdown cannot fight a dialog for input at all | `top-navbar.component.ts:12-16,30`, `.html:5-6`; `UI_STYLE_SYSTEM.md` §17 "Chrome-inert rule" (`:1286`) | ✅ verify-only in the browser walk (T501) |
| §17 entries governing this surface: "Sticky app chrome" (`:1215`), "Chrome-inert rule" (`:1286`), the §4 z-scale (`:169-182`); §5C navbar visual rules (`:473-485`) | `.architecture/UI_STYLE_SYSTEM.md` | ✅ **all verify-only** — no primitive is added or changed; there is no §17 navbar-dropdown entry today and this plan does not add one (single-consumer pattern, not a shared primitive) |
| The archive view is the `archive=1` query param on `/abwab`, not a route | `features/abwab/README.md:272-280` (URL-contract table; `archive` row at `:276`) | ✅ the «الأرشيف» entry is `routerLink /abwab` + `queryParams {archive:'1'}` |
| … `q` still filters the archive tree, so `/abwab?archive=1&q=…` is a real URL | `features/abwab/README.md:61-63` | ✅ its active-state cell is stated, not discovered later (§6a row 3) |
| … `/abwab/templates` carries no URL state at all, by recorded decision | `features/abwab/README.md:343-351` | ✅ the «قوالب الأبواب» entry needs no query handling |
| The shipped templates label: `templatesPageTitle: 'قوالب الأبواب'` — the route title of `/abwab/templates` | `features/abwab/models/abwab.labels.ts:308`; consumed at `abwab.routes.ts:24` | ✅ the child's label **is this string**, not an invented variant (§4.2-6); the doors-header button says «القوالب» (`features/abwab/README.md:616`) and is untouched |
| Angular's boolean `routerLinkActiveOptions` expansion: `true` ⇒ `{paths:'exact', queryParams:'exact', fragment:'ignored', matrixParams:'ignored'}`; `false` ⇒ all-`subset` | `node_modules/@angular/router/router_module.d.d.ts:3293-3296`; option domains at `:101-128` | ✅ read from the installed types, not from memory — the load-bearing fact of §5.2 |
| **No unit spec exists for the navbar** — `core/layout/top-navbar/` is 3 files, no `.spec.ts`; no app-level spec mounts or asserts it (grep `TopNavbar|navbar` over `src/app/**/*.spec.ts`: zero hits) | `core/layout/top-navbar/` | ✅ measured — the most-seen surface in the app is pinned by nothing but three opt-in e2e flows; drives §7 |
| What pins it instead: `shell-nav.e2e.ts` — 3 flows: `nav-link--mushaf` click, words **hover** + «الرئيسية» click scoped to `#words-menu`, more-menu `nav-menu-link--mutashabihat` | `e2e/shell-nav.e2e.ts:3-32`; the hover-not-click comment at `:15-16` | ✅ all three survive the generalization **if** §4.2-3's contract holds; the `#words-menu` scoping is what keeps the new abwab «الرئيسية» from colliding with the locator (risk 4) |
| E2E is opt-in and never a tier | `TESTING_STRATEGY.md` §6 (`:415+`), `Frontend/quran-dashboard-ui/CLAUDE.md` | ✅ if run, it is evidence |
| Tier B triggers on `core/` / app-shell changes; the validated frontend commands: full suite `npm test` (191 files / 2,161 tests, ~205 s, fork cap baked in), `npm run build` | `TESTING_STRATEGY.md` §3 Tier B, §6 | ✅ the gates of §7 |
| **No backend surface** ⇒ no route-smoke tier, no `SmokeRouteCatalog` entry, no `dotnet` gate | `TESTING_STRATEGY.md` §4 row "API endpoint added/changed" — not triggered; no `Backend/` file is in scope | ✅ **stated so its absence is not read as an omission** (§4.1-8) |
| `TESTING_DEBT.md` structure: per-slice sections, one concrete trigger per row; parity entries and required tiers are not debt-able | `docs/TESTING_DEBT.md:1-18`; `ux-slice-g` section ends the file at `:95` | ✅ the `ux-slice-h` section appends after it |
| `core/README.md`'s recorded description of the nav model (`words-nav-items.ts` as "the Words-section sub-nav rendered as the top-navbar dropdown") | `core/README.md:44-52` | ✅ amended in the same change (§5.3) |
| Root `CLAUDE.md` Active Spec Kit Feature is `None` | root `CLAUDE.md` | ✅ set to `ux-slice-h` at T101, cleared at close |
| Design context: nav copy on the app shell — register is scholarly/calm, Arabic-first; navbar visual rules unchanged | `PRODUCT.md` (register, users), `DESIGN.md`, `UI_STYLE_SYSTEM.md` §5C | ✅ no visual token, color, or motion change is scheduled; the dropdown reuses `.dropdown-menu` as-is |

### DRIFT — where current code contradicts the audit or this commission

| # | The audit / commission says | `dev` at `50b7eef5` says | This plan follows |
|---|---|---|---|
| DRIFT-1 | Item 22 Fix/Size (`:914-919`): "Add an optional `children?: NavItem[]` … to `NavItem` … Then add the abwab children" — reading as if the children nest inside `NAV_ITEMS` in `nav-items.ts`. | The words children's routes are **functions imported from `route-paths.ts`** (`words-nav-items.ts:1-8`), and `route-paths.ts` imports `NAV_ITEMS` back (`route-paths.ts:1`) and calls `navRoute(...)` at module init (`:19-22`). Nesting the words children into `nav-items.ts` creates an import cycle whose init order **always** hits a TDZ `ReferenceError` — whichever module loads first, the other's consts are read before initialization. `nav-items.ts` today imports nothing, and that is load-bearing. | **The interface changes in `nav-items.ts`; the children attach in a composition module.** New `core/navigation/nav-menu.ts` exports `NAV_MENU: NavItem[]` — `NAV_ITEMS` with `children` attached from a plain `childrenByParentKey` table (words children from `words-nav-items.ts`, abwab children beside them). One-way imports: `nav-menu.ts → {nav-items, route-paths, words-nav-items}`. The navbar consumes `NAV_MENU`; every other consumer keeps `NAV_ITEMS` untouched. Data-driven is preserved — the *template* branches on `item.children`, never on a key (§4.2-1/2). |
| DRIFT-2 | Item 22 decision 1 (`:921-925`) and the commission: the الأرشيف entry "needs `queryParams: 'exact'`" via "the object form" of `IsActiveMatchOptions`, because "every existing dropdown link is a plain path" and the string shorthand won't do. | The existing words children use the **deprecated boolean shorthand** `{ exact: sub.route === wordsHubRoute }` (`.html:58`) — and Angular expands `exact: true` to `{paths:'exact', queryParams:'exact', fragment:'ignored', matrixParams:'ignored'}` (`router_module.d.d.ts:3293-3296`). Under the generalized rule `exact: child.route === item.route`, **both** `/abwab` children (الرئيسية and الأرشيف share the parent's path) get `exact: true` — i.e. `queryParams: 'exact'` — for free. `routerLinkActive` compares against the link's own `UrlTree` *including* its `[queryParams]`, so الأرشيف is active exactly on `/abwab?archive=1` and inert on the live view. | **No object form anywhere.** The generic branch carries the words shorthand unchanged: `{ exact: child.route === item.route }` — which is also byte-identical behavior for the words hub («الرئيسية» under words is the child whose route equals the parent's, same as today's `sub.route === wordsHubRoute`). The commission asked for exactly this verification and this is the finding: the predicted object-form requirement dissolves once the shorthand's expansion is read from the installed types. The consequences (`queryParams:'exact'` on **both** `/abwab` children) are stated cell-by-cell in §6a, including the two flagged cells (rows 3 and 5). |
| DRIFT-3 | Item 22's precedent framing: reuse the words dropdown mechanics as-is. | The words children's `@for` tracks `sub.route` (`.html:52`) — unique among the words children. The abwab children contain **two entries with `route: '/abwab'`** (الرئيسية and الأرشيف), so track-by-route produces duplicate `@for` keys (NG0955, duplicate-key collision) the moment the generic branch renders them. | Children track `child.key` (unique by construction — the words children gain keys in the `NavItem[]` retype anyway). The top-level `@for (item of primaryItems; track item.route)` stays as-is; top-level routes are unique. |
| DRIFT-4 | Item 22 decision 2 (`:926-928`): «الرئيسية» "collides with the dashboard's «لوحة التحكم» register; «الأبواب» or «شجرة الأبواب» reads better" — flagged as copy for the user to settle. | — (a copy question, not a code fact) | **The user settled it: «الرئيسية».** Locked decision, recorded without argument — and it has the words precedent on its side: the words dropdown's own hub entry is already «الرئيسية» (`words-nav-items.ts:18`), so the abwab dropdown reads identically to the shipped pattern. Do not substitute «الأبواب» or «شجرة الأبواب». |

## 0. Guard result

Task arithmetic: Phase 1 = 2, Phase 2 = 2, Phase 3 = 3, Phase 4 = 1, Phase 5 = 2, Phase 6 = 2.
**12 tasks — under the 30-task threshold. One slice, no split.**

Recorded so a mid-execution split does not get drawn on task count: if this slice had split, the
seam is **after Phase 3** — "parity" (Phases 1–3: the data model and the desktop generalization,
whose acceptance is that the words dropdown and the «المزيد» menu behave identically to `dev`)
versus "widening" (Phase 4: the mobile children, the one surface where behavior deliberately
changes for a non-abwab feature). The seam is parity-vs-widening because that is the honest
risk boundary: Phases 1–3 must be invisible everywhere except the new abwab trigger; Phase 4 is
visible by design.

## 1. Objective

| # | Deliverable | Home | Audit item |
|---|---|---|---|
| 1 | «الأبواب» renders as a hover dropdown with three children — «الرئيسية» → `/abwab`, «قوالب الأبواب» → `/abwab/templates`, «الأرشيف» → `/abwab` + `{archive:'1'}` — in that order | `top-navbar.component.html`, `core/navigation/` | 22 |
| 2 | The dropdown is data-driven: `NavItem` gains `children?` (and `queryParams?`), the `item.key === 'words'` branch is replaced by `@if (item.children)`, and the words dropdown comes out **behaviorally identical** | `nav-items.ts`, `nav-menu.ts` (new), `words-nav-items.ts`, `top-navbar.component.{ts,html,scss}` | 22 |
| 3 | `wordsOpen`/`moreOpen` collapse into one `openMenuKey: string \| null`; mutual exclusion stops being pairwise and the «المزيد» menu's observable behavior does not move | `top-navbar.component.ts` | 22 |
| 4 | «الأرشيف» is the app's first query-param nav entry, active exactly on `/abwab?archive=1` and never on the live view; «الرئيسية» never lights while `archive=1`; the parent lights on any `/abwab*` URL — the full matrix in §6a, every cell stated | `top-navbar.component.html` | 22 decision 1 |
| 5 | The recorded opposite decision is amended in the same change, without argument | `abwab.routes.ts:19-21` | 22 (⟲ reversal) |
| 6 | The mobile flat list gains **all** children — words and abwab — nested under their parents with the parent row still navigable; a deliberate behavior change on the words mobile surface | `top-navbar.component.{html,scss}` | 22 mobile note, widened by user decision |
| 7 | Accessibility preserved, not re-derived: `aria-haspopup`/`aria-controls`/`aria-expanded`, the chevron, Escape, outside-click, RTL, and the `--qd-z-mobile-nav` rung — all carried through the generic branch unchanged | same | 22 |
| 8 | Docs true in the same change: `core/README.md` nav paragraph, §17 verify-only, the `abwab.routes.ts` comment, and the `TESTING_DEBT.md` rows this posture owes | four files, named in §5.3 | repo law |

## 2. Scope

**In:**

- **Frontend — `core/navigation/`**
  - `nav-items.ts` — `NavItem` gains `children?: NavItem[]` and `queryParams?: Record<string, string>`; the `NAV_ITEMS` array is untouched.
  - `nav-menu.ts` — **new**: the composition module exporting `NAV_MENU` (DRIFT-1), holding the abwab child entries and attaching the words children.
  - `words-nav-items.ts` — `WORDS_MENU_ITEMS` retyped `readonly NavItem[]` (children gain `key`/`labelEn`/`group`); `WordsNavItem` deleted (no other consumer).
- **Frontend — `core/layout/top-navbar/`**
  - `top-navbar.component.ts` — consumes `NAV_MENU`; `openMenuKey` replaces `wordsOpen`/`moreOpen`; `openMenu`/`closeMenu`/`toggleMenu`/`isMenuActive` replace the words- and more-specific members; Escape and outside-click generalized.
  - `top-navbar.component.html` — the `@if (item.children)` branch; the more menu re-keyed onto `openMenuKey === 'more'` with its markup otherwise untouched; the mobile nested child lists.
  - `top-navbar.component.scss` — `.words-dropdown` → `.nav-dropdown` (the more `<li>` joins the same class); the mobile sub-list indentation rule. No token, color, motion, or z change.
- **Frontend — `features/abwab/`**
  - `abwab.routes.ts:19-21` — the comment amendment only (§5.3). No route change.
- **Docs (same change, repo law)** — `core/README.md:44-52`; `.architecture/UI_STYLE_SYSTEM.md` §17 (verify-only unless something moved); `docs/TESTING_DEBT.md` (new `ux-slice-h` section); `docs/feature-ux-slice-h/evidence.md` (new); root `CLAUDE.md` (Active Spec Kit Feature, set and cleared).

**Out (named so nobody "finishes the thought"):**

- **Any new nav item beyond the three abwab children.** No entry for anything else, no
  reordering of `NAV_ITEMS`, no group changes.
- **Any change to the words dropdown's behavior.** Hover-open, click-toggle, Escape,
  outside-click, active states, ids, testids — identical to `dev`. The mobile words children
  are the one sanctioned words-surface change (decision 5), and they are additive.
- **Any route added, renamed, or removed.** `/abwab/templates` keeps its title source
  (`ABWAB_LABELS.templatesPageTitle`); `navLabel`'s registry gains no key.
- **Lazy-loading / preloading work.** The production first-navigation lag is a separate backlog
  item; nothing here touches loaders.
- **Caching** — Slice I owns it, last.
- **Permission or auth gating on any nav entry** — the public-browse posture
  (`app.routes.spec.ts`) is untouched.
- **`qd-context-menu` reuse.** This is a hover menu with different semantics
  (audit `:907-911`); the primitive is not composed and not touched.
- **A §17 entry for the nav dropdown.** It remains a single-consumer pattern inside
  `top-navbar`, not a shared primitive; §17 documents shared primitives.
- **The «المزيد» menu's markup and semantics** beyond the state-field rename. It is a group
  dropdown, not an `item.children` dropdown, and it does not gain hover-open.
- **Any planning-artifact sweep or N-2 deletion** — deferred to the single cleanup pass after
  Slice I.
- **Any `dev → main` merge.**

## 3. Non-goals

- **No litigation of the reversal.** `abwab.routes.ts:19-21` recorded "an item in the nav
  nobody asked for"; the user asked. The comment is amended to the new fact (§5.3); no
  argument paragraph is added anywhere.
- **No new test suites, per the rush-period posture** (continued from Slices F/G). Existing
  suites RUN before merge; every gap becomes a `docs/TESTING_DEBT.md` row in the same change
  (§7). There is no route-smoke tier to run because there is no backend change — stated in
  §4.1-8 so the absence is legible.
- **No planning-artifact sweep — standing user decision.** All sweeps and N-2 evictions wait
  for the post-Slice-I cleanup pass.
- **No visual redesign of the navbar.** §5C of `UI_STYLE_SYSTEM.md` stands; the new dropdown
  reuses `.dropdown-menu`, `.dropdown-link`, `.chevron`, and the existing tokens without a
  single new value.

## 4. Locked decisions

### 4.1 Carried in from the audit / the commission / prior slices / standing rules

1. **⟲ Reversal, record do not litigate.** The `abwab.routes.ts:19-21` comment is amended in
   the same change with no argument paragraph — the same treatment items 12/13/20 received.
2. **Data-driven, not a second special case.** `NavItem.children`, `@if (item.children)`,
   one `openMenuKey: string | null`. The words dropdown is the precedent, not a thing to
   improve — it must come out behaviorally identical.
3. **The abwab children, in this order:** «الرئيسية» → `/abwab`; «قوالب الأبواب» →
   `/abwab/templates`; «الأرشيف» → `/abwab` with `{ archive: '1' }`. «الرئيسية» is the user's
   chosen label (DRIFT-4); the templates label is the shipped `templatesPageTitle` string
   (`abwab.labels.ts:308`), not a variant.
4. **«الأرشيف» active semantics:** active exactly on `/abwab?archive=1`, never on the live
   view; «الرئيسية» never lights while `archive=1`; the parent lights on any `/abwab*` URL.
   The full matrix is §6a. The mechanism satisfying this is DRIFT-2's finding.
5. **Mobile — user decision, deliberately widening the slice:** the flat mobile list shows
   **all** children — words and abwab. A non-abwab surface (the words mobile nav) changes on
   purpose. The flattened shape is fixed in §4.2-8.
6. **Same-change README + §17 amendments are repo law and in scope.** No sweep, no N-2
   deletion — deferred to the post-Slice-I pass.
7. **Rush-period testing posture:** no new suites; existing suites run before merge; gaps
   become `TESTING_DEBT.md` rows in the same change.
8. **Tier B.** `core/layout` and `core/navigation` are shared app-shell surfaces
   (`TESTING_STRATEGY.md` §3/§4) — the full frontend suite and `npm run build` are the gates.
   **Frontend only — no backend, no route, so no route-smoke tier and no `SmokeRouteCatalog`
   entry is owed.** Stated here explicitly so a reviewer does not read the absence of a smoke
   run or a catalog diff as an omission: the trigger (`Backend/api/` routes, contracts, auth,
   middleware, binding) is not touched by any file in §2.

### 4.2 Decided by this plan

1. **The children live in a composition module, not in `NAV_ITEMS`** (DRIFT-1). New
   `core/navigation/nav-menu.ts` exports `NAV_MENU: NavItem[]`, built by mapping `NAV_ITEMS`
   over a `childrenByParentKey: Record<string, NavItem[]>` table — a data table, so the wiring
   is declarative and the template never branches on a key. Consequences, all verified against
   the consumer census: `NAV_ITEMS` array entries are byte-identical; `navLabel`'s domain is
   unchanged (children have keys, but `navLabel` searches `NAV_ITEMS`, which never carries
   them); `app.routes.ts`'s placeholder filter and both route specs see nothing new.
2. **`WORDS_MENU_ITEMS` retypes to `readonly NavItem[]` in place.** The six entries gain
   `key` (`words-home`, `words-unique`, `words-roots`, `words-lemmas`, `words-stems`,
   `words-types`), `labelEn`, and `group: 'primary'`. `WordsNavItem` is deleted — grep shows
   no consumer outside the file and the navbar. The file keeps its recorded
   labels-owned-in-core comment; the abwab children follow the same rule (§4.2-6).
3. **The generic branch preserves the words DOM contract by interpolation.** Menu id
   `[id]="item.key + '-menu'"` (→ `words-menu`, `abwab-menu`), trigger testid
   `'nav-' + item.key + '-trigger'` (→ `nav-words-trigger`, `nav-abwab-trigger`),
   `aria-controls` bound to the same id, `aria-haspopup="true"`, `[attr.aria-expanded]`, the
   chevron svg verbatim. Child links gain `[attr.data-testid]="'nav-menu-link--' + child.key"`
   — the more menu's existing convention (`.html:120`), new on the words children (an inert
   attribute addition, recorded as the one DOM delta besides the class rename). `shell-nav.e2e.ts`'s
   three locators (`nav-words-trigger`, `#words-menu`, role-scoped «الرئيسية») all resolve
   unchanged.
4. **Hover-open belongs to the children branch only.** `(mouseenter)`/`(mouseleave)` bind on
   the generic branch's `<li>`, exactly as the words `<li>` today. The «المزيد» menu keeps
   click-only opening — it is not an `item.children` dropdown and gains no hover path.
5. **The state collapse, precisely.** `openMenuKey: string | null` replaces `wordsOpen` and
   `moreOpen`; `'more'` is the more menu's key. `openMenu(key)` sets it (implicitly closing
   any other — exclusion by construction, no longer pairwise); `toggleMenu(key)` sets
   `openMenuKey === key ? null : key`; `closeMenu(key)` clears **only if** `openMenuKey === key`
   (preserving `closeWords`'s only-close-yourself semantics on `mouseleave`, `.ts:100-102`).
   Escape: if `openMenuKey` set, clear it; the mobile branch unchanged — one press still closes
   everything open, as today (`.ts:53-64`). Outside-click: both dropdown `<li>`s carry a shared
   `nav-dropdown` class (replacing `.words-dropdown`, joining `.more-dropdown`); the handler
   checks the single `el.querySelector('.nav-dropdown.open')` instead of two class-specific
   queries. `toggleMobile`/`closeMobile` set `openMenuKey = null` where they cleared both
   booleans. **Observable behavior of the «المزيد» menu moves by nothing** — if any browser-walk
   step in T501 finds otherwise, that is stop condition 1.
6. **The abwab child labels are owned in core, words-precedent.** `words-nav-items.ts:15-16`
   records the rule: menu labels live in `core/navigation`, routes come from constants. The
   abwab children's `labelAr` values are written in `nav-menu.ts` («الرئيسية», «قوالب
   الأبواب», «الأرشيف»); T101's evidence records the equality check against
   `ABWAB_LABELS.templatesPageTitle` (`abwab.labels.ts:308`) — same string, deliberately
   duplicated across the core/feature boundary exactly as every words label already is. The
   child routes are literals `'/abwab'` / `'/abwab/templates'` — importing `ABWAB_ROUTE_PATH`
   from `route-paths.ts` would be fine (no cycle from `nav-menu.ts`), and is used where it
   exists; there is no exported templates path constant, and this slice does not add one
   (the segment is owned by `abwab.routes.ts:22`; a second export would be a second source
   of truth for one string).
7. **Active options: the words shorthand, generalized** (DRIFT-2).
   `[routerLinkActiveOptions]="{ exact: child.route === item.route }"` on every child link.
   Expansion (verified, `router_module.d.d.ts:3293-3296`): the two `/abwab`-path children get
   `paths:'exact', queryParams:'exact'`; «قوالب الأبواب» gets all-`subset`. The parent trigger
   uses `isMenuActive(item)` = `router.isActive(item.route, { paths: 'subset', queryParams:
   'ignored', fragment: 'ignored', matrixParams: 'ignored' })` — `isWordsActive`'s exact body
   (`.ts:142-149`) with the route parameterized. Every resulting cell is in §6a; the two
   flagged cells (rows 3 and 5) are consequences of `queryParams:'exact'` and are accepted as
   the uniform-child-semantics trade, consistent with the shipped words hub behavior.
8. **The mobile flattened shape, fixed.** Inside `ul.mobile-menu-list`, a parent with children
   renders its own row **unchanged and still navigable** (the link, options, and `closeMobile`
   click as today, `.html:293-305`), followed by a nested `<ul class="mobile-menu-sublist">` of
   child links — one indentation level via `padding-inline-start` (logical property, so RTL
   comes free), `mobile-link` styling otherwise inherited, active options per §4.2-7, click
   closes the panel, track `child.key`. No collapse/expand affordance — the list is short
   (6 words + 3 abwab children) and an accordion would be new interaction surface this slice
   does not need. The words children appearing here is the sanctioned widening (§4.1-5).
9. **The desktop parent stops being a link — words precedent, stated not hidden.** Today
   «الأبواب» is a navigable link (`.html:68-80`); as a dropdown parent it becomes a button
   that only toggles, exactly like the words trigger (`.html:21-30`). Desktop reach to `/abwab`
   is the «الرئيسية» child (plus the mobile parent row, which stays a link). This is what the
   words item already does; recorded so the affordance change is a decision, not a surprise.
10. **Children track `child.key`** (DRIFT-3). The top-level `track item.route` stays.
11. **The `abwab.routes.ts` comment amendment, verbatim scope:** the sentence chain at
    `:19-21` is rewritten to record that the nav now carries the workshop entry as a child of
    «الأبواب» in the navbar's menu model (`nav-menu.ts`), that `NAV_ITEMS` still carries no
    `templates` key, and that the title therefore remains `ABWAB_LABELS.templatesPageTitle`
    because `navLabel` (which throws on unknown keys) still cannot serve it. The mechanical
    prediction the old comment made stays true and stays written; only the "nobody asked for"
    clause dies.

## 5. The ground truth this plan is derived from

### 5.1 The consumer census — what a `children` field can and cannot break

Every `NAV_ITEMS` / `WORDS_MENU_ITEMS` / `navLabel` consumer, by grep at `50b7eef5`:

| Consumer | Reads | Effect of this slice |
|---|---|---|
| `top-navbar.component.ts:6,8,32-35,38` | `NAV_ITEMS` groups, `WORDS_MENU_ITEMS`, `WORDS_ROUTE_PATH` | **rewired to `NAV_MENU`** — the one consumer that changes |
| `route-paths.ts:1,4,19-22` | `NAV_ITEMS` by key at module init | untouched — no new key, no entry reshaped |
| `app.routes.ts:2,10-13` | `NAV_ITEMS` filtered by key → placeholder routes | untouched — children never enter `NAV_ITEMS`, so no phantom placeholder route can appear |
| `abwab.routes.ts:3,16` | `navLabel('abwab')` | untouched; `:24` keeps `ABWAB_LABELS.templatesPageTitle` (§4.2-11) |
| `route-paths.spec.ts:3,15-16` | `NAV_ITEMS` route equality | untouched |
| `words.routes.spec.ts:8,103-112` | placeholder-key filter, words route | untouched |
| `abwab.routes.spec.ts:9,21` | `navLabel('abwab')` as route title | untouched |

No consumer the audit did not name exists, and none reads a field this slice reshapes — stop
condition 2 was checked at plan time and is clear. If execution finds an eighth consumer, that
is the stop.

### 5.2 The active-option semantics, from the installed types

`routerLinkActiveOptions` accepts `{ exact: boolean }` (deprecated) or `IsActiveMatchOptions`
(`router_module.d.d.ts:101-128, 3293-3296`):

- `exact: true` ⇒ `{ paths: 'exact', queryParams: 'exact', fragment: 'ignored', matrixParams: 'ignored' }`
- `exact: false` ⇒ `{ paths: 'subset', queryParams: 'subset', fragment: 'ignored', matrixParams: 'ignored' }`

`routerLinkActive` matches against the anchor's own `UrlTree`, which includes its
`[queryParams]`. Therefore, under `{ exact: child.route === item.route }`:

| Child | Options resolved | Meaning |
|---|---|---|
| «الرئيسية» (`/abwab`, no params) | `exact: true` | active iff URL path is exactly `/abwab` **and** the URL carries no query params |
| «الأرشيف» (`/abwab` + `{archive:'1'}`) | `exact: true` | active iff URL is exactly `/abwab?archive=1` |
| «قوالب الأبواب» (`/abwab/templates`) | `exact: false` | active on `/abwab/templates` with any params (it has none by contract, `features/abwab/README.md:343-351`) |
| words hub «الرئيسية» | `exact: true` | **unchanged from today** — `sub.route === wordsHubRoute` is the same predicate |

This is DRIFT-2's resolution: the locked decision's required `queryParams: 'exact'` arrives via
the shipped boolean shorthand, and no object form enters the template.

### 5.3 The amendment ledger — every recorded statement, by file and line

| File | Line(s) | What is there now | Treatment |
|---|---|---|---|
| `abwab.routes.ts` | `:19-21` | "…its title is its own page title rather than a `navLabel`: `navLabel` throws on a key `NAV_ITEMS` does not carry, and adding one would put an item in the nav nobody asked for." | **rewritten per §4.2-11** — the throw fact and the title source survive; the "nobody asked for" clause is replaced by the recorded reversal. T303 |
| `core/README.md` | `:44-52` | `navigation/` described as `route-paths` + `nav-items` + `app-title.strategy` + `words-nav-items` ("the Words-section sub-nav rendered as the top-navbar dropdown") | rewritten: the nav menu model — `nav-items.ts` (flat registry: routes, titles, placeholder derivation), `nav-menu.ts` (the navbar's presentation tree, children attached outside `NAV_ITEMS` **because of the `route-paths` import cycle**, recorded so nobody "simplifies" the children back in), `words-nav-items.ts` (words children as `NavItem[]`), and the الأرشيف query-param entry as the app's first. T601 |
| `words-nav-items.ts` | `:15-16` | the labels-owned-in-core comment | survives; extended one clause to note the entries are `NavItem`s consumed via `nav-menu.ts` |
| `.architecture/UI_STYLE_SYSTEM.md` | §17 "Sticky app chrome" (`:1215`), "Chrome-inert rule" (`:1286`), §4 z-scale (`:169-182`), §5C (`:473-485`) | current truth | **verify-only** — nothing this slice does moves a rung, a token, or an inert rule. Amended only if T501's walk proves otherwise, in the same commit. T601 |
| `docs/TESTING_DEBT.md` | end of file (after `:95`) | — | new `ux-slice-h` section with §7's rows. T602 |
| root `CLAUDE.md` | Active Spec Kit Feature | `None` | `ux-slice-h` + this plan at T101; back to `None` at T602 |

**Do not touch, and do not "fix" while here:** `NAV_ITEMS`' entries and order; `navLabel`'s
throw (it is a guard, not a bug); the placeholder-route derivation (`app.routes.ts:10-20`); the
`{ exact: item.route === '/dashboard' }` options on top-level links (`.html:74,299`);
`isMoreActive()`'s per-item `paths:'exact'` (`.ts:131-140`); the more menu's markup, label, and
click-only opening; the dropdown/mobile z rungs and their comments (`.scss:76-78,151`); the
Chrome-inert wiring (`.ts:12-16,30`, `.html:5-6`); `shell-nav.e2e.ts` (it must pass unedited —
an edit there is a parity regression, not maintenance).

## 6. Phases

Every phase is one commit. The build is green at each commit boundary.

### Phase 1 — Baseline and record (2 tasks)

**Files** — root `CLAUDE.md`; `docs/feature-ux-slice-h/evidence.md` (new).

- **T101 — Baseline, recorded before anything is touched.** Set the root `CLAUDE.md` Active
  Spec Kit Feature to `ux-slice-h` + this plan. Create `evidence.md` and record, as measured
  numbers: `npm test` (full suite — expect 191 files / 2,161 tests per `TESTING_STRATEGY.md`
  §6; any drift from those figures is recorded, not assumed away) and `npm run build`. Record
  the label-equality check: the «قوالب الأبواب» child string === `ABWAB_LABELS.templatesPageTitle`
  (`abwab.labels.ts:308`). Record explicitly: **no backend gate runs in this slice and why**
  (§4.1-8). A baseline that is not green is a stop condition, not a starting point.
- **T102 — Sweep for recorded statements the reversal falsifies.** `grep -rn` across `src/`,
  `e2e/`, `docs/`, `.architecture/`, and the frontend READMEs for: `nobody asked`,
  `nav-link--abwab`, `words-dropdown`, `wordsOpen`, `WordsNavItem`, `WORDS_MENU_ITEMS`, and
  «الأرشيف» near nav copy. Every hit must be in §5.3's ledger, in its do-not-touch list, or
  it is a finding folded into the ledger before Phase 2. Record the grep and result in
  `evidence.md`. (Plan-time greps found: the routes comment, the scss class, the component's
  own members, `core/README.md` — nothing else.)

### Phase 2 — The nav data model (2 tasks)

**Files** — `core/navigation/nav-items.ts`, `words-nav-items.ts`, `nav-menu.ts` (new).

- **T201 — The interface and the words retype.** `NavItem` gains `children?: NavItem[]` and
  `queryParams?: Record<string, string>` with a `//` comment stating the one non-obvious rule:
  children are navbar-menu presentation only — they never enter `NAV_ITEMS`, `navLabel`'s
  registry, or the placeholder-route derivation. `WORDS_MENU_ITEMS` retypes to
  `readonly NavItem[]` per §4.2-2; `WordsNavItem` deleted.
- **T202 — The composition module.** `nav-menu.ts` per §4.2-1: the three abwab child entries
  (order per §4.1-3, الأرشيف carrying `queryParams: { archive: '1' }`), the
  `childrenByParentKey` table, and the exported `NAV_MENU`. A `//` comment records DRIFT-1's
  cycle (`route-paths.ts` imports `NAV_ITEMS`, so children whose routes come from
  `route-paths` functions cannot live in `nav-items.ts`) — otherwise the next reader inlines
  the children and hits the TDZ at runtime. This phase compiles with the navbar untouched:
  nothing consumes `NAV_MENU` yet, and the navbar's `sub.labelAr`/`sub.route` reads are
  fields `NavItem` also carries.

### Phase 3 — The desktop generalization (3 tasks)

**Files** — `top-navbar.component.{ts,html,scss}`; `features/abwab/abwab.routes.ts`.

- **T301 — The template branch.** Replace `@if (item.key === 'words')` (`.html:14-67`) with
  `@if (item.children)` per §4.2-3: interpolated id/testid/`aria-controls`, the chevron
  verbatim, hover-open on the `<li>` (§4.2-4), child links with
  `[queryParams]="child.queryParams ?? null"`, `routerLinkActive="active"`,
  `[routerLinkActiveOptions]="{ exact: child.route === item.route }"` (§4.2-7),
  `(click)="closeMenu(item.key)"`, testid `nav-menu-link--<key>`, `track child.key`
  (§4.2-10). The `@else` link branch survives for childless items; `nav-link--abwab` ceases
  to render (Precondition table: nothing selects it). The component consumes `NAV_MENU`.
- **T302 — The state collapse.** `openMenuKey` per §4.2-5, the more menu re-keyed onto
  `'more'` with markup otherwise untouched, Escape and outside-click generalized, `.scss`
  `.words-dropdown` → `.nav-dropdown` (the more `<li>` joins it; `:57-60` and `:67-70`
  collapse to one selector each). `isMenuActive(item)` replaces `isWordsActive` (§4.2-7);
  `isMoreActive` untouched.
- **T303 — The comment amendment.** `abwab.routes.ts:19-21` per §4.2-11. This lands in the
  commit that makes the nav entry real, so the comment and the fact flip together.

### Phase 4 — The mobile widening (1 task)

**Files** — `top-navbar.component.{html,scss}`.

- **T401 — The nested child lists.** Per §4.2-8: parent rows unchanged and navigable; a
  `mobile-menu-sublist` under each parent with children; `padding-inline-start` indentation
  (one new rule in `.scss`, tokens only — `var(--qd-space-6)`-class spacing, no new values);
  active options per §4.2-7; clicks close the panel; `track child.key`. The words children
  appear on mobile for the first time — the sanctioned widening, stated in the commit message.

### Phase 5 — Verification (2 tasks)

- **T501 — The browser walk.** jsdom cannot hover, and no navbar spec exists (Precondition
  table) — the browser is the only check for most of §6b. Walk and record in `evidence.md`,
  desktop then mobile viewport:
  - **Words parity (the acceptance for §4.1-2):** hover opens; click toggles — including the
    shell-nav quirk that hover-then-click *closes* (`shell-nav.e2e.ts:15-16`); `mouseleave`
    closes; Escape closes; outside-click closes; opening «المزيد» while words is open closes
    words (and every pairing of the three menus, both orders); child click navigates and
    closes; `aria-expanded` toggles; chevron rotates; Tab reaches the trigger, then the open
    menu's links; active states on a words route.
  - **Abwab dropdown:** the three children render in order with the locked labels; every §6a
    row walked URL-by-URL, including both flagged cells (rows 3 and 5) — seeing them is
    confirmation, not a defect; the الأرشيف click lands on `/abwab?archive=1` and the archive
    view opens; RTL alignment of the menu under the trigger (inset-inline-start).
  - **«المزيد» parity:** click-toggle, no hover-open, Escape, outside-click, active state on
    `/mutashabihat` — identical to `dev`, or stop condition 1 fires.
  - **Layering:** open a dropdown, then open an abwab modal — the navbar goes inert and the
    dialog paints above (Chrome-inert + 45 < 50); confirm §17 needs no amendment.
  - **Mobile:** children indented under both parents, parent rows still navigate, active rows
    per §6a, panel closes on child click.
- **T502 — Tier B/C.** Full `npm test` (expect the T101 counts **unchanged** — this slice
  writes no spec) and `npm run build`. Any count that moves against T101 is explained per-file
  or it is a finding. Optionally run `npx playwright test e2e/shell-nav.e2e.ts` as evidence
  (never a tier, §4.1-8): all three flows must pass **unedited**.

### Phase 6 — Docs true again (2 tasks)

- **T601 — READMEs and §17.** `core/README.md:44-52` per §5.3. §17 verify-only pass over
  "Sticky app chrome", "Chrome-inert rule", the §4 z-scale, and §5C — amend only what T501
  proved moved (expected: nothing). `words-nav-items.ts:15-16` comment clause per §5.3.
- **T602 — Debt and close-out.** Append the `ux-slice-h` section to `docs/TESTING_DEBT.md`
  with §7's rows. Re-run T102's sweep; every remaining hit amended or recorded. Clear the root
  `CLAUDE.md` Active Spec Kit Feature to `None`. **No planning folder deleted, swept, or
  repointed.**

| Phase | Commit | Gate before the next phase starts |
|---|---|---|
| 1 | `docs(ux-slice-h): baseline and reversal sweep` | T101 green; T102 findings folded into §5.3 |
| 2 | `feat(ux-slice-h): NavItem children and the nav menu model` | `npm run build` (navbar untouched, additive only) |
| 3 | `feat(ux-slice-h): الأبواب becomes a data-driven hover dropdown` | build green; words dropdown behaviorally identical in a quick manual check |
| 4 | `feat(ux-slice-h): the mobile list gains the words and abwab children` | build green |
| 5 | `test(ux-slice-h): browser walk and Tier B/C evidence` | T501 walked and recorded; T502 counts unchanged |
| 6 | `docs(ux-slice-h): README, §17 verification, and the debt this slice owes` | sweep clean |

## 6a. The active-state matrix — URL × nav element

The substance of this slice. Options per §5.2; "parent" means the trigger button's `.active`
class via `isMenuActive` (`paths:'subset', queryParams:'ignored'`). ✅ = highlighted, — = not.

| # | URL | «الأبواب» parent | «الرئيسية» | «قوالب الأبواب» | «الأرشيف» | words parent | words hub «الرئيسية» | words «الجذور» |
|---|---|---|---|---|---|---|---|---|
| 1 | `/abwab` | ✅ | ✅ | — | — | — | — | — |
| 2 | `/abwab?archive=1` | ✅ | — (queryParams `exact`) | — | ✅ | — | — | — |
| 3 | `/abwab?archive=1&q=x` (archive search — real, `features/abwab/README.md:61-63`) | ✅ | — | — | — **(flagged)** | — | — | — |
| 4 | `/abwab/templates` | ✅ (paths subset) | — (paths exact) | ✅ | — | — | — | — |
| 5 | `/abwab?door=5` (or any live-view param set) | ✅ | — **(flagged)** | — | — (params ≠ `{archive:1}`) | — | — | — |
| 6 | `/dashboard/words/roots` | — | — | — | — | ✅ | — (exact) | ✅ |
| 7 | `/dashboard/words` | — | — | — | — | ✅ | ✅ | — |
| 8 | `/tafsirs` (any unrelated route) | — | — | — | — | — | — | — |

The mobile rows follow the same columns: the parent **row** uses today's
`{ exact: item.route === '/dashboard' }` (⇒ all-`subset` for abwab/words), so it matches the
parent-trigger column; child rows match their desktop columns exactly.

**The two flagged cells, accepted deliberately (§4.2-7):** row 3 — الأرشيف goes dark while the
archive view is being searched, and row 5 — الرئيسية goes dark while a door is selected on the
live view. Both are consequences of `queryParams:'exact'`, which is the locked requirement for
the archive/live separation, applied uniformly. The parent stays lit in both, so the section is
never unmarked. The words hub child has had exactly these semantics since it shipped; changing
either cell would need a per-child options override or a bespoke active computation, and
neither is bought in this slice. Recorded here so a future "why did الرئيسية unlight" report
finds a decision, not a bug.

## 6b. The interaction matrix — the generalized menu

| Event | While closed | While this menu open | While the *other* menu open |
|---|---|---|---|
| `mouseenter` parent `<li>` (children menus only) | opens; `openMenuKey = key` | no-op | switches — the single field closes the other by construction |
| `mouseleave` parent `<li>` | no-op | closes (`closeMenu(key)` — only if still own) | no-op on the other (the `=== key` guard) |
| Trigger click | opens | **closes** — the hover-then-click toggle quirk, preserved (`shell-nav.e2e.ts:15-16` documents it as shipped behavior) | switches |
| «المزيد» trigger click | opens (`'more'`) | closes | switches |
| «المزيد» hover | **nothing** — click-only, unchanged (§4.2-4) | — | — |
| Escape (document) | no-op | closes; also closes the mobile panel if open — one press, everything, as today | — |
| Click outside the open `.nav-dropdown` | no-op | closes | — |
| Child link click | — | navigates + closes + `routerLinkActive` recomputes per §6a | — |
| Tab | trigger is a tab stop; menu links are tab stops only while rendered (`@if` open) — words behavior, unchanged | trigger → links in DOM order | — |
| Mobile toggle | opens panel; `openMenuKey = null` | closes any desktop menu | — |
| A modal opens (scroll lock) | navbar inert (`.html:5-6`) — the whole nav, dropdown included, leaves the tab order; dropdown (45) < backdrop (50) | same | — |

Focus is never trapped and never moved programmatically — the words dropdown does neither
today, and parity is the contract.

## 7. Testing posture and the debt it owes

**Posture (locked, §4.1-7):** no new suites. Existing suites RUN. No parity entry is owed —
no route exists in this slice (§4.1-8). Gaps become `TESTING_DEBT.md` rows in this change.

**The gates that run (`TESTING_STRATEGY.md` §6):**

- `npm test` — T101 (baseline) and T502 (close), full suite both times: `core/` is shared
  app-shell infrastructure, so Tier B's full-suite trigger fires and Tier C's pre-PR gate is
  satisfied by the same run. Expect 191 files / 2,161 tests, **unchanged** — this slice writes
  no spec. The specs nearest the changed code (`route-paths.spec.ts`, `words.routes.spec.ts`,
  `abwab.routes.spec.ts`, `app.routes.spec.ts`) ride inside the full runs.
- `npm run build` — T101, T502.
- **No backend command runs.** No `dotnet` gate, no route-smoke tier, no
  `Tests.Smoke.Data` statement — there is nothing for them to check (§4.1-8), and this line
  exists so the evidence's silence on them reads as the decision it is.

**Not a gate:** Playwright. `shell-nav.e2e.ts` is the one e2e touching this surface; if run
(T502, optional), it is evidence of words/more parity, never a tier.

**What this posture leaves uncovered — honestly: the app shell is the highest-traffic surface
in the product and the thinnest-pinned in the repo.** Every user of every feature crosses this
component, and its total automated cover is three opt-in e2e flows. The rows:

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| H1 | **The navbar itself, wholesale** — no unit spec exists at all: menu open/close state, `openMenuKey` mutual exclusion (now load-bearing for three menus), Escape/outside-click dismissal, `aria-expanded`, the inert-under-lock binding. This predates the slice; the slice makes the state machine shared, which raises the cost of it being unpinned | `core/layout/top-navbar/` | The next change to the navbar or the nav model — auth-gated entries, a fourth dropdown, or Slice I if caching adds any nav affordance |
| H2 | **The §6a active-state matrix** — the query-param cells (rows 2/3/5) are exactly where a `routerLinkActiveOptions` regression hides, they are assertable in jsdom with a router harness, and nothing asserts them | `top-navbar.component.html`, `core/navigation/nav-menu.ts` | The next nav-entry addition, or any change to the abwab URL contract's `archive` key |
| H3 | **The mobile flattened children** — nesting, indentation, parent-row navigability, per-row active state | `top-navbar.component.{html,scss}` | The next mobile-nav change |
| H4 | **One e2e flow for the new dropdown** — hover «الأبواب», click «الأرشيف», land on `/abwab?archive=1` with the archive view open. `shell-nav.e2e.ts` is the shipped template; this is one ~10-line test in an existing file, the cheapest row here | `e2e/shell-nav.e2e.ts` | The next time the navbar or the abwab URL contract changes shape |

H4 is the honest one to flag in review: the posture's logic applies, but the file, fixture, and
pattern all exist, so the marginal cost is a fraction of the others. Deferring it is a choice,
not a constraint.

## 8. Risk register

| # | Risk | Likelihood | Blast radius | Mitigation in this plan |
|---|---|---|---|---|
| 1 | The words children are inlined into `nav-items.ts` "for simplicity", hitting the `route-paths` import cycle at runtime | **high** — it is the audit's own naive reading | The app fails at module init (TDZ), possibly only in some load orders | DRIFT-1 names the mechanism; §4.2-1 fixes the module; T202 writes the reason into `nav-menu.ts` itself |
| 2 | The generalized branch subtly changes words behavior (hover/click/dismiss/active) | medium | The most-seen surface in the product regresses; only 3 opt-in e2e flows could catch it | §4.2-3/-4/-5 pin the contract field by field; T501's parity walk is the acceptance; `shell-nav.e2e.ts` must pass unedited |
| 3 | Track-by-route ships for children | medium — it is the words template's own pattern | NG0955 duplicate-key collision on the two `/abwab` children | DRIFT-3, §4.2-10 |
| 4 | An e2e or test locator matches the **new** «الرئيسية» link | low | `shell-nav.e2e.ts:18` — already scoped to `#words-menu`, so safe; the risk is future locators | Precondition table records the scoping as load-bearing; H4's future test must scope to `#abwab-menu` |
| 5 | The «المزيد» menu's behavior moves in the `openMenuKey` collapse (e.g. it gains hover-open, or Escape ordering changes) | low | A shipped shell surface changes unasked | §4.2-4/-5 state the invariants; T501 walks it; **stop condition 1 if it moves anyway** |
| 6 | The flagged §6a cells (rows 3/5) are "fixed" mid-execution by loosening `queryParams` | medium — they look like bugs | الأرشيف lights on the live view (row 3's fix breaks row 2) or الرئيسية lights under `archive=1` — the exact defects the locked decision exists to prevent | §6a states the cells and the trade; §4.2-7 records uniformity as the decision |
| 7 | Desktop users lose the one-click «الأبواب» → `/abwab` path and file it as a regression | certain, by design | An affordance change on the shell | §4.2-9 records it as the words precedent; «الرئيسية» is first in the menu; mobile parent row still navigates |
| 8 | The mobile words children surprise a words-feature owner | certain, by design | A non-abwab surface changed in an abwab slice | §4.1-5 records it as the user's decision; T401's commit message states it |
| 9 | The dropdown outpaints a modal, or vice versa | low | Layering regression on the shell | No z value moves; the rung story is verified, not touched (Precondition table); T501's layering step; Chrome-inert makes the fight unreachable anyway |
| 10 | `core/README.md` or §17 drifts from the shipped mechanism | medium | The next agent plans against a stale nav model | T601 is a task with a named line range, not a reminder |

**Rollback:** every phase is one green commit; reverting Phase 3 alone restores the hard-coded
words branch without touching the data model, and reverting Phases 2–4 together is a clean
return to `dev` behavior. No migration, no contract, no stored state anywhere in the slice.

## 9. Obligations checklist (all must be true at close)

- [ ] «الأبواب» is a hover dropdown with exactly three children, in the locked order, with the locked labels — «الرئيسية» first, verbatim.
- [ ] The «قوالب الأبواب» label equals `ABWAB_LABELS.templatesPageTitle`, and the equality check is in `evidence.md`.
- [ ] «الأرشيف» navigates with `queryParams: { archive: '1' }` and its active state matches §6a row 2 — and rows 3 and 5 were *observed*, not skipped.
- [ ] The template branches on `item.children`; no `item.key === '…'` branch remains in the dropdown path.
- [ ] `NAV_ITEMS`' entries are byte-identical; the children live in `nav-menu.ts`; `navLabel`'s domain is unchanged.
- [ ] `wordsOpen` and `moreOpen` no longer exist; `openMenuKey` is the only menu state; the «المزيد» menu's observable behavior is unchanged.
- [ ] The words dropdown is behaviorally identical: T501's parity walk recorded, `shell-nav.e2e.ts` unedited (and green if run).
- [ ] `words-menu` id, `nav-words-trigger` testid, `aria-haspopup`/`aria-controls`/`aria-expanded`, and the chevron all survive via interpolation.
- [ ] Children track `child.key`; no NG0955 in the console during T501.
- [ ] The mobile list nests **all** children under their parents; parent rows still navigate; indentation is `padding-inline-start`.
- [ ] `abwab.routes.ts:19-21` is amended per §4.2-11; the `navLabel`-throws fact and the title source survive; no argument paragraph exists.
- [ ] No z value, token, §17 rung, or Chrome-inert wiring moved; the §17 verify-only pass is recorded.
- [ ] `core/README.md:44-52` describes the shipped model, including the cycle rationale.
- [ ] `TESTING_DEBT.md` carries the `ux-slice-h` section (H1–H4).
- [ ] Full `npm test` and `npm run build` green at T101 and T502 with unchanged counts; no backend command ran, and `evidence.md` says why.
- [ ] `evidence.md` records T101, T102, T501's walk (including the flagged cells), T502, and T602's sweep.
- [ ] Root `CLAUDE.md` Active Spec Kit Feature back to `None`; no planning folder deleted, swept, or repointed; no package install; no `dev → main` merge.

## 10. Execution note

Phase 2 lands the data model before any consumer changes — `NAV_MENU` is exported and unused,
which is green, and the words retype is invisible to the navbar because `NavItem` is a
superset of `WordsNavItem`. Phase 3 is the single commit where behavior can move, which is why
its gate includes a manual words check *before* Phase 4 builds on top. The mobile widening is
kept out of Phase 3 deliberately: it is the one intentional behavior change, and it should not
share a commit with the phase whose acceptance is "nothing changed".

**Branch:** off `dev`, PR into `dev`. Never `main`.

## 11. Stop conditions

Stop and ask if any of these is true:

1. **Collapsing `wordsOpen`/`moreOpen` into `openMenuKey` changes any observable behavior of
   the «المزيد» menu** — in code reading or in T501's walk. Bring the specific interaction and
   both behaviors. (Commission-named stop.)
2. **A `NAV_ITEMS` consumer exists that §5.1's census missed and that the `children` or
   `queryParams` field would break.** The census found seven, all top-level-field readers; an
   eighth is a stop, not a workaround. (Commission-named stop.)
3. **T101's baseline is not green.** A baseline failure is not a starting point.
4. **Words parity fails in T501 and the fix would change the generic branch's contract**
   (§4.2-3/-4/-5) rather than its implementation — that means the generalization design is
   wrong, not the code.

**Flagged, not a stop — the user should see it before execution:** §6a rows 3 and 5 (الأرشيف
unlit during archive search; الرئيسية unlit while a door is selected) and §4.2-9 (the desktop
«الأبواب» trigger stops navigating). All three follow from locked decisions and shipped words
precedent; they are surfaced here because they are the visible consequences a user meets first.
