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
