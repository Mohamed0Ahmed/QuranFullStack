# Coding Principles

## 1. Context First

Before implementation work, follow the relevant workspace and project instruction files.

Read only the context documents that are relevant to the task:

- Backend/API/data tasks do not require design context unless the task affects product behavior or user-facing output.
- Frontend/UI/layout tasks must follow the project design and product context.
- Data-processing tasks must follow source-data safety and validation rules.

This document defines the general coding principles for the whole FullStack workspace.

## 2. Clean Code

- Use clear, descriptive names.
- Prefer small focused functions/classes/components.
- Each unit should have one clear responsibility.
- Avoid vague names like DataItem, Obj, Temp, Info2.
- Avoid unnecessary comments that explain obvious code.
- Code should read clearly without hidden surprises.

Deeper clean-code review guidance (naming/functions, comments/formatting, SOLID,
DRY/KISS/YAGNI, and AI-generated-code failure modes) lives under
`.claude/skills/engineering-review/references/clean-code-guard/`. It is reference
material for the `engineering-review` skill, not a separate skill.

Project overrides for that generic guidance:

- In C#/.NET code, project convention may use `I`-prefixed interface names (e.g.
  `IUserService`); this overrides the generic clean-code "no `I` prefix" naming rule.
- At the API boundary, the project `ApiResponse` contract and
  `Backend/.architecture/API_GUIDELINES.md` are authoritative; the generic clean-code
  "prefer exceptions over return codes" guidance applies inside layers only and must
  not replace the API response envelope.

## 3. SOLID

- Single Responsibility: one reason to change.
- Open/Closed: add behavior without breaking existing code where practical.
- Liskov Substitution: implementations must honor their contracts.
- Interface Segregation: prefer focused interfaces over large general-purpose ones.
- Dependency Inversion: high-level logic depends on abstractions, not concrete infrastructure.

## 4. DRY, KISS, YAGNI

- Do not duplicate business or validation logic.
- Prefer the simplest solution that satisfies the current requirement.
- Do not build future features or abstractions before they are needed.

## 5. Separation of Concerns

Backend:

- Controllers handle HTTP only.
- Application handles use cases.
- Domain holds business rules.
- Infrastructure handles database/files/external integrations.

Frontend:

- Components handle presentation and interaction.
- Services handle data loading and reusable logic.
- Models/interfaces define clear data contracts.
- Avoid complex domain logic directly inside templates.

## 6. Strong Typing

- Use explicit types in C# and TypeScript.
- Avoid TypeScript `any` unless there is a clear reason.
- Prefer enums/constants for known values.
- Avoid magic strings and magic numbers.

## 7. Focused Changes

- Keep each task scoped.
- Do not mix feature work, refactoring, UI redesign, data changes, and architecture changes unless explicitly requested.
- Do not touch unrelated files.

## 8. Error Handling

- Errors should be specific and actionable.
- Do not expose raw internal exceptions to users.
- Avoid generic messages when a clearer message is possible.

## 9. Testing and Verification

- Prioritize tests for data parsing, validation, mapping, importers, and business rules.
- Run build after relevant changes when possible.
- Run tests when tests exist or when logic is sensitive.
- Report build/test status in the final summary.
- Deeper test-code quality guidance lives in the `test-guard` skill
  (`.claude/skills/test-guard/`): use `references/dotnet.md` for backend tests and
  `references/jest.md` for frontend tests.
- Quranic Data Safety (§10) applies to tests too: never invent Quran text, tafsir,
  translations, morphology, roots, or gates in tests unless clearly synthetic.

## 10. Quranic Data Safety

- Quranic data is source-sensitive.
- Never invent Quran text, ayah text, tafsir, translations, morphology, gates/topics, or religious content.
- Never silently modify source data.
- Do not hide data problems.
- Keep traceability from generated data back to source files.
- Any data processor/importer/generator must produce a clear report with totals, missing records, duplicates, warnings, and validation result.

## 11. UI and Product Consistency

- Follow PRODUCT.md and DESIGN.md.
- Arabic-first and RTL-aware.
- Avoid generic SaaS dashboard style.
- Avoid kitschy religious decoration.
- Avoid gamified style.
- Avoid dense enterprise greige.
- For UI/layout/design tasks, follow the Impeccable guidance in the root instructions.

## 12. Definition of Done

Every implementation summary should include:

- Changed files
- Build status
- Test status if applicable
- Validation/report path if data-related
- Any skipped or uncertain items
