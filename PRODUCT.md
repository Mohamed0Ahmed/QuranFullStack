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
- Review ayah links and verify that each verse is connected to the correct topic.
- Organize gates (أبواب), the thematic spine the content is structured around.
- Prepare and publish reviewed content.

Success looks like curators moving through review and organization confidently,
with the structure always legible, edits feeling safe, and published output
being accurate and well-formed.

## Product Voice

Product language is precise, respectful, calm, and plain. It supports sustained
research work without distracting from the content or hiding uncertainty.

## UI Rebuild Status

The Angular interface is being rebuilt in owner-reviewed phases. No permanent
visual design authority is active during this rebuild. Each phase follows the
owner's explicit direction, while the current Angular code remains the source of
truth for behavior that is already implemented.

Permanent design rules, tokens, and component contracts will be extracted and
documented only after the complete interface has been reviewed and approved.

## Product Invariants

- Arabic-first and right-to-left behavior remain functional across the product.
- Accessibility and responsive behavior remain product requirements.
- Existing routes, permissions, data contracts, and safe editing workflows stay
  intact unless an approved phase explicitly changes them.
- Quran text, glyphs, markers, fonts, word boundaries, and source data are
  protected. `CODING_PRINCIPLES.md` §10 is the canonical Quran data-safety
  authority, and the implicated renderer and source code own implemented truth.
- Missing or uncertain Quran-related data is reported, never invented or silently
  corrected.

## Accessibility & Inclusion

- Arabic interface copy and right-to-left navigation are the baseline.
- Controls remain keyboard operable and expose clear accessible names and states.
- Text and controls remain legible for long review sessions.
- WCAG 2.1 AA is the working accessibility baseline unless the owner requests a
  stricter target.
- Motion respects reduced-motion preferences and never changes Quran rendering.
