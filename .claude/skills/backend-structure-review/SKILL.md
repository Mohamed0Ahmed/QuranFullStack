---
name: backend-structure-review
description: Use when asked for a focused review or placement decision about Quran Dashboard backend folders, files, layer boundaries, or file-responsibility thresholds.
---

# Backend Structure Review

## Responsibility

Answer an explicitly requested backend structure question with review-only findings or a
placement decision: file/folder placement by domain/feature (against technical-type
dumping folders like global `Enums/`, `Models/`, `DTOs/`, `Helpers/`, `Utils/`), Clean
Architecture layer boundaries and dependency direction, and file-size/responsibility
thresholds.

**Not this skill's job:** firing automatically on ordinary new files, the holistic
review or Quran/API/test/performance review (that is the explicitly requested
`engineering-review` or performance skill), implementing fixes, builds, tests, or Git.

## Workflow

1. Confirm the requested paths or question; review only that scope.
2. Read the relevant heading of `Backend/.architecture/BACKEND_STRUCTURE.md` — canonical
   for placement, feature/domain organization, and thresholds — and cite it rather than
   restating it.
3. Separate blocking findings from optional recommendations; do not over-engineer or
   request broad refactors unless necessary.

## Conditional context

- `Backend/.architecture/CLEAN_ARCHITECTURE.md` — only when layer responsibilities or
  dependency direction are implicated.
- `Backend/.architecture/API_GUIDELINES.md` — only when the API boundary is implicated.
- `Backend/.architecture/BACKEND_STRUCTURE.md` §File Size and Responsibility Guidelines —
  only when a size/responsibility threshold is implicated.
- `CODING_PRINCIPLES.md` §10 and
  `.claude/skills/engineering-review/references/quran-data-safety.md` — only when the
  reviewed structure touches source-sensitive Quran data or importer traceability.

Use neutral terminology per `SKILLS_AND_ARCHITECTURE_GUIDE.md` §Review terminology
("overloaded service", "oversized service" — never "God service").

## Output

# Backend Structure Review

- **Verdict:** PASS / PASS WITH NOTES / NEEDS CHANGES / BLOCKED
- **Scope reviewed** — paths/question and docs cited.
- **Findings** — blocking first, then placement/layering/size/anti-pattern notes.
- **Recommendations** — practical, scoped.

If a referenced document or the file tree is unavailable, say so rather than inventing
its rules. Do not implement fixes unless explicitly asked.
