---
name: engineering-review
description: >-
  Review-only engineering/code review for the Quran Dashboard FullStack workspace
  (.NET backend + Angular frontend). Use this skill whenever the user asks to
  review code, a diff, a PR, a branch, or a change, or asks whether a change is
  ready to merge, follows the coding principles, or respects the architecture,
  even if they don't say the word "review". It checks Clean Code, SOLID,
  DRY/KISS/YAGNI, separation of concerns, backend/frontend architecture, strong
  typing, focused scope, error handling, Quranic data safety, and
  build/test/report verification, then returns a structured verdict. This is a
  review skill only: do not implement fixes unless the user explicitly asks.
---

# Engineering Review Skill

Use this skill to review code changes in the Quran Dashboard FullStack workspace.

This skill is for review only. Do not implement fixes unless explicitly asked.

## Required Context

Before reviewing, read:

- CODING_PRINCIPLES.md

If the change touches Backend, also read:

- Backend/AGENTS.md
- Backend/CLAUDE.md

If the change touches Frontend, also read:

- Frontend/quran-dashboard-ui/AGENTS.md
- Frontend/quran-dashboard-ui/CLAUDE.md

If the review is UI/layout/design-related, also follow the frontend/product/design guidance referenced by the project instructions (PRODUCT.md and DESIGN.md).

## Review Goals

Review the implementation for:

1. Clean Code

- Clear names.
- Small focused functions/classes/components.
- No vague names like DataItem, Obj, Temp, Info2.
- No unnecessary comments explaining obvious code.
- Code flow is readable.

2. SOLID

- Single Responsibility is respected.
- Abstractions are focused and useful.
- High-level logic does not depend directly on concrete infrastructure.
- Interfaces are not bloated.
- Implementations honor their contracts.

3. Architecture
   Backend:

- Controllers are thin.
- Business rules are in Domain/Application.
- Infrastructure handles database/files/external integrations.
- Application does not depend on Infrastructure.
- Domain remains independent.

Frontend:

- Components are not too large.
- Complex logic is not hidden inside templates.
- Reusable logic is in services/utilities.
- Typed models/interfaces are used.
- UI is organized under clear feature/shared/core/layout folders when applicable.

4. DRY / KISS / YAGNI

- No repeated business or validation logic.
- No unnecessary abstractions.
- No future features added without request.
- The solution is as simple as the requirement allows.

5. Strong Typing

- C# and TypeScript use explicit types.
- Avoid TypeScript any unless justified.
- Known values use enums/constants where appropriate.
- No magic strings or magic numbers when named constants would be clearer.

6. Focused Changes

- The change matches the requested scope.
- No unrelated files were changed.
- No broad refactors mixed into feature work.
- No UI redesign unless requested.

7. Error Handling

- Errors are actionable and specific.
- Raw internal exceptions are not exposed to users.
- Generic error messages are avoided when clearer messages are possible.

8. Quranic Data Safety

- No invented Quran text, ayah text, tafsir, translations, morphology, gates/topics, or religious content.
- Source data is not silently modified.
- Data problems are not hidden.
- Generated data preserves traceability.
- Data processors/importers/generators produce reports with totals, missing records, duplicates, warnings, and validation result.

9. UI/Product Consistency
   When reviewing Frontend/UI changes:

- Arabic-first.
- RTL-aware.
- Calm scholarly dashboard.
- Avoid generic SaaS style.
- Avoid kitschy religious decoration.
- Avoid gamified style.
- Avoid dense enterprise greige.
- Respect the project design direction.

10. Testing and Verification

- Build was run when relevant.
- Tests were run when available or logic is sensitive.
- Data-related work includes a validation/report path.
- Any skipped verification is clearly stated.

## Review Output Format

Return the review in this structure:

# Engineering Review

## Verdict

Use one of:

- PASS
- PASS WITH NOTES
- NEEDS CHANGES
- BLOCKED

## Summary

Briefly describe what was reviewed.

## Blocking Issues

List issues that must be fixed before merge.
If none, write:
None.

## Non-Blocking Notes

List improvements or observations.
If none, write:
None.

## Scope Check

State whether the change stayed within scope.

## Architecture Check

State whether Backend/Frontend architecture rules were respected.

## Quranic Data Safety Check

State whether any Quranic/source-sensitive data risk exists.

## Verification

Mention build/test/report status as provided or observed.
If unknown, say unknown.

## Changed Files Reviewed

List the changed files if known.

Rules:

- Be direct and practical.
- Do not invent facts.
- If build/test status is unknown, say unknown.
- Separate blocking issues from optional notes.
- Do not request large refactors unless necessary.
- Do not implement fixes unless explicitly asked.
