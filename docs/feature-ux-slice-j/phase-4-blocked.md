# Slice J — Phase 4 (J8 badge header) stopped at its own stop condition

**Status:** blocked, nothing shipped. Phases 1–3 landed; Phase 4 was not started beyond
inspection. Recorded 2026-08-02.

## The stop condition that fired

`plan.md` §2.3: *"Alignment is structural (shared grid), not approximate. **Stop condition:**
if implementation cannot make header cells and row cells share one grid template (e.g. the
actions/flag cluster forces divergent row layouts), STOP and report — do not ship eyeballed
alignment."*

Restated in §6 as stop condition (1).

## Why it cannot be done as scoped

The plan assumed the header could be a sibling `<div>` reusing the row's grid template. The
row is not a grid, and two properties of its current layout defeat a shared template:

1. **The row is depth-padded flex.** `.abwab-tree__row` is `display: flex` with
   `padding-inline-start: calc(var(--abwab-tree-depth, 0) * var(--qd-space-5) + var(--qd-space-2))`
   (`abwab-tree.component.scss:8-17`). Every depth level starts its content at a different
   inline offset, so no leading track is shared row-to-row.
2. **The trailing cluster has intrinsic, undeclared widths.** `.abwab-tree__flags` and
   `.abwab-tree__actions` are both `flex: none` with content-derived widths
   (`abwab-tree.component.scss:137-165`). The badge group's distance from the row's
   inline-end is therefore whatever those two clusters happen to measure. A sibling header
   has no way to reproduce that offset from CSS alone.

The badge group itself is fine — a fixed `repeat(3, var(--w))` grid at `flex: none` does land
at an identical position on every row, exactly as the plan says. The problem is only that a
**header outside the rows** cannot find that position.

## The two ways through, and why neither was taken unilaterally

**(a) Convert the row to subgrid — the correct fix.** Make `.abwab-tree` the grid, give each
row `grid-template-columns: subgrid`, and move the depth indent off the row box onto its first
cell. Rows and header then share real tracks and alignment is structural by construction.

Not taken because it rewrites the row layout rather than wrapping three spans (the plan scoped
4.1–4.2 as a badge-group wrap), and because that layout is what the truncation contract's
measured budget cites — `UI_STYLE_SYSTEM.md`'s truncation entry records the name holding
~184px beside all three badges at 1024px. Changing how the name column is sized invalidates a
measured number that would have to be re-measured in a browser, and no browser path was
available this session (see the DoD note below). Shipping an unverifiable change to a measured
invariant is worse than shipping nothing.

**(b) Duplicate the trailing clusters as hidden placeholders.** Render
`.abwab-tree__flags` / `.abwab-tree__actions` copies in the header with `visibility: hidden`
content so their intrinsic widths match.

Not taken because it borrows the widths by coincidence rather than by contract: it silently
misaligns the moment anyone edits `relationsFlagLabel` or the `＋`/`⋯` glyphs. That is
eyeballed alignment with extra steps, which is the thing the stop condition forbids.

## Recommendation

Scope option (a) as its own slice, with the row-layout change stated up front and a
re-measurement of the truncation budget at 1024px in its acceptance criteria. It is a tree
layout change that happens to enable a header, not a header change.

## What was deliberately NOT done alongside

**The «ع» prefix stays.** `rowDepthBadge` still returns `` `ع${depth}` ``
(`abwab.labels.ts`). The plan removes it *because the header disambiguates the three columns*
— `abwab.labels.ts:137`'s own comment says a bare numeral "would read as a fourth count."
With no header, that justification does not hold, so the prefix ships with the header or not
at all. Same reasoning retires 4.1 (a grid group nothing reads), 4.6, and 4.7.

Phase 4 is therefore blocked in full, not in part.
