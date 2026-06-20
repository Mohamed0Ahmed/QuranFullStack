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

The official visual identity is the **Real Pages prototype** (`/projects/Real
Pages`, brand reference "الباحث القرآني"), adopted **with adaptation** as the
visual source of truth. See the extraction report
`Frontend/quran-dashboard-ui/report/ui/real-pages-visual-system-extraction-report.md`.

The identity is **navy + gold + parchment**:

- **Warm parchment light mode.** A calm Quranic research workspace on a warm
  parchment canvas, with near-white elevated content cards lifting off it.
- **Deep navy structural identity.** Navy carries the structural/primary role
  (primary buttons, brand mark) and the footer.
- **Restrained gold accent.** A single muted gold accent for active states, links,
  icon highlights, and section eyebrows — used sparingly, never as decoration.
- **Premium dark footer / dark anchor sections.** The footer is a deep navy anchor
  with warm off-white text and gold accents; dark anchor surfaces give the page an
  end-cap and visual weight.
- **Subtle card elevation and hover movement.** Cards rest with a soft shadow and
  lift slightly on hover (small translate + stronger shadow/border). Controlled
  soft shadows are part of the identity, not an exception.
- **Clean light navbar.** The top bar stays light/near-white and clearly distinct
  from content, never a heavy colored bar.
- **Calm, non-distracting motion.** Quick, subtle transitions only; no bounce, no
  showy animation; reduced-motion respected.
- **Quran text rendering stays sacred and stable.** Quran/Mushaf glyph fonts and
  rendering are unchanged and are **never animated**.

This supersedes any earlier exploratory color direction (including the
green / teal / petrol chrome exploration): the official direction is now the
prototype's navy + gold + parchment. The Angular app stays **light + dark** only
(prototype *ivory* → light, *midnight* → dark; *sage* is not adopted). Implementation
details and the future token contract live in
`Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md` and `DESIGN.md`.

## Anti-references

This should NOT look like any of the following:

- **Generic SaaS template.** Bootstrap-style admin themes, identical gradient
  stat cards, cookie-cutter dashboard layouts.
- **Kitschy religious decor.** Gold *filigree*, crescent moons, mosque clipart,
  overwrought ornamentation. Reverence comes from restraint, not decoration. (Note:
  this bans decorative gold *ornament*. The restrained gold *accent color* in the
  Visual Identity above is a different thing and is allowed — it is used sparingly
  for state and emphasis, never as applied decoration.)
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
