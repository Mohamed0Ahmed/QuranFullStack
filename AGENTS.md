<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan
<!-- SPECKIT END -->

## Workspace Project Instructions

This repository is a FullStack workspace.

The root `AGENTS.md` contains general instructions that apply to the whole workspace.

When working on the Backend project, also read and follow:

- `Backend/AGENTS.md`

When working on the Frontend project, also read and follow:

- `Frontend/quran-dashboard-ui/AGENTS.md`

If a task touches both Backend and Frontend, read all relevant instruction files before making changes.

If a project-specific instruction conflicts with a root instruction, follow the more specific project instruction unless it would violate a root safety or product rule.

## Coding Principles

Before any implementation work, read and follow:

- `CODING_PRINCIPLES.md`

These principles apply to the whole FullStack workspace. Project-specific instruction files may add more detailed rules for Backend or Frontend work.

## Engineering Review Skill

For engineering/code reviews, use the project review skill:

- `.claude/skills/engineering-review/SKILL.md`

OpenCode can use:

- `.opencode/command/engineering-review.md`

## Design Context and Impeccable

Before any UI, layout, visual design, or dashboard screen work, read
`PRODUCT.md` and `DESIGN.md`.

- `PRODUCT.md` defines the product strategy and dashboard principles.
- `DESIGN.md` defines the current visual direction.
- Use Impeccable only for UI, design, layout, critique, and polish tasks.
- Do not run Impeccable commands automatically.
- Only use Impeccable when the user explicitly asks for it or when a UI/design
  task clearly requires it.
- Do not use Impeccable for backend work, data processing, API design, or other
  non-UI tasks.
- Impeccable must not expand scope, add features, or invent Quran content.
- For Quran-related UI, never invent Quran text, tafsir, translations,
  morphology, counts, or scholarly claims.
- Any UI work must preserve the product direction: Arabic-first, RTL, calm
  scholarly dashboard, restrained color, parchment and ink feel, Naskh display
  plus Arabic sans UI, no generic SaaS template, no kitschy religious
  decoration, no gamified consumer style, and no dense enterprise greige.
