# Domain Docs

How the engineering skills should consume this repository’s domain documentation.

## Before exploring, read these

- `CONTEXT.md` at the repository root
- Relevant ADRs under `docs/adr/`

If these files do not exist, proceed silently. Do not flag their absence or suggest creating them upfront. The domain-modeling workflows create them lazily when terminology or decisions are resolved.

## File structure

This repository uses a single-context layout:

/
├── CONTEXT.md
├── docs/
│   └── adr/
└── Backend/
└── Frontend/

`CONTEXT.md` contains the shared domain vocabulary for both Backend and Frontend. System-wide architectural decisions live under `docs/adr/`.

## Use the glossary’s vocabulary

When output names a domain concept—in an issue title, proposal, hypothesis, or test name—use the term defined in `CONTEXT.md`. Do not drift to synonyms the glossary explicitly avoids.

If a required concept is absent, reconsider whether the language belongs to the project or note the gap for domain-modeling work.

## Flag ADR conflicts

If output contradicts an existing ADR, surface the conflict explicitly instead of silently overriding it.
