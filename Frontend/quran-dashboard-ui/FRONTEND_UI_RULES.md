# Frontend UI Rules (mandatory)

The short, non-negotiable rules for any UI change in `Frontend/quran-dashboard-ui`. Deep
mechanics stay in `.architecture/UI_STYLE_SYSTEM.md`; the permanent visual authority is
`.architecture/golden-ui/`, whose `GOLDEN_VISUAL_VERIFICATION.md` owns how a UI-visible change is
verified. This file wins over habit, not over the Golden contracts or the repository kernel.

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
rule, or renderer boundary outside the exact linking-selection exception below. Generic CSS and
generic components must not reach Quran renderer descendants. Re-measure geometry against the real
Arabic faces before accepting a board pixel.

The linking ayah-selection card has one approved Compact-only display exception: reduce its existing
word size by `--qd-s-2`, use `1.55` line-height, and add `--qd-s-2` block padding to background-only
word highlights. It changes no font, text, glyph, word boundary, source data, or other Quran surface.

Door highlighting has one approved visual exception: a highlighted word uses its assigned door token
behind unchanged Quran text, inset by the 10px `--qd-mushaf-door-highlight-inset` from both block
edges. A word or ayah marker belonging to multiple selected doors uses fixed multi-door gradients
independent of assigned colors: a light background gradient for the word and a darker readable
gradient for the marker glyph. In forced-colors, solid block edges identify a single-door word and
a system-color underline identifies a single-door marker; dashed perimeters identify multi-door
words and markers. The treatment never changes or animates the font, text, glyph shape, word
boundaries, or line metrics.

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

Never write a raw responsive threshold, in px or rem. The legacy `360/420/640` constants are gone
repository-wide (D11) and `npm run check:golden-ui` scans every stylesheet under `src/`; adding one
back is a defect.

## 5. One gutter, four page intents

Only the page shell applies inline gutters: `16 / 24 / 32 / 40px` at Compact / Medium / Wide /
Wide-plus. `.qd-page` carries block rhythm only. `.qd-page-shell` holds the only
`padding-inline: var(--qd-page-gutter)` declaration in the tree and the checker fails on a second
one; a feature frame, explorer stylesheet, or nested surface may never add another, and page-level
horizontal scrolling is a defect in every mode.

Each route declares exactly one named intent: `capped-reading` (72rem), `full-data` (100rem),
`split-workspace` (100rem), `protected-mushaf` (feature-owned). Rails are `16 / 18 / 20rem` and
appear only in Wide.

## 6. Prohibited effects

No gradients outside the fixed multi-door Mushaf word and ayah-marker highlight defined in §3,
glass, resting card shadows, hover lifts, active-state translation, decorative entrance motion,
decorative imagery, or gamification. Shadow exists only on floating layers (`--qd-shadow-layer`).
Motion is state feedback at 120–160ms and honours `prefers-reduced-motion`.

Green is state, never decoration: solid green is the single primary action, tint is current/selected,
and a 2px logical `border-inline-start` thread marks selection. Generic hover is
`--qd-surface-quiet`. Every status carries a label plus icon/shape, never colour alone.

## 7. Direction

Layout uses logical properties only (`inline-start/end`, `margin-inline`, `padding-inline`,
`border-inline-start`, `inset-inline`). Latin values are isolated at the value element with
`.qd-ltr-isolate`; they never reverse a container.

## 8. Five async owners, and no adapter

Async state is five separate owners — skeleton, refreshing, empty, error/notFound, notice — each
with its own role/live-region and geometry contract. The `qd-state` adapter that conflated the
first three was deleted in Plan 7 Phase 11 at zero consumers; `npm run check:golden-ui` fails on
any `<qd-state` / `QdStateComponent` reference and on `src/app/shared/ui/state/` reappearing. Pick
the owner, never a variant flag.

## 9. Authority boundary

Active Spec Kit artifacts own feature intent, implementation code owns current behavior, and the
triggered `.architecture/` sources own structural and visual rules. Production-source comments stay
forbidden by default under `CODING_PRINCIPLES.md` §2. Do not create or update a code-area README.

## 10. The gate

`npm run check:golden-ui` enforces one band truth, one token truth, the single route-gutter owner,
the prohibited effects, the retired `qd-state` adapter, and three boundaries worth stating exactly,
because a documented guarantee the gate does not deliver is worse than none:

- **Modal widths.** `modal-shell.component.scss` must declare exactly the four named variants, and
  no Golden-layer stylesheet may give a modal-named selector its own inline-axis size (`width`,
  `min/max-width`, `inline-size`, `min/max-inline-size`). Colour, padding and other non-geometry
  rules on projected dialog content stay legal; a competing dialog geometry fails with its file,
  line and selector. Component stylesheets outside the Golden layer are not scanned for this.
- **Resting elevation.** Across **every** stylesheet under `src/`, a `box-shadow` reading any
  `--qd-shadow*` token fails unless it is `--qd-shadow-layer` on a declared floating owner (modal
  shell, floating layer, nav menu, the detail-shell restore button). Widening that list is a
  deliberate edit to the checker.
- **Quran renderer boundary.** Golden-layer stylesheets only, but on a normalized selector list:
  multi-line lists are split across newlines and pseudo-classes/elements are stripped before the
  protected-name match, so `.mushaf-line:hover` and `.a,\n.mushaf-word` are both inspected.

It also scans **every** stylesheet under `src/` for a raw responsive threshold, in px *or* rem. Its
pattern allowlist contains only the two permanent colour-literal boundary entries and the two fixed
multi-door gradient declarations in the token owner. A separate consumer check permits each gradient
token exactly once in the Mushaf word renderer and rejects every other consumer.
