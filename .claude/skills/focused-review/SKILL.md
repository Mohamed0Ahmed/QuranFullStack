---
name: focused-review
description: Use when asked for a scoped Quran Dashboard review of one phase, task, fix, selected file set, or explicit architecture, security, or data-safety checkpoint.
---

# Focused Review

## Responsibility

Produce a scoped, read-only review of one explicitly named implementation slice — a
phase, task, fix, selected file set, or explicit architecture/security/data-safety
checkpoint — then stop. The owned result is exactly:

1. Freeze the requested scope and state it.
2. Inspect only that code/diff plus the minimum adjacent code needed to understand it.
3. Compare it with only the relevant slice of the active plan/spec/contract.
4. Load only the context implicated by the scope or a concrete candidate finding.
5. Report scoped findings and what was not reviewed; stop.

Never expand the scope: not from selected files to the branch, not from one phase to
the feature, not from one checkpoint to final readiness. A later focused re-review is
another explicit narrow request and closes no final boundary.

**Not this skill's job:** running builds, tests, or any verification; producing a
formal verdict; computing final evidence sufficiency; invoking another project Skill;
fixing findings; Git/PR/deployment or any other delivery work; loading the Spec Kit
formal add-on or the deep formal-review closure. The formal findings-and-verdict
review belongs to `engineering-review`. Performance review belongs to the performance
Skill when that is the requested review; test-code quality remains `test-guard`;
Backend placement/layering remains `backend-structure-review`.

## Conditional context (exact headings, only when implicated)

Always: the requested scope's code/diff plus its relevant active plan/spec/contract
slice. Conditionally load only:

- **Clean-code responsibility:** the implicated headings of `CODING_PRINCIPLES.md`
  §§2–4 and §7.
- **Area architecture:** the exact Backend **or** Frontend architecture/API/style
  headings for the scope's area — never both by default.
- **Auth/access scope:** `docs/contracts/security-access.md`, the nearest auth README,
  and `Backend/.architecture/API_GUIDELINES.md` §11 Security and Safety.
- **User-facing/product/visual scope only:** `PRODUCT.md`/`DESIGN.md`.
- **Quran scope only:** Quran safety (`CODING_PRINCIPLES.md` §10) plus the nearest
  source/rendering owner.
- **Supplied checkpoint evidence:** the exact `TESTING_CONSTITUTION.md` rule or owning Backend/E2E
  README section, only to label that evidence.

## Evidence, not execution

This review may state that relevant supplied evidence is current, missing, failed,
skipped, or unknown, but it never runs verification and never judges the whole
feature's final evidence union. Consume an existing same-diff Test Guard result when
supplied; its absence never causes an invocation or a promotion to formal review.

## Output

# Focused Review

- **Status:** CLEAR | FINDINGS
- **Scope reviewed:** exact scope and context consulted
- **Findings:** numbered and ordered BLOCKING -> MAJOR -> MINOR -> NOTE; "None." when
  clear
- **Evidence observed:** only when part of the requested checkpoint
- **Not reviewed:** explicit exclusions

Severity terms reuse the project's current meanings for ordering only.
`CLEAR`/`FINDINGS` is not a formal verdict and closes no final boundary.
