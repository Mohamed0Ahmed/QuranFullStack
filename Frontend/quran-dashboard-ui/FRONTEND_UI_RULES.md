# Frontend UI Rules (mandatory)

The short, non-negotiable rules for any UI change in `Frontend/quran-dashboard-ui`. Deep
mechanics stay in `.architecture/UI_STYLE_SYSTEM.md`; the permanent visual authority is
`.architecture/golden-ui/`. This file wins over habit, not over the Golden contracts or the
repository kernel.

## 1. Ownership ladder — pick the smallest owner that fits

1. CSS variables in `src/styles/_tokens.scss` own semantic values. Nothing else declares a colour,
   radius, shadow, spacing step, control height, gutter, rail, measure, or modal width.
2. Tailwind, configured from those tokens, is the default template styling mechanism.
3. Small `qd-*` global classes own cross-feature *meaning* utilities cannot express (page intents,
   surface levels, selection thread, hit-area expansion, LTR isolate, status semantics).
4. Shared Angular components/directives own repeated visual **and** interaction contracts.
5. Feature components own domain composition, labels, data, URL state, permissions, and genuinely
   different renderers.
6. Specialised SCSS stays valid where it is materially clearer or safer: complex selectors,
   pseudo-elements, state precedence, scroll/sticky/fixed/`dvh`/safe-area geometry, third-party or
   projected content, reduced motion, browser quirks, and all Quran/Arabic rendering.

No `@apply` rewrites. No aliasing of readable direct utilities. No universal page/table/list/modal/
workspace component whose inputs encode feature names or domain booleans.

## 2. Scope of the current cycle

Light/day mode only. The existing dark overrides stay present and unreviewed; do not add Golden dark
values and do not edit the theme toggle. A dark mismatch is recorded, never "fixed" in passing.

## 3. Fonts and protected content

Use the currently approved project fonts. The Golden boards' preview faces authorise nothing. Never
add a font package, never touch a Quran font, glyph mapping, line metric, ligature helper, marker
rule, or renderer boundary. Generic CSS and generic components must not reach Quran renderer
descendants. Re-measure geometry against the real Arabic faces before accepting a board pixel.

## 4. Responsive bands — one vocabulary

`src/app/shared/layout/breakpoints.contract.json` is the only source of band values. TypeScript and
Tailwind read it directly; `src/styles/_breakpoints.scss` is the Sass adapter and is checked against
it.

| Band | Range | Meaning |
|---|---|---|
| Compact | `<= 767` | single column, sheet navigation |
| Medium | `768–1079` | a designed mode, never a squeezed Wide |
| Wide | `>= 1080` | desktop navigation, rails, splits |
| Wide-plus | `>= 1440` | measure enhancement only, not a fourth structure |

Never write a raw pixel threshold. Legacy `360/420/640` constants are being removed phase by phase;
adding one is a defect.

## 5. One gutter, four page intents

Only the page shell applies inline gutters: `16 / 24 / 32 / 40px` at Compact / Medium / Wide /
Wide-plus. `.qd-page` carries block rhythm only. A feature frame, explorer stylesheet, or nested
surface may never add a second inline gutter, and page-level horizontal scrolling is a defect in
every mode.

Each route declares exactly one named intent: `capped-reading` (72rem), `full-data` (100rem),
`split-workspace` (100rem), `protected-mushaf` (feature-owned). Rails are `16 / 18 / 20rem` and
appear only in Wide.

## 6. Prohibited effects

No gradients, glass, resting card shadows, hover lifts, active-state translation, decorative
entrance motion, decorative imagery, or gamification. Shadow exists only on floating layers
(`--qd-shadow-layer`). Motion is state feedback at 120–160ms and honours `prefers-reduced-motion`.

Green is state, never decoration: solid green is the single primary action, tint is current/selected,
and a 2px logical `border-inline-start` thread marks selection. Generic hover is
`--qd-surface-quiet`. Every status carries a label plus icon/shape, never colour alone.

## 7. Direction

Layout uses logical properties only (`inline-start/end`, `margin-inline`, `padding-inline`,
`border-inline-start`, `inset-inline`). Latin values are isolated at the value element with
`.qd-ltr-isolate`; they never reverse a container.

## 8. `qd-state` does not grow

`src/app/shared/ui/state/` is a temporary compatibility adapter for the five async owners
(skeleton, refreshing, empty, error/notFound, notice). New code imports the canonical owners.
`npm run check:golden-ui` fails when the adapter's call-site count rises above the recorded
baseline, and the baseline may only fall.

## 9. Nearest README duty

Before changing an area, read its nearest README, and update that README in the same change when the
truth it describes moves. Production-source comments stay forbidden by default: durable rationale
belongs in the nearest README or in this file.

## 10. The gate

`npm run check:golden-ui` enforces one band truth, one token truth, the page-shell contract, the
prohibited effects, and the `qd-state` baseline. Its legacy allowlist is explicit, dated to the
phase that retires each entry, and may only shrink.
