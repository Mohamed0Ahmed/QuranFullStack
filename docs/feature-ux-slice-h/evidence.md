# Slice H — Evidence

Plan: `docs/feature-ux-slice-h/plan.md`. Branch: `ux-slice-h`, off `dev` @ `50b7eef5`.

## T101 — Baseline (dev @ `50b7eef5`, clean)

| Check | Command | Result |
|---|---|---|
| Frontend build | `npm run build` | Succeeded, 18.278s. Pre-existing budget warnings (initial bundle +69.58 kB over 500 kB budget; two mushaf SCSS files over their 4 kB budget) — none introduced by this slice, carried forward as-is. |
| Frontend tests | `npm test` | 193 files, 2343 tests passed, 0 failed. 181.25s. |
| **No backend gate** | — | No `Backend/` file is in scope for this slice (§4.1-8, §7). No `dotnet` command runs; no route-smoke tier; no `SmokeRouteCatalog` entry is owed. Stated here so the absence is legible as a decision, not an omission. |
| Label equality | `ABWAB_LABELS.templatesPageTitle` (`Frontend/quran-dashboard-ui/src/app/features/abwab/models/abwab.labels.ts:308`) | `'قوالب الأبواب'` — this is the exact string the «قوالب الأبواب» child label must equal (§4.2-6); confirmed by reading the source, not assumed. |

**Baseline verdict:** both green. T101's counts (193 files / 2343 tests) are the number T502
must reproduce unchanged — this slice writes no spec.

## T102 (of this evidence record) — Branch and feature record

- Branch `ux-slice-h` created off `dev` @ `50b7eef5`.
- Plan committed to the branch (not to `dev`), commit `4e184bd3`.
- Root `CLAUDE.md` "Active Spec Kit Feature" section updated: entry added for `ux-slice-h`.
  No planning-artifact sweep in this slice (§3, standing user decision).

## T102 — Reversal sweep

`grep -rn` across `src/`, `e2e/`, `docs/`, `.architecture/`, and the frontend READMEs for:
`nobody asked`, `nav-link--abwab`, `words-dropdown`, `wordsOpen`, `WordsNavItem`,
`WORDS_MENU_ITEMS`, and «الأرشيف» near nav copy.

Result — matches the plan-time prediction exactly, nothing new:

- `nobody asked` — one code hit, `abwab.routes.ts:21` (the comment T303 amends per §4.2-11);
  the rest are the historical audit (`docs/abwab-ux-audit.md`, untouched — historical record)
  and this plan's own text.
- `nav-link--abwab` — zero code hits outside the plan (the plan's own precondition note that
  nothing selects it).
- `words-dropdown` — `top-navbar.component.{html,scss,ts}` (the class this slice renames to
  `.nav-dropdown`, T302) plus the plan's own text. No test/e2e selector.
- `wordsOpen` — `top-navbar.component.{html,ts}` only (the field T302 collapses into
  `openMenuKey`), plus `docs/abwab-ux-audit.md` (historical) and the plan.
- `WordsNavItem` / `WORDS_MENU_ITEMS` — `words-nav-items.ts` (T201 retypes/deletes),
  `top-navbar.component.ts:8,38` (T301 rewires), `core/README.md:51` (T601 rewrites). No
  consumer outside these.
- «الأرشيف» near nav copy in `core/` — zero hits (the entry doesn't exist yet; this is the
  slice adding it).

Every hit is in §5.3's ledger or its do-not-touch list. No eighth consumer found (stop
condition 2 stays clear).

## Phase 3 — finding: `.nav-dropdown.open` outside-click selector self-closes on open

§4.2-5's literal text ("the handler checks the single `el.querySelector('.nav-dropdown.open')`")
was implemented as written, then caught by the browser check the Phase 3 gate requires ("words
dropdown behaviorally identical in a quick manual check"): clicking any trigger from closed
never opened it — the trigger's own `(click)` handler set `openMenuKey` synchronously, but the
`document:click` listener (same bubbling event, same synchronous task) ran before Angular's
change detection had applied the `[class.open]` binding, so `querySelector('.nav-dropdown.open')`
still found nothing open and immediately closed what had just opened. The original per-menu
handlers never hit this because they queried a **static** class (`.words-dropdown` /
`.more-dropdown`, always present) rather than the CD-applied `.open` modifier.

Original words/more handlers avoided the race by construction; the generalized version
introduces it because one shared selector now has to distinguish *which* dropdown is open,
and `.open` isn't the right thing to key on for that. Fix: each dropdown `<li>` also carries a
static `[attr.data-menu-key]="item.key"` (`"more"` for the more `<li>`), and the outside-click
handler queries `.nav-dropdown[data-menu-key="${openMenuKey}"]` instead — a static attribute
present from render, immune to the CD-timing race, while still resolving to exactly the one
open dropdown among several `.nav-dropdown` elements. Verified in the browser (click-to-open,
outside-click-close, hover-open, mutual exclusion, Escape, mouseleave, hover-then-click-closes
quirk — all pass) and confirmed the full suite is unaffected (193/2343, unchanged).

## T501 — The browser walk

Backend running against the local dev DB (Kestrel with the frontend's mkcert PEM, per the
cert-mismatch setup note — otherwise every API call reads as a backend failure); frontend on
`npm run start:https`. Desktop walk via `chrome-devtools-mcp` (real hover/click/keyboard
events, 1440×900); mobile walk via the same tool's viewport emulation (390×844, `isMobile`).
`resize_window`/`resize_page` alone did not change the actual CDP viewport in this
environment — the `emulate` tool's `viewport` parameter (CDP `Emulation.setDeviceMetricsOverride`)
is what worked; noted for any future browser-driven check in this repo.

**Words parity (acceptance for §4.1-2):**
- Hover opens (`mouseenter` on the `<li>`); click while hover-open closes — the shell-nav
  hover-then-click quirk (`shell-nav.e2e.ts:15-16`) reproduced exactly.
- `mouseleave` closes; confirmed live when clicking away from the trigger to a page button
  closed the dropdown before the click's own action ran.
- Escape closes (real `Escape` keypress via `chrome-devtools-mcp`).
- Outside-click closes; clicking the trigger itself when closed opens it (see the Phase 3
  finding above — this is the behavior the fix restores).
- Opening «المزيد» while another menu is open closes the other (single `openMenuKey` field,
  exclusion by construction) — verified both pairings.
- Child link click navigates and closes the menu.
- `aria-expanded` toggles; chevron rotates 180°.
- Words children testids present: `nav-menu-link--words-{home,unique,roots,lemmas,stems,types}`.
- Active state: `/dashboard/words/roots` lights the words parent trigger (`isMenuActive`,
  `paths:'subset'`), matching §6a row 6.

**Abwab dropdown:**
- Three children render in order with the locked labels: «الرئيسية», «قوالب الأبواب»,
  «الأرشيف» — screenshotted at 1440×900, RTL alignment (`inset-inline-start`) confirmed
  correct under the trigger, same as the words dropdown's shipped positioning.
- Every §6a row walked by URL, both flagged cells observed as predicted:
  - row 1 `/abwab`: parent ✅, الرئيسية ✅, قوالب الأبواب —, الأرشيف —
  - row 2 `/abwab?archive=1`: parent ✅, الرئيسية —, الأرشيف ✅
  - row 4 `/abwab/templates`: parent ✅ (subset), الرئيسية — (exact), قوالب الأبواب ✅ — page
    title reads «قوالب الأبواب — المنهج القرآني», confirming `ABWAB_LABELS.templatesPageTitle`
    is still the route title source, unaffected by the nav change.
  - row 3 `/abwab?archive=1&q=x` (archive search): parent ✅, all three children — including
    الأرشيف, the flagged cell — walked live; matches §6a exactly.
  - row 5 `/abwab?door=5` (a live-view param set): parent ✅, all three children — including
    الرئيسية, the flagged cell — walked live; matches §6a exactly.
  - Both flagged cells are consequences of the locked `queryParams:'exact'` decision (§4.2-7);
    seeing them unlit here is the confirmation the plan called for, not a defect.
- «الأرشيف» click lands on `/abwab?archive=1` and the archive view opens (confirmed via row 2's
  URL walk above — reached by direct navigation, equivalent to the click's own `queryParams`
  binding since both produce the same `UrlTree`).

**«المزيد» parity:** click-toggle confirmed (open triggers correctly after the Phase 3 fix),
no hover-open (untouched code path), Escape and outside-click confirmed via the interaction
sweep in the Phase 3 finding — identical to `dev`. No stop condition 1 trigger.

**Layering:** opened the abwab "باب رئيسي جديد" create-door modal; `<nav class="qd-navbar">`
gained `inert=""` and `aria-hidden="true"` while the modal was open — the Chrome-inert
wiring (`.ts` unchanged, `.html:5-6` unchanged) is intact. No §17 amendment needed.

**Mobile (390×844, real viewport emulation):** hamburger menu replaces the desktop nav;
opening the panel shows both sublists nested under their parents in the locked order — words'
six children under «الكلمات والجذور», the three abwab children under «الأبواب» — screenshotted
and visually confirmed. Indentation measured at 32px (`padding-inline-start: var(--qd-space-6)`).
Parent rows remain independently clickable links (clicking «الأبواب»'s own row navigated to
`/abwab` and closed the panel); child clicks navigate and close the panel. Console checked via
`chrome-devtools-mcp`'s `list_console_messages` after the full interaction sweep and again
after a fresh reload — empty both times, no NG0955 duplicate-key warnings (children track
`child.key`, per DRIFT-3).

## T502 — Tier B/C

| Check | Command | Result |
|---|---|---|
| Frontend tests | `npm test` | 193 files, 2343 tests passed, 0 failed. 182.47s. Identical to T101's baseline — this slice writes no spec. |
| Frontend build | `npm run build` | Succeeded, 17.802s. Same pre-existing budget warnings, bundle total 571.29 kB (+1.71 kB vs T101's 569.58 kB — the new `nav-menu.ts` module and mobile-sublist markup/CSS; not a new warning category). |
| `shell-nav.e2e.ts` (optional, evidence only per §4.1-8 — never a tier) | `npx playwright test e2e/shell-nav.e2e.ts --project=default` | 3 passed, 6.1s. Unedited file, all three flows green: Mushaf link, words-dropdown-to-hub (hover), more-dropdown-to-mutashabihat. |
| No backend command | — | Not run — no `Backend/` file in scope (§4.1-8). |

**Verdict:** both gates green, counts unchanged from baseline. No regression.

## T601 — READMEs and §17

`core/README.md:44-52` rewritten: the nav menu model is now described as three files
(`nav-items.ts` flat registry, `words-nav-items.ts`, `nav-menu.ts` composition module), the
import-cycle reason children live outside `NAV_ITEMS`, and «الأرشيف» as the app's first
query-param nav entry. `words-nav-items.ts:15-16`'s comment clause was already extended during
T201 (the "Consumed as `NavItem[]` via `nav-menu.ts`" sentence) — no further change needed.

§17 verify-only pass: grepped `UI_STYLE_SYSTEM.md` for the old class names
(`words-dropdown`/`more-dropdown`/`nav-dropdown`/`wordsOpen`/`moreOpen`) — zero hits, the
"Sticky app chrome" and "Chrome-inert rule" entries describe z-rungs and inert wiring only,
neither of which this slice touches. §5C's navbar visual rules (color/hover/pill) are
untouched by a structural change. No amendment made — matches the plan's expectation exactly.

## T602 — Debt and close-out

`docs/TESTING_DEBT.md` gained the `ux-slice-h` section (rows H1-H4, verbatim from plan §7).

Re-ran T102's sweep across `src/`, `e2e/`, `docs/`, `.architecture/`, and the frontend READMEs:
every hit is either historical (`docs/abwab-ux-audit.md`, other slices' closed plans), this
slice's own plan/evidence text, or expected live code (`WORDS_MENU_ITEMS` still exists,
retyped per §4.2-2, not renamed; `core/README.md`'s own new description; `nav-menu.ts`'s
الأرشيف entry). No stray `wordsOpen`, `words-dropdown`, or `nav-link--abwab` survives in live
code.

Root `CLAUDE.md` Active Spec Kit Feature cleared back to `None`. No planning folder deleted,
swept, or repointed — deferred to the post-Slice-I cleanup pass per standing decision. No
package install. No `dev → main` merge.

Dev servers (backend on 5015/5014, frontend already running on 4200 before this session)
left as found — the frontend server was pre-existing and reused throughout; the backend
instance started for this verification was stopped after T602.
