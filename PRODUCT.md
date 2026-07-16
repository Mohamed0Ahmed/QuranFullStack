# Product

## Register

product

## Users

Admins, supervisors, and teachers who curate Quranic research content for the
منهج قرآني (Manhag Qurany) methodology. They are Arabic-speaking subject-matter
people, not casual end users. Their context is focused desk work: long review
sessions organizing structured material, checking that each ayah is linked to
the right place, and getting content ready to publish. They value accuracy,
clarity, and not fighting the tool.

## Product Purpose

A research and content-management dashboard for a structured Quranic
methodology. It exists to turn scattered research into a trustworthy,
publishable body of work:

- Manage Quran research data.
- Review ayah links (verify each verse is connected to the correct topic).
- Organize gates (أبواب), the thematic spine the content is structured around.
- Prepare and publish reviewed content.

Success looks like curators moving through review and organization confidently,
with the structure always legible, edits feeling safe, and published output
being accurate and well-formed.

## Brand Personality

Scholarly and calm. Reverent, focused, unhurried. The interface should feel like
a quiet archive or a serious research workspace, not a busy SaaS app. Voice is
precise, respectful, and plain. Three words: scholarly, calm, trustworthy. The
emotional goal is confidence and sustained focus.

## Visual Identity

The official visual identity is the **flat parchment + single scholarly-green**
direction, locked in the approved static comps under `docs/design-preview/`
(read its `README.md`; the divergence list there is the record of what changed).
`DESIGN.md` is the design system of record, with the token contract in
`Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md`.

> **Superseded (historical).** The previous visual source of truth was the
> **Real Pages prototype** (`/projects/Real Pages`, brand reference
> "الباحث القرآني"): a navy + gold + parchment identity with soft card
> elevation and hover lifts, which itself superseded an earlier exploratory
> green / teal / petrol chrome direction. Its extraction report remains at
> `Frontend/quran-dashboard-ui/report/ui/real-pages-visual-system-extraction-report.md`
> as historical reference.

The identity is **flat parchment + ink + one scholarly green**:

- **Warm parchment light mode.** A calm Quranic research workspace on a warm
  parchment canvas with near-white content cards — structure carried by
  hairline borders, not elevation.
- **Fully flat in light.** No resting card shadows, no hover lifts, no
  gradients, no navbar blur. A single shadow exists only on floating layers
  (dropdowns, popovers, modals, drawers).
- **One scholarly green.** A single muted green is both the structural/primary
  color (primary buttons, brand mark) and the accent (focus ring, selection,
  active states, links, icon highlights). Gold is retired; the old
  restrained-gold discipline carries over as an allowed-green list — green is
  used sparingly for state and emphasis, never as decoration.
- **The green thread.** A 2px green line or edge means "current" everywhere:
  the active tab, the selected row's inline-start edge, the selected mushaf
  word.
- **Navy is footer-only.** The footer remains the one deep-navy anchor — flat,
  with warm off-white text and a sage accent. Navy appears nowhere else in the
  light theme.
- **Clean flat navbar.** The top bar is an opaque light surface with a hairline
  bottom border — never a heavy colored bar, never translucent or blurred.
- **Calm, non-distracting motion.** Quick, subtle transitions only; no bounce, no
  showy animation; reduced-motion respected.
- **Quran text rendering stays sacred and stable.** Quran/Mushaf glyph fonts and
  rendering are unchanged and are **never animated**.

The Angular app stays **light + dark** only. The dark theme still runs the
previous navy + gold values and remains functional; reconciling dark to the
green direction is a deliberately deferred later task. Theme-neutral shape and
motion changes (flat navbar/footer geometry, lift removal, crisper radii)
already apply to dark.

## Anti-references

This should NOT look like any of the following:

- **Generic SaaS template.** Bootstrap-style admin themes, identical gradient
  stat cards, cookie-cutter dashboard layouts.
- **Kitschy religious decor.** Gold *filigree*, crescent moons, mosque clipart,
  overwrought ornamentation. Reverence comes from restraint, not decoration. (Note:
  this bans decorative gold *ornament*. Gold no longer appears as an accent color
  either — the restrained *accent color* in the Visual Identity above is now a
  single scholarly green, used sparingly for state and emphasis, never as applied
  decoration.)
- **Consumer / gamified.** Bright playful palettes, emoji, badges, reward-style
  UI. This is professional curation work.
- **Dense enterprise greige.** Cramped gray-on-gray tables with no breathing
  room. Joyless density is not the same as seriousness.

## Design Principles

1. **Reverence without ornament.** The gravity of the content shows through
   restraint, careful typography, and generous space, never through applied
   decoration.
2. **Calm for long focus.** Curators spend hours in review. Reduce visual noise,
   respect attention, and never gamify the work.
3. **Structure you can trust.** The gate and ayah hierarchy is the spine of the
   product. Make organization legible at a glance and make editing feel safe,
   with review clearly separated from publishing.
4. **Arabic-first, genuinely.** RTL layout and Arabic typography are the default,
   designed in from the start, not a mirrored afterthought. The interface should
   read naturally right-to-left.
5. **Earned familiarity.** Use standard, predictable tool patterns so curators
   move fast. Surprise has no place in the everyday review flow.

## Accessibility & Inclusion

- **RTL-first, Arabic-first.** Right-to-left layout and Arabic interface copy are
  the baseline. Direction-aware spacing, icons, and flow throughout.
- **Arabic typography care.** Legible Arabic faces, correct rendering of
  diacritics (tashkeel), and generous line height for Quran and research text.
- **Long-session legibility.** Comfortable contrast and sizing for extended
  reading and review work.
- **WCAG 2.1 AA** as the working baseline (confirm if a stricter target is
  required). Never rely on color alone to convey review or publish state.
- **Respect reduced-motion preferences;** motion conveys state only.
