# Contract: Expected Timeline Generation + 409 (Story 3)

**Source**: Master Plan §18.2 step 3 / exit. Governs optimistic concurrency across the whole
Abwab timeline.

## Obligations

- **Every** mutation port/command **and every actionable read** MUST carry an
  `ExpectedTimelineGeneration`. A **foundation contract/source test fails** if any of them
  omits it (including representative security and personal commands).
- On a mutation whose `ExpectedTimelineGeneration` no longer matches the current generation,
  the system MUST return the **exact 409** conflict `abwab.timeline_generation_stale` (§11)
  **before any row mutation** — no partial write, no side effect.
- This holds even when the command's **target row/revision was untouched** (a generation
  advance elsewhere still invalidates the command).

## Generation state

- Singleton monotonic generation using the `uint`/xmin convention.
- Exactly one immutable **generation-zero `TimelineGenerationBoundary`** root is seeded by
  migration. Only `033`'s sealed restore transaction may insert non-root boundaries.
- ChangeSet generation stamping is **immutable**.

## Test anchors

- Contract/source test: enumerate all mutation ports/commands + actionable reads → each must
  declare `ExpectedTimelineGeneration`.
- Behavior test: advance generation, replay an old command → exact 409
  `abwab.timeline_generation_stale`, **0 rows mutated**, including a fixture whose target
  row/revision was never touched.
- Seed test: exactly 1 gen-zero root; root edit/delete/duplicate all fail.
