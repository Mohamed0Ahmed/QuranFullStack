# Deep Review Traversal Aid (optional)

An optional walk order for a thorough code-quality pass inside `engineering-review`. The
parent Skill owns severity, output format, and the verdict; this file adds no independent
rules. Canonical principles live in `CODING_PRINCIPLES.md` — §2 Clean Code and its
`Comment Policy`, §3 SOLID, §4 DRY/KISS/YAGNI, §7 Focused Changes — and the AI-specific
patterns in [ai-failure-modes.md](ai-failure-modes.md).

Before walking, classify the request: a **refactor review** (observable behavior must not
change; label any behavior-changing suggestion separately as "Behavior change — confirm
with author") or a **correctness review** (behavior findings in scope). Ask if unclear.

**A. Naming & functions (§2):** generic or vague identifiers; function length, parameter
count, and single responsibility; boolean flag arguments; value-returning functions that
also mutate observable state; predicates that mutate.

**B. Comments & formatting (§2 `Comment Policy`):** apply the canonical policy and its
scope boundary exactly — production source only; tests, `.claude/`, `.agents/`,
`.specify/`, `Backend/scripts/`, the DataImporter, build config, and generated files are
out of scope. Also check commented-out code and style consistency with the surrounding
file (casing, quoting, import order).

**C. SOLID (§3):** unrelated responsibilities in one class; type-tag dispatch that grows
with the codebase; substitution violations (unsupported operations, strengthened
preconditions); interfaces whose clients use only a subset; high-level code importing
concretes from low-level modules.

**D. DRY / KISS / YAGNI (§4):** knowledge duplication (≥5-line blocks — confirm it is
knowledge, not coincidental text, before recommending extraction); wrong abstractions
accumulating per-caller branches and flags; excessive branching or nesting; optional
parameters, config flags, single-implementation abstractions, and "swappable" wrappers
with no present-day need.

**E. AI failure modes:** walk the fourteen patterns in
[ai-failure-modes.md](ai-failure-modes.md) — the highest-leverage pass for generated code.

For each finding, quote the offending code (file + line), name the principle or failure
mode, and propose a concrete direction. Severity and the report shape come from the parent
Skill.
