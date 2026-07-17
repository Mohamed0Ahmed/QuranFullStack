# Feature 030 — Explorer & Overlay Polish Batch — Verification Record

- **Branch:** `restyle/flat-green-light`. Not merged; no PR opened.
- **Date:** 2026-07-17.
- **Scope:** the 9 planned items (C1, N1–N8). N8 was cross-stack; everything else frontend-only.

## Commits (one per phase, in plan order)

| Phase | Commit | Items |
|---|---|---|
| P0 | `fe10762` | plan: bake the N8 cross-stack decision |
| P1 | `17c7f07` | N1 active-chip no-op · N5 dropdown focus gating |
| P2 | `97a25a8` | N2 fixed modal geometry · N6 kind/count header |
| P3 | `240c943` | N4 3-chip count ranges · Enter-commit |
| P4+P7 | `08d2e84` | N3 (12 of 13 rows) · N8-FE column-header sorting |
| P6 | `0623d12` | N8-BE asc/desc sort contract (the hard gate) |
| P4 | `50abe07` | N3 row 5 — list/selection states stop pushing the grid |
| P5 | `44fad08` | N7-b hover calibration |

P4 and P7 share one commit: both rewrite the word-types table and page, so their hunks
cannot be split by path. Every other phase is independently revertible.

**Ordering gate honoured:** N8-BE (`0623d12`) landed and was verified before N8-FE,
because the new frontend emits tokens an old backend rejects. The reverse (old frontend
against the new backend) is safe — legacy tokens are aliases.

## Automated gates

| Gate | Result |
|---|---|
| Backend suite (`dotnet test Backend/QuranDashboard.sln`, Testcontainers) | **1532 passed / 0 failed / 0 skipped** |
| Frontend suite (`npm test`) | **152 files / 1754 tests passed** |
| Backend build | **0 warnings / 0 errors** (baseline was 0) |
| Frontend production build (`npm run build`) | **succeeds** |
| `Backend/scripts/check-api-contract` | **"API contract up to date."** (exit 0) |
| `git diff --check` | clean |

Two frontend component-style budget **warnings** remain, both the mushaf U1 pattern:
`selected-word-section` 4.32 kB (pre-existing, untouched) and `selected-ayah-section`
4.38 kB (new; the same pattern its precedent sibling already exceeds the budget with).
Trimming either means cutting load-bearing reservation logic.

## Browser verification (light theme, real app against the dev server)

| Check | Evidence |
|---|---|
| **N8-FE cycle** | المواضع header: `?sort=occurrences` → `?sort=occurrences-asc` → released. Bare token = natural direction, suffix = opposite, exactly per contract. |
| **N8 alias equivalence** | `occurrences` and `occurrences-desc` return byte-identical rows against a freshly built merged backend (curl). |
| **N3 row 5** | An explorer error renders INSIDE the table shell with the header row still mounted; the grid does not move. |
| **N3 row 6** | `explorer-result-count` shows its muted `—` placeholder on error instead of rendering nothing. |
| **N7 hover** | Hovering one word paints ALL words of that ayah, across wrapped lines; the ayah marker is excluded; selection still wins on a word that is both. |
| **N7-b calibration** | Measured — see below. |
| **N6 header** | Back · kind chip (`صيغة معجمية`) · title · reserved count · Close, no overflow at depth > 1. |
| **N2 phone band** | All four ≤767px rules compiled and live. |
| **Quran rendering** | Al-Fatiha renders correctly — glyphs, ayah markers, surah ornament unchanged. |

### N7-b resolved by measurement, not by eye

The intensity ladder cannot be read off the percentages: selection mixes 16% into
`--qd-surface` (L 0.994) while hover mixes into `--qd-bg` (L 0.967), and the different
bases nearly cancel the gap. Measured against the real canvas and the real selected-word
wash (L 0.913):

| mix | ΔL vs canvas | ΔL vs selection |
|---|---|---|
| 10% (as first shipped) | 0.048 | **0.006 — indistinguishable from selection** |
| 12% (top of the sanctioned band) | 0.057 | **−0.004 — ladder INVERTS** |
| **8% (shipped)** | **0.038** | **0.016** |

8% keeps hover perceptible against the canvas (~1.7× the 0.022 baseline that made
`--qd-surface-hover` unusable here) while staying visibly below selection.

### N2 phone header budget, recomputed from measured widths

Measured intrinsic widths: Back 82.9px · kind chip 81.5px · count 96px (6rem) ·
Close 63.8px · gaps 8px.

- **Before the fix @390px:** 326px available vs 356.2px needed → ~30px overflow with the
  title already collapsed to zero, and the shrink landing on Back/Close (they were not
  `flex-shrink: 0`), wrapping their Arabic labels.
- **After the fix @390px:** 350px available vs 242.7px needed → the title keeps ~107px.

## Deviations from the plan

1. **N8 alpha tie-break (user decision).** The plan's "ALWAYS append
   `FirstWordOrderInMushaf → Id`" rested on a false premise: alpha's existing chain is
   text → Id with no mushaf tie-break, and it is already deterministic (`Id` is unique).
   Following the plan literally would have reordered existing `sort=alpha` links.
   Alpha keeps its chain; `StemsListReadTests`' pinned sequence passes unmodified.
   Unique-words alpha likewise keeps its own pre-existing mushaf tie-break, and gained
   the `.ThenBy(Id)` it was missing.
2. **N3-b (user decision) refined by implementation.** The table-shell variant is right
   for `error`/`empty` (list states) but wrong for `notFound` (a panel/selection state) —
   it would have hidden a populated table. `notFound` went to the detail panel instead.
   On lemmas/stems the panel already rendered it, so the page banner was a duplicate
   `role="status"`; deleting it removed a double announcement.
3. **N8 Swagger.** The plan assumed `<param>` docs existed to edit; four of five
   controllers had none. The real trap was CS1573 (documenting one param flags every
   undocumented sibling), resolved by documenting every param.
4. **N8-d/e, N4-a, N7-a, N2-a, N6-a/b, N3-a/c/d** shipped as decided.

## Known residuals (deliberate, evidenced)

1. **N3 row 5 — unique-words `notFound`/`restored-error` still shift.**
   `buildRestoredWordNotFound` sets `isOpen: false`, so the ≤1023px modal never renders
   and the desktop inline panel shows the select-a-word prompt; hosting the message there
   would silently drop it below desktop. Closing it means flipping that state contract
   (beyond N3's template/SCSS-only scope) and would pop a modal backdrop for a word that
   does not exist. Recorded under N3-b in the plan.
2. **Unique-words `.ThenBy(Id)` is unreachable.** `FirstWordOrderInMushaf` carries a DB
   unique index, so the tie it guards cannot occur. Harmless hardening; not behaviourally
   provable.
3. **Sortable header labels sit ~5px inward** of adjacent plain header labels (the sort
   button's own padding + border). Cosmetic.
4. **N3 row 9's `min-block-size: 52rem`** was derived arithmetically; marker-dense pages
   may run slightly taller. `min-*` cannot clip, so the failure mode is a residual shift,
   not lost content.

## Outstanding — needs a human pass

- **Dark theme.** All new colour comes from theme-mapped tokens and resolves
  automatically, but nothing here was reviewed by eye in dark. The N7-b calibration above
  was measured in **light only**; dark uses the same 8% and its own base tones, so the
  ladder should be re-measured there.
- **Reduced motion, keyboard, and the 768px band** were covered by unit tests and code
  review, not by a live pass.
- **Layout-shift overlay** at 1440/768/390 for the N3 rows not observed live (rows 1–4,
  8–12, 14).
