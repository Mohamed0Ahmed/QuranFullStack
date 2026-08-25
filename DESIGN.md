# Design Transition

## Status

The Angular UI is currently being rebuilt in owner-reviewed phases. No permanent
design system or visual rule set is active during this transition.

Historical palettes, layouts, component treatments, and visual documentation are
not authorities for new work. The owner's explicit direction controls each active
phase. The current Angular implementation is implementation truth for shipped
behavior, but it is not yet the permanent design contract.

## Functional Boundaries During the Rebuild

- Preserve Quran rendering and source integrity under `CODING_PRINCIPLES.md` §10.
- Preserve Arabic-first RTL behavior, accessibility, responsive operation,
  authentication, permissions, navigation, and data contracts unless an approved
  phase explicitly changes them.
- Do not extract or declare new permanent visual rules while the interface is still
  being shaped.

## Completion

After the full interface is reviewed and approved, extract the final tokens,
component contracts, layout conventions, interaction patterns, and verification
rules from the accepted Angular implementation. That later work will replace this
transition note with the permanent design documentation.
